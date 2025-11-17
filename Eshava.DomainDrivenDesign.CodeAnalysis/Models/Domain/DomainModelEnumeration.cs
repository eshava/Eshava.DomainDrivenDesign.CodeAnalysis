using System.Collections.Generic;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Domain
{
	public class DomainModelEnumeration
	{
		public DomainModelEnumeration()
		{
			Items = [];
		}

		public string Name { get; set; }
		public string ClassificationKey { get; set; }
		/// <summary>
		/// If set, overrides the <see cref="ClassificationKey"/>
		/// </summary>
		public string FeatureName { get; set; }

		public List<DomainModelEnumerationItem> Items { get; set; }
	}
}