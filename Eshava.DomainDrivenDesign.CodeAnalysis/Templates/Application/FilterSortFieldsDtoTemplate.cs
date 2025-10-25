using Eshava.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Constants;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application;
using Microsoft.CodeAnalysis.CSharp;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Templates.Application
{
	public static class FilterSortFieldsDtoTemplate
	{
		public static string GetSortFieldsDto(ApplicationUseCase useCase, ApplicationUseCaseDto dto, string useCaseNamespace, bool addAssemblyCommentToFiles)
		{
			var unitInformation = new UnitInformation($"{useCase.ClassificationKey}{useCase.UseCaseName}SortFieldsDto", useCaseNamespace, addConstructor: false, addAssemblyComment: addAssemblyCommentToFiles);
			unitInformation.AddClassModifier(SyntaxKind.PublicKeyword, SyntaxKind.PartialKeyword);

			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.Linq.MODELS);
			
			foreach (var property in dto.Properties)
			{
				if (!(property.IsSortable ?? false))
				{
					continue;
				}

				unitInformation.AddProperty(property.Name.ToProperty("SortField".ToType(), SyntaxKind.PublicKeyword, true, true), property.Name);
			}

			return unitInformation.CreateCodeString();
		}
	}
}