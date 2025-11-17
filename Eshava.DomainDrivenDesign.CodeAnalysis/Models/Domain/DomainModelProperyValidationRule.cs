using System.Collections.Generic;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Domain
{
	public class DomainModelProperyValidationRule
	{
		public DomainModelProperyValidationRule()
		{
			RelatedProperties = [];
		}

		public ValidationRuleType Type { get; set; }

		public List<string> RelatedProperties { get; set; }
	}
}