using Eshava.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Constants;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application;
using Microsoft.CodeAnalysis.CSharp;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Templates.Application
{
	public static class FilterDtoTemplate
	{
		public static string GetFilterDto(ApplicationUseCase useCase, string useCaseNamespace, bool addAssemblyCommentToFiles)
		{
			var unitInformation = new UnitInformation($"{useCase.ClassificationKey}{useCase.UseCaseName}FilterDto", useCaseNamespace, addConstructor: false, addAssemblyComment: addAssemblyCommentToFiles);
			unitInformation.AddClassModifier(SyntaxKind.PublicKeyword, SyntaxKind.PartialKeyword);

			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Application.DTOS);

			unitInformation.AddBaseType("AbstractFilterDto".ToIdentifierName().ToSimpleBaseType());

			var dtoNameFilterFields = $"{useCase.ClassificationKey}{useCase.UseCaseName}FilterFieldsDto";
			unitInformation.AddProperty("FilterFields".ToProperty(dtoNameFilterFields.ToType(), SyntaxKind.PublicKeyword, true, true), "FilterFields");

			var dtoNameSortFields = $"{useCase.ClassificationKey}{useCase.UseCaseName}SortFieldsDto";
			unitInformation.AddProperty("SortFields".ToProperty(dtoNameSortFields.ToType(), SyntaxKind.PublicKeyword, true, true), "SortFields");

			var getFilterFieldsDeclarationName = "GetFilterFields";
			var getFilterFieldsDeclaration = getFilterFieldsDeclarationName
				.ToMethodDefinition(
					Eshava.CodeAnalysis.SyntaxConstants.Object,
					SyntaxKind.PublicKeyword,
					SyntaxKind.OverrideKeyword
				)
				.WithExpressionBody("FilterFields".ToIdentifierName());
			unitInformation.AddMethod((getFilterFieldsDeclarationName, getFilterFieldsDeclaration));

			var getSortFieldsDeclarationName = "GetSortFields";
			var getSortFieldsDeclaration = getSortFieldsDeclarationName
				.ToMethodDefinition(
					Eshava.CodeAnalysis.SyntaxConstants.Object,
					SyntaxKind.PublicKeyword,
					SyntaxKind.OverrideKeyword
				)
				.WithExpressionBody("SortFields".ToIdentifierName());
			unitInformation.AddMethod((getSortFieldsDeclarationName, getSortFieldsDeclaration));

			return unitInformation.CreateCodeString();
		}
	}
}