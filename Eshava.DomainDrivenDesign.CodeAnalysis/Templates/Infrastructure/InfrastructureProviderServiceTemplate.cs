using System.Collections.Generic;
using System.Linq;
using Eshava.CodeAnalysis;
using Eshava.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Constants;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Templates.Infrastructure
{
	public static class InfrastructureProviderServiceTemplate
	{
		public static string GetProviderService(
			InfrastructureModel model,
			ReferenceDomainModelMap domainModelMap,
			string domain,
			string featureName,
			string fullQualifiedDomainNamespace,
			string fullQualifiedApplicationNamespace,
			InfrastructureProject project,
			string databaseSettingsInterface,
			string databaseSettingsInterfaceUsing,
			Dictionary<string, List<InfrastructureModel>> childsForModel,
			IEnumerable<InfrastructureCodeSnippet> codeSnippets
		)
		{
			var providerCodeSnippet = codeSnippets
				.FirstOrDefault(cs => cs.ApplyOnInstrastructureProviderService)
				?? new InfrastructureCodeSnippet();

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
			var className = $"{domainModelMap.DomainModelName}InfrastructureProviderService";

			var unitInformation = new UnitInformation(className, @namespace, addAssemblyComment: project.AddAssemblyCommentToFiles);
			unitInformation.AddClassModifier(SyntaxKind.InternalKeyword, SyntaxKind.PartialKeyword);
			unitInformation.AddContructorModifier(SyntaxKind.PublicKeyword);

			unitInformation.AddUsing(CommonNames.Namespaces.LOGGING);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.VALIDATION.INTERFACES);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Infrastructure.INTERFACES);
			unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Infrastructure.PROVIDERS);
			unitInformation.AddUsing($"{fullQualifiedApplicationNamespace}.{featureName}{model.ClassificationKey.ToPlural()}.Commands");
			unitInformation.AddUsing(databaseSettingsInterfaceUsing);

			var repositories = domainModelMap.GetRepositories(project.FullQualifiedNamespace);
			foreach (var repository in repositories)
			{
				unitInformation.AddUsing(repository.Using);
				unitInformation.AddConstructorParameter(repository.Name, repository.Type);
			}

			string baseClass;
			InfrastructureProjectAlternativeClass alternativeClass;
			if (domainModelMap.ChildDomainModels.Any())
			{
				unitInformation.AddUsing(CommonNames.Namespaces.TASKS);
				unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.EXTENSIONS);
				unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.MODELS);
				unitInformation.AddUsing(CommonNames.Namespaces.Eshava.DomainDrivenDesign.Domain.EXTENSIONS);

				var childMethods = GetProcessChildsMethods(model, childsForModel, domainModelMap, domain, true, providerCodeSnippet.PropertyStatements).ToList();
				childMethods.ForEach(m => unitInformation.AddMethod(m));

				alternativeClass = project.AlternativeClasses.FirstOrDefault(ac => ac.Type == InfrastructureAlternativeClassType.AggregateProviderService);
				baseClass = alternativeClass is null
					? InfrastructureNames.ABSTRACTAGGREGATEINFRASTRUCTUREPROVIDER
					: alternativeClass.ClassName;
			}
			else
			{
				alternativeClass = project.AlternativeClasses.FirstOrDefault(ac => ac.Type == InfrastructureAlternativeClassType.ProviderService);
				baseClass = alternativeClass is null
					? InfrastructureNames.ABSTRACTINFRASTRUCTUREPROVIDER
					: alternativeClass.ClassName;
			}

			var fullDomainModelName = $"Domain.{domain}.{domainModelMap.DomainModel.NamespaceDirectory}.{domainModelMap.DomainModelName}";

			var baseType = baseClass.AsGeneric(fullDomainModelName, model.IdentifierType).ToSimpleBaseType();
			var providerServiceInterface = $"I{className}".ToType().ToSimpleBaseType();
			unitInformation.AddBaseType(baseType, providerServiceInterface);
			unitInformation.AddUsing(alternativeClass?.Using);

			var fieldAndArgument = Enums.ParameterTargetTypes.Field | Enums.ParameterTargetTypes.Argument;
			unitInformation.AddConstructorParameter(databaseSettings, fieldAndArgument);
			unitInformation.AddConstructorArgument($"{domainModelMap.DomainModelName.ToVariableName()}Repository");

			if (domainModelMap is not null)
			{
				foreach (var foreignKeyReference in domainModelMap.ForeignKeyReferences)
				{
					if (foreignKeyReference.IsProcessingProperty || foreignKeyReference.DomainModel.IsValueObject)
					{
						continue;
					}

					unitInformation.AddUsing(CommonNames.Namespaces.GENERIC);
					unitInformation.AddUsing(CommonNames.Namespaces.TASKS);
					unitInformation.AddUsing(CommonNames.Namespaces.Eshava.Core.MODELS);
					unitInformation.AddConstructorBodyStatementAndField($"{domainModelMap.DomainModelName.ToVariableName()}Repository", $"I{domainModelMap.DomainModelName}Repository".ToIdentifierName());

					unitInformation.AddMethod(CreateReadForMethod(domainModelMap, foreignKeyReference, fullDomainModelName));
				}

				CheckAndAddProviderReferences(unitInformation, domainModelMap, alternativeClass, providerCodeSnippet);
			}
			else
			{
				CheckAndAddProviderReferences(unitInformation, null, alternativeClass, providerCodeSnippet);
			}

			unitInformation.AddConstructorParameter(DomainNames.VALIDATION.Parameter);
			unitInformation.AddLogger(className);

			return unitInformation.CreateCodeString();
		}

		private static void CheckAndAddProviderReferences(
			UnitInformation unitInformation,
			ReferenceDomainModelMap domainModelMap,
			InfrastructureProjectAlternativeClass alternativeClass,
			InfrastructureCodeSnippet codeSnippet
		)
		{
			if (alternativeClass?.ConstructorParameters?.Any() ?? false)
			{
				foreach (var constructorParameter in alternativeClass.ConstructorParameters)
				{
					unitInformation.AddUsing(constructorParameter.UsingForType);
					unitInformation.AddConstructorParameter(constructorParameter.Name, constructorParameter.Type.ToIdentifierName(), Enums.ParameterTargetTypes.Argument);
				}
			}

			if (codeSnippet is not null)
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
			}

			if (domainModelMap is null)
			{
				return;
			}

			foreach (var constructorParameter in domainModelMap.DomainModel.ProviderServiceConstructorParameters ?? [])
			{
				unitInformation.AddUsing(constructorParameter.UsingForType);
				unitInformation.AddConstructorParameter(constructorParameter.Name, constructorParameter.Type.ToIdentifierName());
			}

			foreach (var child in domainModelMap.ChildDomainModels)
			{
				CheckAndAddProviderReferences(unitInformation, child, null, null);
			}
		}

		private static IEnumerable<(string Name, MethodDeclarationSyntax Method)> GetProcessChildsMethods(InfrastructureModel model, Dictionary<string, List<InfrastructureModel>> childsForModel, ReferenceDomainModelMap domainModelMap, string domain, bool isTopLevelMethod, IEnumerable<InfrastructureModelPropertyCodeSnippet> codeSnippets)
		{
			var methods = new List<(string Name, MethodDeclarationSyntax Method)>();

			if (!childsForModel.TryGetValue(model.Name, out var childModels))
			{
				return methods;
			}

			if (domainModelMap.ChildDomainModels.Count > 0)
			{
				methods.Add(CreateExcecuteCompletionActionsForCreateMethod(model, childModels, domainModelMap, domain, isTopLevelMethod, codeSnippets));
				methods.Add(CreateExcecuteCompletionActionsForUpdateMethod(model, childModels, domainModelMap, domain, isTopLevelMethod, codeSnippets));
				methods.Add(CreateExcecutePrerequisitesActionsForDeleteMethod(model, childModels, domainModelMap, domain, isTopLevelMethod, codeSnippets));

				foreach (var childDomainModelMap in domainModelMap.ChildDomainModels)
				{
					var childModel = childModels.FirstOrDefault(cm => cm.Name == childDomainModelMap.DataModelName);
					if (childModel is null)
					{
						continue;
					}

					methods.AddRange(GetProcessChildsMethods(childModel, childsForModel, childDomainModelMap, domain, false, codeSnippets));
				}
			}

			return methods;
		}

		private static (string Name, MethodDeclarationSyntax Method) CreateExcecuteCompletionActionsForCreateMethod(InfrastructureModel model, IEnumerable<InfrastructureModel> childsForModel, ReferenceDomainModelMap domainModelMap, string domain, bool isTopLevelMethod, IEnumerable<InfrastructureModelPropertyCodeSnippet> codeSnippets)
		{
			return CreateChildActionMethod(MethodAction.Create, true, model, childsForModel, domainModelMap, domain, isTopLevelMethod, codeSnippets);
		}

		private static (string Name, MethodDeclarationSyntax Method) CreateExcecuteCompletionActionsForUpdateMethod(InfrastructureModel model, IEnumerable<InfrastructureModel> childsForModel, ReferenceDomainModelMap domainModelMap, string domain, bool isTopLevelMethod, IEnumerable<InfrastructureModelPropertyCodeSnippet> codeSnippets)
		{
			return CreateChildActionMethod(MethodAction.Update, true, model, childsForModel, domainModelMap, domain, isTopLevelMethod, codeSnippets);
		}

		private static (string Name, MethodDeclarationSyntax Method) CreateExcecutePrerequisitesActionsForDeleteMethod(InfrastructureModel model, IEnumerable<InfrastructureModel> childsForModel, ReferenceDomainModelMap domainModelMap, string domain, bool isTopLevelMethod, IEnumerable<InfrastructureModelPropertyCodeSnippet> codeSnippets)
		{
			return CreateChildActionMethod(MethodAction.Delete, false, model, childsForModel, domainModelMap, domain, isTopLevelMethod, codeSnippets);
		}


		private static string GetByPassMethodName(MethodAction methodAction)
		{
			switch (methodAction)
			{
				case MethodAction.Create:
					return "ByPassActionForCreate";

				case MethodAction.Update:
					return "ByPassActionForUpdate";

				case MethodAction.Delete:
					return "ByPassActionForDelete";
			}

			return "";
		}

		private static (string Name, MethodDeclarationSyntax Method) CreateChildActionMethod(
			MethodAction methodAction,
			bool addCreationBag,
			InfrastructureModel model,
			IEnumerable<InfrastructureModel> childsForModel,
			ReferenceDomainModelMap domainModelMap,
			string domain,
			bool isTopLevelMethod,
			IEnumerable<InfrastructureModelPropertyCodeSnippet> codeSnippets
		)
		{
			var methodName = "";
			var childMethod = "";

			switch (methodAction)
			{
				case MethodAction.Create:
					methodName = "ExcecuteCompletionActionsForCreateAsync";
					childMethod = "SaveChildsAsync";

					break;
				case MethodAction.Update:
					methodName = "ExcecuteCompletionActionsForUpdateAsync";
					childMethod = "SaveChildsAsync";

					break;
				case MethodAction.Delete:
					methodName = "ExcecutePrerequisitesActionsForDeleteAsync";
					childMethod = "DeleteChildsAsync";

					break;
			}


			var statements = new List<StatementSyntax>();

			if (domainModelMap.DomainModel.AddInfrastructureProviderServiceByPassMethod)
			{
				var byPassParameter = new List<ExpressionSyntax>
				{
					 model.ClassificationKey.ToVariableName().ToIdentifierName()
				};

				if (addCreationBag && domainModelMap.IsChildDomainModel)
				{
					byPassParameter.Add($"{domainModelMap.AggregateDomainModel.ClassificationKey.ToVariableName()}CreationBag".ToIdentifierName());
				}

				StatementHelpers.AddLocalAsyncMethodCallAndFaultyCheck(statements, GetByPassMethodName(methodAction), $"byPass{domainModelMap.DomainModelName}Result", (TypeSyntax)null, byPassParameter.ToArray());
			}

			if (addCreationBag)
			{
				var creationBagArguments = new List<ArgumentSyntax>
				{
					model.ClassificationKey.ToVariableName().Access("Id").Access("Value").ToArgument()
				};

				foreach (var property in model.Properties.Where(p => p.AddToCreationBag))
				{
					if (property.SkipFromDomainModel)
					{
						var propertySnippet = codeSnippets.FirstOrDefault(cs => cs.CodeSnippeKey == $"{model.Name}.{property.Name}" && cs.IsMapping)
							?? codeSnippets.FirstOrDefault(cs => cs.CodeSnippeKey == property.Name && cs.IsMapping);

						if (propertySnippet is not null)
						{
							creationBagArguments.Add(propertySnippet.Expression.ToArgument());
						}
						else
						{
							creationBagArguments.Add(SyntaxHelper.CreateDefaultOf(property.Type.ToType()).ToArgument());
						}

						continue;
					}

					creationBagArguments.Add(model.ClassificationKey.ToVariableName().Access(property.Name).ToArgument());
				}

				statements.Add(
					"creationBag"
					.ToVariableStatement(
						$"{domainModelMap.DomainModelName}CreationBag"
						.ToType()
						.ToInstance(creationBagArguments.ToArray())
					)
				);

			}

			foreach (var childDomainModelMap in domainModelMap.ChildDomainModels)
			{
				if (childsForModel.All(cfm => cfm.Name != childDomainModelMap.DataModelName))
				{
					continue;
				}

				var childArguments = new List<ExpressionSyntax>
				{
					domainModelMap.ClassificationKey.ToVariableName().Access(childDomainModelMap.ChildEnumerableName.ToPlural())
				};

				if (addCreationBag)
				{
					childArguments.Add("creationBag".ToIdentifierName());
				}

				childArguments.Add(childDomainModelMap.DomainModelName.ToRepositoryName().ToFieldName().ToIdentifierName());
				childArguments.Add("_".ToParameterExpression(StatementHelpers.GetResponseData(true, true)));

				var childStatements = new List<StatementSyntax>();
				var prerequisitesOrCompletionStatements = new List<StatementSyntax>();

				var saveChildResultName = $"{childDomainModelMap.ChildEnumerableName.ToPlural().ToVariableName()}Result";

				StatementHelpers.AddLocalAsyncMethodCallAndFaultyCheck(childStatements, childMethod, saveChildResultName, (TypeSyntax)null, childArguments.ToArray());

				if (childDomainModelMap.ChildDomainModels.Count > 0)
				{
					var processChildParameter = new List<ExpressionSyntax>
					{
						 childDomainModelMap.ChildEnumerableName.ToVariableName().ToIdentifierName()
					};

					if (addCreationBag)
					{
						processChildParameter.Add("creationBag".ToIdentifierName());
					}

					var processChildResultName = $"process{childDomainModelMap.ChildEnumerableName}Result";
					var loopStatements = new List<StatementSyntax>();
					StatementHelpers.AddLocalAsyncMethodCallAndFaultyCheck(loopStatements, methodName, processChildResultName, (TypeSyntax)null, processChildParameter.ToArray());


					prerequisitesOrCompletionStatements.Add(
						domainModelMap.ClassificationKey
						.ToVariableName()
						.Access(childDomainModelMap.ChildEnumerableName.ToPlural())
						.ForEach(childDomainModelMap.ChildEnumerableName.ToVariableName(), loopStatements)
					);
				}
				else if (childDomainModelMap.ChildDomainModels.Count == 0 && childDomainModelMap.DomainModel.AddInfrastructureProviderServiceByPassMethod)
				{
					var byPassParameter = new List<ExpressionSyntax>
					{
						 childDomainModelMap.ChildEnumerableName.ToVariableName().ToIdentifierName()
					};

					if (addCreationBag && childDomainModelMap.IsChildDomainModel)
					{
						byPassParameter.Add("creationBag".ToIdentifierName());
					}

					var byPassChildResultName = $"byPass{childDomainModelMap.ChildEnumerableName}Result";
					var loopStatements = new List<StatementSyntax>();
					StatementHelpers.AddLocalAsyncMethodCallAndFaultyCheck(loopStatements, GetByPassMethodName(methodAction), byPassChildResultName, (TypeSyntax)null, byPassParameter.ToArray());

					prerequisitesOrCompletionStatements.Add(
						domainModelMap.ClassificationKey
						.ToVariableName()
						.Access(childDomainModelMap.ChildEnumerableName.ToPlural())
						.ForEach(childDomainModelMap.ChildEnumerableName.ToVariableName(), loopStatements)
					);
				}

				if (methodAction == MethodAction.Delete)
				{
					statements.AddRange(prerequisitesOrCompletionStatements);
					statements.AddRange(childStatements);
				}
				else
				{
					statements.AddRange(childStatements);
					statements.AddRange(prerequisitesOrCompletionStatements);
				}
			}

			statements.Add(
				Eshava.CodeAnalysis.SyntaxConstants.True
				.Access(CommonNames.Extensions.TORESPONSEDATA)
				.Call()
				.Return()
			);

			var methodModifiers = isTopLevelMethod
				? new List<SyntaxKind>
				{
					SyntaxKind.ProtectedKeyword,
					SyntaxKind.AsyncKeyword,
					SyntaxKind.OverrideKeyword
				}
				: new List<SyntaxKind>
				{
					SyntaxKind.PrivateKeyword,
					SyntaxKind.AsyncKeyword
				};

			var methodDeclaration = methodName.ToMethod(
				"Task".AsGeneric(CommonNames.RESPONSEDATA.AsGeneric(Eshava.CodeAnalysis.SyntaxConstants.Bool)),
				statements,
				methodModifiers.ToArray()
			);

			var domainModelReferenceType = $"Domain.{domain}.{domainModelMap.DomainModel.NamespaceDirectory}.{domainModelMap.DomainModelName}".ToType();
			var methodParameter = new List<ParameterSyntax>
			{
				model.ClassificationKey
					.ToVariableName()
					.ToParameter()
					.WithType(domainModelReferenceType)
			};

			if (addCreationBag && domainModelMap.IsChildDomainModel)
			{
				var aggregateReferenceType = $"{domainModelMap.AggregateDomainModel.DomainModelName}CreationBag".ToType();
				methodParameter.Add(
					$"{domainModelMap.AggregateDomainModel.ClassificationKey.ToVariableName()}CreationBag"
					.ToParameter()
					.WithType(aggregateReferenceType)
				);
			}

			return (
				methodName,
				methodDeclaration
				.WithParameter(methodParameter.ToArray())
			);
		}

		private static (string Name, MethodDeclarationSyntax Method) CreateReadForMethod(
			ReferenceDomainModelMap domainModelMap,
			ReferenceDomainModel foreignKeyReference,
			string fullDomainModelName
		)
		{
			var statements = new List<StatementSyntax>();
			var methodName = $"ReadFor{foreignKeyReference.PropertyName}Async";
			var parameterName = $"{foreignKeyReference.PropertyName.ToVariableName()}";

			var call = StatementHelpers.GetMethodCall($"{domainModelMap.DomainModelName.ToVariableName().ToFieldName()}Repository".ToIdentifierName(), methodName, parameterName.ToIdentifierName());

			statements.Add(call.Return());

			var methodDeclaration = methodName.ToMethod(
				"Task".AsGeneric(CommonNames.RESPONSEDATA.AsGeneric("IEnumerable".AsGeneric(fullDomainModelName))),
				statements,
				SyntaxKind.PublicKeyword
			);

			return (
				methodName,
				methodDeclaration
				.WithParameter(
					parameterName
					.ToParameter()
					.WithType(domainModelMap.IdentifierType.ToType())
				)
			);
		}
		private enum MethodAction
		{
			Create = 1,
			Update = 2,
			Delete = 3
		}
	}
}