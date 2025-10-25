using Eshava.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Constants;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models;
using Microsoft.CodeAnalysis.CSharp;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Templates.Application
{
	public static class InfrastructureProviderServiceInterfaceTemplate
	{
		public static string GetInterface(ReferenceDomainModelMap domainModelMap, string domainModelNamespace, string applicationDomainModelNamespace, bool addAssemblyCommentToFiles)
		{
			var interfaceName = $"I{domainModelMap.DomainModelName}InfrastructureProviderService";
			var fullDomainModelName = domainModelMap.DomainModelName.IsUncountable()
				? $"{domainModelNamespace}.{domainModelMap.DomainModelName}"
				: domainModelMap.DomainModelName;

			var unitInformation = new UnitInformation(interfaceName, applicationDomainModelNamespace, true, addAssemblyComment: addAssemblyCommentToFiles);
			unitInformation.AddClassModifier(SyntaxKind.PublicKeyword, SyntaxKind.PartialKeyword);
			unitInformation.AddBaseType("IInfrastructureProvider".AsGeneric(fullDomainModelName, domainModelMap.IdentifierType).ToSimpleBaseType());

			unitInformation.AddUsing(CommonNames.Namespaces.TASKS);
			unitInformation.AddUsing(CommonNames.Namespaces.GENERIC);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.MODELS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Application.Interfaces.PROVIDERS);

			if (!domainModelMap.DomainModelName.IsUncountable())
			{
				unitInformation.AddUsing(domainModelNamespace);
			}

			foreach (var foreignKeyReference in domainModelMap.ForeignKeyReferences)
			{
				if (foreignKeyReference.IsProcessingProperty)
				{
					continue;
				}

				var methodDeclarationName = $"ReadFor{foreignKeyReference.PropertyName}Async";
				var methodDeclaration = methodDeclarationName
					.ToMethodDefinition(
					"Task".AsGeneric("ResponseData".AsGeneric("IEnumerable".AsGeneric(fullDomainModelName))),
					null
					)
					.WithParameter($"{foreignKeyReference.PropertyName.ToVariableName()}".ToParameter().WithType(domainModelMap.IdentifierType.ToType()))
					.AddSemicolon();

				unitInformation.AddMethod((methodDeclarationName, methodDeclaration));
			}

			return unitInformation.CreateCodeString();
		}
	}
}