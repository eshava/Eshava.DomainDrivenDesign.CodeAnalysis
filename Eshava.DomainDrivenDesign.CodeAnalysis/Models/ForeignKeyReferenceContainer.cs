using System.Collections.Generic;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models
{
	public class ForeignKeyReferenceContainer
	{
		private Dictionary<string, Dictionary<string, ForeignKeyCache>> _foreignKeyReferenceTypes = new Dictionary<string, Dictionary<string, ForeignKeyCache>>();

		public ForeignKeyReferenceContainer()
		{
			_foreignKeyReferenceTypes = new Dictionary<string, Dictionary<string, ForeignKeyCache>>();
			ForeignKeyHashSets = new List<ForeignKeyCache>();
		}

		public List<ForeignKeyCache> ForeignKeyHashSets { get; set; }

		public void AddReference(ReferenceDomainModel foreignKeyReference, string domainModelName)
		{
			if (!_foreignKeyReferenceTypes.ContainsKey(foreignKeyReference.Domain))
			{
				_foreignKeyReferenceTypes.Add(foreignKeyReference.Domain, new Dictionary<string, ForeignKeyCache>());
			}

			var hashSetName = $"{foreignKeyReference.Domain.ToVariableName()}{foreignKeyReference.ClassificationKey}Ids";

			if (_foreignKeyReferenceTypes[foreignKeyReference.Domain].TryGetValue(foreignKeyReference.DomainModelName, out var cache))
			{
				if (!cache.Owner.Contains(domainModelName))
				{
					cache.Owner.Add(domainModelName);
				}
			}
			else
			{
				cache = new ForeignKeyCache
				{
					Domain = foreignKeyReference.Domain,
					DomainModelName = foreignKeyReference.DomainModelName,
					ClassificationKey = foreignKeyReference.ClassificationKey,
					HashSetName = hashSetName,
					IdentifierType = foreignKeyReference.DomainModel.IdentifierType,
					Owner = [domainModelName]
				};

				_foreignKeyReferenceTypes[foreignKeyReference.Domain].Add(foreignKeyReference.DomainModelName, cache);
				ForeignKeyHashSets.Add(cache);
			}
		}
	}
}