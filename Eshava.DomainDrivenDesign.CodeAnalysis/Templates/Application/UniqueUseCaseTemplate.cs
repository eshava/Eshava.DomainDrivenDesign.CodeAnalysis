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
	public static class UniqueUseCaseTemplate
	{
		public static string GetUseCase(UseCaseTemplateRequest request, List<UseCaseCodeSnippet> codeSnippets)
		{
			var className = request.UseCase.ClassName;
			var queryProviderType = request.UseCase.NamespaceClassificationKey.ToQueryProviderType();
			var queryProviderName = request.UseCase.NamespaceClassificationKey.ToQueryProviderName();
			var alternativeClass = request.AlternativeClasses.FirstOrDefault(ac => ac.Type == ApplicationUseCaseType.Unique);

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
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.EXTENSIONS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.Linq.INTERFACES);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.Linq.MODELS);
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
			unitInformation.AddConstructorParameter(queryProviderName, queryProviderType);

			CheckAndAddProviderReferences(unitInformation, request.UseCase, alternativeClass, codeSnippets);

			unitInformation.AddLogger(className);

			unitInformation.AddMethod(CreateCheckUniqueMethod(request.UseCase, codeSnippets));

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

		private static (string Name, MemberDeclarationSyntax) CreateCheckUniqueMethod(ApplicationUseCase useCase, List<UseCaseCodeSnippet> codeSnippets)
		{
			var tryBlockStatements = new List<StatementSyntax>();
			var requestFilter = "request".Access("Filter");
			var returnDataType = useCase.ResponseType;

			var providerResult = $"{useCase.ClassificationKey.ToVariableName()}Result";
			var providerCountResult = $"{useCase.ClassificationKey.ToVariableName()}CountResult";
			var provider = useCase.ClassificationKey.ToQueryProviderName().ToFieldName();
			var filterRequest = "filterRequest";

			CodeSnippetHelpers.AddStatements(tryBlockStatements, returnDataType, codeSnippets);

			var dto = useCase.Dtos.First();
			ExpressionSyntax filterExpression = null;
			foreach (var property in dto.Properties)
			{
				if (property.Name == "Id"
					|| (property.Type.EndsWith("?") && !(property.SearchOperations?.Contains("IsNull") ?? false)))
				{
					continue;
				}

				var propertyExpression = "p".Access(property.Name);
				var requestExpression = "request".Access(property.Name);
				var notEquals = property.SearchOperations?.Contains("NotEqual") ?? false;

				var condition = notEquals
					? propertyExpression.NotEquals(requestExpression)
					: propertyExpression.ToEquals(requestExpression);

				if (filterExpression is null)
				{
					filterExpression = condition;
				}
				else
				{
					filterExpression = filterExpression.And(condition);
				}
			}

			tryBlockStatements.Add(
				filterRequest.ToVariableStatement(
					"FilterRequestDto"
					.AsGeneric(dto.Name)
					.ToInstanceWithInitializer(
						"Sort"
							.ToIdentifierName()
							.Assign(
								"List".AsGeneric("OrderByCondition").ToInstance()
							),
						"Where"
							.ToIdentifierName()
							.Assign(
								"List".AsGeneric("Expression".AsGeneric("Func".AsGeneric(dto.Name, "bool")))
								.ToInstanceWithInitializer(
									"p".ToParameterExpression(filterExpression)
								)
							)
					)
				)
			);

			foreach (var property in dto.Properties)
			{
				if (property.Name != "Id"
					&& (!property.Type.EndsWith("?") || (property.SearchOperations?.Contains("IsNull") ?? false)))
				{
					continue;
				}

				var propertyExpression = "p".Access(property.Name);
				var requestExpression = "request".Access(property.Name);
				var notEquals = property.SearchOperations?.Contains("NotEqual") ?? false;

				var propertyCondition = property.Name == "Id" || notEquals
					? propertyExpression.NotEquals(requestExpression)
					: propertyExpression.ToEquals(requestExpression);

				tryBlockStatements.Add(
					"request"
						.Access(property.Name)
						.Access("HasValue")
						.If(
							filterRequest
							.ToIdentifierName()
							.Access("Where")
							.Access("Add")
							.Call("p".ToParameterExpression(propertyCondition).ToArgument())
							.ToExpressionStatement()
						)
				);
			}

			var countBlockStatments = new List<StatementSyntax>();
			StatementHelpers.AddAsyncMethodCallAndFaultyCheck(countBlockStatments, provider, $"{useCase.UseCaseName}CountAsync", providerCountResult, returnDataType, filterRequest.ToIdentifierName());

			countBlockStatments.Add(returnDataType
				.ToIdentifierName()
				.ToInstanceWithInitializer(
					"Unique"
					.ToIdentifierName()
					.Assign(
						providerCountResult
						.Access("Data")
						.ToEquals("0".ToLiteralInt())
					)
				)
				.Access(CommonNames.Extensions.TORESPONSEDATA)
				.Call()
				.Return()
			);

			tryBlockStatements.Add(
				"request"
				.Access("AddMatches")
				.Not()
				.If(countBlockStatments.ToArray())
			);

			StatementHelpers.AddAsyncMethodCallAndFaultyCheck(tryBlockStatements, provider, $"{useCase.UseCaseName}Async", providerResult, returnDataType, "filterRequest".ToIdentifierName());
			tryBlockStatements.Add(returnDataType
				.ToIdentifierName()
				.ToInstanceWithInitializer(
					"Unique"
						.ToIdentifierName()
						.Assign(
							providerResult
							.Access("Data")
							.Access("Any")
							.Call()
							.Not()
						),
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
	}
}