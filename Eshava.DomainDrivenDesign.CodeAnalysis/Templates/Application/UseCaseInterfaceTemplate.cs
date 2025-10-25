using Eshava.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Constants;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application;
using Microsoft.CodeAnalysis.CSharp;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Templates.Application
{
	public static class UseCaseInterfaceTemplate
	{
		public static string GetInterface(ApplicationUseCase useCase, string useCaseNamespace, bool addAssemblyCommentToFiles)
		{
			var requestName = useCase.RequestType;
			var responseName = useCase.ResponseType;

			var unitInformation = new UnitInformation($"I{useCase.ClassName}", useCaseNamespace, isInterface: true, addAssemblyComment: addAssemblyCommentToFiles);
			unitInformation.AddClassModifier(SyntaxKind.PublicKeyword, SyntaxKind.PartialKeyword);

			unitInformation.AddUsing(CommonNames.Namespaces.TASKS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.MODELS);

			var methodDeclarationName = $"{useCase.UseCaseName}Async";
			var methodDeclaration = methodDeclarationName
				.ToMethodDefinition(
				"Task".AsGeneric("ResponseData".AsGeneric(responseName)),
				null
				)
				.WithParameter("request".ToParameter().WithType(requestName.ToType()))
				.AddSemicolon();

			unitInformation.AddMethod((methodDeclarationName, methodDeclaration));

			if (useCase.AddValidationConfigurationMethod && (useCase.Type == ApplicationUseCaseType.Create || useCase.Type == ApplicationUseCaseType.Update))
			{
				unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Application.DTOS);

				var validationMethodDeclarationName = "GetValidationConfiguration";
				var validationMethodDeclaration = validationMethodDeclarationName
					.ToMethodDefinition(
					"ResponseData".AsGeneric("ValidationConfigurationResponse"),
					null
					)
					.AddSemicolon();

				unitInformation.AddMethod((validationMethodDeclarationName, validationMethodDeclaration));
			}

			return unitInformation.CreateCodeString();
		}
	}
}