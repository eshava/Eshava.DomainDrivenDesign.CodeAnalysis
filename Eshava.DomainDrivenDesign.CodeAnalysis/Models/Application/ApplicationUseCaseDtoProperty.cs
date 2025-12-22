using System.Collections.Generic;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application
{
	public class ApplicationUseCaseDtoProperty
	{
		private bool _isVirtualProperty = false;

		public ApplicationUseCaseDtoProperty()
		{
			Attributes = [];
			SearchOperations = [];
		}

		public ApplicationUseCaseDtoProperty(bool isVirtualProperty) : this()
		{
			_isVirtualProperty = isVirtualProperty;
		}

		public string Name { get; set; }
		public string Type { get; set; }
		public string UsingForType { get; set; }
		public bool IsEnumerable { get; set; }
		public bool IsGroupProperty { get; set; }

		public bool? IsSortable { get; set; }
		public bool? UseForDefaultSorting { get; set; }
		public bool? IsSearchable { get; set; }
		public List<string> SearchOperations { get; set; }
		public List<AttributeDefinition> Attributes { get; set; }

		/// <summary>
		/// Can be navigation path
		/// e. g.: <see cref="Name"/> is ProductName on dto OrderPosition, than <see cref="ReferencePropertyName"/> is Product.Name
		/// </summary>
		public string ReferenceProperty { get; set; }

		internal bool IsVirtualProperty => _isVirtualProperty;

		internal bool IsNullableType
		{
			get
			{
				return Type.EndsWith("?");
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