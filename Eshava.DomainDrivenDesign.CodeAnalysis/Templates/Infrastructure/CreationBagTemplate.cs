using System.Linq;
using Eshava.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Constants;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Templates.Infrastructure
{
	public static class CreationBagTemplate
	{
		public static string GetCreationBag(InfrastructureModel model, ReferenceDomainModelMap domainModel, string fullQualifiedDomainNamespace, bool addAssemblyCommentToFiles)
		{
			var @namespace = $"{fullQualifiedDomainNamespace}.{model.ClassificationKey.ToPlural()}";
			var modelName = $"{domainModel.DomainModelName}CreationBag";

			var unitInformation = new UnitInformation(modelName, @namespace, addAssemblyComment: addAssemblyCommentToFiles);
			unitInformation.AddClassModifier(SyntaxKind.InternalKeyword);
			unitInformation.AddContructorModifier(SyntaxKind.InternalKeyword);

			unitInformation.AddUsing(CommonNames.Namespaces.SYSTEM);

			unitInformation.AddConstructorParameter($"{model.ClassificationKey.ToVariableName()}Id", model.IdentifierType.ToType(), Enums.ParameterTargetType.PropertyReadonly);


			foreach (var property in model.Properties.Where(p => p.AddToCreationBag))
			{
				unitInformation.AddConstructorParameter(property.Name.ToVariableName(), property.Type.ToType(), Enums.ParameterTargetType.PropertyReadonly);
			}

			return unitInformation.CreateCodeString();
		}
	}
}