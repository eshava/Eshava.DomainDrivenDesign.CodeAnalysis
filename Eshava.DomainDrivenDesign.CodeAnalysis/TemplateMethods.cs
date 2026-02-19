using System.Collections.Generic;
using Eshava.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Eshava.DomainDrivenDesign.CodeAnalysis
{
	public static class TemplateMethods
	{
		public static (string Name, MemberDeclarationSyntax) CreateRegisterMethod(string methodName, List<DependencyInjection> dependencyInjections)
		{
			var statements = new List<StatementSyntax>();
			StatementHelpers.AddScoped(statements, dependencyInjections);

			statements.Add(
				"services"
				.ToIdentifierName()
				.Return()
			);

			var methodDeclaration = methodName.ToMethod(
				"IServiceCollection".ToIdentifierName(),
				statements,
				SyntaxKind.PrivateKeyword,
				SyntaxKind.StaticKeyword
			);

			methodDeclaration = methodDeclaration
				.WithParameter(
					"services"
					.ToParameter()
					.WithType("IServiceCollection".ToType())
				);

			return (methodName, methodDeclaration);
		}		

		public static string GetDomain(string referenceDomain, string defaultDomain)
		{
			return referenceDomain.IsNullOrEmpty()
				? defaultDomain
				: referenceDomain;
		}		
	}
}