using System;
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
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Eshava.DomainDrivenDesign.CodeAnalysis
{
	public static class TemplateMethods
	{
		public delegate List<StatementSyntax> CreateMainMethodActions(
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
		);

		public static void AddDomainModelUsings(UnitInformation unitInformation, ReferenceDomainModelMap domainModelMap, string domainProjectNamespace, string domain)
		{
			if (!domainModelMap.DomainModel.IsNamespaceDirectoryUncountable)
			{
				unitInformation.AddUsing($"{domainProjectNamespace}.{domain}.{domainModelMap.DomainModel.NamespaceDirectory}");
			}

			foreach (var childDomainModel in domainModelMap.ChildDomainModels)
			{
				if (childDomainModel.DomainModel.IsNamespaceDirectoryUncountable)
				{
					continue;
				}

				unitInformation.AddUsing($"{domainProjectNamespace}.{domain}.{childDomainModel.DomainModel.NamespaceDirectory}");
			}

			if (domainModelMap.IsChildDomainModel && !domainModelMap.AggregateDomainModel.DomainModel.IsNamespaceDirectoryUncountable)
			{
				unitInformation.AddUsing($"{domainProjectNamespace}.{domain}.{domainModelMap.AggregateDomainModel.DomainModel.NamespaceDirectory}");
			}
		}

		public static void AddReferenceUsageChecks(UnitInformation unitInformation, string projectFullQualifiedNamespace, UseCasesMap useCasesMap, ApplicationUseCase useCase, ReferenceDomainModelMap domainModelMap)
		{
			var domainModelsExcludedFromForeignKeyCheck = useCase.ExcludedFromForeignKeyCheck
				.GroupBy(e => e.Domain)
				.ToDictionary(e => e.Key, e => e.Select(m => m.Name).ToImmutableHashSet());

			var domainModelsToSkip = useCase.DeactivateBefore
				.GroupBy(e => e.Domain)
				.ToDictionary(e => e.Key, e => e.Select(m => m.Name).ToImmutableHashSet());

			var references = new List<ReferenceDomainModel>();
			references.AddRange(domainModelMap.ReferencesToMe);


			if (domainModelMap.IsAggregate)
			{
				references.AddRange(domainModelMap.ChildDomainModels.SelectMany(cdm => cdm.ReferencesToMe));
			}

			foreach (var domainModel in references)
			{

				if (domainModelsExcludedFromForeignKeyCheck.ContainsKey(domainModel.Domain)
					&& domainModelsExcludedFromForeignKeyCheck[domainModel.Domain].Contains(domainModel.DomainModelName))
				{
					continue;
				}

				if (domainModelsToSkip.ContainsKey(domainModel.Domain)
					&& domainModelsToSkip[domainModel.Domain].Contains(domainModel.DomainModelName))
				{
					continue;
				}

				var queryProviderFeatureName = useCasesMap.GetFeatureName(domainModel.Domain, domainModel.ClassificationKey);
				var queryProviderType = domainModel.ClassificationKey.ToQueryProviderType();
				var queryProviderName = domainModel.ClassificationKey.ToQueryProviderName();
				var queryProviderField = $"_{queryProviderName}";

				if (!unitInformation.ConstructorParameters.Any(p => p.Name == queryProviderName))
				{
					var providerUsing = domainModel.ClassificationKey.GetQueriesNamespace(domainModel.Domain, queryProviderFeatureName, projectFullQualifiedNamespace);

					unitInformation.AddUsing(providerUsing);
					unitInformation.AddConstructorParameter(queryProviderName, queryProviderType);
				}
			}
		}

		public static void AddReferenceTypes(UnitInformation unitInformation, UseCasesMap useCasesMap, List<ForeignKeyCache> referenceTypes, string fullQualifiedNamespace)
		{
			foreach (var referenceType in referenceTypes)
			{
				if (!referenceType.IsUsed)
				{
					continue;
				}

				var referenceQueryProviderFeatureName = useCasesMap.GetFeatureName(referenceType.Domain, referenceType.ClassificationKey);
				unitInformation.AddUsing(referenceType.ClassificationKey.GetQueriesNamespace(referenceType.Domain, referenceQueryProviderFeatureName, fullQualifiedNamespace));
				var referenceQueryProviderType = referenceType.ClassificationKey.ToQueryProviderType();
				var referenceQueryProviderName = referenceType.ClassificationKey.ToQueryProviderName();
				unitInformation.AddConstructorParameter(referenceQueryProviderName, referenceQueryProviderType);
			}
		}

		public static ForeignKeyReferenceContainer CollectForeignKeyReferenceTypes(string domainProjectNamespace, ReferenceDomainModelMap domainModelMap)
		{
			var container = new ForeignKeyReferenceContainer();

			var maps = new List<ReferenceDomainModelMap> { domainModelMap };
			if (domainModelMap.IsAggregate && domainModelMap.ChildDomainModels.Count > 0)
			{
				maps.AddRange(domainModelMap.ChildDomainModels);
			}

			foreach (var map in maps)
			{
				foreach (var foreignKeyReference in map.ForeignKeyReferences)
				{
					if (foreignKeyReference.DomainModel.IsValueObject)
					{
						continue;
					}

					container.AddReference(foreignKeyReference, map.DomainModelName);
				}
			}

			return container;
		}

		public static List<(ForeignKeyCache ForeignKey, ApplicationUseCaseDtoProperty Property)> CollectForeignKeysAndAddToMethodCall(
			ForeignKeyReferenceContainer foreignKeyReferenceContainer,
			ReferenceDomainModelMap childDomainModel,
			ReferenceDtoMap childReferenceDto,
			List<StatementSyntax> statements,
			List<ExpressionSyntax> methodArguments,
			bool addForeignKeyHashsetVariable
		)
		{
			var dtoForeignKeyReferences = new List<(ForeignKeyCache ForeignKey, ApplicationUseCaseDtoProperty Property)>();
			var allForeignKeyReferences = foreignKeyReferenceContainer.ForeignKeyHashSets
				.Where(set => set.Owner.Contains(childDomainModel.DomainModelName))
				.ToList();

			foreach (var foreignKeyReference in allForeignKeyReferences)
			{
				(var isUsed, var properties) = foreignKeyReference.IsReferencedInDto(childDomainModel, childReferenceDto);
				if (!isUsed)
				{
					continue;
				}

				if (addForeignKeyHashsetVariable && statements is not null)
				{
					var hashSetStatement = foreignKeyReference.HashSetName
						.ToVariableStatement(foreignKeyReference.HashSetType.ToInstance());

					if (statements.All(s => !s.IsEquivalentTo(hashSetStatement)))
					{
						statements.Add(hashSetStatement);
					}
				}

				if (methodArguments is not null)
				{
					var hashSetParameter = foreignKeyReference.HashSetName.ToIdentifierName();
					if (methodArguments.All(ma => !ma.IsEquivalentTo(hashSetParameter)))
					{
						methodArguments.Add(hashSetParameter);
					}
				}

				foreach (var property in properties)
				{
					dtoForeignKeyReferences.Add((foreignKeyReference, property));
				}
			}

			return dtoForeignKeyReferences;
		}

		public static (string Name, MemberDeclarationSyntax Method) CreateCreateChildMethod(
			ReferenceDomainModelMap childDomainModel,
			ReferenceDtoMap dtoMap,
			string aggregateParameterName,
			TypeSyntax aggregateDomainModelType,
			string childDtoVariableName,
			string domainProjectNamespace,
			List<(ForeignKeyCache ForeignKey, ApplicationUseCaseDtoProperty Property)> foreignKeyHashSets,
			HashSet<string> domainModelWithMappings,
			bool skipForeignKeyHashsetParameter,
			string createResultVariable,
			List<StatementSyntax> additionalStatements
		)
		{
			var statements = new List<StatementSyntax>();
			var childDomainModelType = childDomainModel.GetDomainModelTypeName(domainProjectNamespace);
			var isAsync = false;

			if (childDomainModel.DomainModel.HasValidationRules)
			{
				isAsync = true;
				StatementHelpers.AddLocalAsyncMethodCallAndFaultyCheck(statements, "CheckValidationConstraintsAsync", "constraintsResult", childDomainModelType, childDomainModel.ClassificationKey.ToVariableName().ToIdentifierName());
			}

			if (foreignKeyHashSets is not null && foreignKeyHashSets.Count > 0)
			{
				isAsync = true;
				CreateForeignKeyCheckStatements(childDomainModel, childDtoVariableName.ToIdentifierName(), foreignKeyHashSets, statements, childDomainModelType.ToType(), skipForeignKeyHashsetParameter);
			}

			var addMethodArguments = new List<ArgumentSyntax>
			{
				childDtoVariableName.ToIdentifierName().ToArgument()
			};

			if (domainModelWithMappings.Contains(childDomainModel.DomainModelName))
			{
				addMethodArguments.Add($"{childDomainModel.DomainModelName.ToFieldName()}Mappings".ToIdentifierName().ToArgument());
			}

			if ((additionalStatements?.Count ?? 0) > 0)
			{
				isAsync = true;

				statements.Add(
					createResultVariable
					.ToVariableStatement(
						aggregateParameterName
						.Access($"Add{childDomainModel.ChildEnumerableName}")
						.Call(addMethodArguments.ToArray())
					)
				);

				statements.AddRange(additionalStatements);

				statements.Add(
					createResultVariable
					.ToIdentifierName()
					.Return(!isAsync)
				);
			}
			else
			{
				statements.Add(aggregateParameterName
					.Access($"Add{childDomainModel.ChildEnumerableName}")
					.Call(addMethodArguments.ToArray())
					.Return(!isAsync)
				);
			}

			var accessModifier = new List<SyntaxKind>
			{
				SyntaxKind.PrivateKeyword
			};

			if (isAsync)
			{
				accessModifier.Add(SyntaxKind.AsyncKeyword);
			}

			var methodName = $"Create{childDomainModel.ClassificationKey}Async";
			var methodDeclaration = methodName.ToMethod(
				"Task".AsGeneric("ResponseData".AsGeneric(childDomainModelType)),
				statements,
				accessModifier.ToArray()
			);

			var methodParameter = new List<ParameterSyntax>
			{
				aggregateParameterName
						.ToParameter()
						.WithType(aggregateDomainModelType),
					childDtoVariableName
						.ToParameter()
						.WithType(dtoMap.DtoName.ToType())
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

			return (methodName, methodDeclaration.WithParameter(methodParameter.ToArray()));
		}

		public static void CreateForeignKeyCheckStatements(
			ReferenceDomainModelMap domainModelMap,
			ExpressionSyntax dtoVariableName,
			List<(ForeignKeyCache ForeignKey, ApplicationUseCaseDtoProperty Property)> foreignKeyHashSets,
			List<StatementSyntax> statements,
			TypeSyntax methodReturnType,
			bool skipForeignKeyHashsetParameter
		)
		{
			foreach (var foreignKeyHashSet in foreignKeyHashSets)
			{
				var methodArguments = new List<ExpressionSyntax>
				{
					foreignKeyHashSet.Property.IsNullableType
						? dtoVariableName.Access(foreignKeyHashSet.Property.Name).Access("Value")
						: dtoVariableName.Access(foreignKeyHashSet.Property.Name)
				};

				if (!skipForeignKeyHashsetParameter && (domainModelMap.IsChildDomainModel || foreignKeyHashSet.ForeignKey.Owner.Count > 1))
				{
					methodArguments.Add(foreignKeyHashSet.ForeignKey.HashSetName.ToIdentifierName());
				}
				else
				{
					methodArguments.Add(Eshava.CodeAnalysis.SyntaxConstants.Null);
				}

				var checkStatements = new List<StatementSyntax>();

				StatementHelpers.AddLocalAsyncMethodCallAndFaultyCheck(
					checkStatements,
					$"Check{foreignKeyHashSet.ForeignKey.ClassificationKey}ExistenceAsync",
					$"check{foreignKeyHashSet.Property.Name}ExistenceResult",
					methodReturnType,
					methodArguments.ToArray()
				);

				if (foreignKeyHashSet.Property.IsNullableType)
				{
					statements.Add(
						dtoVariableName
							.Access(foreignKeyHashSet.Property.Name)
							.Access("HasValue")
							.If(checkStatements.ToArray())
					);
				}
				else
				{
					statements.AddRange(checkStatements);
				}
			}
		}

		public static List<(string Name, MethodDeclarationSyntax Method)> CreateExistsForeignKeyMethod(List<ForeignKeyCache> foreignKeys)
		{
			var methodDeclarations = new List<(string Name, MethodDeclarationSyntax Method)>();

			foreach (var foreignKey in foreignKeys)
			{
				if (!foreignKey.IsUsed)
				{
					continue;
				}

				var referenceQueryProviderName = foreignKey.ClassificationKey.ToQueryProviderName().ToFieldName();

				var statements = new List<StatementSyntax>();
				var foreignKeyStatements = new List<StatementSyntax>();
				var foreignKeyExistsResult = $"{foreignKey.ClassificationKey.ToVariableName()}ExistsResult";
				var foreignKeyIdVariableName = $"{foreignKey.ClassificationKey.ToVariableName()}Id";

				statements.Add(
					foreignKey.HashSetName
					.ToIdentifierName()
					.IsNotNull()
					.And(
						foreignKey.HashSetName
						.Access("Contains")
						.Call(foreignKeyIdVariableName.ToArgument()))


					.If(StatementHelpers.GetResponseDataReturn(true))
				);

				StatementHelpers.AddAsyncMethodCallAndFaultyCheck(statements, referenceQueryProviderName, $"ExistsAsync", foreignKeyExistsResult, (TypeSyntax)null, foreignKeyIdVariableName.ToIdentifierName());

				statements.Add(
					foreignKeyExistsResult
					.Access("Data")
					.Not()
					.If(
						"MessageConstants"
						.Access("INVALIDDATAERROR")
						.Access("ToFaultyResponse".AsGeneric(Eshava.CodeAnalysis.SyntaxConstants.Bool))
						.Call()
						.Access(CommonNames.Extensions.ADDVALIDATIONERROR)
						.Call(
							foreignKey.ClassificationKey.ToLiteralString().ToArgument(),
							"MessageConstants".Access("NOTEXISTING").ToArgument(),
							foreignKeyIdVariableName.ToArgument()
						)
						.Return()
					)
				);


				statements.Add(
					foreignKey.HashSetName
					.ToIdentifierName()
					.IsNotNull()
					.If(
						foreignKey.HashSetName
						.Access("Add")
						.Call(foreignKeyIdVariableName.ToArgument())
						.ToExpressionStatement()
					)
				);


				StatementHelpers.AddResponseDataReturn(statements, true);

				var methodDeclarationName = $"Check{foreignKey.ClassificationKey}ExistenceAsync";
				var methodDeclaration = methodDeclarationName
					.ToMethod(
						SyntaxConstants.TaskResponseDataBool,
						statements,
						SyntaxKind.PrivateKeyword,
						SyntaxKind.AsyncKeyword
					);

				methodDeclarations.Add((
					methodDeclarationName,
					methodDeclaration
					.WithParameter(
						foreignKeyIdVariableName
						.ToParameter()
						.WithType(foreignKey.IdentifierType.ToType()),
						foreignKey.HashSetName
							.ToParameter()
							.WithType(foreignKey.HashSetType)
					)
				));
			}

			return methodDeclarations;
		}

		public static bool AddReferenceUsageChecks(
			ApplicationUseCase useCase,
			ReferenceDomainModelMap domainModelMap,
			List<StatementSyntax> statements,
			string projectFullQualifiedNamespace,
			ExpressionSyntax primaryKeyVariable,
			ExpressionSyntax aggregateVariableName

		)
		{
			var hasAsyncMethodCalls = false;

			var domainModelsExcludedFromForeignKeyCheck = useCase.ExcludedFromForeignKeyCheck
				.GroupBy(e => e.Domain)
				.ToDictionary(e => e.Key, e => e.Select(m => m.Name).ToImmutableHashSet());

			var domainModelsToSkip = useCase.DeactivateBefore
				.GroupBy(e => e.Domain)
				.ToDictionary(e => e.Key, e => e.Select(m => m.Name).ToImmutableHashSet());

			var usedQueryProvider = new HashSet<string>();
			foreach (var referenceToMe in domainModelMap.ReferencesToMe)
			{
				if (referenceToMe.IsProcessingProperty)
				{
					continue;
				}

				var queryProviderName = referenceToMe.ClassificationKey.ToQueryProviderName();
				if (usedQueryProvider.Contains(queryProviderName))
				{
					continue;
				}

				usedQueryProvider.Add(queryProviderName);
				hasAsyncMethodCalls |= AddReferenceUsageCheck(statements, domainModelsExcludedFromForeignKeyCheck, domainModelsToSkip, referenceToMe, projectFullQualifiedNamespace, primaryKeyVariable);
			}

			if (domainModelMap.IsAggregate && aggregateVariableName is not null)
			{
				foreach (var childDomainModel in domainModelMap.ChildDomainModels)
				{
					var childStatements = new List<StatementSyntax>();
					var childItemName = childDomainModel.ChildEnumerableName.ToVariableName();
					var childItemId = childItemName.Access("Id").Access("Value");
					var atLeastOnCheck = false;
					var usedChildQueryProvider = new HashSet<string>();

					foreach (var referenceToMe in childDomainModel.ReferencesToMe)
					{
						if (referenceToMe.IsProcessingProperty)
						{
							continue;
						}

						var queryProviderName = referenceToMe.ClassificationKey.ToQueryProviderName();
						if (usedChildQueryProvider.Contains(queryProviderName))
						{
							continue;
						}


						atLeastOnCheck = true;
						usedChildQueryProvider.Add(queryProviderName);
						hasAsyncMethodCalls |= AddReferenceUsageCheck(childStatements, domainModelsExcludedFromForeignKeyCheck, domainModelsToSkip, referenceToMe, projectFullQualifiedNamespace, childItemId);
					}

					if (atLeastOnCheck)
					{
						statements.Add(aggregateVariableName.Access(childDomainModel.ChildEnumerableName.ToPlural()).ForEach(childItemName, childStatements));
					}
				}
			}

			return hasAsyncMethodCalls;
		}

		public static (string Name, MemberDeclarationSyntax Method) CreateUseCaseMainMethod(
			ApplicationUseCase useCase,
			string domain,
			ReferenceDomainModelMap domainModel,
			DtoReferenceMap dtoReferenceMap,
			string domainProjectNamespace,
			ForeignKeyReferenceContainer foreignKeyReferenceContainer,
			HashSet<string> domainModelWithMappings,
			List<UseCaseCodeSnippet> codeSnippets,
			CreateMainMethodActions createMethodActions
		)
		{
			var tryBlockStatements = new List<StatementSyntax>();
			var returnDataType = useCase.ResponseType;
			var provider = domainModel
				.GetTopLevelDomainModel()
				.DomainModelName
				.ToProviderName()
				.ToFieldName();
			var domainModelId = $"{domainModel.ClassificationKey.ToVariableName()}Id";

			if (useCase.Type == ApplicationUseCaseType.Create)
			{
				tryBlockStatements.Add(
					domainModelId
					.ToVariableStatement(Eshava.CodeAnalysis.SyntaxConstants.Default.Call(domainModel.IdentifierType.ToArgument()))
				);
			}

			var methodActions = createMethodActions(
				useCase,
				domain,
				domainModel,
				dtoReferenceMap,
				returnDataType,
				provider,
				domainModelId,
				domainProjectNamespace,
				domainModel.DomainModel.HasValidationRules,
				foreignKeyReferenceContainer,
				domainModelWithMappings,
				codeSnippets
			);

			if (useCase.WarpInTransaction)
			{
				var transactionStatements = new List<StatementSyntax>();

				methodActions.Add("transaction".Access("Complete").Call().ToExpressionStatement());

				tryBlockStatements.Add("transaction"
					.ToVariable(provider
						.Access("CreateTransactionScope")
						.Call()
					)
					.Using(methodActions)
				);

				tryBlockStatements.AddRange(transactionStatements);
			}
			else
			{
				tryBlockStatements.AddRange(methodActions);
			}

			if (useCase.Type == ApplicationUseCaseType.Create)
			{
				tryBlockStatements.Add(returnDataType
					.ToIdentifierName()
					.ToInstanceWithInitializer(
						"Id"
						.ToIdentifierName()
						.Assign(
							domainModelId.ToIdentifierName()
						)
					)
					.Access(CommonNames.Extensions.TORESPONSEDATA)
					.Call()
					.Return()
				);

			}
			else
			{
				tryBlockStatements.Add(returnDataType
					.ToIdentifierName()
					.ToInstance()
					.Access(CommonNames.Extensions.TORESPONSEDATA)
					.Call(
						"HttpStatusCode"
						.Access("NoContent")
						.ToArgument()
					)
					.Return()
				);
			}

			var statements = new List<StatementSyntax>
			{
				tryBlockStatements.TryCatch(
					CreateCatchBlock(returnDataType, useCase, domainModel, true)
				)
			};

			var methodDeclaration = useCase.MethodName.ToMethod(
				"Task".AsGeneric("ResponseData".AsGeneric(returnDataType)),
				statements,
				SyntaxKind.PublicKeyword,
				SyntaxKind.AsyncKeyword
			);

			methodDeclaration = methodDeclaration
				.WithParameter(
					"request"
					.ToVariableName()
					.ToParameter()
					.WithType(useCase.RequestType.ToType())
				);

			return (useCase.MethodName, methodDeclaration);
		}

		public static List<(string Name, MemberDeclarationSyntax Method)> CreateCreateChildsMethods(
			UseCaseTemplateRequest request,
			ReferenceDomainModelMap domainModelMap,
			ForeignKeyReferenceContainer foreignKeyReferenceContainer,
			HashSet<string> domainModelWithMappings,
			bool skipForeignKeyHashsetParameter,
			bool topLevelCall
		)
		{
			var methodDeclarations = new List<(string Name, MemberDeclarationSyntax Method)>();

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
				var childVariableName = domainModelMap.ClassificationKey.ToVariableName();
				var dtoForeignKeyReferences = request.UseCase.CheckForeignKeyReferencesAutomatically
					? TemplateMethods.CollectForeignKeysAndAddToMethodCall(
						foreignKeyReferenceContainer,
						domainModelMap,
						dtoMap,
						null,
						null,
						skipForeignKeyHashsetParameter
					)
					: null;

				if (topLevelCall)
				{
					var topLevelDomainModel = domainModelMap.GetTopLevelDomainModel();
					if (topLevelDomainModel.DomainModelName != domainModelMap.AggregateDomainModel.DomainModelName)
					{
						methodDeclarations.Add(CreateCollectChildWrapperMethodForCreate(domainModelMap, dtoMap, request.DomainProjectNamespace, request.UseCase.ReadAggregateByChildId));
					}

					methodDeclarations.AddRange(CreateCreateChildAndSubChildMethods(request, domainModelMap, dtoMap, aggregateParameterName, domainModelType, dtoForeignKeyReferences, foreignKeyReferenceContainer, domainModelWithMappings, skipForeignKeyHashsetParameter, false));
				}
				else if (!domainModelMap.IsAggregate)
				{
					methodDeclarations.Add(
						TemplateMethods.CreateCreateChildMethod(
							domainModelMap,
							dtoMap,
							aggregateParameterName,
							domainModelType,
							childVariableName,
							request.DomainProjectNamespace,
							dtoForeignKeyReferences,
							domainModelWithMappings,
							skipForeignKeyHashsetParameter,
							null,
							null
						)
					);
				}

				return methodDeclarations;
			}

			if (!domainModelMap.IsAggregate)
			{
				return methodDeclarations;
			}

			domainModelType = domainModelMap.GetDomainModelTypeName(request.DomainProjectNamespace).ToType();
			aggregateParameterName = domainModelMap.ClassificationKey.ToVariableName();

			foreach (var childDomainModel in domainModelMap.ChildDomainModels)
			{
				var childReferenceProperty = dtoMap.ChildReferenceProperties.FirstOrDefault(p => p.Dto.DomainModelName == childDomainModel.DomainModelName);
				if (childReferenceProperty is null)
				{
					continue;
				}

				var statements = new List<StatementSyntax>();
				string parameterName;
				TypeSyntax parameterType;

				if (childReferenceProperty.Property.IsEnumerable)
				{
					parameterName = childReferenceProperty.Dto.ClassificationKey.ToVariableName().ToPlural();
					parameterType = "IEnumerable".AsGeneric(childReferenceProperty.Dto.DtoName);

					var childVariableName = childDomainModel.ClassificationKey.ToVariableName();
					var methodArguments = new List<ExpressionSyntax>
					{
						aggregateParameterName.ToIdentifierName(),
						childVariableName.ToIdentifierName()
					};

					var dtoForeignKeyReferences = request.UseCase.CheckForeignKeyReferencesAutomatically
						? TemplateMethods.CollectForeignKeysAndAddToMethodCall(
							foreignKeyReferenceContainer,
							childDomainModel,
							childReferenceProperty.Dto,
							statements,
							methodArguments,
							skipForeignKeyHashsetParameter
						)
						: null;


					var createStatements = new List<StatementSyntax>();
					StatementHelpers.AddLocalAsyncMethodCallAndFaultyCheck(createStatements, $"Create{childDomainModel.ClassificationKey}Async", "createResult", Eshava.CodeAnalysis.SyntaxConstants.Bool, methodArguments.ToArray());
					statements.Add(parameterName.ToIdentifierName().ForEach(childVariableName, createStatements.ToArray()));

					StatementHelpers.AddResponseDataReturn(statements, true);

					var methodDeclarationName = $"Create{childDomainModel.ClassificationKey.ToPlural()}Async";
					var methodDeclaration = methodDeclarationName.ToMethod(
						SyntaxConstants.TaskResponseDataBool,
						statements,
						SyntaxKind.PrivateKeyword,
						SyntaxKind.AsyncKeyword
					);

					var methodParameter = new List<ParameterSyntax>
					{
						aggregateParameterName
							.ToParameter()
							.WithType(domainModelType),
						parameterName
							.ToVariableName()
							.ToParameter()
							.WithType(parameterType)
					};

					if (!skipForeignKeyHashsetParameter && dtoForeignKeyReferences is not null)
					{
						foreach (var item in dtoForeignKeyReferences)
						{
							var hashSetParameter = item.ForeignKey.HashSetName
								.ToParameter()
								.WithType(item.ForeignKey.HashSetType);

							if (methodParameter.All(mp => !mp.IsEquivalentTo(hashSetParameter)))
							{
								methodParameter.Add(hashSetParameter);
							}
						}
					}

					methodDeclarations.Add((methodDeclarationName, methodDeclaration.WithParameter(methodParameter.ToArray())));

					methodDeclarations.AddRange(CreateCreateChildAndSubChildMethods(request, childDomainModel, childReferenceProperty.Dto, aggregateParameterName, domainModelType, dtoForeignKeyReferences, foreignKeyReferenceContainer, domainModelWithMappings, false, false));
				}
				else
				{
					var dtoForeignKeyReferences = request.UseCase.CheckForeignKeyReferencesAutomatically
						? TemplateMethods.CollectForeignKeysAndAddToMethodCall(
							foreignKeyReferenceContainer,
							childDomainModel,
							childReferenceProperty.Dto,
							null,
							null,
							skipForeignKeyHashsetParameter
						)
						: null;

					methodDeclarations.AddRange(CreateCreateChildAndSubChildMethods(request, childDomainModel, childReferenceProperty.Dto, aggregateParameterName, domainModelType, dtoForeignKeyReferences, foreignKeyReferenceContainer, domainModelWithMappings, skipForeignKeyHashsetParameter, request.UseCase.Type != ApplicationUseCaseType.Create));
				}
			}

			return methodDeclarations;
		}

		public static (string Name, MemberDeclarationSyntax Method) CreateCheckValidationConstraintsMethod(ApplicationUseCase useCase, string domain, ReferenceDomainModelMap domainModel, ApplicationUseCaseDto dto)
		{
			var provider = domainModel.ClassificationKey.ToQueryProviderName().ToFieldName();
			var dtoVariableName = domainModel.ClassificationKey.ToVariableName();

			var statements = new List<StatementSyntax>();

			foreach (var property in domainModel.DomainModel.Properties)
			{
				var dtoProperty = dto.Properties.FirstOrDefault(p => !p.ReferenceProperty.IsNullOrEmpty() && p.ReferenceProperty == property.Name)
					?? dto.Properties.FirstOrDefault(p => p.Name == property.Name);

				if (dtoProperty is null)
				{
					continue;
				}

				foreach (var rule in property.ValidationRules)
				{
					switch (rule.Type)
					{
						case ValidationRuleType.Unique:
							AddUniqueCheck(statements, domainModel, property, rule, provider, dtoVariableName, dto, dtoProperty);

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
					dtoVariableName
					.ToParameter()
					.WithType(dto.Name.ToType())
				);

			return (methodDeclarationName, methodDeclaration);
		}

		public static void AddCreateChildModelsStatements(ReferenceDomainModelMap domainModelMap, ReferenceDtoMap dtoMap, List<StatementSyntax> statements, string returnDataType, string createResult, ExpressionSyntax dto)
		{
			foreach (var childDomainModel in domainModelMap.ChildDomainModels)
			{
				var childReferenceProperty = dtoMap.ChildReferenceProperties.FirstOrDefault(p => p.Dto.DomainModelName == childDomainModel.DomainModelName);
				if (childReferenceProperty is null)
				{
					continue;
				}

				var methodReference = childReferenceProperty.Property.IsEnumerable
					? childDomainModel.ClassificationKey.ToPlural()
					: childDomainModel.ClassificationKey;

				StatementHelpers.AddLocalAsyncMethodCallAndFaultyCheck(
					statements,
					$"Create{methodReference}Async",
					$"create{childReferenceProperty.Property.Name}Result",
					returnDataType,
					createResult.Access("Data"),
					dto.Access(childReferenceProperty.Property.Name)
				);
			}
		}

		public static List<StatementSyntax> CreateCatchBlock(string returnDataType, ApplicationUseCase useCase, ReferenceDomainModelMap domainModel, bool referencesAreFields)
		{
			var message = "";
			var messageType = EshavaMessageConstant.UnexpectedError;
			var additional = new List<(ExpressionSyntax Property, string Name)>();

			switch (useCase.Type)
			{
				case ApplicationUseCaseType.Read:
				case ApplicationUseCaseType.Search:
				case ApplicationUseCaseType.SearchCount:
					message = $"Entity {useCase.ClassificationKey} could not be read";
					messageType = EshavaMessageConstant.ReadDataError;

					break;
				case ApplicationUseCaseType.Create:
					message = $"Entity {domainModel.ClassificationKey} could not be created";
					messageType = EshavaMessageConstant.CreateDataError;
					additional.Add(("request".Access(domainModel.ClassificationKey), ""));

					break;
				case ApplicationUseCaseType.Update:
					message = $"Entity {domainModel.ClassificationKey} could not be updated";
					messageType = EshavaMessageConstant.UpdateDataError;
					additional.Add(("request".Access(domainModel.ClassificationKey), ""));

					break;
				case ApplicationUseCaseType.Delete:
					message = $"Entity {domainModel.ClassificationKey} could not be deactivated";
					messageType = EshavaMessageConstant.DeleteDataError;

					break;
				default:
					message = "An error occurred";
					messageType = EshavaMessageConstant.UnexpectedError;

					break;
			}

			if (useCase.Type == ApplicationUseCaseType.Update
				|| useCase.Type == ApplicationUseCaseType.Delete)
			{

				if (domainModel.IsChildDomainModel && !useCase.ReadAggregateByChildId)
				{
					additional.Add(("request".Access($"{domainModel.AggregateDomainModel.ClassificationKey}Id"), ""));
				}

				additional.Add(("request".Access($"{domainModel.ClassificationKey}Id"), ""));
			}
			else if (useCase.Type == ApplicationUseCaseType.Create)
			{
				if (domainModel.IsChildDomainModel)
				{
					additional.Add(("request".Access($"{domainModel.AggregateDomainModel.ClassificationKey}Id"), ""));
				}
			}
			else if (useCase.Type == ApplicationUseCaseType.Read)
			{
				additional.Add(("request".Access($"{useCase.ClassificationKey}Id"), ""));
			}

			return returnDataType.CreateCatchBlock(message.ToLiteralString(), messageType, referencesAreFields, additional.ToArray());
		}

		public static (string Name, MemberDeclarationSyntax) CreateValidationConfigurationMethod(ApplicationUseCase useCase)
		{
			var dto = useCase.Dtos.FirstOrDefault(d => d.Name == useCase.MainDto);

			var validationMainDtoName = $"Validation{useCase.MainDto}";

			var config = useCase.ValidationConfigurationAsTreeStructure
				? Eshava.CodeAnalysis.SyntaxConstants.True.ToArgument()
				: Eshava.CodeAnalysis.SyntaxConstants.False.ToArgument();

			var statements = new List<StatementSyntax>
			{
				"GetValidationConfiguration".AsGeneric(validationMainDtoName).Call(config).Return()
			};

			var methodDeclarationName = "GetValidationConfiguration";
			var methodDeclaration = methodDeclarationName.ToMethod(
				"ResponseData".AsGeneric("ValidationConfigurationResponse"),
				statements,
				SyntaxKind.PublicKeyword
			);

			return (methodDeclarationName, methodDeclaration);
		}

		public static (string Name, MemberDeclarationSyntax) CreateRegisterMethod(string methodName, List<DependencyInjection> dependencyInjections)
		{
			var statements = new List<StatementSyntax>();
			StatementHelpers.AddScoped(statements, dependencyInjections);

			statements.Add(
				"services"
				.ToIdentifierName()
				.Return()
			);

			var methodDeclaration = methodName.ToMethod(
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

			return (methodName, methodDeclaration);
		}

		public static IEnumerable<StatementSyntax> CreateCollectChildStatementsForDeactivate(
			ReferenceDomainModelMap childDomainModel,
			string domainProjectNamespace,
			TypeSyntax returnType,
			bool readAggregateByChildId
		)
		{
			if (!readAggregateByChildId)
			{
				var statements = new List<StatementSyntax>();
				var loopDomainModel = childDomainModel;

				do
				{
					statements.Add(
						$"{loopDomainModel.ClassificationKey}Id"
						.ToVariableName()
						.ToVariableStatement(
							"request"
							.Access($"{loopDomainModel.ClassificationKey}Id")
						)
					);

					loopDomainModel = loopDomainModel.AggregateDomainModel;
				} while (loopDomainModel.IsChildDomainModel);

				statements.AddRange(CreateCollectChildStatements(childDomainModel, new List<(TypeSyntax Type, string Name)>(), domainProjectNamespace, true, true, returnType, false).Statements);

				return statements;
			}

			return CreateCollectChildStatementsForReadByChildId(childDomainModel, domainProjectNamespace, returnType, false, true, false, null);
		}

		public static (string Name, MemberDeclarationSyntax Method) CreateCollectChildWrapperMethodForUpdate(
			ReferenceDomainModelMap childDomainModel,
			ReferenceDtoMap dtoMap,
			string domainProjectNamespace,
			bool readAggregateByChildId
		)
		{
			var methodParameter = new List<(TypeSyntax Type, string Name)>();

			var childPatchStatement = "KeyValuePair"
				.AsGeneric(
					childDomainModel.IdentifierType.ToIdentifierName(),
					"IList".AsGeneric("Patch".AsGeneric(childDomainModel.GetDomainModelTypeName(domainProjectNamespace)))
				);

			var childVariableName = childDomainModel.ClassificationKey.ToVariableName();
			methodParameter.Add((Type: childPatchStatement, Name: $"{childVariableName}Patches"));
			methodParameter.Add((Type: "PartialPutDocumentLayer".ToType(), Name: $"{childVariableName}DocumentLayer"));

			return CreateCollectChildWrapperMethod(childDomainModel, "Update", methodParameter, domainProjectNamespace, true, readAggregateByChildId);
		}

		public static string GetDomain(string referenceDomain, string defaultDomain)
		{
			return referenceDomain.IsNullOrEmpty()
				? defaultDomain
				: referenceDomain;
		}

		public static List<InterpolatedStringContentSyntax> CreateSqlQueryWithoutWhereCondition(InfrastructureModel model, string domain, List<QueryAnalysisItem> relatedDataModels, bool implementSoftDelete, bool asCount)
		{
			var interpolatedColumnParts = new List<InterpolatedStringContentSyntax>();
			var interpolatedTableParts = new List<InterpolatedStringContentSyntax>();
			var interpolatedStringParts = new List<InterpolatedStringContentSyntax>
			{
				@"
					SELECT".Interpolate(),
			};

			var isFirstColum = true;
			var modelItem = relatedDataModels.First(m => m.DataModel.Name == model.Name && m.Domain == domain && m.IsRootModel);

			if (asCount)
			{
				interpolatedColumnParts.Add(@"
						COUNT(".Interpolate());
				interpolatedColumnParts.Add(modelItem.TableAliasConstant.ToIdentifierName().Interpolate());
				interpolatedColumnParts.Add(".".Interpolate());
				interpolatedColumnParts.Add(Eshava.CodeAnalysis.SyntaxConstants.NameOf.Call(modelItem.DataModel.Name.Access("Id").ToArgument()).Interpolate());
				interpolatedColumnParts.Add(")".Interpolate());
			}
			else
			{
				var addedSelectParts = new HashSet<string>();
				foreach (var item in relatedDataModels)
				{
					if (item.DtoProperty is null || item.DtoProperty.IsVirtualProperty)
					{
						continue;
					}

					var dataType = item.GetDataType(domain, model.Name);

					if (item.DtoProperty.Name == "*")
					{
						var selectPart = $"{item.TableAliasConstant}.*";
						if (!addedSelectParts.Contains(selectPart))
						{
							AddSelectColumnSeparator(interpolatedColumnParts, ref isFirstColum);

							interpolatedColumnParts.Add(item.TableAliasConstant.ToIdentifierName().Interpolate());
							interpolatedColumnParts.Add(".*".Interpolate());
							addedSelectParts.Add(selectPart);
						}
					}
					else
					{
						var propertyName = item.Property.Name;
						// Check if its a value object
						if (item.DataModel.TableName.IsNullOrEmpty())
						{
							propertyName = item.ParentProperty.Name;
							dataType = item.ParentDataModel.Name;
						}

						var selectPart = $"{item.TableAliasConstant}.{propertyName}";

						if (!addedSelectParts.Contains(selectPart))
						{
							AddSelectColumnSeparator(interpolatedColumnParts, ref isFirstColum);

							interpolatedColumnParts.Add(item.TableAliasConstant.ToIdentifierName().Interpolate());
							interpolatedColumnParts.Add(".".Interpolate());
							interpolatedColumnParts.Add(Eshava.CodeAnalysis.SyntaxConstants.NameOf.Call(dataType.Access(propertyName).ToArgument()).Interpolate());
							addedSelectParts.Add(selectPart);
						}
					}
				}
			}

			var referenceRelations = relatedDataModels
				.Where(m => m.DataModel.Name != modelItem.DataModel.Name || (m.DataModel.Name == modelItem.DataModel.Name && m.TableAliasConstant != modelItem.TableAliasConstant) || (m.Domain != modelItem.Domain && m.DataModel.Name == modelItem.DataModel.Name))
				.GroupBy(m => new { m.Domain, Model = m.DataModel.Name, Property = m.Property.Name, m.TableAliasConstant })
				.Select(g => g.First())
				.ToList();

			CreateJoinQueryParts(modelItem, referenceRelations, interpolatedTableParts, implementSoftDelete);

			interpolatedStringParts.AddRange(interpolatedColumnParts);
			interpolatedStringParts.Add(@"
					FROM
						".Interpolate());
			interpolatedStringParts.Add("TypeAnalyzer".Access("GetTableName".AsGeneric(modelItem.GetDataType(domain, model.Name))).Call().Interpolate());
			interpolatedStringParts.Add(" ".Interpolate());
			interpolatedStringParts.Add(modelItem.TableAliasConstant.ToIdentifierName().Interpolate());
			interpolatedStringParts.AddRange(interpolatedTableParts);

			return interpolatedStringParts;
		}

		public static HashSet<string> CheckForPropertyMappings(UnitInformation unitInformation, string domain, IEnumerable<ApplicationUseCaseDto> useCaseDtos, ReferenceMap domainModelReferenceMap)
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

		private static void AddSelectColumnSeparator(List<InterpolatedStringContentSyntax> interpolatedColumnParts, ref bool isFirstColum)
		{
			if (isFirstColum)
			{
				interpolatedColumnParts.Add(@"
						 ".Interpolate());
				isFirstColum = false;
			}
			else
			{
				interpolatedColumnParts.Add(@"
						,".Interpolate());
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="parentItem">Contains information of the domain and data model for the repository</param>
		/// <param name="items">Data model relationships for joins</param>
		/// <param name="interpolatedTableParts"></param>
		/// <param name="implementSoftDelete"></param>
		private static void CreateJoinQueryParts(
			QueryAnalysisItem parentItem,
			List<QueryAnalysisItem> items,
			List<InterpolatedStringContentSyntax> interpolatedTableParts,
			bool implementSoftDelete
		)
		{
			var processedTableAliases = new HashSet<string>();
			foreach (var item in items)
			{
				if (item.ParentDomain != parentItem.Domain || item.ParentDataModel.Name != parentItem.DataModel.Name)
				{
					continue;
				}

				if (item.Property.IsReference && !item.Property.IsParentReference)
				{
					continue;
				}

				CreateJoinQueryParts(parentItem.Domain, parentItem.DataModel.Name, parentItem, item, items, interpolatedTableParts, processedTableAliases, implementSoftDelete);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="referenceDomain">Domain of the repository</param>
		/// <param name="referenceDataModel">Data model of the repository</param>
		/// <param name="parentItem">Information about the data model above the join</param>
		/// <param name="item">Information about the data model to be joined</param>
		/// <param name="items">Data model relationships for joins</param>
		/// <param name="interpolatedTableParts"></param>
		/// <param name="processedTableAliases"></param>
		/// <param name="implementSoftDelete"></param>
		private static void CreateJoinQueryParts(
			string referenceDomain,
			string referenceDataModel,
			QueryAnalysisItem parentItem,
			QueryAnalysisItem item,
			List<QueryAnalysisItem> items,
			List<InterpolatedStringContentSyntax> interpolatedTableParts,
			HashSet<string> processedTableAliases,
			bool implementSoftDelete
		)
		{
			if (processedTableAliases.Contains(item.TableAliasConstant))
			{
				return;
			}

			processedTableAliases.Add(item.TableAliasConstant);

			var match = false;
			if (item.DtoProperty is not null && item.ParentProperty is null)
			{
				// It's a virtual property
				if (item.Property is null)
				{

				}
				else if (item.Property.IsParentReference)
				{
					var dataType = item.GetDataType(referenceDomain, referenceDataModel);
					var parentDataType = parentItem.GetDataType(referenceDomain, referenceDataModel);
					interpolatedTableParts.AddRange(GetJoinsQueryParts(item.TableAliasConstant, dataType, item.Property.Name, parentItem.TableAliasConstant, parentDataType, "Id", implementSoftDelete));
					match = true;
				}
				else
				{
					processedTableAliases.Remove(item.TableAliasConstant);
				}
			}
			else
			{
				var referenceProperty = parentItem.DataModel.Properties.FirstOrDefault(p => p.IsReference && p.Name == item.ParentProperty.ReferencePropertyName);
				if (referenceProperty is null)
				{
					referenceProperty = item.DataModel.Properties.FirstOrDefault(p => p.IsReference && p.Name == item.ParentProperty.ReferencePropertyName);
					if (referenceProperty is not null)
					{
						var dataType = item.GetDataType(referenceDomain, referenceDataModel);
						var parentDataType = parentItem.GetDataType(referenceDomain, referenceDataModel);
						interpolatedTableParts.AddRange(GetJoinsQueryParts(item.TableAliasConstant, dataType, referenceProperty.Name, parentItem.TableAliasConstant, parentDataType, "Id", implementSoftDelete));
						match = true;
					}
				}
				else
				{
					var dataType = item.GetDataType(referenceDomain, referenceDataModel);
					var parentDataType = parentItem.GetDataType(referenceDomain, referenceDataModel);

					// current models has a foreign key for this domain model
					interpolatedTableParts.AddRange(GetJoinsQueryParts(item.TableAliasConstant, dataType, "Id", parentItem.TableAliasConstant, parentDataType, referenceProperty.Name, implementSoftDelete));
					match = true;
				}
			}

			if (match)
			{
				foreach (var newItem in items)
				{
					if (newItem.ParentDomain != item.Domain || newItem.ParentDataModel.Name != item.DataModel.Name)
					{
						continue;
					}

					if (newItem.DtoProperty is null)
					{
						continue;
					}

					if (!(newItem.DtoProperty?.ReferenceProperty.IsNullOrEmpty() ?? true) && !(item.DtoProperty?.ReferenceProperty.IsNullOrEmpty() ?? true))
					{
						var newDtoRefParts = newItem.DtoProperty.ReferenceProperty.Split('.');
						var newDtoRef = String.Concat(newDtoRefParts.Take(newDtoRefParts.Length - 2));

						var dtoRefParts = item.DtoProperty.ReferenceProperty.Split('.');
						var dtoRef = String.Concat(dtoRefParts.Take(dtoRefParts.Length - 1));

						if (dtoRef != newDtoRef)
						{
							continue;
						}
					}

					CreateJoinQueryParts(referenceDomain, referenceDataModel, item, newItem, items, interpolatedTableParts, processedTableAliases, implementSoftDelete);
				}
			}
		}

		private static List<InterpolatedStringContentSyntax> GetJoinsQueryParts(string tableAliasJoin, string dataTypeJoin, string propertyJoin, string tableAliasParent, string dataTypeParent, string propertyParent, bool implementSoftDelete)
		{
			var interpolatedTableParts = new List<InterpolatedStringContentSyntax>
			{
				@"
					LEFT JOIN
						".Interpolate(),
				"TypeAnalyzer".Access("GetTableName".AsGeneric(dataTypeJoin)).Call().Interpolate(),
				" ".Interpolate(),
				tableAliasJoin.ToIdentifierName().Interpolate(),
				@"
							ON ".Interpolate(),
				tableAliasJoin.ToIdentifierName().Interpolate(),
				".".Interpolate(),
				Eshava.CodeAnalysis.SyntaxConstants.NameOf.Call(dataTypeJoin.Access(propertyJoin).ToArgument()).Interpolate(),
				" = ".Interpolate(),
				tableAliasParent.ToIdentifierName().Interpolate(),
				".".Interpolate(),
				Eshava.CodeAnalysis.SyntaxConstants.NameOf.Call(dataTypeParent.Access(propertyParent).ToArgument()).Interpolate()
			};

			if (implementSoftDelete)
			{
				interpolatedTableParts.Add(@"
							AND ".Interpolate());
				interpolatedTableParts.Add(tableAliasJoin.ToIdentifierName().Interpolate());
				interpolatedTableParts.Add(".".Interpolate());
				interpolatedTableParts.Add(Eshava.CodeAnalysis.SyntaxConstants.NameOf.Call(dataTypeJoin.Access("Status").ToArgument()).Interpolate());
				interpolatedTableParts.Add(" = @Status".Interpolate());
			}

			return interpolatedTableParts;
		}

		private static IEnumerable<StatementSyntax> CreateCollectChildStatementsForReadByChildId(
			ReferenceDomainModelMap childDomainModel,
			string domainProjectNamespace,
			TypeSyntax returnType,
			bool returnAsTask,
			bool addTopLevelAggregateVariable,
			bool addChildAggregateVariableInsteadOfChildVariable,
			ExpressionSyntax childIdExpression
		)
		{
			var statements = new List<StatementSyntax>();
			var topLevelDomainModel = childDomainModel.GetTopLevelDomainModel();
			var childDomainModelAggregateIsTopLevelAggregate = topLevelDomainModel.DomainModelName == childDomainModel.AggregateDomainModel.DomainModelName;
			var aggregateParameterName = topLevelDomainModel.ClassificationKey.ToVariableName();
			var childAggregateVariableName = childDomainModel.AggregateDomainModel.ClassificationKey.ToVariableName();
			var childAggregateType = childDomainModel.AggregateDomainModel.GetDomainModelTypeName(domainProjectNamespace);
			var childVariableName = childDomainModel.ClassificationKey.ToVariableName();
			var childType = childDomainModel.GetDomainModelTypeName(domainProjectNamespace);
			returnType ??= childType.ToType();

			if (addTopLevelAggregateVariable)
			{
				statements.Add(aggregateParameterName.ToVariableStatement($"{aggregateParameterName}Result".Access("Data")));
			}

			if (addChildAggregateVariableInsteadOfChildVariable)
			{
				statements.Add(childAggregateVariableName.ToVariableStatement(childAggregateType.DefaultOf()));
			}
			else if (!childDomainModelAggregateIsTopLevelAggregate)
			{
				statements.Add(childVariableName.ToVariableStatement(childType.DefaultOf()));
			}

			var loopBeforeDomainModel = childDomainModel;
			var loopDomainModel = childDomainModel.AggregateDomainModel;

			var previousLoopStatements = new List<StatementSyntax>();

			while (loopBeforeDomainModel.IsChildDomainModel)
			{
				var loopDomainModelIsDirectAggregate = loopBeforeDomainModel.DomainModelName == childDomainModel.DomainModelName;
				if (loopDomainModelIsDirectAggregate)
				{
					childIdExpression ??= "request".Access($"{childDomainModel.ClassificationKey}Id");
					var loopItemName = loopDomainModel.DomainModelName == topLevelDomainModel.DomainModelName
						? loopDomainModel.ClassificationKey.ToVariableName()
						: $"{loopDomainModel.ClassificationKey.ToVariableName()}Item";

					previousLoopStatements.Add(
						$"{childVariableName}Result"
						.ToVariableStatement(
							loopItemName
							.Access($"Get{loopBeforeDomainModel.ChildEnumerableName}")
							.Call(childIdExpression.ToArgument())
						)
					);

					// is direct aggregate also the top level aggregates
					if (loopDomainModel.DomainModelName != topLevelDomainModel.DomainModelName)
					{
						previousLoopStatements.Add($"{childVariableName}Result".Access("IsFaulty").If(Eshava.CodeAnalysis.SyntaxConstants.Continue));
						if (!addChildAggregateVariableInsteadOfChildVariable)
						{
							previousLoopStatements.Add(childVariableName.ToIdentifierName().Assign($"{childVariableName}Result".Access("Data")).ToExpressionStatement());
						}
					}
					else
					{
						previousLoopStatements.Add($"{childVariableName}Result".ToFaultyCheck(returnType, returnAsTask));
						if (!addChildAggregateVariableInsteadOfChildVariable)
						{
							previousLoopStatements.Add(childVariableName.ToVariableStatement($"{childVariableName}Result".Access("Data")));
						}
					}

					if (addChildAggregateVariableInsteadOfChildVariable)
					{
						previousLoopStatements.Add(childAggregateVariableName.ToIdentifierName().Assign(loopItemName.ToIdentifierName()).ToExpressionStatement());
					}

					if (loopDomainModel.DomainModelName != topLevelDomainModel.DomainModelName)
					{
						previousLoopStatements.Add(Eshava.CodeAnalysis.SyntaxConstants.Break);
					}
				}
				else
				{
					var loopItemName = loopDomainModel.DomainModelName == topLevelDomainModel.DomainModelName
						? loopDomainModel.ClassificationKey.ToVariableName()
						: $"{loopDomainModel.ClassificationKey.ToVariableName()}Item";

					var currentLoopStatements = new List<StatementSyntax>
					{
						loopItemName
							.ToVariableName()
							.Access(loopBeforeDomainModel.ChildEnumerableName.ToPlural())
							.ForEach(
								$"{loopBeforeDomainModel.ClassificationKey.ToVariableName()}Item",
								previousLoopStatements
							)
					};

					if (loopDomainModel.DomainModelName != topLevelDomainModel.DomainModelName)
					{
						currentLoopStatements.Add(
							(addChildAggregateVariableInsteadOfChildVariable ? childAggregateVariableName : childVariableName)
								.ToIdentifierName()
								.IsNotNull()
								.If(Eshava.CodeAnalysis.SyntaxConstants.Break)
						);
					}

					previousLoopStatements = currentLoopStatements;
				}

				loopBeforeDomainModel = loopDomainModel;
				loopDomainModel = loopDomainModel.AggregateDomainModel;
			}

			statements.AddRange(previousLoopStatements);

			if (!childDomainModelAggregateIsTopLevelAggregate)
			{
				var ifExpression = "MessageConstants"
					.Access("NOTEXISTING")
					.Access(
						"ToFaultyResponse"
						.AsGeneric(returnType)
					)
					.Call();

				if (returnAsTask)
				{
					ifExpression = ifExpression
						.Access("ToTask")
						.Call();
				}

				statements.Add(
					(addChildAggregateVariableInsteadOfChildVariable ? childAggregateVariableName : childVariableName)
					.ToIdentifierName()
					.IsNull()
					.If(ifExpression.Return())
				);
			}

			return statements;
		}

		private static (string Name, MemberDeclarationSyntax Method) CreateCollectChildWrapperMethodForCreate(
			ReferenceDomainModelMap childDomainModel,
			ReferenceDtoMap dtoMap,
			string domainProjectNamespace,
			bool readAggregateByChildId
		)
		{
			var childVariableName = childDomainModel.ClassificationKey.ToVariableName();

			return CreateCollectChildWrapperMethod(childDomainModel, "Create", new List<(TypeSyntax Type, string Name)> { (Type: dtoMap.DtoName.ToType(), Name: childVariableName) }, domainProjectNamespace, true, readAggregateByChildId);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="request"></param>
		/// <param name="childDomainModel"></param>
		/// <param name="dtoMap"></param>
		/// <param name="methodName">Create or Update</param>
		/// <returns></returns>
		private static (string Name, MemberDeclarationSyntax Method) CreateCollectChildWrapperMethod(
			ReferenceDomainModelMap childDomainModel,
			string methodName,
			IEnumerable<(TypeSyntax Type, string Name)> transferParameter,
			string domainProjectNamespace,
			bool returnAsTask,
			bool readAggregateByChildId
		)
		{
			var childDomainModelType = childDomainModel.GetDomainModelTypeName(domainProjectNamespace);
			List<StatementSyntax> statements = null;
			List<ParameterSyntax> methodParameters = null;

			if (readAggregateByChildId)
			{
				statements = new List<StatementSyntax>();

				var topLevelDomainModel = childDomainModel.GetTopLevelDomainModel();

				var aggregateDomainModelType = topLevelDomainModel.GetDomainModelTypeName(domainProjectNamespace).ToType();
				var aggregateParameterName = topLevelDomainModel.ClassificationKey.ToVariableName();

				methodParameters = new List<ParameterSyntax>
				{
					aggregateParameterName
							.ToParameter()
							.WithType(aggregateDomainModelType)
				};

				methodParameters.AddRange(transferParameter.Select(p => p.Name.ToParameter().WithType(p.Type)));

				var domainModelToTransfer = childDomainModel;
				var identifierVariableName = "";
				var addChildAggregateVariableInsteadOfChildVariable = false;
				TypeSyntax returnType = null;
				if (methodName == "Create")
				{
					identifierVariableName = $"{childDomainModel.AggregateDomainModel.ClassificationKey}Id".ToVariableName();
					methodParameters.Add(identifierVariableName.ToParameter().WithType(childDomainModel.AggregateDomainModel.IdentifierType.ToType()));
					domainModelToTransfer = childDomainModel.AggregateDomainModel;
					returnType = childDomainModelType.ToType();
				}
				else
				{
					identifierVariableName = $"{childDomainModel.ClassificationKey}Id".ToVariableName();
					methodParameters.Add(identifierVariableName.ToParameter().WithType(childDomainModel.IdentifierType.ToType()));
					addChildAggregateVariableInsteadOfChildVariable = true;
				}

				statements.AddRange(CreateCollectChildStatementsForReadByChildId(domainModelToTransfer, domainProjectNamespace, returnType, true, false, addChildAggregateVariableInsteadOfChildVariable, identifierVariableName.ToIdentifierName()));
			}
			else
			{
				(statements, methodParameters) = CreateCollectChildStatements(childDomainModel, transferParameter, domainProjectNamespace, false, false, null, returnAsTask);
			}

			var methodCallArguments = new List<ArgumentSyntax>
			{
				childDomainModel.AggregateDomainModel.ClassificationKey.ToVariableName().ToArgument()
			};

			methodCallArguments.AddRange(transferParameter.Select(p => p.Name.ToArgument()));

			statements.Add(
				$"{methodName}{childDomainModel.ClassificationKey}Async"
				.ToIdentifierName()
				.Call(methodCallArguments.ToArray())
				.Return()
			);

			var methodDeclarationName = $"{methodName}{childDomainModel.ClassificationKey}Async";
			var methodDeclaration = methodDeclarationName.ToMethod(
				"Task".AsGeneric("ResponseData".AsGeneric(childDomainModelType)),
				statements,
				SyntaxKind.PrivateKeyword
			);

			return (methodDeclarationName, methodDeclaration.WithParameter(methodParameters.ToArray()));
		}

		private static (List<StatementSyntax> Statements, List<ParameterSyntax> MethodParameter) CreateCollectChildStatements(
			ReferenceDomainModelMap childDomainModel,
			IEnumerable<(TypeSyntax Type, string Name)> transferParameter,
			string domainProjectNamespace,
			bool addTopLevelDomainModelVariable,
			bool addDomainModelVariable,
			TypeSyntax returnType,
			bool returnAsTask
		)
		{
			var statements = new List<StatementSyntax>();
			var topLevelDomainModel = childDomainModel.GetTopLevelDomainModel();

			var aggregateDomainModelType = topLevelDomainModel.GetDomainModelTypeName(domainProjectNamespace).ToType();
			var aggregateParameterName = topLevelDomainModel.ClassificationKey.ToVariableName();
			var childVariableName = childDomainModel.ClassificationKey.ToVariableName();
			returnType ??= childDomainModel.GetDomainModelTypeName(domainProjectNamespace).ToType();

			var baseStatementCount = 0;
			if (addTopLevelDomainModelVariable)
			{
				baseStatementCount++;
				statements.Add(aggregateParameterName.ToVariableStatement($"{aggregateParameterName}Result".Access("Data")));
			}

			var methodParameters = new List<ParameterSyntax>
			{
				aggregateParameterName
						.ToParameter()
						.WithType(aggregateDomainModelType)
			};

			methodParameters.AddRange(transferParameter.Select(p => p.Name.ToParameter().WithType(p.Type)));
			var baseParameterCount = methodParameters.Count;

			var loopBeforeDomainModel = childDomainModel;
			var loopDomainModel = addDomainModelVariable
				? childDomainModel
				: childDomainModel.AggregateDomainModel;

			while (loopDomainModel.IsChildDomainModel)
			{
				var loopModelVariableName = loopDomainModel.ClassificationKey.ToVariableName();
				var loopModelResult = $"{loopModelVariableName}Result";

				methodParameters.Insert(baseParameterCount,
					$"{loopModelVariableName}Id"
						.ToParameter()
						.WithType(loopDomainModel.IdentifierType.ToType())
				);

				statements.Insert(baseStatementCount + 0,
					loopModelResult
					.ToVariableStatement(
						loopDomainModel.AggregateDomainModel
						.ClassificationKey
						.ToVariableName()
						.Access($"Get{loopDomainModel.ChildEnumerableName}")
						.Call($"{loopModelVariableName}Id".ToArgument())
					)
				);

				statements.Insert(baseStatementCount + 1,
					loopModelResult.ToIdentifierName().ToFaultyCheck(returnType, returnAsTask)
				);

				statements.Insert(baseStatementCount + 2,
					loopModelVariableName.ToVariableStatement(loopModelResult.Access("Data"))
				);

				loopBeforeDomainModel = loopDomainModel;
				loopDomainModel = loopDomainModel.AggregateDomainModel;
			}

			return (statements, methodParameters);
		}

		private static bool AddReferenceUsageCheck(
			List<StatementSyntax> statements,
			Dictionary<string, ImmutableHashSet<string>> domainModelsExcludedFromForeignKeyCheck,
			Dictionary<string, ImmutableHashSet<string>> domainModelsToSkip,
			ReferenceDomainModel domainModel,
			string projectFullQualifiedNamespace,
			ExpressionSyntax primaryKeyVariable
		)
		{
			if (domainModelsExcludedFromForeignKeyCheck.ContainsKey(domainModel.Domain)
				&& domainModelsExcludedFromForeignKeyCheck[domainModel.Domain].Contains(domainModel.DomainModelName))
			{
				return false;
			}

			if (domainModelsToSkip.ContainsKey(domainModel.Domain)
				&& domainModelsToSkip[domainModel.Domain].Contains(domainModel.DomainModelName))
			{
				return false;
			}

			var queryProviderType = domainModel.ClassificationKey.ToQueryProviderType();
			var queryProviderName = domainModel.ClassificationKey.ToQueryProviderName();
			var queryProviderField = queryProviderName.ToFieldName();

			var resultName = $"{domainModel.ClassificationKey.ToVariableName()}{domainModel.PropertyName}Result";

			StatementHelpers.AddAsyncMethodCallAndFaultyCheck(statements, queryProviderField, domainModel.IsUsedMethodName, resultName, (TypeSyntax)null, primaryKeyVariable);

			statements.Add(
				resultName
				.ToIdentifierName()
				.Access("Data")
				.If(
					SyntaxConstants.ResponseDataBool
					.CreateFaultyResponse(EshavaMessageConstant.StillAssigned.Map())
					.Return()
				)
			);

			return true;
		}

		private static List<(string Name, MemberDeclarationSyntax Method)> CreateCreateChildAndSubChildMethods(
			UseCaseTemplateRequest request,
			ReferenceDomainModelMap childDomainModel,
			ReferenceDtoMap dtoMap,
			string aggregateParameterName,
			TypeSyntax aggregateParameterType,
			List<(ForeignKeyCache ForeignKey, ApplicationUseCaseDtoProperty Property)> dtoForeignKeyReferences,
			ForeignKeyReferenceContainer foreignKeyReferenceContainer,
			HashSet<string> domainModelWithMappings,
			bool skipForeignKeyHashsetParameter,
			bool skipCreateMethod
		)
		{
			var methodDeclarations = new List<(string Name, MemberDeclarationSyntax Method)>();

			if (!skipCreateMethod)
			{
				var childVariableName = childDomainModel.ClassificationKey.ToVariableName();

				var createChildResultVariable = $"create{childDomainModel.ClassificationKey}Result";
				var additionalCreateChildStatements = CreateSubChildModelStatements(request, childDomainModel, createChildResultVariable, childVariableName);

				methodDeclarations.Add(
					CreateCreateChildMethod(
						childDomainModel,
						dtoMap,
						aggregateParameterName,
						aggregateParameterType,
						childVariableName,
						request.DomainProjectNamespace,
						dtoForeignKeyReferences,
						domainModelWithMappings,
						skipForeignKeyHashsetParameter,
						createChildResultVariable,
						additionalCreateChildStatements
					)
				);

				if (childDomainModel.DomainModel.HasValidationRules)
				{
					methodDeclarations.Add(CreateCheckValidationConstraintsMethod(request.UseCase, request.Domain, childDomainModel, dtoMap.Dto));
				}
			}

			if (childDomainModel.IsAggregate && childDomainModel.ChildDomainModels.Count > 0)
			{
				methodDeclarations.AddRange(CreateCreateChildsMethods(request, childDomainModel, foreignKeyReferenceContainer, domainModelWithMappings, skipForeignKeyHashsetParameter, false));
			}

			return methodDeclarations;
		}

		private static List<StatementSyntax> CreateSubChildModelStatements(UseCaseTemplateRequest request, ReferenceDomainModelMap childDomainModel, string createChildResultVariable, string childVariableName)
		{
			var additionalCreateChildStatements = new List<StatementSyntax>();

			if (childDomainModel.IsAggregate && childDomainModel.ChildDomainModels.Count > 0)
			{
				if (!request.DtoReferenceMap.TryGetDtoByDomainModel(request.Domain, request.UseCase.UseCaseName, request.UseCase.NamespaceClassificationKey, childDomainModel.DomainModelName, out var dtoMap))
				{
					return additionalCreateChildStatements;
				}

				var childDomainModelType = childDomainModel.GetDomainModelTypeName(request.DomainProjectNamespace);
				AddCreateChildModelsStatements(childDomainModel, dtoMap, additionalCreateChildStatements, childDomainModelType, createChildResultVariable, childVariableName.ToIdentifierName());
			}

			return additionalCreateChildStatements;
		}

		private static void AddUniqueCheck(
			List<StatementSyntax> statements,
			ReferenceDomainModelMap domainModel,
			DomainModelProperty property,
			DomainModelPropertyValidationRule rule,
			string provider,
			string dtoVariableName,
			ApplicationUseCaseDto dto,
			ApplicationUseCaseDtoProperty dtoProperty
		)
		{
			var arguments = new List<ExpressionSyntax>
			{
				Eshava.CodeAnalysis.SyntaxConstants.Null,
				dtoVariableName.Access(dtoProperty.Name)
			};

			var relatedProperties = new List<(string Domain, string Dto)>();
			var missingRelatedProperty = new List<string>();
			if (rule.RelatedProperties.Count > 0)
			{
				foreach (var relatedProperty in rule.RelatedProperties)
				{
					var relatedDtoProperty = dto.Properties.FirstOrDefault(p => !p.ReferenceProperty.IsNullOrEmpty() && p.ReferenceProperty == relatedProperty)
						?? dto.Properties.FirstOrDefault(p => p.Name == relatedProperty);

					if (relatedDtoProperty is null)
					{
						missingRelatedProperty.Add($"Missing related validation property in dto: {relatedProperty}");

						continue;
					}

					relatedProperties.Add((relatedProperty, relatedDtoProperty.Name));
					arguments.Add(dtoVariableName.Access(relatedDtoProperty.Name));
				}
			}

			if (missingRelatedProperty.Count > 0)
			{
				statements.Add(missingRelatedProperty.CreateCommentStatement());

				return;
			}

			var resultName = $"isUnique{property.Name}Result";
			StatementHelpers.AddAsyncMethodCallAndFaultyCheck(statements, provider, $"IsUnique{property.Name}Async", resultName, (TypeSyntax)null, arguments.ToArray());

			var uniqueFaultyResult = SyntaxConstants.ResponseDataBool.CreateFaultyResponse(
				EshavaMessageConstant.InvalidDataError.Map(),
				(property.Name, "Unique", dtoVariableName.Access(dtoProperty.Name))
			);

			foreach (var relatedProperty in relatedProperties)
			{
				uniqueFaultyResult = uniqueFaultyResult
					.AddValidationError(relatedProperty.Domain, "Unique", dtoVariableName.Access(relatedProperty.Dto));
			}

			statements.Add(
				resultName
				.Access("Data")
				.Not()
				.If(uniqueFaultyResult.Return())
			);
		}
	}
}