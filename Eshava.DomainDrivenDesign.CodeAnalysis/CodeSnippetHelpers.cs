using System.Collections.Generic;
using System.Linq;
using Eshava.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application;
using Eshava.DomainDrivenDesign.CodeAnalysis.Templates;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Eshava.DomainDrivenDesign.CodeAnalysis
{
	public static class CodeSnippetHelpers
	{
		public static void AddRequestProperties(UnitInformation unitInformation, List<UseCaseCodeSnippet> codeSnippets)
		{
			foreach (var codeSnippet in codeSnippets)
			{
				foreach (var @using in codeSnippet.AdditionalUsings)
				{
					unitInformation.AddUsing(@using);
				}

				foreach (var property in codeSnippet.RequestProperties)
				{
					unitInformation.AddUsing(property.Using);

					var attributes = (property.Attributes?.Any() ?? false)
						? AttributeTemplate.CreateAttributes(property.Attributes)
						: [];
					unitInformation.AddProperty(property.Name.ToProperty(property.Type.ToType(), SyntaxKind.PublicKeyword, true, true, attributes: attributes), property.Name);
				}
			}
		}

		public static void AddConstructorParameters(UnitInformation unitInformation, List<UseCaseCodeSnippet> codeSnippets)
		{
			foreach (var codeSnippet in codeSnippets)
			{
				foreach (var @using in codeSnippet.AdditionalUsings)
				{
					unitInformation.AddUsing(@using);
				}

				foreach (var parameter in codeSnippet.ConstructorParameters)
				{
					unitInformation.AddUsing(parameter.Using);

					unitInformation.AddConstructorParameter(parameter.Name, parameter.Type);
				}
			}
		}

		public static void AddStatements(List<StatementSyntax> statements, string responseType, List<UseCaseCodeSnippet> codeSnippets)
		{
			foreach (var codeSnippet in codeSnippets)
			{
				foreach (var codeSnippetStatement in codeSnippet.Statements)
				{
					if (codeSnippetStatement.CreateFaultyCheck)
					{
						statements.Add(
							codeSnippetStatement.VariableToCheck
							.ToFaultyCheck(responseType.ToType())
						);

						continue;
					}

					if (codeSnippetStatement.CreateExpressionCheck)
					{
						statements.Add(
							codeSnippetStatement.Expression.If(
								GetReturnResponseTypeStatement(responseType)
							)
						);

						continue;
					}

					if (codeSnippetStatement.ReturnResponseInstance)
					{
						statements.Add(
							GetReturnResponseTypeStatement(responseType)
						);

						continue;
					}

					statements.Add(codeSnippetStatement.Statement);
				}
			}
		}

		private static StatementSyntax GetReturnResponseTypeStatement(string responseType)
		{
			return responseType
				.ToIdentifierName()
				.ToInstance()
				.Access(Constants.CommonNames.Extensions.TORESPONSEDATA)
				.Call()
				.Return();
		}
	}
}