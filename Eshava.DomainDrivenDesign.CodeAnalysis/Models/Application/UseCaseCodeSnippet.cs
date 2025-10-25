using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

	public class UseCaseCodeSnippetParameter
	{
		public string Using { get; set; }
		public string Type { get; set; }
		public string Name { get; set; }

		/// <summary>
		/// Will be only applies on properties
		/// </summary>
		public List<AttributeDefinition> Attributes { get; set; }
	}

	public class UseCaseCodeSnippetStatement
	{
		/// <summary>
		/// Returns a new instance of the response type of the use case
		/// </summary>
		public bool ReturnResponseInstance { get; set; }

		/// <summary>
		/// Adds an "IsFaulty" check based on a ResponseData instance (<see cref="VariableToCheck"/>) 
		/// and calls ConvertTo on the ResponseData instance with the use case response type as generic type parameter
		/// </summary>
		public bool CreateFaultyCheck { get; set; }
		public ExpressionSyntax VariableToCheck { get; set; }

		/// <summary>
		/// Adds an expression check (<see cref="Expression"/>) 
		/// and returns a new instance of the response type of the use case (<see cref="ReturnResponseInstance"/>)
		/// </summary>
		public bool CreateExpressionCheck { get; set; }
		public ExpressionSyntax Expression { get; set; }

		/// <summary>
		/// Will be used, if no option is true
		/// </summary>
		public StatementSyntax Statement { get; set; }
	}
}