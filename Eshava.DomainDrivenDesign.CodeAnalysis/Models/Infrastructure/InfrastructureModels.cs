using System.Collections.Generic;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure
{
	public class InfrastructureModels
	{
		public InfrastructureModels()
		{
			Namespaces = [];
		}

		public List<InfrastructureModelNamespace> Namespaces { get; set; }
	}	
}