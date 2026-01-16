using System.Collections.Generic;
using System.Linq;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Domain
{
	public class DomainModel
	{
		public DomainModel()
		{
			Properties = [];
			ChildDomainModels = [];
			ProviderServiceConstructorParameters = [];
		}

		public bool IsAggregate { get; set; }
		public bool IsValueObject { get; set; }
		public List<string> ChildDomainModels { get; set; }
		public List<ConstructorParameter> ProviderServiceConstructorParameters { get; set; }
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
		public List<DomainModelProperty> Properties { get; set; }
		public bool AddGeneralPatchMethod { get; set; }
		public bool AddInfrastructureProviderServiceByPassMethod { get; set; }
		public bool CustomCreatedOrChangedChildMethod { get; set; }
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
}