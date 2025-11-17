using System.Collections.Generic;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application
{
	public class ApplicationProject
	{
		public ApplicationProject()
		{
			AlternativeClasses = new List<ApplicationProjectAlternativeClass>();
		}

		public string FullQualifiedNamespace { get; set; }
		public string ScopedSettingsClass { get; set; }
		public string ScopedSettingsUsing { get; set; }

		public List<ApplicationProjectAlternativeClass> AlternativeClasses { get; set; }

		/// <summary>
		/// Configuration property for code compilation
		/// </summary>
		public bool AddAssemblyCommentToFiles { get; set; }
	}

	public class ApplicationProjectAlternativeClass
	{
		public ApplicationProjectAlternativeClass()
		{
			ConstructorParameters = new List<ApplicationProjectAlternativeClassConstructorParameter>();
		}

		public ApplicationUseCaseType Type { get; set; }
		public string Using { get; set; }
		public string ClassName { get; set; }
		public List<ApplicationProjectAlternativeClassConstructorParameter> ConstructorParameters { get; set; }
	}

	public class ApplicationProjectAlternativeClassConstructorParameter
	{
		public string Name { get; set; }
		public string Type { get; set; }
		public string UsingForType { get; set; }
	}
}