using System.Collections.Generic;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure
{
	public class InfrastructureEnvironment
	{
		public InfrastructureProject Project { get; set; }
		public string Domain { get; set; }
		public string InfrastructureNamespace => Project.FullQualifiedNamespace;
		public string InfrastructureNamespaceWithDomain { get; set; }
		public string ApplicationNamespaceWithDomain { get; set; }
		public string DomainProjectNamespace { get; set; }
		public IEnumerable<InfrastructureCodeSnippet> CodeSnippets { get; set; }

		public string GetFullDomainModelName(ReferenceDomainModelMap domainModelMap)
		{
			var fullDomainModelName = domainModelMap.GetDomainModelTypeName(DomainProjectNamespace, InfrastructureNamespaceWithDomain);
			if (!fullDomainModelName.Contains("."))
			{
				fullDomainModelName = $"Domain.{domainModelMap.Domain}.{domainModelMap.DomainModel.NamespaceDirectory}.{domainModelMap.DomainModelName}";
			}

			return fullDomainModelName;
		}
	}
}