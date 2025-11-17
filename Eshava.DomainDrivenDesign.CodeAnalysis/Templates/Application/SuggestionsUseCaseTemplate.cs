using System.Collections.Generic;
using System.Linq;
using Eshava.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Constants;
using Eshava.DomainDrivenDesign.CodeAnalysis.Enums;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Templates.Application
{
	public static class SuggestionsUseCaseTemplate
	{
		public static string GetUseCase(UseCaseTemplateRequest request, List<UseCaseCodeSnippet> codeSnippets)
		{
			var className = request.UseCase.ClassName;
			var queryProviderType = request.UseCase.NamespaceClassificationKey.ToQueryProviderType();
			var queryProviderName = request.UseCase.NamespaceClassificationKey.ToQueryProviderName();
			var alternativeClass = request.AlternativeClasses.FirstOrDefault(ac => ac.Type == ApplicationUseCaseType.Suggestions);

			var useCaseInterface = $"I{className}".ToType().ToSimpleBaseType();

			var unitInformation = new UnitInformation(className, request.UseCaseNamespace, addAssemblyComment: request.AddAssemblyCommentToFiles);
			unitInformation.AddClassModifier(SyntaxKind.InternalKeyword, SyntaxKind.PartialKeyword);
			unitInformation.AddContructorModifier(SyntaxKind.PublicKeyword);

			if (alternativeClass is null)
			{
				unitInformation.AddBaseType(useCaseInterface);
			}
			else
			{
				unitInformation.AddBaseType(alternativeClass.ClassName.ToSimpleBaseType(), useCaseInterface);
				unitInformation.AddUsing(alternativeClass.Using);
			}

			if ((request.UseCase.Attributes?.Count ?? 0) > 0)
			{
				foreach (var attribute in request.UseCase.Attributes)
				{
					unitInformation.AddUsing(attribute.UsingForType);
				}

				unitInformation.AddAttributes(AttributeTemplate.CreateAttributes(request.UseCase.Attributes));
			}

			unitInformation.AddUsing(CommonNames.Namespaces.SYSTEM);
			unitInformation.AddUsing(CommonNames.Namespaces.LINQ);
			unitInformation.AddUsing(CommonNames.Namespaces.TASKS);
			unitInformation.AddUsing(CommonNames.Namespaces.GENERIC);
			unitInformation.AddUsing(CommonNames.Namespaces.EXPRESSION);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.Linq.INTERFACES);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.Linq.MODELS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.Linq.ENUMS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.EXTENSIONS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.MODELS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Application.DTOS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Application.USECAES);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Domain.CONSTANTS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Domain.EXTENSIONS);
			unitInformation.AddUsing(CommonNames.Namespaces.LOGGING);

			var scopedSettingsTargetType = ParameterTargetTypes.Field;
			if (alternativeClass?.ConstructorParameters?.Any(cp => cp.Type == request.ScopedSettingsClass) ?? false)
			{
				scopedSettingsTargetType |= ParameterTargetTypes.Argument;
			}

			unitInformation.AddScopedSettings(request.ScopedSettingsUsing, request.ScopedSettingsClass, scopedSettingsTargetType);
			unitInformation.AddSortQueryEngine();
			unitInformation.AddConstructorParameter(queryProviderName, queryProviderType);

			CheckAndAddProviderReferences(unitInformation, request.UseCase, alternativeClass, codeSnippets);

			unitInformation.AddLogger(className);

			unitInformation.AddMethod(CreateSuggestionMethod(request.UseCase, codeSnippets));

			return unitInformation.CreateCodeString();
		}

		private static void CheckAndAddProviderReferences(UnitInformation unitInformation, ApplicationUseCase applicationUseCase, ApplicationProjectAlternativeClass alternativeClass, List<UseCaseCodeSnippet> codeSnippets)
		{
			if (alternativeClass?.ConstructorParameters?.Any() ?? false)
			{
				foreach (var additionalContructorParameter in alternativeClass.ConstructorParameters)
				{
					unitInformation.AddUsing(additionalContructorParameter.UsingForType);
					unitInformation.AddConstructorParameter(additionalContructorParameter.Name, additionalContructorParameter.Type, ParameterTargetTypes.Argument);
				}
			}

			foreach (var additionalContructorParameter in applicationUseCase.AdditionalContructorParameter)
			{
				unitInformation.AddUsing(additionalContructorParameter.UsingForType);
				unitInformation.AddConstructorParameter(additionalContructorParameter.Name, additionalContructorParameter.Type);
			}

			CodeSnippetHelpers.AddConstructorParameters(unitInformation, codeSnippets);
		}

		private static (string Name, MemberDeclarationSyntax) CreateSuggestionMethod(ApplicationUseCase useCase, List<UseCaseCodeSnippet> codeSnippets)
		{
			var tryBlockStatements = new List<StatementSyntax>();
			var returnDataType = useCase.ResponseType;

			var providerResult = $"{useCase.ClassificationKey.ToVariableName()}Result";
			var provider = useCase.ClassificationKey.ToQueryProviderName().ToFieldName();
			var filterRequest = "filterRequest";

			CodeSnippetHelpers.AddStatements(tryBlockStatements, returnDataType, codeSnippets);

			tryBlockStatements.Add(
				"searchTerm".ToVariableStatement("request".Access("SearchTerm"))
			);

			tryBlockStatements.Add(
				"searchTerm"
				.ToIdentifierName()
				.Access("IsNullOrEmpty")
				.Call()
				.If(
					"ResponseData".AsGeneric(returnDataType).CreateFaultyResponse(
						EshavaMessageConstant.InvalidDataError.Map(),
						("SearchTerm", "Required", null)
					)
					.Return()
				)
			);

			tryBlockStatements.Add(
				"sortOrder"
				.ToVariableStatement(
					"request"
					.Access("SortOrder")
					.ToEquals("SortOrder".Access("Descending"))
					.ShortIf("SortOrder".Access("Descending"), "SortOrder".Access("Ascending"))
				)
			);

			tryBlockStatements.Add(
				"searchOperation".ToVariableStatement("CompareOperator".Access("Contains"))
			);

			var allowedOperations = new List<StatementSyntax>
			{
				"searchOperation".ToIdentifierName().Assign("request".Access("SearchOperation")).ToExpressionStatement()
			};

			var allowedOperationsSection = allowedOperations.ToSwitchSection(
				("CompareOperator".Access("StartsWith"), null),
				("CompareOperator".Access("EndsWith"), null),
				("CompareOperator".Access("ContainsNot"), null)
			);

			tryBlockStatements.Add(
				"request".Access("SearchOperation").ToSwitchStatement(allowedOperationsSection)
			);

			var dto = useCase.Dtos.First();
			tryBlockStatements.Add(
				"filterConditions"
				.ToVariableStatement(
					"List".AsGeneric("Expression".AsGeneric("Func".AsGeneric(dto.Name, "bool")))
					.ToInstance()
				)
			);

			tryBlockStatements.Add(
				"sortingConditions"
				.ToVariableStatement(
					"List".AsGeneric("OrderByCondition")
					.ToInstance()
				)
			);

			var filterConditions = "filterConditions".ToIdentifierName();
			var sortingConditions = "sortingConditions".ToIdentifierName();
			var searchProperty = dto.Properties.FirstOrDefault(p => p.IsSearchable == true);
			if (searchProperty is not null)
			{
				var conditionContains = new List<StatementSyntax>{
					GetAddSearchConditionStatement(filterConditions, searchProperty.Name, "Contains")
				};
				var conditionContainsNot = new List<StatementSyntax>{
					GetAddSearchConditionStatement(filterConditions, searchProperty.Name, "Contains", true)
				};
				var conditionStartsWith = new List<StatementSyntax>{
					GetAddSearchConditionStatement(filterConditions, searchProperty.Name, "StartsWith")
				};
				var conditionEndsWith = new List<StatementSyntax>{
					GetAddSearchConditionStatement(filterConditions, searchProperty.Name, "EndsWith")
				};

				tryBlockStatements.Add(
					"searchOperation"
					.ToIdentifierName()
					.ToSwitchStatement(
						conditionContains.ToSwitchSection(("CompareOperator".Access("Contains"), null)),
						conditionContainsNot.ToSwitchSection(("CompareOperator".Access("ContainsNot"), null)),
						conditionStartsWith.ToSwitchSection(("CompareOperator".Access("StartsWith"), null)),
						conditionEndsWith.ToSwitchSection(("CompareOperator".Access("EndsWith"), null))
					)
				);

				tryBlockStatements.Add(
					sortingConditions
					.Access("Add")
					.Call(
						ApplicationNames.Engines.SORTING
						.ToFieldName()
						.Access("BuildSortCondition".AsGeneric(dto.Name))
						.Call(
							"sortOrder".ToIdentifierName().ToArgument(),
							"p".ToParameterExpression("p".Access(searchProperty.Name)).ToArgument()
						)
						.Access("Data")
						.ToArgument()
					)
					.ToExpressionStatement()
				);
			}

			tryBlockStatements.Add(
				filterRequest.ToVariableStatement(
					"FilterRequestDto"
					.AsGeneric(dto.Name)
					.ToInstanceWithInitializer(
						"Sort"
							.ToIdentifierName()
							.Assign(sortingConditions),
						"Where"
							.ToIdentifierName()
							.Assign(filterConditions)
					)
				)
			);

			StatementHelpers.AddAsyncMethodCallAndFaultyCheck(tryBlockStatements, provider, $"{useCase.UseCaseName}Async", providerResult, returnDataType, "filterRequest".ToIdentifierName());
			tryBlockStatements.Add(returnDataType
				.ToIdentifierName()
				.ToInstanceWithInitializer(
					useCase.ClassificationKey.ToPlural()
						.ToIdentifierName()
						.Assign(
							providerResult
							.Access("Data")
						)
				)
				.Access(CommonNames.Extensions.TORESPONSEDATA)
				.Call()
				.Return()
			);


			var statements = new List<StatementSyntax>
			{
				tryBlockStatements.TryCatch(
					TemplateMethods.CreateCatchBlock(returnDataType, useCase, null, true)
				)
			};

			var methodDeclarationName = $"{useCase.UseCaseName}Async";
			var methodDeclaration = methodDeclarationName.ToMethod(
				"Task".AsGeneric("ResponseData".AsGeneric(returnDataType)),
				statements,
				SyntaxKind.PublicKeyword,
				SyntaxKind.AsyncKeyword
			);

			methodDeclaration = methodDeclaration
				.WithParameter(
					"request"
					.ToVariableName()
					.ToParameter()
					.WithType(useCase.RequestType.ToType())
				);

			return (methodDeclarationName, methodDeclaration);
		}

		private static StatementSyntax GetAddSearchConditionStatement(IdentifierNameSyntax filterConditions, string propertyName, string method, bool negate = false)
		{
			ExpressionSyntax conditionExpression = "p".Access(propertyName)
				.Access(method)
				.Call("searchTerm".ToIdentifierName().ToArgument());

			if (negate)
			{
				conditionExpression = conditionExpression.Not();
			}

			return filterConditions
				.Access("Add")
				.Call("p".ToParameterExpression(conditionExpression).ToArgument())
				.ToExpressionStatement();
		}
	}
}