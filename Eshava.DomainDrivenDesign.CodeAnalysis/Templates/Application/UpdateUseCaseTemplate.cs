using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Eshava.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Constants;
using Eshava.DomainDrivenDesign.CodeAnalysis.Enums;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Domain;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Templates.Application
{
	public static class UpdateUseCaseTemplate
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
			var alternativeClass = request.AlternativeClasses.FirstOrDefault(ac => ac.Type == ApplicationUseCaseType.Update);

			var baseType = alternativeClass is null
				? CommonNames.Application.Abstracts.UPDATEUSECASE.AsGeneric(domainModelTypeName, request.UseCase.MainDto, domainModelMap.IdentifierType).ToSimpleBaseType()
				: alternativeClass.ClassName.AsGeneric(domainModelTypeName, request.UseCase.MainDto, domainModelMap.IdentifierType).ToSimpleBaseType();
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
			unitInformation.AddUsing(CommonNames.Namespaces.EXPRESSION);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.EXTENSIONS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.MODELS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Application.USECAES);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Application.EXTENSIONS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Application.PARTIALPUT);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Domain.CONSTANTS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Domain.EXTENSIONS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Domain.MODELS);

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
			var domainModelWithMappings = new HashSet<string>(); /* ToDo */

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
					CreateUpdateMethodActions
				)
			);

			if (request.UseCase.AddValidationConfigurationMethod)
			{
				unitInformation.AddMethod(TemplateMethods.CreateValidationConfigurationMethod(request.UseCase));
			}

			var childCreateMethodsResult = CreateProcessChildsMethods(request, domainModelMap, foreignKeyReferenceContainer, domainModelWithMappings, true, false);
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
				unitInformation.AddMethod(CreateCheckValidationConstraintsMethod(domainModelMap, request.DomainProjectNamespace));
			}

			if (request.UseCase.CheckForeignKeyReferencesAutomatically)
			{
				TemplateMethods.AddReferenceTypes(unitInformation, request.UseCasesMap, foreignKeyReferenceContainer.ForeignKeyHashSets, request.ApplicationProjectNamespace);

				var foreignKeyCheckMethods = TemplateMethods.CreateExistsForeignKeyMethod(foreignKeyReferenceContainer.ForeignKeyHashSets);
				foreach (var foreignKeyCheckMethod in foreignKeyCheckMethods)
				{
					unitInformation.AddMethod(foreignKeyCheckMethod);
				}

				TemplateMethods.AddReferenceUsageChecks(unitInformation, request.ApplicationProjectNamespace, request.UseCasesMap, request.UseCase, domainModelMap);

				if (!domainModelMap.IsChildDomainModel)
				{
					CreateForeignKeyCheckMethodForPatches(
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

		private static List<StatementSyntax> CreateUpdateMethodActions(
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
			var domainModel = useCase.GetDomainModelReferenceName().GetDomainModelTypeName(domain, domainModelMap.ClassificationKey, domainModelMap.FeatureName, domainProjectNamespace);

			CodeSnippetHelpers.AddStatements(statements, returnDataType, codeSnippets);

			if (!dtoReferenceMap.TryGetDtoByDomainModel(domain, useCase.UseCaseName, useCase.NamespaceClassificationKey, domainModelMap.DomainModelName, out var dtoMap))
			{
				throw new System.ArgumentException($"Dto {domainModelMap.DomainModelName} not found");
			}

			(var readStatements, var providerResult) = StatementHelpers.ReadDomainModel(useCase, domainModelMap, provider, returnDataType, false);

			statements.AddRange(readStatements);

			statements.Add(
				"patchesResult"
				.ToVariableStatement(
					"request"
					.Access(domainModelMap.ClassificationKey)
					.Access("GetPatchInformation".AsGeneric(dtoMap.DtoName, domainModel))
					.Call()
				)
			);

			statements.Add("patchesResult".ToFaultyCheck(returnDataType));

			statements.Add(
				"patchesResult"
				.ToIdentifierName()
				.Assign(
					"ExecuteBeforeAsync"
					.ToIdentifierName()
					.Call(
						"patchesResult".Access("Data").ToArgument(),
						"request".Access(domainModelMap.ClassificationKey).ToArgument()
					)
					.Await()
				)
				.ToExpressionStatement()
			);
			statements.Add("patchesResult".ToFaultyCheck(returnDataType));

			var agregatePatchStatements = new List<StatementSyntax>();

			if (hasValidationRules && !domainModelMap.IsChildDomainModel)
			{
				StatementHelpers.AddLocalAsyncMethodCallAndFaultyCheck(agregatePatchStatements, "CheckValidationConstraintsAsync", "constraintsResult", returnDataType, providerResult, "patchesResult".Access("Data"));
			}

			if (domainModelMap.IsChildDomainModel)
			{
				var childPatchStatement = "KeyValuePair"
					.AsGeneric(
						domainModelMap.IdentifierType.ToIdentifierName(),
						"IList".AsGeneric("Patch".AsGeneric(domainModelMap.GetDomainModelTypeName(domainProjectNamespace)))
					)
					.ToInstance(
						"request".Access($"{domainModelMap.ClassificationKey}Id").ToArgument(),
						"patchesResult".Access("Data").ToArgument()
					);

				var methodArguments = new List<ExpressionSyntax>
				{
					providerResult,
					childPatchStatement,
					"request".Access(domainModelMap.ClassificationKey)
				};

				if (useCase.ReadAggregateByChildId)
				{
					methodArguments.Add("request".Access($"{domainModelMap.ClassificationKey}Id"));
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

				StatementHelpers.AddLocalAsyncMethodCallAndFaultyCheck(agregatePatchStatements, $"Update{domainModelMap.ClassificationKey}Async", "updateResult", returnDataType, methodArguments.ToArray());
			}
			else
			{
				if (useCase.CheckForeignKeyReferencesAutomatically)
				{
					var domainModelName = domainModelMap.GetDomainModelTypeName(domainProjectNamespace);
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
						StatementHelpers.AddLocalAsyncMethodCallAndFaultyCheck(statements, "CheckForeignKeysAsync", "checkForeignKeyhResult", returnDataType, true, "patchesResult".ToIdentifierName());
					}
				}

				StatementHelpers.AddMethodCallAndFaultyCheck(agregatePatchStatements, providerResult, "Patch", "entityPatchResult", returnDataType, "patchesResult".Access("Data"));
			}

			if (!domainModelMap.IsAggregate || domainModelMap.ChildDomainModels.Count == 0)
			{
				statements.Add(
					"patchesResult"
					.Access("Data")
					.Access("Count")
					.ToEquals("0".ToLiteralInt())
					.If(returnDataType.ToNoContentResponseDataReturn())
				);

				statements.AddRange(agregatePatchStatements);
			}
			else
			{
				statements.AddRange(agregatePatchStatements);
			}

			if (domainModelMap.IsAggregate && !domainModelMap.IsChildDomainModel && domainModelMap.ChildDomainModels.Count > 0)
			{
				foreach (var childDomainModel in domainModelMap.ChildDomainModels)
				{
					var childReferenceProperty = dtoMap.ChildReferenceProperties.FirstOrDefault(p => p.Dto.DomainModelName == childDomainModel.DomainModelName);
					if (childReferenceProperty is null)
					{
						continue;
					}

					StatementHelpers.AddLocalAsyncMethodCallAndFaultyCheck(
						statements,
						$"Process{childReferenceProperty.Property.Name}ChangesAsync",
						$"process{childReferenceProperty.Property.Name}ChangesResult",
						returnDataType,
						providerResult,
						"request".Access(domainModelMap.ClassificationKey)
					);
				}
			}

			statements.Add(
				providerResult
				.Access("IsChanged")
				.Not()
				.If(returnDataType.ToNoContentResponseDataReturn())
			);

			StatementHelpers.AddAsyncMethodCallAndFaultyCheck(statements, provider, "SaveAsync", "saveResult", returnDataType, providerResult);

			if (domainModelMap.IsChildDomainModel)
			{
				StatementHelpers.AddLocalAsyncMethodCallAndFaultyCheck(statements, "ExecuteAfter", null, returnDataType, "updateResult".Access("Data"), "request".Access(domainModelMap.ClassificationKey));
			}
			else
			{
				StatementHelpers.AddLocalAsyncMethodCallAndFaultyCheck(statements, "ExecuteAfter", null, returnDataType, providerResult, "request".Access(domainModelMap.ClassificationKey));
			}

			return statements;
		}

		private static (string Name, MemberDeclarationSyntax) CreateCheckValidationConstraintsMethod(ReferenceDomainModelMap domainModelMap, string domainProjectNamespace)
		{
			var provider = domainModelMap.ClassificationKey.ToQueryProviderName().ToFieldName();
			var domainModelVariableName = domainModelMap.DomainModelName.ToVariableName();
			var domainModelType = domainModelMap.DomainModelName.GetDomainModelTypeName(domainModelMap.Domain, domainModelMap.ClassificationKey, domainModelMap.FeatureName, domainProjectNamespace);

			var statements = new List<StatementSyntax>();

			foreach (var property in domainModelMap.DomainModel.Properties)
			{
				foreach (var rule in property.ValidationRules)
				{
					switch (rule.Type)
					{
						case ValidationRuleType.Unique:
							AddUniqueCheck(statements, domainModelMap, property, rule, provider, domainModelVariableName);

							break;
					}
				}
			}

			statements.Add(StatementHelpers.GetResponseDataReturn(true));

			var methodDeclarationName = "CheckValidationConstraintsAsync";
			var methodDeclaration = methodDeclarationName.ToMethod(
				SyntaxConstants.TaskResponseDataBool,
				statements,
				SyntaxKind.PrivateKeyword,
				SyntaxKind.AsyncKeyword
			);

			methodDeclaration = methodDeclaration
				.WithParameter(
					domainModelVariableName
						.ToParameter()
						.WithType(domainModelType.ToIdentifierName()),
					"patches"
						.ToParameter()
						.WithType("IList".AsGeneric("Patch".AsGeneric(domainModelType)))

				);

			return (methodDeclarationName, methodDeclaration);
		}

		private static void AddUniqueCheck(
			List<StatementSyntax> statements,
			ReferenceDomainModelMap domainModel,
			DomainModelPropery property,
			DomainModelProperyValidationRule rule,
			string provider,
			string domainModelVariableName
		)
		{
			var propertyPatchValue = $"{property.Name.ToVariableName()}Value";
			var propertyPatchName = $"{property.Name.ToVariableName()}Patch";
			statements.Add(propertyPatchName.CreatePatchVariable(property.Name, "patches".ToIdentifierName()));

			var arguments = new List<ExpressionSyntax>
			{
				domainModelVariableName.Access("Id"),
				propertyPatchValue.ToIdentifierName()
			};

			var ifStatements = new List<StatementSyntax>
			{
				propertyPatchValue.ToVariableStatement(propertyPatchName.Access("Value").Cast(property.TypeWithUsing.ToType()))
			};

			if (rule.RelatedProperties.Count > 0)
			{
				foreach (var relatedPropertyName in rule.RelatedProperties)
				{
					var relatedProperty = domainModel.DomainModel.Properties.FirstOrDefault(p => p.Name == relatedPropertyName);
					if (relatedProperty is null)
					{
						continue;
					}

					var relatedPropertyPatchValue = $"{relatedProperty.Name.ToVariableName()}Value";
					var relatedPropertyPatchName = $"{relatedProperty.Name.ToVariableName()}Patch";
					ifStatements.Add(relatedPropertyPatchName.CreatePatchVariable(relatedProperty.Name, "patches".ToIdentifierName()));

					ifStatements.Add(
						relatedPropertyPatchValue
						.ToVariableStatement(
							relatedPropertyPatchName
							.Access("Value", true)
							.AsType(relatedProperty.TypeWithUsing.ToType())
							.AddNullFallback(domainModelVariableName.Access(relatedProperty.Name))
						)
					);

					arguments.Add(relatedPropertyPatchValue.ToIdentifierName());
				}
			}

			var resultName = $"isUnique{property.Name}Result";
			StatementHelpers.AddAsyncMethodCallAndFaultyCheck(ifStatements, provider, $"IsUnique{property.Name}Async", resultName, (TypeSyntax)null, arguments.ToArray());

			var uniqueFaultyResult = SyntaxConstants.ResponseDataBool.CreateFaultyResponse(
				EshavaMessageConstant.InvalidDataError.Map(),
				(property.Name, "Unique", propertyPatchValue.ToIdentifierName())
			);

			foreach (var relatedProperty in rule.RelatedProperties)
			{
				uniqueFaultyResult = uniqueFaultyResult
					.AddValidationError(relatedProperty, "Unique", $"{relatedProperty.ToVariableName()}Value".ToIdentifierName());
			}

			ifStatements.Add(
				resultName
				.Access("Data")
				.Not()
				.If(uniqueFaultyResult.Return())
			);

			statements.Add(
				propertyPatchName
				.Access("Value", true)
				.IsNotNull()
				.If(ifStatements.ToArray())
			);
		}

		private static List<(string Name, MemberDeclarationSyntax)> CreateProcessChildsMethods(
			UseCaseTemplateRequest request,
			ReferenceDomainModelMap domainModelMap,
			ForeignKeyReferenceContainer foreignKeyReferenceContainer,
			HashSet<string> domainModelWithMappings,
			bool topLevelCall,
			bool pretendTopLevelCall
		)
		{
			var methodDeclarations = new List<(string Name, MemberDeclarationSyntax)>();

			if (!request.DtoReferenceMap.TryGetDtoByDomainModel(request.Domain, request.UseCase.UseCaseName, request.UseCase.NamespaceClassificationKey, domainModelMap.DomainModelName, out var dtoMap))
			{
				return methodDeclarations;
			}

			var aggregateParameterName = "";
			var domainModelType = (TypeSyntax)null;
			if ((topLevelCall || (!topLevelCall && !domainModelMap.IsAggregate)) && domainModelMap.IsChildDomainModel)
			{
				aggregateParameterName = domainModelMap.AggregateDomainModel.ClassificationKey.ToVariableName();
				domainModelType = domainModelMap.AggregateDomainModel.GetDomainModelTypeName(request.DomainProjectNamespace).ToType();
				var childDtoVariableName = $"{domainModelMap.ClassificationKey.ToVariableName()}Patches";

				if (!topLevelCall && domainModelMap.IsAggregate)
				{
					return methodDeclarations;
				}

				var methodArguments = new List<ExpressionSyntax>
				{
					aggregateParameterName.ToIdentifierName(),
					childDtoVariableName.ToIdentifierName(),
					Eshava.CodeAnalysis.SyntaxConstants.Null
				};

				var dtoForeignKeyReferences = request.UseCase.CheckForeignKeyReferencesAutomatically
					? TemplateMethods.CollectForeignKeysAndAddToMethodCall(
						foreignKeyReferenceContainer,
						domainModelMap,
						dtoMap,
						null,
						methodArguments,
						false
					)
					: null;

				if (topLevelCall)
				{
					var topLevelDomainModel = domainModelMap.GetTopLevelDomainModel();
					if (topLevelDomainModel.DomainModelName != domainModelMap.AggregateDomainModel.DomainModelName)
					{
						methodDeclarations.Add(TemplateMethods.CreateCollectChildWrapperMethodForUpdate(domainModelMap, dtoMap, request.DomainProjectNamespace, request.UseCase.ReadAggregateByChildId));
					}

					methodDeclarations.AddRange(CreateUpdateChildMethod(request, foreignKeyReferenceContainer, domainModelMap, dtoMap, aggregateParameterName, domainModelType, request.DomainProjectNamespace, dtoForeignKeyReferences, domainModelWithMappings, true, true));
				}
				else if (!domainModelMap.IsAggregate)
				{
					methodDeclarations.AddRange(CreateUpdateChildMethod(request, foreignKeyReferenceContainer, domainModelMap, dtoMap, aggregateParameterName, domainModelType, request.DomainProjectNamespace, dtoForeignKeyReferences, domainModelWithMappings, true, false));
				}

				return methodDeclarations;

			}

			if (!domainModelMap.IsAggregate)
			{
				return methodDeclarations;
			}

			domainModelType = domainModelMap.GetDomainModelTypeName(request.DomainProjectNamespace).ToType();
			aggregateParameterName = domainModelMap.ClassificationKey.ToVariableName();
			var patchDocumentName = $"{aggregateParameterName}Document";

			foreach (var childDomainModel in domainModelMap.ChildDomainModels)
			{
				var childDomainModelType = childDomainModel.GetDomainModelTypeName(request.DomainProjectNamespace);
				var childReferenceProperty = dtoMap.ChildReferenceProperties.FirstOrDefault(p => p.Dto.DomainModelName == childDomainModel.DomainModelName);
				if (childReferenceProperty is null)
				{
					continue;
				}

				var statements = new List<StatementSyntax>();
				var changeResult = "changesResult";
				var changeResultData = changeResult.Access("Data");

				var mappingSource = "Source".ToPropertyExpressionTupleElement(childReferenceProperty.Dto.DtoName);
				var mappingTarget = "Target".ToPropertyExpressionTupleElement(childDomainModelType);

				statements.Add(
					changeResult
					.ToVariableStatement(
						patchDocumentName
						.Access("GetPatchInformation".AsGeneric(dtoMap.DtoName, childReferenceProperty.Dto.DtoName, childDomainModelType, childDomainModel.IdentifierType))
						.Call(
							"p".ToPropertyExpression(childReferenceProperty.Property.Name).ToArgument(),
							"List".AsGeneric(mappingSource.ToTupleType(mappingTarget)).ToInstance().ToArgument()
						)
					)
				);

				statements.Add(changeResult.ToFaultyCheck(Eshava.CodeAnalysis.SyntaxConstants.Bool));
				var childVariableName = childDomainModel.ClassificationKey.ToVariableName();

				var foreignKeyMethodArguments = new List<ExpressionSyntax>();

				var createMethodArguments = new List<ExpressionSyntax>
					{
						aggregateParameterName.ToIdentifierName(),
						changeResultData.Access("ItemsToAdd")
					};

				var documentLayerVariableName = $"{childDomainModel.ClassificationKey.ToVariableName()}DocumentLayer";
				var updateMethodArguments = new List<ExpressionSyntax>
					{
						aggregateParameterName.ToIdentifierName(),
						childVariableName.ToIdentifierName(),
						documentLayerVariableName.ToIdentifierName()
					};

				var dtoForeignKeyReferences = request.UseCase.CheckForeignKeyReferencesAutomatically
					? TemplateMethods.CollectForeignKeysAndAddToMethodCall(
						foreignKeyReferenceContainer,
						childDomainModel,
						childReferenceProperty.Dto,
						statements,
						foreignKeyMethodArguments,
						true
					)
					: null;

				createMethodArguments.AddRange(foreignKeyMethodArguments);
				updateMethodArguments.AddRange(foreignKeyMethodArguments);

				if (childReferenceProperty.Property.IsEnumerable)
				{
					StatementHelpers.AddLocalAsyncMethodCallAndFaultyCheck(statements, $"Create{childDomainModel.ClassificationKey.ToPlural()}Async", "createResult", Eshava.CodeAnalysis.SyntaxConstants.Bool, createMethodArguments.ToArray());
				}

				var updateStatements = new List<StatementSyntax>
					{
						documentLayerVariableName
						.ToVariableStatement(
							patchDocumentName.Access("GetLayerForIdentifier".AsGeneric(dtoMap.DtoName, childDomainModel.IdentifierType))
							.Call(
								"p".ToPropertyExpression(childReferenceProperty.Property.Name).ToArgument(),
								childVariableName.Access("Key").ToArgument()
							)
						)
					};

				StatementHelpers.AddLocalAsyncMethodCallAndFaultyCheck(updateStatements, $"Update{childDomainModel.ClassificationKey}Async", "updateResult", Eshava.CodeAnalysis.SyntaxConstants.Bool, updateMethodArguments.ToArray());
				statements.Add(changeResultData.Access("ItemsToPatch").ForEach(childVariableName, updateStatements.ToArray()));

				if (childReferenceProperty.Property.IsEnumerable)
				{
					var removeStatements = new List<StatementSyntax>();
					StatementHelpers.AddLocalAsyncMethodCallAndFaultyCheck(removeStatements, $"Deactivate{childDomainModel.ClassificationKey}Async", "deactivateResult", (TypeSyntax)null, updateMethodArguments.Take(2).ToArray());
					statements.Add(changeResultData.Access("ItemsToRemove").ForEach(childVariableName, removeStatements.ToArray()));
				}

				if (topLevelCall || pretendTopLevelCall)
				{
					methodDeclarations.AddRange(TemplateMethods.CreateCreateChildsMethods(request, domainModelMap, foreignKeyReferenceContainer, domainModelWithMappings, false, false));
				}
				methodDeclarations.AddRange(CreateUpdateChildMethod(request, foreignKeyReferenceContainer, childDomainModel, childReferenceProperty.Dto, aggregateParameterName, domainModelType, request.DomainProjectNamespace, dtoForeignKeyReferences, domainModelWithMappings, false, false));

				if (childReferenceProperty.Property.IsEnumerable)
				{
					methodDeclarations.Add(CreateDeactivateChildMethod(request.UseCase, childDomainModel, childReferenceProperty.Dto, aggregateParameterName, domainModelType, request.ApplicationProjectNamespace, request.DomainProjectNamespace));
				}

				StatementHelpers.AddResponseDataReturn(statements, true);

				var methodDeclarationName = $"Process{childReferenceProperty.Property.Name}ChangesAsync";
				var methodDeclaration = methodDeclarationName.ToMethod(
					SyntaxConstants.TaskResponseDataBool,
					statements,
					SyntaxKind.PrivateKeyword,
					SyntaxKind.AsyncKeyword
				);

				var partialPutDocument = topLevelCall
					? patchDocumentName
						.ToParameter()
						.WithType("PartialPutDocument".AsGeneric(dtoMap.DtoName))
					: patchDocumentName
						.ToParameter()
						.WithType("PartialPutDocumentLayer".ToType())
						;

				methodDeclarations.Insert(0, (
					methodDeclarationName,
					methodDeclaration
					.WithParameter(
						aggregateParameterName
							.ToParameter()
							.WithType(domainModelType),
						partialPutDocument
					)
				));
			}

			return methodDeclarations;
		}

		public static IEnumerable<(string Name, MemberDeclarationSyntax)> CreateUpdateChildMethod(
			UseCaseTemplateRequest request,
			ForeignKeyReferenceContainer foreignKeyReferenceContainer,
			ReferenceDomainModelMap childDomainModel,
			ReferenceDtoMap dtoMap,
			string aggregateParameterName,
			TypeSyntax aggregateDomainModelType,
			string domainProjectNamespace,
			List<(ForeignKeyCache ForeignKey, ApplicationUseCaseDtoProperty Property)> foreignKeyHashSets,
			HashSet<string> domainModelWithMappings,
			bool skipForeignKeyHashsetParameter,
			bool pretentTopLevelCall
		)
		{
			var methodDeclarations = new List<(string Name, MemberDeclarationSyntax)>();

			var statements = new List<StatementSyntax>();
			var createResultVariable = $"create{childDomainModel.ClassificationKey}Result";
			var childDomainModelType = childDomainModel.GetDomainModelTypeName(domainProjectNamespace);
			var childDtoVariableName = $"{childDomainModel.ClassificationKey.ToVariableName()}Patches";
			var childVariableName = $"{childDomainModel.ClassificationKey.ToVariableName()}Result";
			var childVariablePatchName = $"{childDomainModel.ClassificationKey.ToVariableName()}PatchResult";
			var hasAsyncMethodCalls = false;

			var methodModifier = new List<SyntaxKind>
			{
				SyntaxKind.PrivateKeyword
			};

			var subChildDomainModelsWithReferences = GetChildDomainModelWithDtoReference(childDomainModel, dtoMap);

			if (foreignKeyHashSets is not null)
			{
				hasAsyncMethodCalls = CreateForeignKeyCheckStatementsForPatches(childDomainModel, childDtoVariableName.ToIdentifierName(), "Value", foreignKeyHashSets, statements, childDomainModelType.ToType(), skipForeignKeyHashsetParameter)
					|| childDomainModel.DomainModel.HasValidationRules
					|| subChildDomainModelsWithReferences.Count > 0;
			}

			StatementHelpers.AddMethodCallAndFaultyCheck(statements, aggregateParameterName, $"Get{childDomainModel.ChildEnumerableName}", childVariableName, (TypeSyntax)null, !hasAsyncMethodCalls, childDtoVariableName.Access("Key"));

			if (childDomainModel.DomainModel.HasValidationRules)
			{
				hasAsyncMethodCalls = true;
				StatementHelpers.AddLocalAsyncMethodCallAndFaultyCheck(statements, "CheckValidationConstraintsAsync", "constraintsResult", childDomainModelType.ToType(), childVariableName.Access("Data"), childDtoVariableName.Access("Value"));

				methodDeclarations.Add(CreateCheckValidationConstraintsMethod(childDomainModel, domainProjectNamespace));
			}
			else if (subChildDomainModelsWithReferences.Count > 0 && !hasAsyncMethodCalls)
			{
				hasAsyncMethodCalls = true;
			}

			statements.Add(
				childVariablePatchName
				.ToVariableStatement(
					childVariableName
					.Access("Data")
					.Access("Patch")
					.Call(
						childDtoVariableName.Access("Value").ToArgument()
					)
				)
			);

			statements.Add(childVariablePatchName.ToFaultyCheck(childDomainModelType.ToType(), !hasAsyncMethodCalls));

			var documentLayerVariableName = $"{childDomainModel.ClassificationKey.ToVariableName()}DocumentLayer";
			if (subChildDomainModelsWithReferences.Count > 0)
			{
				methodDeclarations.AddRange(CreateProcessChildsMethods(request, childDomainModel, foreignKeyReferenceContainer, domainModelWithMappings, false, pretentTopLevelCall));

				foreach (var reference in subChildDomainModelsWithReferences)
				{
					var processChildsStatements = new List<StatementSyntax>();
					StatementHelpers.AddLocalAsyncMethodCallAndFaultyCheck(
						processChildsStatements,
						$"Process{reference.Property.Property.Name}ChangesAsync",
						$"process{reference.Property.Property.Name}ChangesResult",
						childDomainModelType.ToType(),
						childVariableName.Access("Data"),
						documentLayerVariableName.ToIdentifierName()
					);

					statements.Add(
						documentLayerVariableName
						.ToIdentifierName()
						.IsNotNull()
						.If(processChildsStatements.ToArray())
					);
				}
			}

			ExpressionSyntax returnStatement = childVariableName.ToIdentifierName();

			if (hasAsyncMethodCalls)
			{
				methodModifier.Add(SyntaxKind.AsyncKeyword);
			}
			else
			{
				returnStatement = returnStatement
					.Access("ToTask")
					.Call();
			}

			statements.Add(
				returnStatement
				.Return()
			);

			var methodDeclarationname = $"Update{childDomainModel.ClassificationKey}Async";
			var methodDeclaration = methodDeclarationname.ToMethod(
				"Task".AsGeneric("ResponseData".AsGeneric(childDomainModelType)),
				statements,
				methodModifier.ToArray()
			);

			var methodParameter = new List<ParameterSyntax>
			{
				aggregateParameterName
					.ToParameter()
					.WithType(aggregateDomainModelType),
				childDtoVariableName
					.ToParameter()
					.WithType("KeyValuePair".AsGeneric(childDomainModel.IdentifierType.ToIdentifierName(), "IList".AsGeneric("Patch".AsGeneric(childDomainModelType)))),
				documentLayerVariableName
					.ToParameter()
					.WithType("PartialPutDocumentLayer".ToType()),
			};

			if (!skipForeignKeyHashsetParameter && foreignKeyHashSets is not null)
			{
				foreach (var foreignKeyHashSet in foreignKeyHashSets)
				{
					var hashSetParameter = foreignKeyHashSet
						.ForeignKey
						.HashSetName
						.ToParameter()
						.WithType(foreignKeyHashSet.ForeignKey.HashSetType);

					if (methodParameter.All(mp => !mp.IsEquivalentTo(hashSetParameter)))
					{
						methodParameter.Add(hashSetParameter);
					}
				}
			}

			methodDeclaration = methodDeclaration
				.WithParameter(methodParameter.ToArray());

			methodDeclarations.Add((methodDeclarationname, methodDeclaration));

			return methodDeclarations;
		}

		public static (string Name, MemberDeclarationSyntax Method) CreateDeactivateChildMethod(
			ApplicationUseCase useCase,
			ReferenceDomainModelMap childDomainModel,
			ReferenceDtoMap dtoMap,
			string aggregateParameterName,
			TypeSyntax aggregateDomainModelType,
			string applicationProjectNamespace,
			string domainProjectNamespace
		)
		{
			var statements = new List<StatementSyntax>();
			var createResultVariable = $"create{childDomainModel.ClassificationKey}Result";
			var childDomainModelType = childDomainModel.GetDomainModelTypeName(domainProjectNamespace);
			var childDtoVariableName = $"{childDomainModel.ClassificationKey.ToVariableName()}Id";
			var childVariableName = $"{childDomainModel.ClassificationKey.ToVariableName()}Result";
			var hasAsyncMethodCalls = false;

			var methodModifier = new List<SyntaxKind>
			{
				SyntaxKind.PrivateKeyword
			};

			if (useCase.CheckForeignKeyReferencesAutomatically)
			{
				hasAsyncMethodCalls = TemplateMethods.AddReferenceUsageChecks(useCase, childDomainModel, statements, applicationProjectNamespace, childDtoVariableName.ToIdentifierName(), null);
			}

			StatementHelpers.AddMethodCallAndFaultyCheck(statements, aggregateParameterName, $"Get{childDomainModel.ChildEnumerableName}", childVariableName, Eshava.CodeAnalysis.SyntaxConstants.Bool, !hasAsyncMethodCalls, childDtoVariableName.ToIdentifierName());

			var returnStatement = childVariableName
				.Access("Data")
				.Access("Deactivate")
				.Call();

			if (hasAsyncMethodCalls)
			{
				methodModifier.Add(SyntaxKind.AsyncKeyword);
			}
			else
			{
				returnStatement = returnStatement
					.Access("ToTask")
					.Call();
			}

			statements.Add(
				returnStatement
				.Return()
			);

			var methodDeclarationName = $"Deactivate{childDomainModel.ClassificationKey}Async";
			var methodDeclaration = methodDeclarationName.ToMethod(
				SyntaxConstants.TaskResponseDataBool,
				statements,
				methodModifier.ToArray()
			);

			var methodParameter = new List<ParameterSyntax>
			{
				aggregateParameterName
						.ToParameter()
						.WithType(aggregateDomainModelType),
					childDtoVariableName
						.ToParameter()
						.WithType(childDomainModel.IdentifierType.ToType())
			};

			return (methodDeclarationName, methodDeclaration.WithParameter(methodParameter.ToArray()));
		}

		public static void CreateForeignKeyCheckMethodForPatches(
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

			CreateForeignKeyCheckStatementsForPatches(domainModelMap, "patchesResult".ToIdentifierName(), "Data", dtoForeignKeyReferences, statements, Eshava.CodeAnalysis.SyntaxConstants.Bool, true);

			statements.Add(
				Eshava.CodeAnalysis.SyntaxConstants.True
				.Access("ToResponseData")
				.Call()
				.Return()
			);

			var methodDeclarationName = "CheckForeignKeysAsync";
			var methodDeclaration = methodDeclarationName.ToMethod(
				SyntaxConstants.TaskResponseDataBool,
				statements,
				SyntaxKind.PrivateKeyword,
				SyntaxKind.AsyncKeyword
			);

			unitInformation.AddMethod((
				methodDeclarationName,
				methodDeclaration
				.WithParameter("patchesResult"
					.ToParameter()
					.WithType("ResponseData".AsGeneric("IList".AsGeneric("Patch".AsGeneric(domainModelName)))))
			));
		}

		public static bool CreateForeignKeyCheckStatementsForPatches(
			ReferenceDomainModelMap domainModelMap,
			ExpressionSyntax dtoVariableName,
			string dtoVariableNameAccessProperty,
			List<(ForeignKeyCache ForeignKey, ApplicationUseCaseDtoProperty Property)> foreignKeyHashSets,
			List<StatementSyntax> statements,
			TypeSyntax methodReturnType,
			bool skipForeignKeyHashsetParameter
		)
		{
			var hasAsyncMethodCalls = false;
			foreach (var foreignKeyHashSet in foreignKeyHashSets)
			{
				hasAsyncMethodCalls = true;

				var patchName = $"{foreignKeyHashSet.Property.Name.ToVariableName()}Patch";

				statements.Add(patchName.CreatePatchVariable(foreignKeyHashSet.Property.Name, dtoVariableName.Access(dtoVariableNameAccessProperty)));

				var methodArguments = new List<ExpressionSyntax>
				{
					patchName.Access("Value").Cast(foreignKeyHashSet.Property.TypeWithUsing.ToType(true))
				};

				if (!skipForeignKeyHashsetParameter && (domainModelMap.IsChildDomainModel || foreignKeyHashSet.ForeignKey.Owner.Count > 1))
				{
					methodArguments.Add(foreignKeyHashSet.ForeignKey.HashSetName.ToIdentifierName());
				}
				else
				{
					methodArguments.Add(Eshava.CodeAnalysis.SyntaxConstants.Null);
				}

				var patchStatements = new List<StatementSyntax>();

				StatementHelpers.AddLocalAsyncMethodCallAndFaultyCheck(
					patchStatements,
					$"Check{foreignKeyHashSet.ForeignKey.ClassificationKey}ExistenceAsync",
					$"check{foreignKeyHashSet.Property.Name}ExistenceResult",
					methodReturnType,
					methodArguments.ToArray()
				);

				statements.Add(
					patchName
					.Access("Value", true)
					.AsType(foreignKeyHashSet.Property.TypeWithUsing.ToType(true).AsNullable())
					.IsNotNull()
					.If(patchStatements.ToArray())
				);
			}

			return hasAsyncMethodCalls;
		}

		private static List<(ReferenceDomainModelMap DomainModelMap, ReferenceDtoProperty Property)> GetChildDomainModelWithDtoReference(ReferenceDomainModelMap domainModelMap, ReferenceDtoMap dtoMap)
		{
			var childsWithReference = new List<(ReferenceDomainModelMap DomainModelMap, ReferenceDtoProperty Property)>();

			if (!domainModelMap.IsAggregate || domainModelMap.ChildDomainModels.Count == 0)
			{
				return childsWithReference;
			}

			foreach (var childDomainModel in domainModelMap.ChildDomainModels)
			{
				var childReferenceProperty = dtoMap.ChildReferenceProperties.FirstOrDefault(p => p.Dto.DomainModelName == childDomainModel.DomainModelName);
				if (childReferenceProperty is null)
				{
					continue;
				}

				childsWithReference.Add((childDomainModel, childReferenceProperty));
			}

			return childsWithReference;
		}
	}
}