using Eshava.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Constants;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure;
using Microsoft.CodeAnalysis.CSharp;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Templates.Infrastructure
{
	public static class RepositoryInterfaceTemplate
	{
		public static string GetInterface(InfrastructureModel model, ReferenceDomainModelMap domainModelMap, string domain, string fullQualifiedDomainNamespace, InfrastructureProject project)
		{
			var @namespace = $"{fullQualifiedDomainNamespace}.{model.ClassificationKey.ToPlural()}";
			var interfaceName = $"I{domainModelMap.DomainModelName}Repository";
			string baseInterface;

			var unitInformation = new UnitInformation(interfaceName, @namespace, isInterface: true, addAssemblyComment: project.AddAssemblyCommentToFiles);

			unitInformation.AddClassModifier(SyntaxKind.InternalKeyword, SyntaxKind.PartialKeyword);

			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Infrastructure.INTERFACESREPOSITORIES);

			if (model.IsChild)
			{
				unitInformation.AddUsing($"{project.FullQualifiedNamespace}.{domain}.{domainModelMap.AggregateDomainModel.ClassificationKey.ToPlural()}");
				baseInterface = InfrastructureNames.INTERFACECHILDDOMAINMODELREPOSITORY;
			}
			else
			{
				baseInterface = InfrastructureNames.INTERFACEDOMAINMODELREPOSITORY;
			}

			var fullDomainModelName = $"Domain.{domain}.{domainModelMap.DomainModel.NamespaceDirectory}.{domainModelMap.DomainModelName}";
			var baseType = model.IsChild
				? baseInterface.AsGeneric(fullDomainModelName, $"{domainModelMap.AggregateDomainModel.ClassificationKey.ToPlural()}.{domainModelMap.AggregateDomainModel.DomainModelName}CreationBag", model.IdentifierType).ToSimpleBaseType()
				: baseInterface.AsGeneric(fullDomainModelName, model.IdentifierType).ToSimpleBaseType();

			unitInformation.AddBaseType(baseType);

			if (domainModelMap is not null && !domainModelMap.IsChildDomainModel)
			{
				foreach (var foreignKeyReference in domainModelMap.ForeignKeyReferences)
				{
					if (foreignKeyReference.IsProcessingProperty || foreignKeyReference.DomainModel.IsValueObject)
					{
						continue;
					}

					unitInformation.AddUsing(CommonNames.Namespaces.GENERIC);
					unitInformation.AddUsing(CommonNames.Namespaces.TASKS);
					unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.MODELS);

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
			}

			return unitInformation.CreateCodeString();
		}
	}
}