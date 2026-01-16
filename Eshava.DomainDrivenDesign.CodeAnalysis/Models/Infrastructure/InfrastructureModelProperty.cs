using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure
{
	public class InfrastructureModelProperty
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