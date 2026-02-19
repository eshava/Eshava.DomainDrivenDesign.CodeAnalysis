using Eshava.DomainDrivenDesign.CodeAnalysis.Enums;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure
{
	public class InfrastructureExceptionCodeSnippet
	{
		public string DataModelName { get; set; }
		public string MethodName { get; set; }
		public string ClassName { get; set; }

		/// <summary>
		/// Prevents the application of the code snippet
		/// </summary>
		public bool SkipUsage { get; set; }
		/// <summary>
		/// Causes the code snippet of this class to be used instead of the actual code snippet. 
		/// Is overruled by <see cref="SkipUsage"/>
		/// </summary>
		public bool UseInstead { get; set; }

		/// <summary>
		/// Have to be set, if <see cref=">UseInstead"/> is true
		/// </summary>
		public ExpressionSyntax Expression { get; set; }

		public OperationType Operation { get; set; } = OperationType.Equal;
	}
}