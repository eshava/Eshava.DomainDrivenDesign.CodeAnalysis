using System;
using System.Collections.Generic;
using System.Linq;
using Eshava.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Constants;
using Eshava.DomainDrivenDesign.CodeAnalysis.Enums;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Domain;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Templates.Domain
{
	public static class DomainModelTemplate
	{
		public static string GetDomainModel(ReferenceDomainModelMap domainModelMap, DomainProject project, ReferenceMap domainModelReferenceMap)
		{
			var @namespaceDomain = $"{project.FullQualifiedNamespace}.{domainModelMap.Domain}";
			var @namespace = $"{@namespaceDomain}.{domainModelMap.DomainModel.NamespaceDirectory}";
			(var baseClass, var standard) = GetBaseClass(domainModelMap, project);
			var baseType = baseClass.AsGeneric(domainModelMap.DomainModelName, domainModelMap.IdentifierType).ToSimpleBaseType();

			var unitInformation = new UnitInformation(domainModelMap.DomainModelName, @namespace, addAssemblyComment: project.AddAssemblyCommentToFiles);

			unitInformation.AddClassModifier(SyntaxKind.PublicKeyword, SyntaxKind.PartialKeyword);
			unitInformation.AddContructorModifier(SyntaxKind.PrivateKeyword);
			unitInformation.AddBaseType(baseType);

			unitInformation.AddUsing(CommonNames.Namespaces.SYSTEM);
			unitInformation.AddUsing(CommonNames.Namespaces.LINQ);
			unitInformation.AddUsing(CommonNames.Namespaces.EXPRESSION);
			unitInformation.AddUsing(CommonNames.Namespaces.GENERIC);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.EXTENSIONS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.MODELS);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.VALIDATION.INTERFACES);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Domain.EXTENSIONS);
			unitInformation.AddUsing(project.AlternativeUsing);

			unitInformation.AddValidationEngine(true);

			if (standard)
			{
				unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Domain.MODELS);
			}

			AddEventDomainProperty(unitInformation, domainModelMap.Domain);

			foreach (var property in domainModelMap.DomainModel.Properties)
			{
				unitInformation.AddUsing(property.UsingForType);

				if (property.Attributes?.Any() ?? false)
				{
					foreach (var attribute in property.Attributes)
					{
						unitInformation.AddUsing(attribute.UsingForType);
					}
				}

				var attributes = AttributeTemplate.CreateAttributes(property.Attributes);
				unitInformation.AddProperty(property.Name.ToProperty(property.Type.ToType(), SyntaxKind.PublicKeyword, true, true, modifierSetter: SyntaxKind.PrivateKeyword, attributes: attributes), property.Name);
			}

			if (domainModelMap.IsAggregate && (domainModelMap.ChildDomainModels?.Count ?? 0) > 0)
			{
				foreach (var childDomainModel in domainModelMap.ChildDomainModels)
				{
					if (!childDomainModel.DomainModel.IsNamespaceDirectoryUncountable)
					{
						unitInformation.AddUsing($"{@namespaceDomain}.{childDomainModel.DomainModel.NamespaceDirectory}");
					}

					unitInformation.AddField(CreateChildListField(childDomainModel));
					unitInformation.AddField(CreateChildChangedField(childDomainModel));

					var propertyName = childDomainModel.ChildEnumerableName.ToPlural();
					var fieldName = propertyName.ToFieldName();

					var propertyDeclaration = propertyName.ToProperty("IReadOnlyList".AsGeneric(childDomainModel.GetDomainModelTypeName(null)), SyntaxKind.PublicKeyword, false, false)
						.WithExpressionBody(fieldName.Call("AsReadOnly"));

					unitInformation.AddProperty(propertyDeclaration, propertyName);
				}
			}

			if (domainModelMap.IsChildDomainModel)
			{
				unitInformation.AddField(DomainNames.CALLBACK.ToFieldName(), DomainNames.CALLBACK.ToFieldName().ToField(GetCallbackType(domainModelMap.DomainModelName), Eshava.CodeAnalysis.SyntaxConstants.Null));
			}

			unitInformation.AddMethod(CreateDataToInstanceMethod(domainModelMap));
			unitInformation.AddMethod(CreateEntityFromDtoMethod(domainModelMap));
			unitInformation.AddMethod(CreateEntityFromPropertiesMethod(domainModelMap));

			if (domainModelMap.DomainModel.AddGeneralPatchMethod)
			{
				unitInformation.AddMethod(CreateGeneralPatchMethod(domainModelMap));
			}

			// aggregate root messages
			if (domainModelMap.IsAggregate)
			{
				var deactivateMethodDeclaration = CreateDeactivateMethod(domainModelMap.ChildDomainModels);
				if (deactivateMethodDeclaration.Method is not null)
				{
					unitInformation.AddMethod(deactivateMethodDeclaration);
				}

				var childMethods = CreateGetAndAddChildsMethods(domainModelMap.ChildDomainModels);
				foreach (var childMethod in childMethods)
				{
					unitInformation.AddMethod(childMethod);
				}

				unitInformation.AddMethod(CreateAreAllChildsValidMethod(domainModelMap.ChildDomainModels));
				unitInformation.AddMethod(CreateHasChangesInChildsMethod(domainModelMap.ChildDomainModels));
				unitInformation.AddMethod(CreateSetChildMethod(domainModelMap.ChildDomainModels));
				unitInformation.AddMethod(CreateClearChildChangesMethod(domainModelMap.ChildDomainModels));
				unitInformation.AddMethod(CreateClearChildEventsMethod(domainModelMap.ChildDomainModels));
				unitInformation.AddMethod(CreateGetChildDomainEventsMethod(domainModelMap.ChildDomainModels));

				var childDelegateMethods = CreateChildDelegateAndAggregateInitMethods(domainModelMap, domainModelMap.ChildDomainModels);
				foreach (var childMethod in childDelegateMethods)
				{
					unitInformation.AddMethod(childMethod);
				}
			}

			if (domainModelMap.IsChildDomainModel)
			{
				unitInformation.AddMethod(CreateSetActionCallbackMethod(domainModelMap));
			}

			return unitInformation.CreateCodeString();
		}

		private static (string Class, bool Standard) GetBaseClass(ReferenceDomainModelMap domainModelMap, DomainProject project)
		{
			if (domainModelMap.IsAggregate)
			{
				if (project.AlternativeAbstractAggregate.IsNullOrEmpty())
				{
					return (DomainNames.ABSTRACTAGGREGATE, true);
				}

				return (project.AlternativeAbstractAggregate, false);
			}

			if (domainModelMap.IsChildDomainModel)
			{
				if (project.AlternativeAbstractChildDomainModel.IsNullOrEmpty())
				{
					return (DomainNames.ABSTRACTCHILDDOMAINMODEL, true);
				}

				return (project.AlternativeAbstractChildDomainModel, false);
			}

			if (project.AlternativeAbstractDomainModel.IsNullOrEmpty())
			{
				return (DomainNames.ABSTRACTDOMAINMODEL, true);
			}

			return (project.AlternativeAbstractDomainModel, false);
		}

		private static (string FieldName, FieldType Type, FieldDeclarationSyntax Declaration) CreateChildListField(ReferenceDomainModelMap domainModelMap)
		{
			var type = "IList".AsGeneric(domainModelMap.GetDomainModelTypeName(null));
			var fieldName = domainModelMap.ChildEnumerableName.ToFieldName().ToPlural();

			return (fieldName, FieldType.Others, fieldName.ToField(type, Eshava.CodeAnalysis.SyntaxConstants.Default));
		}

		private static (string FieldName, FieldType Type, FieldDeclarationSyntax Declaration) CreateChildChangedField(ReferenceDomainModelMap domainModelMap)
		{
			var func = GetCallbackType(domainModelMap.GetDomainModelTypeName(null));
			var fieldName = GetCallbackFieldName(domainModelMap);
			var responseDataExpression = StatementHelpers.GetResponseData(true);

			return (fieldName, FieldType.Others, fieldName.ToField(func, "p".ToParameterExpression().WithExpressionBody(responseDataExpression)));
		}

		private static string GetCallbackFieldName(AbstractReferenceDomainModel domainModel)
		{
			return $"{domainModel.ChildEnumerableName.ToFieldName()}Changed";
		}

		private static (string Name, MemberDeclarationSyntax Method) CreateSetChildMethod(IEnumerable<ReferenceDomainModelMap> childDomainModels)
		{
			var setChildsDeclarationName = "SetChilds";
			var setChildsDeclaration = setChildsDeclarationName.ToMethod(Eshava.CodeAnalysis.SyntaxConstants.Void, null, SyntaxKind.PrivateKeyword);

			var parameterList = new List<ParameterSyntax>();
			var syntaxStatements = new List<StatementSyntax>();

			foreach (var childDomainModel in childDomainModels)
			{
				var childType = childDomainModel.GetDomainModelTypeName(null);
				var fieldName = childDomainModel.ChildEnumerableName.ToFieldName().ToPlural();
				var parameterName = childDomainModel.ChildEnumerableName.ToVariableName().ToPlural();

				var parameterDeclaration = parameterName.ToParameter()
					.WithType("IEnumerable".AsGeneric(childType));

				parameterList.Add(parameterDeclaration);

				var listInstance = "List"
					.AsGeneric(childType)
					.ToInstance();

				var toListCall = parameterName
					.ToIdentifierName()
					.Access("ToList".ToIdentifierName(), true)
					.Call()
					.AddNullFallback(listInstance);


				syntaxStatements.Add(fieldName.ToIdentifierName().Assign(toListCall).ToExpressionStatement());
			}

			return (
				setChildsDeclarationName,
				setChildsDeclaration
				.WithParameter(parameterList.ToArray())
				.AddBodyStatements(syntaxStatements.ToArray())
			);
		}

		private static IEnumerable<(string Name, MemberDeclarationSyntax Method)> CreateGetAndAddChildsMethods(IEnumerable<ReferenceDomainModelMap> childDomainModels)
		{
			var methodDeclarations = new List<(string Name, MemberDeclarationSyntax Method)>();

			if (childDomainModels?.Any() ?? false)
			{
				foreach (var childDomainModel in childDomainModels)
				{
					var childType = childDomainModel.GetDomainModelTypeName(null);

					// Get child method
					var methodName = "Get" + childDomainModel.ChildEnumerableName;
					var returnValue = CommonNames.RESPONSEDATA.AsGeneric(childType);
					var statements = new List<StatementSyntax>();
					var fieldName = childDomainModel.ChildEnumerableName.ToFieldName().ToPlural();
					var parameterName = childDomainModel.ClassificationKey.ToVariableName() + "Id";

					statements.Add(
						"GetChild"
						.ToIdentifierName()
						.Call(fieldName.ToArgument(), parameterName.ToArgument())
						.Return()
					);

					methodDeclarations.Add((
						methodName,
						methodName
						.ToMethod(returnValue, statements, SyntaxKind.PublicKeyword)
						.WithParameter(
							parameterName
							.ToParameter()
							.WithType(childDomainModel.IdentifierType.ToType())
						)
					));

					// Add child method
					var addChildDeclarationName = "Add" + childDomainModel.ChildEnumerableName;
					var addChildDeclaration = addChildDeclarationName.ToMethod(
						CommonNames.RESPONSEDATA.AsGeneric(childDomainModel.DomainModelName),
						null,
						SyntaxKind.PublicKeyword
					);

					var childActionCallback = GetCallbackFieldName(childDomainModel);

					var parameterList = new List<ParameterSyntax>();
					statements = new List<StatementSyntax>();

					var dtoDeclaration = "dto".ToParameter()
						.WithType("TDto".ToType());

					parameterList.Add(dtoDeclaration);
					parameterList.Add(CreateDtoMappingTuple(childType));

					var createResult = "createResult"
						.ToVariableStatement(
							childType.Call(
								"CreateEntity",
								false,
								"dto".ToArgument(),
								DomainNames.VALIDATION.Parameter.Name.ToPropertyName().ToArgument(),
								"mappings".ToArgument()
							)
						);

					statements.Add(createResult);

					statements.Add(
						"createResult"
						.ToIdentifierName()
						.Access("IsFaulty")
						.If("createResult".ToIdentifierName().Return())
					);

					statements.Add(
						fieldName
						.Call(
							"Add",
							false,
							"createResult".ToIdentifierName().Access("Data").ToArgument()
						)
						.ToExpressionStatement()
					);

					statements.Add(
						childActionCallback
						.ToIdentifierName()
						.IsNull()
						.If(
							"createResult"
							.ToIdentifierName()
							.Return()
						)
					);

					statements.Add(
						"createResult"
						.ToIdentifierName()
						.Access("Data")
						.Access("SetActionCallback")
						.Call(childActionCallback.ToArgument())
						.ToExpressionStatement()
					);

					StatementHelpers.AddLocalMethodCallAndFaultyCheck(
						statements,
						childActionCallback,
						"actionCallbackResult",
						childDomainModel.DomainModelName,
						"createResult"
							.ToIdentifierName()
							.Access("Data")
					);

					statements.Add(
						"createResult"
						.ToIdentifierName()
						.Return()
					);

					addChildDeclaration = addChildDeclaration
						.WithTypeParameter("TDto".ToTypeParameter())
						.WithConstraints(("TDto", Eshava.CodeAnalysis.SyntaxConstants.ClassConstraint.AsArray()))
						.WithParameter(parameterList.ToArray())
						.AddBodyStatements(statements.ToArray());
					;

					methodDeclarations.Add((addChildDeclarationName, addChildDeclaration));
				}
			}

			return methodDeclarations;
		}

		private static IEnumerable<(string Name, MemberDeclarationSyntax Method)> CreateChildDelegateAndAggregateInitMethods(ReferenceDomainModelMap aggregate, IEnumerable<ReferenceDomainModelMap> childDomainModels)
		{
			var methodDeclarations = new List<(string Name, MemberDeclarationSyntax Method)>();
			var initStatements = new List<StatementSyntax>();

			if (childDomainModels?.Any() ?? false)
			{
				foreach (var childDomainModel in childDomainModels)
				{
					var childType = childDomainModel.GetDomainModelTypeName(null);
					var methodName = $"CreatedOrChanged{childDomainModel.DomainModelName}";
					var returnValue = CommonNames.RESPONSEDATA.AsGeneric(Eshava.CodeAnalysis.SyntaxConstants.Bool);
					var statements = new List<StatementSyntax>();
					var parameterName = childDomainModel.ClassificationKey.ToVariableName();

					initStatements.Add(
						GetCallbackFieldName(childDomainModel)
						.ToIdentifierName()
						.Assign(methodName.ToIdentifierName())
						.ToExpressionStatement()
					);

					if (aggregate.IsChildDomainModel)
					{
						StatementHelpers.AddLocalMethodCallAndFaultyCheck(statements, "Validate", "validationResult", (TypeSyntax)null);
						statements.Add(
							DomainNames.CALLBACKFIELD
							.ToIdentifierName()
							.IsNull()
							.ShortIf(
								"validationResult".ToIdentifierName(),
								StatementHelpers.GetMethodCall(null, DomainNames.CALLBACKFIELD, Eshava.CodeAnalysis.SyntaxConstants.This)
							)
							.Return()
						);
					}
					else
					{
						statements.Add(
							"Validate"
							.ToIdentifierName()
							.Call()
							.Return()
						);
					}

					methodDeclarations.Add((
						methodName,
						methodName.ToMethod(
							returnValue,
							statements,
							SyntaxKind.ProtectedKeyword,
							SyntaxKind.VirtualKeyword
						)
						.WithParameter(
							parameterName.ToParameter().WithType(childType.ToType())
						)
					));
				}

				methodDeclarations.Add((
					"Init",
					"Init".ToMethod(
						Eshava.CodeAnalysis.SyntaxConstants.Void,
						initStatements,
						SyntaxKind.ProtectedKeyword,
						SyntaxKind.OverrideKeyword
					)
				));
			}

			return methodDeclarations;
		}

		private static (string Name, MemberDeclarationSyntax Method) CreateAreAllChildsValidMethod(IEnumerable<ReferenceDomainModelMap> childDomainModels)
		{
			return CreateChildCheckMethod(childDomainModels, "AreAllChildsValid", "All", "IsValid", (left, right) => left.And(right));
		}

		private static (string Name, MemberDeclarationSyntax Method) CreateHasChangesInChildsMethod(IEnumerable<ReferenceDomainModelMap> childDomainModels)
		{
			return CreateChildCheckMethod(childDomainModels, "HasChangesInChilds", "Any", "IsChanged", (left, right) => left.Or(right));
		}

		private static (string Name, MemberDeclarationSyntax Method) CreateChildCheckMethod(IEnumerable<ReferenceDomainModelMap> childDomainModels, string methodName, string methodCallName, string propertyName, Func<ExpressionSyntax, ExpressionSyntax, BinaryExpressionSyntax> link)
		{
			var statements = new List<StatementSyntax>();
			if (childDomainModels?.Any() ?? false)
			{
				var expressionSyntax = default(ExpressionSyntax);
				foreach (var childDomainModel in childDomainModels)
				{
					var fieldName = childDomainModel.ChildEnumerableName.ToFieldName().ToPlural();
					var argumentExpression = "p".ToPropertyExpression(propertyName);
					var methodCall = fieldName.Call(methodCallName, false, argumentExpression.ToArgument());

					if (expressionSyntax is null)
					{
						expressionSyntax = methodCall;
					}
					else
					{
						expressionSyntax = link(expressionSyntax, methodCall);
					}
				}

				statements.Add(expressionSyntax.Return());
			}
			else
			{
				statements.Add(Eshava.CodeAnalysis.SyntaxConstants.True.Return());
			}

			return (
				methodName,
				methodName.ToMethod(
					Eshava.CodeAnalysis.SyntaxConstants.Bool,
					statements,
					SyntaxKind.ProtectedKeyword,
					SyntaxKind.OverrideKeyword
				)
			);
		}

		private static (string Name, MemberDeclarationSyntax Method) CreateClearChildChangesMethod(IEnumerable<ReferenceDomainModelMap> childDomainModels)
		{
			return CreateChildClearMethod(childDomainModels, "ClearChildChanges");
		}

		private static (string Name, MemberDeclarationSyntax Method) CreateClearChildEventsMethod(IEnumerable<ReferenceDomainModelMap> childDomainModels)
		{
			return CreateChildClearMethod(childDomainModels, "ClearChildEvents");
		}

		private static (string Name, MemberDeclarationSyntax Method) CreateChildClearMethod(IEnumerable<ReferenceDomainModelMap> childDomainModels, string methodName)
		{
			var statements = new List<StatementSyntax>();
			if (childDomainModels?.Any() ?? false)
			{
				foreach (var childDomainModel in childDomainModels)
				{
					var fieldName = childDomainModel.ChildEnumerableName.ToFieldName().ToPlural();
					statements.Add(
						methodName
							.AsGeneric(childDomainModel.DomainModelName, childDomainModel.DomainModel.IdentifierType)
							.Call(fieldName.ToArgument())
							.ToExpressionStatement()
					);
				}
			}

			return (
				methodName,
				methodName.ToMethod(
					Eshava.CodeAnalysis.SyntaxConstants.Void,
					statements,
					SyntaxKind.ProtectedKeyword,
					SyntaxKind.OverrideKeyword
				)
			);
		}

		private static (string Name, MemberDeclarationSyntax Method) CreateGetChildDomainEventsMethod(IEnumerable<ReferenceDomainModelMap> childDomainModels)
		{
			var methodName = "GetChildDomainEvents";
			var domainEventListName = "domainEvents";
			var resultName = "domainEventsResult";

			var statements = new List<StatementSyntax>
			{
				domainEventListName.ToVariableStatement("List".AsGeneric("DomainEvent").ToInstance())
			};

			if (childDomainModels?.Any() ?? false)
			{
				statements.Add(
					resultName
						.ToVariableStatement(
							"default"
							.ToIdentifierName()
							.Call("IEnumerable".AsGeneric("DomainEvent").ToArgument())
						)
				);

				foreach (var childDomainModel in childDomainModels)
				{
					var fieldName = childDomainModel.ChildEnumerableName.ToFieldName().ToPlural();
					var methodCall = methodName
							.AsGeneric(childDomainModel.DomainModelName, childDomainModel.DomainModel.IdentifierType)
							.Call(fieldName.ToArgument());

					statements.Add(
						resultName
							.ToIdentifierName()
							.Assign(methodCall)
							.ToExpressionStatement()
					);

					statements.Add(
						resultName
							.ToIdentifierName()
							.Access("Any")
							.Call()
							.If(
								domainEventListName
									.ToIdentifierName()
									.Access("AddRange")
									.Call(resultName.ToIdentifierName().ToArgument())
									.ToExpressionStatement()
							)
					);
				}
			}

			statements.Add(domainEventListName.ToIdentifierName().Return());

			return (
				methodName,
				methodName.ToMethod(
					"IEnumerable".AsGeneric("DomainEvent"),
					statements,
					SyntaxKind.ProtectedKeyword,
					SyntaxKind.OverrideKeyword
				)
			);
		}

		private static (string Name, MemberDeclarationSyntax Method) CreateSetActionCallbackMethod(ReferenceDomainModelMap childDomainModel)
		{
			var statements = new List<StatementSyntax>
			{
				DomainNames.CALLBACK
					.ToFieldName()
					.ToIdentifierName()
					.Assign(DomainNames.CALLBACK.ToIdentifierName())
					.ToExpressionStatement()
			};

			var methodDeclarationName = "SetActionCallback";
			var methodDeclaration = methodDeclarationName.ToMethod(
				Eshava.CodeAnalysis.SyntaxConstants.Void,
				statements,
				SyntaxKind.InternalKeyword
			);

			methodDeclaration = methodDeclaration
				.WithParameter(
					DomainNames.CALLBACK
					.ToParameter()
					.WithType(GetCallbackType(childDomainModel.DomainModelName))
				);

			return (methodDeclarationName, methodDeclaration);
		}

		private static (string Name, MemberDeclarationSyntax Method) CreateDeactivateMethod(IEnumerable<ReferenceDomainModelMap> childDomainModels)
		{
			if (!(childDomainModels?.Any() ?? false))
			{
				return (null, null);
			}

			var statements = new List<StatementSyntax>();

			foreach (var childDomainModel in childDomainModels)
			{
				var fieldName = childDomainModel.ChildEnumerableName.ToFieldName().ToPlural();
				StatementHelpers.AddLocalMethodCallAndFaultyCheck(
					statements,
					"DeactivateChilds",
					[childDomainModel.GetDomainModelTypeName(null), childDomainModel.IdentifierType],
					childDomainModel.ChildEnumerableName.ToVariableName() + "Result",
					(TypeSyntax)null,
					fieldName.ToIdentifierName()
				);
			}

			statements.Add(
				Eshava.CodeAnalysis.SyntaxConstants.Base
				.Call("Deactivate".ToIdentifierName(), false)
				.Return()
			);

			var methodName = "Deactivate";

			return (methodName, methodName.ToMethod(
				Constants.SyntaxConstants.ResponseDataBool,
				statements,
				SyntaxKind.PublicKeyword,
				SyntaxKind.OverrideKeyword
			));
		}

		private static ChildInitializerContainer CollectChildsForInitialization(ReferenceDomainModelMap domainModelMap)
		{
			var container = new ChildInitializerContainer();

			if (domainModelMap.IsAggregate && (domainModelMap.ChildDomainModels?.Any() ?? false))
			{
				foreach (var childDomainModel in domainModelMap.ChildDomainModels)
				{
					var parameterName = childDomainModel.DomainModelName.ToVariableName() + "List";
					var variableName = childDomainModel.DomainModelName.ToVariableName();

					var parameterDeclaration = parameterName
					.ToParameter()
					.WithType(
						"IEnumerable"
						.AsGeneric(
							childDomainModel.GetDomainModelTypeName(null)
						)
					);

					container.Parameters.Add(parameterDeclaration);

					var childLoopStatements = new List<StatementSyntax>
					{
						variableName
							.ToIdentifierName()
							.Access("SetActionCallback")
							.Call(
								"instance"
								.ToIdentifierName()
								.Access(GetCallbackFieldName(childDomainModel))
								.ToArgument()
							)
							.ToExpressionStatement()
					};

					container.Statements.Add(
						parameterName
						.ToIdentifierName()
						.ForEach(variableName, childLoopStatements)
					);

					container.ChildArguments.Add(
						parameterName
						.ToIdentifierName()
					);
				}
			}

			return container;
		}

		private static (string Name, MemberDeclarationSyntax Method) CreateDataToInstanceMethod(ReferenceDomainModelMap domainModelMap)
		{
			var dataToInstanceDeclarationName = "DataToInstance";
			var dataToInstanceDeclaration = dataToInstanceDeclarationName.ToMethod(
				domainModelMap.DomainModelName.ToType(),
				null,
				SyntaxKind.PublicKeyword,
				SyntaxKind.StaticKeyword
			);

			var statements = new List<StatementSyntax>();
			var parameterList = new List<ParameterSyntax>();
			var patchesDeclaration = "patches".ToParameter()
					.WithType("IEnumerable".AsGeneric("Patch".AsGeneric(domainModelMap.DomainModelName)));

			parameterList.Add(patchesDeclaration);

			var domainModelInstance = CreateDomainModelInstanceSyntax(domainModelMap);

			var instance = "instance".ToVariableStatement(domainModelInstance);
			statements.Add(instance);

			var childContainer = CollectChildsForInitialization(domainModelMap);
			parameterList.AddRange(childContainer.Parameters);
			statements.AddRange(childContainer.Statements);

			parameterList.Add(
				DomainNames.VALIDATION.Parameter.Name
				.ToParameter()
				.WithType(DomainNames.VALIDATION.Parameter.Type)
			);

			var instanceStatment = "instance".ToIdentifierName();

			if (childContainer.ChildArguments.Count > 0)
			{
				StatementHelpers.AddMethodCall(statements, instanceStatment, "SetChilds", null, null, childContainer.ChildArguments.ToArray());
			}

			StatementHelpers.AddMethodCall(statements, instanceStatment, "ApplyPatches", null, null, "patches".ToIdentifierName().Access("ToList").Call());
			StatementHelpers.AddMethodCall(statements, instanceStatment, "ClearChanges", null, null);

			statements.Add(instanceStatment.Return());

			dataToInstanceDeclaration = dataToInstanceDeclaration
				.WithParameter(parameterList.ToArray())
				.AddBodyStatements(statements.ToArray());

			return (dataToInstanceDeclarationName, dataToInstanceDeclaration);
		}

		private static (string Name, MemberDeclarationSyntax Method) CreateEntityFromDtoMethod(ReferenceDomainModelMap domainModelMap)
		{
			var accessModified = !domainModelMap.IsChildDomainModel
				? SyntaxKind.PublicKeyword
				: SyntaxKind.InternalKeyword;

			var dataToInstanceDeclarationName = "CreateEntity";
			var dataToInstanceDeclaration = dataToInstanceDeclarationName.ToMethod(
				CommonNames.RESPONSEDATA.AsGeneric(domainModelMap.DomainModelName),
				null,
				accessModified,
				SyntaxKind.StaticKeyword
			);

			var parameterList = new List<ParameterSyntax>();
			var statements = new List<StatementSyntax>();

			var dtoDeclaration = "dto".ToParameter()
				.WithType("TDto".ToType());

			parameterList.Add(dtoDeclaration);

			var validationEngineDeclaration = DomainNames.VALIDATION.Parameter.Name.ToParameter()
				.WithType(DomainNames.VALIDATION.Parameter.Type);

			parameterList.Add(validationEngineDeclaration);
			parameterList.Add(CreateDtoMappingTuple(domainModelMap.DomainModelName));

			StatementHelpers.AddMethodCall(statements, "dto".ToIdentifierName(), "ToPatches", null, "patches", "mappings".ToIdentifierName());

			var domainModelInstance = CreateDomainModelInstanceSyntax(domainModelMap);

			statements.Add(
				"instance"
				.ToVariableStatement(domainModelInstance)
			);

			var childContainer = CollectChildsForInitialization(domainModelMap);
			if (childContainer.ChildArguments.Count > 0)
			{
				StatementHelpers.AddMethodCall(statements, "instance".ToIdentifierName(), "SetChilds", null, null, childContainer.ChildArguments.Select(a => Eshava.CodeAnalysis.SyntaxConstants.Null).ToArray());
			}

			StatementHelpers.AddMethodCallAndFaultyCheck(statements, "instance".ToIdentifierName(), "Create", null, "createResult", domainModelMap.DomainModelName.ToType(), "patches".ToIdentifierName());

			statements.Add(
				"instance"
				.ToIdentifierName()
				.Access(CommonNames.Extensions.TORESPONSEDATA)
				.Call()
				.Return()
			);

			return (
				dataToInstanceDeclarationName,
				dataToInstanceDeclaration
				.WithTypeParameter("TDto".ToTypeParameter())
				.WithConstraints(("TDto", Eshava.CodeAnalysis.SyntaxConstants.ClassConstraint.AsArray()))
				.WithParameter(parameterList.ToArray())
				.AddBodyStatements(statements.ToArray())
			);
		}

		private static ParameterSyntax CreateDtoMappingTuple(string typeName)
		{
			var mappingDto = "Expression"
				.AsGeneric("Func".AsGeneric("TDto".ToIdentifierName(), Eshava.CodeAnalysis.SyntaxConstants.Object))
				.ToTupleElement()
				.WithIdentifier("Dto".ToIdentifier());

			var mappingDomain = "Expression"
				.AsGeneric("Func".AsGeneric(typeName.ToIdentifierName(), Eshava.CodeAnalysis.SyntaxConstants.Object))
				.ToTupleElement()
				.WithIdentifier("Domain".ToIdentifier());

			return "mappings"
				.ToParameter()
				.WithType(
					"IEnumerable"
					.AsGeneric(mappingDto.ToTupleType(mappingDomain))
				)
				.WithDefault(Eshava.CodeAnalysis.SyntaxConstants.Null.ToEqualsValueClause());
		}

		private static (string Name, MemberDeclarationSyntax Method) CreateEntityFromPropertiesMethod(ReferenceDomainModelMap domainModelMap)
		{
			var accessModified = !domainModelMap.IsChildDomainModel
				? SyntaxKind.PublicKeyword
				: SyntaxKind.InternalKeyword;

			var dataToInstanceDeclarationName = "CreateEntity";
			var dataToInstanceDeclaration = dataToInstanceDeclarationName.ToMethod(
				CommonNames.RESPONSEDATA.AsGeneric(domainModelMap.DomainModelName),
				null,
				accessModified,
				SyntaxKind.StaticKeyword
			);

			var parameterList = new List<ParameterSyntax>();
			var statements = new List<StatementSyntax>();
			var patchStatements = new List<InvocationExpressionSyntax>();

			foreach (var property in domainModelMap.DomainModel.Properties)
			{
				if (property.SkipForConstructor)
				{
					continue;
				}

				var variableName = property.Name.ToVariableName();
				var propertyParameterDeclaration = variableName.ToParameter()
					.WithType(property.Type.ToType());

				parameterList.Add(propertyParameterDeclaration);

				var propertyExpression = "p".ToPropertyExpression(property.Name).ToArgument();
				var variableArgument = variableName.ToIdentifierName().ToArgument();

				patchStatements.Add(
					"Patch"
					.AsGeneric(domainModelMap.DomainModelName)
					.Access("Create")
					.Call(propertyExpression, variableArgument)
				);
			}


			parameterList.Add(
				DomainNames.VALIDATION.Parameter.Name
				.ToParameter()
				.WithType(DomainNames.VALIDATION.Parameter.Type)
			);

			statements.Add(
				"patches"
				.ToVariableStatement(
					"List"
					.AsGeneric(
						"Patch"
						.AsGeneric(domainModelMap.DomainModelName)
					)
					.ToInstance()
					.WithInitializer(patchStatements.ToArray())
				)
			);

			statements.Add(
				"instance"
				.ToVariableStatement(
					CreateDomainModelInstanceSyntax(domainModelMap)
				)
			);

			var childContainer = CollectChildsForInitialization(domainModelMap);
			if (childContainer.ChildArguments.Count > 0)
			{
				StatementHelpers.AddMethodCall(statements, "instance".ToIdentifierName(), "SetChilds", null, null, childContainer.ChildArguments.Select(a => Eshava.CodeAnalysis.SyntaxConstants.Null).ToArray());
			}

			StatementHelpers.AddMethodCallAndFaultyCheck(statements, "instance".ToIdentifierName(), "Create", null, "createResult", domainModelMap.DomainModelName.ToType(), "patches".ToIdentifierName());

			statements.Add(
				"instance"
				.ToIdentifierName()
				.Access(CommonNames.Extensions.TORESPONSEDATA)
				.Call()
				.Return()
			);

			return (
				dataToInstanceDeclarationName,
				dataToInstanceDeclaration
				.WithParameter(parameterList.ToArray())
				.AddBodyStatements(statements.ToArray())
			);
		}

		private static (string Name, MemberDeclarationSyntax Method) CreateGeneralPatchMethod(ReferenceDomainModelMap domainModelMap)
		{
			var statements = new List<StatementSyntax>
			{
				"patches"
					.ToIdentifierName()
					.Access("Count", true)
					.AddNullFallback("0".ToLiteralInt())
					.Parenthesize()
					.LessThanOrEqual("0".ToLiteralInt())
					.If(
						Eshava.CodeAnalysis.SyntaxConstants.True
						.Access(CommonNames.Extensions.TORESPONSEDATA)
						.Call()
						.Return()
					)
			};

			StatementHelpers.AddLocalMethodCallAndFaultyCheck(statements, "AreAllPatchesAllowed", null, (TypeSyntax)null, "patches".ToIdentifierName());

			if (domainModelMap.IsChildDomainModel)
			{
				StatementHelpers.AddLocalMethodCallAndFaultyCheck(statements, "Update", null, (TypeSyntax)null, "patches".ToIdentifierName());
				statements.Add(
					DomainNames.CALLBACKFIELD
					.ToIdentifierName()
					.IsNull()
					.If("updateResult".ToIdentifierName().Return())
				);

				statements.Add(
					StatementHelpers.GetMethodCall(null, DomainNames.CALLBACKFIELD, Eshava.CodeAnalysis.SyntaxConstants.This)
					.Return()
				);
			}
			else
			{
				statements.Add(
					StatementHelpers.GetMethodCall(null, "Update", "patches".ToIdentifierName())
					.Return()
				);
			}

			var methodDeclarationName = "Patch";
			var methodDeclaration = methodDeclarationName.ToMethod(
				Constants.SyntaxConstants.ResponseDataBool,
				statements,
				SyntaxKind.PublicKeyword
			);

			return (
				methodDeclarationName,
				methodDeclaration
				.WithParameter(
					"patches"
					.ToVariableName()
					.ToParameter()
					.WithType("IList".AsGeneric("Patch".AsGeneric(domainModelMap.DomainModelName)))
				)
			);
		}

		private static TypeSyntax GetCallbackType(string typeName)
		{
			return "Func".AsGeneric(typeName.ToIdentifierName(), Constants.SyntaxConstants.ResponseDataBool);
		}

		private static ObjectCreationExpressionSyntax CreateDomainModelInstanceSyntax(ReferenceDomainModelMap domainModelMap)
		{
			var validation = DomainNames.VALIDATION.Parameter.Name.ToIdentifierName();
			var type = domainModelMap.DomainModelName.ToType();

			return type.ToInstance(validation.ToArgument());
		}

		private static void AddEventDomainProperty(UnitInformation unitInformation, string domain)
		{
			var propertyName = "EventDomain";
			var property = propertyName
				.ToProperty(Eshava.CodeAnalysis.SyntaxConstants.String, [SyntaxKind.PublicKeyword, SyntaxKind.OverrideKeyword], false, false)
				.WithExpressionBody(domain.ToLiteralString());

			unitInformation.AddProperty(property, propertyName);
		}

		private class ChildInitializerContainer
		{
			public ChildInitializerContainer()
			{
				Statements = [];
				Parameters = [];
				ChildArguments = [];
			}

			public List<StatementSyntax> Statements { get; set; }
			public List<ParameterSyntax> Parameters { get; set; }
			public List<ExpressionSyntax> ChildArguments { get; set; }
		}
	}
}