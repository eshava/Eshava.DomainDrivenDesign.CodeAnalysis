using System;
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
			Dictionary<string, List<InfrastructureModel>> childsForModel,
			Dictionary<string, InfrastructureModel> modelsForDomain,
			ReferenceMap domainModelReferenceMap,
			IEnumerable<InfrastructureCodeSnippet> codeSnippets
		)
		{
			var repositoryCodeSnippet = codeSnippets
				.FirstOrDefault(cs => cs.ApplyOnRepository)
				?? new InfrastructureCodeSnippet();

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

				unitInformation.AddMethod(CreateReadMethod(model, domainModelMap, fullDomainModelName, childsForModel, relatedDataModels, repositoryCodeSnippet.PropertyStatements, project.ImplementSoftDelete));
				var valueObjectCreateMethods = CreateValueObjectCreateMethods(model, domainModelMap, domainModelReferenceMap, childsForModel, modelsForDomain, true);
				foreach (var valueObjectCreateMethod in valueObjectCreateMethods)
				{
					unitInformation.AddMethod(valueObjectCreateMethod);
				}
			}

			var baseType = model.IsChild
				? baseClass.AsGeneric(fullDomainModelName, $"{domainModelMap.AggregateDomainModel.ClassificationKey.ToPlural()}.{domainModelMap.AggregateDomainModel.DomainModelName}CreationBag", model.Name, model.IdentifierType, project.ScopedSettingsClass).ToSimpleBaseType()
				: baseClass.AsGeneric(fullDomainModelName, model.Name, model.IdentifierType, project.ScopedSettingsClass).ToSimpleBaseType();
			var repositoryInterface = $"I{className}".ToType().ToSimpleBaseType();
			unitInformation.AddBaseType(baseType, repositoryInterface);
			unitInformation.AddUsing(alternativeClass?.Using);

			CheckAndAddProviderReferences(unitInformation, alternativeClass, repositoryCodeSnippet);

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
					unitInformation.AddMethod(CreateReadForMethod(model, childsForModel, domainModelMap, foreignKeyReference, relatedDataModels, fullDomainModelName, repositoryCodeSnippet.PropertyStatements, project.ImplementSoftDelete));
				}
			}

			unitInformation.AddLogger(className, true);

			unitInformation.AddMethod(CreateFromDomainModelMethod(model, fullDomainModelName, parentModel, domainModelMap, domain, modelsForDomain, domainModelReferenceMap, repositoryCodeSnippet.PropertyStatements));
			unitInformation.AddMethod(CreateGetPropertyNameMethod(domainModelMap, model, modelsForDomain));

			var valueObjectPatchMethod = CreateMapPatchesForValueObjectsMethod(model, domainModelMap, modelsForDomain);
			if (!valueObjectPatchMethod.Name.IsNullOrEmpty())
			{
				unitInformation.AddMethod(valueObjectPatchMethod);
			}

			return unitInformation.CreateCodeString();
		}

		private static void CheckAndAddProviderReferences(UnitInformation unitInformation, InfrastructureProjectAlternativeClass alternativeClass, InfrastructureCodeSnippet codeSnippet)
		{
			foreach (var additionalUsing in codeSnippet.AdditionalUsings ?? [])
			{
				unitInformation.AddUsing(additionalUsing);
			}

			foreach (var constructorParameter in codeSnippet.ConstructorParameters ?? [])
			{
				unitInformation.AddUsing(constructorParameter.Using);
				unitInformation.AddConstructorParameter(constructorParameter.Name, constructorParameter.Type.ToIdentifierName(), Enums.ParameterTargetTypes.Field);
			}

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
			IEnumerable<InfrastructureModelPropertyCodeSnippet> codeSnippets,
			bool implementSoftDelete
		)
		{
			var readByPropertyName = "Id";
			var readByVariableName = $"{model.ClassificationKey}Id";
			(var query, var queryParameters) = GetReadByQuery(model, readByPropertyName, readByVariableName, domainModelMap, relatedDataModels, codeSnippets, implementSoftDelete);


			var tryBlockStatements = new List<StatementSyntax>
			{
				query
			};

			var usingInnerStatments = new List<StatementSyntax>
			{
				GetReadByQueryResult(domainModelMap, model, childsForModel, readByVariableName, queryParameters, implementSoftDelete)
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

		private static IEnumerable<(string Name, MemberDeclarationSyntax)> CreateValueObjectCreateMethods(
			InfrastructureModel model,
			ReferenceDomainModelMap domainModelMap,
			ReferenceMap domainModelReferenceMap,
			Dictionary<string, List<InfrastructureModel>> childsForModel,
			Dictionary<string, InfrastructureModel> modelsForDomain,
			bool isTopCall
		)
		{
			var methods = new List<(string Name, MemberDeclarationSyntax)>();
			var valueObjectMethod = CreateValueObjectCreateMethods(model, domainModelMap, modelsForDomain, isTopCall);
			if (!valueObjectMethod.Name.IsNullOrEmpty())
			{
				methods.Add(valueObjectMethod);
			}

			foreach (var childModel in domainModelMap.ChildDomainModels)
			{
				if (!childsForModel.TryGetValue(model.Name, out var childModels))
				{
					continue;
				}

				var childDataModel = childModels.FirstOrDefault(m => m.Name == childModel.DataModelName);
				if (childDataModel is null)
				{
					continue;
				}

				if (!domainModelReferenceMap.TryGetDomainModel(childModel.Domain, childModel.DomainModelName, out var childDomainModelMap))
				{
					continue;
				}

				var childMethodsResult = CreateValueObjectCreateMethods(childDataModel, childDomainModelMap, domainModelReferenceMap, childsForModel, modelsForDomain, false);
				if (childMethodsResult.Any())
				{
					methods.AddRange(childMethodsResult);
				}
			}

			return methods;
		}

		private static (string Name, MemberDeclarationSyntax) CreateValueObjectCreateMethods(
			InfrastructureModel dataModel,
			ReferenceDomainModelMap domainModelMap,
			Dictionary<string, InfrastructureModel> modelsForDomain,
			bool isTopCall
		)
		{
			var valueObjects = domainModelMap.ForeignKeyReferences.Where(reference => reference.DomainModel.IsValueObject).ToList();
			if (valueObjects.Count == 0)
			{
				return (null, null);
			}

			var dataModelProperties = dataModel.Properties.ToDictionary(p => p.Name, p => p);

			var @namespace = $"Domain.{domainModelMap.Domain}.{domainModelMap.DomainModel.NamespaceDirectory}";
			var fullDomainModelName = $"{@namespace}.{domainModelMap.DomainModelName}";
			var statements = new List<StatementSyntax>
			{
				"patches"
				.ToVariableStatement(
					"List"
					.AsGeneric("Patch".AsGeneric(fullDomainModelName))
					.ToInstance()
				)
			};

			foreach (var valueObject in valueObjects)
			{
				var domainModelProperty = domainModelMap.DomainModel.Properties.FirstOrDefault(p => p.Name == valueObject.PropertyName);
				if (domainModelProperty is null)
				{
					continue;
				}

				if (domainModelProperty.ProcessAsUnit)
				{
					(var valueObjectDataModel, var dataModelProperty) = ValueObjectDataModelForValueObjectModel(domainModelProperty, dataModelProperties, modelsForDomain);
					if (valueObjectDataModel is null)
					{
						continue;
					}

					var dataValueObjectStatements = new List<StatementSyntax>();
					var dataInstance = "dataInstance".Access(dataModelProperty.Name);
					var dataValueObjectProperties = valueObjectDataModel.Properties.ToDictionary(p => p.Name, p => p);

					ConvertToValueObject(dataValueObjectStatements, valueObject, dataInstance, domainModelMap.DomainModelName, @namespace, fullDomainModelName, dataModelProperty.Name, dataValueObjectProperties);

					statements.Add(
						dataInstance
						.IsNotNull()
						.If(dataValueObjectStatements.ToArray())
					);

					continue;
				}

				ConvertToValueObject(statements, valueObject, "dataInstance".ToIdentifierName(), domainModelMap.DomainModelName, @namespace, fullDomainModelName, null, dataModelProperties);
			}

			statements.Add(
				"patches"
				.ToIdentifierName()
				.Return()
			);

			var methodDeclarationName = $"CreateValueObjectsFor{domainModelMap.DomainModelName}";
			var methodDeclaration = methodDeclarationName.ToMethod(
				"IEnumerable".AsGeneric("Patch".AsGeneric(fullDomainModelName)),
				statements,
				SyntaxKind.PrivateKeyword
			);

			var dataModelReferenceType = isTopCall
				? dataModel.Name.ToType()
				: $"{dataModel.ClassificationKey.ToPlural()}.{dataModel.Name}".ToType();

			var dataModelReferenceParameter = "dataInstance".ToParameter().WithType(dataModelReferenceType);
			var validationEngineParameter = ApplicationNames.Engines.VALIDATIONENGINE.ToParameter().WithType(ApplicationNames.Engines.VALIDATIONENGINETYPE.ToType());

			methodDeclaration = methodDeclaration.WithParameter(dataModelReferenceParameter, validationEngineParameter);

			return (methodDeclarationName, methodDeclaration);
		}

		private static (InfrastructureModel ValueObjectDataModel, InfrastructureModelProperty DataModelProperty) ValueObjectDataModelForValueObjectModel(
			DomainModelProperty domainModelProperty,
			Dictionary<string, InfrastructureModelProperty> dataModelProperties,
			Dictionary<string, InfrastructureModel> modelsForDomain
		)
		{
			var dataModelPropertyName = domainModelProperty.DataModelPropertyName.IsNullOrEmpty()
					? domainModelProperty.Name
					: domainModelProperty.DataModelPropertyName;

			if (!dataModelProperties.TryGetValue(dataModelPropertyName, out var dataModelProperty))
			{
				return (null, null);
			}

			if (!modelsForDomain.TryGetValue(dataModelProperty.Type, out var valueObjectDataModel))
			{
				return (null, null);
			}

			if (valueObjectDataModel.UseCustomMapping)
			{
				return (null, null);
			}

			return (valueObjectDataModel, dataModelProperty);
		}

		private static void ConvertToValueObject(
			List<StatementSyntax> statements,
			ReferenceDomainModel valueObject,
			ExpressionSyntax dataInstance,
			string domainModelName,
			string @namespace,
			string fullDomainModelName,
			string dataModelPropertyNamePrefix,
			Dictionary<string, InfrastructureModelProperty> dataModelProperties
		)
		{
			var constructorParametersWithoutMapping = new List<ArgumentSyntax>();
			var constructorParametersWithMapping = new List<ArgumentSyntax>();
			var valueObjectVariableName = valueObject.PropertyName.ToVariableName();

			foreach (var property in valueObject.DomainModel.Properties)
			{
				var dataModelPropertyName = property.DataModelPropertyName.IsNullOrEmpty()
					? property.Name
					: property.DataModelPropertyName;

				var dataModelPropertyValueAccess = dataInstance
					.Access(dataModelPropertyName);

				constructorParametersWithoutMapping.Add(
					dataModelPropertyValueAccess
					.ToArgument()
				);

				if (!dataModelProperties.TryGetValue(dataModelPropertyName, out var dataModelProperty))
				{
					constructorParametersWithMapping.Add(
						dataModelPropertyValueAccess
						.ToArgument()
					);

					continue;
				}

				var propertyMappingKey = dataModelPropertyNamePrefix.IsNullOrEmpty()
					? dataModelProperty.Name
					: $"{dataModelPropertyNamePrefix}.{dataModelProperty.Name}";

				constructorParametersWithMapping.Add(
					$"{domainModelName.ToFieldName()}PropertyValueToDomainMappings"
					.Access("TryGetValue")
					.Call(propertyMappingKey.ToLiteralArgument(), $"out var {dataModelProperty.Name.ToVariableName()}Mapped".ToArgument())
					.ShortIf(
						$"{dataModelProperty.Name.ToVariableName()}Mapped".ToIdentifierName().Call(dataModelPropertyValueAccess.ToArgument()).Cast(dataModelProperty.Type.ToType()),
						dataModelPropertyValueAccess
					)
					.ToArgument()
				);
			}

			statements.Add(
				valueObjectVariableName
				.ToVariableStatement(
					$"{domainModelName.ToFieldName()}PropertyValueToDomainMappings"
					.Access("Count")
					.ToEquals("0".ToLiteralInt())
					.ShortIf(
						$"{@namespace}.{valueObject.DomainModelName}"
							.ToType()
							.ToInstance(constructorParametersWithoutMapping.ToArray()),
						$"{@namespace}.{valueObject.DomainModelName}"
							.ToType()
							.ToInstance(constructorParametersWithMapping.ToArray())
					)
				)
			);

			var validationResult = $"{valueObjectVariableName}ValidationResult";
			statements.Add(
				validationResult
				.ToVariableStatement(
					ApplicationNames.Engines.VALIDATIONENGINE
					.Access("Validate")
					.Call(valueObjectVariableName.ToArgument())
				)
			);

			statements.Add(
				validationResult
				.Access("IsValid")
				.If(
					"patches"
					.Access("Add")
					.Call(
						"Patch"
						.AsGeneric(fullDomainModelName)
						.Access("Create")
						.Call(
							"p".ToPropertyExpression(valueObject.PropertyName).ToArgument(),
							valueObjectVariableName.ToArgument()
						)
						.ToArgument()
					)
					.ToExpressionStatement()
				)
			);
		}

		private static (string Name, MemberDeclarationSyntax) CreateFromDomainModelMethod(
			InfrastructureModel dataModel,
			string fullDomainModelName,
			InfrastructureModel parentDataModel,
			ReferenceDomainModelMap domainModelMap,
			string domain,
			Dictionary<string, InfrastructureModel> modelsForDomain,
			ReferenceMap domainModelReferenceMap,
			IEnumerable<InfrastructureModelPropertyCodeSnippet> codeSnippets
		)
		{
			var statements = new List<StatementSyntax>
			{
				"model"
					.ToIdentifierName()
					.IsNull()
					.If(Eshava.CodeAnalysis.SyntaxConstants.Null.Return())
			};

			var withMappingStatements = new List<StatementSyntax>();
			var withoutMappingStatements = new List<StatementSyntax>();

			var dataModelInstance = dataModel.Name.ToType().ToInstance();
			var instance = "instance".ToVariableStatement(dataModelInstance);
			statements.Add(instance);

			var instanceVar = "instance".ToIdentifierName();
			var modelVar = "model".ToIdentifierName();

			var domainModelPropertiesByMapperProperty = domainModelMap.DomainModel.Properties
				.Where(p => !p.DataModelPropertyName.IsNullOrEmpty())
				.ToDictionary(p => p.DataModelPropertyName, p => p);

			var domainModelProperties = domainModelMap.DomainModel.Properties
				.ToDictionary(p => p.Name, p => p);

			// Domain model name
			var valueObjectCache = new Dictionary<string, ValueObjectCacheItem>();

			// Domain model property name with a value object as type -> Mapping from data model property to value object property
			var valueObjectAssignments = new Dictionary<string, (InfrastructureModelProperty DataPropertyParent, List<(InfrastructureModelProperty DataProperty, DomainModelProperty DomainModelProperty)> Assignments)>();

			foreach (var dataModelProperty in dataModel.Properties)
			{
				if (dataModelProperty.SkipFromDomainModel)
				{
					if (dataModel.IsChild)
					{
						if (parentDataModel.Properties.Any(p => p.AddToCreationBag && p.Name == dataModelProperty.Name))
						{
							statements.Add(
								instanceVar
								.Access(dataModelProperty.Name)
								.Assign("creationBag".Access(dataModelProperty.Name))
								.ToExpressionStatement()
							);
						}

						continue;
					}

					var propertySnippet = codeSnippets.FirstOrDefault(cs => cs.CodeSnippeKey == $"{dataModel.Name}.{dataModelProperty.Name}" && cs.IsMapping)
							?? codeSnippets.FirstOrDefault(cs => cs.CodeSnippeKey == dataModelProperty.Name && cs.IsMapping);

					if (propertySnippet is null)
					{
						continue;
					}

					statements.Add(
						instanceVar
						.Access(dataModelProperty.Name)
						.Assign(propertySnippet.Expression)
						.ToExpressionStatement()
					);

					continue;
				}

				if (dataModel.IsChild && dataModelProperty.Name == $"{parentDataModel.ClassificationKey}Id")
				{
					statements.Add(
						instanceVar
						.Access(dataModelProperty.Name)
						.Assign("creationBag".Access($"{parentDataModel.ClassificationKey}Id"))
						.ToExpressionStatement()
					);
				}
				else if (dataModel.IsChild && parentDataModel.Properties.Any(p => p.AddToCreationBag && p.Name == dataModelProperty.Name))
				{
					statements.Add(
						instanceVar
						.Access(dataModelProperty.Name)
						.Assign("creationBag".Access(dataModelProperty.Name))
						.ToExpressionStatement()
					);
				}
				else if (dataModelProperty.Name == domainModelMap.DomainModel.DataModelTypeProperty)
				{
					var propertyValue = domainModelMap.DomainModel.DataModelTypePropertyValue;
					ExpressionSyntax valueExpression = dataModelProperty.Type switch
					{
						"int" => propertyValue.ToLiteralInt(),
						"long" => propertyValue.ToLiteralLong(),
						"string" => propertyValue.ToLiteralString(),
						_ => propertyValue.ToIdentifierName()
					};

					statements.Add(
						instanceVar
						.Access(dataModelProperty.Name)
						.Assign(valueExpression)
						.ToExpressionStatement()
					);
				}
				else
				{
					if (!domainModelPropertiesByMapperProperty.TryGetValue(dataModelProperty.Name, out var domainModelProperty))
					{
						domainModelProperties.TryGetValue(dataModelProperty.Name, out domainModelProperty);
					}

					// Check for value object
					if (domainModelProperty is null)
					{
						CollectValueObjectPropertiesForDataModelCreation(dataModelProperty, domainModelMap, valueObjectCache, valueObjectAssignments);

						continue;
					}

					// Check for value object
					// Check if property is value object
					if (domainModelProperty.ProcessAsUnit)
					{
						domainModelReferenceMap.TryGetDomainModel(domain, domainModelProperty.Type.Replace("?", ""), out var valueObjectMap);
						modelsForDomain.TryGetValue(dataModelProperty.Type.Replace("?", ""), out var valueObjectDataModel);

						if (valueObjectMap is null)
						{
							continue;
						}

						if (valueObjectDataModel is not null && !valueObjectDataModel.UseCustomMapping)
						{
							CollectValueObjectPropertiesForDataModelCreation(dataModelProperty, valueObjectDataModel, domainModelMap, valueObjectMap, valueObjectCache, valueObjectAssignments);

							continue;
						}
					}

					var domainModelPropertyAccessor = modelVar.Access(domainModelProperty.Name);
					var statementWithMapping = CreatePropertyAssignment(instanceVar, null, dataModelProperty, domainModelProperty, domainModelPropertyAccessor, true);
					if (statementWithMapping is not null)
					{
						withMappingStatements.Add(statementWithMapping);
					}

					var statementWithoutMapping = CreatePropertyAssignment(instanceVar, null, dataModelProperty, domainModelProperty, domainModelPropertyAccessor, false);
					if (statementWithoutMapping is not null)
					{
						withoutMappingStatements.Add(statementWithoutMapping);
					}
				}
			}

			foreach (var assigments in valueObjectAssignments)
			{
				var withMappingAssignmentStatements = new List<StatementSyntax>();
				var withoutMappingAssignmentStatements = new List<StatementSyntax>();
				var valueObjectAccess = modelVar.Access(assigments.Key);

				if (assigments.Value.DataPropertyParent is not null)
				{
					var modelInstance = instanceVar
						.Access(assigments.Value.DataPropertyParent.Name)
						.Assign(
							assigments.Value.DataPropertyParent.Type.Replace("?", "")
							.ToIdentifierName()
							.ToInstance()
						)
						.ToExpressionStatement();

					withMappingAssignmentStatements.Add(modelInstance);
					withoutMappingAssignmentStatements.Add(modelInstance);
				}

				foreach (var propertyMapping in assigments.Value.Assignments)
				{
					var domainModelPropertyAccessor = valueObjectAccess.Access(propertyMapping.DomainModelProperty.Name);
					withMappingAssignmentStatements.Add(CreatePropertyAssignment(instanceVar, assigments.Value.DataPropertyParent?.Name, propertyMapping.DataProperty, propertyMapping.DomainModelProperty, domainModelPropertyAccessor, true));
					withoutMappingAssignmentStatements.Add(CreatePropertyAssignment(instanceVar, assigments.Value.DataPropertyParent?.Name, propertyMapping.DataProperty, propertyMapping.DomainModelProperty, domainModelPropertyAccessor, false));
				}

				withMappingStatements.Add(valueObjectAccess
					.IsNotNull()
					.If(withMappingAssignmentStatements.ToArray())
				);

				withoutMappingStatements.Add(valueObjectAccess
					.IsNotNull()
					.If(withoutMappingAssignmentStatements.ToArray())
				);
			}

			statements.Add(
				"PropertyValueToDataMappings"
				.Access("Count")
				.ToEquals("0".ToLiteralInt())
				.If(withoutMappingStatements.ToArray())
				.Else(withMappingStatements.ToArray())
			);

			if (dataModel.IsChild)
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
				dataModel.Name.ToType(),
				statements,
				SyntaxKind.ProtectedKeyword,
				SyntaxKind.OverrideKeyword
			);

			var domainModelReferenceType = fullDomainModelName.ToType();
			var domainModelReferenceParameter = "model".ToParameter().WithType(domainModelReferenceType);

			if (dataModel.IsChild)
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
			InfrastructureModelProperty property,
			ReferenceDomainModelMap domainModelMap,
			Dictionary<string, ValueObjectCacheItem> valueObjectCache,
			Dictionary<string, (InfrastructureModelProperty DataPropertyParent, List<(InfrastructureModelProperty DataProperty, DomainModelProperty DomainModelProperty)> Assignments)> valueObjectAssignments
		)
		{
			foreach (var dmProperty in domainModelMap.DomainModel.Properties)
			{
				var foreignKeyReference = domainModelMap.ForeignKeyReferences.FirstOrDefault(fkr => fkr.DomainModelName == dmProperty.Type);
				if (foreignKeyReference is null)
				{
					continue;
				}

				if (!valueObjectCache.TryGetValue(foreignKeyReference.DomainModelName, out var valueObjectCacheItem))
				{
					var valueObjectPropertiesByMapperProperty = foreignKeyReference.DomainModel.Properties
						.Where(p => !p.DataModelPropertyName.IsNullOrEmpty())
						.ToDictionary(p => p.DataModelPropertyName, p => p);

					var valueObjectProperties = foreignKeyReference.DomainModel.Properties
						.ToDictionary(p => p.Name, p => p);

					valueObjectCacheItem = new ValueObjectCacheItem
					{
						DomainModel = foreignKeyReference,
						PropertyMappings = valueObjectPropertiesByMapperProperty,
						Properties = valueObjectProperties,
					};

					valueObjectCache.Add(foreignKeyReference.DomainModelName, valueObjectCacheItem);
				}

				if (!valueObjectCacheItem.PropertyMappings.TryGetValue(property.Name, out var domainModelProperty))
				{
					valueObjectCacheItem.Properties.TryGetValue(property.Name, out domainModelProperty);
				}

				if (domainModelProperty is null)
				{
					continue;
				}

				if (!valueObjectAssignments.TryGetValue(dmProperty.Name, out var assigments))
				{
					assigments = (null, []);
					valueObjectAssignments.Add(dmProperty.Name, assigments);
				}

				assigments.Assignments.Add((property, domainModelProperty));

				break;
			}
		}

		private static void CollectValueObjectPropertiesForDataModelCreation(
			InfrastructureModelProperty parentdataModelProperty,
			InfrastructureModel valueObjectDataModel,
			ReferenceDomainModelMap domainModelMap,
			ReferenceDomainModelMap valueObjectMap,
			Dictionary<string, ValueObjectCacheItem> valueObjectCache,
			Dictionary<string, (InfrastructureModelProperty DataPropertyParent, List<(InfrastructureModelProperty DataProperty, DomainModelProperty DomainModelProperty)> Assignments)> valueObjectAssignments
		)
		{
			var foreignKeyReference = domainModelMap.ForeignKeyReferences.FirstOrDefault(fkr => fkr.DomainModelName == valueObjectMap.DomainModelName);
			if (!valueObjectCache.TryGetValue(foreignKeyReference.DomainModelName, out var valueObjectCacheItem))
			{
				var valueObjectPropertiesByMapperProperty = foreignKeyReference.DomainModel.Properties
					.Where(p => !p.DataModelPropertyName.IsNullOrEmpty())
					.ToDictionary(p => p.DataModelPropertyName, p => p);

				var valueObjectProperties = foreignKeyReference.DomainModel.Properties
					.ToDictionary(p => p.Name, p => p);

				valueObjectCacheItem = new ValueObjectCacheItem
				{
					DomainModel = foreignKeyReference,
					PropertyMappings = valueObjectPropertiesByMapperProperty,
					Properties = valueObjectProperties,
				};

				valueObjectCache.Add(foreignKeyReference.DomainModelName, valueObjectCacheItem);
			}

			foreach (var dataModelProperty in valueObjectDataModel.Properties)
			{
				if (!valueObjectCacheItem.PropertyMappings.TryGetValue(dataModelProperty.Name, out var domainModelProperty))
				{
					valueObjectCacheItem.Properties.TryGetValue(dataModelProperty.Name, out domainModelProperty);
				}

				if (domainModelProperty is null)
				{
					continue;
				}

				if (!valueObjectAssignments.TryGetValue(foreignKeyReference.PropertyName, out var assigments))
				{
					assigments = (parentdataModelProperty, []);
					valueObjectAssignments.Add(foreignKeyReference.PropertyName, assigments);
				}

				assigments.Assignments.Add((dataModelProperty, domainModelProperty));
			}
		}

		private static StatementSyntax CreatePropertyAssignment(
			ExpressionSyntax instanceVar,
			string dataModelPropertyPrefix,
			InfrastructureModelProperty dataModelProperty,
			DomainModelProperty domainModelProperty,
			ExpressionSyntax domainModelPropertyAccessor,
			bool withValueMapping
		)
		{
			var instanceVarTemp = dataModelPropertyPrefix.IsNullOrEmpty()
				? instanceVar
				: instanceVar.Access(dataModelPropertyPrefix);


			var dataModelPropertyType = dataModelProperty.Type.Replace("?", "");
			var domainModelPropertyType = domainModelProperty.Type.Replace("?", "");

			var domainModelPropertyValueExpression = dataModelPropertyType != domainModelPropertyType && !domainModelProperty.ProcessAsUnit
				? domainModelPropertyAccessor.Cast(dataModelProperty.Type.ToType())
				: domainModelPropertyAccessor;

			if (!withValueMapping)
			{
				// Check if property is value object
				if (domainModelProperty.ProcessAsUnit)
				{
					return null;
				}

				return instanceVarTemp
					.Access(dataModelProperty.Name)
					.Assign(domainModelPropertyValueExpression)
					.ToExpressionStatement();
			}

			var dataModelPropertyName = dataModelPropertyPrefix.IsNullOrEmpty()
				? dataModelProperty.Name
				: $"{dataModelPropertyPrefix}.{dataModelProperty.Name}";

			var dataModelPropertyMapped = dataModelPropertyPrefix.IsNullOrEmpty()
				? $"{dataModelProperty.Name.ToVariableName()}Mapped"
				: $"{dataModelPropertyPrefix.ToVariableName()}{dataModelProperty.Name}Mapped";

			return instanceVarTemp
				.Access(dataModelProperty.Name)
				.Assign(
					"PropertyValueToDataMappings"
					.Access("TryGetValue")
					.Call(dataModelPropertyName.ToLiteralArgument(), $"out var {dataModelPropertyMapped}".ToArgument())
					.ShortIf(
						dataModelPropertyMapped.ToIdentifierName().Call(domainModelPropertyValueExpression.ToArgument()).Cast(dataModelProperty.Type.ToType()),
						(domainModelProperty.ProcessAsUnit && dataModelPropertyType != domainModelPropertyType)
							? Eshava.CodeAnalysis.SyntaxConstants.Default
							: domainModelPropertyValueExpression
					)
				)
				.ToExpressionStatement();
		}

		private static (string Name, MemberDeclarationSyntax) CreateReadForMethod(
			InfrastructureModel model,
			Dictionary<string, List<InfrastructureModel>> childsForModel,
			ReferenceDomainModelMap domainModelMap,
			ReferenceDomainModel foreignKeyReference,
			List<QueryAnalysisItem> relatedDataModels,
			string fullDomainModelName,
			IEnumerable<InfrastructureModelPropertyCodeSnippet> codeSnippets,
			bool implementSoftDelete
		)
		{
			var readByPropertyName = foreignKeyReference.PropertyName;
			var readByVariableName = foreignKeyReference.PropertyName;
			var returnListName = $"{domainModelMap.DomainModelName.ToVariableName()}Models";

			(var query, var queryParameters) = GetReadByQuery(model, readByPropertyName, readByVariableName, domainModelMap, relatedDataModels, codeSnippets, implementSoftDelete);

			var tryBlockStatements = new List<StatementSyntax>
			{
				query
			};

			var usingInnerStatments = new List<StatementSyntax>
			{
				GetReadByQueryResult(domainModelMap, model, childsForModel, readByVariableName, queryParameters, implementSoftDelete),
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

		private static (string Name, MemberDeclarationSyntax) CreateMapPatchesForValueObjectsMethod(
			InfrastructureModel dataModel,
			ReferenceDomainModelMap domainModelMap,
			Dictionary<string, InfrastructureModel> modelsForDomain
		)
		{
			var valueObjectReferences = domainModelMap.ForeignKeyReferences
				.Where(@ref => @ref.DomainModel.IsValueObject)
				.ToList();

			if (valueObjectReferences.Count == 0)
			{
				return (null, null);
			}

			var fullDomainModelName = $"Domain.{domainModelMap.Domain}.{domainModelMap.DomainModel.NamespaceDirectory}.{domainModelMap.DomainModelName}";
			var loopStatements = new List<StatementSyntax>();
			var dataModelProperties = dataModel.Properties.ToDictionary(p => p.Name, p => p);
			var domainModelProperties = domainModelMap.DomainModel.Properties.ToDictionary(p => p.Name, p => p);

			foreach (var valueObjectReference in valueObjectReferences)
			{
				var valueObjectProperty = domainModelProperties[valueObjectReference.PropertyName];
				if (valueObjectProperty.ProcessAsUnit)
				{
					(var valueObjectDataModel, var valueObjectDataModelProperty) = ValueObjectDataModelForValueObjectModel(valueObjectProperty, dataModelProperties, modelsForDomain);
					if (valueObjectDataModel is null || valueObjectDataModel.UseCustomMapping)
					{
						continue;
					}

					loopStatements.Add(CreatePatchesCodeForValueObjectDataModel(domainModelMap, valueObjectReference, valueObjectDataModel, valueObjectDataModelProperty));

					continue;
				}

				loopStatements.Add(CreatePatchesCodeForValueObject(domainModelMap, valueObjectReference, dataModelProperties));
			}

			var statements = new List<StatementSyntax>
			{
				"patches"
					.ToIdentifierName()
					.ForEach("patch",loopStatements)
			};

			var methodDeclarationName = "MapValueObjects";
			var methodDeclaration = methodDeclarationName.ToMethod(
				Eshava.CodeAnalysis.SyntaxConstants.Void,
				statements,
				SyntaxKind.ProtectedKeyword,
				SyntaxKind.OverrideKeyword
			);

			methodDeclaration = methodDeclaration
				.WithParameter(
					"patches"
					.ToParameter()
					.WithType("IEnumerable".AsGeneric("Patch".AsGeneric(fullDomainModelName))),
					"dataModelChanges"
					.ToParameter()
					.WithType("IDictionary".AsGeneric(Eshava.CodeAnalysis.SyntaxConstants.String, Eshava.CodeAnalysis.SyntaxConstants.Object))
				);

			return (methodDeclarationName, methodDeclaration);
		}

		private static StatementSyntax CreatePatchesCodeForValueObjectDataModel(
			ReferenceDomainModelMap domainModelMap,
			ReferenceDomainModel valueObjectReference,
			InfrastructureModel valueObjectDataModel,
			InfrastructureModelProperty valueObjectDataModelProperty
		)
		{
			var fullValueObjectName = $"Domain.{domainModelMap.Domain}.{domainModelMap.DomainModel.NamespaceDirectory}.{valueObjectReference.DomainModelName}";
			var dataModelProperties = valueObjectDataModel.Properties.ToDictionary(p => p.Name, p => p);
			var valueObjectStatements = new List<StatementSyntax>
			{
				valueObjectReference.PropertyName
					.ToVariableName()
					.ToVariableStatement(
						"patch"
						.Access("Value")
						.AsType(fullValueObjectName.ToType(), false)
					)
			};

			var ifStatements = new List<StatementSyntax>
			{
				"dataModelChanges"
					.Access("Add")
					.Call(
						valueObjectDataModelProperty.Name.ToLiteralString().ToArgument(),
						Eshava.CodeAnalysis.SyntaxConstants.Null.ToArgument()
					)
					.ToExpressionStatement()
			};

			var instanceName = $"data{valueObjectDataModelProperty.Name}";
			var instanceStatement = instanceName
				.ToVariableName()
				.ToVariableStatement(
					valueObjectDataModelProperty.Type
					.ToIdentifierName()
					.ToInstance()
				);

			var instancePatchStatement = "dataModelChanges"
				.Access("Add")
				.Call(
					valueObjectDataModelProperty.Name.ToLiteralString().ToArgument(),
					instanceName.ToIdentifierName().ToArgument()
				)
				.ToExpressionStatement();

			var elseWithMappingStatements = new List<StatementSyntax>
			{
				instanceStatement
			};

			var elseWithoutMappingStatements = new List<StatementSyntax>
			{
				instanceStatement
			};

			foreach (var domainModelProperty in valueObjectReference.DomainModel.Properties)
			{
				var dataModelPropertyName = domainModelProperty.DataModelPropertyName.IsNullOrEmpty()
					? domainModelProperty.Name
					: domainModelProperty.DataModelPropertyName;

				if (!dataModelProperties.TryGetValue(dataModelPropertyName, out var dataModelProperty))
				{
					continue;
				}

				var dataModelPropertyType = dataModelProperty.Type.Replace("?", "");
				var domainModelPropertyType = domainModelProperty.Type.Replace("?", "");
				var domainModelPropertyAccessor = valueObjectReference.PropertyName.ToVariableName().Access(domainModelProperty.Name);
				var domainModelPropertyValueExpression = dataModelPropertyType != domainModelPropertyType
					? domainModelPropertyAccessor.Cast(domainModelProperty.Type.ToType())
					: domainModelPropertyAccessor;

				elseWithMappingStatements.Add(
					instanceName
					.Access(dataModelProperty.Name)
					.Assign(
						"PropertyValueToDataMappings"
							.Access("TryGetValue")
							.Call(dataModelProperty.Name.ToLiteralArgument(), $"out var {domainModelProperty.Name.ToVariableName()}Mapped".ToArgument())
							.ShortIf(
								$"{domainModelProperty.Name.ToVariableName()}Mapped".ToIdentifierName().Call(domainModelPropertyAccessor.ToArgument()).Cast(dataModelProperty.Type.ToType()),
								domainModelPropertyValueExpression
							)
					)
					.ToExpressionStatement()
				);

				elseWithoutMappingStatements.Add(
					instanceName
					.Access(dataModelProperty.Name)
					.Assign(valueObjectReference.PropertyName.ToVariableName().Access(domainModelProperty.Name))
					.ToExpressionStatement()
				);
			}

			elseWithMappingStatements.Add(instancePatchStatement);
			elseWithoutMappingStatements.Add(instancePatchStatement);

			valueObjectStatements.Add(
				valueObjectReference.PropertyName
				.ToVariableName()
				.ToIdentifierName()
				.IsNull()
				.If(ifStatements.ToArray())
				.ElseIf(
					[
						"PropertyValueToDataMappings"
							.Access("Count")
							.ToEquals("0".ToLiteralInt())
							.If(elseWithoutMappingStatements.ToArray())
					],
					elseWithMappingStatements.ToArray()
				)
			);

			valueObjectStatements.Add(
				Eshava.CodeAnalysis.SyntaxConstants.Continue
			);

			return "patch"
				.Access("PropertyName")
				.ToEquals(valueObjectReference.PropertyName.ToLiteralString())
				.If(valueObjectStatements.ToArray());
		}

		private static StatementSyntax CreatePatchesCodeForValueObject(
			ReferenceDomainModelMap domainModelMap,
			ReferenceDomainModel valueObjectReference,
			Dictionary<string, InfrastructureModelProperty> dataModelProperties
		)
		{
			var fullValueObjectName = $"Domain.{domainModelMap.Domain}.{domainModelMap.DomainModel.NamespaceDirectory}.{valueObjectReference.DomainModelName}";
			var valueObjectStatements = new List<StatementSyntax>
			{
				valueObjectReference.PropertyName
				.ToVariableName()
				.ToVariableStatement(
					"patch"
					.Access("Value")
					.AsType(fullValueObjectName.ToType(), false)
				)
			};

			var ifStatements = new List<StatementSyntax>();
			var elseWithMappingStatements = new List<StatementSyntax>();
			var elseWithoutMappingStatements = new List<StatementSyntax>();

			foreach (var domainModelProperty in valueObjectReference.DomainModel.Properties)
			{
				var dataModelPropertyName = domainModelProperty.DataModelPropertyName.IsNullOrEmpty()
					? domainModelProperty.Name
					: domainModelProperty.DataModelPropertyName;

				if (!dataModelProperties.TryGetValue(dataModelPropertyName, out var dataModelProperty))
				{
					continue;
				}

				var defaultArgument = DataTypeConstants.NotNullableTypes.Contains(dataModelProperty.Type)
					? Eshava.CodeAnalysis.SyntaxConstants.Default.Call(dataModelProperty.Type.ToArgument()).ToArgument()
					: Eshava.CodeAnalysis.SyntaxConstants.Null.ToArgument();

				ifStatements.Add(
					"dataModelChanges"
					.Access("Add")
					.Call(
						dataModelPropertyName.ToLiteralString().ToArgument(),
						defaultArgument
					)
					.ToExpressionStatement()
				);

				var dataModelPropertyType = dataModelProperty.Type.Replace("?", "");
				var domainModelPropertyType = domainModelProperty.Type.Replace("?", "");
				var domainModelPropertyAccessor = valueObjectReference.PropertyName.ToVariableName().Access(domainModelProperty.Name);
				var domainModelPropertyValueExpression = dataModelPropertyType != domainModelPropertyType
					? domainModelPropertyAccessor.Cast(domainModelProperty.Type.ToType())
					: domainModelPropertyAccessor;

				elseWithMappingStatements.Add(
					"dataModelChanges"
					.Access("Add")
					.Call(
						dataModelPropertyName.ToLiteralString().ToArgument(),
						"PropertyValueToDataMappings"
							.Access("TryGetValue")
							.Call(dataModelProperty.Name.ToLiteralArgument(), $"out var {domainModelProperty.Name.ToVariableName()}Mapped".ToArgument())
							.ShortIf(
								$"{domainModelProperty.Name.ToVariableName()}Mapped".ToIdentifierName().Call(domainModelPropertyAccessor.ToArgument()).Cast(dataModelProperty.Type.ToType()),
								domainModelPropertyValueExpression
							)
							.ToArgument()
					)
					.ToExpressionStatement()
				);

				elseWithoutMappingStatements.Add(
					"dataModelChanges"
					.Access("Add")
					.Call(
						dataModelProperty.Name.ToLiteralString().ToArgument(),
						valueObjectReference.PropertyName.ToVariableName().Access(domainModelProperty.Name).ToArgument()
					)
					.ToExpressionStatement()
				);
			}

			valueObjectStatements.Add(
				valueObjectReference.PropertyName
				.ToVariableName()
				.ToIdentifierName()
				.IsNull()
				.If(ifStatements.ToArray())
				.ElseIf(
					[
						"PropertyValueToDataMappings"
							.Access("Count")
							.ToEquals("0".ToLiteralInt())
							.If(elseWithoutMappingStatements.ToArray())
					],
					elseWithMappingStatements.ToArray()
				)
			);

			valueObjectStatements.Add(
				Eshava.CodeAnalysis.SyntaxConstants.Continue
			);

			return "patch"
				.Access("PropertyName")
				.ToEquals(valueObjectReference.PropertyName.ToLiteralString())
				.If(valueObjectStatements.ToArray());
		}

		private static (string Name, MemberDeclarationSyntax) CreateGetPropertyNameMethod(
			ReferenceDomainModelMap domainModelMap,
			InfrastructureModel dataModel,
			Dictionary<string, InfrastructureModel> modelsForDomain
		)
		{
			var fullDomainModelName = $"Domain.{domainModelMap.Domain}.{domainModelMap.DomainModel.NamespaceDirectory}.{domainModelMap.DomainModelName}";
			var dataToDomainName = GetMappingName(domainModelMap.DataModelName, domainModelMap.DomainModelName, true);

			var valueObjectReferences = domainModelMap.ForeignKeyReferences
				.Where(@ref => @ref.DomainModel.IsValueObject)
				.ToList();

			var domainModelProperties = domainModelMap.DomainModel.Properties
				.ToDictionary(p => p.Name, p => p);

			var statements = new List<StatementSyntax>();
			var dataModelProperties = dataModel.Properties.ToDictionary(p => p.Name, p => p);

			foreach (var valueObjectReference in valueObjectReferences)
			{
				var domainModelProperty = domainModelProperties[valueObjectReference.PropertyName];
				if (domainModelProperty.ProcessAsUnit)
				{
					(var valueObjectDataModel, var dataModelProperty) = ValueObjectDataModelForValueObjectModel(domainModelProperty, dataModelProperties, modelsForDomain);
					if (valueObjectDataModel is null || valueObjectDataModel.UseCustomMapping)
					{
						continue;
					}

					statements.Add(
						"patch"
						.Access("PropertyName")
						.ToEquals(domainModelProperty.Name.ToLiteralString())
						.If(Eshava.CodeAnalysis.SyntaxConstants.Null.Return())
					);
				}
			}

			statements.Add(
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
				)
			);

			statements.Add(
				"mapping"
				.Access("Domain")
				.IsNotNull()
				.If(
					"mapping"
					.Access("Data")
					.Access("GetMemberExpressionString")
					.Call()
					.Return()
				)
			);

			statements.Add(
				Eshava.CodeAnalysis.SyntaxConstants.Base
				.Access("GetPropertyName")
				.Call("patch".ToArgument())
				.Return()
			);

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

		private static (LocalDeclarationStatementSyntax Query, List<InfrastructureModelPropertyCodeSnippet> QueryParameter) GetReadByQuery(
			InfrastructureModel dataModel,
			string readByPropertyName,
			string readByVariableName,
			ReferenceDomainModelMap domainModelMap,
			List<QueryAnalysisItem> relatedDataModels,
			IEnumerable<InfrastructureModelPropertyCodeSnippet> codeSnippets,
			bool implementSoftDelete
		)
		{

			var interpolatedStringParts = TemplateMethods.CreateSqlQueryWithoutWhereCondition(dataModel, domainModelMap.Domain, relatedDataModels, implementSoftDelete, false);
			var modelItem = relatedDataModels.First(m => m.DataModel.Name == dataModel.Name && m.IsRootModel);

			interpolatedStringParts.Add(@"
				WHERE
					".Interpolate());
			interpolatedStringParts.Add(modelItem.TableAliasConstant.ToIdentifierName().Interpolate());
			interpolatedStringParts.Add(".".Interpolate());
			interpolatedStringParts.Add(Eshava.CodeAnalysis.SyntaxConstants.NameOf.Call(dataModel.Name.Access(readByPropertyName).ToArgument()).Interpolate());
			interpolatedStringParts.Add($@" = @{readByVariableName}".Interpolate());

			if (implementSoftDelete)
			{
				interpolatedStringParts.Add($@"
				AND
					".Interpolate());
				interpolatedStringParts.Add(modelItem.TableAliasConstant.ToIdentifierName().Interpolate());
				interpolatedStringParts.Add(".".Interpolate());
				interpolatedStringParts.Add(Eshava.CodeAnalysis.SyntaxConstants.NameOf.Call(dataModel.Name.Access("Status").ToArgument()).Interpolate());
				interpolatedStringParts.Add(@" = @Status".Interpolate());
			}

			var appliedSnippets = new List<InfrastructureModelPropertyCodeSnippet>();
			foreach (var codeSnippet in codeSnippets)
			{
				foreach (var dataModelProperty in dataModel.Properties)
				{
					var propertySnippet = codeSnippets.FirstOrDefault(cs => cs.CodeSnippeKey == $"{dataModel.Name}.{dataModelProperty.Name}" && cs.IsFilter)
							?? codeSnippets.FirstOrDefault(cs => cs.CodeSnippeKey == dataModelProperty.Name && cs.IsFilter);

					if (propertySnippet is null)
					{
						continue;
					}

					interpolatedStringParts.Add($@"
				AND
					".Interpolate());
					interpolatedStringParts.Add(modelItem.TableAliasConstant.ToIdentifierName().Interpolate());
					interpolatedStringParts.Add(".".Interpolate());
					interpolatedStringParts.Add(Eshava.CodeAnalysis.SyntaxConstants.NameOf.Call(dataModel.Name.Access(propertySnippet.PropertyName).ToArgument()).Interpolate());
					interpolatedStringParts.Add($@" = @{propertySnippet.PropertyName}".Interpolate());

					appliedSnippets.Add(propertySnippet);
				}
			}

			interpolatedStringParts.Add($@"
					".Interpolate());

			return ("query".ToVariableStatement(interpolatedStringParts.ToRawStringExpression()), appliedSnippets);
		}

		private static StatementSyntax GetReadByQueryResult(
			ReferenceDomainModelMap domainModelMap,
			InfrastructureModel model,
			Dictionary<string, List<InfrastructureModel>> childsForModel,
			string readByVariableName,
			List<InfrastructureModelPropertyCodeSnippet> addtionalQueryParameters,
			bool implementSoftDelete
		)
		{
			var parameterItems = new List<(ExpressionSyntax Property, string Name)>
			{
				(readByVariableName.ToVariableName().ToIdentifierName(), readByVariableName)
			};

			if (implementSoftDelete)
			{
				parameterItems.Add(("Status".Access("Active"), "Status"));
			}

			foreach (var addtionalQueryParameter in addtionalQueryParameters)
			{
				parameterItems.Add((addtionalQueryParameter.Expression, addtionalQueryParameter.PropertyName));
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

			var generateDomainPatchListArguments = new List<ArgumentSyntax>
			{
				modelItemName.ToArgument(),
				dataToDomainName.ToArgument(),
				$"{domainModelMap.DomainModelName.ToFieldName()}PropertyValueToDomainMappings".ToArgument()
			};

			if (domainModelMap.ForeignKeyReferences.Any(@ref => @ref.DomainModel.IsValueObject))
			{
				generateDomainPatchListArguments.Add($"CreateValueObjectsFor{domainModelMap.DomainModelName}".ToArgument());
				generateDomainPatchListArguments.Add(ApplicationNames.Engines.VALIDATIONENGINE.ToFieldName().ToArgument());
			}

			loopStatements.Add(patchesVariableName
				.ToVariableStatement(
					"GenerateDomainPatchList".AsGeneric(modelType, fullDomainModelName)
					.Call(generateDomainPatchListArguments.ToArray())
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

			var fieldDeclarations = new List<(string FieldName, FieldDeclarationSyntax Declaration)>();
			var fullDomainModelName = $"Domain.{domainModelMap.Domain}.{domainModelMap.DomainModel.NamespaceDirectory}.{domainModelMap.DomainModelName}";

			// Property name mappings
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

			fieldDeclarations.Add((dataToDomainName, dataToDomainName.ToStaticReadonlyField(dataToDomainType, dataToDomainInstance)));

			// Property value mappings
			var propertyValueName = $"{domainModelMap.DomainModelName.ToFieldName()}PropertyValueToDomainMappings";
			var propertyValueType = "Dictionary".AsGeneric(Eshava.CodeAnalysis.SyntaxConstants.String, "Func".AsGeneric("object", "object"));
			var propertyValueInstance = propertyValueType.ToCollectionExpression();

			fieldDeclarations.Add((propertyValueName, propertyValueName.ToStaticReadonlyField(propertyValueType, propertyValueInstance)));

			foreach (var childDomainModel in domainModelMap.ChildDomainModels)
			{
				fieldDeclarations.AddRange(CollectDataToDomainPropertyMappings(childDomainModel));
			}

			return fieldDeclarations;
		}

		private class ValueObjectCacheItem
		{
			public ReferenceDomainModel DomainModel { get; set; }
			public Dictionary<string, DomainModelProperty> PropertyMappings { get; set; }
			public Dictionary<string, DomainModelProperty> Properties { get; set; }
		}
	}
}