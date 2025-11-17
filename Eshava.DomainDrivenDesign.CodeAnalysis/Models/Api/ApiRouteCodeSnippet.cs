using System.Collections.Generic;
using System.Linq;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Api
{
	public class ApiRouteCodeSnippet
	{
		public ApiRouteCodeSnippet()
		{
			AdditionalUsings = [];
		}

		public IEnumerable<string> AdditionalUsings { get; set; }
		public List<ApiRouteCodeSnippetParameter> Parameters { get; set; }

		/// <summary>
		/// If empty, the code snipped will be applies on all use case types
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