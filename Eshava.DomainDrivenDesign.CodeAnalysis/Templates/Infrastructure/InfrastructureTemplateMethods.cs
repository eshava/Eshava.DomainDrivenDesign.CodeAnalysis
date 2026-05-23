using System;
using System.Collections.Generic;
using System.Linq;
using Eshava.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Enums;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Templates.Infrastructure
{
	public static class InfrastructureTemplateMethods
	{
		public static void AddMissingQueryAnalysisItemsForApplicableCodeSnippets(
			List<QueryAnalysisItem> items,
			string rootDomain,
			List<ApplicableWhereConditionCodeSnippet> applicableWhereConditionCodeSnippets,
			bool isForDomainModelRepository
		)
		{
			if (applicableWhereConditionCodeSnippets is null || applicableWhereConditionCodeSnippets.Count == 0)
			{
				return;
			}

			foreach (var applicableCodeSnippet in applicableWhereConditionCodeSnippets)
			{
				var modelChain = applicableCodeSnippet.ModelChain
					.GetItemsFromRoot()
					.ToList();

				if (modelChain.Count < 2)
				{
					continue;
				}

				for (var i = 1; i < modelChain.Count; i++)
				{
					var parentChainItem = modelChain[i - 1];
					var currentChainItem = modelChain[i];
					var parentModel = parentChainItem.Model;
					var parentDomain = parentChainItem.Domain;
					var currentModel = currentChainItem.Model;
					var currentDomain = currentChainItem.Domain;

					if (items.Any(item => item.ParentDomain == parentDomain
						&& item.Domain == currentDomain
						&& item.ParentDataModel?.Name == parentModel.Name
						&& item.DataModel.Name == currentModel.Name))
					{
						continue;
					}

					var parentNavigationProperty = parentModel.Properties.FirstOrDefault(p => p.ReferenceType == currentModel.Name && !p.ReferencePropertyName.IsNullOrEmpty() && (p.ReferenceDomain.IsNullOrEmpty() || p.ReferenceDomain == currentDomain));
					var parentReferenceProperty = parentModel.Properties.FirstOrDefault(p => p.IsReference && p.ReferenceType == currentModel.Name && (p.ReferenceDomain.IsNullOrEmpty() || p.ReferenceDomain == currentDomain));
					var property = currentModel.Properties.FirstOrDefault(p => p.IsReference && p.ReferenceType == parentModel.Name && (p.ReferenceDomain.IsNullOrEmpty() || p.ReferenceDomain == parentDomain));
					var parentProperty = parentNavigationProperty;
					var isEnumerable = false;
					Models.Application.ApplicationUseCaseDtoProperty dtoProperty = null;

					if (parentReferenceProperty is null && property is null)
					{
						continue;
					}

					if (parentReferenceProperty is not null)
					{
						parentProperty ??= new InfrastructureModelProperty
						{
							Name = currentModel.Name,
							Type = currentModel.Name,
							ReferenceType = currentModel.Name,
							ReferencePropertyName = parentReferenceProperty.Name,
							ReferenceDomain = parentReferenceProperty.ReferenceDomain
						};

						property = new InfrastructureModelProperty
						{
							Name = "Id",
							Type = currentModel.IdentifierType
						};
					}
					else
					{
						isEnumerable = true;
						dtoProperty = new Models.Application.ApplicationUseCaseDtoProperty { Name = "*" };
					}

					var tableAlis = currentModel.Name.CreateModelConstantField();
					items.Add(new QueryAnalysisItem
					{
						ParentDomain = parentDomain,
						Domain = currentDomain,
						ParentDataModel = parentModel,
						DataModel = currentModel,
						ParentProperty = parentProperty,
						Property = property,
						ParentDtoName = null,
						ParentDtoPropertyName = null,
						Dto = null,
						DtoProperty = isForDomainModelRepository ? dtoProperty : null,
						IsEnumerable = isEnumerable,
						IsGroupBy = isEnumerable,
						IsRootModel = currentDomain == rootDomain && i == 0,
						IsOnlyForSqlJoinCalculation = true,
						TableAliasConstant = tableAlis.FieldName,
						TableAliasField = tableAlis.Declaration
					});
				}
			}
		}

		public static (List<InterpolatedStringContentSyntax> Query, List<InterpolatedStringContentSyntax> Columns) CreateSqlQueryWithoutWhereCondition(
			InfrastructureModel model,
			string domain,
			List<QueryAnalysisItem> relatedDataModels,
			bool implementSoftDelete,
			bool asCount,
			List<(ExpressionSyntax Property, string Name)> queryParameters,
			MethodMetaData metaData
		)
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
					if (item.DtoProperty is null)
					{
						continue;
					}

					if (item.DtoProperty.IsVirtualProperty)
					{
						if (item.TypeProperty.Property is not null && item.DtoProperty.Name != "*")
						{
							isFirstColum = AddSelectStatement(interpolatedColumnParts, isFirstColum, addedSelectParts, item, item.GetDataType(domain, model.Name), item.TypeProperty.Property.Name);
						}

						continue;
					}

					var dataType = item.GetDataType(domain, model.Name);

					if (item.DtoProperty.Name == "*")
					{
						var selectPart = $"{item.TableAliasConstant}.*";
						if (!addedSelectParts.Contains(selectPart))
						{
							isFirstColum = AddSelectColumnSeparator(interpolatedColumnParts, isFirstColum);

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

						isFirstColum = AddSelectStatement(interpolatedColumnParts, isFirstColum, addedSelectParts, item, dataType, propertyName);
					}
				}
			}

			var referenceRelations = relatedDataModels
				.Where(m => m.DataModel.Name != modelItem.DataModel.Name
					|| (m.DataModel.Name == modelItem.DataModel.Name && m.TableAliasConstant != modelItem.TableAliasConstant)
					|| (m.Domain != modelItem.Domain && m.DataModel.Name == modelItem.DataModel.Name)
				)
				.GroupBy(m => new { m.Domain, Model = m.DataModel.Name, Property = m.Property.Name, m.TableAliasConstant })
				.Select(g => g.First())
				.ToList();

			CreateJoinQueryParts(modelItem, referenceRelations, interpolatedTableParts, implementSoftDelete, queryParameters, metaData);

			interpolatedStringParts.AddRange(interpolatedColumnParts);
			interpolatedStringParts.Add(@"
					FROM
						".Interpolate());
			interpolatedStringParts.Add("TypeAnalyzer".Access("GetTableName".AsGeneric(modelItem.GetDataType(domain, model.Name))).Call().Interpolate());
			interpolatedStringParts.Add(" ".Interpolate());
			interpolatedStringParts.Add(modelItem.TableAliasConstant.ToIdentifierName().Interpolate());
			interpolatedStringParts.AddRange(interpolatedTableParts);

			return (interpolatedStringParts, interpolatedColumnParts);
		}

		public static List<InterpolatedStringContentSyntax> CreateSqlJoinQueryParts(
			InfrastructureModel model,
			string domain,
			string tableAliasConstant,
			List<QueryAnalysisItem> relatedDataModels,
			bool implementSoftDelete,
			List<(ExpressionSyntax Property, string Name)> queryParameters,
			MethodMetaData metaData
		)
		{
			var interpolatedTableParts = new List<InterpolatedStringContentSyntax>();
			var modelItem = new QueryAnalysisItem
			{
				Domain = domain,
				DataModel = model,
				TableAliasConstant = tableAliasConstant,
				IsRootModel = true
			};

			var referenceRelations = relatedDataModels
				.Where(m => m.DataModel.Name != modelItem.DataModel.Name
					|| (m.DataModel.Name == modelItem.DataModel.Name && m.TableAliasConstant != modelItem.TableAliasConstant)
					|| (m.Domain != modelItem.Domain && m.DataModel.Name == modelItem.DataModel.Name)
				)
				.GroupBy(m => new { m.Domain, Model = m.DataModel.Name, Property = m.Property.Name, m.TableAliasConstant })
				.Select(g => g.First())
				.ToList();

			CreateJoinQueryParts(modelItem, referenceRelations, interpolatedTableParts, implementSoftDelete, queryParameters, metaData);

			return interpolatedTableParts;
		}

		private static bool AddSelectStatement(List<InterpolatedStringContentSyntax> interpolatedColumnParts, bool isFirstColum, HashSet<string> addedSelectParts, QueryAnalysisItem item, string dataType, string propertyName)
		{
			var selectPart = $"{item.TableAliasConstant}.{propertyName}";

			if (!addedSelectParts.Contains(selectPart))
			{
				isFirstColum = AddSelectColumnSeparator(interpolatedColumnParts, isFirstColum);

				interpolatedColumnParts.Add(item.TableAliasConstant.ToIdentifierName().Interpolate());
				interpolatedColumnParts.Add(".".Interpolate());
				interpolatedColumnParts.Add(Eshava.CodeAnalysis.SyntaxConstants.NameOf.Call(dataType.Access(propertyName).ToArgument()).Interpolate());
				addedSelectParts.Add(selectPart);
			}

			return isFirstColum;
		}

		public static void AddCodeSnippetReadConditions(
			List<InterpolatedStringContentSyntax> interpolatedStringParts,
			List<(ExpressionSyntax Property, string Name)> queryParameters,
			InfrastructureModel dataModel,
			string dataModelTypeForJoin,
			IdentifierNameSyntax tableAliasConstant,
			MethodMetaData metaData,
			bool isForJoinCondition = false
		)
		{
			if (dataModelTypeForJoin.IsNullOrEmpty())
			{
				dataModelTypeForJoin = dataModel.Name;
			}

			foreach (var dataModelProperty in dataModel.Properties)
			{
				var snippetExpression = GetCodeSnippet(dataModel, dataModelProperty, metaData, true, false, isForJoinCondition);
				if (snippetExpression.Expression is null)
				{
					continue;
				}

				if (isForJoinCondition)
				{
					interpolatedStringParts.Add(@"
							AND ".Interpolate());
				}
				else
				{
					interpolatedStringParts.Add(@"
					AND
						".Interpolate());
				}

				var snippetOperation = snippetExpression.Operation.Map();

				var parameterName = snippetExpression.UseDefault
					? dataModelProperty.Name
					: dataModelProperty.Name + DateTime.UtcNow.Ticks.ToString();

				interpolatedStringParts.Add(tableAliasConstant.Interpolate());
				interpolatedStringParts.Add(".".Interpolate());
				interpolatedStringParts.Add(Eshava.CodeAnalysis.SyntaxConstants.NameOf.Call(dataModelTypeForJoin.Access(dataModelProperty.Name).ToArgument()).Interpolate());
				interpolatedStringParts.Add($@" {snippetOperation} @{parameterName}".Interpolate());

				if (queryParameters.All(qp => qp.Name != parameterName))
				{
					queryParameters.Add((snippetExpression.Expression, parameterName));
				}
			}
		}

		public static (bool UseDefault, bool Skip, ExpressionSyntax Expression, OperationType Operation) GetCodeSnippedForStatusProperty(
			string dataModelName,
			MethodMetaData metaData,
			bool isFilter,
			bool isMapping
		)
		{
			var propertySnippet = GetCodeSnippet(dataModelName, "Status", metaData.CodeSnippets, isFilter, isMapping);
			if (propertySnippet is null || propertySnippet.Exceptions.Count == 0)
			{
				return (true, false, null, OperationType.Equal);
			}

			var snippetException = GetCodeSnippetException(propertySnippet, dataModelName, metaData);

			if (snippetException is null)
			{
				return (true, false, null, OperationType.Equal);
			}

			if (snippetException.SkipUsage)
			{
				return (false, true, null, OperationType.Equal);
			}

			if (snippetException.UseInstead && snippetException?.Expression is not null)
			{
				return (false, false, snippetException.Expression, snippetException.Operation);
			}

			return (true, false, null, OperationType.Equal);
		}

		public static (ExpressionSyntax Expression, OperationType Operation, bool UseDefault) GetCodeSnippet(
			InfrastructureModel model,
			InfrastructureModelProperty property,
			MethodMetaData metaData,
			bool isFilter,
			bool isMapping,
			bool isForJoinCondition
		)
		{
			var propertySnippet = GetCodeSnippet(model.Name, property.Name, metaData.CodeSnippets, isFilter, isMapping);
			if (propertySnippet is null)
			{
				return (null, OperationType.Equal, true);
			}

			if (propertySnippet.ForceAsWhereCondition && isForJoinCondition)
			{
				return (null, OperationType.Equal, true);
			}

			if (propertySnippet.Exceptions.Count > 0)
			{
				var snippetException = GetCodeSnippetException(propertySnippet, model.Name, metaData);

				if (snippetException?.SkipUsage ?? false)
				{
					return (null, OperationType.Equal, true);
				}

				if (snippetException?.UseInstead ?? false && snippetException?.Expression is not null)
				{
					return (snippetException.Expression, snippetException.Operation, false);
				}

				if (snippetException is not null)
				{
					return (propertySnippet.Expression, snippetException.Operation, false);
				}
			}

			return (propertySnippet.Expression, propertySnippet.Operation, true);
		}

		public static List<ApplicableWhereConditionCodeSnippet> FilterApplicableWhereConditionCodeSnippets(
			IEnumerable<ApplicableWhereConditionCodeSnippet> applicableWhereConditionCodeSnippets,
			MethodMetaData metaData
		)
		{
			if (applicableWhereConditionCodeSnippets is null)
			{
				return [];
			}

			return applicableWhereConditionCodeSnippets
				.Where(snippet => !(GetCodeSnippetException(snippet.CodeSnippet, snippet.ModelChain.Model.Name, metaData)?.SkipUsage ?? false))
				.ToList();
		}

		public static List<QueryAnalysisItem> FilterQueryAnalysisItemsForApplicableWhereConditionCodeSnippets(
			IEnumerable<QueryAnalysisItem> relatedDataModels,
			MethodMetaData metaData
		)
		{
			if (relatedDataModels is null)
			{
				return [];
			}

			var queryAnalysisItems = relatedDataModels.ToList();

			return queryAnalysisItems
				.Where(item => !item.IsOnlyForSqlJoinCalculation
					|| HasApplicableWhereConditionCodeSnippet(item, queryAnalysisItems, metaData, new HashSet<string>()))
				.ToList();
		}

		public static List<ApplicableWhereConditionCodeSnippet> GetApplicableWhereConditionCodeSnippets(
			InfrastructureModel model,
			string modelDomain,
			Dictionary<string, Dictionary<string, InfrastructureModel>> infratructureModelsByDomainAndName,
			IEnumerable<InfrastructureModelPropertyCodeSnippet> whereConditionCodeSnippets
		)
		{
			var applicableWhereConditionCodeSnippets = new List<ApplicableWhereConditionCodeSnippet>();
			if (model is null || whereConditionCodeSnippets is null)
			{
				return applicableWhereConditionCodeSnippets;
			}

			CollectApplicableWhereConditionCodeSnippets(model, modelDomain, infratructureModelsByDomainAndName, whereConditionCodeSnippets.ToList(), applicableWhereConditionCodeSnippets, null, new HashSet<string>());

			return applicableWhereConditionCodeSnippets;
		}

		public static void AddStatusWhereCondition(
			string modelConstant,
			string dataModelName,
			List<InterpolatedStringContentSyntax> interpolatedStringParts,
			List<(ExpressionSyntax Property, string Name)> queryParameters,
			MethodMetaData metaData
		)
		{
			(var useDefault, var skip, var expression, var operation) = GetCodeSnippedForStatusProperty(dataModelName, metaData, true, false);
			if (skip)
			{
				return;
			}

			var variableName = useDefault
				? "Status"
				: "Status" + DateTime.UtcNow.Ticks.ToString();

			var operationType = useDefault
				? "="
				: operation.Map();

			interpolatedStringParts.Add($@"
					AND
						".Interpolate());
			interpolatedStringParts.Add(modelConstant.ToIdentifierName().Interpolate());
			interpolatedStringParts.Add(".".Interpolate());
			interpolatedStringParts.Add(Eshava.CodeAnalysis.SyntaxConstants.NameOf.Call(dataModelName.Access("Status").ToArgument()).Interpolate());
			interpolatedStringParts.Add($" {operationType} @{variableName}".Interpolate());

			if (!useDefault)
			{
				queryParameters.Add((expression, variableName));
			}
		}

		public static InfrastructureModelPropertyCodeSnippet GetCodeSnippet(
			string dataModelName,
			string dataModelPropertyName,
			IEnumerable<InfrastructureModelPropertyCodeSnippet> codeSnippets,
			bool isFilter,
			bool isMapping
		)
		{
			if (!codeSnippets.Any())
			{
				return null;
			}

			Func<InfrastructureModelPropertyCodeSnippet, bool> usage = cs => (isMapping && cs.IsMapping) || (isFilter && cs.IsFilter);
			var propertySnippet = codeSnippets.FirstOrDefault(cs => cs.CodeSnippeKey == $"{dataModelName}.{dataModelPropertyName}" && usage(cs))
				?? codeSnippets.FirstOrDefault(cs => cs.CodeSnippeKey == dataModelPropertyName && usage(cs));

			return propertySnippet;
		}

		public static void CheckAnalysisItemForTypeProperty(List<QueryAnalysisItem> items)
		{
			foreach (var itemGroup in items.GroupBy(item => new { item.TableAliasConstant, Model = item.DataModel.Name, Property = item.Property?.Name }))
			{
				if (itemGroup.Count() == 1)
				{
					continue;
				}

				var item = itemGroup.First();

				InfrastructureModelProperty typeProperty = null;
				var typeValues = new List<string>();
				foreach (var itemWithTypeProperty in itemGroup.Where(p => p.TypeProperty.Property != null))
				{
					typeProperty = itemWithTypeProperty.TypeProperty.Property;
					typeValues.AddRange(itemWithTypeProperty.TypeProperty.Values);
				}

				item.TypeProperty = (typeProperty, typeValues);
			}
		}

		private static void CollectApplicableWhereConditionCodeSnippets(
			InfrastructureModel model,
			string modelDomain,
			Dictionary<string, Dictionary<string, InfrastructureModel>> infratructureModelsByDomainAndName,
			IReadOnlyCollection<InfrastructureModelPropertyCodeSnippet> whereConditionCodeSnippets,
			List<ApplicableWhereConditionCodeSnippet> applicableWhereConditionCodeSnippets,
			ApplicableInfrastructureModelChainItem currentModelChain,
			HashSet<string> processedModels
		)
		{
			var processedModelKey = $"{modelDomain}.{model.Name}";
			if (!processedModels.Add(processedModelKey))
			{
				return;
			}

			var modelChain = new ApplicableInfrastructureModelChainItem(modelDomain, model, currentModelChain);

			foreach (var property in model.Properties)
			{
				foreach (var codeSnippet in whereConditionCodeSnippets.Where(cs => IsApplicableWhereConditionCodeSnippet(model, property, cs)))
				{
					applicableWhereConditionCodeSnippets.Add(new ApplicableWhereConditionCodeSnippet(codeSnippet, property, modelChain));
				}

				if (!property.IsReference || property.ReferenceType.IsNullOrEmpty())
				{
					continue;
				}

				var referenceDomain = property.ReferenceDomain.IsNullOrEmpty()
					? modelDomain
					: property.ReferenceDomain;

				infratructureModelsByDomainAndName.TryGetValue(referenceDomain, out var modelsForReferenceDomain);
				if (modelsForReferenceDomain is null || !modelsForReferenceDomain.TryGetValue(property.ReferenceType, out var referencedModel))
				{
					continue;
				}

				CollectApplicableWhereConditionCodeSnippets(referencedModel, referenceDomain, infratructureModelsByDomainAndName, whereConditionCodeSnippets, applicableWhereConditionCodeSnippets, modelChain, processedModels);
			}

			processedModels.Remove(processedModelKey);
		}

		private static bool IsApplicableWhereConditionCodeSnippet(
			InfrastructureModel model,
			InfrastructureModelProperty property,
			InfrastructureModelPropertyCodeSnippet codeSnippet
		)
		{
			var codeSnippetKey = codeSnippet.CodeSnippeKey;

			return codeSnippetKey == $"{model.Name}.{property.Name}"
				|| codeSnippetKey == property.Name;
		}

		private static InfrastructureExceptionCodeSnippet GetCodeSnippetException(
			InfrastructureModelPropertyCodeSnippet propertySnippet,
			string dataModelName,
			MethodMetaData metaData
		)
		{
			if (propertySnippet?.Exceptions is null || propertySnippet.Exceptions.Count == 0 || metaData is null)
			{
				return null;
			}

			return propertySnippet.Exceptions.FirstOrDefault(e =>
				e.ClassName == metaData.ClassName
				&& e.MethodName == metaData.MethodName
				&& e.DataModelName == dataModelName
			);
		}
		private static bool HasApplicableWhereConditionCodeSnippet(
			QueryAnalysisItem item,
			List<QueryAnalysisItem> relatedDataModels,
			MethodMetaData metaData,
			HashSet<string> processedItems
		)
		{
			var itemKey = $"{item.Domain}.{item.DataModel?.Name}.{item.TableAliasConstant}";
			if (!processedItems.Add(itemKey))
			{
				return false;
			}

			var whereConditionCodeSnippets = metaData.CodeSnippets.Where(cs => cs.ForceAsWhereCondition).ToList();
			var hasApplicableSnippet = item.DataModel?.Properties.Any(property =>
			{
				var propertySnippet = GetCodeSnippet(item.DataModel.Name, property.Name, whereConditionCodeSnippets, true, false);
				if (propertySnippet is null)
				{
					return false;
				}

				var snippetException = GetCodeSnippetException(propertySnippet, item.DataModel.Name, metaData);
				if (snippetException?.SkipUsage ?? false)
				{
					return false;
				}

				return snippetException?.UseInstead != true || snippetException.Expression is not null || propertySnippet.Expression is not null;
			}) ?? false;

			if (hasApplicableSnippet)
			{
				return true;
			}

			foreach (var childItem in relatedDataModels.Where(child => child.IsOnlyForSqlJoinCalculation && child.ParentDomain == item.Domain && child.ParentDataModel?.Name == item.DataModel?.Name))
			{
				if (HasApplicableWhereConditionCodeSnippet(childItem, relatedDataModels, metaData, new HashSet<string>(processedItems)))
				{
					return true;
				}
			}

			return false;
		}

		private static bool AddSelectColumnSeparator(List<InterpolatedStringContentSyntax> interpolatedColumnParts, bool isFirstColum)
		{
			if (isFirstColum)
			{
				interpolatedColumnParts.Add(@"
						 ".Interpolate());
			}
			else
			{
				interpolatedColumnParts.Add(@"
						,".Interpolate());
			}

			return false;
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
			bool implementSoftDelete,
			List<(ExpressionSyntax Property, string Name)> queryParameters,
			MethodMetaData metaData
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

				CreateJoinQueryParts(parentItem.Domain, parentItem.DataModel.Name, parentItem, item, items, interpolatedTableParts, processedTableAliases, implementSoftDelete, queryParameters, metaData);
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
			bool implementSoftDelete,
			List<(ExpressionSyntax Property, string Name)> queryParameters,
			MethodMetaData metaData
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

					var codeSnippetTableParts = new List<InterpolatedStringContentSyntax>();
					AddCodeSnippetReadConditions(codeSnippetTableParts, queryParameters, item.DataModel, dataType, item.TableAliasConstant.ToIdentifierName(), metaData, true);

					interpolatedTableParts.AddRange(
						GetJoinsQueryParts(
							item.TableAliasConstant,
							dataType,
							item.DataModel.Name,
							item.Property.Name,
							parentItem.TableAliasConstant,
							parentDataType,
							"Id",
							implementSoftDelete,
							queryParameters,
							metaData,
							codeSnippetTableParts.Count > 0 ? SqlJoinType.Join : SqlJoinType.LeftJoin
						)
					);
					match = true;

					CheckAndAddTypePropertyJoinCondition(item, interpolatedTableParts, queryParameters, dataType);
					if (codeSnippetTableParts.Count > 0)
					{
						interpolatedTableParts.AddRange(codeSnippetTableParts);
					}
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

						var codeSnippetTableParts = new List<InterpolatedStringContentSyntax>();
						AddCodeSnippetReadConditions(codeSnippetTableParts, queryParameters, item.DataModel, dataType, item.TableAliasConstant.ToIdentifierName(), metaData, true);

						interpolatedTableParts.AddRange(
							GetJoinsQueryParts(
								item.TableAliasConstant,
								dataType,
								item.DataModel.Name,
								referenceProperty.Name,
								parentItem.TableAliasConstant,
								parentDataType,
								"Id",
								implementSoftDelete,
								queryParameters,
								metaData,
								codeSnippetTableParts.Count > 0 ? SqlJoinType.Join : SqlJoinType.LeftJoin
							)
						);
						match = true;

						if (codeSnippetTableParts.Count > 0)
						{
							interpolatedTableParts.AddRange(codeSnippetTableParts);
						}
					}
				}
				else
				{
					var dataType = item.GetDataType(referenceDomain, referenceDataModel);
					var parentDataType = parentItem.GetDataType(referenceDomain, referenceDataModel);

					var codeSnippetTableParts = new List<InterpolatedStringContentSyntax>();
					AddCodeSnippetReadConditions(codeSnippetTableParts, queryParameters, item.DataModel, dataType, item.TableAliasConstant.ToIdentifierName(), metaData, true);

					// current models has a foreign key for this domain model
					interpolatedTableParts.AddRange(
						GetJoinsQueryParts(
							item.TableAliasConstant,
							dataType,
							item.DataModel.Name,
							"Id",
							parentItem.TableAliasConstant,
							parentDataType,
							referenceProperty.Name,
							implementSoftDelete,
							queryParameters,
							metaData,
							codeSnippetTableParts.Count > 0 ? SqlJoinType.Join : SqlJoinType.LeftJoin
						)
					);
					match = true;

					if (codeSnippetTableParts.Count > 0)
					{
						interpolatedTableParts.AddRange(codeSnippetTableParts);
					}
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

					if (newItem.DtoProperty is null && !newItem.IsOnlyForSqlJoinCalculation)
					{
						continue;
					}

					if (!newItem.IsOnlyForSqlJoinCalculation
						&& !(newItem.DtoProperty?.ReferenceProperty.IsNullOrEmpty() ?? true)
						&& !(item.DtoProperty?.ReferenceProperty.IsNullOrEmpty() ?? true))
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

					CreateJoinQueryParts(referenceDomain, referenceDataModel, item, newItem, items, interpolatedTableParts, processedTableAliases, implementSoftDelete, queryParameters, metaData);
				}
			}
		}

		private static void CheckAndAddTypePropertyJoinCondition(QueryAnalysisItem item, List<InterpolatedStringContentSyntax> interpolatedTableParts, List<(ExpressionSyntax Property, string Name)> queryParameters, string dataType)
		{
			if (item.TypeProperty.Property is not null && item.TypeProperty.Values.Count > 0)
			{
				interpolatedTableParts.Add(@"
							AND ".Interpolate());

				var snippetOperation = item.TypeProperty.Values.Count == 1
					? OperationType.Equal.Map()
					: OperationType.In.Map();

				var typePropertyParameter = $"{item.DataModel.Name}{item.TypeProperty.Property.Name}";

				interpolatedTableParts.Add(item.TableAliasConstant.ToIdentifierName().Interpolate());
				interpolatedTableParts.Add(".".Interpolate());
				interpolatedTableParts.Add(Eshava.CodeAnalysis.SyntaxConstants.NameOf.Call(dataType.Access(item.TypeProperty.Property.Name).ToArgument()).Interpolate());
				interpolatedTableParts.Add($@" {snippetOperation} @{typePropertyParameter}".Interpolate());

				if (queryParameters.All(qp => qp.Name != typePropertyParameter))
				{
					ExpressionSyntax typeValue = null;
					if (item.TypeProperty.Values.Count == 1)
					{
						typeValue = item.TypeProperty.Values[0].ToLiteral(item.TypeProperty.Property.Type);
					}
					else
					{
						var typeValues = item.TypeProperty.Values.Select(v => v.ToLiteral(item.TypeProperty.Property.Type)).ToArray();
						typeValue = "List".AsGeneric(item.TypeProperty.Property.Type).ToInstanceWithInitializer(typeValues);
					}

					queryParameters.Add((typeValue, typePropertyParameter));
				}
			}
		}

		private static List<InterpolatedStringContentSyntax> GetJoinsQueryParts(
			string tableAliasJoin,
			string dataTypeJoin,
			string dataModelName,
			string propertyJoin,
			string tableAliasParent,
			string dataTypeParent,
			string propertyParent,
			bool implementSoftDelete,
			List<(ExpressionSyntax Property, string Name)> queryParameters,
			MethodMetaData metaData,
			SqlJoinType joinType = SqlJoinType.LeftJoin
		)
		{
			var joinTypeString = joinType switch
			{
				SqlJoinType.Join => "JOIN",
				SqlJoinType.LeftJoin => "LEFT JOIN",
				SqlJoinType.RightJoin => "RIGHT JOIN",
				_ => "LEFT JOIN"
			};

			var interpolatedTableParts = new List<InterpolatedStringContentSyntax>
			{
				$@"
					{joinTypeString}
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
				AddStatusJoinCondition(tableAliasJoin, dataTypeJoin, dataModelName, interpolatedTableParts, queryParameters, metaData);
			}

			return interpolatedTableParts;
		}

		private static void AddStatusJoinCondition(
			string tableAliasJoin,
			string dataTypeJoin,
			string dataModelName,
			List<InterpolatedStringContentSyntax> interpolatedTableParts,
			List<(ExpressionSyntax Property, string Name)> queryParameters,
			MethodMetaData metaData
		)
		{
			(var useDefault, var skip, var expression, var operation) = GetCodeSnippedForStatusProperty(dataModelName, metaData, true, false);
			if (skip)
			{
				return;
			}

			var variableName = useDefault
				? "Status"
				: $"Status{DateTime.UtcNow.Ticks}";

			var operationType = useDefault
				? "="
				: operation.Map();

			interpolatedTableParts.Add(@"
							AND ".Interpolate());
			interpolatedTableParts.Add(tableAliasJoin.ToIdentifierName().Interpolate());
			interpolatedTableParts.Add(".".Interpolate());
			interpolatedTableParts.Add(Eshava.CodeAnalysis.SyntaxConstants.NameOf.Call(dataTypeJoin.Access("Status").ToArgument()).Interpolate());
			interpolatedTableParts.Add($" {operationType} @{variableName}".Interpolate());

			if (!useDefault)
			{
				queryParameters.Add((expression, variableName));
			}
		}
	}
}
