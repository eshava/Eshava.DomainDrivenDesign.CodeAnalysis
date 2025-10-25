using System.Collections.Generic;
using System.Linq;
using Eshava.CodeAnalysis.Extensions;
using Eshava.Example.SourceGenerator.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Constants;
using Eshava.DomainDrivenDesign.CodeAnalysis.Factories;
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
	public class ApplicationGenerator : IIncrementalGenerator
	{
		public void Initialize(IncrementalGeneratorInitializationContext initContext)
		{
			var configurationFiles = initContext.ReadConfigurationFiles(false, true, true, true, ConfigurationFileTypes.ApplicationProject);
			var codeSnippeds = GetCodeSnippets();

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

				var infrastructureProjectConfig = configurationFile.FirstOrDefault(f => f.Type == ConfigurationFileTypes.InfrastructureProject)?.Parse<InfrastructureProject>() ?? new InfrastructureProject();
				var infrastructureModelsConfigs = configurationFile.Where(f => f.Type == ConfigurationFileTypes.InfrastructureModels).Select(f => f.Parse<InfrastructureModels>()).ToList();

				if (applicationProjectConfig is null || applicationUseCasesConfigs is null)
				{
					return;
				}

				applicationProjectConfig.AddAssemblyCommentToFiles = true;

				var factoryResult = ApplicationFactory.GenerateSourceCode(
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

		private List<UseCaseCodeSnippet> GetCodeSnippets()
		{
			return [
				GetApplicationPreparationCodeSnippet()
			];
		}
		
		private UseCaseCodeSnippet GetApplicationPreparationCodeSnippet()
		{
			return new UseCaseCodeSnippet
			{
				RequestProperties =
				[
					new UseCaseCodeSnippetParameter
					{
						Name = "ApplicationId",
						Type = "int",
						Using = null,
						Attributes =
						[
							new Eshava.DomainDrivenDesign.CodeAnalysis.Models.AttributeDefinition
							{
								Name = $"{CommonNames.Namespaces.NEWTONSOFT}.JsonIgnore"
							},
							new Eshava.DomainDrivenDesign.CodeAnalysis.Models.AttributeDefinition
							{
								Name = $"{CommonNames.Namespaces.JSON}.JsonIgnore"
							}
						]
					}
				],
				ConstructorParameters =
				[
					new UseCaseCodeSnippetParameter
					{
						Name = "applicationPreparationService",
						Type = "IApplicationPreparationService",
						Using = "Eshava.Example.Application.Common"
					}
				],
				ApplyOnUseCaseTypes =
				[
				//ApplicationUseCaseType.Read,
				//ApplicationUseCaseType.Search
				],
				Statements =
				[
					new UseCaseCodeSnippetStatement
					{
						Statement = "applicationCheckResult"
							.ToVariableStatement(
								"_applicationPreparationService"
								.Access("PrepareAsync")
								.Call("request".Access("ApplicationId").ToArgument())
								.Await()
							)
					},
					new UseCaseCodeSnippetStatement
					{
						CreateFaultyCheck = true,
						VariableToCheck = "applicationCheckResult".ToIdentifierName()
					},
					new UseCaseCodeSnippetStatement
					{
						CreateExpressionCheck = true,
						ReturnResponseInstance = true,
						Expression = "applicationCheckResult".Access("Data").Not()
					}
				]
			};
		}
	}
}