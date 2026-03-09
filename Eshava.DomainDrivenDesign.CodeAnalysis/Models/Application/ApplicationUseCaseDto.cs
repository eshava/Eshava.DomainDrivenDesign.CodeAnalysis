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
		/// Property that only exists in data model and defines the type identifier property for the dto
		/// </summary>
		public string DataModelTypeProperty { get; set; }
		/// <summary>
		/// Value of <see cref="DataModelTypeProperty"/>
		/// </summary>
		public string DataModelTypePropertyValue { get; set; }

		/// <summary>
		/// Overrides <see cref="Domain.DomainModel.AddGeneralPatchMethod"/> and <see cref="Domain.DomainModel.HasGeneralPatchMethod"/> generator behaviour
		/// Generated code will call a self implemented method
		/// </summary>
		public bool HasUseCaseSpecificPatchMethod { get; set; }

		/// <summary>
		/// Optional
		/// </summary>
		public List<ApplicationUseCaseDtoProperty> Properties { get; set; }
		public List<ApplicationUseCaseDtoProperty> ValidationRuleProperties { get; set; }

		public ApplicationCustomUseCaseDtoSettings CustomUseCaseSettings { get; set; }
	}
}