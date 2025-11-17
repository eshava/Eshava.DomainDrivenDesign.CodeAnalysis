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
	public static class ReadUseCaseTemplate
	{
		public static string GetUseCase(UseCaseTemplateRequest request, List<UseCaseCodeSnippet> codeSnippets)
		{
			var className = request.UseCase.ClassName;
			var queryProviderType = request.UseCase.NamespaceClassificationKey.ToQueryProviderType();
			var queryProviderName = request.UseCase.NamespaceClassificationKey.ToQueryProviderName();
			var alternativeClass = request.AlternativeClasses.FirstOrDefault(ac => ac.Type == ApplicationUseCaseType.Read);

			var baseType = alternativeClass is null
				? CommonNames.Application.Abstracts.READUSECASE.AsGeneric(request.UseCase.RequestType, request.UseCase.MainDto).ToSimpleBaseType()
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
			unitInformation.AddUsing(CommonNames.Namespaces.TASKS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.EXTENSIONS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.MODELS);
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

			unitInformation.AddMethod(CreateReadMethod(request.UseCase, codeSnippets));

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

		private static (string Name, MemberDeclarationSyntax Method) CreateReadMethod(ApplicationUseCase useCase, List<UseCaseCodeSnippet> codeSnippets)
		{
			var tryBlockStatements = new List<StatementSyntax>();
			var requestEntityId = "request".Access($"{useCase.ClassificationKey}Id");
			var returnDataType = useCase.ResponseType;

			var providerResult = $"{useCase.ClassificationKey.ToVariableName()}Result";
			var provider = useCase.ClassificationKey.ToQueryProviderName().ToFieldName();

			CodeSnippetHelpers.AddStatements(tryBlockStatements, returnDataType, codeSnippets);

			StatementHelpers.AddAsyncMethodCallAndFaultyCheck(tryBlockStatements, provider, $"{useCase.UseCaseName}Async", providerResult, returnDataType, requestEntityId);
			tryBlockStatements.Add(providerResult.ToNullCheck(returnDataType));

			StatementHelpers.AddLocalAsyncMethodCallAndFaultyCheck(tryBlockStatements, "AdjustDtoAsync", providerResult, returnDataType, false, "request".ToIdentifierName(), providerResult.Access("Data"));

			tryBlockStatements.Add(returnDataType
				.ToIdentifierName()
				.ToInstanceWithInitializer(
					useCase.ClassificationKey
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

			var methodDeclarationName = useCase.MethodName;
			var methodDeclaration = methodDeclarationName.ToMethod(
				"Task".AsGeneric("ResponseData".AsGeneric(returnDataType)),
				statements,
				SyntaxKind.PublicKeyword,
				SyntaxKind.AsyncKeyword
			);

			return (
				methodDeclarationName,
				methodDeclaration
				.WithParameter(
					"request"
					.ToVariableName()
					.ToParameter()
					.WithType(useCase.RequestType.ToType())
				)
			);
		}
	}
}