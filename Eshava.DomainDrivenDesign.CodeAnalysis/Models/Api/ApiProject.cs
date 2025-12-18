namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Api
{
	public class ApiProject
	{
		public string FullQualifiedNamespace { get; set; }
		public string EndpointNamespace { get; set; }
		public string ScopedSettingsClass { get; set; }
		public string ScopedSettingsUsing { get; set; }
		public string ResponseDataExtensionsUsing { get; set; }
		public string ErrorResponseClass { get; set; }
		public string ErrorResponseUsing { get; set; }

		/// <summary>
		/// Configuration property for code compilation
		/// </summary>
		public bool AddAssemblyCommentToFiles { get; set; }
	}
}