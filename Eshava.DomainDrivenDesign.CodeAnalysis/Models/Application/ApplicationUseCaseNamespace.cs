using System.Collections.Generic;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application
{
	public class ApplicationUseCaseNamespace
	{
		public ApplicationUseCaseNamespace()
		{
			UseCases = [];
		}

		public string Domain { get; set; }
		public List<ApplicationUseCase> UseCases { get; set; }
	}
}