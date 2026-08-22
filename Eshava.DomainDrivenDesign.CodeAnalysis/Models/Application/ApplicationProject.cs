using System.Collections.Generic;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application
{
	public class ApplicationProject
	{
		public ApplicationProject()
		{
			AlternativeClasses = [];
		}

		public string FullQualifiedNamespace { get; set; }
		public string ScopedSettingsClass { get; set; }
		public string ScopedSettingsUsing { get; set; }

		public List<ApplicationProjectAlternativeClass> AlternativeClasses { get; set; }

		/// <summary>
		/// Whether the project serializes with Newtonsoft.Json instead of System.Text.Json.
		///
		/// It decides which JsonIgnore attribute the generated request classes carry, and only that.
		/// Default is false, so a project needs no reference to Newtonsoft.Json - which is the point:
		/// generated code must not force a package on a project that does not serialize with it.
		/// </summary>
		public bool UseNewtonsoftJson { get; set; }

		/// <summary>
		/// Configuration property for code compilation
		/// </summary>
		public bool AddAssemblyCommentToFiles { get; set; }
	}	
}