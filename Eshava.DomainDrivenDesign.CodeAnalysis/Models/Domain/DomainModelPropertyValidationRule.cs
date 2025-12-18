using System.Collections.Generic;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Domain
{
	public class DomainModelPropertyValidationRule
	{
		public DomainModelPropertyValidationRule()
		{
			RelatedProperties = [];
		}

		public ValidationRuleType Type { get; set; }

		public List<string> RelatedProperties { get; set; }
	}
}