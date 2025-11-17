using System.Collections.Generic;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Domain
{
	public class DomainModelPropery
	{
		public DomainModelPropery()
		{
			ValidationRules = [];
			Attributes = [];
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
}