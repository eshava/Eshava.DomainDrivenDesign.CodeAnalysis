using System.Collections.Generic;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application
{
	public class ApplicationProjectAlternativeClass
	{
		public ApplicationProjectAlternativeClass()
		{
			ConstructorParameters = [];
		}

		public ApplicationUseCaseType Type { get; set; }
		public string Using { get; set; }
		public string ClassName { get; set; }
		public List<ConstructorParameter> ConstructorParameters { get; set; }
	}
}