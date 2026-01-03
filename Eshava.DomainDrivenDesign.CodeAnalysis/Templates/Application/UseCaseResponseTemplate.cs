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
	public static class UseCaseResponseTemplate
	{
		public static string GetResponse(
			ApplicationUseCase useCase, 
			string domain, 
			string useCaseNamespace, 
			ReferenceMap domainModelReferenceMap,
			DtoReferenceMap dtoReferenceMap,
			bool addAssemblyCommentToFiles
		)
		{
			var responseName = useCase.ResponseType;

			var unitInformation = new UnitInformation(responseName, useCaseNamespace, addConstructor: false, addAssemblyComment: addAssemblyCommentToFiles);
			unitInformation.AddClassModifier(SyntaxKind.PublicKeyword, SyntaxKind.PartialKeyword);

			unitInformation.AddUsing(CommonNames.Namespaces.SYSTEM);
			unitInformation.AddUsing(CommonNames.Namespaces.GENERIC);

			switch (useCase.Type)
			{
				case ApplicationUseCaseType.Read:

					AddProperty(unitInformation, useCase.ClassificationKey, (useCase.MainDto ?? useCase.Dtos.First().Name).ToType());

					break;
				case ApplicationUseCaseType.Search:

					AddProperty(unitInformation, useCase.ClassificationKey.ToPlural(), "IEnumerable".AsGeneric(useCase.MainDto ?? useCase.Dtos.First().Name));
					AddProperty(unitInformation, "Total", Eshava.CodeAnalysis.SyntaxConstants.Int);

					break;

				case ApplicationUseCaseType.SearchCount:

					AddProperty(unitInformation, "Total", Eshava.CodeAnalysis.SyntaxConstants.Int);

					break;
				case ApplicationUseCaseType.Create:
					var referenceDomainModelName = useCase.GetDomainModelReferenceName();
					if (!domainModelReferenceMap.TryGetDomainModel(domain, referenceDomainModelName, out var domainModelMap))
					{
						throw new System.ArgumentException("DomainModel not found", $"{domain}.{referenceDomainModelName}");
					}

					AddProperty(unitInformation, "Id", domainModelMap.IdentifierType.ToType());

					break;
				case ApplicationUseCaseType.Unique:

					AddProperty(unitInformation, "Unique", Eshava.CodeAnalysis.SyntaxConstants.Bool);
					AddProperty(unitInformation, useCase.ClassificationKey.ToPlural(), "IEnumerable".AsGeneric(useCase.MainDto ?? useCase.Dtos.First().Name));

					break;
				case ApplicationUseCaseType.Suggestions:

					dtoReferenceMap.TryGetDto(domain, useCase.UseCaseName, useCase.ClassificationKey, useCase.MainDto ?? useCase.Dtos.First().Name, out var suggestionDtoMap);
					var suggestionSearchProperty = suggestionDtoMap.Dto.Properties.FirstOrDefault(p => p.IsSearchable == true);

					if (useCase.ReduceResultDtosToSuggestionProperty && suggestionSearchProperty is not null)
					{
						unitInformation.AddUsing(suggestionSearchProperty.UsingForType);
						AddProperty(unitInformation, "Suggestions", "IEnumerable".AsGeneric(suggestionSearchProperty.Type));
					}
					else
					{
						AddProperty(unitInformation, useCase.ClassificationKey.ToPlural(), "IEnumerable".AsGeneric(useCase.MainDto ?? useCase.Dtos.First().Name));
					}

					break;
			}

			return unitInformation.CreateCodeString();
		}

		private static void AddProperty(UnitInformation unitInformation, string propertyName, TypeSyntax type)
		{
			unitInformation.AddProperty(propertyName.ToProperty(type, SyntaxKind.PublicKeyword, true, true), propertyName);
		}
	}
}