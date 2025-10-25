using System.Collections.Generic;
using System.Linq;
using Eshava.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Constants;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application;
using Microsoft.CodeAnalysis.CSharp;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Templates.Application
{
	public static class ServiceCollectionExtensionTemplate
	{
		public static string GetServiceCollection(ApplicationProject project, List<DependencyInjection> dependencyInjections)
		{
			var unitInformation = new UnitInformation("ServiceCollectionExtensions", $"{project.FullQualifiedNamespace}.Extensions", addConstructor: false, addAssemblyComment: project.AddAssemblyCommentToFiles);

			unitInformation.AddUsing(CommonNames.Namespaces.DEPENDENCYINJECTION);
			unitInformation.AddClassModifier(SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword, SyntaxKind.PartialKeyword);

			foreach (var @using in dependencyInjections.SelectMany(di => di.GetUsings()).ToList())
			{
				unitInformation.AddUsing(@using);
			}

			unitInformation.AddMethod(TemplateMethods.CreateRegisterMethod("AddGeneratedUseCases", dependencyInjections));

			return unitInformation.CreateCodeString();
		}
	}
}