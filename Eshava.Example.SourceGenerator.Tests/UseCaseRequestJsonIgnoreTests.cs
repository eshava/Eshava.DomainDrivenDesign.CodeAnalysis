using System.Collections.Generic;
using System.Linq;
using Eshava.DomainDrivenDesign.CodeAnalysis.Constants;
using Eshava.DomainDrivenDesign.CodeAnalysis.Factories;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Eshava.Example.SourceGenerator.Tests
{
	/// <summary>
	/// The route and identifier properties of a generated request class are hidden from the
	/// serializer. Which JsonIgnore attribute says so follows the serializer the project uses -
	/// emitting both would oblige every consuming project to reference both packages, whichever one
	/// it actually serializes with.
	/// </summary>
	[TestClass]
	public class UseCaseRequestJsonIgnoreTests : AbstractTests
	{
		private static readonly string _systemTextJsonAttribute = $"[{CommonNames.Namespaces.JSON}.{CommonNames.Attributes.JSONIGNORE}]";
		private static readonly string _newtonsoftAttribute = $"[{CommonNames.Namespaces.NEWTONSOFT}.{CommonNames.Attributes.JSONIGNORE}]";

		[TestMethod]
		public void SystemTextJsonIsWhatAProjectGetsWithoutSayingAnythingTest()
		{
			var requests = GenerateRequests(useNewtonsoftJson: false);

			requests.Should().NotBeEmpty("the example configuration has to produce request classes");
			requests.Should().Contain(request => request.Contains(_systemTextJsonAttribute));
			requests.Should().NotContain(request => request.Contains(_newtonsoftAttribute), "no project should be obliged to reference Newtonsoft.Json");
		}

		[TestMethod]
		public void NewtonsoftIsUsedWhereTheProjectSaysSoTest()
		{
			var requests = GenerateRequests(useNewtonsoftJson: true);

			requests.Should().NotBeEmpty();
			requests.Should().Contain(request => request.Contains(_newtonsoftAttribute));
			requests.Should().NotContain(request => request.Contains(_systemTextJsonAttribute), "the two are alternatives, not a pair");
		}

		/// <summary>
		/// The generated request classes of the example configuration, with the serializer switch set
		/// one way or the other. Everything else about the configuration is left as it is.
		/// </summary>
		private static List<string> GenerateRequests(bool useNewtonsoftJson)
		{
			var data = Init();
			data.ApplicationProject.UseNewtonsoftJson = useNewtonsoftJson;

			var result = ApplicationFactory.GenerateSourceCode(
				data.ApplicationProject,
				data.ApplicationUseCases,
				data.DomainProject,
				data.DomainModels,
				data.InfrastructureProject,
				data.InfrastructureModels,
				[]
			);

			return result.SourceCode
				.Where(source => source.SourceName.EndsWith("Request.g.cs"))
				.Select(source => source.SourceCode)
				.ToList();
		}
	}
}