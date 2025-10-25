using System.Collections.Generic;
using System.Linq;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Domain
{
	public class DomainModels
	{
		public DomainModels()
		{
			Namespaces = new List<DomainModelNamespace>();
		}
		public List<DomainModelNamespace> Namespaces { get; set; }
	}

	public class DomainModelNamespace
	{
		public DomainModelNamespace()
		{
			Models = new List<DomainModel>();
			Enumerations = new List<DomainModelEnumeration>();
		}

		public string Domain { get; set; }
		public List<DomainModel> Models { get; set; }
		public List<DomainModelEnumeration> Enumerations { get; set; }
	}

	public class DomainModelEnumeration
	{
		public DomainModelEnumeration()
		{
			Items = new List<DomainModelEnumerationItem>();
		}

		public string Name { get; set; }
		public string ClassificationKey { get; set; }
		/// <summary>
		/// If set, overrides the <see cref="ClassificationKey"/>
		/// </summary>
		public string FeatureName { get; set; }

		public List<DomainModelEnumerationItem> Items { get; set; }
	}

	public class DomainModelEnumerationItem
	{
		public string Name { get; set; }
		public int Value { get; set; }
	}

	public class DomainModel
	{
		public DomainModel()
		{
			Properties = new List<DomainModelPropery>();
			ChildDomainModels = new List<string>();
			ProviderServiceConstructorParameters = new List<ProviderServiceConstructorParameter>();
		}

		public bool IsAggregate { get; set; }
		public List<string> ChildDomainModels { get; set; }
		public List<ProviderServiceConstructorParameter> ProviderServiceConstructorParameters { get; set; }
		public string IdentifierType { get; set; }
		public string Name { get; set; }
		public string ClassificationKey { get; set; }
		public string DataModelName { get; set; }
		/// <summary>
		/// Property that only exists in Data Model and defines the type identifier property for the domain model
		/// </summary>
		public string DataModelTypeProperty { get; set; }
		/// <summary>
		/// Value of <see cref="DataModelTypeProperty"/>
		/// </summary>
		public string DataModelTypePropertyValue { get; set; }

		/// <summary>
		/// If set, overrides the <see cref="ClassificationKey"/>
		/// </summary>
		public string FeatureName { get; set; }
		public List<DomainModelPropery> Properties { get; set; }
		public bool AddGeneralPatchMethod { get; set; }
		public bool AddInfrastructureProviderServiceByPassMethod { get; set; }
		internal bool HasValidationRules => Properties.Count(p => p.ValidationRules.Count > 0) > 0;
		internal string NamespaceDirectory => GetNamespaceDirectory(FeatureName, ClassificationKey);
		internal bool IsNamespaceDirectoryUncountable => CheckIsNamespaceDirectoryUncountable(FeatureName, ClassificationKey);

		public static string GetNamespaceDirectory(string featureName, string classificationKey)
		{
			return featureName.IsNullOrEmpty()
				? classificationKey.ToPlural()
				: featureName
				;
		}

		public static bool CheckIsNamespaceDirectoryUncountable(string featureName, string classificationKey)
		{
			return featureName.IsNullOrEmpty()
				? classificationKey.IsUncountable()
				: false;
		}
	}

	public class ProviderServiceConstructorParameter
	{
		public string Name { get; set; }
		public string Type { get; set; }
		public string UsingForType { get; set; }
	}

	public class DomainModelPropery
	{
		public DomainModelPropery()
		{
			ValidationRules = new List<DomainModelProperyValidationRule>();
		}

		public string Name { get; set; }
		public string Type { get; set; }
		public string UsingForType { get; set; }

		/// <summary>
		/// Should be used, if the domain model property name is different to the data model property name  
		/// </summary>
		public string DataModelPropertyName { get; set; }


		public bool SkipForConstructor { get; set; }

		/// <summary>
		/// Property is foreign key to <see cref="ReferenceType"/>
		/// </summary>
		public bool IsReference { get; set; }
		public string ReferenceType { get; set; }
		public string ReferenceDomain { get; set; }
		public bool IsProcessingProperty { get; set; }

		public List<DomainModelProperyValidationRule> ValidationRules { get; set; }
		public List<AttributeDefinition> Attributes { get; set; }

		internal bool HasValidReference
		{
			get
			{
				return IsReference
					&& !ReferenceType.IsNullOrEmpty();
			}
		}

		internal string TypeWithUsing
		{
			get
			{
				return UsingForType.IsNullOrEmpty()
					? Type
					: $"{UsingForType}.{Type}";
			}
		}
	}

	public class DomainModelProperyValidationRule
	{
		public DomainModelProperyValidationRule()
		{
			RelatedProperties = new List<string>();
		}

		public ValidationRuleType Type { get; set; }

		public List<string> RelatedProperties { get; set; }
	}

	public enum ValidationRuleType
	{
		None = 0,
		Unique = 1
	}
}