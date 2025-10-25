using System.Linq;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Eshava.Example.SourceGenerator.Tests
{
	[TestClass]
	public class DependencyAnalysisTests : AbstractTests
	{
		[TestMethod]
		[Ignore]
		public void TestAnalyse()
		{
			var data = Init();

			var result = DependencyAnalysis.Analyse(
				data.DomainModels.Merge(),
				data.InfrastructureModels.Merge()
			);

			var domainModels = result.GetReferenceDomainModelMaps().ToList();

			if (true)
			{

			}
		}
	}
}