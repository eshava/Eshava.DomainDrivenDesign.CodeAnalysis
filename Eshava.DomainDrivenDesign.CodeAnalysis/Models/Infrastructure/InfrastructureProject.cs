using System.Collections.Generic;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure
{
	public class InfrastructureProject
	{
		public InfrastructureProject()
		{
			AlternativeClasses = [];
		}

		public string FullQualifiedNamespace { get; set; }
		public string AlternativeUsing { get; set; }
		public string AlternativeAbstractDatabaseModel { get; set; }
		public List<InfrastructureProjectAlternativeClass> AlternativeClasses { get; set; }

		public string ScopedSettingsClass { get; set; }
		public string ScopedSettingsUsing { get; set; }
		public bool ImplementSoftDelete { get; set; }

		/// <summary>
		/// Configuration property for code compilation
		/// </summary>
		public bool AddAssemblyCommentToFiles { get; set; }
	}
}