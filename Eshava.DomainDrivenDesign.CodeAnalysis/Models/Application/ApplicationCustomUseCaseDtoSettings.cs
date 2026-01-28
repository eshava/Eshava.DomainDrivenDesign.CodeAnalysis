namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application
{
	public class ApplicationCustomUseCaseDtoSettings
	{
		public bool AddToRequest { get; set; }
		public bool AddOnlyPropertiesToRequest { get; set; }
		public string RequestPropertyName { get; set; }
		public bool AddToResponse { get; set; }
		public bool AddOnlyPropertiesToResponse { get; set; }
		public string ResponsePropertyName { get; set; }
		public bool IsEnumerable { get; set; }
	}
}