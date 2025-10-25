using Eshava.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Domain;
using Microsoft.CodeAnalysis.CSharp;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Templates.Domain
{
	public static class EnumerationTemplate
	{
		public static string GetEnumeration(ReferenceEnumMap enumerationMap, DomainProject project)
		{
			var @namespace = $"{project.FullQualifiedNamespace}.{enumerationMap.Namespace}";

			var unitInformation = new UnitInformation(enumerationMap.Name, @namespace, isEnumeration: true, addAssemblyComment: project.AddAssemblyCommentToFiles);
			unitInformation.AddClassModifier(SyntaxKind.PublicKeyword);

			foreach (var item in enumerationMap.Enumeration.Items)
			{
				unitInformation.EnumerationMembers.Add(item.Name.ToEnumerationMember(item.Value));
			}

			return unitInformation.CreateCodeString();
		}
	}
}