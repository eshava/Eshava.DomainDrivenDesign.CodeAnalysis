using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Api
{
	public class ApiRouteCodeSnippetParameter
	{
		public string Using { get; set; }
		public string Type { get; set; }
		public string Name { get; set; }

		/// <summary>
		/// Name of the property of the use case request to which the parameter is to be mapped
		/// </summary>
		public string RequestPropertyName { get; set; }

		public ExpressionSyntax AssignExpression { get; set; }
	}
}