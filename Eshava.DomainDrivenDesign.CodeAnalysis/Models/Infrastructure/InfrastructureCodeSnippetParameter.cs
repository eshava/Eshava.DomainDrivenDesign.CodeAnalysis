using System.Collections.Generic;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure
{
	public class InfrastructureCodeSnippetParameter
	{
		public InfrastructureCodeSnippetParameter()
		{
			Attributes = [];
		}

		public string Using { get; set; }
		public string Type { get; set; }
		public string Name { get; set; }

		/// <summary>
		/// Will be only applies on properties
		/// </summary>
		public List<AttributeDefinition> Attributes { get; set; }
	}
}