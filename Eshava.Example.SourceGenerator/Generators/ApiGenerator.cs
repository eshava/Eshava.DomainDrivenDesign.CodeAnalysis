using System.Collections.Generic;
using System.Linq;
using Eshava.CodeAnalysis.Extensions;
using Eshava.Example.SourceGenerator.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Factories;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Api;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Domain;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure;
using Microsoft.CodeAnalysis;

namespace Eshava.Example.SourceGenerator.Generators
{
	/// <summary>
	/// https://roslynquoter.azurewebsites.net/
	/// </summary>
	[Generator(LanguageNames.CSharp)]
	public class ApiGenerator : IIncrementalGenerator
	{
		public void Initialize(IncrementalGeneratorInitializationContext initContext)
		{
			var configurationFiles = initContext.ReadConfigurationFiles(true, true, true, true, ConfigurationFileTypes.ApiProject);
			var codeSnippeds = GetCodeSnippets();

			initContext.RegisterSourceOutput(configurationFiles, (context, configurationFile) =>
			{
				if (!configurationFile.Any(f => f.Type == ConfigurationFileTypes.Activator))
				{
					return;
				}

				var apiProjectConfig = configurationFile.FirstOrDefault(f => f.Type == ConfigurationFileTypes.ApiProject)?.Parse<ApiProject>();
				var apiRoutesConfigs = configurationFile.Where(f => f.Type == ConfigurationFileTypes.ApiRoutes).Select(f => f.Parse<ApiRoutes>()).ToList();

				var applicationProjectConfig = configurationFile.FirstOrDefault(f => f.Type == ConfigurationFileTypes.ApplicationProject)?.Parse<ApplicationProject>();
				var applicationUseCasesConfigs = configurationFile.Where(f => f.Type == ConfigurationFileTypes.ApplicationUseCases).Select(f => f.Parse<ApplicationUseCases>()).ToList();

				var domainProjectConfig = configurationFile.FirstOrDefault(f => f.Type == ConfigurationFileTypes.DomainProject)?.Parse<DomainProject>();
				var domainModelsConfigs = configurationFile.Where(f => f.Type == ConfigurationFileTypes.DomainModels).Select(f => f.Parse<DomainModels>()).ToList();

				var infrastructureProjectConfig = configurationFile.FirstOrDefault(f => f.Type == ConfigurationFileTypes.InfrastructureProject)?.Parse<InfrastructureProject>() ?? new InfrastructureProject();
				var infrastructureModelsConfigs = configurationFile.Where(f => f.Type == ConfigurationFileTypes.InfrastructureModels).Select(f => f.Parse<InfrastructureModels>()).ToList();

				if (apiProjectConfig is null || apiRoutesConfigs is null)
				{
					return;
				}

				apiProjectConfig.AddAssemblyCommentToFiles = true;

				var factoryResult = ApiFactory.GenerateSourceCode(
					apiProjectConfig,
					apiRoutesConfigs,
					applicationProjectConfig,
					applicationUseCasesConfigs,
					domainProjectConfig,
					domainModelsConfigs,
					infrastructureProjectConfig,
					infrastructureModelsConfigs,
					codeSnippeds
				);

				foreach (var item in factoryResult.SourceCode)
				{
					context.AddSource(item.SourceName, item.SourceCode);
				}
			});
		}

		private List<ApiRouteCodeSnippet> GetCodeSnippets()
		{
			return [
				GetApplicationPreparationCodeSnippet()
			];
		}

		private ApiRouteCodeSnippet GetApplicationPreparationCodeSnippet()
		{
			return new ApiRouteCodeSnippet
			{
				Parameters =
				[
					new ApiRouteCodeSnippetParameter
					{
						Name = "scopedSettings",
						Type = "ExampleScopedSettings",
						Using = "Eshava.Example.Application.Settings",
						RequestPropertyName = "ApplicationId",
						AssignExpression = "scopedSettings".ToIdentifierName().Access("ApplicationId")
					}
				],
				ApplyOnUseCaseTypes =
				[
				//ApplicationUseCaseType.Read,
				//ApplicationUseCaseType.Search
				]
			};
		}
	}
}