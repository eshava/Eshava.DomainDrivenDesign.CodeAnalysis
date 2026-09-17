using System.Linq;
using System.Text.RegularExpressions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Factories;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Eshava.Example.SourceGenerator.Tests
{
	/// <summary>
	/// A child a create payload does not carry arrives as null, because the generated dto declares the
	/// property without an initializer. The create use case used to walk it without asking, which made
	/// every caller that left a child out fail with a NullReferenceException. Whether a missing child is
	/// an error is read off the dto property at run time - the generator cannot see it, because a child
	/// is no property of the domain model and the configured dto property carries nothing.
	/// </summary>
	[TestClass]
	public class CreateUseCaseChildNullGuardTests : AbstractTests
	{
		private const string CUSTOMER_CREATE = "CustomerDDDCreateUseCase.g.cs";
		private const string CUSTOMER_CREATE_WITH_SINGLE_CHILD = "CustomerDDDCreateWithSingleOfficeAndLocationUseCase.g.cs";

		[TestMethod]
		public void AChildListIsOnlyCreatedWhenItIsSentTest()
		{
			var useCase = GenerateUseCase(CUSTOMER_CREATE);

			useCase.Should().Contain("if (request.Customer.Offices is null) {");
			useCase.Should().Contain("} else { var createOfficesResult = await CreateOfficeDDDsAsync(createCustomerResult.Data, request.Customer.Offices);");
		}

		[TestMethod]
		public void AMissingChildIsAnErrorOnlyWhereTheDtoDeclaresItRequiredTest()
		{
			var useCase = GenerateUseCase(CUSTOMER_CREATE);

			useCase.Should().Contain("if (System.Attribute.IsDefined(typeof(CustomerDDDCreateCustomerCreateDto).GetProperty(\"Offices\"), typeof(System.ComponentModel.DataAnnotations.RequiredAttribute))) { return ResponseData<CustomerDDDCreateResponse>.CreateFaultyResponse(MessageConstants.INVALIDDATA).AddValidationError(\"Offices\", \"Required\"); }");
		}

		[TestMethod]
		public void AChildListOfAChildIsGuardedAsWellTest()
		{
			// Offices carry Locations, and a payload may leave those out on any of the three levels
			var useCase = GenerateUseCase(CUSTOMER_CREATE);

			useCase.Should().Contain("if (office.Locations is null) { if (System.Attribute.IsDefined(typeof(CustomerDDDCreateOfficeCreateDto).GetProperty(\"Locations\")");
			useCase.Should().Contain("} else { var createLocationsResult = await CreateLocationDDDsAsync(createOfficeResult.Data, office.Locations);");
			useCase.Should().Contain("if (location.Buildings is null) { if (System.Attribute.IsDefined(typeof(CustomerDDDCreateLocationCreateDto).GetProperty(\"Buildings\")");
			useCase.Should().Contain("} else { var createBuildingsResult = await CreateBuildingDDDsAsync(createLocationResult.Data, location.Buildings);");
		}

		[TestMethod]
		public void ASingleChildIsOnlyCreatedWhenItIsSentTest()
		{
			// The same hole without a list: the single child dto is handed to the domain model unchecked
			var useCase = GenerateUseCase(CUSTOMER_CREATE_WITH_SINGLE_CHILD);

			useCase.Should().Contain("if (request.Customer.Office is null) { if (System.Attribute.IsDefined(typeof(CustomerDDDCreateWithSingleOfficeAndLocationCustomerCreateDto).GetProperty(\"Office\")");
			useCase.Should().Contain("} else { var createOfficeResult = await CreateOfficeDDDAsync(createCustomerResult.Data, request.Customer.Office);");
			useCase.Should().Contain("if (office.Location is null) { if (System.Attribute.IsDefined(typeof(CustomerDDDCreateWithSingleOfficeAndLocationOfficeCreateDto).GetProperty(\"Location\")");
			useCase.Should().Contain("} else { var createLocationResult = await CreateLocationDDDAsync(createOfficeResult.Data, office.Location);");
		}

		/// <summary>
		/// The generated application source with the given name, produced from the example configuration
		/// and reduced to single spaces so the assertions do not depend on the line endings of the machine
		/// the tests run on.
		/// </summary>
		private static string GenerateUseCase(string sourceName)
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

			// the dot keeps the interface apart from the class: ICustomerDDDCreateUseCase ends with the same name
			var source = result.SourceCode.FirstOrDefault(sc => sc.SourceName.EndsWith("." + sourceName));
			source.SourceCode.Should().NotBeNull($"the example configuration has to produce {sourceName}");

			return Regex.Replace(source.SourceCode, @"\s+", " ");
		}
	}
}