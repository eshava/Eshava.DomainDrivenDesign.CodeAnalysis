using System.Collections.Generic;
using System.Linq;
using Eshava.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Constants;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure;
using Microsoft.CodeAnalysis.CSharp;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Templates.Infrastructure
{
	public static class DataModelTemplate
	{
		public static string GetDatabaseModel(
			InfrastructureModel model,
			string domain,
			string fullQualifiedDomainNamespace,
			string alternativeAbstractDatabaseModel,
			string alternativeUsing,
			Dictionary<string, List<InfrastructureModel>> childsForModel,
			Dictionary<string, Dictionary<string, string>> infrastructureModels,
			bool addAssemblyCommentToFiles
		)
		{
			var @namespace = $"{fullQualifiedDomainNamespace}.{model.ClassificationKey.ToPlural()}";

			var unitInformation = new UnitInformation(model.Name, @namespace, addConstructor: false, addAssemblyComment: addAssemblyCommentToFiles);
			unitInformation.AddClassModifier(SyntaxKind.InternalKeyword, SyntaxKind.PartialKeyword);
			unitInformation.AddContructorModifier(SyntaxKind.PublicKeyword);

			unitInformation.AddUsing(CommonNames.Namespaces.SYSTEM);
			unitInformation.AddUsing(alternativeUsing);

			if (!model.TableName.IsNullOrEmpty())
			{
				var baseClass = alternativeAbstractDatabaseModel;
				if (alternativeAbstractDatabaseModel.IsNullOrEmpty())
				{
					baseClass = InfrastructureNames.ABSTRACTDATABASEMODEL;
					unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Infrastructure.MODELS);
				}

				var baseType = baseClass.AsGeneric(model.IdentifierType).ToSimpleBaseType();
				unitInformation.AddBaseType(baseType);
			}

			foreach (var property in model.Properties)
			{
				if (!property.UsingForType.IsNullOrEmpty())
				{
					unitInformation.AddUsing(property.UsingForType);
				}

				var referenceType = property.Type;
				if (property.Type == property.ReferenceType)
				{
					var referenceNameSpace = "";
					if (property.ReferenceDomain.IsNullOrEmpty() || property.ReferenceDomain == domain)
					{
						referenceNameSpace = infrastructureModels[domain][property.ReferenceType].ToPlural();
					}
					else
					{
						referenceNameSpace = $"{property.ReferenceDomain}.{infrastructureModels[property.ReferenceDomain][property.ReferenceType].ToPlural()}";
					}

					referenceType = $"{referenceNameSpace}.{referenceType}";
				}

				unitInformation.AddProperty(property.Name.ToProperty(referenceType.ToType(), SyntaxKind.PublicKeyword, true, true), property.Name);
			}

			if (childsForModel.TryGetValue(model.Name, out var childs))
			{
				foreach (var child in childs)
				{
					var property = model.Properties.FirstOrDefault(p => (p.Type == p.ReferenceType && p.Type == child.Name) || p.Name == child.ClassificationKey);
					if (property is null)
					{
						var referenceNameSpace = infrastructureModels[domain][child.Name].ToPlural();
						var referenceType = $"{referenceNameSpace}.{child.Name}";
						unitInformation.AddProperty(child.ClassificationKey.ToProperty(referenceType.ToType(), SyntaxKind.PublicKeyword, true, true), child.ClassificationKey);
					}
				}
			}

			return unitInformation.CreateCodeString();
		}
	}
}