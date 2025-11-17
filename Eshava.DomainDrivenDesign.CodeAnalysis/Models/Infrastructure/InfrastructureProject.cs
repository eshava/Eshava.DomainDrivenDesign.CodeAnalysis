using System.Collections.Generic;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure
{
	public class InfrastructureProject
	{
		public InfrastructureProject()
		{
			AlternativeClasses = new List<InfrastructureProjectAlternativeClass>();
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

	public class InfrastructureProjectAlternativeClass
	{
		public InfrastructureProjectAlternativeClass()
		{
			ConstructorParameters = new List<InfrastructureProjectAlternativeClassConstructorParameter>();
		}

		public InfrastructureAlternativeClassType Type { get; set; }
		public string Using { get; set; }
		public string ClassName { get; set; }
		public List<InfrastructureProjectAlternativeClassConstructorParameter> ConstructorParameters { get; set; }
	}

	public class InfrastructureProjectAlternativeClassConstructorParameter
	{
		public string Name { get; set; }
		public string Type { get; set; }
		public string UsingForType { get; set; }
	}

	public enum InfrastructureAlternativeClassType
	{
		None = 0,
		DomainModelRepository = 1,
		ChildDomainModelRepository = 2,
		QueryRepository = 3,
		ProviderService = 4,
		AggregateProviderService = 5,
		QueryProviderService = 6
	}
}