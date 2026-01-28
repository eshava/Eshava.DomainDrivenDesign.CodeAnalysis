using System.Linq;
using Eshava.Example.SourceGenerator.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Factories;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Domain;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using Eshava.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;

namespace Eshava.Example.SourceGenerator.Generators
{
	/// <summary>
	/// https://roslynquoter.azurewebsites.net/
	/// </summary>
	[Generator(LanguageNames.CSharp)]
	public class InfrastructureGenerator : IIncrementalGenerator
	{
		public void Initialize(IncrementalGeneratorInitializationContext initContext)
		{
			var configurationFiles = initContext.ReadConfigurationFiles(false, true, true, true, ConfigurationFileTypes.InfrastructureProject);

			initContext.RegisterSourceOutput(configurationFiles, (context, configurationFile) =>
			{
				if (!configurationFile.Any(f => f.Type == ConfigurationFileTypes.Activator))
				{
					return;
				}

				var applicationProjectConfig = configurationFile.FirstOrDefault(f => f.Type == ConfigurationFileTypes.ApplicationProject)?.Parse<ApplicationProject>();
				var applicationUseCasesConfigs = configurationFile.Where(f => f.Type == ConfigurationFileTypes.ApplicationUseCases).Select(f => f.Parse<ApplicationUseCases>()).ToList();

				var domainProjectConfig = configurationFile.FirstOrDefault(f => f.Type == ConfigurationFileTypes.DomainProject)?.Parse<DomainProject>();
				var domainModelsConfigs = configurationFile.Where(f => f.Type == ConfigurationFileTypes.DomainModels).Select(f => f.Parse<DomainModels>()).ToList();

				var infrastructureProjectConfig = configurationFile.FirstOrDefault(f => f.Type == ConfigurationFileTypes.InfrastructureProject)?.Parse<InfrastructureProject>();
				var infrastructureModelsConfigs = configurationFile.Where(f => f.Type == ConfigurationFileTypes.InfrastructureModels).Select(f => f.Parse<InfrastructureModels>()).ToList();

				if (infrastructureProjectConfig is null || infrastructureModelsConfigs is null)
				{
					return;
				}

				infrastructureProjectConfig.AddAssemblyCommentToFiles = true;

				var factoryResult = InfrastructureFactory.GenerateSourceCode(
					applicationProjectConfig,
					applicationUseCasesConfigs,
					domainProjectConfig,
					domainModelsConfigs,
					infrastructureProjectConfig,
					infrastructureModelsConfigs,
					GetCodeSnippets()
				);

				foreach (var item in factoryResult.SourceCode)
				{
					context.AddSource(item.SourceName.HashNamespace(), item.SourceCode);
				}
			});
		}

		private List<InfrastructureCodeSnippet> GetCodeSnippets()
		{
			return [
				GetUserIdRepositoryCodeSnippet(),
				GetUserIdQueryRepositoryCodeSnippet(),
				GetUserIdInfrastructureProviderServiceCodeSnippet()
			];
		}

		private InfrastructureCodeSnippet GetUserIdRepositoryCodeSnippet()
		{
			return new InfrastructureCodeSnippet
			{
				ApplyOnRepository = true,
				PropertyStatements =
				[
					new InfrastructureModelPropertyCodeSnippet
					{
						IsMapping = true,
						IsFilter = true,
						PropertyName = "UserId",
						Expression = "ScopedSettings".ToIdentifierName().Access("UserId")
					}
				]
			};
		}

		private InfrastructureCodeSnippet GetUserIdInfrastructureProviderServiceCodeSnippet()
		{
			return new InfrastructureCodeSnippet
			{
				ApplyOnInstrastructureProviderService = true,
				ConstructorParameters = [
					new InfrastructureCodeSnippetParameter
					{
						Name = "scopedSettings",
						Type = "ExampleScopedSettings",
						Using = "Eshava.Example.Application.Settings"
					}
				],
				PropertyStatements =
				[
					new InfrastructureModelPropertyCodeSnippet
					{
						IsMapping = true,
						PropertyName = "UserId",
						Expression = "_scopedSettings".ToIdentifierName().Access("UserId")
					}
				]
			};
		}

		private InfrastructureCodeSnippet GetUserIdQueryRepositoryCodeSnippet()
		{
			return new InfrastructureCodeSnippet
			{
				ApplyOnQueryRepository = true,
				PropertyStatements =
				[
					new InfrastructureModelPropertyCodeSnippet
					{
						IsFilter = true,
						PropertyName = "UserId",
						Expression = "_scopedSettings".ToIdentifierName().Access("UserId")
					}
				]
			};
		}
	}
}