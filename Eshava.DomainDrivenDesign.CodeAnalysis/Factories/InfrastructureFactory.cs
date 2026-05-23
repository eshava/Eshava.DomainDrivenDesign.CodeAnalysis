using System;
using System.Collections.Generic;
using System.Linq;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Domain;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure;
using Eshava.DomainDrivenDesign.CodeAnalysis.Templates.Infrastructure;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Factories
{
	public static class InfrastructureFactory
	{
		public static FactoryResult GenerateSourceCode(
			ApplicationProject applicationProjectConfig,
			IEnumerable<ApplicationUseCases> applicationUseCasesConfigs,
			DomainProject domainProjectConfig,
			IEnumerable<DomainModels> domainModelsConfigs,
			InfrastructureProject infrastructureProjectConfig,
			IEnumerable<InfrastructureModels> infrastructureModelsConfigs,
			IEnumerable<InfrastructureCodeSnippet> codeSnippets
		)
		{
			codeSnippets ??= [];

			var domainModelsConfig = domainModelsConfigs.Merge();
			var infrastructureModelsConfig = infrastructureModelsConfigs.Merge();
			var applicationUseCasesConfig = applicationUseCasesConfigs.Merge();

			infrastructureModelsConfig.Namespaces.ForEach(ns => CheckReferenceProperties(ns.Models));

			var referenceMap = domainModelsConfig is null
				? new ReferenceMap()
				: DependencyAnalysis.Analyse(domainModelsConfig, infrastructureModelsConfig);

			var dtoReferenceMap = applicationUseCasesConfig is null || domainModelsConfig is null
				? new DtoReferenceMap()
				: DtoDependencyAnalysis.Analyse(applicationUseCasesConfig, referenceMap, infrastructureModelsConfig);

			var useCasesMap = applicationUseCasesConfig is null || domainModelsConfig is null
				? new UseCasesMap()
				: UseCaseAnalysis.Analyse(applicationProjectConfig, referenceMap, dtoReferenceMap, applicationUseCasesConfig);

			var infrastructureModels = infrastructureModelsConfig.Namespaces
				.ToDictionary(
					d => d.Domain,
					d => d.Models.ToDictionary(m => m.Name, m => m.ClassificationKey)
				);

			foreach (var useCaseMap in useCasesMap.GetUseCaseMaps())
			{
				CheckDownwardCompatibility(useCaseMap.Domain, useCaseMap.UseCase, dtoReferenceMap);
			}

			var factoryResult = new FactoryResult();

			var dependencyInjections = new List<DependencyInjection>();
			var dependencyInjectionsDbConfigurations = new List<DependencyInjection>();
			var dependencyInjectionsTransformationProfiles = new List<DependencyInjection>();

			var infrastructureModelsByDomainAndName = new Dictionary<string, Dictionary<string, InfrastructureModel>>();
			foreach (var @namespace in infrastructureModelsConfig.Namespaces)
			{
				if (!infrastructureModelsByDomainAndName.TryGetValue(@namespace.Domain, out var modelsForDomain))
				{
					modelsForDomain = new Dictionary<string, InfrastructureModel>();
					infrastructureModelsByDomainAndName[@namespace.Domain] = modelsForDomain;
				}

				foreach (var model in @namespace.Models)
				{
					modelsForDomain.Add(model.Name, model);
				}
			}

			foreach (var @namespace in infrastructureModelsConfig.Namespaces)
			{
				var environment = new InfrastructureEnvironment
				{
					Project = infrastructureProjectConfig,
					InfrastructureNamespaceWithDomain = $"{infrastructureProjectConfig.FullQualifiedNamespace}.{@namespace.Domain}",
					ApplicationNamespaceWithDomain = $"{applicationProjectConfig.FullQualifiedNamespace}.{@namespace.Domain}",
					DomainProjectNamespace = domainProjectConfig.FullQualifiedNamespace,
					Domain = @namespace.Domain,
					ModelsByDomainAndName = infrastructureModelsByDomainAndName,
					CodeSnippets = codeSnippets
				};

				AddTransformProfiles(
					factoryResult,
					infrastructureProjectConfig.FullQualifiedNamespace,
					applicationProjectConfig?.FullQualifiedNamespace,
					@namespace.Domain,
					infrastructureModels,
					applicationUseCasesConfig?.Namespaces.FirstOrDefault(ns => ns.Domain == @namespace.Domain)?.UseCases ?? new List<ApplicationUseCase>(),
					dependencyInjectionsTransformationProfiles,
					infrastructureProjectConfig.AddAssemblyCommentToFiles
				);

				var childsForModel = @namespace.Models
					.Where(m => !m.ReferencedParent.IsNullOrEmpty())
					.GroupBy(m => m.ReferencedParent)
					.ToDictionary(m => m.Key, m => m.Select(c => c).ToList());

				// Workaround to allow multiple data models for the same classification key
				// It's risky, because query repository are based on classification key.
				// First come, first serve. The first data model reference occurence during, query provider map collection, will be use.
				// The other data model will be ignored.
				// Use this feature only, if all data models for the same classification key are equal.
				// Purpose: Two domain models, one as separate domain model, the other as aggregate child
				var processedClassificationKeys = new HashSet<string>();

				foreach (var model in @namespace.Models)
				{
					var databaseModel = DataModelTemplate.GetDatabaseModel(model, @namespace.Domain, environment.InfrastructureNamespaceWithDomain, infrastructureProjectConfig.AlternativeAbstractDatabaseModel, infrastructureProjectConfig.AlternativeUsing, childsForModel, infrastructureModels, infrastructureProjectConfig.AddAssemblyCommentToFiles);
					var databaseModelSourceName = $"{environment.InfrastructureNamespaceWithDomain}.{model.ClassificationKey.ToPlural()}.{model.Name}.g.cs";

					factoryResult.AddSource(databaseModelSourceName, databaseModel);

					if (!model.TableName.IsNullOrEmpty() && model.CreateDbConfiguration)
					{
						var dbConfigModel = DbConfigurationTemplate.GetDbConfiguration(model, @namespace.DatabaseSchema, environment.InfrastructureNamespaceWithDomain, infrastructureProjectConfig.AddAssemblyCommentToFiles);
						var dbConfigModelSourceName = $"{environment.InfrastructureNamespaceWithDomain}.{model.ClassificationKey.ToPlural()}.{model.Name}DbConfiguration.g.cs";

						factoryResult.AddSource(dbConfigModelSourceName, dbConfigModel);

						dependencyInjectionsDbConfigurations.Add(new DependencyInjection
						{
							Class = $"{model.Name}DbConfiguration",
							ClassUsing = $"{environment.InfrastructureNamespaceWithDomain}.{model.ClassificationKey.ToPlural()}"
						});
					}

					AddCreationBag(
						factoryResult,
						model,
						referenceMap,
						environment
					);

					AddRepository(
						factoryResult,
						model,
						@namespace.DatabaseSettingsInterface,
						@namespace.DatabaseSettingsInterfaceUsing,
						infrastructureProjectConfig,
						childsForModel,
						referenceMap,
						dependencyInjections,
						environment
					);

					if (model.CreateProviderService)
					{
						AddProviderService(
							factoryResult,
							model,
							@namespace.DatabaseSettingsInterface,
							@namespace.DatabaseSettingsInterfaceUsing,
							childsForModel,
							referenceMap,
							useCasesMap,
							dependencyInjections,
							environment
						);
					}

					if (useCasesMap.TryGetQueryProvider(@namespace.Domain, model.ClassificationKey, out var queryProviderMap) && !processedClassificationKeys.Contains(model.ClassificationKey))
					{
						processedClassificationKeys.Add(model.ClassificationKey);

						AddQueryProviderService(
							factoryResult,
							model,
							queryProviderMap,
							dependencyInjections,
							environment
						);

						AddQueryRepository(
							factoryResult,
							model,
							childsForModel,
							@namespace.DatabaseSettingsInterface,
							@namespace.DatabaseSettingsInterfaceUsing,
							queryProviderMap,
							dependencyInjections,
							environment
						);
					}
				}
			}

			AddServiceCollection(factoryResult, infrastructureProjectConfig, dependencyInjections, dependencyInjectionsDbConfigurations, dependencyInjectionsTransformationProfiles);

			return factoryResult;
		}

		private static void AddServiceCollection(
			FactoryResult factoryResult,
			InfrastructureProject infrastructureProject,
			List<DependencyInjection> dependencyInjections,
			List<DependencyInjection> dependencyInjectionsDbConfigurations,
			List<DependencyInjection> dependencyInjectionsTransformationProfiles
		)
		{
			var serviceCollection = ServiceCollectionExtensionTemplate.GetServiceCollection(infrastructureProject, dependencyInjections, dependencyInjectionsDbConfigurations, dependencyInjectionsTransformationProfiles);
			var sourceName = $"{infrastructureProject.FullQualifiedNamespace}.Extensions.ServiceCollectionExtensions.g.cs";

			factoryResult.AddSource(sourceName, serviceCollection);
		}

		private static void AddProviderService(
			FactoryResult factoryResult,
			InfrastructureModel model,
			string databaseSettingsInterface,
			string databaseSettingsInterfaceUsing,
			Dictionary<string, List<InfrastructureModel>> childsForModel,
			ReferenceMap domainModelReferenceMap,
			UseCasesMap useCasesMap,
			List<DependencyInjection> dependencyInjections,
			InfrastructureEnvironment environment
		)
		{
			if (!model.CreateProviderService)
			{
				return;
			}

			if (!domainModelReferenceMap.TryGetDomainModelByDataModel(environment.Domain, model.Name, out var domainModels))
			{
				domainModels = Array.Empty<ReferenceDomainModelMap>();
			}

			if (domainModels.Any())
			{
				foreach (var domainModel in domainModels)
				{
					AddProviderService(
						factoryResult,
						model,
						databaseSettingsInterface,
						databaseSettingsInterfaceUsing,
						childsForModel,
						domainModel,
						useCasesMap,
						dependencyInjections,
						environment
					);
				}
			}
			else
			{
				AddProviderService(
					factoryResult,
					model,
					databaseSettingsInterface,
					databaseSettingsInterfaceUsing,
					childsForModel,
					(ReferenceDomainModelMap)null,
					useCasesMap,
					dependencyInjections,
					environment
				);
			}
		}

		private static void AddProviderService(
			FactoryResult factoryResult,
			InfrastructureModel model,
			string databaseSettingsInterface,
			string databaseSettingsInterfaceUsing,
			Dictionary<string, List<InfrastructureModel>> childsForModel,
			ReferenceDomainModelMap domainModel,
			UseCasesMap useCasesMap,
			List<DependencyInjection> dependencyInjections,
			InfrastructureEnvironment environment
		)
		{
			if (domainModel is null || domainModel.IsChildDomainModel)
			{
				return;
			}

			var featureName = useCasesMap.GetFeatureName(environment.Domain, model.ClassificationKey);
			if (!featureName.IsNullOrEmpty())
			{
				featureName += ".";
			}

			var providerService = InfrastructureProviderServiceTemplate.GetProviderService(
				model,
				domainModel,
				featureName,
				databaseSettingsInterface,
				databaseSettingsInterfaceUsing,
				childsForModel,
				environment
			);

			var providerServiceSourceName = $"{environment.InfrastructureNamespaceWithDomain}.{model.ClassificationKey.ToPlural()}.{domainModel.DomainModelName}InfrastructureProviderService.g.cs";

			factoryResult.AddSource(providerServiceSourceName, providerService);

			dependencyInjections.Add(new DependencyInjection
			{
				Interface = $"I{domainModel.DomainModelName}InfrastructureProviderService",
				InterfaceUsing = $"{environment.ApplicationNamespaceWithDomain}.{featureName}{model.ClassificationKey.ToPlural()}.Commands",
				Class = $"{domainModel.DomainModelName}InfrastructureProviderService",
				ClassUsing = $"{environment.InfrastructureNamespaceWithDomain}.{model.ClassificationKey.ToPlural()}"
			});
		}

		private static void AddQueryProviderService(
			FactoryResult factoryResult,
			InfrastructureModel model,
			QueryProviderMap queryProviderMap,
			List<DependencyInjection> dependencyInjections,
			InfrastructureEnvironment environment
		)
		{
			var providerService = QueryInfrastructureProviderServiceTemplate.GetProviderService(
				model,
				queryProviderMap,
				environment
			);

			var featureNameNamespace = queryProviderMap.FeatureName.IsNullOrEmpty() ? "" : $"{queryProviderMap.FeatureName}.";
			var providerServiceSourceName = $"{environment.InfrastructureNamespaceWithDomain}.{model.ClassificationKey.ToPlural()}.{model.ClassificationKey}QueryInfrastructureProviderService.g.cs";

			factoryResult.AddSource(providerServiceSourceName, providerService);

			dependencyInjections.Add(new DependencyInjection
			{
				Interface = $"I{model.ClassificationKey}QueryInfrastructureProviderService",
				InterfaceUsing = $"{environment.ApplicationNamespaceWithDomain}.{featureNameNamespace}{model.ClassificationKey.ToPlural()}.Queries",
				Class = $"{model.ClassificationKey}QueryInfrastructureProviderService",
				ClassUsing = $"{environment.InfrastructureNamespaceWithDomain}.{model.ClassificationKey.ToPlural()}"
			});
		}

		private static void AddRepository(
			FactoryResult factoryResult,
			InfrastructureModel model,
			string databaseSettingsInterface,
			string databaseSettingsInterfaceUsing,
			InfrastructureProject infrastructureProject,
			Dictionary<string, List<InfrastructureModel>> childsForModel,
			ReferenceMap domainModelReferenceMap,
			List<DependencyInjection> dependencyInjections,
			InfrastructureEnvironment environment
		)
		{
			if (!model.CreateRepository)
			{
				return;
			}

			if (!domainModelReferenceMap.TryGetDomainModelByDataModel(environment.Domain, model.Name, out var domainModels))
			{
				domainModels = Array.Empty<ReferenceDomainModelMap>();
			}

			InfrastructureModel parent = null;
			if (domainModels.Any(dm => dm.IsChildDomainModel))
			{
				parent = environment.ModelsByDomainAndName[environment.Domain].Values.FirstOrDefault(m => m.Name == model.ReferencedParent);
			}

			if (domainModels.Any())
			{
				foreach (var domainModel in domainModels)
				{
					AddRepository(
					factoryResult,
					parent,
					model,
					databaseSettingsInterface,
					databaseSettingsInterfaceUsing,
					infrastructureProject,
					childsForModel,
					domainModel,
					domainModelReferenceMap,
					dependencyInjections,
					environment
				);
				}
			}
			else
			{
				AddRepository(
					factoryResult,
					parent,
					model,
					databaseSettingsInterface,
					databaseSettingsInterfaceUsing,
					infrastructureProject,
					childsForModel,
					null,
					domainModelReferenceMap,
					dependencyInjections,
					environment
				);
			}
		}

		private static void AddRepository(
			FactoryResult factoryResult,
			InfrastructureModel parent,
			InfrastructureModel model,
			string databaseSettingsInterface,
			string databaseSettingsInterfaceUsing,
			InfrastructureProject infrastructureProject,
			Dictionary<string, List<InfrastructureModel>> childsForModel,
			ReferenceDomainModelMap domainModel,
			ReferenceMap domainModelReferenceMap,
			List<DependencyInjection> dependencyInjections,
			InfrastructureEnvironment environment
		)
		{
			if (domainModel is null)
			{
				return;
			}

			var repositoryInterface = RepositoryInterfaceTemplate.GetInterface(
				model,
				domainModel,
				environment
			);

			var repositoryInterfaceSourceName = $"{environment.InfrastructureNamespaceWithDomain}.{model.ClassificationKey.ToPlural()}.I{domainModel.DomainModelName}Repository.g.cs";

			factoryResult.AddSource(repositoryInterfaceSourceName, repositoryInterface);

			var repository = RepositoryTemplate.GetRepository(
				model,
				domainModel,
				databaseSettingsInterface,
				databaseSettingsInterfaceUsing,
				parent,
				childsForModel,
				domainModelReferenceMap,
				environment
			);

			var repositorySourceName = $"{environment.InfrastructureNamespaceWithDomain}.{model.ClassificationKey.ToPlural()}.{domainModel.DomainModelName}Repository.g.cs";

			factoryResult.AddSource(repositorySourceName, repository);

			dependencyInjections.Add(new DependencyInjection
			{
				Interface = $"I{domainModel.DomainModelName}Repository",
				InterfaceUsing = $"{environment.InfrastructureNamespaceWithDomain}.{model.ClassificationKey.ToPlural()}",
				Class = $"{domainModel.DomainModelName}Repository",
				ClassUsing = $"{environment.InfrastructureNamespaceWithDomain}.{model.ClassificationKey.ToPlural()}"
			});
		}

		private static void AddQueryRepository(
			FactoryResult factoryResult,
			InfrastructureModel model,
			Dictionary<string, List<InfrastructureModel>> childsForModel,
			string databaseSettingsInterface,
			string databaseSettingsInterfaceUsing,
			QueryProviderMap queryProviderMap,
			List<DependencyInjection> dependencyInjections,
			InfrastructureEnvironment environment
		)
		{
			var @interface = QueryRepositoryInterfaceTemplate.GetInterface(model, queryProviderMap, environment);
			var interfaceSourceName = $"{environment.InfrastructureNamespaceWithDomain}.{model.ClassificationKey.ToPlural()}.I{model.ClassificationKey}QueryRepository.g.cs";

			factoryResult.AddSource(interfaceSourceName, @interface);

			var repository = QueryRepositoryTemplate.GetRepository(
				model,
				childsForModel,
				queryProviderMap,
				databaseSettingsInterface,
				databaseSettingsInterfaceUsing,
				environment
			);

			var repositorySourceName = $"{environment.InfrastructureNamespaceWithDomain}.{model.ClassificationKey.ToPlural()}.{model.ClassificationKey}QueryRepository.g.cs";

			factoryResult.AddSource(repositorySourceName, repository);

			dependencyInjections.Add(new DependencyInjection
			{
				Interface = $"I{model.ClassificationKey}QueryRepository",
				InterfaceUsing = $"{environment.InfrastructureNamespaceWithDomain}.{model.ClassificationKey.ToPlural()}",
				Class = $"{model.ClassificationKey}QueryRepository",
				ClassUsing = $"{environment.InfrastructureNamespaceWithDomain}.{model.ClassificationKey.ToPlural()}"
			});
		}

		private static void AddTransformProfiles(
			FactoryResult factoryResult,
			string infrastructureProjectNamespace,
			string applicationProjectNamespace,
			string domain,
			Dictionary<string, Dictionary<string, string>> infrastructureModels,
			IEnumerable<ApplicationUseCase> useCases,
			List<DependencyInjection> dependencyInjections,
			bool addAssemblyCommentToFiles
		)
		{
			var transformationProfile = TransformProfileTemplate.GetTransformProfile(
				infrastructureProjectNamespace,
				applicationProjectNamespace,
				domain,
				infrastructureModels,
				useCases,
				addAssemblyCommentToFiles
			);

			if (transformationProfile.IsNullOrEmpty())
			{
				return;
			}

			var transformationProfileSourceName = $"{infrastructureProjectNamespace}.{domain}.{domain}AutoGenTransformProfile.g.cs";

			factoryResult.AddSource(transformationProfileSourceName, transformationProfile);

			dependencyInjections.Add(new DependencyInjection
			{
				Class = $"{domain}AutoGenTransformProfile",
				ClassUsing = $"{infrastructureProjectNamespace}.{domain}"
			});
		}

		private static void AddCreationBag(
			FactoryResult factoryResult,
			InfrastructureModel model,
			ReferenceMap domainModelReferenceMap,
			InfrastructureEnvironment environment
		)
		{
			if (!model.CreateCreationBag)
			{
				return;
			}

			if (!domainModelReferenceMap.TryGetDomainModelByDataModel(environment.Domain, model.Name, out var domainModels))
			{
				domainModels = Array.Empty<ReferenceDomainModelMap>();
			}

			if (domainModels.Any())
			{
				foreach (var domainModel in domainModels)
				{
					AddCreationBag(
						factoryResult,
						model,
						domainModel,
						environment
					);
				}
			}
		}

		private static void AddCreationBag(
			FactoryResult factoryResult,
			InfrastructureModel model,
			ReferenceDomainModelMap domainModel,
			InfrastructureEnvironment environment
		)
		{
			var creationBag = CreationBagTemplate.GetCreationBag(model, domainModel, environment);
			var creationBagSourceName = $"{environment.InfrastructureNamespaceWithDomain}.{model.ClassificationKey.ToPlural()}.{domainModel.DomainModelName}CreationBag.g.cs";

			factoryResult.AddSource(creationBagSourceName, creationBag);
		}

		private static void CheckReferenceProperties(IEnumerable<InfrastructureModel> models)
		{
			foreach (var model in models)
			{
				var foreignKeyProperties = model.Properties.Where(p => p.IsReference).ToList();
				foreach (var foreignKeyProperty in foreignKeyProperties)
				{
					if (foreignKeyProperty.IsParentReference && model.ReferencedParent.IsNullOrEmpty())
					{
						model.ReferencedParent = foreignKeyProperty.ReferenceType;
					}

					if (model.Properties.FirstOrDefault(p => p.ReferencePropertyName == foreignKeyProperty.Name) is null)
					{
						var propertyName = foreignKeyProperty.Name.EndsWith("Id")
							? foreignKeyProperty.Name.Substring(0, foreignKeyProperty.Name.Length - 2)
							: foreignKeyProperty.Name + "Reference";

						model.Properties.Add(new InfrastructureModelProperty
						{
							Type = foreignKeyProperty.ReferenceType,
							Name = propertyName,
							ReferencePropertyName = foreignKeyProperty.Name,
							ReferenceType = foreignKeyProperty.ReferenceType,
							ReferenceDomain = foreignKeyProperty.ReferenceDomain,
							SkipFromDomainModel = true
						});
					}
				}
			}
		}

		private static void CheckDownwardCompatibility(string domain, ApplicationUseCase useCase, DtoReferenceMap dtoReferenceMap)
		{
#pragma warning disable CS0618 // Type or member is obsolete
			if (!useCase.DataModelTypeProperty.IsNullOrEmpty()
				&& !useCase.MainDto.IsNullOrEmpty()
				&& dtoReferenceMap.TryGetDto(domain, useCase.UseCaseName, useCase.ClassificationKey, useCase.MainDto, out var mainDtoMap)
				&& mainDtoMap.Dto.DataModelTypeProperty.IsNullOrEmpty())
			{
				mainDtoMap.Dto.DataModelTypeProperty = useCase.DataModelTypeProperty;
				mainDtoMap.Dto.DataModelTypePropertyValue = useCase.DataModelTypePropertyValue;
			}
#pragma warning restore CS0618 // Type or member is obsolete
		}
	}
}