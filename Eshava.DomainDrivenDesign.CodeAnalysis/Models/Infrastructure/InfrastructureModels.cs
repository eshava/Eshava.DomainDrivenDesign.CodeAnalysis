using System.Collections.Generic;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure
{
	public class InfrastructureModels
	{
		public InfrastructureModels()
		{
			Namespaces = new List<InfrastructureModelNamespace>();
		}
		public List<InfrastructureModelNamespace> Namespaces { get; set; }
	}

	public class InfrastructureModelNamespace
	{
		public InfrastructureModelNamespace()
		{
			Models = new List<InfrastructureModel>();
		}

		public string Domain { get; set; }
		public string DatabaseSchema { get; set; }
		public string DatabaseSettingsInterface { get; set; }
		public string DatabaseSettingsInterfaceUsing { get; set; }
		public List<InfrastructureModel> Models { get; set; }
	}

	public class InfrastructureModel
	{
		public InfrastructureModel()
		{
			Properties = new List<InfrastructureModelPropery>();
			QueryProviderServiceConstructorParameters = new List<QueryProviderServiceConstructorParameter>();
		}

		public List<QueryProviderServiceConstructorParameter> QueryProviderServiceConstructorParameters { get; set; }
		public string IdentifierType { get; set; }
		public bool IdentifierGenerationOnAdd { get; set; }
		public string TableName { get; set; }
		public string Name { get; set; }
		public string ClassificationKey { get; set; }
		public List<InfrastructureModelPropery> Properties { get; set; }

		public bool CreateCreationBag { get; set; }
		public bool IsChild { get; set; }
		public bool CreateRepository { get; set; }
		public bool CreateProviderService { get; set; }
		public string ReferencedParent { get; set; }
	}

	public class QueryProviderServiceConstructorParameter
	{
		public string Name { get; set; }
		public string Type { get; set; }
		public string UsingForType { get; set; }
	}

	public class InfrastructureModelPropery
	{
		public string Name { get; set; }
		public string Type { get; set; }
		public string UsingForType { get; set; }

		/// <summary>
		/// Property is foreign key to <see cref="ReferenceType"/>
		/// </summary>
		public bool IsReference { get; set; }
		public string ReferenceType { get; set; }
		public string ReferencePropertyName { get; set; }
		public string ReferenceDomain { get; set; }
		
		public bool SkipFromDomainModel { get; set; }
		public bool AddToCreationBag { get; set; }
		public bool IsParentReference { get; set; }

		internal string TypeWithUsing
		{
			get
			{
				if (UsingForType.IsNullOrEmpty())
				{
					return Type;
				}

				return $"{UsingForType}.{Type}";
			}
		}
	}
}
