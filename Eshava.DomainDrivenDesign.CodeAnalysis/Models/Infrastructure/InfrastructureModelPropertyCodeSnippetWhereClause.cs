using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure
{
	public class InfrastructureModelPropertyCodeSnippetWhereClause
	{
		/// <summary>
		/// Only if <see cref="InfrastructureModelPropertyCodeSnippet.IsFilter"/> is set true, 
		/// this property forces the code snippet to be used as a where condition.
		/// </summary>
		public bool ForceAsWhereCondition { get; set; }
		public bool IsConditionalWhereCondition => Condition is not null;
		public ExpressionSyntax Condition { get; set; }
	}
}