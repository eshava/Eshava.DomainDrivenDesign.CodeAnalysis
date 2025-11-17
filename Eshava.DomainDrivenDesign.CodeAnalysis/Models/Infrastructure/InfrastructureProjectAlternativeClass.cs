using System.Collections.Generic;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure
{
	public class InfrastructureProjectAlternativeClass
	{
		public InfrastructureProjectAlternativeClass()
		{
			ConstructorParameters = [];
		}

		public InfrastructureAlternativeClassType Type { get; set; }
		public string Using { get; set; }
		public string ClassName { get; set; }
		public List<ConstructorParameter> ConstructorParameters { get; set; }
	}
}