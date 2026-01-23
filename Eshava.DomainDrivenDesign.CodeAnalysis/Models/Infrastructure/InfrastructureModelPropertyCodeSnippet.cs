using System;
using System.Collections.Generic;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure
{
	public class InfrastructureModelPropertyCodeSnippet
	{
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
	}
}