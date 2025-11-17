using System.Collections.Generic;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application
{
	public class ApplicationUseCases
	{
		public ApplicationUseCases()
		{
			Namespaces = [];
		}
		public List<ApplicationUseCaseNamespace> Namespaces { get; set; }
	}
}