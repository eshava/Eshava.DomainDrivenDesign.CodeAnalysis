using System.Collections.Generic;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Domain
{
	public class DomainModelProperty
	{
		public DomainModelProperty()
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
		/// <summary>
		/// Adds a ReadByPropertyName method to infrastructure provider service and repository in order to read the domain model
		/// Note: Either <see cref="ReadByProperty"/> or <see cref="ReadManyByProperty"/> can be used.
		/// </summary>
		public bool ReadByProperty { get; set; }
		/// <summary>
		/// Adds a ReadByPropertyName method to infrastructure provider service and repository in order to read multiple domain models
		/// Note: Either <see cref="ReadByProperty"/> or <see cref="ReadManyByProperty"/> can be used.
		/// </summary>
		public bool ReadManyByProperty { get; set; }

		/// <summary>
		/// Only for value objects
		/// Value object will be automatically mapped to an data model (if configured) or mapped by custom code
		/// </summary>
		public bool ProcessAsUnit { get; set; }

		public List<DomainModelPropertyValidationRule> ValidationRules { get; set; }
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

		internal bool AllowReadByProperty
		{
			get
			{
				return ReadByProperty 
					&& !IsProcessingProperty 
					&& !ProcessAsUnit;
			}
		}

		internal bool AllowReadManyByProperty
		{
			get
			{
				return ReadManyByProperty
					&& !ReadByProperty
					&& !IsProcessingProperty
					&& !ProcessAsUnit;
			}
		}
	}
}