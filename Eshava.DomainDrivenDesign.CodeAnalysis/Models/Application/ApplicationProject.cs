namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application
{
	public class ApplicationProject
	{
		public string FullQualifiedNamespace { get; set; }
		public string ScopedSettingsClass { get; set; }
		public string ScopedSettingsUsing { get; set; }
		
		/// <summary>
		/// Configuration property for code compilation
		/// </summary>
		public bool AddAssemblyCommentToFiles{ get; set; }
	}
}