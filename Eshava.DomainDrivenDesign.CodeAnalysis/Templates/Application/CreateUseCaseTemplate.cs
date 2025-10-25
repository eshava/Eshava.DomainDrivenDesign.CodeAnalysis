using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Eshava.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Constants;
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

			var baseType = CommonNames.Application.Abstracts.CREATEUSECASE.AsGeneric(request.UseCase.MainDto, domainModelTypeName, domainModelMap.IdentifierType).ToSimpleBaseType();
			var useCaseInterface = $"I{className}".ToType().ToSimpleBaseType();

			var unitInformation = new UnitInformation(className, request.UseCaseNamespace, addAssemblyComment: request.AddAssemblyCommentToFiles);
			unitInformation.AddClassModifier(SyntaxKind.InternalKeyword, SyntaxKind.PartialKeyword);
			unitInformation.AddContructorModifier(SyntaxKind.PublicKeyword);
			unitInformation.AddBaseType(baseType, useCaseInterface);

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

			unitInformation.AddScopedSettings(request.ScopedSettingsUsing, request.ScopedSettingsClass);
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

			unitInformation.AddMethod(
				TemplateMethods.CreateUseCaseMainMethod(
					request.UseCase,
					request.Domain,
					domainModelMap,
					request.DtoReferenceMap,
					request.DomainProjectNamespace,
					foreignKeyReferenceContainer,
					codeSnippets,
					CreateCreateMethodActions
				)
			);

			if (request.UseCase.AddValidationConfigurationMethod)
			{
				unitInformation.AddMethod(TemplateMethods.CreateValidationConfigurationMethod(request.UseCase));
			}

			var childCreateMethodsResult = TemplateMethods.CreateCreateChildsMethods(request, domainModelMap, foreignKeyReferenceContainer, true, true);
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

			foreach (var additionalContructorParameter in request.UseCase.AdditionalContructorParameter)
			{
				unitInformation.AddUsing(additionalContructorParameter.UsingForType);
				unitInformation.AddConstructorParameter(additionalContructorParameter.Name, additionalContructorParameter.Type);
			}

			CodeSnippetHelpers.AddConstructorParameters(unitInformation, codeSnippets);

			unitInformation.AddLogger(className);

			return unitInformation.CreateCodeString();
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

				StatementHelpers.AddMethodCallAndFaultyCheck(statements, domainModelName, "CreateEntity", createResult, returnDataType, dto, DomainNames.VALIDATION.ENGINE.ToFieldName().ToIdentifierName());
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