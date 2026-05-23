using System.Collections.Generic;
using Eshava.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure;

namespace Eshava.Example.SourceGenerator.Generators
{
	public static class CodeSnippets
	{
		public static List<InfrastructureCodeSnippet> GetInfrastructureCodeSnippets()
		{
			return [
				GetUserIdAndStatusRepositoryCodeSnippet(),
				GetUserIdAndStatusQueryRepositoryCodeSnippet(),
				GetUserIdInfrastructureProviderServiceCodeSnippet()
			];
		}

		private static InfrastructureCodeSnippet GetUserIdAndStatusRepositoryCodeSnippet()
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
						Expression = "ScopedSettings".ToIdentifierName().Access("UserId"),
						Exceptions = [
							new InfrastructureExceptionCodeSnippet
							{
								DataModelName = "CustomerData",
								MethodName = "ReadAsync",
								ClassName = "CustomerDDDRepository",
								SkipUsage = true
							},
							new InfrastructureExceptionCodeSnippet
							{
								DataModelName = "CustomerData",
								MethodName = "ReadByNameAsync",
								ClassName = "CustomerDDDRepository",
								UseInstead = true,
								Operation = DomainDrivenDesign.CodeAnalysis.Enums.OperationType.NotEqual,
								Expression = "0".ToLiteralInt()
							},
							new InfrastructureExceptionCodeSnippet
							{
								DataModelName = "OfficeData",
								MethodName = "ReadAsync",
								ClassName = "CustomerDDDRepository",
								UseInstead = true,
								Operation = DomainDrivenDesign.CodeAnalysis.Enums.OperationType.NotEqual,
								Expression = "0".ToLiteralInt()
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
					},
					new InfrastructureModelPropertyCodeSnippet
					{
						IsMapping = false,
						IsFilter = true,
						ForceAsWhereCondition = true,
						PropertyName = "AccessLevel",
						Expression = "777".ToLiteralInt(),
						Exceptions = [
							new InfrastructureExceptionCodeSnippet
							{
								DataModelName = "CustomerCategoryData",
								MethodName = "ReadForCustomerCategoryIdAsync",
								ClassName = "CustomerDDDRepository",
								UseInstead = true,
								Operation = DomainDrivenDesign.CodeAnalysis.Enums.OperationType.NotEqual,
								Expression = "555".ToLiteralInt(),
							},
							new InfrastructureExceptionCodeSnippet
							{
								DataModelName = "CustomerCategoryData",
								MethodName = "ReadByNameAsync",
								ClassName = "CustomerDDDRepository",
								SkipUsage = true
							},
							new InfrastructureExceptionCodeSnippet
							{
								DataModelName = "CustomerCategoryData",
								MethodName = "ReadAsync",
								ClassName = "OrderDDDRepository",
								SkipUsage = true
							}
						]
					}
				]
			};
		}

		private static InfrastructureCodeSnippet GetUserIdInfrastructureProviderServiceCodeSnippet()
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

		private static InfrastructureCodeSnippet GetUserIdAndStatusQueryRepositoryCodeSnippet()
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
						IsMapping = false,
						IsFilter = true,
						ForceAsWhereCondition = true,
						PropertyName = "AccessLevel",
						Expression = "777".ToLiteralInt(),
						Exceptions = [
							new InfrastructureExceptionCodeSnippet
							{
								DataModelName = "CustomerCategoryData",
								MethodName = "ExistsAsync",
								ClassName = "OrderPositionQueryRepository",
								SkipUsage = true
							},
							new InfrastructureExceptionCodeSnippet
							{
								DataModelName = "CustomerCategoryData",
								MethodName = "ReadAsync",
								ClassName = "OrderPositionQueryRepository",
								SkipUsage = true
							},
							new InfrastructureExceptionCodeSnippet
							{
								DataModelName = "CustomerCategoryData",
								MethodName = "SearchAsync",
								ClassName = "OrderPositionQueryRepository",
								SkipUsage = true
							},
							new InfrastructureExceptionCodeSnippet
							{
								DataModelName = "CustomerCategoryData",
								MethodName = "IsUniqueNameAsync",
								ClassName = "OfficeQueryRepository",
								UseInstead = true,
								Operation = DomainDrivenDesign.CodeAnalysis.Enums.OperationType.NotEqual,
								Expression = "555".ToLiteralInt(),
							},
							new InfrastructureExceptionCodeSnippet
							{
								DataModelName = "CustomerCategoryData",
								MethodName = "IsUniqueNameAsync",
								ClassName = "CustomerQueryRepository",
								UseInstead = true,
								Operation = DomainDrivenDesign.CodeAnalysis.Enums.OperationType.NotEqual,
								Expression = "555".ToLiteralInt(),
							},
							new InfrastructureExceptionCodeSnippet
							{
								DataModelName = "CustomerCategoryData",
								MethodName = "IsUsedMainProductIdAsync",
								ClassName = "CustomerQueryRepository",
								SkipUsage = true
							},
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
