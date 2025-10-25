using System.Linq;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Eshava.Example.SourceGenerator.Tests
{
	[TestClass]
	public class DtoDependencyAnalysisTests : AbstractTests
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


			var result = DtoDependencyAnalysis.Analyse(
				data.ApplicationUseCases.Merge(),
				domainModelResult,
				data.InfrastructureModels.Merge()
			);

			var dtos = result.GetReferenceDtoMaps().ToList();

			if (true)
			{

			}
		}
	}
}
