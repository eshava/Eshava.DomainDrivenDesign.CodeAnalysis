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
		public static List<InterpolatedStringContentSyntax> CreateSqlQueryWithoutWhereCondition(
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
				.Where(m => m.DataModel.Name != modelItem.DataModel.Name || (m.DataModel.Name == modelItem.DataModel.Name && m.TableAliasConstant != modelItem.TableAliasConstant) || (m.Domain != modelItem.Domain && m.DataModel.Name == modelItem.DataModel.Name))
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

			return interpolatedStringParts;
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
			bool isJoinCondition = false
		)
		{
			if (dataModelTypeForJoin.IsNullOrEmpty())
			{
				dataModelTypeForJoin = dataModel.Name;
			}

			foreach (var dataModelProperty in dataModel.Properties)
			{
				var snippetExpression = GetCodeSnippet(dataModel, dataModelProperty, metaData, true, false);
				if (snippetExpression.Expression is null)
				{
					continue;
				}

				if (isJoinCondition)
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

				interpolatedStringParts.Add(tableAliasConstant.Interpolate());
				interpolatedStringParts.Add(".".Interpolate());
				interpolatedStringParts.Add(Eshava.CodeAnalysis.SyntaxConstants.NameOf.Call(dataModelTypeForJoin.Access(dataModelProperty.Name).ToArgument()).Interpolate());
				interpolatedStringParts.Add($@" {snippetOperation} @{dataModelProperty.Name}".Interpolate());

				if (queryParameters.All(qp => qp.Name != dataModelProperty.Name))
				{
					queryParameters.Add((snippetExpression.Expression, dataModelProperty.Name));
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

			var snippetException = propertySnippet.Exceptions.FirstOrDefault(e =>
				e.ClassName == metaData.ClassName
				&& e.MethodName == metaData.MethodName
				&& e.DataModelName == dataModelName
			);

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

		public static (ExpressionSyntax Expression, OperationType Operation) GetCodeSnippet(
			InfrastructureModel model,
			InfrastructureModelProperty property,
			MethodMetaData metaData,
			bool isFilter,
			bool isMapping
		)
		{
			var propertySnippet = GetCodeSnippet(model.Name, property.Name, metaData.CodeSnippets, isFilter, isMapping);
			if (propertySnippet is null)
			{
				return (null, OperationType.Equal);
			}

			if (propertySnippet.Exceptions.Count > 0)
			{
				var snippetException = propertySnippet.Exceptions.FirstOrDefault(e =>
					e.ClassName == metaData.ClassName
					&& e.MethodName == metaData.MethodName
					&& e.DataModelName == model.Name
				);

				if (snippetException?.SkipUsage ?? false)
				{
					return (null, OperationType.Equal);
				}

				if (snippetException?.UseInstead ?? false && snippetException?.Expression is not null)
				{
					return (snippetException.Expression, snippetException.Operation);
				}

				if (snippetException is not null)
				{
					return (propertySnippet.Expression, snippetException.Operation);
				}
			}

			return (propertySnippet.Expression, propertySnippet.Operation);
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
				: $"Status{DateTime.UtcNow.Ticks}";

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
							metaData
						)
					);
					match = true;

					CheckAndAddTypePropertyJoinCondition(item, interpolatedTableParts, queryParameters, dataType);
					AddCodeSnippetReadConditions(interpolatedTableParts, queryParameters, item.DataModel, dataType, item.TableAliasConstant.ToIdentifierName(), metaData, true);
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
								metaData
							)
						);
						match = true;

						AddCodeSnippetReadConditions(interpolatedTableParts, queryParameters, item.DataModel, dataType, item.TableAliasConstant.ToIdentifierName(), metaData, true);
					}
				}
				else
				{
					var dataType = item.GetDataType(referenceDomain, referenceDataModel);
					var parentDataType = parentItem.GetDataType(referenceDomain, referenceDataModel);

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
							metaData
						)
					);
					match = true;

					AddCodeSnippetReadConditions(interpolatedTableParts, queryParameters, item.DataModel, dataType, item.TableAliasConstant.ToIdentifierName(), metaData, true);
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

					if (newItem.DtoProperty is null)
					{
						continue;
					}

					if (!(newItem.DtoProperty?.ReferenceProperty.IsNullOrEmpty() ?? true) && !(item.DtoProperty?.ReferenceProperty.IsNullOrEmpty() ?? true))
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
			MethodMetaData metaData
		)
		{
			var interpolatedTableParts = new List<InterpolatedStringContentSyntax>
			{
				@"
					LEFT JOIN
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
