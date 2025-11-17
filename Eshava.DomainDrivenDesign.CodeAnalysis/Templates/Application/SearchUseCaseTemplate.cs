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
	public static class SearchUseCaseTemplate
	{
		public static string GetUseCase(UseCaseTemplateRequest request, List<UseCaseCodeSnippet> codeSnippets, bool asCount)
		{
			var className = request.UseCase.ClassName;
			var queryProviderType = request.UseCase.NamespaceClassificationKey.ToQueryProviderType();
			var queryProviderName = request.UseCase.NamespaceClassificationKey.ToQueryProviderName();
			var alternativeClass = request.AlternativeClasses.FirstOrDefault(ac => ac.Type == ApplicationUseCaseType.Search);

			var baseType = alternativeClass is null
				? CommonNames.Application.Abstracts.SEARCHUSECASE.AsGeneric(request.UseCase.RequestType, request.UseCase.MainDto).ToSimpleBaseType()
				: alternativeClass.ClassName.AsGeneric(request.UseCase.RequestType, request.UseCase.MainDto).ToSimpleBaseType();
			var useCaseInterface = $"I{className}".ToType().ToSimpleBaseType();

			var unitInformation = new UnitInformation(className, request.UseCaseNamespace, addAssemblyComment: request.AddAssemblyCommentToFiles);
			unitInformation.AddClassModifier(SyntaxKind.InternalKeyword, SyntaxKind.PartialKeyword);
			unitInformation.AddContructorModifier(SyntaxKind.PublicKeyword);
			unitInformation.AddBaseType(baseType, useCaseInterface);
			unitInformation.AddUsing(alternativeClass?.Using);

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
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.EXTENSIONS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.Linq.INTERFACES);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.MODELS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Application.USECAES);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Domain.CONSTANTS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Domain.EXTENSIONS);
			unitInformation.AddUsing(CommonNames.Namespaces.LOGGING);

			if (asCount)
			{
				unitInformation.AddUsing(request.UseCaseNamespace.Substring(0, request.UseCaseNamespace.Length - 5));
			}

			var scopedSettingsTargetType = ParameterTargetTypes.Field;
			if (alternativeClass?.ConstructorParameters?.Any(cp => cp.Type == request.ScopedSettingsClass) ?? false)
			{
				scopedSettingsTargetType |= ParameterTargetTypes.Argument;
			}

			unitInformation.AddScopedSettings(request.ScopedSettingsUsing, request.ScopedSettingsClass, scopedSettingsTargetType);
			unitInformation.AddWhereAndSortQueryEngine();
			unitInformation.AddConstructorParameter(queryProviderName, queryProviderType);

			CheckAndAddProviderReferences(unitInformation, request.UseCase, alternativeClass, codeSnippets);

			unitInformation.AddLogger(className);

			if (asCount)
			{
				unitInformation.AddMethod(CreateSearchCountMethod(request.UseCase, request.Domain, codeSnippets));
			}
			else
			{
				unitInformation.AddMethod(CreateSearchMethod(request.UseCase, request.Domain, codeSnippets));
			}

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

		private static (string Name, MemberDeclarationSyntax) CreateSearchMethod(ApplicationUseCase useCase, string domain, List<UseCaseCodeSnippet> codeSnippets)
		{
			var tryBlockStatements = new List<StatementSyntax>();
			var requestFilter = "request".Access("Filter");
			var returnDataType = useCase.ResponseType;

			var providerResult = $"{useCase.ClassificationKey.ToVariableName()}Result";
			var providerCountResult = $"{useCase.ClassificationKey.ToVariableName()}CountResult";
			var provider = useCase.ClassificationKey.ToQueryProviderName().ToFieldName();

			CodeSnippetHelpers.AddStatements(tryBlockStatements, returnDataType, codeSnippets);

			AddRequestFilterStatements(tryBlockStatements, useCase.UseCaseName, useCase.ClassificationKey, useCase.MainDto, returnDataType, provider, providerResult);

			tryBlockStatements.Add(
				"dtos"
				.ToVariableStatement(
					providerResult
					.Access("Data")
					.Access("ToList")
					.Call()
				)
			);

			tryBlockStatements.Add(
				"searchResult"
				.ToVariableStatement(
					returnDataType
					.ToIdentifierName()
					.ToInstance()
					.WithInitializer(
						useCase.ClassificationKey.ToPlural().ToIdentifierName().Assign("dtos".ToIdentifierName()),
						"Total".ToIdentifierName().Assign("0".ToLiteralInt())
					)
				)
			);

			var skipStatement = requestFilter.Access("Skip").ToEquals("0".ToLiteralInt());
			var takeStatement = requestFilter.Access("Take").GreaterThan("0".ToLiteralInt());
			var countStatement = "dtos".Access("Count").ToEquals(requestFilter.Access("Take"));

			tryBlockStatements.Add(
				skipStatement
				.And(takeStatement)
				.And(countStatement)
				.If(
					"filterRequest"
						.Access("Skip")
						.Assign("0".ToLiteralInt())
						.ToExpressionStatement(),
					"filterRequest"
						.Access("Take")
						.Assign("0".ToLiteralInt())
						.ToExpressionStatement(),
					providerCountResult
						.ToVariableStatement(
							provider
								.Access($"{useCase.UseCaseName}CountAsync")
								.Call("filterRequest".ToArgument())
								.Await()
						),
					providerCountResult
						.ToFaultyCheck(returnDataType),
					"searchResult"
						.Access("Total")
						.Assign(
							providerCountResult
								.Access("Data")
						)
						.ToExpressionStatement()
				).ElseIf(
					skipStatement,
					"searchResult"
					.Access("Total")
					.Assign(
						"searchResult"
						.Access(useCase.ClassificationKey.ToPlural())
						.Access("Count")
						.Call()
					)
					.ToExpressionStatement()
				)
			);

			tryBlockStatements.Add(
				"adjustDtosResult"
				.ToVariableStatement(
					"AdjustDtosAsync"
					.ToIdentifierName()
					.Call(
						"request".ToArgument(),
						"searchResult".Access(useCase.ClassificationKey.ToPlural()).ToArgument()
					)
					.Await()
				)
			);

			tryBlockStatements.Add("adjustDtosResult".ToFaultyCheck(returnDataType));
			tryBlockStatements.Add(
				"searchResult"
				.Access(useCase.ClassificationKey.ToPlural())
				.Assign("adjustDtosResult".Access("Data"))
				.ToExpressionStatement()
			);

			tryBlockStatements.Add(
				"searchResult"
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

		private static (string, MemberDeclarationSyntax Method) CreateSearchCountMethod(ApplicationUseCase useCase, string domain, List<UseCaseCodeSnippet> codeSnippets)
		{
			var tryBlockStatements = new List<StatementSyntax>();
			var requestFilter = "request".Access("Filter");
			var returnDataType = useCase.ResponseType;

			var providerCountResult = $"{useCase.ClassificationKey.ToVariableName()}CountResult";
			var provider = useCase.ClassificationKey.ToQueryProviderName().ToFieldName();

			CodeSnippetHelpers.AddStatements(tryBlockStatements, returnDataType, codeSnippets);

			AddRequestFilterStatements(tryBlockStatements, useCase.UseCaseName, useCase.ClassificationKey, useCase.MainDto, returnDataType, provider, providerCountResult);

			tryBlockStatements.Add(
				"searchResult"
				.ToVariableStatement(
					returnDataType
					.ToIdentifierName()
					.ToInstanceWithInitializer(
						"Total".ToIdentifierName().Assign(providerCountResult.Access("Data"))
					)
				)
			);

			tryBlockStatements.Add(
				"searchResult"
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

			var methodDeclarationName = useCase.MethodName;
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

		private static void AddRequestFilterStatements(
			List<StatementSyntax> statements,
			string useCaseName,
			string classificationKey,
			string searchDtoName,
			string returnDataType,
			string provider,
			string providerResult
		)
		{
			var requestFilter = "request".Access("Filter");

			statements.Add(
				requestFilter
				.ToEquals(Eshava.CodeAnalysis.SyntaxConstants.Null)
				.If(
					requestFilter
					.Assign(
						$"{classificationKey}{useCaseName}FilterDto"
						.ToIdentifierName()
						.ToInstance()
					)
					.ToExpressionStatement()
				)
			);


			StatementHelpers.AddLocalMethodCallAndFaultyCheck(statements, "GetFilterRequest", "filterRequestResult", returnDataType, requestFilter);

			statements.Add("filterRequest"
				.ToVariableStatement(
					"filterRequestResult".Access("Data")
				)
			);

			StatementHelpers.AddAsyncMethodCallAndFaultyCheck(statements, provider, $"{useCaseName}Async", providerResult, returnDataType, "filterRequest".ToIdentifierName());
		}
	}
}