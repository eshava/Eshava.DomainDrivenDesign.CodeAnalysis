using System.Linq;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Eshava.Example.SourceGenerator.Tests
{
	[TestClass]
	public class UseCasesMapAnalysisTests : AbstractTests
	{
		[TestMethod]
		[Ignore]
		public void TestAnalyse()
		{
			var data = Init();

			var domainModelResult = DependencyAnalysis.Analyse(
				data.DomainModels.Merge(),
				data.InfrastructureModels.Merge()
			);


			var dtoResult = DtoDependencyAnalysis.Analyse(
				data.ApplicationUseCases.Merge(),
				domainModelResult,
				data.InfrastructureModels.Merge()
			);

			var result = UseCaseAnalysis.Analyse(data.ApplicationProject, domainModelResult, dtoResult, data.ApplicationUseCases.Merge());


			var useCaseMaps = result.GetUseCaseMaps().ToList();
			var methodMaps = result.GetUseCaseQueryProviderMethodMaps().ToList();

			if (true)
			{

			}
		}
	}
}
