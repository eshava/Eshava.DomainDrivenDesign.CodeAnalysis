using System.Collections.Generic;
using System.Linq;
using Eshava.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Enums;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Domain;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models
{
	public static class UseCaseAnalysis
	{
		public static UseCasesMap Analyse(
			ApplicationProject project,
			ReferenceMap referenceMap,
			DtoReferenceMap dtoReferenceMap,
			ApplicationUseCases applicationUseCases
		)
		{
			var useCasesMap = new UseCasesMap();
			useCasesMap.AddFeatureNames(DetermineFeatureNameForClassificationKeys(applicationUseCases, referenceMap));

			foreach (var @namespace in applicationUseCases.Namespaces)
			{
				foreach (var useCase in @namespace.UseCases)
				{
					referenceMap.TryGetDomainModel(@namespace.Domain, useCase.GetDomainModelReferenceName(), out var domainModelMap);
					dtoReferenceMap.TryGetDto(@namespace.Domain, useCase.UseCaseName, useCase.ClassificationKey, (useCase.MainDto ?? useCase.Dtos.FirstOrDefault()?.Name), out var dtoMap);

					CreateUseCaseMap(useCasesMap, project.FullQualifiedNamespace, @namespace.Domain, useCase, domainModelMap, dtoMap, referenceMap);

					if (useCase.Type == ApplicationUseCaseType.Search)
					{
						var countUseCase = useCase.ConvertToCountUseCase();
						CreateUseCaseMap(useCasesMap, project.FullQualifiedNamespace, @namespace.Domain, countUseCase, domainModelMap, dtoMap, referenceMap);
					}
				}
			}

			AddQueryProviderMethod(useCasesMap, referenceMap);

			return useCasesMap;
		}

		private static void CreateUseCaseMap(UseCasesMap useCasesMap, string applicationProjectNamespace, string domain, ApplicationUseCase useCase, ReferenceDomainModelMap domainModelMap, ReferenceDtoMap dtoMap, ReferenceMap referenceMap)
		{
			var useCaseNamespace = GetUseCaseNamespace(applicationProjectNamespace, domain, useCase);
			var useCaseMap = new UseCaseMap
			{
				Domain = domain,
				Namespace = useCaseNamespace,
				ReferenceDomainModelMap = domainModelMap,
				ReferenceDtoMap = dtoMap,
				UseCase = useCase
			};

			AddQueryProviderMethod(useCasesMap, useCaseMap, referenceMap);

			useCasesMap.AddUseCase(useCaseMap);
		}

		private static string GetUseCaseNamespace(string applicationProjectNamespace, string domain, ApplicationUseCase useCase)
		{
			var featureNameNamespace = useCase.FeatureName.IsNullOrEmpty() ? "" : $"{useCase.FeatureName}.";
			var useCaseNamespace = $"{applicationProjectNamespace}.{domain}.{featureNameNamespace}{useCase.NamespaceClassificationKey.ToPlural()}.";

			switch (useCase.Type)
			{
				case ApplicationUseCaseType.Create:
				case ApplicationUseCaseType.Update:
				case ApplicationUseCaseType.Delete:
					useCaseNamespace += "Commands";

					break;

				case ApplicationUseCaseType.None:
				case ApplicationUseCaseType.Read:
				case ApplicationUseCaseType.Search:
				case ApplicationUseCaseType.SearchCount:
				case ApplicationUseCaseType.Unique:
				case ApplicationUseCaseType.Suggestions:
				default:
					useCaseNamespace += "Queries";

					break;
			}

			useCaseNamespace += $".{useCase.UseCaseName}";

			return useCaseNamespace;
		}

		private static void AddQueryProviderMethod(UseCasesMap useCasesMap, ReferenceMap domainModelReferenceMap)
		{
			foreach (var domainModelReference in domainModelReferenceMap.GetReferenceDomainModelMaps())
			{
				foreach (var referenceToMe in domainModelReference.ReferencesToMe)
				{
					if (referenceToMe.IsProcessingProperty)
					{
						continue;
					}

					var method = new UseCaseQueryProviderMethodMap
					{
						Domain = referenceToMe.Domain,
						ClassificationKey = referenceToMe.ClassificationKey,
						FeatureName = useCasesMap.GetFeatureName(referenceToMe.Domain, referenceToMe.ClassificationKey),
						Name = referenceToMe.IsUsedMethodName,
						Type = MethodType.IsUsedForeignKey,
						ReturnType = new UseCaseQueryProviderMethodParameterTypeMap
						{
							Type = Eshava.CodeAnalysis.SyntaxConstants.Bool,
						},
						ParameterTypes = [
							new UseCaseQueryProviderMethodParameterTypeMap
							{
								Name = referenceToMe.PropertyName.ToVariableName(),
								Type = domainModelReference.IdentifierType.ToType(),
								ReferenceType = domainModelReference.DataModelName,
								ReferenceDomain = domainModelReference.Domain
							}
						]
					};

					useCasesMap.AddQueryProviderMethod(method);
				}

				if (domainModelReference.IsChildDomainModel)
				{
					AddReadByAggregateIdMethod(useCasesMap, domainModelReference, domainModelReference.AggregateDomainModel);
				}
			}
		}

		private static void AddReadByAggregateIdMethod(UseCasesMap useCasesMap, ReferenceDomainModelMap childDomainModel, ReferenceDomainModelMap aggregateDomainModel)
		{
			var method = new UseCaseQueryProviderMethodMap
			{
				Domain = childDomainModel.Domain,
				ClassificationKey = childDomainModel.ClassificationKey,
				AggregateClassificationKey = aggregateDomainModel.ClassificationKey,
				FeatureName = useCasesMap.GetFeatureName(childDomainModel.Domain, childDomainModel.ClassificationKey),
				Name = $"Read{aggregateDomainModel.ClassificationKey}IdAsync",
				Type = MethodType.ReadAggregateId,
				ReturnType = new UseCaseQueryProviderMethodParameterTypeMap
				{
					Type = aggregateDomainModel.IdentifierType.ToType(),
				},
				ParameterTypes = [
					new UseCaseQueryProviderMethodParameterTypeMap
					{
						Name = $"{childDomainModel.ClassificationKey.ToVariableName()}Id",
						Type = childDomainModel.IdentifierType.ToType()
					}
				]
			};

			useCasesMap.AddQueryProviderMethod(method);

			if (aggregateDomainModel.IsChildDomainModel)
			{
				AddReadByAggregateIdMethod(useCasesMap, childDomainModel, aggregateDomainModel.AggregateDomainModel);
			}
		}

		private static void AddQueryProviderMethod(UseCasesMap useCasesMap, UseCaseMap useCaseMap, ReferenceMap referenceMap)
		{
			if (useCaseMap.UseCase.SkipInfrastructureProviderServiceMethod)
			{
				return;
			}

			if (useCaseMap.ReferenceDomainModelMap is not null)
			{
				AddQueryProviderExistsMethod(useCasesMap, useCaseMap, useCaseMap.ReferenceDomainModelMap.Domain, useCaseMap.ReferenceDomainModelMap.ClassificationKey, useCaseMap.ReferenceDomainModelMap.IdentifierType, false);
			}

			switch (useCaseMap.UseCase.Type)
			{
				case ApplicationUseCaseType.Read:
					AddQueryProviderReadMethod(useCasesMap, useCaseMap);

					break;
				case ApplicationUseCaseType.Suggestions:
				case ApplicationUseCaseType.Search:
					AddQueryProviderSearchMethod(useCasesMap, useCaseMap);

					break;
				case ApplicationUseCaseType.SearchCount:
					AddQueryProviderSearchCountMethod(useCasesMap, useCaseMap);

					break;
				case ApplicationUseCaseType.Create:
				case ApplicationUseCaseType.Update:

					var referencedDomainModelNames = useCaseMap.UseCase.Dtos.Select(dto => dto.ReferenceModelName).ToList();
					foreach (var referencedDomainModelName in referencedDomainModelNames)
					{
						if (!referenceMap.TryGetDomainModel(useCaseMap.Domain, referencedDomainModelName, out var referencedDomainModel))
						{
							continue;
						}

						if (!referencedDomainModel.DomainModel.HasValidationRules)
						{
							continue;
						}

						foreach (var property in referencedDomainModel.DomainModel.Properties)
						{
							foreach (var rule in property.ValidationRules)
							{
								switch (rule.Type)
								{
									case ValidationRuleType.Unique:
										AddQueryProviderUniqueMethod(useCasesMap, useCaseMap, referencedDomainModel, property, rule);

										break;
								}
							}
						}

					}

					AddQueryProviderExistsMethod(useCasesMap, useCaseMap, useCaseMap.ReferenceDtoMap, useCaseMap.ReferenceDomainModelMap);

					foreach (var dtoPropery in useCaseMap.ReferenceDtoMap.ChildReferenceProperties)
					{
						var childDomainModel = useCaseMap.ReferenceDomainModelMap.ChildDomainModels.FirstOrDefault(m => m.DomainModelName == dtoPropery.Dto.DomainModelName);
						if (childDomainModel is null)
						{
							continue;
						}

						AddQueryProviderExistsMethod(useCasesMap, useCaseMap, dtoPropery.Dto, childDomainModel);
					}

					break;

				case ApplicationUseCaseType.Unique:
					AddQueryProviderCheckUniqueMethod(useCasesMap, useCaseMap);

					break;
			}
		}

		private static void AddQueryProviderExistsMethod(UseCasesMap useCasesMap, UseCaseMap useCaseMap, ReferenceDtoMap dtoMap, ReferenceDomainModelMap domainModelMap)
		{
			foreach (var dtoProperty in dtoMap.Dto.Properties)
			{
				var dtoPropertyName = dtoProperty.ReferenceProperty.IsNullOrEmpty()
					? dtoProperty.Name
					: dtoProperty.ReferenceProperty;

				var referenceDomainModel = domainModelMap.ForeignKeyReferences.FirstOrDefault(p => p.PropertyName == dtoPropertyName);
				if (referenceDomainModel is null)
				{
					continue;
				}

				AddQueryProviderExistsMethod(useCasesMap, useCaseMap, referenceDomainModel.Domain, referenceDomainModel.ClassificationKey, referenceDomainModel.DomainModel.IdentifierType);
			}
		}

		private static void AddQueryProviderExistsMethod(UseCasesMap useCasesMap, UseCaseMap useCaseMap, string domain, string classificationKey, string identifierType, bool addToUseCase = true)
		{
			var methodName = "ExistsAsync";

			if (useCasesMap.TryGetMethod(domain, classificationKey, methodName, out var method))
			{
				if (addToUseCase)
				{
					useCaseMap.QueryProviderMethodMaps.Add(method);
				}

				return;
			}

			method = new UseCaseQueryProviderMethodMap
			{
				Domain = domain,
				ClassificationKey = classificationKey,
				FeatureName = useCasesMap.GetFeatureName(domain, classificationKey),
				Name = methodName,
				Type = MethodType.Exists,
				ReturnType = new UseCaseQueryProviderMethodParameterTypeMap
				{
					Type = Eshava.CodeAnalysis.SyntaxConstants.Bool
				},
				ParameterTypes = [
					new UseCaseQueryProviderMethodParameterTypeMap
					{
						Name = $"{classificationKey.ToVariableName()}Id",
						Type = identifierType.ToType()
					}
				]
			};

			if (addToUseCase)
			{
				useCaseMap.QueryProviderMethodMaps.Add(method);
			}
			useCasesMap.AddQueryProviderMethod(method);
		}

		private static void AddQueryProviderReadMethod(UseCasesMap useCasesMap, UseCaseMap useCaseMap)
		{
			var method = new UseCaseQueryProviderMethodMap
			{
				Domain = useCaseMap.Domain,
				ClassificationKey = useCaseMap.ReferenceDtoMap.NamespaceClassificationKey,
				FeatureName = useCaseMap.UseCase.FeatureName,
				Name = useCaseMap.UseCase.UseCaseName + "Async",
				Type = MethodType.Read,
				DataModelTypeProperty = useCaseMap.UseCase.DataModelTypeProperty,
				DataModelTypePropertyValue = useCaseMap.UseCase.DataModelTypePropertyValue,
				UseCustomGroupDtoMethod = useCaseMap.UseCase.UseCustomGroupDtoMethod,
				ReturnType = new UseCaseQueryProviderMethodParameterTypeMap
				{
					Type = useCaseMap.ReferenceDtoMap.DtoName.ToType(),
					TypeUsings = [useCaseMap.Namespace],
					DtoMap = useCaseMap.ReferenceDtoMap
				},
				ParameterTypes = [
					new UseCaseQueryProviderMethodParameterTypeMap
					{
						Name = $"{useCaseMap.ReferenceDtoMap.NamespaceClassificationKey.ToVariableName()}Id",
						Type = useCaseMap.ReferenceDtoMap.DataModel.IdentifierType.ToType()
					}
				]
			};

			useCaseMap.QueryProviderMethodMaps.Add(method);
			useCasesMap.AddQueryProviderMethod(method);
		}

		private static void AddQueryProviderCheckUniqueMethod(UseCasesMap useCasesMap, UseCaseMap useCaseMap)
		{
			var searchMethod = new UseCaseQueryProviderMethodMap
			{
				Domain = useCaseMap.Domain,
				ClassificationKey = useCaseMap.ReferenceDtoMap.NamespaceClassificationKey,
				FeatureName = useCaseMap.UseCase.FeatureName,
				Name = $"{useCaseMap.UseCase.UseCaseName}Async",
				Type = MethodType.Search,
				UseCustomGroupDtoMethod = useCaseMap.UseCase.UseCustomGroupDtoMethod,
				ReturnType = new UseCaseQueryProviderMethodParameterTypeMap
				{
					TypeName = "IEnumerable",
					Generic =
					[
						new UseCaseQueryProviderMethodParameterTypeMap
						{
							Type = useCaseMap.ReferenceDtoMap.DtoName.ToType(),
							TypeUsings = [useCaseMap.Namespace],
							DtoMap = useCaseMap.ReferenceDtoMap
						}
					]
				},
				ParameterTypes = [
					new UseCaseQueryProviderMethodParameterTypeMap
					{
						Name = "searchRequest",
						TypeName = "FilterRequestDto",
						Generic =
						[
							new UseCaseQueryProviderMethodParameterTypeMap
							{
								Type = useCaseMap.ReferenceDtoMap.DtoName.ToType(),
								TypeUsings = [useCaseMap.Namespace],
								DtoMap = useCaseMap.ReferenceDtoMap
							}
						]
					}
				]
			};

			useCaseMap.QueryProviderMethodMaps.Add(searchMethod);
			useCasesMap.AddQueryProviderMethod(searchMethod);

			var searchCountMethod = new UseCaseQueryProviderMethodMap
			{
				Domain = useCaseMap.Domain,
				ClassificationKey = useCaseMap.ReferenceDtoMap.NamespaceClassificationKey,
				FeatureName = useCaseMap.UseCase.FeatureName,
				Name = $"{useCaseMap.UseCase.UseCaseName}CountAsync",
				Type = MethodType.SearchCount,
				ReturnType = new UseCaseQueryProviderMethodParameterTypeMap
				{
					Type = Eshava.CodeAnalysis.SyntaxConstants.Int
				},
				ParameterTypes = [
					new UseCaseQueryProviderMethodParameterTypeMap
					{
						Name = "searchRequest",
						TypeName = "FilterRequestDto",
						Generic =
						[
							new UseCaseQueryProviderMethodParameterTypeMap
							{
								Type = useCaseMap.ReferenceDtoMap.DtoName.ToType(),
								TypeUsings = [useCaseMap.Namespace],
								DtoMap = useCaseMap.ReferenceDtoMap
							}
						]
					}
				]
			};

			useCaseMap.QueryProviderMethodMaps.Add(searchCountMethod);
			useCasesMap.AddQueryProviderMethod(searchCountMethod);
		}


		private static void AddQueryProviderSearchMethod(UseCasesMap useCasesMap, UseCaseMap useCaseMap)
		{
			var method = new UseCaseQueryProviderMethodMap
			{
				Domain = useCaseMap.Domain,
				ClassificationKey = useCaseMap.ReferenceDtoMap.NamespaceClassificationKey,
				FeatureName = useCaseMap.UseCase.FeatureName,
				Name = useCaseMap.UseCase.UseCaseName + "Async",
				Type = MethodType.Search,
				UseCustomGroupDtoMethod = useCaseMap.UseCase.UseCustomGroupDtoMethod,
				ReturnType = new UseCaseQueryProviderMethodParameterTypeMap
				{
					TypeName = "IEnumerable",
					Generic =
					[
						new UseCaseQueryProviderMethodParameterTypeMap
						{
							Type = useCaseMap.ReferenceDtoMap.DtoName.ToType(),
							TypeUsings = [useCaseMap.Namespace],
							DtoMap = useCaseMap.ReferenceDtoMap
						}
					]
				},
				ParameterTypes = [
					new UseCaseQueryProviderMethodParameterTypeMap
					{
						Name = "searchRequest",
						TypeName = "FilterRequestDto",
						Generic =
						[
							new UseCaseQueryProviderMethodParameterTypeMap
							{
								Type = useCaseMap.ReferenceDtoMap.DtoName.ToType(),
								TypeUsings = [useCaseMap.Namespace],
								DtoMap = useCaseMap.ReferenceDtoMap
							}
						]
					}
				]
			};

			useCaseMap.QueryProviderMethodMaps.Add(method);
			useCasesMap.AddQueryProviderMethod(method);
		}

		private static void AddQueryProviderSearchCountMethod(UseCasesMap useCasesMap, UseCaseMap useCaseMap)
		{
			var method = new UseCaseQueryProviderMethodMap
			{
				Domain = useCaseMap.Domain,
				ClassificationKey = useCaseMap.ReferenceDtoMap.NamespaceClassificationKey,
				FeatureName = useCaseMap.UseCase.FeatureName,
				Name = useCaseMap.UseCase.UseCaseName + "Async",
				Type = MethodType.SearchCount,
				ReturnType = new UseCaseQueryProviderMethodParameterTypeMap
				{
					Type = Eshava.CodeAnalysis.SyntaxConstants.Int
				},
				ParameterTypes = [
					new UseCaseQueryProviderMethodParameterTypeMap
					{
						Name = "searchRequest",
						TypeName = "FilterRequestDto",
						Generic =
						[
							new UseCaseQueryProviderMethodParameterTypeMap
							{
								Type = useCaseMap.ReferenceDtoMap.DtoName.ToType(),
								TypeUsings = [useCaseMap.Namespace],
								DtoMap = useCaseMap.ReferenceDtoMap
							}
						]
					}
				]
			};

			useCaseMap.QueryProviderMethodMaps.Add(method);
			useCasesMap.AddQueryProviderMethod(method);
		}

		private static void AddQueryProviderUniqueMethod(UseCasesMap useCasesMap, UseCaseMap useCaseMap, ReferenceDomainModelMap domainModelMap, DomainModelPropery property, DomainModelProperyValidationRule rule)
		{
			var methodName = $"IsUnique{property.Name}Async";

			if (useCasesMap.TryGetMethod(useCaseMap.Domain, domainModelMap.ClassificationKey, methodName, out var method))
			{
				useCaseMap.QueryProviderMethodMaps.Add(method);

				return;
			}

			method = new UseCaseQueryProviderMethodMap
			{
				Domain = useCaseMap.Domain,
				ClassificationKey = domainModelMap.ClassificationKey,
				FeatureName = useCaseMap.UseCase.FeatureName,
				Name = methodName,
				Type = MethodType.IsUnique,
				ReturnType = new UseCaseQueryProviderMethodParameterTypeMap
				{
					Type = Eshava.CodeAnalysis.SyntaxConstants.Bool
				},
				ParameterTypes = [
					new UseCaseQueryProviderMethodParameterTypeMap
					{
						Name = $"{domainModelMap.ClassificationKey.ToVariableName()}Id",
						Type = domainModelMap.IdentifierType.ToType().AsNullable()
					},
					new UseCaseQueryProviderMethodParameterTypeMap
					{
						Name = $"{property.Name.ToVariableName()}",
						DataModelPropertyName = property.DataModelPropertyName,
						Type = property.Type.ToType(),
						TypeUsings = property.UsingForType.IsNullOrEmpty()
							? []
							: [property.UsingForType]
					}
				]
			};

			if (rule.RelatedProperties.Count > 0)
			{
				method.ParameterTypes.AddRange(rule.RelatedProperties.Select(p =>
				{
					var relatedProperty = domainModelMap.DomainModel.Properties.First(dmp => dmp.Name == p);

					return new UseCaseQueryProviderMethodParameterTypeMap
					{
						Name = $"{p.ToVariableName()}",
						DataModelPropertyName = relatedProperty.DataModelPropertyName,
						Type = relatedProperty.Type.ToType(),
						TypeUsings = relatedProperty.UsingForType.IsNullOrEmpty()
							? []
							: [relatedProperty.UsingForType]
					};
				}));
			}

			useCaseMap.QueryProviderMethodMaps.Add(method);
			useCasesMap.AddQueryProviderMethod(method);
		}

		private static HashSet<ApplicationUseCaseType> _domainModelUseCaseTypes = [ApplicationUseCaseType.Create, ApplicationUseCaseType.Update, ApplicationUseCaseType.Delete];

		private static Dictionary<string, Dictionary<string, string>> DetermineFeatureNameForClassificationKeys(ApplicationUseCases applicationUseCases, ReferenceMap referenceMap)
		{
			var featureNames = new Dictionary<string, Dictionary<string, string>>();

			foreach (var @namespace in applicationUseCases.Namespaces)
			{
				if (!featureNames.TryGetValue(@namespace.Domain, out var domainFeatureNames))
				{
					domainFeatureNames = new Dictionary<string, string>();
					featureNames.Add(@namespace.Domain, domainFeatureNames);
				}

				foreach (var useCase in @namespace.UseCases.Where(uc => !uc.FeatureName.IsNullOrEmpty()))
				{
					if (!domainFeatureNames.ContainsKey(useCase.ClassificationKey))
					{
						domainFeatureNames.Add(useCase.ClassificationKey, useCase.FeatureName);
					}

					if (!_domainModelUseCaseTypes.Contains(useCase.Type))
					{
						continue;
					}

					if (useCase.Type == ApplicationUseCaseType.Delete)
					{
						if (!referenceMap.TryGetDomainModel(@namespace.Domain, useCase.DomainModelReference, out var domainModel))
						{
							continue;
						}

						if (!domainFeatureNames.ContainsKey(domainModel.ClassificationKey))
						{
							domainFeatureNames.Add(domainModel.ClassificationKey, useCase.FeatureName);
						}

						continue;
					}

					foreach (var dto in useCase.Dtos)
					{
						if (!referenceMap.TryGetDomainModel(@namespace.Domain, dto.ReferenceModelName, out var domainModel))
						{
							continue;
						}

						if (!domainFeatureNames.ContainsKey(domainModel.ClassificationKey))
						{
							domainFeatureNames.Add(domainModel.ClassificationKey, useCase.FeatureName);
						}
					}
				}
			}

			return featureNames;
		}
	}


	public class UseCasesMap
	{
		/// <summary>
		/// Domain -> ClassificationKey -> MethodName
		/// </summary>
		private readonly Dictionary<string, Dictionary<string, Dictionary<string, UseCaseQueryProviderMethodMap>>> _queryProviderMethods = [];
		/// <summary>
		/// Domain -> ClassificationKey -> UseCaseName
		/// </summary>
		private readonly Dictionary<string, Dictionary<string, Dictionary<string, List<UseCaseMap>>>> _useCases = [];
		/// <summary>
		/// Domain -> ClassificationKey
		/// </summary>
		private Dictionary<string, Dictionary<string, string>> _featureNames = [];

		public void AddQueryProviderMethod(UseCaseQueryProviderMethodMap method)
		{
			if (!_queryProviderMethods.ContainsKey(method.Domain))
			{
				_queryProviderMethods.Add(method.Domain, new Dictionary<string, Dictionary<string, UseCaseQueryProviderMethodMap>>());
			}

			if (!_queryProviderMethods[method.Domain].ContainsKey(method.ClassificationKey))
			{
				_queryProviderMethods[method.Domain].Add(method.ClassificationKey, new Dictionary<string, UseCaseQueryProviderMethodMap>());
			}

			if (!_queryProviderMethods[method.Domain][method.ClassificationKey].ContainsKey(method.Name))
			{
				_queryProviderMethods[method.Domain][method.ClassificationKey].Add(method.Name, method);
			}
		}

		public void AddUseCase(UseCaseMap useCase)
		{
			if (!_useCases.ContainsKey(useCase.Domain))
			{
				_useCases.Add(useCase.Domain, new Dictionary<string, Dictionary<string, List<UseCaseMap>>>());
			}

			if (!_useCases[useCase.Domain].ContainsKey(useCase.UseCase.NamespaceClassificationKey))
			{
				_useCases[useCase.Domain].Add(useCase.UseCase.NamespaceClassificationKey, new Dictionary<string, List<UseCaseMap>>());
			}

			if (!_useCases[useCase.Domain][useCase.UseCase.NamespaceClassificationKey].ContainsKey(useCase.UseCase.UseCaseName))
			{
				_useCases[useCase.Domain][useCase.UseCase.NamespaceClassificationKey].Add(useCase.UseCase.UseCaseName, new List<UseCaseMap>());
			}

			_useCases[useCase.Domain][useCase.UseCase.NamespaceClassificationKey][useCase.UseCase.UseCaseName].Add(useCase);
		}

		public void AddFeatureNames(Dictionary<string, Dictionary<string, string>> featureNames)
		{
			_featureNames = featureNames;
		}

		public bool TryGetMethod(string domain, string domainModelName, string methodName, out UseCaseQueryProviderMethodMap methodMap)
		{
			if (!_queryProviderMethods.ContainsKey(domain)
				|| !_queryProviderMethods[domain].ContainsKey(domainModelName)
				|| !_queryProviderMethods[domain][domainModelName].ContainsKey(methodName)
			)
			{
				methodMap = null;

				return false;
			}

			methodMap = _queryProviderMethods[domain][domainModelName][methodName];

			return true;
		}

		public bool TryGetQueryProvider(string domain, string classificationKey, out QueryProviderMap queryProviderMap)
		{
			if (!_queryProviderMethods.ContainsKey(domain)
				|| !_queryProviderMethods[domain].ContainsKey(classificationKey)
			)
			{
				queryProviderMap = null;

				return false;
			}

			queryProviderMap = new QueryProviderMap
			{
				Domain = domain,
				ClassificationKey = classificationKey,
				FeatureName = _queryProviderMethods[domain][classificationKey].Values.FirstOrDefault()?.FeatureName,
				Methods = _queryProviderMethods[domain][classificationKey].Values
			};

			return true;
		}

		public bool TryGetUseCase(string domain, string classificationKey, string useCaseName, string referenceModel, out UseCaseMap useCaseMap)
		{
			if (!_useCases.ContainsKey(domain)
				|| !_useCases[domain].ContainsKey(classificationKey)
				|| !_useCases[domain][classificationKey].ContainsKey(useCaseName)
			)
			{
				useCaseMap = null;

				return false;
			}

			var maps = _useCases[domain][classificationKey][useCaseName];
			if (referenceModel.IsNullOrEmpty())
			{
				useCaseMap = maps.FirstOrDefault();

				return true;
			}

			useCaseMap = maps.FirstOrDefault(map => MatchUseCase(map, referenceModel));

			return useCaseMap is not null;
		}

		public string GetFeatureName(string domain, string classificationKey)
		{
			if (!_featureNames.TryGetValue(domain, out var domainFeatureNames))
			{
				return null;
			}

			if (!domainFeatureNames.TryGetValue(classificationKey, out var featureName))
			{
				return null;
			}

			return featureName;
		}

		public IEnumerable<QueryProviderMap> GetUseCaseQueryProviderMethodMaps()
		{
			return _queryProviderMethods
				.SelectMany(domainCollection =>
					domainCollection.Value.Select(modelCollection => new QueryProviderMap
					{
						Domain = domainCollection.Key,
						ClassificationKey = modelCollection.Key,
						FeatureName = modelCollection.Value.Select(xx => xx.Value).FirstOrDefault()?.FeatureName,
						Methods = modelCollection.Value.Select(xx => xx.Value).ToList()
					})
				)
				.ToList();
		}

		public IEnumerable<UseCaseMap> GetUseCaseMaps()
		{
			foreach (var domain in _useCases)
			{
				foreach (var namespaceClassificationKey in domain.Value)
				{
					foreach (var useCase in namespaceClassificationKey.Value)
					{
						foreach (var userCaseItem in useCase.Value)
						{
							yield return userCaseItem;
						}
					}
				}
			}
		}

		private static bool MatchUseCase(UseCaseMap useCaseMap, string referenceModel)
		{
			switch (useCaseMap.UseCase.Type)
			{
				case ApplicationUseCaseType.Create:
				case ApplicationUseCaseType.Update:
				case ApplicationUseCaseType.Delete:

					return useCaseMap.ReferenceDomainModelMap.DomainModelName == referenceModel;
			}

			return useCaseMap.ReferenceDtoMap.DataModelName == referenceModel;
		}
	}

	public class UseCaseMap
	{
		public UseCaseMap()
		{
			QueryProviderMethodMaps = new List<UseCaseQueryProviderMethodMap>();
		}

		public string Domain { get; set; }
		public string Namespace { get; set; }
		public ApplicationUseCase UseCase { get; set; }

		public ReferenceDomainModelMap ReferenceDomainModelMap { get; set; }
		public ReferenceDtoMap ReferenceDtoMap { get; set; }

		public List<UseCaseQueryProviderMethodMap> QueryProviderMethodMaps { get; set; }
	}

	public class UseCaseQueryProviderMethodMap
	{
		public UseCaseQueryProviderMethodMap()
		{
			ParameterTypes = new List<UseCaseQueryProviderMethodParameterTypeMap>();
		}

		public string Domain { get; set; }
		public string ClassificationKey { get; set; }
		public string AggregateClassificationKey { get; set; }

		/// <summary>
		/// If set, overrides the <see cref="ClassificationKey"/>
		/// </summary>
		public string FeatureName { get; set; }

		/// <summary>
		/// Method name incl. Async postfix
		/// </summary>
		public string Name { get; set; }
		public MethodType Type { get; set; }

		/// <summary>
		/// Property that only exists in Data Model and defines the type identifier property for the domain model
		/// </summary>
		public string DataModelTypeProperty { get; set; }
		/// <summary>
		/// Value of <see cref="DataModelTypeProperty"/>
		/// </summary>
		public string DataModelTypePropertyValue { get; set; }

		public bool UseCustomGroupDtoMethod { get; set; }
		public UseCaseQueryProviderMethodParameterTypeMap ReturnType { get; set; }
		public List<UseCaseQueryProviderMethodParameterTypeMap> ParameterTypes { get; set; }
	}

	public class UseCaseQueryProviderMethodParameterTypeMap
	{
		public UseCaseQueryProviderMethodParameterTypeMap()
		{
			TypeUsings = new List<string>();
		}

		/// <summary>
		/// Parameter name
		/// </summary>
		public string Name { get; set; }
		public string DataModelPropertyName { get; set; }

		public ReferenceDtoMap DtoMap { get; set; }
		public TypeSyntax Type { get; set; }

		/// <summary>
		/// If <see cref="Type"/> is null, <see cref="TypeName"/> will be used in combination with <see cref="Generic"/>
		/// </summary>
		public string TypeName { get; set; }
		public List<string> TypeUsings { get; set; }
		public UseCaseQueryProviderMethodParameterTypeMap[] Generic { get; set; }
		public string ReferenceType { get; set; }
		public string ReferenceDomain { get; set; }

		public string PropertyName
		{
			get
			{
				if (!DataModelPropertyName.IsNullOrEmpty())
				{
					return DataModelPropertyName;
				}

				return Name.ToPropertyName();
			}
		}

		public TypeSyntax GetParameterType()
		{
			return Map(this);
		}

		public TypeSyntax GetReturnParameterType()
		{
			var returnType = Map(this);

			return "Task"
				.AsGeneric(
					"ResponseData"
					.AsGeneric(returnType)
				);
		}

		public IEnumerable<string> CollectUsings()
		{
			if (Generic is null || Generic.Length == 0)
			{
				return TypeUsings;
			}

			return TypeUsings.Concat(Generic.SelectMany(g => g.CollectUsings())).ToList();
		}

		private static TypeSyntax Map(UseCaseQueryProviderMethodParameterTypeMap typeDefinition)
		{
			if (typeDefinition.Type is not null
				|| typeDefinition.Generic is null
				|| typeDefinition.Generic.Length == 0
			)
			{
				return typeDefinition.Type;
			}

			var genericTypes = typeDefinition.Generic.Select(Map).ToList();

			return typeDefinition.TypeName.AsGeneric(genericTypes.ToArray());
		}
	}

	public class QueryProviderMap
	{
		public string Domain { get; set; }
		public string ClassificationKey { get; set; }
		/// <summary>
		/// If set, overrides the <see cref="ClassificationKey"/>
		/// </summary>
		public string FeatureName { get; set; }
		public IEnumerable<UseCaseQueryProviderMethodMap> Methods { get; set; }
	}
}
