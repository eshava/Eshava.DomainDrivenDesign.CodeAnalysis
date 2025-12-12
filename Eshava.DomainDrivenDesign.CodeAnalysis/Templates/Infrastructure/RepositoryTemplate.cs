using System.Collections.Generic;
using System.Linq;
using Eshava.CodeAnalysis;
using Eshava.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Constants;
using Eshava.DomainDrivenDesign.CodeAnalysis.Enums;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Domain;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Templates.Infrastructure
{
	public static class RepositoryTemplate
	{
		public static string GetRepository(
			InfrastructureModel model,
			ReferenceDomainModelMap domainModelMap,
			string domain,
			string fullQualifiedDomainNamespace,
			InfrastructureProject project,
			string databaseSettingsInterface,
			string databaseSettingsInterfaceUsing,
			InfrastructureModel parentModel,
			Dictionary<string, List<InfrastructureModel>> childsForModel
		)
		{
			var scopedSettings = new NameAndType(CommonNames.SCOPEDSETTINGS, project.ScopedSettingsClass.ToType());
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
			var className = $"{domainModelMap.DomainModelName}Repository";
			string baseClass;
			InfrastructureProjectAlternativeClass alternativeClass;

			var unitInformation = new UnitInformation(className, @namespace, addAssemblyComment: project.AddAssemblyCommentToFiles);
			unitInformation.AddClassModifier(SyntaxKind.InternalKeyword, SyntaxKind.PartialKeyword);
			unitInformation.AddContructorModifier(SyntaxKind.PublicKeyword);


			unitInformation.AddUsing(CommonNames.Namespaces.EXPRESSION);
			unitInformation.AddUsing(CommonNames.Namespaces.GENERIC);
			unitInformation.AddUsing(CommonNames.Namespaces.LINQ);
			unitInformation.AddUsing(CommonNames.Namespaces.LOGGING);
			unitInformation.AddUsing(CommonNames.Namespaces.SYSTEM);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.EXTENSIONS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.Linq.INTERFACES);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.MODELS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Domain.EXTENSIONS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Domain.MODELS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Infrastructure.INTERFACES);
			unitInformation.AddUsing(project.ScopedSettingsUsing);
			unitInformation.AddUsing(project.AlternativeUsing);
			unitInformation.AddUsing(databaseSettingsInterfaceUsing);

			unitInformation.AddConstructorParameter(databaseSettings, ParameterTargetTypes.Argument);
			unitInformation.AddConstructorParameter(scopedSettings, ParameterTargetTypes.Argument);
			unitInformation.AddConstructorParameter(InfrastructureNames.Transform.Parameter, ParameterTargetTypes.Argument);

			var fullDomainModelName = $"Domain.{domain}.{domainModelMap.DomainModel.NamespaceDirectory}.{domainModelMap.DomainModelName}";

			var relatedDataModels = CollectDataModelsForReferenceProperties(model, domainModelMap, childsForModel, true);
			relatedDataModels.ForEach(relation => unitInformation.AddField((relation.TableAliasConstant, FieldType.Const, relation.TableAliasField)));

			var collectPropertyMappings = CollectDataToDomainPropertyMappings(domainModelMap);
			collectPropertyMappings.ForEach(mapping => unitInformation.AddField((mapping.FieldName, FieldType.Static, mapping.Declaration)));

			if (model.IsChild)
			{
				unitInformation.AddUsing($"{project.FullQualifiedNamespace}.{domain}.{domainModelMap.AggregateDomainModel.ClassificationKey.ToPlural()}");
				alternativeClass = project.AlternativeClasses.FirstOrDefault(ac => ac.Type == InfrastructureAlternativeClassType.ChildDomainModelRepository);

				if (alternativeClass is null)
				{
					baseClass = InfrastructureNames.ABSTRACTCHILDDOMAINMODELREPOSITORY;
					unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Infrastructure.REPOSITORIES);
				}
				else
				{
					baseClass = alternativeClass.ClassName;
				}
			}
			else
			{
				unitInformation.AddUsing(CommonNames.Namespaces.SYSTEM);
				unitInformation.AddUsing(CommonNames.Namespaces.LINQ);
				unitInformation.AddUsing(CommonNames.Namespaces.TASKS);
				unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Storm.NAME);
				unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Storm.MetaData.NAME);
				unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.EXTENSIONS);
				unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.VALIDATION.INTERFACES);
				unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Domain.CONSTANTS);
				unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Domain.ENUMS);
				unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Domain.EXTENSIONS);

				unitInformation.AddConstructorParameter(DomainNames.VALIDATION.Parameter);

				alternativeClass = project.AlternativeClasses.FirstOrDefault(ac => ac.Type == InfrastructureAlternativeClassType.DomainModelRepository);

				if (alternativeClass is null)
				{
					baseClass = InfrastructureNames.ABSTRACTDOMAINMODELREPOSITORY;
					unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Infrastructure.REPOSITORIES);
				}
				else
				{
					baseClass = alternativeClass.ClassName;
				}

				unitInformation.AddMethod(CreateReadMethod(model, domainModelMap, fullDomainModelName, childsForModel, relatedDataModels, project.ImplementSoftDelete));
			}

			var baseType = model.IsChild
				? baseClass.AsGeneric(fullDomainModelName, $"{domainModelMap.AggregateDomainModel.ClassificationKey.ToPlural()}.{domainModelMap.AggregateDomainModel.DomainModelName}CreationBag", model.Name, model.IdentifierType, project.ScopedSettingsClass).ToSimpleBaseType()
				: baseClass.AsGeneric(fullDomainModelName, model.Name, model.IdentifierType, project.ScopedSettingsClass).ToSimpleBaseType();
			var repositoryInterface = $"I{className}".ToType().ToSimpleBaseType();
			unitInformation.AddBaseType(baseType, repositoryInterface);
			unitInformation.AddUsing(alternativeClass?.Using);

			CheckAndAddProviderReferences(unitInformation, alternativeClass);

			foreach (var property in model.Properties)
			{
				if (!property.UsingForType.IsNullOrEmpty())
				{
					unitInformation.AddUsing(property.UsingForType);
				}
			}

			if (domainModelMap is not null && !domainModelMap.IsChildDomainModel)
			{
				foreach (var foreignKeyReference in domainModelMap.ForeignKeyReferences)
				{
					if (foreignKeyReference.IsProcessingProperty || foreignKeyReference.DomainModel.IsValueObject)
					{
						continue;
					}

					unitInformation.AddUsing(CommonNames.Namespaces.GENERIC);
					unitInformation.AddUsing(CommonNames.Namespaces.TASKS);
					unitInformation.AddMethod(CreateReadForMethod(model, childsForModel, domainModelMap, foreignKeyReference, relatedDataModels, fullDomainModelName, project.ImplementSoftDelete));
				}
			}

			unitInformation.AddLogger(className, true);

			unitInformation.AddMethod(CreateFromDomainModelMethod(model, fullDomainModelName, parentModel, domainModelMap));
			unitInformation.AddMethod(CreateGetPropertyNameMethod(domainModelMap));

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

		private static (string Name, MemberDeclarationSyntax) CreateReadMethod(
			InfrastructureModel model,
			ReferenceDomainModelMap domainModelMap,
			string fullDomainModelName,
			Dictionary<string, List<InfrastructureModel>> childsForModel,
			List<QueryAnalysisItem> relatedDataModels,
			bool implementSoftDelete
		)
		{
			var readByPropertyName = "Id";
			var readByVariableName = $"{model.ClassificationKey}Id";
			var tryBlockStatements = new List<StatementSyntax>
			{
				GetReadByQuery(model, readByPropertyName, readByVariableName, domainModelMap, relatedDataModels, implementSoftDelete)
			};

			var usingInnerStatments = new List<StatementSyntax>
			{
				GetReadByQueryResult(domainModelMap, model, childsForModel, readByVariableName, implementSoftDelete)
			};

			usingInnerStatments.AddRange(GetCreateDomainModelCode(model, childsForModel, domainModelMap, "result", fullDomainModelName, true));

			var domainModelListName = $"{domainModelMap.DomainModelName.ToVariableName()}Models";

			usingInnerStatments.Add(domainModelListName
				.Access("Single")
				.Call()
				.Access(CommonNames.Extensions.TORESPONSEDATA)
				.Call()
				.Return()
			);

			tryBlockStatements.Add("connection"
				.ToVariable("DatabaseSettings"
					.Access("GetConnection")
					.Call()
				)
				.Using(usingInnerStatments)
			);

			var statements = new List<StatementSyntax>
			{
				tryBlockStatements.TryCatch(GetReadByCatchBlock(model, readByVariableName, fullDomainModelName, false))
			};

			var methodDeclarationName = "ReadAsync";
			var methodDeclaration = methodDeclarationName
				.ToMethod(
					"Task".AsGeneric("ResponseData".AsGeneric($"Domain.{domainModelMap.Domain}.{domainModelMap.DomainModel.NamespaceDirectory}.{domainModelMap.DomainModelName}")),
					statements,
					SyntaxKind.PublicKeyword,
					SyntaxKind.AsyncKeyword
				);

			return (
				methodDeclarationName,
				methodDeclaration
				.WithParameter(readByVariableName.ToVariableName()
					.ToParameter()
					.WithType(model.IdentifierType.ToType())
				)
			);
		}

		private static (string Name, MemberDeclarationSyntax) CreateFromDomainModelMethod(
			InfrastructureModel model,
			string fullDomainModelName,
			InfrastructureModel parentModel,
			ReferenceDomainModelMap domainModelMap
		)
		{
			var statements = new List<StatementSyntax>
			{
				"model"
					.ToIdentifierName()
					.IsNull()
					.If(Eshava.CodeAnalysis.SyntaxConstants.Null.Return())
			};

			var dataModelInstance = model.Name.ToType().ToInstance();
			var instance = "instance".ToVariableStatement(dataModelInstance);
			statements.Add(instance);

			var instanceVar = "instance".ToIdentifierName();
			var modelVar = "model".ToIdentifierName();

			var domainModelPropertiesByMapperProperty = domainModelMap.DomainModel.Properties
				.Where(p => !p.DataModelPropertyName.IsNullOrEmpty())
				.ToDictionary(p => p.DataModelPropertyName, p => p);

			var domainModelProperties = domainModelMap.DomainModel.Properties
				.ToDictionary(p => p.Name, p => p);

			var valueObjectCache = new Dictionary<string, ValueObjectCacheItem>();

			// Domain model property name with a value object as type -> Mapping from data model property to value object property
			var valueObjectAssignments = new Dictionary<string, List<(InfrastructureModelPropery DataProperty, DomainModelProperty DomainModelProperty)>>();

			foreach (var property in model.Properties)
			{
				if (property.SkipFromDomainModel)
				{
					continue;
				}

				if (model.IsChild && property.Name == $"{parentModel.ClassificationKey}Id")
				{
					statements.Add(
						instanceVar
						.Access(property.Name)
						.Assign("creationBag".Access($"{parentModel.ClassificationKey}Id"))
						.ToExpressionStatement()
					);
				}
				else if (model.IsChild && parentModel.Properties.Any(p => p.AddToCreationBag && p.Name == property.Name))
				{
					statements.Add(
						instanceVar
						.Access(property.Name)
						.Assign("creationBag".Access(property.Name))
						.ToExpressionStatement()
					);
				}
				else if (property.Name == domainModelMap.DomainModel.DataModelTypeProperty)
				{
					var propertyValue = domainModelMap.DomainModel.DataModelTypePropertyValue;
					ExpressionSyntax valueExpression = property.Type switch
					{
						"int" =>propertyValue.ToLiteralInt(),
						"long" =>propertyValue.ToLiteralLong(),
						"string" =>propertyValue.ToLiteralString(),
						_ => propertyValue.ToIdentifierName()
					};

					statements.Add(
						instanceVar
						.Access(property.Name)
						.Assign(valueExpression)
						.ToExpressionStatement()
					);
				}
				else
				{
					if (!domainModelPropertiesByMapperProperty.TryGetValue(property.Name, out var domainModelProperty))
					{
						domainModelProperties.TryGetValue(property.Name, out domainModelProperty);
					}

					//Check for value object
					if (domainModelProperty is null)
					{
						CollectValueObjectPropertiesForDataModelCreation(property, domainModelMap, valueObjectCache, valueObjectAssignments);

						continue;
					}

					var domainModelPropertyAccessor = modelVar.Access(domainModelProperty.Name);
					statements.Add(CreatePropertyAssignment(instanceVar, property, domainModelProperty, domainModelPropertyAccessor));
				}
			}

			foreach (var assigments in valueObjectAssignments)
			{
				var assignmentStatements = new List<StatementSyntax>();
				var valueObjectAccess = modelVar.Access(assigments.Key);

				foreach (var propertyMapping in assigments.Value)
				{
					var domainModelPropertyAccessor = valueObjectAccess.Access(propertyMapping.DomainModelProperty.Name);
					assignmentStatements.Add(CreatePropertyAssignment(instanceVar, propertyMapping.DataProperty, propertyMapping.DomainModelProperty, domainModelPropertyAccessor));
				}

				statements.Add(valueObjectAccess
					.IsNotNull()
					.If(assignmentStatements.ToArray())
				);
			}

			if (model.IsChild)
			{
				statements.Add(
					"FromDomainModel"
					.ToIdentifierName()
					.Call(instanceVar.ToArgument(), modelVar.ToArgument(), "creationBag".ToArgument())
					.Return()
				);
			}
			else
			{
				statements.Add(
					"FromDomainModel"
					.ToIdentifierName()
					.Call(instanceVar.ToArgument(), modelVar.ToArgument())
					.Return()
				);
			}

			var methodDeclarationName = "FromDomainModel";
			var methodDeclaration = methodDeclarationName.ToMethod(
				model.Name.ToType(),
				statements,
				SyntaxKind.ProtectedKeyword,
				SyntaxKind.OverrideKeyword
			);

			var domainModelReferenceType = fullDomainModelName.ToType();
			var domainModelReferenceParameter = "model".ToParameter().WithType(domainModelReferenceType);

			if (model.IsChild)
			{
				var parentReferenceType = $"{domainModelMap.AggregateDomainModel.ClassificationKey.ToPlural()}.{domainModelMap.AggregateDomainModel.DomainModelName}CreationBag".ToType();
				var parentReferenceParameter = "creationBag".ToParameter().WithType(parentReferenceType);

				methodDeclaration = methodDeclaration.WithParameter(domainModelReferenceParameter, parentReferenceParameter);
			}
			else
			{
				methodDeclaration = methodDeclaration.WithParameter(domainModelReferenceParameter);
			}

			return (methodDeclarationName, methodDeclaration);
		}

		private static void CollectValueObjectPropertiesForDataModelCreation(
			InfrastructureModelPropery property,
			ReferenceDomainModelMap domainModelMap,
			Dictionary<string, ValueObjectCacheItem> valueObjectCache,
			Dictionary<string, List<(InfrastructureModelPropery DataProperty, DomainModelProperty DomainModelProperty)>> valueObjectAssignments
		)
		{
			foreach (var dmProperty in domainModelMap.DomainModel.Properties)
			{
				var foreignKeyReference = domainModelMap.ForeignKeyReferences.FirstOrDefault(fkr => fkr.DomainModelName == dmProperty.Type);
				if (foreignKeyReference is null)
				{
					continue;
				}

				if (!valueObjectCache.TryGetValue(foreignKeyReference.DomainModelName, out var valueObject))
				{
					var valueObjectPropertiesByMapperProperty = foreignKeyReference.DomainModel.Properties
						.Where(p => !p.DataModelPropertyName.IsNullOrEmpty())
						.ToDictionary(p => p.DataModelPropertyName, p => p);

					var valueObjectProperties = foreignKeyReference.DomainModel.Properties
						.ToDictionary(p => p.Name, p => p);

					valueObject = new ValueObjectCacheItem
					{
						DomainModel = foreignKeyReference,
						PropertyMappings = valueObjectPropertiesByMapperProperty,
						Properties = valueObjectProperties
					};

					valueObjectCache.Add(foreignKeyReference.DomainModelName, valueObject);
				}

				if (!valueObject.PropertyMappings.TryGetValue(property.Name, out var domainModelProperty))
				{
					valueObject.Properties.TryGetValue(property.Name, out domainModelProperty);
				}

				if (domainModelProperty is null)
				{
					continue;
				}

				if (!valueObjectAssignments.TryGetValue(dmProperty.Name, out var assigments))
				{
					assigments = [];
					valueObjectAssignments.Add(dmProperty.Name, assigments);
				}

				assigments.Add((property, domainModelProperty));

				break;
			}
		}

		private static StatementSyntax CreatePropertyAssignment(
			IdentifierNameSyntax instanceVar,
			InfrastructureModelPropery property,
			DomainModelProperty domainModelProperty,
			ExpressionSyntax domainModelPropertyAccessor
		)
		{
			var dataPropertyType = property.Type.Replace("?", "");
			var domainModelPropertyType = domainModelProperty.Type.Replace("?", "");

			if (dataPropertyType != domainModelPropertyType)
			{
				return instanceVar
					.Access(property.Name)
					.Assign(domainModelPropertyAccessor.Cast(property.Type.ToType()))
					.ToExpressionStatement();
			}

			return instanceVar
				.Access(property.Name)
				.Assign(domainModelPropertyAccessor)
				.ToExpressionStatement();
		}

		private static (string Name, MemberDeclarationSyntax) CreateReadForMethod(
			InfrastructureModel model,
			Dictionary<string, List<InfrastructureModel>> childsForModel,
			ReferenceDomainModelMap domainModelMap,
			ReferenceDomainModel foreignKeyReference,
			List<QueryAnalysisItem> relatedDataModels,
			string fullDomainModelName,
			bool implementSoftDelete
		)
		{
			var readByPropertyName = foreignKeyReference.PropertyName;
			var readByVariableName = foreignKeyReference.PropertyName;
			var returnListName = $"{domainModelMap.DomainModelName.ToVariableName()}Models";

			var tryBlockStatements = new List<StatementSyntax>
			{
				GetReadByQuery(model, readByPropertyName, readByVariableName, domainModelMap, relatedDataModels, implementSoftDelete),
			};

			var usingInnerStatments = new List<StatementSyntax>
			{
				GetReadByQueryResult(domainModelMap, model, childsForModel, readByVariableName, implementSoftDelete),
			};

			usingInnerStatments.AddRange(GetCreateDomainModelCode(model, childsForModel, domainModelMap, "result", fullDomainModelName, false));
			usingInnerStatments.Add(
				returnListName
				.Access(CommonNames.Extensions.TOIENUMERABLERESPONSEDATA)
				.Call()
				.Return()
			);

			tryBlockStatements.Add("connection"
				.ToVariable("DatabaseSettings"
					.Access("GetConnection")
					.Call()
				)
				.Using(usingInnerStatments)
			);

			var statements = new List<StatementSyntax>
			{
				tryBlockStatements.TryCatch(GetReadByCatchBlock(model, readByVariableName, fullDomainModelName, true))
			};

			var methodDeclarationName = $"ReadFor{foreignKeyReference.PropertyName}Async";
			var methodDeclaration = methodDeclarationName.ToMethod(
				"Task".AsGeneric(CommonNames.RESPONSEDATA.AsGeneric("IEnumerable".AsGeneric(fullDomainModelName))),
				statements,
				SyntaxKind.PublicKeyword,
				SyntaxKind.AsyncKeyword
			);

			methodDeclaration = methodDeclaration
				.WithParameter(
					readByVariableName
					.ToVariableName()
					.ToParameter()
					.WithType(domainModelMap.IdentifierType.ToType())
				);

			return (methodDeclarationName, methodDeclaration);
		}

		private static (string Name, MemberDeclarationSyntax) CreateGetPropertyNameMethod(ReferenceDomainModelMap domainModelMap)
		{
			var fullDomainModelName = $"Domain.{domainModelMap.Domain}.{domainModelMap.DomainModel.NamespaceDirectory}.{domainModelMap.DomainModelName}";
			var dataToDomainName = GetMappingName(domainModelMap.DataModelName, domainModelMap.DomainModelName, true);

			var statements = new List<StatementSyntax>
			{
				"mapping"
				.ToVariableStatement(
					dataToDomainName
					.Access("FirstOrDefault")
					.Call(
						"p".ToParameterExpression(
							"p"
							.Access("Domain")
							.Access("GetMemberExpressionString")
							.Call()
							.ToEquals("patch".Access("PropertyName"))
						)
						.ToArgument()
					)
				),
				"mapping"
				.Access("Domain")
				.IsNotNull()
				.If(
					"mapping"
					.Access("Data")
					.Access("GetMemberExpressionString")
					.Call()
					.Return()
				),
				Eshava.CodeAnalysis.SyntaxConstants.Base
				.Access("GetPropertyName")
				.Call("patch".ToArgument())
				.Return()
			};

			var methodDeclarationName = "GetPropertyName";
			var methodDeclaration = methodDeclarationName.ToMethod(
				Eshava.CodeAnalysis.SyntaxConstants.String,
				statements,
				SyntaxKind.ProtectedKeyword,
				SyntaxKind.OverrideKeyword
			);

			methodDeclaration = methodDeclaration
				.WithParameter(
					"patch"
					.ToVariableName()
					.ToParameter()
					.WithType("Patch".AsGeneric(fullDomainModelName))
				);

			return (methodDeclarationName, methodDeclaration);
		}

		private static LocalDeclarationStatementSyntax GetReadByQuery(
			InfrastructureModel model,
			string readByPropertyName,
			string readByVariableName,
			ReferenceDomainModelMap domainModelMap,
			List<QueryAnalysisItem> relatedDataModels,
			bool implementSoftDelete
		)
		{

			var interpolatedStringParts = TemplateMethods.CreateSqlQueryWithoutWhereCondition(model, domainModelMap.Domain, relatedDataModels, implementSoftDelete, false);
			var modelItem = relatedDataModels.First(m => m.DataModel.Name == model.Name && m.IsRootModel);

			interpolatedStringParts.Add(@"
				WHERE
					".Interpolate());
			interpolatedStringParts.Add(modelItem.TableAliasConstant.ToIdentifierName().Interpolate());
			interpolatedStringParts.Add(".".Interpolate());
			interpolatedStringParts.Add(Eshava.CodeAnalysis.SyntaxConstants.NameOf.Call(model.Name.Access(readByPropertyName).ToArgument()).Interpolate());
			interpolatedStringParts.Add($@" = @{readByVariableName}".Interpolate());

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

			return "query".ToVariableStatement(interpolatedStringParts.ToRawStringExpression());
		}

		private static StatementSyntax GetReadByQueryResult(ReferenceDomainModelMap domainModelMap, InfrastructureModel model, Dictionary<string, List<InfrastructureModel>> childsForModel, string readByVariableName, bool implementSoftDelete)
		{
			var parameterItems = new List<(ExpressionSyntax Property, string Name)>
			{
				(readByVariableName.ToVariableName().ToIdentifierName(), readByVariableName)
			};

			if (implementSoftDelete)
			{
				parameterItems.Add(("Status".Access("Active"), "Status"));
			}

			var mapperExpression = GetMapperExpression(domainModelMap, model, childsForModel);
			var parameterVariableDeclaration = SyntaxHelper.CreateAnonymousObject(parameterItems.ToArray());

			return "result"
				.ToVariableStatement(
					"connection"
					.Access("QueryAsync".AsGeneric(model.Name))
					.Call("query".ToArgument(), mapperExpression.ToArgument(), parameterVariableDeclaration.ToArgument())
					.Await()
				);
		}

		private static SimpleLambdaExpressionSyntax GetMapperExpression(ReferenceDomainModelMap domainModelMap, InfrastructureModel model, Dictionary<string, List<InfrastructureModel>> childsForModel)
		{
			var mapperStatements = CreateMapperStatements(null, domainModelMap, model, childsForModel, [], false).ToList();

			mapperStatements.Add(
				model.ClassificationKey
				.ToVariableName()
				.ToIdentifierName()
				.Return()
			);

			return "mapper".ToParameterExpression(mapperStatements.ToArray());
		}

		private static IEnumerable<StatementSyntax> CreateMapperStatements(InfrastructureModel parentModel, ReferenceDomainModelMap domainModelMap, InfrastructureModel model, Dictionary<string, List<InfrastructureModel>> childsForModel, HashSet<string> processedDataModels, bool checkOnlyChilds)
		{
			var mapperStatements = new List<StatementSyntax>();
			if (!checkOnlyChilds)
			{
				var modelConstantName = model.Name.GetModelConstantName().ToIdentifierName().ToArgument();
				var modelVariableName = model.ClassificationKey.ToVariableName();
				var modelType = parentModel is null
					? model.Name
					: $"{model.ClassificationKey.ToPlural()}.{model.Name}";


				if (parentModel is null)
				{
					mapperStatements.Add(
						 modelVariableName
						.ToVariableStatement(
							"mapper"
							.Access("Map".AsGeneric(modelType))
							.Call(modelConstantName)
						)
					);
				}
				else
				{
					var parentModelVariableName = parentModel.ClassificationKey.ToVariableName();
					var parentPropertyName = parentModel.Properties.FirstOrDefault(p => p.ReferenceType == p.Type && p.ReferenceType == model.Name)?.Name ?? model.ClassificationKey;

					mapperStatements.Add(
						 modelVariableName
						.ToVariableStatement(
							modelType.DefaultOf()
						)
					);

					mapperStatements.Add(
						parentModelVariableName
						.ToIdentifierName()
						.IsNotNull()
						.And(
							"mapper"
							.Access("GetValue".AsGeneric(model.IdentifierType))
							.Call(
								"Id".ToLiteralString().ToArgument(),
								modelConstantName
							)
							.GreaterThan("0".ToLiteralInt())
						)
						.If(
							modelVariableName
								.ToIdentifierName()
								.Assign(
									"mapper"
									.Access("Map".AsGeneric(modelType))
									.Call(modelConstantName)
								)
								.ToExpressionStatement(),
							parentModelVariableName
								.Access(parentPropertyName)
								.Assign(modelVariableName.ToIdentifierName())
								.ToExpressionStatement()
						)
					);
				}
			}

			if (!childsForModel.TryGetValue(model.Name, out var childs))
			{
				childs = new List<InfrastructureModel>();
			}

			foreach (var childDomainModel in domainModelMap.ChildDomainModels)
			{
				checkOnlyChilds = processedDataModels.Contains(childDomainModel.DataModelName);

				var child = childs.FirstOrDefault(c => c.Name == childDomainModel.DataModelName);
				if (child is null)
				{
					continue;
				}

				mapperStatements.AddRange(CreateMapperStatements(model, childDomainModel, child, childsForModel, processedDataModels, checkOnlyChilds));

				if (!processedDataModels.Contains(childDomainModel.DataModelName))
				{
					processedDataModels.Add(childDomainModel.DataModelName);
				}
			}

			return mapperStatements;
		}

		private static List<StatementSyntax> GetReadByCatchBlock(InfrastructureModel model, string readByVariableName, string fullDomainModelName, bool wrapTypeInIEnumerable)
		{
			var catchBlockStatements = new List<StatementSyntax>();
			var additionalDeclaration = SyntaxHelper.CreateAnonymousObject(
				(readByVariableName.ToVariableName().ToIdentifierName(), readByVariableName)
			);

			var message = $"Entity {model.Name} could not be read".ToLiteralString();

			catchBlockStatements.Add("messageGuid"
				.ToVariableStatement(
					"Logger".LogError("ScopedSettings", message, additionalDeclaration)
				)
			);

			catchBlockStatements.Add(fullDomainModelName
				.CreateInternalServerError(EshavaMessageConstant.ReadDataError, wrapTypeInIEnumerable)
				.Return()
			);

			return catchBlockStatements;
		}

		private static List<StatementSyntax> GetCreateDomainModelCode(
			InfrastructureModel model,
			Dictionary<string, List<InfrastructureModel>> childsForModel,
			ReferenceDomainModelMap domainModelMap,
			string resultVariableName,
			string fullDomainModelName,
			bool processAsSingleModel
		)
		{

			var statements = new List<StatementSyntax>();

			if (processAsSingleModel)
			{
				statements.Add(
					resultVariableName
					.Access("Any")
					.Call()
					.Not()
					.If(CommonNames.RESPONSEDATA
						.AsGeneric(fullDomainModelName)
						.ToInstance(Eshava.CodeAnalysis.SyntaxConstants.Null.ToArgument())
						.Return()
					)
				);
			}

			statements.AddRange(GetGroupDataModelStatements(model, domainModelMap, childsForModel));
			statements.AddRange(GetCreateDomainModelStatements(null, model, domainModelMap, childsForModel));

			return statements;
		}

		private static IEnumerable<StatementSyntax> GetCreateDomainModelStatements(
			InfrastructureModel parentModel,
			InfrastructureModel model,
			ReferenceDomainModelMap domainModelMap,
			Dictionary<string, List<InfrastructureModel>> childsForModel
		)
		{
			var statements = new List<StatementSyntax>();
			var isTopLevelCall = parentModel is null;
			var fullDomainModelName = $"Domain.{domainModelMap.Domain}.{domainModelMap.DomainModel.NamespaceDirectory}.{domainModelMap.DomainModelName}";

			var modelType = isTopLevelCall
				? model.Name
				: $"{model.ClassificationKey.ToPlural()}.{model.Name}";

			var modelItemName = $"{model.ClassificationKey.ToVariableName()}Item";
			var dataModelItemsName = $"{modelItemName}s";
			var domainModelItemsName = domainModelMap.IsChildDomainModel
				? $"{modelItemName}sFor{domainModelMap.DomainModelName}"
				: $"{modelItemName}s";
			var patchesVariableName = $"{domainModelMap.DomainModelName.ToVariableName()}Patches";
			var domainModelName = $"{domainModelMap.DomainModelName.ToVariableName()}Model";
			var domainModelListName = $"{domainModelName}s";

			var dataToDomainName = GetMappingName(domainModelMap.DataModelName, domainModelMap.DomainModelName, true);

			statements.Add(
				domainModelListName
				.ToVariableStatement(
					"List".AsGeneric(fullDomainModelName).ToInstance()
				)
			);

			if (!childsForModel.TryGetValue(model.Name, out var childs))
			{
				childs = [];
			}

			var loopStatements = new List<StatementSyntax>();
			var loopParameters = new List<ArgumentSyntax>
			{
				patchesVariableName.ToArgument()
			};

			foreach (var childDomainModel in domainModelMap.ChildDomainModels)
			{
				var child = childs.FirstOrDefault(c => c.Name == childDomainModel.DataModelName);
				if (child is null)
				{
					continue;
				}

				var childStatements = GetCreateDomainModelStatements(model, child, childDomainModel, childsForModel);
				loopStatements.AddRange(childStatements);
				loopParameters.Add($"{childDomainModel.DomainModelName.ToVariableName()}Models".ToArgument());
			}

			loopStatements.Add(patchesVariableName
				.ToVariableStatement(
					"GenerateDomainPatchList".AsGeneric(modelType, fullDomainModelName)
					.Call(modelItemName.ToArgument(), dataToDomainName.ToArgument())
				)
			);

			loopParameters.Add("_validationEngine".ToArgument());

			loopStatements.Add(domainModelName
				.ToVariableStatement(
					fullDomainModelName
					.Access("DataToInstance")
					.Call(loopParameters.ToArray())
				)
			);

			loopStatements.Add(
				domainModelListName
				.Access("Add")
				.Call(domainModelName.ToIdentifierName().ToArgument())
				.ToExpressionStatement()
			);

			if (!isTopLevelCall)
			{
				var parentIdentifier = $"{parentModel.ClassificationKey.ToVariableName()}Item".Access("Id");
				statements.Add(
					domainModelItemsName
					.ToVariableStatement(
						$"{dataModelItemsName}ByParent"
						.Access("ContainsKey")
						.Call(parentIdentifier.ToArgument())
						.ShortIf(
							$"{dataModelItemsName}ByParent".ToIdentifierName().AccessDictionary(parentIdentifier.ToArgument()),
							"List".AsGeneric(modelType).ToInstance()
						)
					)
				);

				if (!domainModelMap.DomainModel.DataModelTypeProperty.IsNullOrEmpty())
				{
					var dataModelProperty = model.Properties.FirstOrDefault(p => p.Name == domainModelMap.DomainModel.DataModelTypeProperty);
					if (dataModelProperty is not null)
					{
						ExpressionSyntax typeValue = null;
						typeValue = dataModelProperty.Type switch
						{
							"int" => domainModelMap.DomainModel.DataModelTypePropertyValue.ToLiteralInt(),
							"long" => domainModelMap.DomainModel.DataModelTypePropertyValue.ToLiteralLong(),
							"string" => domainModelMap.DomainModel.DataModelTypePropertyValue.ToLiteralString(),
							_ => domainModelMap.DomainModel.DataModelTypePropertyValue.ToIdentifierName(),
						};

						statements.Add(
							domainModelItemsName
							.ToIdentifierName()
							.Assign(
								domainModelItemsName
								.Access("Where")
								.Call(
									"p"
									.ToParameterExpression(
										"p"
										.Access(dataModelProperty.Name)
										.ToEquals(typeValue)
									)
									.ToArgument()
								)
								.Access("ToList")
								.Call()
							)
							.ToExpressionStatement()
						);
					}
				}
			}

			statements.Add(
				domainModelItemsName.ToIdentifierName().ForEach(modelItemName, loopStatements)
			);

			return statements;
		}

		private static IEnumerable<StatementSyntax> GetGroupDataModelStatements(InfrastructureModel model, ReferenceDomainModelMap domainModelMap, Dictionary<string, List<InfrastructureModel>> childsForModel)
		{
			var statments = new List<StatementSyntax>();
			(var rawItemsStatments, var itemsStatments, _) = GetGroupDataModelStatements(null, model, domainModelMap, childsForModel, [], false);

			statments.AddRange(rawItemsStatments);
			statments.AddRange(itemsStatments);

			return statments;
		}

		private static (IEnumerable<StatementSyntax> RawItems, IEnumerable<StatementSyntax> Items, HashSet<string> processDataModels) GetGroupDataModelStatements(
			InfrastructureModel parentModel,
			InfrastructureModel model,
			ReferenceDomainModelMap domainModelMap,
			Dictionary<string, List<InfrastructureModel>> childsForModel,
			HashSet<string> processDataModels,
			bool checkOnlyChilds
		)
		{
			var rawItemsStatments = new List<StatementSyntax>();
			var itemsStatments = new List<StatementSyntax>();
			if (!checkOnlyChilds)
			{
				var isTopLevelCall = parentModel is null;

				var itemsListName = isTopLevelCall
					? $"{model.ClassificationKey.ToVariableName()}Items"
					: $"{model.ClassificationKey.ToVariableName()}ItemsByParent";

				if (isTopLevelCall)
				{
					rawItemsStatments.Add(
						$"{model.ClassificationKey.ToVariableName()}RawItems"
						.ToVariableStatement(
							"result".ToIdentifierName()
						)
					);
				}
				else
				{
					var propertyName = parentModel.Properties.FirstOrDefault(p => p.ReferenceType == p.Type && p.ReferenceType == model.Name)?.Name ?? model.ClassificationKey;

					rawItemsStatments.Add(
						$"{model.ClassificationKey.ToVariableName()}RawItems"
						.ToVariableStatement(
							$"{parentModel.ClassificationKey.ToVariableName()}RawItems"
							.Access("Where")
							.Call("p".ToParameterExpression("p".Access(propertyName).IsNotNull()).ToArgument())
							.Access("Select")
							.Call("p".ToPropertyExpression(propertyName).ToArgument())
							.Access("ToList")
							.Call()
						)
					);
				}

				var itemGroupExpression = $"{model.ClassificationKey.ToVariableName()}RawItems"
						.Access("GroupBy")
						.Call("p".ToPropertyExpression("Id").ToArgument())
						.Access("Select")
						.Call("p".ToParameterExpression("p".Access("First").Call()).ToArgument());

				if (isTopLevelCall)
				{
					itemGroupExpression = itemGroupExpression
						.Access("ToList")
						.Call();
				}
				else
				{
					var parentProperty = model.Properties.First(p => p.IsReference && p.ReferenceType == parentModel.Name);
					itemGroupExpression = itemGroupExpression
						.Access("GroupBy")
						.Call("p".ToPropertyExpression(parentProperty.Name).ToArgument())
						.Access("ToDictionary")
						.Call("p".ToPropertyExpression("Key").ToArgument(), "p".ToParameterExpression("p".Access("ToList").Call()).ToArgument())
						;
				}

				itemsStatments.Add(
					itemsListName
					.ToVariableStatement(itemGroupExpression)
				);
			}

			if (!childsForModel.TryGetValue(model.Name, out var childs))
			{
				childs = [];
			}

			foreach (var childDomainModel in domainModelMap.ChildDomainModels)
			{
				checkOnlyChilds = processDataModels.Contains(childDomainModel.DataModelName);

				var child = childs.FirstOrDefault(c => c.Name == childDomainModel.DataModelName);
				if (child is null)
				{
					continue;
				}

				if (!processDataModels.Contains(childDomainModel.DataModelName))
				{
					processDataModels.Add(childDomainModel.DataModelName);
				}

				(var raw, var items, processDataModels) = GetGroupDataModelStatements(model, child, childDomainModel, childsForModel, processDataModels, checkOnlyChilds);

				rawItemsStatments.AddRange(raw);
				itemsStatments.AddRange(items);
			}

			return (rawItemsStatments, itemsStatments, processDataModels);
		}

		private static List<QueryAnalysisItem> CollectDataModelsForReferenceProperties(InfrastructureModel model, ReferenceDomainModelMap domainModelMap, Dictionary<string, List<InfrastructureModel>> childsForModel, bool isTopLevelCall)
		{
			var items = new List<QueryAnalysisItem>();

			if (isTopLevelCall)
			{
				var tableAlis = model.Name.CreateModelConstantField();
				items.Add(new QueryAnalysisItem
				{
					ParentDomain = null,
					Domain = domainModelMap.Domain,
					ParentDataModel = model,
					DataModel = model,
					ParentProperty = null,
					Property = null, // Id property is not configured, its provided by the abstract data base model and is considered as virtual property
					ParentDtoName = null,
					ParentDtoPropertyName = null,
					DtoName = null,
					DtoProperty = new Models.Application.ApplicationUseCaseDtoProperty { Name = "*" },
					IsEnumerable = false,
					IsGroupBy = false,
					TableAliasConstant = tableAlis.FieldName,
					TableAliasField = tableAlis.Declaration,
					IsRootModel = true
				});
			}

			if (!childsForModel.TryGetValue(model.Name, out var childModels))
			{
				return items;
			}

			foreach (var childDomainModel in domainModelMap.ChildDomainModels)
			{
				var childModel = childModels.FirstOrDefault(cm => cm.Name == childDomainModel.DataModelName);
				if (childModel is null)
				{
					continue;
				}

				var tableAlis = childModel.Name.CreateModelConstantField();
				var propertyNameForParent = $"{model.Name}Id";
				var childProperty = childModel.Properties.SingleOrDefault(p => p.IsReference && p.ReferenceType == model.Name && p.Name == propertyNameForParent)
					?? childModel.Properties.SingleOrDefault(p => p.IsReference && p.ReferenceType == model.Name && !p.Type.EndsWith("?"))
					?? childModel.Properties.SingleOrDefault(p => p.IsReference && p.ReferenceType == model.Name)
					;

				items.Add(new QueryAnalysisItem
				{
					ParentDomain = domainModelMap.Domain,
					Domain = childDomainModel.Domain,
					ParentDataModel = model,
					DataModel = childModel,
					ParentProperty = null, // Id property is not configured, its provided by the abstract data base model and is considered as virtual property
					Property = childProperty,
					ParentDtoName = null,
					ParentDtoPropertyName = null,
					DtoName = null,
					DtoProperty = new Models.Application.ApplicationUseCaseDtoProperty { Name = "*" },
					IsEnumerable = true,
					IsGroupBy = true,
					TableAliasConstant = tableAlis.FieldName,
					TableAliasField = tableAlis.Declaration,
				});

				items.AddRange(CollectDataModelsForReferenceProperties(childModel, childDomainModel, childsForModel, false));
			}

			return items;
		}

		private static string GetMappingName(string dataModelName, string domainModelName, bool dataToDomain)
		{
			return dataToDomain
				? $"_{dataModelName.ToVariableName()}To{domainModelName}"
				: $"_{domainModelName.ToVariableName()}To{dataModelName}"
				;
		}

		private static List<(string FieldName, FieldDeclarationSyntax Declaration)> CollectDataToDomainPropertyMappings(ReferenceDomainModelMap domainModelMap)
		{
			var dataModelType = domainModelMap.IsChildDomainModel
				? $"{domainModelMap.ClassificationKey.ToPlural()}.{domainModelMap.DataModelName}"
				: domainModelMap.DataModelName;

			var mappings = new List<(string FieldName, FieldDeclarationSyntax Declaration)>();
			var fullDomainModelName = $"Domain.{domainModelMap.Domain}.{domainModelMap.DomainModel.NamespaceDirectory}.{domainModelMap.DomainModelName}";

			var domainModelProperties = domainModelMap.DomainModel.Properties.Where(p => !p.DataModelPropertyName.IsNullOrEmpty()).ToList();

			var dataToDomainName = GetMappingName(domainModelMap.DataModelName, domainModelMap.DomainModelName, true);

			var dataPropertyType = "Data".ToPropertyExpressionTupleElement(dataModelType);
			var domainPropertyType = "Domain".ToPropertyExpressionTupleElement(fullDomainModelName);

			var dataToDomainType = "List".AsGeneric(dataPropertyType.ToTupleType(domainPropertyType));

			var dataToDomainInstance = domainModelProperties.Count == 0
				? dataToDomainType.ToCollectionExpression()
				: dataToDomainType.ToCollectionExpressionWithInitializer(
					domainModelProperties
					.Select(p => "p"
						.ToPropertyExpression(p.DataModelPropertyName)
						.ToArgument()
						.ToTuple("p"
							.ToPropertyExpression(p.Name)
							.ToArgument()
						)
					).ToArray()
				);

			mappings.Add((dataToDomainName, dataToDomainName.ToStaticReadonlyField(dataToDomainType, dataToDomainInstance)));

			foreach (var childDomainModel in domainModelMap.ChildDomainModels)
			{
				mappings.AddRange(CollectDataToDomainPropertyMappings(childDomainModel));
			}

			return mappings;
		}

		private class ValueObjectCacheItem
		{
			public ReferenceDomainModel DomainModel { get; set; }
			public Dictionary<string, DomainModelProperty> PropertyMappings { get; set; }
			public Dictionary<string, DomainModelProperty> Properties { get; set; }
		}

	}
}