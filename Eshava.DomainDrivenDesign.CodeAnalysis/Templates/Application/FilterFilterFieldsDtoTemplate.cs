using System.Collections.Generic;
using System.Linq;
using Eshava.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Constants;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Templates.Application
{
	public static class FilterFilterFieldsDtoTemplate
	{
		public static IEnumerable<(string ClassName, string SourceCode)> GetFilterFieldsDto(ApplicationUseCase useCase, ApplicationUseCaseDto dto, string useCaseNamespace, bool addAssemblyCommentToFiles)
		{
			var filterFieldClasses = new List<(string ClassName, string SourceCode)>();
			var useCaseDtos = useCase.Dtos.ToDictionary(dto => dto.Name, dto => dto);

			CreateFilterFieldsDto(useCase, dto, "", useCaseNamespace, addAssemblyCommentToFiles, useCaseDtos, filterFieldClasses);

			return filterFieldClasses;
		}

		private static void CreateFilterFieldsDto(
			ApplicationUseCase useCase,
			ApplicationUseCaseDto dto,
			string namePrefix,
			string useCaseNamespace,
			bool addAssemblyCommentToFiles,
			Dictionary<string, ApplicationUseCaseDto> useCaseDtos,
			IList<(string ClassName, string SourceCode)> filterFieldClasses
		)
		{
			var className = $"{useCase.ClassificationKey}{useCase.UseCaseName}{namePrefix}FilterFieldsDto";

			var unitInformation = new UnitInformation(className, useCaseNamespace, addConstructor: false, addAssemblyComment: addAssemblyCommentToFiles);
			unitInformation.AddClassModifier(SyntaxKind.PublicKeyword, SyntaxKind.PartialKeyword);

			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.Linq.ATTRIBUTES);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.Linq.ENUMS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.Linq.MODELS);

			if (!namePrefix.IsNullOrEmpty())
			{
				unitInformation.AddBaseType("NestedFilter".ToIdentifierName().ToSimpleBaseType());
			}

			foreach (var property in dto.Properties)
			{
				if (useCaseDtos.TryGetValue(property.Type, out var propertyDto))
				{
					CreateFilterFieldsDto(useCase, propertyDto, namePrefix + property.Name, useCaseNamespace, addAssemblyCommentToFiles, useCaseDtos, filterFieldClasses);
					unitInformation.AddProperty(property.Name.ToProperty(property.Type.ToType(), SyntaxKind.PublicKeyword, true, true), property.Name);

					continue;
				}

				if (!(property.IsSearchable ?? false))
				{
					continue;
				}

				var attributes = AttributeTemplate.CreateAttributes(CreateOperatorAttributes(property.SearchOperations));
				unitInformation.AddProperty(property.Name.ToProperty("FilterField".ToType(), SyntaxKind.PublicKeyword, true, true, attributes: attributes), property.Name);
			}

			filterFieldClasses.Add((className, unitInformation.CreateCodeString()));
		}

		private static IEnumerable<AttributeDefinition> CreateOperatorAttributes(IEnumerable<string> searchOperations)
		{
			return searchOperations
				.Select(operation => new AttributeDefinition
				{
					Name = "AllowedCompareOperator",
					Parameters =
					[
						new() { Value = $"CompareOperator.{operation}", Type = "enum" }
					]
				})
				.ToList();
		}
	}
}