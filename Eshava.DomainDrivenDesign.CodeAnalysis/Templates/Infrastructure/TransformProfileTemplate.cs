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
			Dictionary<string, Dictionary<string, string>> infrastructureModels,
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

			if (!infrastructureModels.TryGetValue(domain, out var dataModels))
			{
				dataModels = [];
			}

			foreach (var useCase in useCases)
			{
				if (!_allowedUseCases.Contains(useCase.Type))
				{
					continue;
				}

				var useCaseDtos = useCase.Dtos.ToDictionary(dto => dto.Name, dto => dto);
				var useCaseDto = useCaseDtos[useCase.MainDto];
				var dataModel = useCaseDto.ReferenceModelName;

				var properties = new List<(string DtoPropertyName, string DataPropertyName)>();
				CollectProperties(useCase.Type, useCaseDto, null, null, useCaseDtos, dataModels, properties);

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
							"s".ToPropertyExpression(property.DtoPropertyName).ToArgument(),
							"t".ToPropertyExpression(property.DataPropertyName).ToArgument()
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

		private static void CollectProperties(
			ApplicationUseCaseType useCaseType,
			ApplicationUseCaseDto useCaseDto,
			string parentDtoPropertyName,
			string parentDataPropertyName,
			Dictionary<string, ApplicationUseCaseDto> useCaseDtos,
			Dictionary<string, string> dataModels,
			List<(string DtoPropertyName, string DataPropertyName)> propertiesForProfile
		)
		{
			foreach (var property in useCaseDto.Properties)
			{
				if (
					(useCaseType == ApplicationUseCaseType.Search && !(property.IsSearchable ?? false) && !(property.IsSortable ?? false))
					|| (useCaseType != ApplicationUseCaseType.Search && property.ReferenceProperty.IsNullOrEmpty())
				)
				{
					continue;
				}

				if (!property.ReferenceProperty.IsNullOrEmpty())
				{
					var dataName = parentDataPropertyName.IsNullOrEmpty() || property.ReferenceProperty.Contains(".")
						? property.ReferenceProperty
						: $"{parentDataPropertyName}.{property.ReferenceProperty}"
						;

					if (parentDtoPropertyName.IsNullOrEmpty())
					{
						propertiesForProfile.Add((property.Name, dataName));
					}
					else
					{
						propertiesForProfile.Add(($"{parentDtoPropertyName}.{property.Name}", dataName));
					}

					continue;
				}

				if (property.IsEnumerable)
				{
					continue;
				}

				if (!useCaseDtos.TryGetValue(property.Type, out var typeDto))
				{
					var dataName = parentDataPropertyName.IsNullOrEmpty()
						? property.Name
						: $"{parentDataPropertyName}.{property.Name}"
						;

					if (!parentDtoPropertyName.IsNullOrEmpty())
					{
						propertiesForProfile.Add(($"{parentDtoPropertyName}.{property.Name}", dataName));
					}

					continue;
				}


				if (!dataModels.TryGetValue(typeDto.ReferenceModelName, out var classificationKey))
				{
					continue;
				}

				var tempParentDtoPropertyName = parentDtoPropertyName.IsNullOrEmpty()
					? property.Name
					: $"{parentDtoPropertyName}.{property.Name}"
					;

				var tempParentDataPropertyName = parentDataPropertyName.IsNullOrEmpty()
						? classificationKey
						: $"{parentDataPropertyName}.{classificationKey}"
						;

				if (typeDto.ReferenceModelName == useCaseDto.ReferenceModelName)
				{
					tempParentDataPropertyName = null;
				}

				CollectProperties(useCaseType, typeDto, tempParentDtoPropertyName, tempParentDataPropertyName, useCaseDtos, dataModels, propertiesForProfile);
			}
		}
	}
}