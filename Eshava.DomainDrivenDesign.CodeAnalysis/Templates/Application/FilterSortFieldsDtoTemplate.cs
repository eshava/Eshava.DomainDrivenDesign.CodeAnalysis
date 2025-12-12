using System.Collections.Generic;
using System.Linq;
using Eshava.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Constants;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application;
using Microsoft.CodeAnalysis.CSharp;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Templates.Application
{
	public static class FilterSortFieldsDtoTemplate
	{
		public static IEnumerable<(string ClassName, string SourceCode)> GetSortFieldsDto(ApplicationUseCase useCase, ApplicationUseCaseDto dto, string useCaseNamespace, bool addAssemblyCommentToFiles)
		{
			var sortFieldClasses = new List<(string ClassName, string SourceCode)>();
			var useCaseDtos = useCase.Dtos.ToDictionary(dto => dto.Name, dto => dto);

			CreateSortFieldsDto(useCase, dto, "", useCaseNamespace, addAssemblyCommentToFiles, useCaseDtos, sortFieldClasses);

			return sortFieldClasses;
		}

		private static string CreateSortFieldsDto(
			ApplicationUseCase useCase,
			ApplicationUseCaseDto dto,
			string namePrefix,
			string useCaseNamespace,
			bool addAssemblyCommentToFiles,
			Dictionary<string, ApplicationUseCaseDto> useCaseDtos,
			IList<(string ClassName, string SourceCode)> sortFieldClasses
		)
		{
			var className = $"{useCase.ClassificationKey}{useCase.UseCaseName}{namePrefix}SortFieldsDto";

			var unitInformation = new UnitInformation(className, useCaseNamespace, addConstructor: false, addAssemblyComment: addAssemblyCommentToFiles);
			unitInformation.AddClassModifier(SyntaxKind.PublicKeyword, SyntaxKind.PartialKeyword);

			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.Linq.MODELS);

			if (!namePrefix.IsNullOrEmpty())
			{
				unitInformation.AddBaseType("NestedSort".ToIdentifierName().ToSimpleBaseType());
			}

			foreach (var property in dto.Properties)
			{
				if (useCaseDtos.TryGetValue(property.Type, out var propertyDto))
				{
					var sortFieldName = CreateSortFieldsDto(useCase, propertyDto, namePrefix + property.Name, useCaseNamespace, addAssemblyCommentToFiles, useCaseDtos, sortFieldClasses);
					unitInformation.AddProperty(property.Name.ToProperty(sortFieldName.ToType(), SyntaxKind.PublicKeyword, true, true), property.Name);

					continue;
				}

				if (!(property.IsSortable ?? false))
				{
					continue;
				}

				unitInformation.AddProperty(property.Name.ToProperty("SortField".ToType(), SyntaxKind.PublicKeyword, true, true), property.Name);
			}

			sortFieldClasses.Add((className, unitInformation.CreateCodeString()));

			return className;
		}
	}
}