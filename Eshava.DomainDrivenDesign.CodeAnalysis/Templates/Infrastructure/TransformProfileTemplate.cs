using System.Collections.Generic;
using System.Linq;
using Eshava.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Constants;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application;
using Microsoft.CodeAnalysis.CSharp;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Templates.Infrastructure
{
	public static class TransformProfileTemplate
	{
		private static readonly HashSet<ApplicationUseCaseType> _allowedUseCases =
		[
			ApplicationUseCaseType.Search,
			ApplicationUseCaseType.Suggestions,
			ApplicationUseCaseType.Unique
		];

		public static string GetTransformProfile(
			string infrastructureProjectNamespace,
			string applicationProjectNamespace,
			string domain,
			IEnumerable<ApplicationUseCase> useCases,
			bool addAssemblyCommentToFiles
		)
		{
			var @namespace = $"{infrastructureProjectNamespace}.{domain}";
			var classDeclaration = $"{domain}AutoGenTransformProfile";
			var baseType = "TransformProfile".ToSimpleBaseType();


			var unitInformation = new UnitInformation(classDeclaration, @namespace, addAssemblyComment: addAssemblyCommentToFiles);
			unitInformation.AddClassModifier(SyntaxKind.InternalKeyword);
			unitInformation.AddBaseType(baseType);
			unitInformation.AddContructorModifier(SyntaxKind.PublicKeyword);

			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.Linq.NAME);

			foreach (var useCase in useCases)
			{
				if (!_allowedUseCases.Contains(useCase.Type))
				{
					continue;
				}

				var useCaseDto = useCase.Dtos.First(dto => dto.Name == useCase.MainDto);
				var dataModel = useCaseDto.ReferenceModelName;

				var properties = new List<ApplicationUseCaseDtoProperty>();
				foreach (var property in useCaseDto.Properties)
				{
					if (
						(useCase.Type == ApplicationUseCaseType.Search && !(property.IsSearchable ?? false) && !(property.IsSortable ?? false))
						|| property.ReferenceProperty.IsNullOrEmpty()
					)
					{
						continue;
					}

					properties.Add(property);
				}


				if (properties.Count == 0)
				{
					continue;
				}

				var useCaseFeatureName = useCase.FeatureName;
				if (!useCaseFeatureName.IsNullOrEmpty())
				{
					useCaseFeatureName += ".";
				}

				unitInformation.AddUsing($"{applicationProjectNamespace}.{domain}.{useCaseFeatureName}{useCase.ClassificationKey.ToPlural()}.Queries.{useCase.UseCaseName}");

				var map = "CreateMap".AsGeneric(useCaseDto.Name, $"{useCase.ClassificationKey.ToPlural()}.{useCaseDto.ReferenceModelName}").Call();

				foreach (var property in properties)
				{
					map = map
						.Access("ForPath")
						.Call(
							"s".ToPropertyExpression(property.Name).ToArgument(),
							"t".ToPropertyExpression(property.ReferenceProperty).ToArgument()
						);
				}

				unitInformation.AddConstructorBodyStatement(map.ToExpressionStatement());
			}

			if (!unitInformation.ConstructorBodyStatements.Any())
			{
				return null;
			}

			return unitInformation.CreateCodeString();
		}
	}
}
