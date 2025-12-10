using System;
using System.Collections.Generic;
using System.Linq;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Domain;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure;
using Eshava.DomainDrivenDesign.CodeAnalysis.Templates.Application;
using Microsoft.CodeAnalysis;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Factories
{
	public static class ApplicationFactory
	{
		public static FactoryResult GenerateSourceCode(
			ApplicationProject applicationProjectConfig,
			IEnumerable<ApplicationUseCases> applicationUseCasesConfigs,
			DomainProject domainProjectConfig,
			IEnumerable<DomainModels> domainModelsConfigs,
			InfrastructureProject infrastructureProjectConfig,
			IEnumerable<InfrastructureModels> infrastructureModelsConfigs,
			List<UseCaseCodeSnippet> codeSnippets
		)
		{
			var domainModelsConfig = domainModelsConfigs.Merge();
			var infrastructureModelsConfig = infrastructureModelsConfigs.Merge();
			var applicationUseCasesConfig = applicationUseCasesConfigs.Merge();

			var factoryResult = new FactoryResult();
			var dependencyInjections = new List<DependencyInjection>();

			var referenceMap = DependencyAnalysis.Analyse(domainModelsConfig, infrastructureModelsConfig);
			var dtoReferenceMap = DtoDependencyAnalysis.Analyse(applicationUseCasesConfig, referenceMap, infrastructureModelsConfig);
			var useCasesMap = UseCaseAnalysis.Analyse(applicationProjectConfig, referenceMap, dtoReferenceMap, applicationUseCasesConfig);

			CreateInfrastructureProviderServiceInterface(factoryResult, useCasesMap, referenceMap, domainProjectConfig.FullQualifiedNamespace, applicationProjectConfig.FullQualifiedNamespace, applicationProjectConfig.AddAssemblyCommentToFiles);

			foreach (var @namespace in applicationUseCasesConfig.Namespaces)
			{
				var domainNamespace = $"{applicationProjectConfig.FullQualifiedNamespace}.{@namespace.Domain}";

				foreach (var useCase in @namespace.UseCases)
				{
					if (useCase.Type == ApplicationUseCaseType.SearchCount)
					{
						// will be generated automatically

						continue;
					}

					var templateRequest = new UseCaseTemplateRequest
					{
						Domain = @namespace.Domain,
						ApplicationProjectNamespace = applicationProjectConfig.FullQualifiedNamespace,
						DomainProjectNamespace = domainProjectConfig.FullQualifiedNamespace,
						UseCase = useCase,
						UseCaseNamespace = null,
						DomainModelReferenceMap = referenceMap,
						DtoReferenceMap = dtoReferenceMap,
						ScopedSettingsClass = applicationProjectConfig.ScopedSettingsClass,
						ScopedSettingsUsing = applicationProjectConfig.ScopedSettingsUsing,
						UseCasesMap = useCasesMap,
						CodeSnippets = codeSnippets,
						AlternativeClasses = applicationProjectConfig.AlternativeClasses,
						AddAssemblyCommentToFiles = applicationProjectConfig.AddAssemblyCommentToFiles
					};

					AddUseCase(factoryResult, templateRequest, domainNamespace, dependencyInjections);
				}
			}

			CreateQueryInfrastructureProviderServiceInterface(factoryResult, applicationProjectConfig.FullQualifiedNamespace, useCasesMap, applicationProjectConfig.AddAssemblyCommentToFiles);
			AddServiceCollection(factoryResult, applicationProjectConfig, dependencyInjections);

			return factoryResult;
		}

		private static void AddServiceCollection(FactoryResult factoryResult, ApplicationProject applicationProject, List<DependencyInjection> dependencyInjections)
		{
			if (dependencyInjections.Count == 0)
			{
				return;
			}

			var serviceCollection = ServiceCollectionExtensionTemplate.GetServiceCollection(applicationProject, dependencyInjections);
			var sourceName = $"{applicationProject.FullQualifiedNamespace}.Extensions.ServiceCollectionExtensions.g.cs";

			factoryResult.AddSource(sourceName, serviceCollection);
		}

		private static void AddUseCase(
			FactoryResult factoryResult,
			UseCaseTemplateRequest templateRequest,
			string domainNamespace,
			List<DependencyInjection> dependencyInjections
		)
		{
			var referenceDomainModelName = templateRequest.UseCase.GetDomainModelReferenceName();
			var featureNameNamespace = templateRequest.UseCase.FeatureName.IsNullOrEmpty() ? "" : $"{templateRequest.UseCase.FeatureName}.";
			var useCaseNamespace = $"{domainNamespace}.{featureNameNamespace}{templateRequest.UseCase.NamespaceClassificationKey.ToPlural()}.";

			var codeSnippets = templateRequest
				.CodeSnippets
				.Where(cs => cs.IsApplicable(templateRequest.UseCase.Type))
				.ToList();

			switch (templateRequest.UseCase.Type)
			{
				case ApplicationUseCaseType.Create:
				case ApplicationUseCaseType.Update:
				case ApplicationUseCaseType.Delete:
					useCaseNamespace += "Commands";

					break;

				case ApplicationUseCaseType.None:
				case ApplicationUseCaseType.Read:
				case ApplicationUseCaseType.Search:
				case ApplicationUseCaseType.SearchCount:
				case ApplicationUseCaseType.Unique:
				case ApplicationUseCaseType.Suggestions:
				default:
					useCaseNamespace += "Queries";

					break;
			}

			useCaseNamespace += $".{templateRequest.UseCase.UseCaseName}";
			templateRequest.UseCaseNamespace = useCaseNamespace;

			CreateDtos(factoryResult, templateRequest);
			CreateRequest(factoryResult, templateRequest, codeSnippets);
			CreateResponse(factoryResult, templateRequest);
			CreateInterface(factoryResult, templateRequest.UseCase, useCaseNamespace, templateRequest.AddAssemblyCommentToFiles);
			CreateUseCase(factoryResult, templateRequest, codeSnippets, dependencyInjections);

			if (templateRequest.UseCase.Type == ApplicationUseCaseType.Search)
			{
				templateRequest.UseCase.Type = ApplicationUseCaseType.SearchCount;
				templateRequest.UseCase.UseCaseName += "Count";
				useCaseNamespace += "Count";
				templateRequest.UseCaseNamespace = useCaseNamespace;

				CreateDtos(factoryResult, templateRequest);
				CreateRequest(factoryResult, templateRequest, codeSnippets);
				CreateResponse(factoryResult, templateRequest);
				CreateInterface(factoryResult, templateRequest.UseCase, useCaseNamespace, templateRequest.AddAssemblyCommentToFiles);
				CreateUseCase(factoryResult, templateRequest, codeSnippets, dependencyInjections);
			}
		}

		private static void CreateDtos(FactoryResult factoryResult, UseCaseTemplateRequest templateRequest)
		{
			if (templateRequest.UseCase.Type != ApplicationUseCaseType.SearchCount)
			{
				foreach (var dtoDefinition in templateRequest.UseCase.Dtos)
				{
					if (!templateRequest.DtoReferenceMap.TryGetDto(templateRequest.Domain, templateRequest.UseCase.UseCaseName, templateRequest.UseCase.NamespaceClassificationKey, dtoDefinition.Name, out var dtoMap))
					{
						continue;
					}

					var dtoName = dtoMap.DtoName;
					var domainModelName = dtoMap.DomainModelName;

					ReferenceDomainModelMap domainModelMap = null;
					if (templateRequest.UseCase.Type == ApplicationUseCaseType.Create || templateRequest.UseCase.Type == ApplicationUseCaseType.Update)
					{
						templateRequest.DomainModelReferenceMap.TryGetDomainModel(templateRequest.Domain, domainModelName, out domainModelMap);
					}

					var dto = DtoTemplate.GetDto(dtoMap, templateRequest.UseCaseNamespace, domainModelMap, templateRequest.AddAssemblyCommentToFiles);
					var dtoSourceName = $"{templateRequest.UseCaseNamespace}.{dtoName}.g.cs";

					factoryResult.AddSource(dtoSourceName, dto);

					if (templateRequest.UseCase.AddValidationConfigurationMethod)
					{
						var validationDto = DtoTemplate.GetValidationDto(dtoMap, templateRequest.UseCaseNamespace, domainModelMap, templateRequest.AddAssemblyCommentToFiles);
						var validationDtoSourceName = $"{templateRequest.UseCaseNamespace}.Validation{dtoName}.g.cs";

						factoryResult.AddSource(validationDtoSourceName, validationDto);
					}
				}
			}

			var returnDto = templateRequest.UseCase.Dtos.FirstOrDefault(dto => dto.Name == templateRequest.UseCase.MainDto);

			if (templateRequest.UseCase.Type == ApplicationUseCaseType.Search
				|| templateRequest.UseCase.Type == ApplicationUseCaseType.SearchCount
			)
			{
				var sortFieldsDto = FilterSortFieldsDtoTemplate.GetSortFieldsDto(templateRequest.UseCase, returnDto, templateRequest.UseCaseNamespace, templateRequest.AddAssemblyCommentToFiles);
				var sortFieldsDtoSourceName = $"{templateRequest.UseCaseNamespace}.{templateRequest.UseCase.ClassificationKey}{templateRequest.UseCase.UseCaseName}SortFields.g.cs";

				factoryResult.AddSource(sortFieldsDtoSourceName, sortFieldsDto);

				var filterFieldsDto = FilterFilterFieldsDtoTemplate.GetFilterFieldsDto(templateRequest.UseCase, returnDto, templateRequest.UseCaseNamespace, templateRequest.AddAssemblyCommentToFiles);
				var filterFieldsDtoSourceName = $"{templateRequest.UseCaseNamespace}.{templateRequest.UseCase.ClassificationKey}{templateRequest.UseCase.UseCaseName}FilterFields.g.cs";

				factoryResult.AddSource(filterFieldsDtoSourceName, filterFieldsDto);

				var filterDto = FilterDtoTemplate.GetFilterDto(templateRequest.UseCase, templateRequest.UseCaseNamespace, templateRequest.AddAssemblyCommentToFiles);
				var filterDtoSourceName = $"{templateRequest.UseCaseNamespace}.{templateRequest.UseCase.ClassificationKey}{templateRequest.UseCase.UseCaseName}Filter.g.cs";

				factoryResult.AddSource(filterDtoSourceName, filterDto);
			}
		}

		private static void CreateInterface(FactoryResult factoryResult, ApplicationUseCase useCase, string useCaseNamespace, bool addAssemblyCommentToFiles)
		{
			var @interface = UseCaseInterfaceTemplate.GetInterface(useCase, useCaseNamespace, addAssemblyCommentToFiles);
			var interfaceName = $"I{useCase.ClassName}";
			var interfaceSourceName = $"{useCaseNamespace}.{interfaceName}.g.cs";

			factoryResult.AddSource(interfaceSourceName, @interface);
		}

		private static void CreateUseCase(
			FactoryResult factoryResult,
			UseCaseTemplateRequest templateRequest,
			List<UseCaseCodeSnippet> codeSnippets,
			List<DependencyInjection> dependencyInjections
		)
		{
			if (templateRequest.UseCase.SkipUseCaseClass)
			{
				return;
			}

			var useCaseTemplate = "";

			switch (templateRequest.UseCase.Type)
			{
				case ApplicationUseCaseType.Read:
					useCaseTemplate = ReadUseCaseTemplate.GetUseCase(templateRequest, codeSnippets);

					break;
				case ApplicationUseCaseType.Search:
					useCaseTemplate = SearchUseCaseTemplate.GetUseCase(templateRequest, codeSnippets, false);

					break;
				case ApplicationUseCaseType.SearchCount:
					useCaseTemplate = SearchUseCaseTemplate.GetUseCase(templateRequest, codeSnippets, true);

					break;
				case ApplicationUseCaseType.Create:
					useCaseTemplate = CreateUseCaseTemplate.GetUseCase(templateRequest, codeSnippets);

					break;
				case ApplicationUseCaseType.Update:
					useCaseTemplate = UpdateUseCaseTemplate.GetUseCase(templateRequest, codeSnippets);

					break;
				case ApplicationUseCaseType.Delete:
					useCaseTemplate = DeactivateUseCaseTemplate.GetUseCase(templateRequest, codeSnippets);

					break;
				case ApplicationUseCaseType.Unique:
					useCaseTemplate = UniqueUseCaseTemplate.GetUseCase(templateRequest, codeSnippets);

					break;
				case ApplicationUseCaseType.Suggestions:
					useCaseTemplate = SuggestionsUseCaseTemplate.GetUseCase(templateRequest, codeSnippets);

					break;
			}

			var useCaseClassName = templateRequest.UseCase.ClassName;
			var useCaseInterfaceName = $"I{useCaseClassName}";
			var useCaseTemplateName = $"{templateRequest.UseCaseNamespace}.{useCaseClassName}.g.cs";

			if (!useCaseTemplate.IsNullOrEmpty())
			{
				factoryResult.AddSource(useCaseTemplateName, useCaseTemplate);

				dependencyInjections.Add(new DependencyInjection
				{
					Interface = useCaseInterfaceName,
					InterfaceUsing = templateRequest.UseCaseNamespace,
					Class = useCaseClassName,
					ClassUsing = templateRequest.UseCaseNamespace
				});
			}
		}

		private static void CreateInfrastructureProviderServiceInterface(FactoryResult factoryResult, UseCasesMap useCasesMap, ReferenceMap referenceMap, string domainProjectNamespace, string applicationNamespace, bool addAssemblyCommentToFiles)
		{
			var currentDomain = "";
			var domainNamespace = "";
			foreach (var domainModelMap in referenceMap.GetReferenceDomainModelMaps())
			{
				if (domainModelMap.IsChildDomainModel || domainModelMap.IsValueObject)
				{
					continue;
				}

				if (currentDomain != domainModelMap.Domain)
				{
					currentDomain = domainModelMap.Domain;
					domainNamespace = $"{applicationNamespace}.{domainModelMap.Domain}";
				}

				var featureName = useCasesMap.GetFeatureName(domainModelMap.Domain, domainModelMap.ClassificationKey);
				var featureNameNamespace = featureName.IsNullOrEmpty() ? "" : $"{featureName}.";
				var applicationDomainModelNamespace = $"{domainNamespace}.{featureNameNamespace}{domainModelMap.ClassificationKey.ToPlural()}.Commands";

				var @interface = InfrastructureProviderServiceInterfaceTemplate.GetInterface(domainModelMap, $"{domainProjectNamespace}.{domainModelMap.Domain}.{domainModelMap.DomainModel.NamespaceDirectory}", applicationDomainModelNamespace, addAssemblyCommentToFiles);
				var interfaceSourceName = $"{applicationDomainModelNamespace}.I{domainModelMap.DomainModelName}InfrastructureProviderService.g.cs";

				factoryResult.AddSource(interfaceSourceName, @interface);
			}
		}

		private static void CreateQueryInfrastructureProviderServiceInterface(
			FactoryResult factoryResult,
			string applicationFullQualifiedNamespace,
			UseCasesMap useCasesMap,
			bool addAssemblyCommentToFiles
		)
		{
			foreach (var queryProvider in useCasesMap.GetUseCaseQueryProviderMethodMaps())
			{
				var featureNameNamespace = queryProvider.FeatureName.IsNullOrEmpty() ? "" : $"{queryProvider.FeatureName}.";
				var applicationDomainModelNamespace = $"{applicationFullQualifiedNamespace}.{queryProvider.Domain}.{featureNameNamespace}{queryProvider.ClassificationKey.ToPlural()}.Queries";

				var @interface = QueryInfrastructureProviderServiceInterfaceTemplate.GetInterface(queryProvider, applicationDomainModelNamespace, addAssemblyCommentToFiles);
				var interfaceSourceName = $"{applicationDomainModelNamespace}.I{queryProvider.ClassificationKey}QueryInfrastructureProviderService.g.cs";

				factoryResult.AddSource(interfaceSourceName, @interface);
			}
		}

		private static void CreateRequest(FactoryResult factoryResult, UseCaseTemplateRequest templateRequest, List<UseCaseCodeSnippet> codeSnippets)
		{
			var request = UseCaseRequestTemplate.GetRequest(templateRequest.UseCase, templateRequest.Domain, templateRequest.UseCaseNamespace, templateRequest.DomainModelReferenceMap, templateRequest.DtoReferenceMap, codeSnippets, templateRequest.AddAssemblyCommentToFiles);
			var requestSourceName = $"{templateRequest.UseCaseNamespace}.{templateRequest.UseCase.RequestType}.g.cs";

			factoryResult.AddSource(requestSourceName, request);
		}

		private static void CreateResponse(FactoryResult factoryResult, UseCaseTemplateRequest templateRequest)
		{
			var response = UseCaseResponseTemplate.GetResponse(templateRequest.UseCase, templateRequest.Domain, templateRequest.UseCaseNamespace, templateRequest.DomainModelReferenceMap, templateRequest.AddAssemblyCommentToFiles);
			var responseSourceName = $"{templateRequest.UseCaseNamespace}.{templateRequest.UseCase.ResponseType}.g.cs";

			factoryResult.AddSource(responseSourceName, response);
		}
	}
}