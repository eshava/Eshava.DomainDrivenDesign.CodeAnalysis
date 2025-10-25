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
				CollectPropertyUsings(unitInformation, property, propertyAttributes, domainModel, false);
			}

			foreach (var property in dtoMap.Dto.Properties)
			{
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
					CollectPropertyUsings(unitInformation, property, propertyAttributes, domainModel, true);
				}

				foreach (var property in dtoMap.Dto.ValidationRuleProperties)
				{
					var attributes = AttributeTemplate.CreateAttributes(propertyAttributes[property.Name]);
					unitInformation.AddProperty(property.Name.ToProperty(property.Type.ToType(), SyntaxKind.PublicKeyword, true, true, attributes: attributes), property.Name);
				}
			}

			foreach (var property in dtoMap.Dto.Properties)
			{
				CollectPropertyUsings(unitInformation, property, propertyAttributes, domainModel, false);
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

		private static void CollectPropertyUsings(UnitInformation unitInformation, ApplicationUseCaseDtoProperty property, Dictionary<string, List<AttributeDefinition>> propertyAttributes, ReferenceDomainModelMap domainModel, bool isValidation)
		{
			if (!property.UsingForType.IsNullOrEmpty())
			{
				unitInformation.AddUsing(property.UsingForType);
			}

			var attributes = property.Attributes ?? [];
			if (!isValidation)
			{
				var domainAttributes = domainModel
					?.DomainModel
					?.Properties
					.FirstOrDefault(p => p.Name == property.Name)?.Attributes
					?? [];

				foreach (var domainAttribute in domainAttributes)
				{
					var attribute = attributes.FirstOrDefault(a => a.Name == domainAttribute.Name);
					if (attribute is null)
					{
						attributes.Add(domainAttribute);
					}
				}
			}

			propertyAttributes.Add(property.Name, attributes);

			if (attributes.Count > 0)
			{
				foreach (var attribute in attributes)
				{
					if (!attribute.UsingForType.IsNullOrEmpty())
					{
						unitInformation.AddUsing(attribute.UsingForType);
					}
				}
			}
		}
	}
}