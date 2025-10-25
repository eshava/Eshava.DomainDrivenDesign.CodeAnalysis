namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Domain
{
	public class DomainProject
	{
		public string FullQualifiedNamespace { get; set; }
		public string AlternativeUsing { get; set; }
		public string AlternativeAbstractAggregate { get; set; }
		public string AlternativeAbstractDomainModel { get; set; }
		public string AlternativeAbstractChildDomainModel { get; set; }

		/// <summary>
		/// Configuration property for code compilation
		/// </summary>
		public bool AddAssemblyCommentToFiles{ get; set; }
	}
}