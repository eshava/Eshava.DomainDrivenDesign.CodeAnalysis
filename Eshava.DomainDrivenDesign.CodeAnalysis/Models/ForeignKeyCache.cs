using System.Collections.Generic;
using System.Linq;
using Eshava.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models
{
	public class ForeignKeyCache
	{
		public string Domain { get; set; }
		public string DomainModelName { get; set; }
		public string ClassificationKey { get; set; }

		public string HashSetName { get; set; }
		public string IdentifierType { get; set; }
		public HashSet<string> Owner { get; set; }
		public bool IsUsed { get; set; }

		public TypeSyntax HashSetType => "HashSet".AsGeneric(IdentifierType);

		public (bool IsUsed, IEnumerable<ApplicationUseCaseDtoProperty> Properties) IsReferencedInDto(ReferenceDomainModelMap domainModelMap, ReferenceDtoMap dtoMap)
		{
			var foreignKeyReferences = domainModelMap.ForeignKeyReferences
				.Where(r => r.Domain == Domain && r.DomainModelName == DomainModelName)
				.ToList();

			var dtoProperties = dtoMap.Dto.Properties
				.Where(p => foreignKeyReferences.Any(r => r.PropertyName == p.Name))
				.ToList();

			if (dtoProperties.Count > 0)
			{
				IsUsed = true;

				return (true, dtoProperties);
			}

			return (false, null);
		}
	}
}