using System.Collections.Generic;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Domain
{
	public class DomainModelNamespace
	{
		public DomainModelNamespace()
		{
			Models = [];
			Enumerations = [];
		}

		public string Domain { get; set; }
		public List<DomainModel> Models { get; set; }
		public List<DomainModelEnumeration> Enumerations { get; set; }
	}
}