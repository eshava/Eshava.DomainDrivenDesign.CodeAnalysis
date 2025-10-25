using System.Collections.Generic;
using System.Linq;
using Eshava.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Constants;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Api;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Templates.Api
{
	public static class WebApplicationExtensionTemplate
	{
		public static string GetWebApplicationExtension(ApiProject project, List<DependencyInjection> dependencyInjections)
		{
			var unitInformation = new UnitInformation("WebApplicationExtensions", $"{project.FullQualifiedNamespace}.Extensions", addConstructor: false, addAssemblyComment: project.AddAssemblyCommentToFiles);

			unitInformation.AddUsing(CommonNames.Namespaces.AspNetCore.BUILDER);
			unitInformation.AddClassModifier(SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword);

			foreach (var @using in dependencyInjections.SelectMany(di => di.GetUsings()).ToList())
			{
				unitInformation.AddUsing(@using);
			}

			unitInformation.AddMethod(GetMapMethod(dependencyInjections));

			return unitInformation.CreateCodeString();
		}

		private static (string Name, MethodDeclarationSyntax) GetMapMethod(List<DependencyInjection> dependencyInjections)
		{
			var statements = new List<StatementSyntax>();
			var app = "app";

			foreach (var dependency in dependencyInjections)
			{
				statements.Add(
					dependency.Class
					.Access("Map")
					.Call(app.ToArgument())
					.ToExpressionStatement());
			}

			statements.Add(
				app
				.ToIdentifierName()
				.Return()
			);

			var methodDeclarationName = "MapRoutes";
			var methodDeclaration = methodDeclarationName.ToMethod(
				"WebApplication".ToIdentifierName(),
				statements,
				SyntaxKind.PublicKeyword,
				SyntaxKind.StaticKeyword
			);

			return (
				methodDeclarationName,
				methodDeclaration
				.WithParameter(
					app
					.ToParameter()
					.WithType("WebApplication".ToType())
					.AddThisModifier()
				)
			);
		}
	}
}