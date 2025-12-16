using System;
using System.Collections.Generic;
using System.Linq;
using Eshava.CodeAnalysis;
using Eshava.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Constants;
using Eshava.DomainDrivenDesign.CodeAnalysis.Enums;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Templates.Infrastructure
{
	public static class QueryRepositoryTemplate
	{
		public static string GetRepository(
			InfrastructureProject project,
			InfrastructureModel model,
			string domain,
			Dictionary<string, List<InfrastructureModel>> childsForAllModels,
			InfrastructureModels infrastructureModelsConfig,
			QueryProviderMap queryProviderMap,
			string fullQualifiedDomainNamespace,
			string fullQualifiedApplicationNamespace,
			string databaseSettingsInterface,
			string databaseSettingsInterfaceUsing
		)
		{
			NameAndType databaseSettings;
			if (databaseSettingsInterface.IsNullOrEmpty())
			{
				databaseSettings = CommonNames.DatabaseSettings.Parameter;
			}
			else
			{
				databaseSettings = new NameAndType(CommonNames.DatabaseSettings.SETTINGS, databaseSettingsInterface.ToType());
			}

			var @namespace = $"{fullQualifiedDomainNamespace}.{model.ClassificationKey.ToPlural()}";
			var className = $"{model.ClassificationKey}QueryRepository";

			var unitInformation = new UnitInformation(className, @namespace, addAssemblyComment: project.AddAssemblyCommentToFiles);
			unitInformation.AddClassModifier(SyntaxKind.InternalKeyword, SyntaxKind.PartialKeyword);
			unitInformation.AddContructorModifier(SyntaxKind.PublicKeyword);

			unitInformation.AddUsing(CommonNames.Namespaces.SYSTEM);
			unitInformation.AddUsing(CommonNames.Namespaces.TASKS);
			unitInformation.AddUsing(CommonNames.Namespaces.GENERIC);
			unitInformation.AddUsing(CommonNames.Namespaces.LINQ);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.EXTENSIONS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.MODELS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.Linq.INTERFACES);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Storm.NAME);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Storm.Linq.MODELS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Storm.MetaData.NAME);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Application.DTOS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Domain.CONSTANTS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Domain.ENUMS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Domain.EXTENSIONS);

			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Infrastructure.INTERFACES);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Infrastructure.PROVIDERS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Infrastructure.MODELS);
			unitInformation.AddUsing(project.AlternativeUsing);
			unitInformation.AddUsing(databaseSettingsInterfaceUsing);

			var alternativeClass = project.AlternativeClasses.FirstOrDefault(ac => ac.Type == InfrastructureAlternativeClassType.QueryRepository);

			var baseClass = alternativeClass?.ClassName;
			if (baseClass.IsNullOrEmpty())
			{
				baseClass = InfrastructureNames.ABSTRACTQUERYREPOSITORY;
				unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Infrastructure.REPOSITORIES);
			}
			else
			{
				unitInformation.AddUsing(alternativeClass.Using);
			}

			var baseType = baseClass.ToSimpleBaseType();
			var repositoryInterface = $"I{className}".ToType().ToSimpleBaseType();
			unitInformation.AddBaseType(baseType, repositoryInterface);

			var allModelsForDomain = infrastructureModelsConfig.Namespaces.First(ns => ns.Domain == domain).Models.ToList();
			var allModelsForDomainByClassificationKey = allModelsForDomain.GroupBy(m => m.ClassificationKey).ToDictionary(m => m.Key, m => m.ToList());
			var allModelsForDomainDic = allModelsForDomain.ToDictionary(m => m.Name, m => m);
			var childsForModel = childsForAllModels.ContainsKey(model.Name)
			? childsForAllModels[model.Name]
			: new List<InfrastructureModel>();

			var modelConstantNames = CreateModelConstantNames(model, childsForModel, allModelsForDomainDic, allModelsForDomainByClassificationKey, fullQualifiedDomainNamespace);
			foreach (var modelConstantName in modelConstantNames)
			{
				unitInformation.AddUsing(modelConstantName.Using);
				unitInformation.AddField(modelConstantName.Field);
			}

			var scopedSettingsTargetType = ParameterTargetTypes.Field;
			if (alternativeClass?.ConstructorParameters?.Any(cp => cp.Type == project.ScopedSettingsClass) ?? false)
			{
				scopedSettingsTargetType |= ParameterTargetTypes.Argument;
			}

			unitInformation.AddScopedSettings(project.ScopedSettingsUsing, project.ScopedSettingsClass, scopedSettingsTargetType);
			unitInformation.AddConstructorParameter(databaseSettings, ParameterTargetTypes.Argument);
			unitInformation.AddConstructorParameter(InfrastructureNames.Transform.Parameter, ParameterTargetTypes.Argument);
			CheckAndAddProviderReferences(unitInformation, alternativeClass);

			var fieldsForPropertyTypeMapping = new List<(string DataType, string FieldName, FieldDeclarationSyntax Declaration)>();

			foreach (var methodMap in queryProviderMap.Methods)
			{
				MethodCreationResult methodCreationResult = null;

				var addFieldsToMapping = false;
				switch (methodMap.Type)
				{
					case MethodType.Read:
						methodCreationResult = CreateReadMethod(model, methodMap, infrastructureModelsConfig, project.ImplementSoftDelete);

						break;
					case MethodType.Search:
						methodCreationResult = CreateSearchMethod(model, methodMap, infrastructureModelsConfig, project.ImplementSoftDelete);
						addFieldsToMapping = true;

						break;
					case MethodType.SearchCount:
						methodCreationResult = CreateSearchCountMethod(model, methodMap, infrastructureModelsConfig, project.ImplementSoftDelete);
						addFieldsToMapping = true;

						break;
					case MethodType.Exists:
						methodCreationResult = CreateExistsMethod(model, methodMap, project.ImplementSoftDelete);

						break;
					case MethodType.IsUnique:
						methodCreationResult = CreateIsUniqueMethod(model, methodMap, project.ImplementSoftDelete);

						break;
					case MethodType.IsUsedForeignKey:
						methodCreationResult = CreateIsUsedForeignKeyMethod(model, methodMap, project.ImplementSoftDelete);

						break;
					case MethodType.ReadAggregateId:

						if (model.ReferencedParent.IsNullOrEmpty())
						{
							// Workaround to allow multiple data models for the same classification key (see InfrastructureFactory)
							// Find the data model which contains the parent reference

							model = allModelsForDomainByClassificationKey[model.ClassificationKey].FirstOrDefault(m => !m.ReferencedParent.IsNullOrEmpty());
							if (model is null)
							{
								break;
							}
						}

						methodCreationResult = CreateReadAggregateIdMethod(model, methodMap, allModelsForDomainDic, project.ImplementSoftDelete);

						break;
				}

				if (methodCreationResult?.Method.Method is not null)
				{
					methodCreationResult.Usings.ForEach(unitInformation.AddUsing);
					unitInformation.AddMethod(methodCreationResult.Method);
					methodCreationResult.Fields?.ForEach(field => unitInformation.AddField(field.FieldName, field.Declaration));
					if (methodCreationResult.Fields?.Any() ?? false && addFieldsToMapping)
					{
						fieldsForPropertyTypeMapping.AddRange(methodCreationResult.Fields);
					}

				}
			}

			if (fieldsForPropertyTypeMapping.Count > 0)
			{
				CreatePropertyTypeMappings(
					unitInformation,
					fieldsForPropertyTypeMapping
						.GroupBy(f => f.DataType)
						.Select(f => f.First())
						.ToList()
				);
			}

			unitInformation.AddLogger(className, true);

			return unitInformation.CreateCodeString();
		}

		private static void CheckAndAddProviderReferences(UnitInformation unitInformation, InfrastructureProjectAlternativeClass alternativeClass)
		{
			if (!(alternativeClass?.ConstructorParameters?.Any() ?? false))
			{
				return;
			}

			foreach (var constructorParameter in alternativeClass.ConstructorParameters)
			{
				unitInformation.AddUsing(constructorParameter.UsingForType);
				unitInformation.AddConstructorParameter(constructorParameter.Name, constructorParameter.Type.ToIdentifierName(), Enums.ParameterTargetTypes.Argument);
			}
		}

		private static MethodCreationResult CreateExistsMethod(InfrastructureModel model, UseCaseQueryProviderMethodMap methodMap, bool implementSoftDelete)
		{
			var idParameter = methodMap.ParameterTypes.First();
			var modelConstant = model.Name.ToUpper();

			var interpolatedStringParts = new List<InterpolatedStringContentSyntax>
			{
				@"
					SELECT
						 COUNT(".Interpolate(),
				modelConstant.ToIdentifierName().Interpolate(),
				".".Interpolate(),
				Eshava.CodeAnalysis.SyntaxConstants.NameOf.Call(model.Name.Access("Id").ToArgument()).Interpolate(),
				@")
					FROM
						".Interpolate(),
				"TypeAnalyzer".Access("GetTableName".AsGeneric(model.Name)).Call().Interpolate(),
				" ".Interpolate(),
				modelConstant.ToIdentifierName().Interpolate(),
				@"
					WHERE
						".Interpolate(),
				modelConstant.ToIdentifierName().Interpolate(),
				".".Interpolate(),
				Eshava.CodeAnalysis.SyntaxConstants.NameOf.Call(model.Name.Access("Id").ToArgument()).Interpolate(),
				$@" = @{idParameter.Name.ToPropertyName()}".Interpolate()
			};

			if (implementSoftDelete)
			{
				interpolatedStringParts.Add($@"
					AND
						".Interpolate());
				interpolatedStringParts.Add(modelConstant.ToIdentifierName().Interpolate());
				interpolatedStringParts.Add(".".Interpolate());
				interpolatedStringParts.Add(Eshava.CodeAnalysis.SyntaxConstants.NameOf.Call(model.Name.Access("Status").ToArgument()).Interpolate());
				interpolatedStringParts.Add(@" = @Status
						".Interpolate());
			}

			var parameterItems = new List<(ExpressionSyntax Property, string Name)>
			{
				(idParameter.Name.ToIdentifierName(), idParameter.Name.ToPropertyName())
			};

			if (implementSoftDelete)
			{
				parameterItems.Add(("Status".Access("Active"), "Status"));
			}

			var parameterVariableDeclaration = SyntaxHelper.CreateAnonymousObject(parameterItems.ToArray());

			var usingInnerStatments = new List<StatementSyntax>
			{
				"result"
				.ToVariableStatement(
					"connection"
					.Access("ExecuteScalarAsync".AsGeneric("int"))
					.Call("query".ToArgument(), parameterVariableDeclaration.ToArgument())
					.Await()
				),
				"result"
				.ToIdentifierName()
				.GreaterThan("0".ToLiteralInt())
				.Parenthesize()
				.Access("ToResponseData")
				.Call()
				.Return()
			};

			var tryBlockStatements = new List<StatementSyntax>
			{
				"query".ToVariableStatement(interpolatedStringParts.ToRawStringExpression()),
				GetUsingStatement(usingInnerStatments)
			};

			var statements = new List<StatementSyntax>
			{
				tryBlockStatements.TryCatch(
					GetCatchBlock(
						model,
						$"Entity {model.Name} could not be checked",
						"bool",
						false,
						parameterItems.ToArray()
					)
				)
			};

			(var methodUsings, var method) = CreateMethod(model, methodMap, statements);

			return new MethodCreationResult
			{
				Usings = methodUsings,
				Method = method
			};
		}

		private static MethodCreationResult CreateIsUniqueMethod(InfrastructureModel model, UseCaseQueryProviderMethodMap methodMap, bool implementSoftDelete)
		{
			var idParameter = methodMap.ParameterTypes.First();
			var checkParameter = methodMap.ParameterTypes.Skip(1).First();
			var additionalParameters = methodMap.ParameterTypes.Count > 2
				? methodMap.ParameterTypes.Skip(2).ToList()
				: new List<UseCaseQueryProviderMethodParameterTypeMap>();

			var modelConstant = model.Name.ToUpper();

			var interpolatedStringParts = new List<InterpolatedStringContentSyntax>
			{
				@"
					SELECT
						 COUNT(".Interpolate(),
				modelConstant.ToIdentifierName().Interpolate(),
				".".Interpolate(),
				Eshava.CodeAnalysis.SyntaxConstants.NameOf.Call(model.Name.Access("Id").ToArgument()).Interpolate(),
				@")
					FROM
						".Interpolate(),
				"TypeAnalyzer".Access("GetTableName".AsGeneric(model.Name)).Call().Interpolate(),
				" ".Interpolate(),
				modelConstant.ToIdentifierName().Interpolate(),
				@"
					WHERE
						".Interpolate(),
				modelConstant.ToIdentifierName().Interpolate(),
				".".Interpolate(),
				Eshava.CodeAnalysis.SyntaxConstants.NameOf.Call(model.Name.Access(checkParameter.PropertyName).ToArgument()).Interpolate(),
				$@" = @{checkParameter.PropertyName}".Interpolate()
			};

			foreach (var parameter in additionalParameters)
			{
				interpolatedStringParts.Add($@"
					AND
						".Interpolate());
				interpolatedStringParts.Add(modelConstant.ToIdentifierName().Interpolate());
				interpolatedStringParts.Add(".".Interpolate());
				interpolatedStringParts.Add(Eshava.CodeAnalysis.SyntaxConstants.NameOf.Call(model.Name.Access(parameter.PropertyName).ToArgument()).Interpolate());
				interpolatedStringParts.Add($@" = @{parameter.PropertyName}
						".Interpolate());
			}

			if (implementSoftDelete)
			{
				interpolatedStringParts.Add($@"
					AND
						".Interpolate());
				interpolatedStringParts.Add(modelConstant.ToIdentifierName().Interpolate());
				interpolatedStringParts.Add(".".Interpolate());
				interpolatedStringParts.Add(Eshava.CodeAnalysis.SyntaxConstants.NameOf.Call(model.Name.Access("Status").ToArgument()).Interpolate());
				interpolatedStringParts.Add(@" = @Status
						".Interpolate());
			}

			var interpolatedStringIdParts = new List<InterpolatedStringContentSyntax>
			{
				@"
					AND
						".Interpolate(),
				modelConstant.ToIdentifierName().Interpolate(),
				".".Interpolate(),
				Eshava.CodeAnalysis.SyntaxConstants.NameOf.Call(model.Name.Access("Id").ToArgument()).Interpolate(),
				$@" != @{idParameter.PropertyName}".Interpolate()
			};

			var parameterItems = methodMap.ParameterTypes
				.Select(p => (Property: (ExpressionSyntax)p.Name.ToIdentifierName(), Name: p.PropertyName))
				.ToList();


			if (implementSoftDelete)
			{
				parameterItems.Add(("Status".Access("Active"), "Status"));
			}

			var parameterVariableDeclaration = SyntaxHelper.CreateAnonymousObject(parameterItems.ToArray());

			var usingInnerStatments = new List<StatementSyntax>
			{
				"result"
				.ToVariableStatement(
					"connection"
					.Access("ExecuteScalarAsync".AsGeneric("int"))
					.Call("query".ToArgument(), parameterVariableDeclaration.ToArgument())
					.Await()
				),
				"result"
				.ToIdentifierName()
				.ToEquals("0".ToLiteralInt())
				.Parenthesize()
				.Access("ToResponseData")
				.Call()
				.Return()
			};

			var tryBlockStatements = new List<StatementSyntax>
			{
				"query"
					.ToVariableStatement(interpolatedStringParts.ToRawStringExpression()),
				idParameter.Name
					.Access("HasValue")
					.If("query".ToIdentifierName().AddAssign(interpolatedStringIdParts.ToRawStringExpression()).ToExpressionStatement()),
				GetUsingStatement(usingInnerStatments)
			};

			var statements = new List<StatementSyntax>
			{
				tryBlockStatements.TryCatch(
					GetCatchBlock(
						model,
						$"Entity {model.Name} could not be checked",
						"bool",
						false,
						parameterItems.ToArray()
					)
				)
			};

			(var methodUsings, var method) = CreateMethod(model, methodMap, statements);

			return new MethodCreationResult
			{
				Usings = methodUsings,
				Method = method
			};
		}

		private static MethodCreationResult CreateReadAggregateIdMethod(InfrastructureModel childModel, UseCaseQueryProviderMethodMap methodMap, Dictionary<string, InfrastructureModel> allModelsForDomain, bool implementSoftDelete)
		{
			var parentQuery = new Queue<InfrastructureModel>();
			var loopModel = childModel;
			do
			{
				if (!allModelsForDomain.TryGetValue(loopModel.ReferencedParent, out var parentModel))
				{
					break;
				}

				parentQuery.Enqueue(parentModel);
				loopModel = parentModel;

				if (loopModel.ClassificationKey == methodMap.AggregateClassificationKey)
				{
					break;
				}

			} while (!loopModel.ReferencedParent.IsNullOrEmpty());

			var idParameter = methodMap.ParameterTypes.First();
			var childModelConstant = childModel.Name.ToUpper();
			var interpolatedQueryParts = new List<InterpolatedStringContentSyntax>
			{
				@"
					SELECT
						 ".Interpolate()
			};

			var interpolatedFromParts = new List<InterpolatedStringContentSyntax>();

			loopModel = childModel;
			do
			{
				var loopModelConstant = loopModel.Name.ToUpper();

				var parentModel = parentQuery.Dequeue();
				var parentModelConstant = parentModel.Name.ToUpper();
				var parentProperty = loopModel.Properties.First(p => p.ReferenceType == loopModel.ReferencedParent);

				if (parentQuery.Count == 0)
				{
					interpolatedQueryParts.Add(loopModelConstant.ToIdentifierName().Interpolate());
					interpolatedQueryParts.Add(".".Interpolate());
					interpolatedQueryParts.Add(Eshava.CodeAnalysis.SyntaxConstants.NameOf.Call(loopModel.Name.Access(parentProperty.Name).ToArgument()).Interpolate());
				}

				if (loopModel.Name == childModel.Name)
				{
					interpolatedFromParts.Add(@"
					FROM
						".Interpolate());
					interpolatedFromParts.Add("TypeAnalyzer".Access("GetTableName".AsGeneric(childModel.Name)).Call().Interpolate());
					interpolatedFromParts.Add(" ".Interpolate());
					interpolatedFromParts.Add(loopModelConstant.ToIdentifierName().Interpolate());
				}

				if (parentQuery.Count > 0)
				{
					interpolatedFromParts.Add(@"
					JOIN
						".Interpolate());
					interpolatedFromParts.Add("TypeAnalyzer".Access("GetTableName".AsGeneric(parentModel.Name)).Call().Interpolate());
					interpolatedFromParts.Add(" ".Interpolate());
					interpolatedFromParts.Add(parentModelConstant.ToIdentifierName().Interpolate());
					interpolatedFromParts.Add(" ON ".Interpolate());
					interpolatedFromParts.Add(parentModelConstant.ToIdentifierName().Interpolate());
					interpolatedFromParts.Add(".".Interpolate());
					interpolatedFromParts.Add(Eshava.CodeAnalysis.SyntaxConstants.NameOf.Call(parentModel.Name.Access("Id").ToArgument()).Interpolate());
					interpolatedFromParts.Add(" = ".Interpolate());
					interpolatedFromParts.Add(loopModelConstant.ToIdentifierName().Interpolate());
					interpolatedFromParts.Add(".".Interpolate());
					interpolatedFromParts.Add(Eshava.CodeAnalysis.SyntaxConstants.NameOf.Call(loopModel.Name.Access(parentProperty.Name).ToArgument()).Interpolate());

					if (implementSoftDelete)
					{
						interpolatedFromParts.Add($@"
					AND
						".Interpolate());
						interpolatedFromParts.Add(parentModelConstant.ToIdentifierName().Interpolate());
						interpolatedFromParts.Add(".".Interpolate());
						interpolatedFromParts.Add(Eshava.CodeAnalysis.SyntaxConstants.NameOf.Call(parentModel.Name.Access("Status").ToArgument()).Interpolate());
						interpolatedFromParts.Add(@" = @Status".Interpolate());
					}
				}

				loopModel = parentModel;
			} while (parentQuery.Count > 0);

			interpolatedQueryParts.AddRange(interpolatedFromParts);

			interpolatedQueryParts.Add(@"
					WHERE
						".Interpolate());
			interpolatedQueryParts.Add(childModelConstant.ToIdentifierName().Interpolate());
			interpolatedQueryParts.Add(".".Interpolate());
			interpolatedQueryParts.Add(Eshava.CodeAnalysis.SyntaxConstants.NameOf.Call(childModel.Name.Access("Id").ToArgument()).Interpolate());
			interpolatedQueryParts.Add($@" = @{idParameter.Name.ToPropertyName()}".Interpolate());

			if (implementSoftDelete)
			{
				interpolatedQueryParts.Add($@"
					AND
						".Interpolate());
				interpolatedQueryParts.Add(childModelConstant.ToIdentifierName().Interpolate());
				interpolatedQueryParts.Add(".".Interpolate());
				interpolatedQueryParts.Add(Eshava.CodeAnalysis.SyntaxConstants.NameOf.Call(childModel.Name.Access("Status").ToArgument()).Interpolate());
				interpolatedQueryParts.Add(@" = @Status
						".Interpolate());
			}
			else
			{
				interpolatedQueryParts.Add(@"
						".Interpolate());
			}

			var parameterItems = new List<(ExpressionSyntax Property, string Name)>
			{
				(idParameter.Name.ToIdentifierName(), idParameter.Name.ToPropertyName())
			};

			if (implementSoftDelete)
			{
				parameterItems.Add(("Status".Access("Active"), "Status"));
			}

			var parameterVariableDeclaration = SyntaxHelper.CreateAnonymousObject(parameterItems.ToArray());

			var usingInnerStatments = new List<StatementSyntax>
			{
				"result"
				.ToVariableStatement(
					"connection"
					.Access("ExecuteScalarAsync".AsGeneric(loopModel.IdentifierType))
					.Call("query".ToArgument(), parameterVariableDeclaration.ToArgument())
					.Await()
				),
				"result"
				.ToIdentifierName()
				.Access("ToResponseData")
				.Call()
				.Return()
			};

			var tryBlockStatements = new List<StatementSyntax>
			{
				"query".ToVariableStatement(interpolatedQueryParts.ToRawStringExpression()),
				GetUsingStatement(usingInnerStatments)
			};

			var statements = new List<StatementSyntax>
			{
				tryBlockStatements.TryCatch(
					GetCatchBlock(
						childModel,
						$"Entity {childModel.Name} could not be read",
						loopModel.IdentifierType,
						false,
						parameterItems.ToArray()
					)
				)
			};

			(var methodUsings, var method) = CreateMethod(childModel, methodMap, statements);

			return new MethodCreationResult
			{
				Usings = methodUsings,
				Method = method
			};
		}

		private static MethodCreationResult CreateIsUsedForeignKeyMethod(InfrastructureModel model, UseCaseQueryProviderMethodMap methodMap, bool implementSoftDelete)
		{
			var idParameter = methodMap.ParameterTypes.First();
			var modelConstant = model.Name.ToUpper();
			var foreignKeyProperty = model.Properties.First(p => p.ReferenceType == idParameter.ReferenceType && TemplateMethods.GetDomain(p.ReferenceDomain, methodMap.Domain) == idParameter.ReferenceDomain);

			var interpolatedStringParts = new List<InterpolatedStringContentSyntax>
			{
				@"
					SELECT
						 COUNT(".Interpolate(),
				modelConstant.ToIdentifierName().Interpolate(),
				".".Interpolate(),
				Eshava.CodeAnalysis.SyntaxConstants.NameOf.Call(model.Name.Access(foreignKeyProperty.Name).ToArgument()).Interpolate(),
				@")
					FROM
						".Interpolate(),
				"TypeAnalyzer".Access("GetTableName".AsGeneric(model.Name)).Call().Interpolate(),
				" ".Interpolate(),
				modelConstant.ToIdentifierName().Interpolate(),
				@"
					WHERE
						".Interpolate(),
				modelConstant.ToIdentifierName().Interpolate(),
				".".Interpolate(),
				Eshava.CodeAnalysis.SyntaxConstants.NameOf.Call(model.Name.Access(foreignKeyProperty.Name).ToArgument()).Interpolate(),
				$@" = @{idParameter.Name.ToPropertyName()}".Interpolate()
			};

			if (implementSoftDelete)
			{
				interpolatedStringParts.Add($@"
					AND
						".Interpolate());
				interpolatedStringParts.Add(modelConstant.ToIdentifierName().Interpolate());
				interpolatedStringParts.Add(".".Interpolate());
				interpolatedStringParts.Add(Eshava.CodeAnalysis.SyntaxConstants.NameOf.Call(model.Name.Access("Status").ToArgument()).Interpolate());
				interpolatedStringParts.Add(@" = @Status
						".Interpolate());
			}

			var parameterItems = new List<(ExpressionSyntax Property, string Name)>
			{
				(idParameter.Name.ToIdentifierName(), idParameter.Name.ToPropertyName())
			};

			if (implementSoftDelete)
			{
				parameterItems.Add(("Status".Access("Active"), "Status"));
			}

			var parameterVariableDeclaration = SyntaxHelper.CreateAnonymousObject(parameterItems.ToArray());

			var usingInnerStatments = new List<StatementSyntax>
			{
				"result"
				.ToVariableStatement(
					"connection"
					.Access("ExecuteScalarAsync".AsGeneric("int"))
					.Call("query".ToArgument(), parameterVariableDeclaration.ToArgument())
					.Await()
				),
				"result"
				.ToIdentifierName()
				.GreaterThan("0".ToLiteralInt())
				.Parenthesize()
				.Access("ToResponseData")
				.Call()
				.Return()
			};

			var tryBlockStatements = new List<StatementSyntax>
			{
				"query".ToVariableStatement(interpolatedStringParts.ToRawStringExpression()),
				GetUsingStatement(usingInnerStatments)
			};

			var statements = new List<StatementSyntax>
			{
				tryBlockStatements.TryCatch(
					GetCatchBlock(
						model,
						$"Entity {model.Name} could not be checked",
						"bool",
						false,
						parameterItems.ToArray()
					)
				)
			};

			(var methodUsings, var method) = CreateMethod(model, methodMap, statements);

			return new MethodCreationResult
			{
				Usings = methodUsings,
				Method = method
			};
		}

		private static MethodCreationResult CreateReadMethod(
			InfrastructureModel model,
			UseCaseQueryProviderMethodMap methodMap,
			InfrastructureModels infrastructureModelsConfig,
			bool implementSoftDelete
		)
		{
			var returnDto = methodMap.ReturnType.DtoMap;
			var relatedDataModels = CollectDataModelsForReferenceProperties(methodMap.Domain, returnDto, model, infrastructureModelsConfig, true);

			var modelItem = relatedDataModels.First(m => m.DataModel.Name == model.Name && m.Domain == methodMap.Domain && m.IsRootModel);
			var idParameter = methodMap.ParameterTypes.First();

			var interpolatedStringParts = TemplateMethods.CreateSqlQueryWithoutWhereCondition(model, methodMap.Domain, relatedDataModels, implementSoftDelete, false);
			var dtoInitializerExpressions = new List<ExpressionSyntax>();
			GetDtoInitializerExpressions(methodMap.Domain, model.Name, null, null, relatedDataModels, dtoInitializerExpressions, new HashSet<string>());

			interpolatedStringParts.Add(@"
				WHERE
					".Interpolate());
			interpolatedStringParts.Add(modelItem.TableAliasConstant.ToIdentifierName().Interpolate());
			interpolatedStringParts.Add(".".Interpolate());
			interpolatedStringParts.Add(Eshava.CodeAnalysis.SyntaxConstants.NameOf.Call(model.Name.Access("Id").ToArgument()).Interpolate());
			interpolatedStringParts.Add($@" = @{idParameter.Name.ToPropertyName()}".Interpolate());

			if (!methodMap.DataModelTypeProperty.IsNullOrEmpty() && !methodMap.DataModelTypePropertyValue.IsNullOrEmpty())
			{
				interpolatedStringParts.Add($@"
				AND
					".Interpolate());
				interpolatedStringParts.Add(modelItem.TableAliasConstant.ToIdentifierName().Interpolate());
				interpolatedStringParts.Add(".".Interpolate());
				interpolatedStringParts.Add(Eshava.CodeAnalysis.SyntaxConstants.NameOf.Call(model.Name.Access(methodMap.DataModelTypeProperty).ToArgument()).Interpolate());
				interpolatedStringParts.Add($@" = @{methodMap.DataModelTypeProperty}
					".Interpolate());
			}

			if (implementSoftDelete)
			{
				interpolatedStringParts.Add($@"
				AND
					".Interpolate());
				interpolatedStringParts.Add(modelItem.TableAliasConstant.ToIdentifierName().Interpolate());
				interpolatedStringParts.Add(".".Interpolate());
				interpolatedStringParts.Add(Eshava.CodeAnalysis.SyntaxConstants.NameOf.Call(model.Name.Access("Status").ToArgument()).Interpolate());
				interpolatedStringParts.Add(@" = @Status
					".Interpolate());
			}

			var parameterItems = new List<(ExpressionSyntax Property, string Name)>
			{
				(idParameter.Name.ToIdentifierName(), idParameter.Name.ToPropertyName())
			};

			if (!methodMap.DataModelTypeProperty.IsNullOrEmpty() && !methodMap.DataModelTypePropertyValue.IsNullOrEmpty())
			{
				var typeProperty = model.Properties.FirstOrDefault(p => p.Name == methodMap.DataModelTypeProperty);
				if (typeProperty is not null)
				{
					if (typeProperty.Type == "int")
					{
						parameterItems.Add((methodMap.DataModelTypePropertyValue.ToLiteralInt(), methodMap.DataModelTypeProperty));
					}
					else if (typeProperty.Type == "long")
					{
						parameterItems.Add((methodMap.DataModelTypePropertyValue.ToLiteralLong(), methodMap.DataModelTypeProperty));
					}
					else if (typeProperty.Type == "string")
					{
						parameterItems.Add((methodMap.DataModelTypePropertyValue.ToLiteralString(), methodMap.DataModelTypeProperty));
					}
					else
					{
						parameterItems.Add((methodMap.DataModelTypePropertyValue.ToIdentifierName(), methodMap.DataModelTypeProperty));
					}
				}
			}

			if (implementSoftDelete)
			{
				parameterItems.Add(("Status".Access("Active"), "Status"));
			}

			var parameterVariableDeclaration = SyntaxHelper.CreateAnonymousObject(parameterItems.ToArray());
			var mapperExpression = GetMapperExpression(returnDto.DtoName, dtoInitializerExpressions);

			var usingInnerStatments = new List<StatementSyntax>
			{
				"result"
				.ToVariableStatement(
					"connection"
					.Access("QueryAsync")
					.Call("query".ToArgument(), mapperExpression.ToArgument(), parameterVariableDeclaration.ToArgument())
					.Await()
				),
				"result"
				.Access("Any")
				.Call()
				.Not()
				.If("ResponseData".AsGeneric(returnDto.DtoName).ToInstance().Return())
			};

			if (methodMap.UseCustomGroupDtoMethod)
			{
				var groupMethodName = $"Group{methodMap.Name.Substring(0, methodMap.Name.Length - 5)}Result";
				usingInnerStatments.Add(StatementHelpers.GetMethodCall(null, groupMethodName, "result".ToIdentifierName()).Access("ToResponseData").Call().Return());
			}
			else
			{
				var processResultName = CreateGroupResultStatements("result".ToIdentifierName(), null, 0, null, null, returnDto, usingInnerStatments);
				if (processResultName.IsNullOrEmpty())
				{
					processResultName = "result";
				}

				usingInnerStatments.Add(
					processResultName
					.Access("First")
					.Call()
					.Access("ToResponseData")
					.Call()
					.Return()
				);
			}

			var tryBlockStatements = new List<StatementSyntax>
			{
				"query".ToVariableStatement(interpolatedStringParts.ToRawStringExpression()),
				GetUsingStatement(usingInnerStatments)
			};

			var statements = new List<StatementSyntax>
			{
				tryBlockStatements.TryCatch(
					GetCatchBlock(
						model,
						$"Entity {model.Name} could not be read",
						returnDto.DtoName,
						false,
						parameterItems.ToArray()
					)
				)
			};

			(var methodUsings, var method) = CreateMethod(model, methodMap, statements);

			return new MethodCreationResult
			{
				Usings = methodUsings,
				Method = method,
				Fields = GetConstantFields(methodMap.Domain, model.Name, relatedDataModels)
			};
		}

		private static MethodCreationResult CreateSearchMethod(InfrastructureModel model, UseCaseQueryProviderMethodMap methodMap, InfrastructureModels infrastructureModelsConfig, bool implementSoftDelete)
		{
			var returnDto = methodMap.ParameterTypes.First().Generic.First().DtoMap;
			var relatedDataModels = CollectDataModelsForReferenceProperties(methodMap.Domain, returnDto, model, infrastructureModelsConfig, true);

			var modelItem = relatedDataModels.First(m => m.DataModel.Name == model.Name && m.Domain == methodMap.Domain && m.IsRootModel);
			var filterRequestParameter = methodMap.ParameterTypes.First();

			var interpolatedStringParts = TemplateMethods.CreateSqlQueryWithoutWhereCondition(model, methodMap.Domain, relatedDataModels, implementSoftDelete, false);
			var dtoInitializerExpressions = new List<ExpressionSyntax>();
			GetDtoInitializerExpressions(methodMap.Domain, model.Name, null, null, relatedDataModels, dtoInitializerExpressions, new HashSet<string>());

			var mapperExpression = GetMapperExpression(returnDto.DtoName, dtoInitializerExpressions);

			var tryBlockStatements = new List<StatementSyntax>
			{
				"dbFilterRequest"
				.ToVariableStatement("TransformFilterRequest".AsGeneric(returnDto.DtoName, model.Name)
				.Call(filterRequestParameter.Name.ToArgument()))
			};

			if (implementSoftDelete)
			{
				tryBlockStatements.Add(
					"dbFilterRequest"
					.ToIdentifierName()
					.Assign(
						"AddStatusQueryConditions"
						.AsGeneric(model.Name, model.IdentifierType)
						.Call("dbFilterRequest".ToIdentifierName().ToArgument())
					)
					.ToExpressionStatement()
				);
			}

			tryBlockStatements.Add(
				"query"
				.Access("Query")
				.Assign(interpolatedStringParts.ToRawStringExpression())
				.ToExpressionStatement()
			);

			tryBlockStatements.Add(
				"settings"
				.ToVariableStatement(
					"WhereQuerySettings"
					.ToIdentifierName()
					.ToInstanceWithInitializer(
						"PropertyTypeMappings"
						.ToIdentifierName()
						.Assign("PropertyTypeMappings".ToIdentifierName()),
						"QueryParameter"
						.ToIdentifierName()
						.Assign(
							"Dictionary"
							.AsGeneric("string", "object")
							.ToInstance()
						)
					)
				)
			);

			if (implementSoftDelete)
			{
				tryBlockStatements.Add(
					"settings"
					.Access("QueryParameter")
					.Access("Add")
					.Call(
						"Status".ToLiteralString().ToArgument(),
						"Status".Access("Active").ToArgument()
					)
					.ToExpressionStatement()
				);
			}

			StatementHelpers.AddLocalMethodCallAsync(tryBlockStatements, "FilterAsync", null, "result", "query".ToIdentifierName(), "dbFilterRequest".ToIdentifierName(), "settings".ToIdentifierName(), mapperExpression);

			if (methodMap.UseCustomGroupDtoMethod)
			{
				var groupMethodName = $"Group{methodMap.Name.Substring(0, methodMap.Name.Length - 5)}Result";
				tryBlockStatements.Add(StatementHelpers.GetMethodCall(null, groupMethodName, "result".ToIdentifierName()).Access("ToIEnumerableResponseData").Call().Return());
			}
			else
			{
				var processResultName = CreateGroupResultStatements("result".ToIdentifierName(), null, 0, null, null, returnDto, tryBlockStatements);
				if (processResultName.IsNullOrEmpty())
				{
					processResultName = "result";
				}

				tryBlockStatements.Add(
					processResultName
					.Access("ToIEnumerableResponseData")
					.Call()
					.Return()
				);
			}

			var additionalLogParameter = new List<(ExpressionSyntax Property, string Name)>
			{
				("query".Access("Query"), "SqlQuery")
			};

			var statements = new List<StatementSyntax>
			{
				"query".ToVariableStatement("QueryWrapper".ToIdentifierName().ToInstance()),
				tryBlockStatements.TryCatch(
					GetCatchBlock(
						model,
						$"Entity {model.Name} could not be read",
						returnDto.DtoName,
						true,
						additionalLogParameter.ToArray()
					)
				)
			};

			(var methodUsings, var method) = CreateMethod(model, methodMap, statements);

			return new MethodCreationResult
			{
				Usings = methodUsings,
				Method = method,
				Fields = GetConstantFields(methodMap.Domain, model.Name, relatedDataModels)
			};
		}

		private static MethodCreationResult CreateSearchCountMethod(InfrastructureModel model, UseCaseQueryProviderMethodMap methodMap, InfrastructureModels infrastructureModelsConfig, bool implementSoftDelete)
		{
			var returnDto = methodMap.ParameterTypes.First().Generic.First().DtoMap;
			var relatedDataModels = CollectDataModelsForReferenceProperties(methodMap.Domain, returnDto, model, infrastructureModelsConfig, true);

			var modelItem = relatedDataModels.First(m => m.DataModel.Name == model.Name && m.Domain == methodMap.Domain && m.IsRootModel);
			var filterRequestParameter = methodMap.ParameterTypes.First();

			var interpolatedStringParts = TemplateMethods.CreateSqlQueryWithoutWhereCondition(model, methodMap.Domain, relatedDataModels, implementSoftDelete, true);

			var tryBlockStatements = new List<StatementSyntax>
			{
				"dbFilterRequest"
				.ToVariableStatement(
					"TransformFilterRequest"
					.AsGeneric(returnDto.DtoName, model.Name)
					.Call(filterRequestParameter.Name.ToArgument())
				)
			};

			if (implementSoftDelete)
			{
				tryBlockStatements.Add(
					"dbFilterRequest"
					.ToIdentifierName()
					.Assign(
						"AddStatusQueryConditions"
						.AsGeneric(model.Name, model.IdentifierType)
						.Call("dbFilterRequest".ToIdentifierName().ToArgument())
					)
					.ToExpressionStatement());
			}

			tryBlockStatements.Add(
				"query"
				.Access("Query")
				.Assign(interpolatedStringParts.ToRawStringExpression())
				.ToExpressionStatement()
			);

			tryBlockStatements.Add(
				"settings"
				.ToVariableStatement(
					"WhereQuerySettings"
					.ToIdentifierName()
					.ToInstanceWithInitializer(
						"PropertyTypeMappings"
						.ToIdentifierName()
						.Assign("PropertyTypeMappings".ToIdentifierName()),
						"QueryParameter"
						.ToIdentifierName()
						.Assign(
							"Dictionary"
							.AsGeneric("string", "object")
							.ToInstance()
						)
					)
				)
			);

			if (implementSoftDelete)
			{
				tryBlockStatements.Add(
					"settings"
					.Access("QueryParameter")
					.Access("Add")
					.Call(
						"Status".ToLiteralString().ToArgument(),
						"Status".Access("Active").ToArgument()
					)
					.ToExpressionStatement()
				);
			}

			StatementHelpers.AddLocalMethodCallAsync(tryBlockStatements, "FilterCountAsync", null, "result", "query".ToIdentifierName(), "dbFilterRequest".ToIdentifierName(), "settings".ToIdentifierName());

			tryBlockStatements.Add(
				"result"
				.ToIdentifierName()
				.Access("ToResponseData")
				.Call()
				.Return());

			var additionalLogParameter = new List<(ExpressionSyntax Property, string Name)>
			{
				("query".Access("Query"), "SqlQuery")
			};

			var statements = new List<StatementSyntax>
			{
				"query".ToVariableStatement("QueryWrapper".ToIdentifierName().ToInstance()),
				tryBlockStatements.TryCatch(
					GetCatchBlock(
						model,
						$"Entity {model.Name} could not be read",
						"int",
						false,
						additionalLogParameter.ToArray()
					)
				)
			};

			(var methodUsings, var method) = CreateMethod(model, methodMap, statements);

			return new MethodCreationResult
			{
				Usings = methodUsings,
				Method = method,
				Fields = GetConstantFields(methodMap.Domain, model.Name, relatedDataModels)
			};
		}

		private static (List<string> Usings, (string Name, MethodDeclarationSyntax Method) Method) CreateMethod(InfrastructureModel model, UseCaseQueryProviderMethodMap methodMap, List<StatementSyntax> statements)
		{
			var typeUsings = methodMap.ParameterTypes
				.SelectMany(pt => pt.CollectUsings())
				.Concat(methodMap.ReturnType.CollectUsings())
				.Where(@using => !@using.IsNullOrEmpty())
				.Distinct()
				.ToList();

			var parameter = methodMap.ParameterTypes
				.Select(parameterType => parameterType.Name.ToParameter().WithType(parameterType.GetParameterType()))
				.ToArray();

			var methodDeclaration = methodMap.Name
				.ToMethod(
					methodMap.ReturnType.GetReturnParameterType(),
					statements,
					SyntaxKind.PublicKeyword,
					SyntaxKind.AsyncKeyword
				)
				.WithParameter(parameter);

			return (typeUsings, (methodMap.Name, methodDeclaration));
		}

		private static StatementSyntax GetUsingStatement(List<StatementSyntax> usingInnerStatments)
		{
			return "connection"
				.ToVariable("DatabaseSettings"
					.Access("GetConnection")
					.Call()
				)
				.Using(usingInnerStatments);
		}

		private static List<StatementSyntax> GetCatchBlock(InfrastructureModel model, string message, string returnType, bool wrapTypeInIEnumerable, params (ExpressionSyntax Property, string Name)[] logProperties)
		{
			var catchBlockStatements = new List<StatementSyntax>();
			var additionalDeclaration = SyntaxHelper.CreateAnonymousObject(logProperties);

			catchBlockStatements.Add("messageGuid"
				.ToVariableStatement(
					"Logger".LogError("_scopedSettings", message.ToLiteralString(), additionalDeclaration)
				)
			);

			catchBlockStatements.Add(returnType
				.CreateInternalServerError(EshavaMessageConstant.ReadDataError, wrapTypeInIEnumerable)
				.Return()
			);

			return catchBlockStatements;
		}

		private static List<(string Using, (string FieldName, FieldType Type, FieldDeclarationSyntax Declaration) Field)> CreateModelConstantNames(
			InfrastructureModel model,
			IEnumerable<InfrastructureModel> childsForModel,
			Dictionary<string, InfrastructureModel> allModelsForDomain,
			Dictionary<string, List<InfrastructureModel>> allModelsForDomainByClassificationKey,
			string fullQualifiedDomainNamespace
		)
		{
			var fields = new List<(string Using, (string FieldName, FieldType Type, FieldDeclarationSyntax Declaration) Field)>();

			// Workaround to allow multiple data models for the same classification key (see InfrastructureFactory)
			// Create constants for all data models with the same classification key
			foreach (var modelSibling in allModelsForDomainByClassificationKey[model.ClassificationKey])
			{
				fields.Add(("", modelSibling.Name.CreateModelConstantField()));
			}

			foreach (var child in childsForModel)
			{
				var @using = $"{fullQualifiedDomainNamespace}.{child.ClassificationKey.ToPlural()}";
				fields.Add((@using, child.Name.CreateModelConstantField()));
			}

			var loopModel = model;
			while (!loopModel.ReferencedParent.IsNullOrEmpty())
			{
				if (!allModelsForDomain.TryGetValue(loopModel.ReferencedParent, out var parentModel))
				{
					break;
				}

				var @using = $"{fullQualifiedDomainNamespace}.{parentModel.ClassificationKey.ToPlural()}";
				fields.Add((@using, parentModel.Name.CreateModelConstantField()));

				loopModel = parentModel;
			}

			return fields;
		}

		private static List<QueryAnalysisItem> CollectDataModelsForReferenceProperties(
			string domain,
			ReferenceDtoMap dto,
			InfrastructureModel model,
			InfrastructureModels infrastructureModelsConfig,
			bool isRoot
		)
		{
			var relatedDataModels = new List<QueryAnalysisItem>();

			var parentReferenceProperty = model.Properties.FirstOrDefault(p => p.IsParentReference);
			if (parentReferenceProperty is not null)
			{
				var dtoProperty = dto.Dto.Properties.FirstOrDefault(p => p.Name == parentReferenceProperty.Name || p.ReferenceProperty == parentReferenceProperty.Name);
				if (dtoProperty is null)
				{
					dto.Dto.Properties.Add(new ApplicationUseCaseDtoProperty(true)
					{
						Name = parentReferenceProperty.Name,
						Type = parentReferenceProperty.Type
					});
				}
			}

			foreach (var property in dto.Dto.Properties)
			{
				if (property.Type.EndsWith("Dto"))
				{
					var childReferenceProperty = dto.ChildReferenceProperties.FirstOrDefault(p => p.Dto.DtoName == property.Type);
					if (childReferenceProperty is null)
					{
						continue;
					}

					var childDataModel = infrastructureModelsConfig.Namespaces
						.FirstOrDefault(ns => ns.Domain == childReferenceProperty.Dto.Domain)
						?.Models
						?.FirstOrDefault(m => m.Name == childReferenceProperty.Dto.DataModelName);
					if (childDataModel is null)
					{
						continue;
					}

					var currentRelatedDataModels = CollectDataModelsForReferenceProperties(childReferenceProperty.Dto.Domain, childReferenceProperty.Dto, childDataModel, infrastructureModelsConfig, false);
					if (currentRelatedDataModels.Count > 0)
					{
						foreach (var relatedModel in currentRelatedDataModels)
						{
							if (!relatedModel.ParentDtoName.IsNullOrEmpty())
							{
								continue;
							}

							relatedModel.IsEnumerable = property.IsEnumerable;
							relatedModel.ParentDtoPropertyName = property.Name;
							relatedModel.DtoName = childReferenceProperty.Dto.DtoName;
							if (relatedModel.ParentDataModel is null || (relatedModel.ParentDataModel is not null && relatedModel.ParentDataModel.Name == model.Name))
							{
								relatedModel.ParentDataModel = model;
								relatedModel.ParentDomain = domain;
								relatedModel.ParentDtoName = dto.Dto.Name;
							}
							else
							{
								relatedModel.ParentDtoName = childReferenceProperty.Dto.Dto.Name;
							}
						}

						relatedDataModels.AddRange(currentRelatedDataModels);
					}

					continue;
				}


				if (property.ReferenceProperty.IsNullOrEmpty())
				{
					// if it empty the dto property name correspond to the data property name
					var dataProperty = model.Properties.FirstOrDefault(p => p.Name == property.Name);
					if (dataProperty is null)
					{
						if (property.Name != "Id")
						{
							continue;
						}

						dataProperty = new InfrastructureModelPropery
						{
							Name = "Id",
							Type = model.IdentifierType
						};
					}

					var tableAlis = model.Name.CreateModelConstantField();
					relatedDataModels.Add(new QueryAnalysisItem
					{
						ParentDomain = null,
						ParentDataModel = null,
						ParentProperty = null,
						Domain = domain,
						DataModel = model,
						Property = dataProperty,
						DtoProperty = property,
						DtoName = dto.DtoName,
						TableAliasConstant = tableAlis.FieldName,
						TableAliasField = tableAlis.Declaration,
						IsGroupBy = property.IsGroupProperty || property.ReferenceProperty == "Id" || property.Name == "Id",
						IsRootModel = isRoot
					});
				}
				else if (!property.ReferenceProperty.Contains("."))
				{
					// if it not empty and contains no dot, it is property name mapping
					var dataProperty = model.Properties.FirstOrDefault(p => p.Name == property.ReferenceProperty);
					if (dataProperty is null)
					{
						if (property.ReferenceProperty != "Id")
						{
							continue;
						}

						dataProperty = new InfrastructureModelPropery
						{
							Name = "Id",
							Type = model.IdentifierType
						};
					}

					var tableAlis = model.Name.CreateModelConstantField();
					relatedDataModels.Add(new QueryAnalysisItem
					{
						ParentDomain = null,
						ParentDataModel = null,
						ParentProperty = null,
						Domain = domain,
						DataModel = model,
						Property = dataProperty,
						DtoProperty = property,
						DtoName = dto.DtoName,
						TableAliasConstant = tableAlis.FieldName,
						TableAliasField = tableAlis.Declaration,
						IsGroupBy = property.IsGroupProperty || property.ReferenceProperty == "Id" || property.Name == "Id",
						IsRootModel = isRoot
					});
				}
				else
				{
					// if it not empty and contains at least one dot, it is an reference type and a left join is needed
					var referencePropertyParts = property.ReferenceProperty.Split('.');
					var currentRelatedDataModels = new List<QueryAnalysisItem>();
					var referenceResult = CollectDataModelsForReferenceProperties(property, referencePropertyParts, 0, domain, model, infrastructureModelsConfig, currentRelatedDataModels);
					if (referenceResult)
					{
						relatedDataModels.AddRange(currentRelatedDataModels);
					}
				}
			}

			return relatedDataModels = relatedDataModels
				.OrderBy(m => m.TableAliasConstant)
				.ThenBy(m => m.Property.Name)
				.ToList();
		}

		private static bool CollectDataModelsForReferenceProperties(
			ApplicationUseCaseDtoProperty dtoProperty,
			string[] referencePropertyParts,
			int referencePropertyPartsIndex,
			string currentDomain,
			InfrastructureModel currentDataModel,
			InfrastructureModels infrastructureModelsConfig,
			List<QueryAnalysisItem> relatedDataModels
		)
		{
			for (var i = referencePropertyPartsIndex; i < (referencePropertyParts.Length - 1); i++)
			{
				var referenceProperty = referencePropertyParts[i];
				var currentDataProperty = currentDataModel.Properties.FirstOrDefault(p => p.Name == referenceProperty);
				if (currentDataProperty is null)
				{
					return false;
				}

				var referenceDomain = currentDataProperty.ReferenceDomain.IsNullOrEmpty()
					? currentDomain
					: currentDataProperty.ReferenceDomain;

				var referenceDataModel = infrastructureModelsConfig.Namespaces
					.FirstOrDefault(ns => ns.Domain == referenceDomain)
					?.Models
					.FirstOrDefault(m => m.Name == currentDataProperty.ReferenceType);

				if (referenceDataModel is null)
				{
					return false;
				}

				if ((i + 1) < (referencePropertyParts.Length - 1))
				{
					var referencePropertyName = referencePropertyParts[i + 1];
					var dataProperty = referenceDataModel.Properties.FirstOrDefault(p => p.Name == referencePropertyName);
					if (dataProperty is null)
					{
						dataProperty = referenceDataModel.Properties.FirstOrDefault(p => p.Name == (referencePropertyName + "Id") && p.IsReference);
						if (dataProperty is null)
						{
							return false;
						}

						dataProperty = new InfrastructureModelPropery
						{
							Name = referencePropertyName,
							ReferencePropertyName = dataProperty.Name,
							ReferenceType = dataProperty.ReferenceType,
							Type = dataProperty.ReferenceType,
							ReferenceDomain = dataProperty.ReferenceDomain.IsNullOrEmpty() ? referenceDomain : dataProperty.ReferenceDomain
						};
						referenceDataModel.Properties.Add(dataProperty);
					}

					var tableAlis = String.Concat(referencePropertyParts.Take(i + 1)).CreateModelConstantField();
					relatedDataModels.Add(new QueryAnalysisItem
					{
						ParentDomain = currentDomain,
						ParentDataModel = currentDataModel,
						ParentProperty = currentDataProperty,
						Domain = referenceDomain,
						DataModel = referenceDataModel,
						Property = dataProperty,
						DtoProperty = null,
						TableAliasConstant = tableAlis.FieldName,
						TableAliasField = tableAlis.Declaration
					});

					return CollectDataModelsForReferenceProperties(dtoProperty, referencePropertyParts, i + 1, referenceDomain, referenceDataModel, infrastructureModelsConfig, relatedDataModels);
				}
				else
				{
					var referencePropertyName = referencePropertyParts.Last();
					var dataProperty = referenceDataModel.Properties.FirstOrDefault(p => p.Name == referencePropertyName);
					if (dataProperty is null)
					{
						if (referencePropertyName != "Id")
						{
							return false;
						}

						dataProperty = new InfrastructureModelPropery
						{
							Name = "Id",
							Type = referenceDataModel.IdentifierType
						};
					}

					var tableAlis = String.Concat(referencePropertyParts.Take(i + 1)).CreateModelConstantField();
					relatedDataModels.Add(new QueryAnalysisItem
					{
						ParentDomain = currentDomain,
						ParentDataModel = currentDataModel,
						ParentProperty = currentDataProperty,
						Domain = referenceDomain,
						DataModel = referenceDataModel,
						Property = dataProperty,
						DtoProperty = dtoProperty,
						TableAliasConstant = tableAlis.FieldName,
						TableAliasField = tableAlis.Declaration
					});
				}
			}

			return true;
		}

		private static void GetDtoInitializerExpressions(
			string referenceDomain,
			string referenceType,
			string parentDtoName,
			string dtoName,
			List<QueryAnalysisItem> relatedDataModels,
			List<ExpressionSyntax> dtoInitializerExpressions,
			HashSet<string> processedDtoTableAlias
		)
		{
			var parentRelatedDataModels = relatedDataModels
				.Where(m => (m.ParentDtoName is null && parentDtoName is null) || m.DtoName == dtoName)
				.OrderByDescending(m => m.DtoName)
				.ThenBy(m => m.DtoProperty?.Name)
				.ToList();

			foreach (var item in parentRelatedDataModels)
			{
				if (item.DtoProperty is null || item.DtoProperty.IsVirtualProperty)
				{
					continue;
				}

				var propertyType = item.Property.TypeWithUsing;
				if (propertyType.EndsWith("?") && !item.DtoProperty.Type.EndsWith("?"))
				{
					propertyType = propertyType.Substring(0, propertyType.Length - 1);
				}

				var dataType = item.GetDataType(referenceDomain, referenceType);
				dtoInitializerExpressions.Add(
					item.DtoProperty.Name
					.ToIdentifierName()
					.Assign(
						"mapper"
						.Access("GetValue".AsGeneric(propertyType))
						.Call(
							Eshava.CodeAnalysis.SyntaxConstants.NameOf.Call(dataType.Access(item.Property.Name).ToArgument()).ToArgument(),
							item.TableAliasConstant.ToArgument()
						)
					)
				);

				var dtoTableAlias = $"{item.DtoName ?? ""}_{item.TableAliasConstant}";
				if (!processedDtoTableAlias.Contains(dtoTableAlias))
				{
					processedDtoTableAlias.Add(dtoTableAlias);
				}
			}

			if (parentRelatedDataModels.Count == 0 || parentRelatedDataModels[0].DtoName.IsNullOrEmpty())
			{
				return;
			}

			parentDtoName = parentRelatedDataModels[0].DtoName;

			var dtoRelatedDataModels = relatedDataModels.Where(m => m.ParentDtoName == parentDtoName && !processedDtoTableAlias.Contains($"{m.DtoName ?? ""}_{m.TableAliasConstant}")).GroupBy(m => m.ParentDtoPropertyName);
			if (!dtoRelatedDataModels.Any())
			{
				return;
			}

			foreach (var propertyReferences in dtoRelatedDataModels)
			{
				var dtoReferenceItem = propertyReferences.FirstOrDefault(p => p.IsGroupBy) ?? propertyReferences.First();
				dtoName = dtoReferenceItem.DtoName;

				var childDtoInitializerExpressions = new List<ExpressionSyntax>();
				GetDtoInitializerExpressions(referenceDomain, referenceType, parentDtoName, dtoName, relatedDataModels, childDtoInitializerExpressions, processedDtoTableAlias);

				ExpressionSyntax dtoInstance = dtoName.ToIdentifierName().ToInstanceWithInitializer(childDtoInitializerExpressions.ToArray());
				if (dtoReferenceItem.IsEnumerable)
				{
					var dataType = dtoReferenceItem.GetDataType(referenceDomain, referenceType);
					if (dtoReferenceItem.IsGroupBy)
					{
						dtoInstance = "mapper"
							.Access("GetValue".AsGeneric(dtoReferenceItem.Property.TypeWithUsing))
							.Call(
								Eshava.CodeAnalysis.SyntaxConstants.NameOf.Call(dataType.Access(dtoReferenceItem.Property.Name).ToArgument()).ToArgument(),
								dtoReferenceItem.TableAliasConstant.ToArgument()
							)
							.ToEquals(Eshava.CodeAnalysis.SyntaxConstants.Default)
							.ShortIf(
								"List".AsGeneric(dtoName).ToInstance(),
								"List".AsGeneric(dtoName).ToInstanceWithInitializer([dtoInstance])
							);
					}
					else
					{
						dtoInstance = "List".AsGeneric(dtoName).ToInstanceWithInitializer([dtoInstance]);
					}
				}

				dtoInitializerExpressions.Add(
					dtoReferenceItem.ParentDtoPropertyName
					.ToIdentifierName()
					.Assign(dtoInstance)
				);
			}
		}


		private static SimpleLambdaExpressionSyntax GetMapperExpression(string dtoName, List<ExpressionSyntax> dtoInitializerExpressions)
		{
			var mapperStatements = new List<StatementSyntax>
			{
				"dto"
				.ToVariableStatement(
					dtoName
					.ToIdentifierName()
					.ToInstanceWithInitializer(
						dtoInitializerExpressions.ToArray()
					)
				),
				"dto".ToIdentifierName().Return()
			};

			return "mapper".ToParameterExpression(mapperStatements.ToArray());
		}

		private static List<(string DataType, string FieldName, FieldDeclarationSyntax Declaration)> GetConstantFields(string referenceDomain, string referenceType, List<QueryAnalysisItem> relatedDataModels)
		{
			return relatedDataModels
				.GroupBy(m => m.TableAliasConstant)
				.Select(m => (m.First().GetDataType(referenceDomain, referenceType), m.First().TableAliasConstant, m.First().TableAliasField))
				.ToList();
		}

		private static void CreatePropertyTypeMappings(UnitInformation unitInformation, List<(string DataType, string FieldName, FieldDeclarationSyntax Declaration)> fields)
		{
			var dtoInitializerExpressions = new List<InitializerExpressionSyntax>();

			foreach (var field in fields)
			{
				dtoInitializerExpressions.Add(
					field.DataType
					.ToIdentifierName()
					.TypeOf()
					.ToComplexElementInitializerExpression(field.FieldName.ToIdentifierName())
				);
			}

			var fieldName = "_propertyTypeMappings";
			var mappings = fieldName
				.ToField(
					"Dictionary"
						.AsGeneric("Type", "string"),
					"Dictionary"
						.AsGeneric("Type", "string")
						.ToInstanceWithInitializer(dtoInitializerExpressions.ToArray())
				);

			if (unitInformation.AddField(fieldName, mappings))
			{
				unitInformation.AddConstructorBodyStatement(
					"PropertyTypeMappings"
					.ToIdentifierName()
					.Assign(fieldName.ToIdentifierName())
					.ToExpressionStatement()
				);
			}
		}

		private static string CreateGroupResultStatements(ExpressionSyntax dataSource, string nestedAccess, int layerIndex, string layerPostfix, ReferenceDtoMap parentReferenceDto, ReferenceDtoMap referenceDto, List<StatementSyntax> statements)
		{
			if (layerIndex == 0 && referenceDto.ChildReferenceProperties.Count == 0)
			{
				return null;
			}

			var currentListName = $"items{layerIndex}{layerPostfix}";
			if (nestedAccess.IsNullOrEmpty())
			{
				statements.Add(currentListName.ToVariableStatement("List".AsGeneric(referenceDto.DtoName).ToInstance()));
			}

			var groupByParentProperty = referenceDto.Dto.Properties.FirstOrDefault(p => p.IsGroupProperty)
					?? referenceDto.Dto.Properties.FirstOrDefault(p => p.ReferenceProperty == "Id" || p.Name == "Id")
					?? parentReferenceDto?.Dto.Properties.FirstOrDefault(p => p.IsGroupProperty)
					?? parentReferenceDto?.Dto.Properties.FirstOrDefault(p => p.ReferenceProperty == "Id" || p.Name == "Id");

			if (groupByParentProperty is null)
			{
				return currentListName;
			}

			var loopStatements = new List<StatementSyntax>();
			if (nestedAccess.IsNullOrEmpty())
			{
				loopStatements.Add($"item{layerIndex}{referenceDto.Dto.Name}".ToVariableStatement($"group{layerIndex}{referenceDto.Dto.Name}".Access("First").Call()));
			}

			foreach (var item in referenceDto.ChildReferenceProperties)
			{
				if (!item.Property.IsEnumerable)
				{
					if (item.Dto.ChildReferenceProperties.Count > 0)
					{
						CreateGroupResultStatements($"group{layerIndex}{referenceDto.Dto.Name}".ToIdentifierName(), item.Property.Name, layerIndex, layerPostfix, referenceDto, item.Dto, loopStatements);
					}

					continue;
				}

				var groupByProperty = item.Dto.Dto.Properties.FirstOrDefault(p => p.IsGroupProperty)
					?? item.Dto.Dto.Properties.FirstOrDefault(p => p.ReferenceProperty == "Id" || p.Name == "Id");

				if (groupByProperty is null)
				{
					continue;
				}

				var nestedPropertyName = item.Property.Name;
				if (!nestedAccess.IsNullOrEmpty())
				{
					nestedPropertyName = $"{nestedAccess}.{nestedPropertyName}";
				}

				var layerGroupName = nestedAccess.IsNullOrEmpty()
					? $"group{layerIndex}{referenceDto.Dto.Name}"
					: $"group{layerIndex}{parentReferenceDto.Dto.Name}";

				loopStatements.Add(
					$"item{layerIndex}{item.Property.Name}"
					.ToVariableStatement(
						layerGroupName
						.Access("SelectMany")
						.Call($"child{layerIndex}".ToPropertyExpression(nestedPropertyName).ToArgument())
						.Access("ToList")
						.Call()
					)
				);

				CreateGroupResultStatements($"item{layerIndex}{item.Property.Name}".ToIdentifierName(), null, layerIndex + 1, item.Property.Name, referenceDto, item.Dto, loopStatements);


				var layerItemName = nestedAccess.IsNullOrEmpty()
					? $"item{layerIndex}{referenceDto.Dto.Name}"
					: $"item{layerIndex}{parentReferenceDto.Dto.Name}";

				loopStatements.Add(
					layerItemName
					.Access(nestedPropertyName)
					.Assign(
						$"items{layerIndex + 1}{item.Property.Name}".ToIdentifierName()
					)
					.ToExpressionStatement()
				);

			}

			if (nestedAccess.IsNullOrEmpty())
			{
				loopStatements.Add(currentListName.Access("Add").Call($"item{layerIndex}{referenceDto.Dto.Name}".ToArgument()).ToExpressionStatement());

				statements.Add(
					dataSource
					.Access("GroupBy")
					.Call($"item{layerIndex}{referenceDto.Dto.Name}".ToPropertyExpression(groupByParentProperty.Name).ToArgument())
					.ForEach($"group{layerIndex}{referenceDto.Dto.Name}", loopStatements)
				);
			}
			else
			{
				statements.AddRange(loopStatements);
			}

			return currentListName;
		}

		private class MethodCreationResult
		{
			public List<string> Usings { get; set; }
			public (string Name, MethodDeclarationSyntax Method) Method { get; set; }
			public List<(string DataType, string FieldName, FieldDeclarationSyntax Declaration)> Fields { get; set; }
		}
	}
}