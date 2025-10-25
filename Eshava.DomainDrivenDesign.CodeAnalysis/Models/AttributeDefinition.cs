using System.Collections.Generic;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models
{
	public class AttributeDefinition
	{
		public AttributeDefinition()
		{
			Parameters = new List<AttributeParameter>();
		}

		public string Name { get; set; }
		public string UsingForType { get; set; }
		public List<AttributeParameter> Parameters { get; set; }
	}
}