using System;
using System.Collections.Generic;
using System.Linq;
using Eshava.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Constants;
using Eshava.DomainDrivenDesign.CodeAnalysis.Enums;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Extensions
{
	public static class StringExtensions
	{

		private static readonly string[] _uncountable =
		[
			"information",
			"history",
			"data",
		];

		internal static bool IsNullOrEmpty(this string value)
		{
			return String.IsNullOrEmpty(value);
		}

		public static bool IsReserved(this string variableName)
		{
			return Keywords.Reserved.Contains(variableName);
		}

		public static string EscapeReservedName(this string variableName)
		{
			if (!variableName.IsReserved())
			{
				return variableName;
			}

			return $"@{variableName}";
		}

		public static string ToVariableName(this string typeName)
		{
			var variableName = typeName[0].ToString().ToLower() + typeName.Substring(1);

			if (variableName.IsReserved())
			{
				variableName = $"@{variableName}";
			}

			return variableName;
		}

		public static string ToPropertyName(this string variableName)
		{
			if (variableName.StartsWith("@"))
			{
				variableName = variableName.Substring(1);
			}

			var propertyName = variableName[0].ToString().ToUpper() + variableName.Substring(1);

			return propertyName;
		}

		public static string ToFieldName(this string typeName)
		{
			return "_" + typeName.ToVariableName();
		}

		public static bool IsUncountable(this string name)
		{
			if (name.IsNullOrEmpty())
			{
				return true;
			}

			var nameToLower = name.ToLower();

			return _uncountable.Any(name.EndsWith);
		}

		public static string ToPlural(this string name, string uncountableAppendix = null)
		{
			if (name.IsUncountable())
			{
				return uncountableAppendix.IsNullOrEmpty()
					? name
					: $"{name}{uncountableAppendix}";
			}

			if (name.EndsWith("ss"))
			{
				return name + "es";
			}

			if (name.EndsWith("y"))
			{
				return name.Substring(0, name.Length - 1) + "ies";
			}

			return name + "s";
		}

		public static ExpressionSyntax CreateInternalServerError(this string type, EshavaMessageConstant message, bool wrapTypeInIEnumerable = false)
		{
			var returnType = wrapTypeInIEnumerable
				? CommonNames.RESPONSEDATA.AsGeneric("IEnumerable".AsGeneric(type))
				: CommonNames.RESPONSEDATA.AsGeneric(type);

			return returnType
				.Access("CreateInternalServerError")
				.Call(
					message.Map().ToArgument(),
					"ex".ToArgument(),
					"messageGuid".ToArgument()
				);
		}

		public static ExpressionSyntax LogError(this string loggerVariable, string scopedSettings, ExpressionSyntax message, AnonymousObjectCreationExpressionSyntax additional = null)
		{
			var arguments = new List<ArgumentSyntax>
			{
				Eshava.CodeAnalysis.SyntaxConstants.This.ToArgument(),
				scopedSettings.ToArgument(),
				message.ToArgument(),
				"ex".ToArgument(),
			};

			if (additional is not null)
			{
				arguments.Add(additional.ToArgument().WithName("additional"));
			}

			return loggerVariable
				.Access("LogError")
				.Call(arguments.ToArray());
		}

		public static StatementSyntax ToNullCheck(this string result, string returnType)
		{
			return result
				.Access("Data")
				.IsNull()
				.If(
					"MessageConstants"
					.Access("NOTEXISTING")
					.Access(
						"ToFaultyResponse"
						.AsGeneric(returnType)
					)
					.Call()
					.Return()
				);
		}

		public static StatementSyntax ToFaultyCheck(this string result, string returnType = null, bool asTask = false)
		{
			return result.ToFaultyCheck(returnType.IsNullOrEmpty() ? null : returnType.ToType(), asTask);
		}

		public static StatementSyntax ToFaultyCheck(this string result, TypeSyntax returnType = null, bool asTask = false)
		{
			return result.ToIdentifierName().ToFaultyCheck(returnType, asTask);
		}

		public static List<StatementSyntax> CreateCatchBlock(this string returnDataType, ExpressionSyntax message, EshavaMessageConstant errorType, bool referencesAreFields, params (ExpressionSyntax Property, string Name)[] additionalData)
		{
			return StatementHelpers.CreateCatchBlock(returnDataType, message, errorType, referencesAreFields, additionalData);
		}

		public static IdentifierNameSyntax ToRepositoryType(this string domainModelName)
		{
			return $"I{domainModelName}{Constants.CommonNames.Instrastructure.REPOSITORY}".ToIdentifierName();
		}

		public static IdentifierNameSyntax ToQueryRepositoryType(this string domainModelName)
		{
			return $"I{domainModelName}{Constants.CommonNames.Instrastructure.QUERYREPOSITORY}".ToIdentifierName();
		}

		public static string ToRepositoryName(this string domainModelName)
		{
			return $"{domainModelName.ToVariableName()}{Constants.CommonNames.Instrastructure.REPOSITORY}";
		}

		public static string ToQueryRepositoryName(this string domainModelName)
		{
			return $"{domainModelName.ToVariableName()}{Constants.CommonNames.Instrastructure.QUERYREPOSITORY}";
		}

		public static IdentifierNameSyntax ToProviderType(this string domainModelName)
		{
			return $"I{domainModelName}{Constants.CommonNames.Instrastructure.PROVIDER}".ToIdentifierName();
		}

		public static IdentifierNameSyntax ToQueryProviderType(this string domainModelName)
		{
			return $"I{domainModelName}{Constants.CommonNames.Instrastructure.QUERYPROVIDER}".ToIdentifierName();
		}

		public static string ToProviderName(this string domainModelName)
		{
			return $"{domainModelName.ToVariableName()}{Constants.CommonNames.Instrastructure.PROVIDER}";
		}

		public static string ToQueryProviderName(this string domainModelName)
		{
			return $"{domainModelName.ToVariableName()}{Constants.CommonNames.Instrastructure.QUERYPROVIDER}";
		}

		public static string GetDomainModelTypeName(this string domainModelName, string domain, string classificationKey, string featureName, string domainProjectNamespace)
		{
			if (!Eshava.DomainDrivenDesign.CodeAnalysis.Models.Domain.DomainModel.CheckIsNamespaceDirectoryUncountable(featureName, classificationKey))
			{
				return domainModelName;
			}

			return domainProjectNamespace.IsNullOrEmpty()
				? $"{domain}.{classificationKey}.{domainModelName}"
				: $"{domainProjectNamespace}.{domain}.{classificationKey}.{domainModelName}";
		}

		public static string GetCommandsNamespace(this string classificationKey, string domain, string featureName, string applicationProjectNamespace)
		{
			if (!featureName.IsNullOrEmpty())
			{
				featureName += ".";
			}

			return $"{applicationProjectNamespace}.{domain}.{featureName}{classificationKey.ToPlural()}.Commands";
		}

		public static string GetQueriesNamespace(this string classificationKey, string domain, string featureName, string applicationProjectNamespace)
		{
			if (!featureName.IsNullOrEmpty())
			{
				featureName += ".";
			}

			return $"{applicationProjectNamespace}.{domain}.{featureName}{classificationKey.ToPlural()}.Queries";
		}

		public static ReturnStatementSyntax ToNoContentResponseDataReturn(this string returnDataType)
		{
			return returnDataType
				.ToIdentifierName()
				.ToInstance()
				.Access(Constants.CommonNames.Extensions.TORESPONSEDATA)
				.Call(
					"HttpStatusCode"
					.Access("NoContent")
					.ToArgument()
				)
				.Return();
		}

		public static LocalDeclarationStatementSyntax CreatePatchVariable(this string propertyPatchName, string propertyName, ExpressionSyntax patches)
		{
			return propertyPatchName
				.ToVariableStatement(
					patches
					.Access("FirstOrDefault")
					.Call(
						"p"
						.ToParameterExpression()
						.WithExpressionBody(
							"p"
							.Access("PropertyName")
							.ToEquals(propertyName.ToLiteralString())
						)
						.ToArgument()
					)
				);
		}

		public static TupleElementSyntax ToPropertyExpressionTupleElement(this string tupleItemName, string typeName)
		{
			return "Expression"
				.AsGeneric(
					"Func"
					.AsGeneric(
						typeName.ToIdentifierName(),
						Eshava.CodeAnalysis.SyntaxConstants.Object
					)
				)
				.ToTupleElement()
				.WithIdentifier(tupleItemName.ToIdentifier()
			);
		}

		public static (string FieldName, FieldDeclarationSyntax Declaration) CreateModelConstantField(this string modelName)
		{
			var aliasName = modelName.ToVariableName();
			if (aliasName == "user" || aliasName == "permission")
			{
				aliasName += "s";
			}

			var constantName = GetModelConstantName(modelName);

			return (constantName, constantName.ToConstField(Eshava.CodeAnalysis.SyntaxConstants.String, aliasName.ToLiteralString()));
		}

		public static string GetModelConstantName(this string modelName)
		{
			return modelName.ToUpper();
		}
	}
}