using System.Linq;
using Eshava.Example.SourceGenerator.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Factories;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Domain;
using Microsoft.CodeAnalysis;

namespace Eshava.Example.SourceGenerator
{
	/// <summary>
	/// https://roslynquoter.azurewebsites.net/
	/// </summary>
	[Generator(LanguageNames.CSharp)]
	public class DomainGenerator : IIncrementalGenerator
	{
		public void Initialize(IncrementalGeneratorInitializationContext initContext)
		{
			var configurationFiles = initContext.ReadConfigurationFiles(false, false, true, false, ConfigurationFileTypes.DomainProject);

			initContext.RegisterSourceOutput(configurationFiles, (context, configurationFile) =>
			{
				if (!configurationFile.Any(f => f.Type == ConfigurationFileTypes.Activator))
				{
					return;
				}

				var domainProjectConfig = configurationFile.FirstOrDefault(f => f.Type == ConfigurationFileTypes.DomainProject)?.Parse<DomainProject>();
				var domainModelsConfigs = configurationFile.Where(f => f.Type == ConfigurationFileTypes.DomainModels).Select(f => f.Parse<DomainModels>()).ToList();

				if (domainProjectConfig is null || domainModelsConfigs is null)
				{
					return;
				}

				domainProjectConfig.AddAssemblyCommentToFiles = true;

				var factoryResult = DomainFactory.GenerateSourceCode(
					domainProjectConfig,
					domainModelsConfigs
				);

				foreach (var item in factoryResult.SourceCode)
				{
					context.AddSource(item.SourceName, item.SourceCode);
				}
			});
		}
	}
}