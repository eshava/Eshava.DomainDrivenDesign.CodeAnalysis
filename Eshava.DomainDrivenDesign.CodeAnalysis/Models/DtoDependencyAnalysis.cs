using System.Collections.Generic;
using System.Linq;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models
{
	public static class DtoDependencyAnalysis
	{
		public static DtoReferenceMap Analyse(
			ApplicationUseCases applicationUseCases,
			ReferenceMap domainModelReferenceMap,
			InfrastructureModels infrastructureModels
		)
		{
			var map = new DtoReferenceMap();

			var infrastructureDomains = infrastructureModels is null
				? new Dictionary<string, Dictionary<string, InfrastructureModel>>()
				: infrastructureModels.Namespaces
					.ToDictionary(
						d => d.Domain,
						d => d.Models.ToDictionary(m => m.Name, m => m)
					);

			foreach (var @namespace in applicationUseCases.Namespaces)
			{
				foreach (var useCase in @namespace.UseCases)
				{
					if (useCase.Type == ApplicationUseCaseType.SearchCount)
					{
						continue;
					}

					if (useCase.Type == ApplicationUseCaseType.Create
						|| useCase.Type == ApplicationUseCaseType.Update
						|| useCase.Type == ApplicationUseCaseType.Delete
					)
					{
						if (domainModelReferenceMap is null)
						{
							continue;
						}

						var domainModelReferenceName = useCase.GetDomainModelReferenceName();

						if (!domainModelReferenceMap.TryGetDomainModel(@namespace.Domain, domainModelReferenceName, out var domainModelMap))
						{
							throw new System.ArgumentException($"Domain model {@namespace.Domain}.{domainModelReferenceName} not found");
						}

						if (domainModelMap.IsChildDomainModel)
						{
							if (!useCase.UseCaseName.Contains(domainModelMap.DomainModelName))
							{
								useCase.UseCaseName += domainModelMap.DomainModelName;
							}

							useCase.NamespaceClassificationKey = domainModelMap.AggregateDomainModel.ClassificationKey;
							useCase.NamespaceDomainModelReference = domainModelMap.AggregateDomainModel.DomainModelName;
						}
						else
						{
							useCase.NamespaceDomainModelReference = domainModelMap.DomainModelName;
						}
					}

					if (useCase.NamespaceClassificationKey.IsNullOrEmpty())
					{
						useCase.NamespaceClassificationKey = useCase.ClassificationKey;
					}

					var dtoNameCache = new Dictionary<string, ReferenceDtoMap>();
					var dtoCache = new Dictionary<string, ReferenceDtoMap>();

					foreach (var dtoDefinition in useCase.Dtos)
					{
						var dtoMap = new ReferenceDtoMap
						{
							Domain = @namespace.Domain,
							UseCase = useCase.UseCaseName,
							NamespaceClassificationKey = useCase.NamespaceClassificationKey,
							DtoName = GetDtoName(useCase, dtoDefinition),
							Dto = dtoDefinition
						};

						var referenceModelName = GetModelName(useCase, dtoDefinition);
						var infrastructureModel = GetInfrastructureModel(infrastructureDomains,@namespace.Domain,referenceModelName);

						if (useCase.Type == ApplicationUseCaseType.Read
							|| useCase.Type == ApplicationUseCaseType.Read)
						{
							dtoMap.DataModelName = referenceModelName;
							dtoMap.DataModel = infrastructureModel;
							dtoMap.ClassificationKey = infrastructureModel?.ClassificationKey;
						}
						else if (useCase.Type == ApplicationUseCaseType.Create
							|| useCase.Type == ApplicationUseCaseType.Update
							|| useCase.Type == ApplicationUseCaseType.Delete)
						{
							dtoMap.DomainModelName = referenceModelName;
							if (domainModelReferenceMap.TryGetDomainModel(@namespace.Domain, referenceModelName, out var domainModelMap))
							{
								dtoMap.ClassificationKey = domainModelMap.ClassificationKey;
							}
						}

						if (dtoDefinition.Name.IsNullOrEmpty())
						{
							dtoDefinition.Name = referenceModelName;
						}

						dtoNameCache.Add(dtoDefinition.Name, dtoMap);
						dtoCache.Add(dtoMap.DtoName, dtoMap);

						if (dtoDefinition.ReferenceModelName.IsNullOrEmpty())
						{
							dtoDefinition.ReferenceModelName = referenceModelName;
						}

						dtoDefinition.Name = dtoMap.DtoName;

						map.AddDto(dtoMap);
					}

					if (!useCase.MainDto.IsNullOrEmpty() && dtoNameCache.ContainsKey(useCase.MainDto))
					{
						useCase.MainDto = dtoNameCache[useCase.MainDto].DtoName;
					}
					else
					{
						useCase.MainDto = useCase.Dtos.FirstOrDefault()?.Name ?? "";
					}

					foreach (var dtoDefinition in useCase.Dtos)
					{
						var dtoMap = dtoCache[dtoDefinition.Name];

						foreach (var property in dtoDefinition.Properties)
						{
							if (!dtoNameCache.ContainsKey(property.Type))
							{
								continue;
							}

							var referenceDtoMap = dtoNameCache[property.Type];

							property.Type = referenceDtoMap.DtoName;
							dtoMap.ChildReferenceProperties.Add(new ReferenceDtoProperty
							{
								Property = property,
								Dto = referenceDtoMap
							});
						}
					}
				}
			}

			return map;
		}

		private static InfrastructureModel GetInfrastructureModel(Dictionary<string, Dictionary<string, InfrastructureModel>> infrastructureDomains, string domain, string referenceModelName)
		{
			if (!infrastructureDomains.ContainsKey(domain)
				|| !infrastructureDomains[domain].ContainsKey(referenceModelName))
			{
				return null;
			}

			return infrastructureDomains[domain][referenceModelName];
		}

		private static string GetDtoName(ApplicationUseCase useCase, ApplicationUseCaseDto dtoDefinition)
		{
			var dtoName = dtoDefinition.Name;
			if (dtoName.IsNullOrEmpty())
			{
				dtoName = GetModelName(useCase, dtoDefinition);
			}

			return $"{useCase.UseCaseReferenceName()}{useCase.UseCaseName}{dtoName}Dto";
		}

		private static string GetModelName(ApplicationUseCase useCase, ApplicationUseCaseDto dtoDefinition)
		{
			var modelName = dtoDefinition.ReferenceModelName;
			if (modelName.IsNullOrEmpty())
			{
				modelName = useCase.ClassificationKey;
				dtoDefinition.ReferenceModelName = modelName;
			}

			return modelName;
		}
	}

	public class DtoReferenceMap
	{
		/// <summary>
		/// Domain -> UseCase -> DtoName
		/// </summary>
		private readonly Dictionary<string, Dictionary<string, Dictionary<string, ReferenceDtoMap>>> _referencesDto = [];
		/// <summary>
		/// Domain -> UseCase -> DomainModelName
		/// </summary>
		private readonly Dictionary<string, Dictionary<string, Dictionary<string, ReferenceDtoMap>>> _referencesDomainModels = [];

		public void AddDto(ReferenceDtoMap dtoMap)
		{
			var useCase = GetUseCaseKey(dtoMap.UseCase, dtoMap.NamespaceClassificationKey);

			if (!_referencesDto.ContainsKey(dtoMap.Domain))
			{
				_referencesDto.Add(dtoMap.Domain, new Dictionary<string, Dictionary<string, ReferenceDtoMap>>());
			}

			if (!_referencesDto[dtoMap.Domain].ContainsKey(useCase))
			{
				_referencesDto[dtoMap.Domain].Add(useCase, new Dictionary<string, ReferenceDtoMap>());
			}

			_referencesDto[dtoMap.Domain][useCase].Add(dtoMap.DtoName, dtoMap);

			if (!dtoMap.DomainModelName.IsNullOrEmpty())
			{
				if (!_referencesDomainModels.ContainsKey(dtoMap.Domain))
				{
					_referencesDomainModels.Add(dtoMap.Domain, new Dictionary<string, Dictionary<string, ReferenceDtoMap>>());
				}

				if (!_referencesDomainModels[dtoMap.Domain].ContainsKey(useCase))
				{
					_referencesDomainModels[dtoMap.Domain].Add(useCase, new Dictionary<string, ReferenceDtoMap>());
				}

				_referencesDomainModels[dtoMap.Domain][useCase].Add(dtoMap.DomainModelName, dtoMap);
			}
		}

		public bool TryGetDtoByDomainModel(string domain, string useCase, string classificationKey, string domainModelName, out ReferenceDtoMap dtoMap)
		{
			useCase = GetUseCaseKey(useCase, classificationKey);

			if (!_referencesDomainModels.ContainsKey(domain)
				|| !_referencesDomainModels[domain].ContainsKey(useCase)
				|| !_referencesDomainModels[domain][useCase].ContainsKey(domainModelName))
			{
				dtoMap = null;

				return false;
			}

			dtoMap = _referencesDomainModels[domain][useCase][domainModelName];

			return true;
		}

		public bool TryGetDto(string domain, string useCase, string classificationKey, string dtoName, out ReferenceDtoMap dtoMap)
		{
			useCase = GetUseCaseKey(useCase, classificationKey);

			if (!_referencesDto.ContainsKey(domain)
				|| !_referencesDto[domain].ContainsKey(useCase)
				|| !_referencesDto[domain][useCase].ContainsKey(dtoName))
			{
				dtoMap = null;

				return false;
			}

			dtoMap = _referencesDto[domain][useCase][dtoName];

			return true;
		}

		public IEnumerable<ReferenceDtoMap> GetReferenceDtoMaps(string domain, string useCase, string classificationKey)
		{
			useCase = GetUseCaseKey(useCase, classificationKey);

			if (!_referencesDto.ContainsKey(domain)
				|| !_referencesDto[domain].ContainsKey(useCase))
			{
				yield break;
			}

			foreach (var dto in _referencesDto[domain][useCase].Values)
			{
				yield return dto;
			}
		}

		public IEnumerable<ReferenceDtoMap> GetReferenceDtoMaps()
		{
			foreach (var domain in _referencesDto)
			{
				foreach (var useCase in domain.Value)
				{
					foreach (var dto in useCase.Value)
					{
						yield return dto.Value;
					}
				}
			}
		}

		private string GetUseCaseKey(string useCase, string classificationKey)
		{
			return $"{classificationKey}.{useCase}";
		}
	}

	public class ReferenceDtoMap : AbstractReferenceDto
	{
		public ReferenceDtoMap()
		{
			ChildReferenceProperties = new List<ReferenceDtoProperty>();
		}

		public string ClassificationKey { get; set; }
		public string NamespaceClassificationKey { get; set; }
		public List<ReferenceDtoProperty> ChildReferenceProperties { get; set; }
	}

	public abstract class AbstractReferenceDto
	{
		public string Domain { get; set; }
		public string UseCase { get; set; }
		public string DtoName { get; set; }
		public string DomainModelName { get; set; }
		public string DataModelName { get; set; }

		public ApplicationUseCaseDto Dto { get; set; }
		public InfrastructureModel DataModel { get; set; }
	}

	public class ReferenceDtoProperty
	{
		public ApplicationUseCaseDtoProperty Property { get; set; }
		public ReferenceDtoMap Dto { get; set; }
	}
}
