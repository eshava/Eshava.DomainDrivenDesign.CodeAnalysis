using System.Collections.Generic;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Api;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Domain;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure;
using Eshava.DomainDrivenDesign.CodeAnalysis.Templates.Api;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Factories
{
	public static class ApiFactory
	{
		public static FactoryResult GenerateSourceCode(
			ApiProject apiProjectConfig,
			IEnumerable<ApiRoutes> apiRoutesConfigs,
			ApplicationProject applicationProjectConfig,
			IEnumerable<ApplicationUseCases> applicationUseCasesConfigs,
			DomainProject domainProjectConfig,
			IEnumerable<DomainModels> domainModelsConfigs,
			InfrastructureProject infrastructureProjectConfig,
			IEnumerable<InfrastructureModels> infrastructureModelsConfigs,
			List<ApiRouteCodeSnippet> codeSnippets
		)
		{
			var domainModelsConfig = domainModelsConfigs.Merge();
			var infrastructureModelsConfig = infrastructureModelsConfigs.Merge();
			var applicationUseCasesConfig = applicationUseCasesConfigs.Merge();
			var apiRoutesConfig = apiRoutesConfigs.Merge();

			var factoryResult = new FactoryResult();
			var dependencyInjections = new List<DependencyInjection>();

			var referenceMap = DependencyAnalysis.Analyse(domainModelsConfig, infrastructureModelsConfig);
			var dtoReferenceMap = DtoDependencyAnalysis.Analyse(applicationUseCasesConfig, referenceMap, infrastructureModelsConfig);
			var useCasesMap = UseCaseAnalysis.Analyse(applicationProjectConfig, referenceMap, dtoReferenceMap, applicationUseCasesConfig);


			foreach (var @namespace in apiRoutesConfig.Routes)
			{
				if (@namespace.Endpoints.Count == 0)
				{
					continue;
				}

				CreateRoutes(factoryResult, apiProjectConfig, @namespace, useCasesMap, dependencyInjections, codeSnippets);
			}

			WebApplicationExtension(factoryResult, apiProjectConfig, dependencyInjections);

			return factoryResult;
		}

		private static void CreateRoutes(
			FactoryResult factoryResult,
			ApiProject apiProjectConfig,
			ApiRouteNamespace @namespace,
			UseCasesMap useCasesMap,
			List<DependencyInjection> dependencyInjections,
			List<ApiRouteCodeSnippet> codeSnippets
		)
		{
			var endpointNamespace = apiProjectConfig.EndpointNamespace.IsNullOrEmpty()
				? ""
				: $".{apiProjectConfig.EndpointNamespace}";
			var apiNamespace = $"{apiProjectConfig.FullQualifiedNamespace}{endpointNamespace}.{@namespace.Namespace}";

			var routesClassName = @namespace.Name + "Routes";
			var routesTemplate = EndpointRouteTemplate.GetRoute(
				@namespace.Endpoints, 
				routesClassName, 
				apiNamespace, 
				apiProjectConfig.ResponseDataExtensionsUsing, 
				apiProjectConfig.ErrorResponseClass, 
				apiProjectConfig.ErrorResponseUsing, 
				useCasesMap, 
				codeSnippets, 
				apiProjectConfig.AddAssemblyCommentToFiles
			);

			if (!routesTemplate.IsNullOrEmpty())
			{
				var routesTemplateName = $"{apiNamespace}.{routesClassName}.g.cs";

				factoryResult.AddSource(routesTemplateName, routesTemplate);

				dependencyInjections.Add(new DependencyInjection
				{
					Class = routesClassName,
					ClassUsing = apiNamespace
				});
			}
		}

		private static void WebApplicationExtension(FactoryResult factoryResult, ApiProject apiProject, List<DependencyInjection> dependencyInjections)
		{
			if (dependencyInjections.Count == 0)
			{
				return;
			}

			var webApplicationExtension = WebApplicationExtensionTemplate.GetWebApplicationExtension(apiProject, dependencyInjections);
			var sourceName = $"{apiProject.FullQualifiedNamespace}.Extensions.WebApplicationExtensions.g.cs";

			factoryResult.AddSource(sourceName, webApplicationExtension);
		}
	}
}