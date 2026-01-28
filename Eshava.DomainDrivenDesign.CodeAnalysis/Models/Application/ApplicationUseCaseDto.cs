using System.Collections.Generic;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application
{
	public class ApplicationUseCaseDto
	{
		public ApplicationUseCaseDto()
		{
			Properties = [];
			ValidationRuleProperties = [];
		}

		public string Name { get; set; }

		/// <summary>
		/// Domain or data model
		/// </summary>
		public string ReferenceModelName { get; set; }

		/// <summary>
		/// Optional
		/// </summary>
		public List<ApplicationUseCaseDtoProperty> Properties { get; set; }
		public List<ApplicationUseCaseDtoProperty> ValidationRuleProperties { get; set; }

		public ApplicationCustomUseCaseDtoSettings CustomUseCaseSettings { get; set; }
	}
}