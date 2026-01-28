using System.Collections.Generic;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure
{
	public class InfrastructureCodeSnippet
	{
		public InfrastructureCodeSnippet()
		{
			ConstructorParameters = [];
			PropertyStatements = [];
		}

		public bool ApplyOnRepository { get; set; }
		public bool ApplyOnQueryRepository { get; set; }
		public bool ApplyOnInstrastructureProviderService { get; set; }

		public IEnumerable<string> AdditionalUsings { get; set; }
		public List<InfrastructureCodeSnippetParameter> ConstructorParameters { get; set; }
		public List<InfrastructureModelPropertyCodeSnippet> PropertyStatements { get; set; }
	}	
}