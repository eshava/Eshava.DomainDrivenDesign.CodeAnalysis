using System.Collections.Generic;
using System.Linq;
using Eshava.CodeAnalysis;
using Eshava.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Enums;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Eshava.DomainDrivenDesign.CodeAnalysis
{
	public static class StatementHelpers
	{
		public static ExpressionSyntax GetResponseData(bool returnValue, bool @async = false)
		{
			var literal = returnValue
				? SyntaxConstants.True
				: SyntaxConstants.False;

			var method = async
				? Constants.CommonNames.Extensions.TORESPONSEDATAASYNC
				: Constants.CommonNames.Extensions.TORESPONSEDATA;

			return literal.Access(method).Call();
		}

		public static ReturnStatementSyntax GetResponseDataReturn(bool returnValue, bool @async = false)
		{
			return GetResponseData(returnValue, @async).Return();
		}

		public static void AddResponseDataReturn(List<StatementSyntax> statements, bool returnValue, bool @async = false)
		{
			statements.Add(GetResponseDataReturn(returnValue, @async));
		}

		public static void AddLocalAsyncMethodCallAndFaultyCheck(List<StatementSyntax> statements, string methodName, string resultName, string returnDataType, bool createVariable, params ExpressionSyntax[] methodArguments)
		{
			AddMethodCallAndFaultyCheck(
				statements,
				null,
				methodName,
				null,
				true,
				false,
				resultName,
				returnDataType.IsNullOrEmpty() ? null : returnDataType.ToType(),
				createVariable,
				methodArguments
			);
		}

		public static void AddLocalAsyncMethodCallAndFaultyCheck(List<StatementSyntax> statements, string methodName, string resultName, string returnDataType, params ExpressionSyntax[] methodArguments)
		{
			AddMethodCallAndFaultyCheck(
				statements,
				null,
				methodName,
				null,
				true,
				false,
				resultName,
				returnDataType.IsNullOrEmpty() ? null : returnDataType.ToType(),
				true,
				methodArguments
			);
		}

		public static void AddAsyncMethodCallAndFaultyCheck(List<StatementSyntax> statements, string methodProvider, string methodName, string resultName, string returnDataType, params ExpressionSyntax[] methodArguments)
		{
			AddMethodCallAndFaultyCheck(
				statements,
				methodProvider.IsNullOrEmpty() ? null : methodProvider.ToIdentifierName(),
				methodName,
				null,
				true,
				false,
				resultName,
				returnDataType.IsNullOrEmpty() ? null : returnDataType.ToType(),
				true,
				methodArguments
			);
		}

		public static void AddAsyncMethodCallAndFaultyCheck(List<StatementSyntax> statements, ExpressionSyntax methodProvider, string methodName, string resultName, string returnDataType, params ExpressionSyntax[] methodArguments)
		{
			AddMethodCallAndFaultyCheck(
				statements,
				methodProvider,
				methodName,
				null,
				true,
				false,
				resultName,
				returnDataType.IsNullOrEmpty() ? null : returnDataType.ToType(),
				true,
				methodArguments
			);
		}

		public static void AddLocalMethodCallAndFaultyCheck(List<StatementSyntax> statements, string methodName, string resultName, string returnDataType, params ExpressionSyntax[] methodArguments)
		{
			AddMethodCallAndFaultyCheck(
				statements,
				null,
				methodName,
				null,
				false,
				false,
				resultName,
				returnDataType.IsNullOrEmpty() ? null : returnDataType.ToType(),
				true,
				methodArguments
			);
		}

		public static void AddMethodCallAndFaultyCheck(List<StatementSyntax> statements, string methodProvider, string methodName, string resultName, string returnDataType, params ExpressionSyntax[] methodArguments)
		{
			AddMethodCallAndFaultyCheck(
				statements,
				methodProvider.IsNullOrEmpty() ? null : methodProvider.ToIdentifierName(),
				methodName,
				null,
				false,
				false,
				resultName,
				returnDataType.IsNullOrEmpty() ? null : returnDataType.ToType(),
				true,
				methodArguments
			);
		}

		public static void AddMethodCallAndFaultyCheck(List<StatementSyntax> statements, ExpressionSyntax methodProvider, string methodName, string resultName, string returnDataType, params ExpressionSyntax[] methodArguments)
		{
			AddMethodCallAndFaultyCheck(
				statements,
				methodProvider,
				methodName,
				null,
				false,
				false,
				resultName,
				returnDataType.IsNullOrEmpty() ? null : returnDataType.ToType(),
				true,
				methodArguments
			);
		}

		public static void AddLocalAsyncMethodCallAndFaultyCheck(List<StatementSyntax> statements, string methodName, string resultName, TypeSyntax returnDataType, params ExpressionSyntax[] methodArguments)
		{
			AddMethodCallAndFaultyCheck(
				statements,
				null,
				methodName,
				null,
				true,
				false,
				resultName,
				returnDataType,
				true,
				methodArguments
			);
		}

		public static void AddAsyncMethodCallAndFaultyCheck(List<StatementSyntax> statements, string methodProvider, string methodName, string resultName, TypeSyntax returnDataType, params ExpressionSyntax[] methodArguments)
		{
			AddMethodCallAndFaultyCheck(
				statements,
				methodProvider.IsNullOrEmpty() ? null : methodProvider.ToIdentifierName(),
				methodName,
				null,
				true,
				false,
				resultName,
				returnDataType,
				true,
				methodArguments
			);
		}

		public static void AddAsyncMethodCallAndFaultyCheck(List<StatementSyntax> statements, ExpressionSyntax methodProvider, string methodName, string resultName, TypeSyntax returnDataType, params ExpressionSyntax[] methodArguments)
		{
			AddMethodCallAndFaultyCheck(
				statements,
				methodProvider,
				methodName,
				null,
				true,
				false,
				resultName,
				returnDataType,
				true,
				methodArguments
			);
		}

		public static void AddLocalMethodCallAndFaultyCheck(List<StatementSyntax> statements, string methodName, string resultName, TypeSyntax returnDataType, params ExpressionSyntax[] methodArguments)
		{
			AddMethodCallAndFaultyCheck(
				statements,
				null,
				methodName,
				null,
				false,
				false,
				resultName,
				returnDataType,
				true,
				methodArguments
			);
		}

		public static void AddMethodCallAndFaultyCheck(List<StatementSyntax> statements, string methodProvider, string methodName, string resultName, TypeSyntax returnDataType, bool asTask, params ExpressionSyntax[] methodArguments)
		{
			AddMethodCallAndFaultyCheck(
				statements,
				methodProvider.IsNullOrEmpty() ? null : methodProvider.ToIdentifierName(),
				methodName,
				null,
				false,
				asTask,
				resultName,
				returnDataType,
				true,
				methodArguments
			);
		}

		public static void AddMethodCallAndFaultyCheck(List<StatementSyntax> statements, ExpressionSyntax methodProvider, string methodName, string resultName, TypeSyntax returnDataType, params ExpressionSyntax[] methodArguments)
		{
			AddMethodCallAndFaultyCheck(
				statements,
				methodProvider,
				methodName,
				null,
				false,
				false,
				resultName,
				returnDataType,
				true,
				methodArguments
			);
		}

		public static void AddLocalAsyncMethodCallAndFaultyCheck(List<StatementSyntax> statements, string methodName, string[] genericMethodTypes, string resultName, string returnDataType, bool createVariable, params ExpressionSyntax[] methodArguments)
		{
			AddMethodCallAndFaultyCheck(
				statements,
				null,
				methodName,
				genericMethodTypes,
				true,
				false,
				resultName,
				returnDataType.IsNullOrEmpty() ? null : returnDataType.ToType(),
				createVariable,
				methodArguments
			);
		}

		public static void AddLocalAsyncMethodCallAndFaultyCheck(List<StatementSyntax> statements, string methodName, string[] genericMethodTypes, string resultName, string returnDataType, params ExpressionSyntax[] methodArguments)
		{
			AddMethodCallAndFaultyCheck(
				statements,
				null,
				methodName,
				genericMethodTypes,
				true,
				false,
				resultName,
				returnDataType.IsNullOrEmpty() ? null : returnDataType.ToType(),
				true,
				methodArguments
			);
		}

		public static void AddAsyncMethodCallAndFaultyCheck(List<StatementSyntax> statements, string methodProvider, string methodName, string[] genericMethodTypes, string resultName, string returnDataType, params ExpressionSyntax[] methodArguments)
		{
			AddMethodCallAndFaultyCheck(
				statements,
				methodProvider.IsNullOrEmpty() ? null : methodProvider.ToIdentifierName(),
				methodName,
				genericMethodTypes,
				true,
				false,
				resultName,
				returnDataType.IsNullOrEmpty() ? null : returnDataType.ToType(),
				true,
				methodArguments
			);
		}

		public static void AddAsyncMethodCallAndFaultyCheck(List<StatementSyntax> statements, ExpressionSyntax methodProvider, string methodName, string[] genericMethodTypes, string resultName, string returnDataType, params ExpressionSyntax[] methodArguments)
		{
			AddMethodCallAndFaultyCheck(
				statements,
				methodProvider,
				methodName,
				genericMethodTypes,
				true,
				false,
				resultName,
				returnDataType.IsNullOrEmpty() ? null : returnDataType.ToType(),
				true,
				methodArguments
			);
		}

		public static void AddLocalMethodCallAndFaultyCheck(List<StatementSyntax> statements, string methodName, string[] genericMethodTypes, string resultName, string returnDataType, params ExpressionSyntax[] methodArguments)
		{
			AddMethodCallAndFaultyCheck(
				statements,
				null,
				methodName,
				genericMethodTypes,
				false,
				false,
				resultName,
				returnDataType.IsNullOrEmpty() ? null : returnDataType.ToType(),
				true,
				methodArguments
			);
		}

		public static void AddMethodCallAndFaultyCheck(List<StatementSyntax> statements, string methodProvider, string methodName, string[] genericMethodTypes, string resultName, string returnDataType, params ExpressionSyntax[] methodArguments)
		{
			AddMethodCallAndFaultyCheck(
				statements,
				methodProvider.IsNullOrEmpty() ? null : methodProvider.ToIdentifierName(),
				methodName,
				genericMethodTypes,
				false,
				false,
				resultName,
				returnDataType.IsNullOrEmpty() ? null : returnDataType.ToType(),
				true,
				methodArguments
			);
		}

		public static void AddMethodCallAndFaultyCheck(List<StatementSyntax> statements, ExpressionSyntax methodProvider, string methodName, string[] genericMethodTypes, string resultName, string returnDataType, params ExpressionSyntax[] methodArguments)
		{
			AddMethodCallAndFaultyCheck(
				statements,
				methodProvider,
				methodName,
				genericMethodTypes,
				false,
				false,
				resultName,
				returnDataType.IsNullOrEmpty() ? null : returnDataType.ToType(),
				true,
				methodArguments
			);
		}

		public static void AddLocalAsyncMethodCallAndFaultyCheck(List<StatementSyntax> statements, string methodName, string[] genericMethodTypes, string resultName, TypeSyntax returnDataType, params ExpressionSyntax[] methodArguments)
		{
			AddMethodCallAndFaultyCheck(
				statements,
				null,
				methodName,
				genericMethodTypes,
				true,
				false,
				resultName,
				returnDataType,
				true,
				methodArguments
			);
		}

		public static void AddAsyncMethodCallAndFaultyCheck(List<StatementSyntax> statements, string methodProvider, string methodName, string[] genericMethodTypes, string resultName, TypeSyntax returnDataType, params ExpressionSyntax[] methodArguments)
		{
			AddMethodCallAndFaultyCheck(
				statements,
				methodProvider.IsNullOrEmpty() ? null : methodProvider.ToIdentifierName(),
				methodName,
				genericMethodTypes,
				true,
				false,
				resultName,
				returnDataType,
				true,
				methodArguments
			);
		}

		public static void AddAsyncMethodCallAndFaultyCheck(List<StatementSyntax> statements, ExpressionSyntax methodProvider, string methodName, string[] genericMethodTypes, string resultName, TypeSyntax returnDataType, params ExpressionSyntax[] methodArguments)
		{
			AddMethodCallAndFaultyCheck(
				statements,
				methodProvider,
				methodName,
				genericMethodTypes,
				true,
				false,
				resultName,
				returnDataType,
				true,
				methodArguments
			);
		}

		public static void AddLocalMethodCallAndFaultyCheck(List<StatementSyntax> statements, string methodName, string[] genericMethodTypes, string resultName, TypeSyntax returnDataType, params ExpressionSyntax[] methodArguments)
		{
			AddMethodCallAndFaultyCheck(
				statements,
				null,
				methodName,
				genericMethodTypes,
				false,
				false,
				resultName,
				returnDataType,
				true,
				methodArguments
			);
		}

		public static void AddMethodCallAndFaultyCheck(List<StatementSyntax> statements, string methodProvider, string methodName, string[] genericMethodTypes, string resultName, TypeSyntax returnDataType, params ExpressionSyntax[] methodArguments)
		{
			AddMethodCallAndFaultyCheck(
				statements,
				methodProvider.IsNullOrEmpty() ? null : methodProvider.ToIdentifierName(),
				methodName,
				genericMethodTypes,
				false,
				false,
				resultName,
				returnDataType,
				true,
				methodArguments
			);
		}

		public static void AddMethodCallAndFaultyCheck(List<StatementSyntax> statements, ExpressionSyntax methodProvider, string methodName, string[] genericMethodTypes, string resultName, TypeSyntax returnDataType, params ExpressionSyntax[] methodArguments)
		{
			AddMethodCallAndFaultyCheck(
				statements,
				methodProvider,
				methodName,
				genericMethodTypes,
				false,
				false,
				resultName,
				returnDataType,
				true,
				methodArguments
			);
		}

		public static void AddLocalMethodCall(List<StatementSyntax> statements, string methodName, string[] genericMethodTypes, string resultName, params ExpressionSyntax[] methodArguments)
		{
			AddMethodCall(
				statements,
				null,
				methodName,
				genericMethodTypes,
				false,
				resultName,
				true,
				methodArguments
			);
		}

		public static void AddLocalMethodCallAsync(List<StatementSyntax> statements, string methodName, string[] genericMethodTypes, string resultName, params ExpressionSyntax[] methodArguments)
		{
			AddMethodCall(
				statements,
				null,
				methodName,
				genericMethodTypes,
				true,
				resultName,
				true,
				methodArguments
			);
		}

		public static void AddMethodCall(List<StatementSyntax> statements, ExpressionSyntax methodProvider, string methodName, string[] genericMethodTypes, string resultName, params ExpressionSyntax[] methodArguments)
		{
			AddMethodCall(
				statements,
				methodProvider,
				methodName,
				genericMethodTypes,
				false,
				resultName,
				true,
				methodArguments
			);
		}

		public static void AddMethodCallAsync(List<StatementSyntax> statements, ExpressionSyntax methodProvider, string methodName, string[] genericMethodTypes, string resultName, params ExpressionSyntax[] methodArguments)
		{
			AddMethodCall(
				statements,
				methodProvider,
				methodName,
				genericMethodTypes,
				true,
				resultName,
				true,
				methodArguments
			);
		}

		public static ExpressionSyntax GetMethodCall(ExpressionSyntax methodProvider, string methodName, params ExpressionSyntax[] methodArguments)
		{
			return GetMethodCall(methodProvider, methodName, null, false, methodArguments);
		}

		public static ExpressionSyntax GetMethodCall(ExpressionSyntax methodProvider, string methodName, string[] genericMethodTypes, params ExpressionSyntax[] methodArguments)
		{
			return GetMethodCall(methodProvider, methodName, genericMethodTypes, false, methodArguments);
		}

		public static ExpressionSyntax GetMethodCallAsync(ExpressionSyntax methodProvider, string methodName, params ExpressionSyntax[] methodArguments)
		{
			return GetMethodCall(methodProvider, methodName, null, true, methodArguments);
		}

		public static ExpressionSyntax GetMethodCallAsync(ExpressionSyntax methodProvider, string methodName, string[] genericMethodTypes, params ExpressionSyntax[] methodArguments)
		{
			return GetMethodCall(methodProvider, methodName, genericMethodTypes, true, methodArguments);
		}

		private static ExpressionSyntax GetMethodCall(ExpressionSyntax methodProvider, string methodName, string[] genericMethodTypes, bool isAsync, params ExpressionSyntax[] methodArguments)
		{
			ExpressionSyntax methodIdentifier = null;
			if (genericMethodTypes?.Any() ?? false)
			{
				var genericMethod = methodName.AsGeneric(genericMethodTypes);

				methodIdentifier = methodProvider is not null
					? methodProvider.Access(genericMethod)
					: genericMethod;
			}
			else if (methodProvider is not null)
			{
				methodIdentifier = methodProvider.Access(methodName);
			}
			else if (methodIdentifier is null)
			{
				methodIdentifier = methodName.ToIdentifierName();
			}

			ExpressionSyntax call = methodIdentifier
				.Call(
					methodArguments
					.Select(e => e.ToArgument())
					.ToArray()
				);

			if (isAsync)
			{
				call = call.Await();
			}

			return call;
		}

		private static void AddMethodCall(List<StatementSyntax> statements, ExpressionSyntax methodProvider, string methodName, string[] genericMethodTypes, bool isAsync, string resultName, bool createVariable, params ExpressionSyntax[] methodArguments)
		{
			var call = GetMethodCall(methodProvider, methodName, genericMethodTypes, isAsync, methodArguments);

			if (resultName.IsNullOrEmpty())
			{
				statements.Add(
					call.ToExpressionStatement()
				);
			}
			else if (createVariable)
			{
				statements.Add(
					resultName.ToVariableStatement(call)
				);
			}
			else
			{
				statements.Add(
					resultName
					.ToIdentifierName()
					.Assign(call)
					.ToExpressionStatement()
				);
			}
		}

		private static void AddMethodCallAndFaultyCheck(List<StatementSyntax> statements, ExpressionSyntax methodProvider, string methodName, string[] genericMethodTypes, bool isAsync, bool asTask, string resultName, TypeSyntax returnDataType, bool createVariable, params ExpressionSyntax[] methodArguments)
		{
			if (resultName.IsNullOrEmpty())
			{
				resultName = $"{methodName.ToVariableName()}Result";
			}

			if (isAsync && !methodName.EndsWith("Async"))
			{
				methodName += "Async";
			}

			AddMethodCall(
				statements,
				methodProvider,
				methodName,
				genericMethodTypes,
				isAsync,
				resultName,
				createVariable,
				methodArguments
			);

			statements.Add(resultName.ToFaultyCheck(returnDataType, !isAsync && asTask));
		}

		public static List<StatementSyntax> CreateCatchBlock(string returnDataType, ExpressionSyntax message, EshavaMessageConstant errorType, bool referencesAreFields, params (ExpressionSyntax Property, string Name)[] additionalData)
		{
			var catchBlockStatements = new List<StatementSyntax>();
			var logger = referencesAreFields ? "_logger" : "Logger";
			var scopedSettings = referencesAreFields ? "_scopedSettings" : "ScopedSettings";

			AnonymousObjectCreationExpressionSyntax additionalDeclaration = null;
			if (additionalData.Length > 0)
			{
				additionalDeclaration = SyntaxHelper.CreateAnonymousObject(additionalData);
			}

			catchBlockStatements.Add("messageGuid"
				.ToVariableStatement(
					logger.LogError(scopedSettings, message, additionalDeclaration)
				)
			);

			catchBlockStatements.Add(returnDataType
				.CreateInternalServerError(errorType)
				.Return()
			);

			return catchBlockStatements;
		}

		public static (List<StatementSyntax> Statements, ExpressionSyntax ProviderResult) ReadDomainModel(ApplicationUseCase useCase, ReferenceDomainModelMap domainModelMap, string provider, string returnDataType, bool forCreateAction)
		{
			var statements = new List<StatementSyntax>();
			var topLevelAggregate = domainModelMap.GetTopLevelDomainModel();
			var providerResult = $"{topLevelAggregate.ClassificationKey.ToVariableName()}Result";

			var requestEntityId = "request".Access($"{topLevelAggregate.ClassificationKey}Id");
			var entitiyIdForRead = requestEntityId;

			if (domainModelMap.IsChildDomainModel)
			{
				if (forCreateAction)
				{
					if (useCase.ReadAggregateByChildId)
					{
						requestEntityId = "request".Access($"{domainModelMap.AggregateDomainModel.ClassificationKey}Id");

						var aggregateIdResult = $"{topLevelAggregate.ClassificationKey.ToVariableName()}IdResult";
						var queryProvider = $"_{domainModelMap.AggregateDomainModel.ClassificationKey.ToVariableName()}QueryInfrastructureProviderService";

						AddAsyncMethodCallAndFaultyCheck(statements, queryProvider, $"Read{topLevelAggregate.ClassificationKey}IdAsync", aggregateIdResult, returnDataType, requestEntityId);

						entitiyIdForRead = aggregateIdResult.Access("Data");
					}
				}
				else
				{
					if (useCase.ReadAggregateByChildId)
					{
						requestEntityId = "request".Access($"{domainModelMap.ClassificationKey}Id");
						var aggregateIdResult = $"{topLevelAggregate.ClassificationKey.ToVariableName()}IdResult";
						var queryProvider = $"_{domainModelMap.ClassificationKey.ToVariableName()}QueryInfrastructureProviderService";

						AddAsyncMethodCallAndFaultyCheck(statements, queryProvider, $"Read{topLevelAggregate.ClassificationKey}IdAsync", aggregateIdResult, returnDataType, requestEntityId);

						entitiyIdForRead = aggregateIdResult.Access("Data");
					}
					else
					{
						entitiyIdForRead = "request".Access($"{topLevelAggregate.ClassificationKey}Id");
					}
				}
			}

			AddAsyncMethodCallAndFaultyCheck(statements, provider, "ReadAsync", providerResult, returnDataType, entitiyIdForRead);

			statements.Add(providerResult.ToNullCheck(returnDataType));

			return (statements, providerResult.Access("Data"));
		}

		public static void AddScoped(List<StatementSyntax> statements, List<DependencyInjection> dependencyInjections)
		{
			foreach (var dependencyInjection in dependencyInjections)
			{
				statements.Add(
					"services"
					.Access("AddScoped".AsGeneric(dependencyInjection.Interface, dependencyInjection.Class))
					.Call()
					.ToExpressionStatement()
				);
			}
		}
	}
}