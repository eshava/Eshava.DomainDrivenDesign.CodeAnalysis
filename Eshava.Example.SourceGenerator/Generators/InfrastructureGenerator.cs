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

				if (infrastructureProjectConfig is null || infrastructureModelsConfigs.Count == 0)
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
				GetUserIdAndStatusRepositoryCodeSnippet(),
				GetUserIdAndStatusQueryRepositoryCodeSnippet(),
				GetUserIdInfrastructureProviderServiceCodeSnippet()
			];
		}

		private InfrastructureCodeSnippet GetUserIdAndStatusRepositoryCodeSnippet()
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
					},
					new InfrastructureModelPropertyCodeSnippet
					{
						IsFilter = true,
						PropertyName = "Status",
						Expression = null,
						Exceptions = [
							new InfrastructureExceptionCodeSnippet
							{
								DataModelName = "CustomerData",
								MethodName = "ReadByNameAsync",
								ClassName = "CustomerDDDRepository",
								UseInstead = true,
								Operation = DomainDrivenDesign.CodeAnalysis.Enums.OperationType.NotEqual,
								Expression = "Status".Access("Inactive")
							},
							new InfrastructureExceptionCodeSnippet
							{
								DataModelName = "LocationData",
								MethodName = "ReadByNameAsync",
								ClassName = "CustomerDDDRepository",
								UseInstead = true,
								Operation = DomainDrivenDesign.CodeAnalysis.Enums.OperationType.NotEqual,
								Expression = "Status".Access("Inactive")
							}
						]
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

		private InfrastructureCodeSnippet GetUserIdAndStatusQueryRepositoryCodeSnippet()
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
						Expression = "_scopedSettings".ToIdentifierName().Access("UserId"),
						Exceptions = [
							new InfrastructureExceptionCodeSnippet
							{
								DataModelName = "CustomerData",
								MethodName = "SearchOnlyInfrastructureAsync",
								ClassName = "OfficeQueryRepository",
								SkipUsage = true
							},
							new InfrastructureExceptionCodeSnippet
							{
								DataModelName = "OfficeData",
								MethodName = "SearchOnlyInfrastructureAsync",
								ClassName = "OfficeQueryRepository",
								UseInstead = true,
								Operation = DomainDrivenDesign.CodeAnalysis.Enums.OperationType.In,
								Expression = "List".AsGeneric("int").ToInstanceWithInitializer("Int32".ToIdentifierName().Access("MaxValue"), "0".ToLiteralInt())
							},
							new InfrastructureExceptionCodeSnippet
							{
								DataModelName = "OfficeData",
								MethodName = "ReadBillingOfficeOnlyInfrastructureAsync",
								ClassName = "OfficeQueryRepository",
								Operation = DomainDrivenDesign.CodeAnalysis.Enums.OperationType.NotEqual,
							}
						]
					},
					new InfrastructureModelPropertyCodeSnippet
					{
						IsFilter = true,
						PropertyName = "Status",
						Expression = null,
						Exceptions = [
							new InfrastructureExceptionCodeSnippet
							{
								DataModelName = "OfficeData",
								MethodName = "ReadBillingOfficeOnlyInfrastructureAsync",
								ClassName = "OfficeQueryRepository",
								SkipUsage = true
							},
							new InfrastructureExceptionCodeSnippet
							{
								DataModelName = "OfficeData",
								MethodName = "SearchOnlyInfrastructureAsync",
								ClassName = "OfficeQueryRepository",
								UseInstead = true,
								Operation = DomainDrivenDesign.CodeAnalysis.Enums.OperationType.In,
								Expression = "List".AsGeneric("Status").ToInstanceWithInitializer("Status".Access("Active"), "Status".Access("Inactive"))
							},
							new InfrastructureExceptionCodeSnippet
							{
								DataModelName = "CustomerData",
								MethodName = "SearchOnlyInfrastructureAsync",
								ClassName = "OfficeQueryRepository",
								UseInstead = true,
								Operation = DomainDrivenDesign.CodeAnalysis.Enums.OperationType.NotEqual,
								Expression = "Status".Access("Inactive")
							},
							new InfrastructureExceptionCodeSnippet
							{
								DataModelName = "OfficeData",
								MethodName = "ReadCustomerIdAsync",
								ClassName = "OfficeQueryRepository",
								UseInstead = true,
								Operation = DomainDrivenDesign.CodeAnalysis.Enums.OperationType.NotEqual,
								Expression = "Status".Access("Inactive")
							},
							new InfrastructureExceptionCodeSnippet
							{
								DataModelName = "OfficeData",
								MethodName = "ReadCustomerIdAsync",
								ClassName = "LocationQueryRepository",
								UseInstead = true,
								Operation = DomainDrivenDesign.CodeAnalysis.Enums.OperationType.NotEqual,
								Expression = "Status".Access("Inactive")
							},
							new InfrastructureExceptionCodeSnippet
							{
								DataModelName = "LocationData",
								MethodName = "ReadCustomerIdAsync",
								ClassName = "LocationQueryRepository",
								UseInstead = true,
								Operation = DomainDrivenDesign.CodeAnalysis.Enums.OperationType.NotEqual,
								Expression = "Status".Access("Inactive")
							}
						]
					}
				]
			};
		}
	}
}