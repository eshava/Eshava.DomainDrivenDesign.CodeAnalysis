using System;
using System.Collections.Generic;
using System.Linq;
using Eshava.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Constants;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Domain;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Templates.Domain
{
	public static class ValueObjectTemplate
	{
		public static string GetValueObject(ReferenceDomainModelMap domainModelMap, DomainProject project, ReferenceMap domainModelReferenceMap)
		{
			var @namespaceDomain = $"{project.FullQualifiedNamespace}.{domainModelMap.Domain}";
			var @namespace = $"{@namespaceDomain}.{domainModelMap.DomainModel.NamespaceDirectory}";
			(var baseClass, var standard) = GetBaseClass(domainModelMap, project);

			var unitInformation = new UnitInformation(domainModelMap.DomainModelName, @namespace, addAssemblyComment: project.AddAssemblyCommentToFiles);

			unitInformation.AddClassModifier(SyntaxKind.PublicKeyword, SyntaxKind.PartialKeyword);
			unitInformation.AddContructorModifier(SyntaxKind.PublicKeyword);
			unitInformation.AddBaseType(baseClass.ToSimpleBaseType());

			unitInformation.AddUsing(CommonNames.Namespaces.SYSTEM);
			unitInformation.AddUsing(CommonNames.Namespaces.GENERIC);
			unitInformation.AddUsing(project.AlternativeUsing);

			if (standard)
			{
				unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Domain.MODELS);
			}

			foreach (var property in domainModelMap.DomainModel.Properties)
			{
				unitInformation.AddUsing(property.UsingForType);

				if (property.Attributes?.Any() ?? false)
				{
					foreach (var attribute in property.Attributes)
					{
						unitInformation.AddUsing(attribute.UsingForType);
					}
				}

				var attributes = AttributeTemplate.CreateAttributes(property.Attributes);
				unitInformation.AddProperty(property.Name.ToProperty(property.Type.ToType(), SyntaxKind.PublicKeyword, true, false, attributes: attributes), property.Name);
				unitInformation.AddConstructorParameter(property.Name.ToVariableName(), property.Type, Enums.ParameterTargetTypes.PropertyReadonly);
			}

			return unitInformation.CreateCodeString();
		}

		private static (string Class, bool Standard) GetBaseClass(ReferenceDomainModelMap domainModelMap, DomainProject project)
		{
			if (project.AlternativeAbstractValueObject.IsNullOrEmpty())
			{
				return (DomainNames.ABSTRACTVALUEOBJECT, true);
			}

			return (project.AlternativeAbstractValueObject, false);
		}
	}
}