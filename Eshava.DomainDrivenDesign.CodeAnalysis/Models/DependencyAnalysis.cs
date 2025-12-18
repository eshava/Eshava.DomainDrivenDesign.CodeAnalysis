using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Eshava.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Domain;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models
{
	public static class DependencyAnalysis
	{
		// who is referencing me?
		// who am I referencing?

		public static ReferenceMap Analyse(DomainModels domainModels, InfrastructureModels infrastructureModels)
		{
			var map = new ReferenceMap();

			var domainDomains = domainModels is null
				? new Dictionary<string, Dictionary<string, (string ClassificationKey, string FeatureName, string DataModelName)>>()
				: domainModels.Namespaces
				.ToDictionary(
					d => d.Domain,
					d => d.Models.ToDictionary(m => m.Name, m => (m.ClassificationKey, m.FeatureName, m.DataModelName))
				);

			var referencesToMe = new Dictionary<string, Dictionary<string, List<ReferenceDomainModel>>>();
			var foreignKeyReferences = new List<ReferenceDomainModel>();
			var aggregates = new Dictionary<string, List<ReferenceDomainModelMap>>();

			foreach (var @namespace in domainModels.Namespaces)
			{
				foreach (var enumeration in @namespace.Enumerations)
				{
					var enumerationMap = new ReferenceEnumMap
					{
						Domain = @namespace.Domain,
						Name = enumeration.Name,
						Namespace = $"{@namespace.Domain}.{DomainModel.GetNamespaceDirectory(enumeration.FeatureName, enumeration.ClassificationKey)}",
						Enumeration = enumeration
					};

					map.AddEnumeration(enumerationMap);
				}

				foreach (var domainModel in @namespace.Models)
				{
					var domainModelMap = new ReferenceDomainModelMap
					{
						Domain = @namespace.Domain,
						DomainModelName = domainModel.Name,
						ClassificationKey = domainModel.ClassificationKey,
						FeatureName = domainModel.FeatureName,
						IdentifierType = domainModel.IdentifierType,
						DataModelName = domainModel.DataModelName,
						DomainModel = domainModel
					};

					map.AddDomainModel(domainModelMap);

					if (domainModel.IsAggregate)
					{
						if (!aggregates.ContainsKey(@namespace.Domain))
						{
							aggregates.Add(@namespace.Domain, new List<ReferenceDomainModelMap>());
						}

						aggregates[@namespace.Domain].Add(domainModelMap);
					}

					foreach (var property in domainModel.Properties.Where(p => p.HasValidReference))
					{
						var referenceDomain = property.ReferenceDomain.IsNullOrEmpty()
							? @namespace.Domain
							: property.ReferenceDomain;

						if (!domainDomains.ContainsKey(referenceDomain)
							|| !domainDomains[referenceDomain].ContainsKey(property.ReferenceType))
						{
							continue;
						}

						var foreignKeyReference = new ReferenceDomainModel
						{
							PropertyName = property.Name,
							IsProcessingProperty = property.IsProcessingProperty,
							Domain = referenceDomain,
							DomainModelName = property.ReferenceType,
							DataModelName = domainDomains[referenceDomain][property.ReferenceType].DataModelName,
							ClassificationKey = domainDomains[referenceDomain][property.ReferenceType].ClassificationKey,
							FeatureName = domainDomains[referenceDomain][property.ReferenceType].FeatureName
						};

						domainModelMap.ForeignKeyReferences.Add(foreignKeyReference);
						foreignKeyReferences.Add(foreignKeyReference);

						if (!referencesToMe.ContainsKey(referenceDomain))
						{
							referencesToMe.Add(referenceDomain, new Dictionary<string, List<ReferenceDomainModel>>());
						}

						if (!referencesToMe[referenceDomain].ContainsKey(property.ReferenceType))
						{
							referencesToMe[referenceDomain].Add(property.ReferenceType, new List<ReferenceDomainModel>());
						}

						referencesToMe[referenceDomain][property.ReferenceType].Add(new ReferenceDomainModel
						{
							Domain = domainModelMap.Domain,
							DomainModelName = domainModelMap.DomainModelName,
							ClassificationKey = domainModelMap.ClassificationKey,
							FeatureName = domainModelMap.FeatureName,
							DomainModel = domainModelMap.DomainModel,
							DataModelName = domainModelMap.DataModelName,
							PropertyName = property.Name,
							IsProcessingProperty = property.IsProcessingProperty
						});
					}
				}
			}

			foreach (var referencesToMeDomain in referencesToMe)
			{
				foreach (var referencesToMeModel in referencesToMeDomain.Value)
				{
					if (!map.TryGetDomainModel(referencesToMeDomain.Key, referencesToMeModel.Key, out var domainModel))
					{
						continue;
					}

					domainModel.ReferencesToMe = referencesToMeModel.Value;
				}
			}

			foreach (var foreignKeyReference in foreignKeyReferences)
			{
				if (!map.TryGetDomainModel(foreignKeyReference.Domain, foreignKeyReference.DomainModelName, out var domainModel))
				{
					continue;
				}

				foreignKeyReference.DomainModel = domainModel.DomainModel;
			}

			foreach (var domainAggregates in aggregates)
			{
				foreach (var aggregate in domainAggregates.Value)
				{
					foreach (var childModelName in aggregate.DomainModel.ChildDomainModels)
					{
						if (!map.TryGetDomainModel(domainAggregates.Key, childModelName, out var childDomainModel))
						{
							continue;
						}

						childDomainModel.AggregateDomainModel = aggregate;
						aggregate.ChildDomainModels.Add(childDomainModel);
					}
				}
			}

			if (infrastructureModels is not null)
			{
				foreach (var @namespace in infrastructureModels.Namespaces)
				{
					foreach (var dataModel in @namespace.Models)
					{
						var propertiesWithExtenalReferences = dataModel.Properties
							.Where(p => p.IsReference && !p.ReferenceType.IsNullOrEmpty() && !p.ReferenceDomain.IsNullOrEmpty() && p.ReferenceDomain != @namespace.Domain)
							.ToList();

						if (propertiesWithExtenalReferences.Count == 0)
						{
							continue;
						}

						foreach (var property in propertiesWithExtenalReferences)
						{
							if (!map.TryGetDomainModelByDataModel(property.ReferenceDomain, property.ReferenceType, out var referencedDomainModels))
							{
								continue;
							}

							foreach (var referencedDomainModel in referencedDomainModels)
							{
								referencedDomainModel.ReferencesToMe.Add(new ReferenceDomainModel
								{
									DataModelName = dataModel.Name,
									Domain = @namespace.Domain,
									DomainModel = null,
									DomainModelName = null,
									ClassificationKey = dataModel.ClassificationKey,
									FeatureName = null,
									PropertyName = property.Name,
									IsProcessingProperty = false
								});
							}
						}
					}
				}
			}

			return map;
		}
	}

	public class ReferenceMap
	{
		/// <summary>
		/// Domain -> DomainModelName -> DomainModelMap
		/// </summary>
		private readonly Dictionary<string, Dictionary<string, ReferenceDomainModelMap>> _references = [];
		/// <summary>
		/// Domain -> EnumerationName -> Enumeration
		/// </summary>
		private readonly Dictionary<string, Dictionary<string, ReferenceEnumMap>> _enumerationReferences = [];

		public void AddDomainModel(ReferenceDomainModelMap domainModelMap)
		{
			if (!_references.ContainsKey(domainModelMap.Domain))
			{
				_references.Add(domainModelMap.Domain, new Dictionary<string, ReferenceDomainModelMap>());
			}

			_references[domainModelMap.Domain].Add(domainModelMap.DomainModelName, domainModelMap);
		}

		public void AddEnumeration(ReferenceEnumMap referenceEnumMap)
		{
			if (!_enumerationReferences.ContainsKey(referenceEnumMap.Domain))
			{
				_enumerationReferences.Add(referenceEnumMap.Domain, new Dictionary<string, ReferenceEnumMap>());
			}

			_enumerationReferences[referenceEnumMap.Domain].Add(referenceEnumMap.Name, referenceEnumMap);
		}

		public bool TryGetDomainModel(string domain, string name, out ReferenceDomainModelMap domainModelMap)
		{
			if (domain.IsNullOrEmpty() || name.IsNullOrEmpty())
			{
				domainModelMap = null;

				return false;
			}

			if (!_references.ContainsKey(domain) || !_references[domain].ContainsKey(name))
			{
				domainModelMap = null;

				return false;
			}

			domainModelMap = _references[domain][name];

			return true;
		}

		public bool TryGetDomainModelByDataModel(string domain, string dateModelName, out IEnumerable<ReferenceDomainModelMap> domainModelMaps)
		{
			if (domain.IsNullOrEmpty() || dateModelName.IsNullOrEmpty())
			{
				domainModelMaps = Array.Empty<ReferenceDomainModelMap>();

				return false;
			}

			if (!_references.ContainsKey(domain))
			{
				domainModelMaps = Array.Empty<ReferenceDomainModelMap>();

				return false;
			}

			domainModelMaps = _references[domain].Values.Where(m => m.DataModelName == dateModelName).ToList();

			return domainModelMaps.Any();
		}

		public bool TryGetEnumeration(string domain, string name, out ReferenceEnumMap enumMap)
		{
			if (domain.IsNullOrEmpty() || name.IsNullOrEmpty())
			{
				enumMap = null;

				return false;
			}

			if (!_enumerationReferences.ContainsKey(domain) || !_enumerationReferences[domain].ContainsKey(name))
			{
				enumMap = null;

				return false;
			}

			enumMap = _enumerationReferences[domain][name];

			return true;
		}

		public IEnumerable<ReferenceDomainModelMap> GetReferenceDomainModelMaps()
		{
			foreach (var domain in _references)
			{
				foreach (var model in domain.Value)
				{
					yield return model.Value;
				}
			}
		}

		public IEnumerable<ReferenceEnumMap> GetReferenceEnumerationMaps()
		{
			foreach (var domain in _enumerationReferences)
			{
				foreach (var enumeration in domain.Value)
				{
					yield return enumeration.Value;
				}
			}
		}
	}

	public class ReferenceDomainModelMap : AbstractReferenceDomainModel
	{
		public delegate string GetFeatureName(string domain, string classificationKey);

		public ReferenceDomainModelMap()
		{
			ReferencesToMe = new List<ReferenceDomainModel>();
			ForeignKeyReferences = new List<ReferenceDomainModel>();
			ChildDomainModels = new List<ReferenceDomainModelMap>();
		}

		/// <summary>
		/// Type of primary key
		/// </summary>
		public string IdentifierType { get; set; }

		public List<ReferenceDomainModel> ReferencesToMe { get; set; }
		public List<ReferenceDomainModel> ForeignKeyReferences { get; set; }
		public ReferenceDomainModelMap AggregateDomainModel { get; set; }
		public List<ReferenceDomainModelMap> ChildDomainModels { get; set; }

		public bool IsChildDomainModel => AggregateDomainModel is not null;
		public bool IsAggregate => DomainModel?.IsAggregate ?? false;
		public bool IsValueObject => DomainModel?.IsValueObject ?? false;
		public bool HasReferencesToMe
		{
			get
			{
				if (ReferencesToMe.Count > 0)
				{
					return true;
				}

				if (!IsAggregate || ChildDomainModels.Count == 0)
				{
					return false;
				}

				return ChildDomainModels.Any(cdm => cdm.ReferencesToMe.Count > 0);
			}
		}

		public IEnumerable<(string Using, string Name, IdentifierNameSyntax Type)> GetRepositories(string infrastructureProjectNamespace)
		{
			var repositories = new List<(string Using, string Name, IdentifierNameSyntax Type)>();

			var repositoryUsing = $"{infrastructureProjectNamespace}.{Domain}.{ClassificationKey.ToPlural()}";
			var repositoryType = DomainModelName.ToRepositoryType();
			var repositoryName = DomainModelName.ToRepositoryName();

			repositories.Add((repositoryUsing, repositoryName, repositoryType));

			foreach (var childDomainModel in ChildDomainModels)
			{
				repositories.AddRange(childDomainModel.GetRepositories(infrastructureProjectNamespace));
			}

			return repositories;
		}

		public (string Using, string Name, IdentifierNameSyntax Type) GetProvider(string applicationProjectNamespace, string defaultFeatureName, GetFeatureName getFeatureName)
		{
			if (IsChildDomainModel)
			{
				return AggregateDomainModel.GetProvider(applicationProjectNamespace, defaultFeatureName, getFeatureName);
			}

			var featureName = getFeatureName(Domain, ClassificationKey) ?? defaultFeatureName;

			var providerUsing = ClassificationKey.GetCommandsNamespace(Domain, featureName, applicationProjectNamespace);
			var providerType = DomainModelName.ToProviderType();
			var providerName = DomainModelName.ToProviderName();

			return (providerUsing, providerName, providerType);
		}

		public (string Using, string Name, IdentifierNameSyntax Type) GetQueryProvider(string applicationProjectNamespace, string defaultFeatureName, GetFeatureName getFeatureName)
		{
			var featureName = getFeatureName(Domain, ClassificationKey) ?? defaultFeatureName;

			var queryProviderUsing = ClassificationKey.GetQueriesNamespace(Domain, featureName, applicationProjectNamespace);
			var queryProviderType = ClassificationKey.ToQueryProviderType();
			var queryProviderName = ClassificationKey.ToQueryProviderName();

			return (queryProviderUsing, queryProviderName, queryProviderType);
		}

		public IEnumerable<(string Using, string Name, IdentifierNameSyntax Type)> GetQueryProviders(ImmutableHashSet<string> relevantDomainModelNames, string applicationProjectNamespace, string defaultFeatureName, GetFeatureName getFeatureName)
		{
			var queryProviders = new List<(string Using, string Name, IdentifierNameSyntax Type)>();
			if (DomainModel.HasValidationRules)
			{
				queryProviders.Add(GetQueryProvider(applicationProjectNamespace, defaultFeatureName, getFeatureName));
			}

			if (IsAggregate)
			{
				foreach (var childDomainModel in ChildDomainModels)
				{
					if (!relevantDomainModelNames.Contains(childDomainModel.DomainModelName))
					{
						continue;
					}

					queryProviders.AddRange(childDomainModel.GetQueryProviders(relevantDomainModelNames, applicationProjectNamespace, defaultFeatureName, getFeatureName));
				}
			}

			return queryProviders;
		}

		public ReferenceDomainModelMap GetTopLevelDomainModel()
		{
			if (!IsChildDomainModel)
			{
				return this;
			}

			return AggregateDomainModel.GetTopLevelDomainModel();
		}
	}

	public abstract class AbstractReferenceDomainModel
	{
		/// <summary>
		/// DDD domain name
		/// </summary>
		public string Domain { get; set; }

		/// <summary>
		/// Name of the domain model
		/// </summary>
		public string DomainModelName { get; set; }

		/// <summary>
		/// Name of the associated data model
		/// </summary>
		public string DataModelName { get; set; }

		/// <summary>
		/// Name to wrap all data model, domain model and dtos
		/// </summary>
		public string ClassificationKey { get; set; }

		/// <summary>
		/// If set, overrides the <see cref="ClassificationKey"/>
		/// </summary>
		public string FeatureName { get; set; }

		/// <summary>
		/// DDD domain model definition
		/// </summary>
		public DomainModel DomainModel { get; set; }

		public string ChildEnumerableName
		{
			get
			{
				return DomainModelName;
			}
		}

		public string GetDomainModelTypeName(string domainProjectNamespace)
		{
			return DomainModelName.GetDomainModelTypeName(Domain, ClassificationKey, FeatureName, domainProjectNamespace);
		}
	}

	public class ReferenceDomainModel : AbstractReferenceDomainModel
	{
		/// <summary>
		/// Name of the property who is references to the domain model
		/// </summary>
		public string PropertyName { get; set; }
		public bool IsProcessingProperty { get; set; }
		public string IsUsedMethodName => $"IsUsed{PropertyName}Async";
	}

	public class ReferenceEnumMap
	{
		public string Domain { get; set; }
		public string Name { get; set; }
		public string Namespace { get; set; }

		public DomainModelEnumeration Enumeration { get; set; }
	}
}
