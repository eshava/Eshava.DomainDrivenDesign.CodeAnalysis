using System.Linq;
using Eshava.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Constants;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Templates.Application
{
	public static class QueryInfrastructureProviderServiceInterfaceTemplate
	{
		public static string GetInterface(QueryProviderMap queryProvider, string applicationDomainModelNamespace, bool addAssemblyCommentToFiles)
		{
			var unitInformation = new UnitInformation($"I{queryProvider.ClassificationKey}QueryInfrastructureProviderService", applicationDomainModelNamespace, isInterface: true, addAssemblyComment: addAssemblyCommentToFiles);
			unitInformation.AddClassModifier(SyntaxKind.PublicKeyword, SyntaxKind.PartialKeyword);

			unitInformation.AddUsing(CommonNames.Namespaces.GENERIC);
			unitInformation.AddUsing(CommonNames.Namespaces.TASKS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.MODELS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Application.DTOS);

			foreach (var method in queryProvider.Methods)
			{
				var typeUsings = method.ParameterTypes
					.SelectMany(pt => pt.CollectUsings())
					.Concat(method.ReturnType.CollectUsings())
					.Where(@using => !@using.IsNullOrEmpty())
					.Distinct()
					.ToList();

				typeUsings.ForEach(unitInformation.AddUsing);

				var parameter = method.ParameterTypes
					.Select(parameterType => parameterType.Name.ToParameter().WithType(parameterType.GetParameterType()))
					.ToArray();

				var methodDeclaration = method.Name
					.ToMethodDefinition(
						method.ReturnType.GetReturnParameterType(),
						null
					)
					.WithParameter(parameter)
					.AddSemicolon();

				unitInformation.AddMethod((method.Name, methodDeclaration));
			}

			return unitInformation.CreateCodeString();
		}
	}
}