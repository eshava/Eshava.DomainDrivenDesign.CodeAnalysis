using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
	public static class DeactivateUseCaseTemplate
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
			var alternativeClass = request.AlternativeClasses.FirstOrDefault(ac => ac.Type == ApplicationUseCaseType.Delete);

			var baseType = alternativeClass is null
				? CommonNames.Application.Abstracts.DEACTIVATEUSECASE.AsGeneric(domainModelTypeName, domainModelMap.IdentifierType).ToSimpleBaseType()
				: alternativeClass.ClassName.AsGeneric(domainModelTypeName, domainModelMap.IdentifierType).ToSimpleBaseType();
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
			unitInformation.AddUsing(CommonNames.Namespaces.TASKS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.EXTENSIONS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.MODELS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Application.USECAES);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Domain.CONSTANTS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Domain.EXTENSIONS);

			TemplateMethods.AddDomainModelUsings(unitInformation, domainModelMap, request.DomainProjectNamespace, request.Domain);

			var scopedSettingsTargetType = ParameterTargetTypes.Field;
			if (alternativeClass?.ConstructorParameters?.Any(cp => cp.Type == request.ScopedSettingsClass) ?? false)
			{
				scopedSettingsTargetType |= ParameterTargetTypes.Argument;
			}

			unitInformation.AddScopedSettings(request.ScopedSettingsUsing, request.ScopedSettingsClass, scopedSettingsTargetType);

			unitInformation.AddUsing(provider.Using);
			unitInformation.AddConstructorParameter(provider.Name, provider.Type);

			foreach (var queryProvider in queryProviders)
			{
				unitInformation.AddUsing(queryProvider.Using);
				unitInformation.AddConstructorParameter(queryProvider.Name, queryProvider.Type);
			}

			unitInformation.AddMethod(
				TemplateMethods.CreateUseCaseMainMethod(
					request.UseCase,
					null,
					domainModelMap,
					null,
					null,
					null,
					null,
					codeSnippets,
					CreateDeactivateMethodActions
				)
			);

			if (request.UseCase.CheckForeignKeyReferencesAutomatically && domainModelMap.HasReferencesToMe)
			{
				unitInformation.AddMethod(CreateIsDeleteableAutoGenMethod(request, domainModelMap, unitInformation));
			}

			if (request.UseCase.DeactivateBefore.Count > 0)
			{
				unitInformation.AddMethod(CreateExecuteBeforeAutoGenMethod(request, domainModelMap, unitInformation));
			}

			if (request.UseCase.CheckForeignKeyReferencesAutomatically)
			{
				TemplateMethods.AddReferenceUsageChecks(unitInformation, request.ApplicationProjectNamespace, request.UseCasesMap, request.UseCase, domainModelMap);
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

		/// <summary>
		/// Method to map the actual method
		/// </summary>
		/// <param name="useCase"></param>
		/// <param name="domain"></param>
		/// <param name="domainModelMap"></param>
		/// <param name="dtoReferenceMap"></param>
		/// <param name="returnDataType"></param>
		/// <param name="provider"></param>
		/// <param name="domainModelId"></param>
		/// <param name="domainProjectNamespace"></param>
		/// <param name="hasValidationRules"></param>
		/// <param name="foreignKeyReferenceContainer"></param>
		/// <returns></returns>
		private static List<StatementSyntax> CreateDeactivateMethodActions(
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
			return CreateDeactivateMethodActions(useCase, domainModelMap, returnDataType, provider, domainProjectNamespace, codeSnippets);
		}

		private static List<StatementSyntax> CreateDeactivateMethodActions(ApplicationUseCase useCase, ReferenceDomainModelMap domainModelMap, string returnDataType, string provider, string domainProjectNamespace, List<UseCaseCodeSnippet> codeSnippets)
		{
			var statements = new List<StatementSyntax>();
			var requestEntityId = "request".Access($"{useCase.ClassificationKey}Id");

			CodeSnippetHelpers.AddStatements(statements, returnDataType, codeSnippets);

			(var readStatements, var providerResult) = StatementHelpers.ReadDomainModel(useCase, domainModelMap, provider, returnDataType, false);

			statements.AddRange(readStatements);


			if (domainModelMap.IsChildDomainModel)
			{
				statements.AddRange(TemplateMethods.CreateCollectChildStatementsForDeactivate(domainModelMap, domainProjectNamespace, useCase.ResponseType.ToType(), useCase.ReadAggregateByChildId));
			}

			var modelReference = domainModelMap.IsChildDomainModel
					? domainModelMap.ClassificationKey.ToVariableName().ToIdentifierName()
					: providerResult;

			if (useCase.CheckForeignKeyReferencesAutomatically && domainModelMap.HasReferencesToMe)
			{
				StatementHelpers.AddLocalAsyncMethodCallAndFaultyCheck(statements, "IsDeletableAutoGen", null, returnDataType, modelReference);
			}

			StatementHelpers.AddLocalAsyncMethodCallAndFaultyCheck(statements, "IsDeletable", null, returnDataType, modelReference);


			if (domainModelMap.IsChildDomainModel)
			{
				StatementHelpers.AddMethodCallAndFaultyCheck(statements, modelReference, "Deactivate", "deactivateResult", returnDataType);
			}
			else
			{
				StatementHelpers.AddMethodCallAndFaultyCheck(statements, providerResult, "Deactivate", "deactivateResult", returnDataType);
			}

			if (useCase.DeactivateBefore.Count > 0)
			{
				StatementHelpers.AddLocalAsyncMethodCallAndFaultyCheck(statements, "ExecuteBeforeAutoGen", null, returnDataType, modelReference);
			}

			StatementHelpers.AddLocalAsyncMethodCallAndFaultyCheck(statements, "ExecuteBefore", null, returnDataType, modelReference);

			StatementHelpers.AddAsyncMethodCallAndFaultyCheck(statements, provider, "SaveAsync", "saveResult", returnDataType, providerResult);

			StatementHelpers.AddLocalAsyncMethodCallAndFaultyCheck(statements, "ExecuteAfter", null, returnDataType, modelReference);

			return statements;
		}

		private static (string Name, MemberDeclarationSyntax Method) CreateIsDeleteableAutoGenMethod(UseCaseTemplateRequest request, ReferenceDomainModelMap domainModel, UnitInformation unitInformation)
		{
			var statements = new List<StatementSyntax>();
			var primaryKeyVariable = domainModel.ClassificationKey.ToVariableName().Access("Id").Access("Value");

			request.UseCase.ExcludedFromForeignKeyCheck.ForEach(reference => reference.Domain = reference.Domain.IsNullOrEmpty() ? domainModel.Domain : reference.Domain);
			request.UseCase.DeactivateBefore.ForEach(reference => reference.Domain = reference.Domain.IsNullOrEmpty() ? domainModel.Domain : reference.Domain);

			var aggregateName = domainModel.IsAggregate
				? domainModel.ClassificationKey.ToVariableName().ToIdentifierName()
				: null;

			var hasAsyncMethodCalls = TemplateMethods.AddReferenceUsageChecks(request.UseCase, domainModel, statements, request.ApplicationProjectNamespace, primaryKeyVariable, aggregateName);

			statements.Add(StatementHelpers.GetResponseDataReturn(true, !hasAsyncMethodCalls));

			var accessModifier = new List<SyntaxKind>
			{
				SyntaxKind.PrivateKeyword
			};
			if (hasAsyncMethodCalls)
			{
				accessModifier.Add(SyntaxKind.AsyncKeyword);
			}

			var methodDeclarationName = "IsDeletableAutoGenAsync";
			var methodDeclaration = methodDeclarationName.ToMethod(
				SyntaxConstants.TaskResponseDataBool,
				statements,
				accessModifier.ToArray()
			);

			methodDeclaration = methodDeclaration
				.WithParameter(
					domainModel.ClassificationKey.ToVariableName()
					.ToParameter()
					.WithType(domainModel.GetDomainModelTypeName(request.DomainProjectNamespace).ToType())
				);

			return (methodDeclarationName, methodDeclaration);
		}

		private static (string Name, MemberDeclarationSyntax Method) CreateExecuteBeforeAutoGenMethod(UseCaseTemplateRequest request, ReferenceDomainModelMap domainModel, UnitInformation unitInformation)
		{
			var statements = new List<StatementSyntax>();
			var primaryKeyVariable = domainModel.DomainModelName.ToVariableName().Access("Id").Access("Value");

			foreach (var domainModelToDeactivate in request.UseCase.DeactivateBefore)
			{
				var referenceDomainModel = domainModel.ReferencesToMe.FirstOrDefault(r => r.DomainModelName == domainModelToDeactivate.Name);
				if (referenceDomainModel is null)
				{
					continue;
				}

				var providerFeatureName = request.UseCasesMap.GetFeatureName(referenceDomainModel.Domain, referenceDomainModel.ClassificationKey);
				var providerUsing = referenceDomainModel.ClassificationKey.GetCommandsNamespace(referenceDomainModel.Domain, providerFeatureName, request.ApplicationProjectNamespace);
				var providerName = domainModelToDeactivate.Name.ToProviderName();
				var providerType = domainModelToDeactivate.Name.ToProviderType();
				var providerFieldName = providerName.ToFieldName();
				var readMethodName = $"ReadFor{referenceDomainModel.PropertyName}Async";
				var resultName = $"{referenceDomainModel.DomainModelName.ToVariableName()}{referenceDomainModel.PropertyName}Result";
				var itemName = domainModelToDeactivate.Name.ToVariableName();

				unitInformation.AddUsing(providerUsing);
				unitInformation.AddConstructorParameter(providerName, providerType);

				StatementHelpers.AddAsyncMethodCallAndFaultyCheck(statements, providerFieldName, readMethodName, resultName, Eshava.CodeAnalysis.SyntaxConstants.Bool, primaryKeyVariable);

				var itemStatments = new List<StatementSyntax>();

				StatementHelpers.AddMethodCallAndFaultyCheck(itemStatments, itemName, "Deactivate", "deactivateResult", Eshava.CodeAnalysis.SyntaxConstants.Bool, false);
				StatementHelpers.AddAsyncMethodCallAndFaultyCheck(itemStatments, providerFieldName, "SaveAsync", "saveResult", Eshava.CodeAnalysis.SyntaxConstants.Bool, itemName.ToIdentifierName());

				statements.Add(resultName.Access("Data").ForEach(itemName, itemStatments));
			}

			statements.Add(StatementHelpers.GetResponseDataReturn(true));

			var methodDeclarationName = "ExecuteBeforeAutoGenAsync";
			var methodDeclaration = methodDeclarationName.ToMethod(
				SyntaxConstants.TaskResponseDataBool,
				statements,
				SyntaxKind.PrivateKeyword,
				SyntaxKind.AsyncKeyword
			);

			methodDeclaration = methodDeclaration
				.WithParameter(
					domainModel.DomainModelName.ToVariableName()
					.ToParameter()
					.WithType(domainModel.GetDomainModelTypeName(request.DomainProjectNamespace).ToType())
				);

			return (methodDeclarationName, methodDeclaration);
		}
	}
}