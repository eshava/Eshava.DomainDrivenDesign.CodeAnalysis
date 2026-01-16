using System.Collections.Generic;
using System.Linq;
using Eshava.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Constants;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Templates.Application
{
	public static class UseCaseRequestTemplate
	{
		public static string GetRequest(
			ApplicationUseCase useCase, 
			string domain, 
			string useCaseNamespace, 
			ReferenceMap domainModelReferenceMap, 
			DtoReferenceMap dtoReferenceMap, 
			List<UseCaseCodeSnippet> codeSnippets, 
			bool addAssemblyCommentToFiles
		)
		{
			var requestName = useCase.RequestType;
			domainModelReferenceMap.TryGetDomainModel(domain, useCase.GetDomainModelReferenceName(), out var domainModel);

			var unitInformation = new UnitInformation(requestName, useCaseNamespace, addConstructor: false, addAssemblyComment: addAssemblyCommentToFiles);
			unitInformation.AddClassModifier(SyntaxKind.PublicKeyword, SyntaxKind.PartialKeyword);

			unitInformation.AddUsing(CommonNames.Namespaces.SYSTEM);
			unitInformation.AddUsing(CommonNames.Namespaces.GENERIC);

			var attributes = AttributeTemplate.CreateAttributes([
				new AttributeDefinition
				{
					Name = $"{CommonNames.Namespaces.NEWTONSOFT}.JsonIgnore"
				},
				new AttributeDefinition
				{
					Name = $"{CommonNames.Namespaces.JSON}.JsonIgnore"
				}
			]);

			switch (useCase.Type)
			{
				case ApplicationUseCaseType.Read:

					if (!useCase.ReadAggregateByChildId && domainModel is not null)
					{
						AddAggregateProperties(unitInformation, domainModel, attributes, false);
					}

					dtoReferenceMap.TryGetDto(domain, useCase.UseCaseName, useCase.ClassificationKey, useCase.MainDto ?? useCase.Dtos.First().Name, out var readDtoMap);
					AddProperty(unitInformation, $"{useCase.ClassificationKey}Id", null, readDtoMap.DataModel.IdentifierType.ToType(), attributes);

					break;
				case ApplicationUseCaseType.Delete:

					if (!useCase.ReadAggregateByChildId)
					{
						AddAggregateProperties(unitInformation, domainModel, attributes, false);
					}

					AddProperty(unitInformation, $"{domainModel.ClassificationKey}Id", null, domainModel.IdentifierType.ToType(), attributes);

					break;
				case ApplicationUseCaseType.Search:
				case ApplicationUseCaseType.SearchCount:

					var filterDtoName = $"{useCase.ClassificationKey}{useCase.UseCaseName}FilterDto";
					AddProperty(unitInformation, "Filter", null, filterDtoName.ToType());

					break;
				case ApplicationUseCaseType.Create:

					AddAggregateProperties(unitInformation, domainModel, attributes, useCase.ReadAggregateByChildId);
					AddProperty(unitInformation, domainModel.ClassificationKey, null, useCase.MainDto.ToType());

					break;
				case ApplicationUseCaseType.Update:

					unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Application.PARTIALPUT);

					if (!useCase.ReadAggregateByChildId)
					{
						AddAggregateProperties(unitInformation, domainModel, attributes, false);
					}

					AddProperty(unitInformation, $"{domainModel.ClassificationKey}Id", null, domainModel.IdentifierType.ToType(), attributes);
					AddProperty(unitInformation, domainModel.ClassificationKey, null, "PartialPutDocument".AsGeneric(useCase.MainDto));

					break;
				case ApplicationUseCaseType.Unique:

					var uniqueDto = useCase.Dtos.First();
					foreach (var property in uniqueDto.Properties)
					{
						var propertyType = property.Type;
						if (property.Name == "Id")
						{
							propertyType += "?";
						}

						AddProperty(unitInformation, property.Name, property.UsingForType, propertyType.ToType());
					}

					AddProperty(unitInformation, "AddMatches", null, Eshava.CodeAnalysis.SyntaxConstants.Bool);

					break;
				case ApplicationUseCaseType.Suggestions:

					unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.Linq.ENUMS);

					AddProperty(unitInformation, "SearchTerm", null, Eshava.CodeAnalysis.SyntaxConstants.String);
					AddProperty(unitInformation, "SearchOperation", null, "CompareOperator".ToType());
					AddProperty(unitInformation, "SortOrder", null, "SortOrder".ToType());

					dtoReferenceMap.TryGetDto(domain, useCase.UseCaseName, useCase.ClassificationKey, useCase.MainDto ?? useCase.Dtos.First().Name, out var suggestionDtoMap);
					foreach (var property in suggestionDtoMap.Dto.Properties.Where(p => p.AddToRequest ?? false))
					{
						var propertyType = property.IsNullableType || property.Type == "string"
							? property.Type
							: $"{property.Type}?";
						AddProperty(unitInformation, property.Name, property.UsingForType, propertyType.ToType(), attributes);
					}

					break;
			}

			CodeSnippetHelpers.AddRequestProperties(unitInformation, codeSnippets);

			return unitInformation.CreateCodeString();
		}

		private static void AddProperty(UnitInformation unitInformation, string propertyName, string usingForType, TypeSyntax type, IEnumerable<AttributeSyntax> attributes = null)
		{
			if (!usingForType.IsNullOrEmpty())
			{
				unitInformation.AddUsing(usingForType);
			}

			unitInformation.AddProperty(propertyName.ToProperty(type, SyntaxKind.PublicKeyword, true, true, attributes: attributes), propertyName);
		}

		private static void AddAggregateProperties(UnitInformation unitInformation, ReferenceDomainModelMap domainModelMap, List<AttributeSyntax> attributes, bool onlyFirstAggregate)
		{
			if (!domainModelMap.IsChildDomainModel)
			{
				return;
			}

			AddProperty(unitInformation, $"{domainModelMap.AggregateDomainModel.ClassificationKey}Id", null, domainModelMap.AggregateDomainModel.IdentifierType.ToType(), attributes);

			if (!onlyFirstAggregate)
			{
				AddAggregateProperties(unitInformation, domainModelMap.AggregateDomainModel, attributes, false);
			}
		}
	}
}