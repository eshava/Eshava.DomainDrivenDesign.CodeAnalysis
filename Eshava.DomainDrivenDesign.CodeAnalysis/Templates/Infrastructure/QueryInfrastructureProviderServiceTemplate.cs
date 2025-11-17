using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Eshava.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Constants;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Templates.Infrastructure
{
	public static class QueryInfrastructureProviderServiceTemplate
	{
		public static string GetProviderService(
			InfrastructureProject project,
			InfrastructureModel model,
			QueryProviderMap queryProviderMap,
			string fullQualifiedDomainNamespace,
			string fullQualifiedApplicationNamespace,
			bool addAssemblyCommentToFiles
		)
		{
			var @namespace = $"{fullQualifiedDomainNamespace}.{model.ClassificationKey.ToPlural()}";
			var className = $"{model.ClassificationKey}QueryInfrastructureProviderService";
			var featureNameNamespace = queryProviderMap.FeatureName.IsNullOrEmpty() ? "" : $"{queryProviderMap.FeatureName}.";

			var unitInformation = new UnitInformation(className, @namespace, addAssemblyComment: addAssemblyCommentToFiles);
			unitInformation.AddClassModifier(SyntaxKind.InternalKeyword, SyntaxKind.PartialKeyword);
			unitInformation.AddContructorModifier(SyntaxKind.PublicKeyword);

			unitInformation.AddUsing(CommonNames.Namespaces.TASKS);
			unitInformation.AddUsing(CommonNames.Namespaces.GENERIC);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.EXTENSIONS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.MODELS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Application.DTOS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Domain.EXTENSIONS);

			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Infrastructure.INTERFACES);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Infrastructure.PROVIDERS);
			unitInformation.AddUsing($"{fullQualifiedApplicationNamespace}.{featureNameNamespace}{model.ClassificationKey.ToPlural()}.Queries");

			var alternativeClass = project.AlternativeClasses.FirstOrDefault(ac => ac.Type == InfrastructureAlternativeClassType.QueryProviderService);

			var providerServiceBaseTypes = new List<SimpleBaseTypeSyntax>();
			if (alternativeClass is not null)
			{
				unitInformation.AddUsing(alternativeClass.Using);
				providerServiceBaseTypes.Add(alternativeClass.ClassName.ToType().ToSimpleBaseType());
			}

			providerServiceBaseTypes.Add($"I{className}".ToType().ToSimpleBaseType());
			unitInformation.AddBaseType(providerServiceBaseTypes.ToArray());

			unitInformation.AddConstructorParameter($"{model.ClassificationKey.ToVariableName()}QueryRepository", $"I{model.ClassificationKey}QueryRepository".ToIdentifierName());

			CheckAndAddProviderReferences(unitInformation, model, alternativeClass);

			foreach (var methodMap in queryProviderMap.Methods)
			{
				(var usings, var method) = CreateMethod(model, methodMap);
				usings.ForEach(unitInformation.AddUsing);
				unitInformation.AddMethod(method);
			}

			return unitInformation.CreateCodeString();
		}

		private static void CheckAndAddProviderReferences(UnitInformation unitInformation, InfrastructureModel model, InfrastructureProjectAlternativeClass alternativeClass)
		{
			if (alternativeClass?.ConstructorParameters?.Any() ?? false)
			{
				foreach (var constructorParameter in alternativeClass.ConstructorParameters)
				{
					unitInformation.AddUsing(constructorParameter.UsingForType);
					unitInformation.AddConstructorParameter(constructorParameter.Name, constructorParameter.Type.ToIdentifierName(), Enums.ParameterTargetTypes.Argument);
				}
			}

			foreach (var constructorParameter in model.QueryProviderServiceConstructorParameters)
			{
				unitInformation.AddUsing(constructorParameter.UsingForType);
				unitInformation.AddConstructorParameter(constructorParameter.Name, constructorParameter.Type.ToIdentifierName());
			}
		}

		private static (List<string> Usings, (string Name, MethodDeclarationSyntax Method) Method) CreateMethod(InfrastructureModel model, UseCaseQueryProviderMethodMap methodMap)
		{
			var statements = new List<StatementSyntax>();

			var typeUsings = methodMap.ParameterTypes
					.SelectMany(pt => pt.CollectUsings())
					.Concat(methodMap.ReturnType.CollectUsings())
					.Where(@using => !@using.IsNullOrEmpty())
					.Distinct()
					.ToList();

			var parameter = methodMap.ParameterTypes
				.Select(parameterType => parameterType.Name.ToParameter().WithType(parameterType.GetParameterType()))
				.ToArray();


			var call = StatementHelpers.GetMethodCall($"{model.ClassificationKey.ToVariableName().ToFieldName()}QueryRepository".ToIdentifierName(), methodMap.Name, methodMap.ParameterTypes.Select(p => p.Name.ToIdentifierName()).ToArray());

			statements.Add(call.Return());

			var methodDeclaration = methodMap.Name
				.ToMethod(
					methodMap.ReturnType.GetReturnParameterType(),
					statements,
					SyntaxKind.PublicKeyword
				)
				.WithParameter(parameter);

			return (typeUsings, (methodMap.Name, methodDeclaration));
		}
	}
}
