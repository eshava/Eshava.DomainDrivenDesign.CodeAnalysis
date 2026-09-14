using System.Linq;
using System.Text.RegularExpressions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Factories;
using Eshava.Example.SourceGenerator.Generators;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Eshava.Example.SourceGenerator.Tests
{
	/// <summary>
	/// A table carrying code snippet read conditions is joined with an inner join only where the
	/// queried side holds a foreign key that cannot be null - that is the one case in which a match is
	/// guaranteed. A nullable key, or a key sitting on the joined side, keeps a left join, otherwise
	/// every queried row without a match disappears from the result.
	/// </summary>
	[TestClass]
	public class QueryRepositoryJoinTypeTests : AbstractTests
	{
		[TestMethod]
		public void NullableForeignKeyOnTheQueriedSideIsLeftJoinedTest()
		{
			var customerQueryRepository = GenerateCustomerQueryRepository();

			// OfficeData.AlternativeCustomerId is int? and points at CustomerData, which carries the UserId snippet
			var joinType = GetJoinType(customerQueryRepository, "{ALTERNATIVECUSTOMER}.{nameof(CustomerData.Id)} = {OFFICEDATA}.{nameof(Offices.OfficeData.AlternativeCustomerId)}");

			joinType.Should().Be("LEFT JOIN", "an office without an alternative customer must not vanish from the result");
		}

		[TestMethod]
		public void ForeignKeyOnTheJoinedSideIsLeftJoinedTest()
		{
			var customerQueryRepository = GenerateCustomerQueryRepository();

			// OfficeData.CustomerId is the child's reference to the queried customer - a customer may have no office
			var joinType = GetJoinType(customerQueryRepository, "{OFFICEDATA}.{nameof(Offices.OfficeData.CustomerId)} = {CUSTOMERDATA}.{nameof(CustomerData.Id)}");

			joinType.Should().Be("LEFT JOIN", "a customer without an office must still be readable");
		}

		[TestMethod]
		public void RequiredForeignKeyOnTheQueriedSideStaysAnInnerJoinTest()
		{
			var orderQueryRepository = GenerateSource("OrderQueryRepository.g.cs");

			// OrderData.CustomerId is int and points at CustomerData, which carries the UserId snippet
			var joinType = GetJoinType(orderQueryRepository, "{CUSTOMERDATA}.{nameof(Organizations.Customers.CustomerData.Id)} = {ORDERDATA}.{nameof(OrderData.CustomerId)}");

			joinType.Should().Be("JOIN", "a required key with an owner condition on the referenced table guarantees the match");
		}

		[TestMethod]
		public void ReferenceWithoutSnippetConditionsIsLeftJoinedTest()
		{
			var customerQueryRepository = GenerateCustomerQueryRepository();

			// CustomerCategoryData carries no UserId, so no snippet condition is written for it
			var joinType = GetJoinType(customerQueryRepository, "{CUSTOMERCATEGORYDATA}.{nameof(CustomerCategories.CustomerCategoryData.Id)} = {CUSTOMERDATA}.{nameof(CustomerData.CustomerCategoryId)}");

			joinType.Should().Be("LEFT JOIN");
		}

		private static string GenerateCustomerQueryRepository()
		{
			return GenerateSource("CustomerQueryRepository.g.cs");
		}

		/// <summary>
		/// The generated infrastructure source with the given name, produced from the example
		/// configuration and the code snippets the example generator applies.
		/// </summary>
		private static string GenerateSource(string sourceName)
		{
			var data = Init();

			var result = InfrastructureFactory.GenerateSourceCode(
				data.ApplicationProject,
				data.ApplicationUseCases,
				data.DomainProject,
				data.DomainModels,
				data.InfrastructureProject,
				data.InfrastructureModels,
				CodeSnippets.GetInfrastructureCodeSnippets()
			);

			// the dot keeps the interface apart from the class: ICustomerQueryRepository ends with CustomerQueryRepository as well
			var source = result.SourceCode.FirstOrDefault(sc => sc.SourceName.EndsWith("." + sourceName));
			source.SourceCode.Should().NotBeNull($"the example configuration has to produce {sourceName}");

			return source.SourceCode;
		}

		/// <summary>
		/// The join keyword written in front of the given ON condition. The condition has to occur,
		/// and it has to be joined the same way wherever it occurs in the source.
		/// </summary>
		private static string GetJoinType(string source, string onCondition)
		{
			var pattern = @"(LEFT JOIN|JOIN)\s*\r?\n\s*\{TypeAnalyzer\.GetTableName<[\w.]+>\(\)\}\s+\{\w+\}\s*\r?\n\s*ON " + Regex.Escape(onCondition);
			var matches = Regex.Matches(source, pattern);

			matches.Should().NotBeEmpty($"the condition '{onCondition}' has to be part of a join");

			var joinTypes = matches.Select(match => match.Groups[1].Value).Distinct().ToList();
			joinTypes.Should().HaveCount(1, "the same reference has to be joined the same way in every method");

			return joinTypes.Single();
		}
	}
}