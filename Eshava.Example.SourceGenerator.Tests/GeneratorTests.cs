using Eshava.DomainDrivenDesign.CodeAnalysis.Factories;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Eshava.Example.SourceGenerator.Tests
{
	[TestClass]
	public class GeneratorTests : AbstractTests
	{
		[TestMethod]
		[Ignore]
		public void TestApiFactory()
		{
			try
			{
				var data = Init();

				var result = ApiFactory.GenerateSourceCode(
					data.ApiProject,
					data.ApiRoutes,
					data.ApplicationProject,
					data.ApplicationUseCases,
					data.DomainProject,
					data.DomainModels,
					data.InfrastructureProject,
					data.InfrastructureModels,
					[]
				);

				if (true)
				{

				}
			}
			catch (System.Exception ex)
			{


			}
		}

		[TestMethod]
		//[Ignore]
		public void TestApplicationFactory()
		{
			try
			{
				var data = Init();

				var result = ApplicationFactory.GenerateSourceCode(
					data.ApplicationProject,
					data.ApplicationUseCases,
					data.DomainProject,
					data.DomainModels,
					data.InfrastructureProject,
					data.InfrastructureModels,
					[]
				);

				if (true)
				{

				}
			}
			catch (System.Exception ex)
			{


			}
		}

		[TestMethod]
		[Ignore]
		public void TestDomainFactory()
		{
			try
			{
				var data = Init();

				var result = DomainFactory.GenerateSourceCode(
					data.DomainProject,
					data.DomainModels
				);

				if (true)
				{

				}
			}
			catch (System.Exception ex)
			{

			}
		}

		[TestMethod]
		[Ignore]
		public void TestInfrastructureFactory()
		{
			try
			{
				var data = Init();

				var result = InfrastructureFactory.GenerateSourceCode(
					data.ApplicationProject,
					data.ApplicationUseCases,
					data.DomainProject,
					data.DomainModels,
					data.InfrastructureProject,
					data.InfrastructureModels
				);

				if (true)
				{

				}
			}
			catch (System.Exception ex)
			{

			}
		}
	}
}