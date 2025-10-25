using System.Collections.Generic;
using System.Linq;
using Eshava.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Constants;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Templates.Application
{
	public static class FilterFilterFieldsDtoTemplate
	{
		public static string GetFilterFieldsDto(ApplicationUseCase useCase, ApplicationUseCaseDto dto, string useCaseNamespace, bool addAssemblyCommentToFiles)
		{
			var unitInformation = new UnitInformation($"{useCase.ClassificationKey}{useCase.UseCaseName}FilterFieldsDto", useCaseNamespace, addConstructor: false, addAssemblyComment: addAssemblyCommentToFiles);
			unitInformation.AddClassModifier(SyntaxKind.PublicKeyword, SyntaxKind.PartialKeyword);

			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.Linq.ATTRIBUTES);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.Linq.ENUMS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.Linq.MODELS);

			foreach (var property in dto.Properties)
			{
				if (!(property.IsSearchable ?? false))
				{
					continue;
				}

				var attributes = AttributeTemplate.CreateAttributes(CreateOperatorAttributes(property.SearchOperations));
				unitInformation.AddProperty(property.Name.ToProperty("FilterField".ToType(), SyntaxKind.PublicKeyword, true, true, attributes: attributes), property.Name);
			}

			return unitInformation.CreateCodeString();
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