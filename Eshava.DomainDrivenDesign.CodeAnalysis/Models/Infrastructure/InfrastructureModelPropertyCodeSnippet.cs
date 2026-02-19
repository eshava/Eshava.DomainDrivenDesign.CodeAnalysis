using System.Collections.Generic;
using Eshava.DomainDrivenDesign.CodeAnalysis.Enums;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure
{
	public class InfrastructureModelPropertyCodeSnippet
	{
		public InfrastructureModelPropertyCodeSnippet()
		{
			Exceptions = [];
		}

		public string CodeSnippeKey
		{
			get
			{
				return ModelName.IsNullOrEmpty()
					? PropertyName
					: $"{ModelName}.{PropertyName}";
			}
		}

		public string ModelName { get; set; }
		public string PropertyName { get; set; }
		public ExpressionSyntax Expression { get; set; }
		public OperationType Operation { get; set; } = OperationType.Equal;

		public bool IsMapping { get; set; }
		public bool IsFilter { get; set; }

		public List<InfrastructureExceptionCodeSnippet> Exceptions { get; set; }
	}
}