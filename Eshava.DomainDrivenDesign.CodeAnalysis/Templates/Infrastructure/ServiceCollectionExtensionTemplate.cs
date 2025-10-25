using System.Collections.Generic;
using System.Linq;
using Eshava.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Constants;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Templates.Infrastructure
{
	public static class ServiceCollectionExtensionTemplate
	{
		public static string GetServiceCollection(
			InfrastructureProject project,
			List<DependencyInjection> dependencyInjections,
			List<DependencyInjection> dependencyInjectionsDbConfigurations,
			List<DependencyInjection> dependencyInjectionsTransformationProfiles
		)
		{
			var unitInformation = new UnitInformation("ServiceCollectionExtensions", $"{project.FullQualifiedNamespace}.Extensions", addConstructor: false, addAssemblyComment: project.AddAssemblyCommentToFiles);

			unitInformation.AddUsing(CommonNames.Namespaces.DEPENDENCYINJECTION);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Storm.MetaData.NAME);
			unitInformation.AddClassModifier(SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword, SyntaxKind.PartialKeyword);

			foreach (var @using in dependencyInjections.SelectMany(di => di.GetUsings()).ToList())
			{
				unitInformation.AddUsing(@using);
			}

			foreach (var @using in dependencyInjectionsDbConfigurations.SelectMany(di => di.GetUsings()).ToList())
			{
				unitInformation.AddUsing(@using);
			}

			foreach (var @using in dependencyInjectionsTransformationProfiles.SelectMany(di => di.GetUsings()).ToList())
			{
				unitInformation.AddUsing(@using);
			}

			unitInformation.AddMethod(TemplateMethods.CreateRegisterMethod("AddGeneratedProviderAndRepositories", dependencyInjections));
			unitInformation.AddMethod(CreateRegisterDbConfigurationsMethod(dependencyInjectionsDbConfigurations));
			unitInformation.AddMethod(CreateRegisterTransformationProfilesMethod(dependencyInjectionsTransformationProfiles));

			return unitInformation.CreateCodeString();
		}

		private static (string Name, MemberDeclarationSyntax Method) CreateRegisterDbConfigurationsMethod(List<DependencyInjection> dependencyInjections)
		{
			var statements = new List<StatementSyntax>();
			foreach (var dependencyInjection in dependencyInjections)
			{
				statements.Add(
					"TypeAnalyzer"
					.Access("AddType")
					.Call(dependencyInjection.Class.ToIdentifierName().ToInstance().ToArgument())
					.ToExpressionStatement()
				);
			}

			statements.Add(
				"services"
				.ToIdentifierName()
				.Return()
			);

			var methodDeclarationName = "AddGeneratedDbConfigurations";
			var methodDeclaration = methodDeclarationName.ToMethod(
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

			return (methodDeclarationName, methodDeclaration);
		}

		private static (string Name, MemberDeclarationSyntax Method) CreateRegisterTransformationProfilesMethod(List<DependencyInjection> dependencyInjections)
		{
			var statements = new List<StatementSyntax>();
			foreach (var dependencyInjection in dependencyInjections)
			{
				statements.Add(
					dependencyInjection.Class
					.ToIdentifierName()
					.ToInstance()
					.ToExpressionStatement()
				);
			}

			var methodDeclarationName = "AddGeneratedTransformationProfiles";

			var methodDeclaration = methodDeclarationName.ToMethod(
				Eshava.CodeAnalysis.SyntaxConstants.Void,
				statements,
				SyntaxKind.PrivateKeyword,
				SyntaxKind.StaticKeyword
			);

			return (methodDeclarationName, methodDeclaration);
		}
	}
}