using System.Collections.Generic;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Domain
{
	public class DomainModels
	{
		public DomainModels()
		{
			Namespaces = [];
		}

		public List<DomainModelNamespace> Namespaces { get; set; }
	}
}