using System.Collections.Generic;
using System.Linq;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application
{
	public class UseCaseCodeSnippet
	{
		public UseCaseCodeSnippet()
		{
			AdditionalUsings = [];
		}

		public IEnumerable<string> AdditionalUsings { get; set; }
		public List<UseCaseCodeSnippetParameter> RequestProperties { get; set; }
		public List<UseCaseCodeSnippetParameter> ConstructorParameters { get; set; }
		public List<UseCaseCodeSnippetStatement> Statements { get; set; }

		/// <summary>
		/// If empty, the code snippet will be applies on all use case types
		/// </summary>
		public List<ApplicationUseCaseType> ApplyOnUseCaseTypes { get; set; }

		public bool IsApplicable(ApplicationUseCaseType type)
		{
			if (!(ApplyOnUseCaseTypes?.Any() ?? false))
			{
				return true;
			}

			return ApplyOnUseCaseTypes.Any(t => t == type);
		}
	}	
}