using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http.Headers;
using Eshava.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Constants;
using Eshava.DomainDrivenDesign.CodeAnalysis.Enums;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Templates.Application
{
	public static class CreateUseCaseTemplate
	{
		public static string GetUseCase(UseCaseTemplateRequest request, List<UseCaseCodeSnippet> codeSnippets)
		{
			var referenceDomainModelName = request.UseCase.GetDomainModelReferenceName();

			if (!request.DomainModelReferenceMap.TryGetDomainModel(request.Domain, referenceDomainModelName, out var domainModelMap))
			{
				throw new System.ArgumentException("DomainModel not found", $"{request.Domain}.{referenceDomainModelName}");
			}

			var className = request.UseCase.ClassName;
			var relevantDomainModelNames = request.UseCase.Dtos.Select(dto => dto.ReferenceModelName).Distinct().ToImmutableHashSet();
			var provider = domainModelMap.GetProvider(request.ApplicationProjectNamespace, request.UseCase.FeatureName, request.UseCasesMap.GetFeatureName);
			var queryProviders = domainModelMap.GetQueryProviders(relevantDomainModelNames, request.ApplicationProjectNamespace, request.UseCase.FeatureName, request.UseCasesMap.GetFeatureName).ToList();
			if (request.UseCase.ReadAggregateByChildId && domainModelMap.IsChildDomainModel)
			{
				queryProviders.Add(domainModelMap.AggregateDomainModel.GetQueryProvider(request.ApplicationProjectNamespace, request.UseCase.FeatureName, request.UseCasesMap.GetFeatureName));
			}

			var domainModelTypeName = domainModelMap.GetDomainModelTypeName(request.DomainProjectNamespace);
			var alternativeClass = request.AlternativeClasses.FirstOrDefault(ac => ac.Type == ApplicationUseCaseType.Create);

			var baseType = alternativeClass is null
				? CommonNames.Application.Abstracts.CREATEUSECASE.AsGeneric(request.UseCase.MainDto, domainModelTypeName, domainModelMap.IdentifierType).ToSimpleBaseType()
				: alternativeClass.ClassName.AsGeneric(request.UseCase.MainDto, domainModelTypeName, domainModelMap.IdentifierType).ToSimpleBaseType();
			var useCaseInterface = $"I{className}".ToType().ToSimpleBaseType();

			var unitInformation = new UnitInformation(className, request.UseCaseNamespace, addAssemblyComment: request.AddAssemblyCommentToFiles);
			unitInformation.AddClassModifier(SyntaxKind.InternalKeyword, SyntaxKind.PartialKeyword);
			unitInformation.AddContructorModifier(SyntaxKind.PublicKeyword);
			unitInformation.AddBaseType(baseType, useCaseInterface);
			unitInformation.AddUsing(alternativeClass?.Using);

			if ((request.UseCase.Attributes?.Count ?? 0) > 0)
			{
				foreach (var attribute in request.UseCase.Attributes)
				{
					unitInformation.AddUsing(attribute.UsingForType);
				}

				unitInformation.AddAttributes(AttributeTemplate.CreateAttributes(request.UseCase.Attributes));
			}

			unitInformation.AddUsing(CommonNames.Namespaces.SYSTEM);
			unitInformation.AddUsing(CommonNames.Namespaces.SYSTEMNET);
			unitInformation.AddUsing(CommonNames.Namespaces.GENERIC);
			unitInformation.AddUsing(CommonNames.Namespaces.LINQ);
			unitInformation.AddUsing(CommonNames.Namespaces.TASKS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.EXTENSIONS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.MODELS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Application.USECAES);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Domain.CONSTANTS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Domain.EXTENSIONS);

			var scopedSettingsTargetType = ParameterTargetTypes.Field;
			if (alternativeClass?.ConstructorParameters?.Any(cp => cp.Type == request.ScopedSettingsClass) ?? false)
			{
				scopedSettingsTargetType |= ParameterTargetTypes.Argument;
			}

			unitInformation.AddScopedSettings(request.ScopedSettingsUsing, request.ScopedSettingsClass, scopedSettingsTargetType);
			unitInformation.AddValidationEngine();

			TemplateMethods.AddDomainModelUsings(unitInformation, domainModelMap, request.DomainProjectNamespace, request.Domain);

			if (request.UseCase.AddValidationConfigurationMethod)
			{
				unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Application.DTOS);
			}

			unitInformation.AddValidationRuleEngine(request.UseCase.AddValidationConfigurationMethod);
			unitInformation.AddUsing(provider.Using);
			unitInformation.AddConstructorParameter(provider.Name, provider.Type);

			var foreignKeyReferenceContainer = TemplateMethods.CollectForeignKeyReferenceTypes(request.DomainProjectNamespace, domainModelMap);
			var domainModelWithMappings = CheckForPropertyMappings(unitInformation, request.Domain, request.UseCase.Dtos, request.DomainModelReferenceMap);

			unitInformation.AddMethod(
				TemplateMethods.CreateUseCaseMainMethod(
					request.UseCase,
					request.Domain,
					domainModelMap,
					request.DtoReferenceMap,
					request.DomainProjectNamespace,
					foreignKeyReferenceContainer,
					domainModelWithMappings,
					codeSnippets,
					CreateCreateMethodActions
				)
			);

			if (request.UseCase.AddValidationConfigurationMethod)
			{
				unitInformation.AddMethod(TemplateMethods.CreateValidationConfigurationMethod(request.UseCase));
			}

			var childCreateMethodsResult = TemplateMethods.CreateCreateChildsMethods(request, domainModelMap, foreignKeyReferenceContainer, domainModelWithMappings, true, true);
			foreach (var childCreateMethods in childCreateMethodsResult)
			{
				unitInformation.AddMethod(childCreateMethods);
			}

			foreach (var queryProvider in queryProviders)
			{
				unitInformation.AddUsing(queryProvider.Using);
				unitInformation.AddConstructorParameter(queryProvider.Name, queryProvider.Type);
			}

			if (domainModelMap.DomainModel.HasValidationRules && !domainModelMap.IsChildDomainModel)
			{
				unitInformation.AddMethod(TemplateMethods.CreateCheckValidationConstraintsMethod(request.UseCase, request.Domain, domainModelMap, request.UseCase.MainDto.ToType()));
			}

			if (request.UseCase.CheckForeignKeyReferencesAutomatically)
			{
				TemplateMethods.AddReferenceTypes(unitInformation, request.UseCasesMap, foreignKeyReferenceContainer.ForeignKeyHashSets, request.ApplicationProjectNamespace);

				var foreignKeyCheckMethods = TemplateMethods.CreateExistsForeignKeyMethod(foreignKeyReferenceContainer.ForeignKeyHashSets);
				foreach (var foreignKeyCheckMethod in foreignKeyCheckMethods)
				{
					unitInformation.AddMethod(foreignKeyCheckMethod);
				}

				if (!domainModelMap.IsChildDomainModel)
				{
					CreateForeignKeyCheckMethodForDto(
						unitInformation,
						request.UseCase,
						request.Domain,
						domainModelMap,
						request.DtoReferenceMap,
						request.DomainProjectNamespace,
						foreignKeyReferenceContainer
					);
				}
			}

			CheckAndAddProviderReferences(unitInformation, request.UseCase, alternativeClass, codeSnippets);

			unitInformation.AddLogger(className);

			return unitInformation.CreateCodeString();
		}

		private static HashSet<string> CheckForPropertyMappings(UnitInformation unitInformation, string domain, IEnumerable<ApplicationUseCaseDto> useCaseDtos, ReferenceMap domainModelReferenceMap)
		{
			var domainModelWithMappings = new HashSet<string>();

			var dtoDic = useCaseDtos.ToDictionary(dto => dto.Name, dto => dto);

			foreach (var useCaseDto in useCaseDtos)
			{
				if (!domainModelReferenceMap.TryGetDomainModel(domain, useCaseDto.ReferenceModelName, out var domainModel) || domainModel.IsValueObject)
				{
					continue;
				}

				var mappings = new List<(string DtoProperty, string DomainProperty)>();

				foreach (var useCaseDtoProperty in useCaseDto.Properties)
				{
					if (!useCaseDtoProperty.ReferenceProperty.IsNullOrEmpty())
					{
						mappings.Add((useCaseDtoProperty.Name, useCaseDtoProperty.ReferenceProperty));

						continue;
					}

					if (useCaseDtoProperty.IsEnumerable || !dtoDic.TryGetValue(useCaseDtoProperty.Type, out var referenceDto))
					{
						continue;
					}

					if (!domainModelReferenceMap.TryGetDomainModel(domain, referenceDto.ReferenceModelName, out var referenceDomainModel) || !referenceDomainModel.IsValueObject)
					{
						continue;
					}

					foreach (var referenceDtoProperty in referenceDto.Properties)
					{
						var domainProperties = domainModel.DomainModel.Properties.Where(p => p.Type == referenceDto.ReferenceModelName).ToList();
						var domainProperty = domainProperties.Count == 1
							? domainProperties[0]
							: domainProperties.FirstOrDefault(p => p.Name == referenceDtoProperty.Name);

						if (domainProperty is null)
						{
							continue;
						}

						if (!referenceDtoProperty.ReferenceProperty.IsNullOrEmpty())
						{
							var referencePropertyName = referenceDtoProperty.ReferenceProperty;
							if (!referenceDtoProperty.ReferenceProperty.Contains("."))
							{
								referencePropertyName = $"{domainProperty.Name}.{referencePropertyName}";
							}

							mappings.Add(($"{useCaseDtoProperty.Name}.{referenceDtoProperty.Name}", referencePropertyName));

							continue;
						}

						var referenceDomainProperty = referenceDomainModel.DomainModel.Properties.FirstOrDefault(p => p.Name == referenceDtoProperty.Name);
						if (referenceDomainProperty is null)
						{
							continue;
						}

						mappings.Add(($"{useCaseDtoProperty.Name}.{referenceDtoProperty.Name}", $"{domainProperty.Name}.{referenceDomainProperty.Name}"));
					}
				}

				if (mappings.Count > 0)
				{
					var fieldName = $"{useCaseDto.ReferenceModelName.ToFieldName()}Mappings";
					var dtoType = "Dto".ToPropertyExpressionTupleElement(useCaseDto.Name);
					var domainType = "Domain".ToPropertyExpressionTupleElement(useCaseDto.ReferenceModelName);
					var tupleType = dtoType.ToTupleType(domainType);
					var dtoToDomainType = "List".AsGeneric(tupleType);

					var dataToDomainInstance = dtoToDomainType.ToCollectionExpressionWithInitializer(
							mappings
							.Select(p => "dto"
								.ToPropertyExpression(p.DtoProperty)
								.ToArgument()
								.ToTuple("domain"
									.ToPropertyExpression(p.DomainProperty)
									.ToArgument()
								)
							).ToArray()
						);


					var field = fieldName.ToStaticReadonlyField(dtoToDomainType, dataToDomainInstance);

					domainModelWithMappings.Add(useCaseDto.ReferenceModelName);
					unitInformation.AddUsing(CommonNames.Namespaces.EXPRESSION);
					unitInformation.AddField((fieldName, FieldType.Static, field));
				}
			}

			return domainModelWithMappings;
		}

		private static void CheckAndAddProviderReferences(UnitInformation unitInformation, ApplicationUseCase applicationUseCase, ApplicationProjectAlternativeClass alternativeClass, List<UseCaseCodeSnippet> codeSnippets)
		{
			if (alternativeClass?.ConstructorParameters?.Any() ?? false)
			{
				foreach (var additionalContructorParameter in alternativeClass.ConstructorParameters)
				{
					unitInformation.AddUsing(additionalContructorParameter.UsingForType);
					unitInformation.AddConstructorParameter(additionalContructorParameter.Name, additionalContructorParameter.Type, ParameterTargetTypes.Argument);
				}
			}

			foreach (var additionalContructorParameter in applicationUseCase.AdditionalContructorParameter)
			{
				unitInformation.AddUsing(additionalContructorParameter.UsingForType);
				unitInformation.AddConstructorParameter(additionalContructorParameter.Name, additionalContructorParameter.Type);
			}

			CodeSnippetHelpers.AddConstructorParameters(unitInformation, codeSnippets);
		}

		private static List<StatementSyntax> CreateCreateMethodActions(
			ApplicationUseCase useCase,
			string domain,
			ReferenceDomainModelMap domainModelMap,
			DtoReferenceMap dtoReferenceMap,
			string returnDataType,
			string provider,
			string domainModelId,
			string domainProjectNamespace,
			bool hasValidationRules,
			ForeignKeyReferenceContainer foreignKeyReferenceContainer,
			HashSet<string> domainModelWithMappings,
			List<UseCaseCodeSnippet> codeSnippets
		)
		{
			var statements = new List<StatementSyntax>();
			var createResult = $"create{domainModelMap.ClassificationKey}Result";
			var dto = "request".Access(domainModelMap.ClassificationKey);

			CodeSnippetHelpers.AddStatements(statements, returnDataType, codeSnippets);

			if (!dtoReferenceMap.TryGetDtoByDomainModel(domain, useCase.UseCaseName, useCase.NamespaceClassificationKey, domainModelMap.DomainModelName, out var dtoMap))
			{
				throw new System.ArgumentException($"Dto {domainModelMap.DomainModelName} not found");
			}

			StatementHelpers.AddLocalAsyncMethodCallAndFaultyCheck(statements, "ExecuteBefore", null, returnDataType, dto);

			if (hasValidationRules && !domainModelMap.IsChildDomainModel)
			{
				StatementHelpers.AddLocalAsyncMethodCallAndFaultyCheck(statements, "CheckValidationConstraintsAsync", "constraintsResult", returnDataType, dto);
			}

			ExpressionSyntax aggregateReadResult = null;
			if (domainModelMap.IsChildDomainModel)
			{
				(var readStatements, var providerResult) = StatementHelpers.ReadDomainModel(useCase, domainModelMap, provider, returnDataType, true);

				statements.AddRange(readStatements);
				aggregateReadResult = providerResult;

				var dtoForeignKeyReferences = TemplateMethods.CollectForeignKeysAndAddToMethodCall(
					foreignKeyReferenceContainer,
					domainModelMap,
					dtoMap,
					null,
					null,
					false
				);

				var methodArguments = new List<ExpressionSyntax>
				{
					aggregateReadResult,
					"request".Access(domainModelMap.ClassificationKey)
				};

				if (useCase.ReadAggregateByChildId)
				{
					methodArguments.Add("request".Access($"{domainModelMap.AggregateDomainModel.ClassificationKey}Id"));
				}
				else
				{
					var methodArgumentCount = methodArguments.Count;
					var loopDomainModel = domainModelMap.AggregateDomainModel;
					while (loopDomainModel.IsChildDomainModel)
					{
						methodArguments.Insert(methodArgumentCount, "request".Access($"{loopDomainModel.ClassificationKey}Id"));
						loopDomainModel = loopDomainModel.AggregateDomainModel;
					}
				}

				StatementHelpers.AddLocalAsyncMethodCallAndFaultyCheck(
					statements,
					$"Create{domainModelMap.ClassificationKey}Async",
					$"create{domainModelMap.ClassificationKey}Result",
					returnDataType,
					methodArguments.ToArray()
				);
			}
			else
			{
				var domainModelName = domainModelMap.GetDomainModelTypeName(domainProjectNamespace);
				if (useCase.CheckForeignKeyReferencesAutomatically)
				{
					var dtoForeignKeyReferences = TemplateMethods.CollectForeignKeysAndAddToMethodCall(
						foreignKeyReferenceContainer,
						domainModelMap,
						dtoMap,
						null,
						null,
						false
					);

					if (dtoForeignKeyReferences is not null && dtoForeignKeyReferences.Count > 0)
					{
						StatementHelpers.AddLocalAsyncMethodCallAndFaultyCheck(statements, "CheckForeignKeysAsync", "checkForeignKeyhResult", returnDataType, true, "request".Access(useCase.ClassificationKey));
					}
				}

				var methodArguments = new List<ExpressionSyntax>
				{
					dto,
					DomainNames.VALIDATION.ENGINE.ToFieldName().ToIdentifierName()
				};

				if (domainModelWithMappings.Contains(domainModelName))
				{
					methodArguments.Add($"{domainModelName.ToFieldName()}Mappings".ToIdentifierName());
				}

				StatementHelpers.AddMethodCallAndFaultyCheck(statements, domainModelName, "CreateEntity", createResult, returnDataType, methodArguments.ToArray());
			}

			if (domainModelMap.IsAggregate && !domainModelMap.IsChildDomainModel)
			{
				TemplateMethods.AddCreateChildModelsStatements(domainModelMap, dtoMap, statements, returnDataType, createResult, dto);
			}

			var entityToSave = domainModelMap.IsChildDomainModel
				? aggregateReadResult
				: createResult.Access("Data");

			StatementHelpers.AddAsyncMethodCallAndFaultyCheck(statements, provider, "SaveAsync", "saveResult", returnDataType, entityToSave);

			statements.Add(
				domainModelId
				.ToIdentifierName()
				.Assign(
					createResult
					.Access("Data")
					.Access("Id")
					.Access("Value")
				)
				.ToExpressionStatement()
			);

			StatementHelpers.AddLocalAsyncMethodCallAndFaultyCheck(statements, "ExecuteAfter", null, returnDataType, dto, createResult.Access("Data"));

			return statements;
		}

		public static void CreateForeignKeyCheckMethodForDto(
			UnitInformation unitInformation,
			ApplicationUseCase useCase,
			string domain,
			ReferenceDomainModelMap domainModelMap,
			DtoReferenceMap dtoReferenceMap,
			string domainProjectNamespace,
			ForeignKeyReferenceContainer foreignKeyReferenceContainer
			)
		{
			if (!dtoReferenceMap.TryGetDtoByDomainModel(domain, useCase.UseCaseName, useCase.NamespaceClassificationKey, domainModelMap.DomainModelName, out var dtoMap))
			{
				throw new System.ArgumentException($"Dto {domainModelMap.DomainModelName} not found");
			}

			var domainModelName = domainModelMap.GetDomainModelTypeName(domainProjectNamespace);
			var dtoForeignKeyReferences = TemplateMethods.CollectForeignKeysAndAddToMethodCall(
				foreignKeyReferenceContainer,
				domainModelMap,
				dtoMap,
				null,
				null,
				false
			);

			if (dtoForeignKeyReferences is null || dtoForeignKeyReferences.Count == 0)
			{
				return;
			}

			var statements = new List<StatementSyntax>();

			TemplateMethods.CreateForeignKeyCheckStatements(domainModelMap, useCase.ClassificationKey.ToVariableName().ToIdentifierName(), dtoForeignKeyReferences, statements, Eshava.CodeAnalysis.SyntaxConstants.Bool, true);

			statements.Add(
				Eshava.CodeAnalysis.SyntaxConstants.True
				.Access("ToResponseData")
				.Call()
				.Return()
			);

			var methodDeclarationName = $"CheckForeignKeysAsync";
			var methodDeclaration = methodDeclarationName.ToMethod(
				SyntaxConstants.TaskResponseDataBool,
				statements,
				SyntaxKind.PrivateKeyword,
				SyntaxKind.AsyncKeyword
			);

			unitInformation.AddMethod((
				methodDeclarationName,
				methodDeclaration
				.WithParameter(useCase.ClassificationKey.ToVariableName()
					.ToParameter()
					.WithType(dtoMap.DtoName.ToType()))
			));
		}
	}
}