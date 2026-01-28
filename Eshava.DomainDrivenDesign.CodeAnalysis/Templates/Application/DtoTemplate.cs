using System.Collections.Generic;
using System.Linq;
using Eshava.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Constants;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Templates.Application
{
	public static class DtoTemplate
	{
		public static string GetDto(ReferenceDtoMap dtoMap, string useCaseNamespace, ReferenceDomainModelMap domainModel, bool addAssemblyCommentToFiles)
		{
			var unitInformation = new UnitInformation(dtoMap.DtoName, useCaseNamespace, addConstructor: false, addAssemblyComment: addAssemblyCommentToFiles);
			unitInformation.AddClassModifier(SyntaxKind.PublicKeyword, SyntaxKind.PartialKeyword);

			unitInformation.AddUsing(CommonNames.Namespaces.SYSTEM);
			unitInformation.AddUsing(CommonNames.Namespaces.LINQ);
			unitInformation.AddUsing(CommonNames.Namespaces.GENERIC);

			var propertyAttributes = new Dictionary<string, List<AttributeDefinition>>();

			foreach (var property in dtoMap.Dto.Properties)
			{
				TemplateMethods.CollectPropertyUsings(unitInformation, property, propertyAttributes, domainModel, false);

				var attributes = AttributeTemplate.CreateAttributes(propertyAttributes[property.Name]);
				var propertyType = property.IsEnumerable
					? "IEnumerable".AsGeneric(property.Type)
					: property.Type.ToType();

				unitInformation.AddProperty(property.Name.ToProperty(propertyType, SyntaxKind.PublicKeyword, true, true, attributes: attributes), property.Name);
			}

			return unitInformation.CreateCodeString();
		}

		public static string GetValidationDto(ReferenceDtoMap dtoMap, string useCaseNamespace, ReferenceDomainModelMap domainModel, bool addAssemblyCommentToFiles)
		{
			var unitInformation = new UnitInformation($"Validation{dtoMap.DtoName}", useCaseNamespace, addConstructor: false, addAssemblyComment: addAssemblyCommentToFiles);
			unitInformation.AddClassModifier(SyntaxKind.InternalKeyword);

			unitInformation.AddUsing(CommonNames.Namespaces.SYSTEM);
			unitInformation.AddUsing(CommonNames.Namespaces.LINQ);
			unitInformation.AddUsing(CommonNames.Namespaces.GENERIC);

			var propertyAttributes = new Dictionary<string, List<AttributeDefinition>>();

			if (dtoMap.Dto.ValidationRuleProperties is not null)
			{
				foreach (var property in dtoMap.Dto.ValidationRuleProperties)
				{
					TemplateMethods.CollectPropertyUsings(unitInformation, property, propertyAttributes, domainModel, true);
				}

				foreach (var property in dtoMap.Dto.ValidationRuleProperties)
				{
					var attributes = AttributeTemplate.CreateAttributes(propertyAttributes[property.Name]);
					unitInformation.AddProperty(property.Name.ToProperty(property.Type.ToType(), SyntaxKind.PublicKeyword, true, true, attributes: attributes), property.Name);
				}
			}

			foreach (var property in dtoMap.Dto.Properties)
			{
				TemplateMethods.CollectPropertyUsings(unitInformation, property, propertyAttributes, domainModel, false);
			}

			foreach (var property in dtoMap.Dto.Properties)
			{
				var attributes = AttributeTemplate.CreateAttributes(propertyAttributes[property.Name]);
				var childDto = dtoMap.ChildReferenceProperties.FirstOrDefault(dto => dto.Property.Name == property.Name);

				var type = childDto is null
					? property.Type.ToType()
					: $"Validation{property.Type}".ToType();

				unitInformation.AddProperty(property.Name.ToProperty(type, SyntaxKind.PublicKeyword, true, true, attributes: attributes), property.Name);
			}

			return unitInformation.CreateCodeString();
		}
	}
}