using System.Collections.Generic;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application
{
	public class UseCaseCodeSnippetParameter
	{
		public UseCaseCodeSnippetParameter()
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