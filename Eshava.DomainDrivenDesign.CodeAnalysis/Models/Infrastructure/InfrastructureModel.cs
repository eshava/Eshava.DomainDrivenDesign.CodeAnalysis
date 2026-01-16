using System.Collections.Generic;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure
{
	public class InfrastructureModel
	{
		public InfrastructureModel()
		{
			Properties = [];
			QueryProviderServiceConstructorParameters = [];
		}

		public List<ConstructorParameter> QueryProviderServiceConstructorParameters { get; set; }
		public string IdentifierType { get; set; }
		public bool IdentifierGenerationOnAdd { get; set; }
		public string TableName { get; set; }
		public string Name { get; set; }
		public string ClassificationKey { get; set; }
		public List<InfrastructureModelProperty> Properties { get; set; }

		public bool CreateCreationBag { get; set; }
		public bool IsChild { get; set; }
		public bool CreateRepository { get; set; }
		public bool CreateProviderService { get; set; }
		public bool CreateDbConfiguration { get; set; }
		public string ReferencedParent { get; set; }
		/// <summary>
		/// Only for value objects
		/// If activated, no name based 
		/// </summary>
		public bool UseCustomMapping { get; set; }
	}
}