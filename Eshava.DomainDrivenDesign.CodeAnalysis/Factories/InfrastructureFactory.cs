using System;
using System.Collections.Generic;
using System.Linq;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Domain;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure;
using Eshava.DomainDrivenDesign.CodeAnalysis.Templates.Infrastructure;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Factories
{
	public static class InfrastructureFactory
	{
		public static FactoryResult GenerateSourceCode(
			ApplicationProject applicationProjectConfig,
			IEnumerable<ApplicationUseCases> applicationUseCasesConfigs,
			DomainProject domainProjectConfig,
			IEnumerable<DomainModels> domainModelsConfigs,
			InfrastructureProject infrastructureProjectConfig,
			IEnumerable<InfrastructureModels> infrastructureModelsConfigs
		)
		{
			var domainModelsConfig = domainModelsConfigs.Merge();
			var infrastructureModelsConfig = infrastructureModelsConfigs.Merge();
			var applicationUseCasesConfig = applicationUseCasesConfigs.Merge();

			infrastructureModelsConfig.Namespaces.ForEach(ns => CheckReferenceProperties(ns.Models));

			var referenceMap = domainModelsConfig is null
				? new ReferenceMap()
				: DependencyAnalysis.Analyse(domainModelsConfig, infrastructureModelsConfig);

			var dtoReferenceMap = applicationUseCasesConfig is null || domainModelsConfig is null
				? new DtoReferenceMap()
				: DtoDependencyAnalysis.Analyse(applicationUseCasesConfig, referenceMap, infrastructureModelsConfig);

			var useCasesMap = applicationUseCasesConfig is null || domainModelsConfig is null
				? new UseCasesMap()
				: UseCaseAnalysis.Analyse(applicationProjectConfig, referenceMap, dtoReferenceMap, applicationUseCasesConfig);

			var infrastructureModels = infrastructureModelsConfig.Namespaces
				.ToDictionary(
					d => d.Domain,
					d => d.Models.ToDictionary(m => m.Name, m => m.ClassificationKey)
				);

			var factoryResult = new FactoryResult();

			var dependencyInjections = new List<DependencyInjection>();
			var dependencyInjectionsDbConfigurations = new List<DependencyInjection>();
			var dependencyInjectionsTransformationProfiles = new List<DependencyInjection>();

			foreach (var @namespace in infrastructureModelsConfig.Namespaces)
			{
				AddTransformProfiles(
					factoryResult,
					infrastructureProjectConfig.FullQualifiedNamespace,
					applicationProjectConfig?.FullQualifiedNamespace,
					@namespace.Domain,
					infrastructureModels,
					applicationUseCasesConfig?.Namespaces.FirstOrDefault(ns => ns.Domain == @namespace.Domain)?.UseCases ?? new List<ApplicationUseCase>(),
					dependencyInjectionsTransformationProfiles,
					infrastructureProjectConfig.AddAssemblyCommentToFiles
				);

				var domainNamespace = $"{infrastructureProjectConfig.FullQualifiedNamespace}.{@namespace.Domain}";
				var applicationNamespace = $"{applicationProjectConfig.FullQualifiedNamespace}.{@namespace.Domain}";

				var childsForModel = @namespace.Models
					.Where(m => m.IsChild && !m.ReferencedParent.IsNullOrEmpty())
					.GroupBy(m => m.ReferencedParent)
					.ToDictionary(m => m.Key, m => m.Select(c => c).ToList());

				// Workaround to allow multiple data models for the same classification key
				// It's risky, because query repository are based on classification key.
				// First come, first serve. The first data model reference occurence during, query provider map collection, will be use.
				// The other data model will be ignored.
				// Use this feature only, if all data models for the same classification key are equal.
				// Purpose: Two domain models, one as separate domain model, the other as aggregate child
				var processedClassificationKeys = new HashSet<string>();

				foreach (var model in @namespace.Models)
				{
					var databaseModel = DataModelTemplate.GetDatabaseModel(model, @namespace.Domain, domainNamespace, infrastructureProjectConfig.AlternativeAbstractDatabaseModel, infrastructureProjectConfig.AlternativeUsing, childsForModel, infrastructureModels, infrastructureProjectConfig.AddAssemblyCommentToFiles);
					var databaseModelSourceName = $"{domainNamespace}.{model.ClassificationKey.ToPlural()}.{model.Name}.g.cs";

					factoryResult.AddSource(databaseModelSourceName, databaseModel);

					if (!model.TableName.IsNullOrEmpty())
					{
						var dbConfigModel = DbConfigurationTemplate.GetDbConfiguration(model, @namespace.DatabaseSchema, domainNamespace, infrastructureProjectConfig.AddAssemblyCommentToFiles);
						var dbConfigModelSourceName = $"{domainNamespace}.{model.ClassificationKey.ToPlural()}.{model.Name}DbConfiguration.g.cs";

						factoryResult.AddSource(dbConfigModelSourceName, dbConfigModel);

						dependencyInjectionsDbConfigurations.Add(new DependencyInjection
						{
							Class = $"{model.Name}DbConfiguration",
							ClassUsing = $"{domainNamespace}.{model.ClassificationKey.ToPlural()}"
						});
					}

					var modelsFromNamespace = @namespace.Models.ToDictionary(m => m.Name, m => m);

					AddCreationBag(
						factoryResult,
						model,
						@namespace.Domain,
						domainNamespace,
						referenceMap,
						infrastructureProjectConfig.AddAssemblyCommentToFiles
					);

					AddRepository(
						factoryResult,
						model,
						@namespace.Domain,
						domainNamespace,
						@namespace.DatabaseSettingsInterface,
						@namespace.DatabaseSettingsInterfaceUsing,
						infrastructureProjectConfig,
						modelsFromNamespace,
						childsForModel,
						referenceMap,
						dependencyInjections
					);

					if (model.CreateProviderService)
					{
						AddProviderService(
							factoryResult,
							model,
							@namespace.Domain,
							domainNamespace,
							applicationNamespace,
							@namespace.DatabaseSettingsInterface,
							@namespace.DatabaseSettingsInterfaceUsing,
							infrastructureProjectConfig,
							childsForModel,
							referenceMap,
							useCasesMap,
							dependencyInjections
						);
					}

					if (useCasesMap.TryGetQueryProvider(@namespace.Domain, model.ClassificationKey, out var queryProviderMap) && !processedClassificationKeys.Contains(model.ClassificationKey))
					{
						processedClassificationKeys.Add(model.ClassificationKey);

						AddQueryProviderService(
							factoryResult,
							infrastructureProjectConfig,
							model,
							domainNamespace,
							applicationNamespace,
							queryProviderMap,
							dependencyInjections,
							infrastructureProjectConfig.AddAssemblyCommentToFiles
						);

						AddQueryRepository(
							factoryResult,
							infrastructureProjectConfig,
							infrastructureModelsConfig,
							model,
							@namespace.Domain,
							childsForModel,
							domainNamespace,
							applicationNamespace,
							@namespace.DatabaseSettingsInterface,
							@namespace.DatabaseSettingsInterfaceUsing,
							queryProviderMap,
							dependencyInjections
						);
					}
				}
			}

			AddServiceCollection(factoryResult, infrastructureProjectConfig, dependencyInjections, dependencyInjectionsDbConfigurations, dependencyInjectionsTransformationProfiles);

			return factoryResult;
		}

		private static void AddServiceCollection(
			FactoryResult factoryResult,
			InfrastructureProject infrastructureProject,
			List<DependencyInjection> dependencyInjections,
			List<DependencyInjection> dependencyInjectionsDbConfigurations,
			List<DependencyInjection> dependencyInjectionsTransformationProfiles
		)
		{
			var serviceCollection = ServiceCollectionExtensionTemplate.GetServiceCollection(infrastructureProject, dependencyInjections, dependencyInjectionsDbConfigurations, dependencyInjectionsTransformationProfiles);
			var sourceName = $"{infrastructureProject.FullQualifiedNamespace}.Extensions.ServiceCollectionExtensions.g.cs";

			factoryResult.AddSource(sourceName, serviceCollection);
		}

		private static void AddProviderService(
			FactoryResult factoryResult,
			InfrastructureModel model,
			string domain,
			string domainNamespace,
			string applicationNamespace,
			string databaseSettingsInterface,
			string databaseSettingsInterfaceUsing,
			InfrastructureProject infrastructureProject,
			Dictionary<string, List<InfrastructureModel>> childsForModel,
			ReferenceMap domainModelReferenceMap,
			UseCasesMap useCasesMap,
			List<DependencyInjection> dependencyInjections
		)
		{
			if (!model.CreateProviderService)
			{
				return;
			}

			if (!domainModelReferenceMap.TryGetDomainModelByDataModel(domain, model.Name, out var domainModels))
			{
				domainModels = Array.Empty<ReferenceDomainModelMap>();
			}

			if (domainModels.Any())
			{
				foreach (var domainModel in domainModels)
				{
					AddProviderService(
						factoryResult,
						model,
						domain,
						domainNamespace,
						applicationNamespace,
						databaseSettingsInterface,
						databaseSettingsInterfaceUsing,
						infrastructureProject,
						childsForModel,
						domainModel,
						useCasesMap,
						dependencyInjections
					);
				}
			}
			else
			{
				AddProviderService(
					factoryResult,
					model,
					domain,
					domainNamespace,
					applicationNamespace,
					databaseSettingsInterface,
					databaseSettingsInterfaceUsing,
					infrastructureProject,
					childsForModel,
					(ReferenceDomainModelMap)null,
					useCasesMap,
					dependencyInjections
				);
			}
		}

		private static void AddProviderService(
			FactoryResult factoryResult,
			InfrastructureModel model,
			string domain,
			string domainNamespace,
			string applicationNamespace,
			string databaseSettingsInterface,
			string databaseSettingsInterfaceUsing,
			InfrastructureProject infrastructureProject,
			Dictionary<string, List<InfrastructureModel>> childsForModel,
			ReferenceDomainModelMap domainModel,
			UseCasesMap useCasesMap,
			List<DependencyInjection> dependencyInjections
		)
		{
			if (domainModel is null)
			{
				return;
			}

			var featureName = useCasesMap.GetFeatureName(domain, model.ClassificationKey);
			if (!featureName.IsNullOrEmpty())
			{
				featureName += ".";
			}

			var providerService = InfrastructureProviderServiceTemplate.GetProviderService(
				model,
				domainModel,
				domain,
				featureName,
				domainNamespace,
				applicationNamespace,
				infrastructureProject,
				databaseSettingsInterface,
				databaseSettingsInterfaceUsing,
				childsForModel
			);

			var providerServiceSourceName = $"{domainNamespace}.{model.ClassificationKey.ToPlural()}.{domainModel.DomainModelName}InfrastructureProviderService.g.cs";

			factoryResult.AddSource(providerServiceSourceName, providerService);

			dependencyInjections.Add(new DependencyInjection
			{
				Interface = $"I{domainModel.DomainModelName}InfrastructureProviderService",
				InterfaceUsing = $"{applicationNamespace}.{featureName}{model.ClassificationKey.ToPlural()}.Commands",
				Class = $"{domainModel.DomainModelName}InfrastructureProviderService",
				ClassUsing = $"{domainNamespace}.{model.ClassificationKey.ToPlural()}"
			});
		}

		private static void AddQueryProviderService(
			FactoryResult factoryResult,
			InfrastructureProject infrastructureProject,
			InfrastructureModel model,
			string domainNamespace,
			string applicationNamespace,
			QueryProviderMap queryProviderMap,
			List<DependencyInjection> dependencyInjections,
			bool addAssemblyCommentToFiles
		)
		{
			var providerService = QueryInfrastructureProviderServiceTemplate.GetProviderService(
				infrastructureProject,
				model,
				queryProviderMap,
				domainNamespace,
				applicationNamespace,
				addAssemblyCommentToFiles
			);

			var featureNameNamespace = queryProviderMap.FeatureName.IsNullOrEmpty() ? "" : $"{queryProviderMap.FeatureName}.";
			var providerServiceSourceName = $"{domainNamespace}.{model.ClassificationKey.ToPlural()}.{model.ClassificationKey}QueryInfrastructureProviderService.g.cs";

			factoryResult.AddSource(providerServiceSourceName, providerService);

			dependencyInjections.Add(new DependencyInjection
			{
				Interface = $"I{model.ClassificationKey}QueryInfrastructureProviderService",
				InterfaceUsing = $"{applicationNamespace}.{featureNameNamespace}{model.ClassificationKey.ToPlural()}.Queries",
				Class = $"{model.ClassificationKey}QueryInfrastructureProviderService",
				ClassUsing = $"{domainNamespace}.{model.ClassificationKey.ToPlural()}"
			});
		}

		private static void AddRepository(
			FactoryResult factoryResult,
			InfrastructureModel model,
			string domain,
			string domainNamespace,
			string databaseSettingsInterface,
			string databaseSettingsInterfaceUsing,
			InfrastructureProject infrastructureProject,
			Dictionary<string, InfrastructureModel> modelsFromNamespace,
			Dictionary<string, List<InfrastructureModel>> childsForModel,
			ReferenceMap domainModelReferenceMap,
			List<DependencyInjection> dependencyInjections
		)
		{
			if (!model.CreateRepository)
			{
				return;
			}

			InfrastructureModel parent = null;
			if (model.IsChild)
			{
				parent = modelsFromNamespace.Values.FirstOrDefault(m => m.Name == model.ReferencedParent);
			}

			if (!domainModelReferenceMap.TryGetDomainModelByDataModel(domain, model.Name, out var domainModels))
			{
				domainModels = Array.Empty<ReferenceDomainModelMap>();
			}

			if (domainModels.Any())
			{
				foreach (var domainModel in domainModels)
				{
					AddRepository(
					factoryResult,
					parent,
					model,
					domain,
					domainNamespace,
					databaseSettingsInterface,
					databaseSettingsInterfaceUsing,
					infrastructureProject,
					modelsFromNamespace,
					childsForModel,
					domainModel,
					domainModelReferenceMap,
					dependencyInjections
				);
				}
			}
			else
			{
				AddRepository(
					factoryResult,
					parent,
					model,
					domain,
					domainNamespace,
					databaseSettingsInterface,
					databaseSettingsInterfaceUsing,
					infrastructureProject,
					modelsFromNamespace,
					childsForModel,
					null,
					domainModelReferenceMap,
					dependencyInjections
				);
			}
		}

		private static void AddRepository(
			FactoryResult factoryResult,
			InfrastructureModel parent,
			InfrastructureModel model,
			string domain,
			string domainNamespace,
			string databaseSettingsInterface,
			string databaseSettingsInterfaceUsing,
			InfrastructureProject infrastructureProject,
			Dictionary<string, InfrastructureModel> modelsFromNamespace,
			Dictionary<string, List<InfrastructureModel>> childsForModel,
			ReferenceDomainModelMap domainModel,
			ReferenceMap domainModelReferenceMap,
			List<DependencyInjection> dependencyInjections
		)
		{
			if (domainModel is null)
			{
				return;
			}

			var repositoryInterface = RepositoryInterfaceTemplate.GetInterface(
				model,
				domainModel,
				domain,
				domainNamespace,
				infrastructureProject
			);

			var repositoryInterfaceSourceName = $"{domainNamespace}.{model.ClassificationKey.ToPlural()}.I{domainModel.DomainModelName}Repository.g.cs";

			factoryResult.AddSource(repositoryInterfaceSourceName, repositoryInterface);

			var repository = RepositoryTemplate.GetRepository(
				model,
				domainModel,
				domain,
				domainNamespace,
				infrastructureProject,
				databaseSettingsInterface,
				databaseSettingsInterfaceUsing,
				parent,
				childsForModel,
				modelsFromNamespace,
				domainModelReferenceMap
			);

			var repositorySourceName = $"{domainNamespace}.{model.ClassificationKey.ToPlural()}.{domainModel.DomainModelName}Repository.g.cs";

			factoryResult.AddSource(repositorySourceName, repository);

			dependencyInjections.Add(new DependencyInjection
			{
				Interface = $"I{domainModel.DomainModelName}Repository",
				InterfaceUsing = $"{domainNamespace}.{model.ClassificationKey.ToPlural()}",
				Class = $"{domainModel.DomainModelName}Repository",
				ClassUsing = $"{domainNamespace}.{model.ClassificationKey.ToPlural()}"
			});
		}

		private static void AddQueryRepository(
			FactoryResult factoryResult,
			InfrastructureProject infrastructureProject,
			InfrastructureModels infrastructureModelsConfig,
			InfrastructureModel model,
			string domain,
			Dictionary<string, List<InfrastructureModel>> childsForModel,
			string domainNamespace,
			string applicationNamespace,
			string databaseSettingsInterface,
			string databaseSettingsInterfaceUsing,
			QueryProviderMap queryProviderMap,
			List<DependencyInjection> dependencyInjections
		)
		{
			var @interface = QueryRepositoryInterfaceTemplate.GetInterface(
				model,
				queryProviderMap,
				domainNamespace,
				infrastructureProject.AddAssemblyCommentToFiles
			);

			var interfaceSourceName = $"{domainNamespace}.{model.ClassificationKey.ToPlural()}.I{model.ClassificationKey}QueryRepository.g.cs";

			factoryResult.AddSource(interfaceSourceName, @interface);

			var repository = QueryRepositoryTemplate.GetRepository(
				infrastructureProject,
				model,
				domain,
				childsForModel,
				infrastructureModelsConfig,
				queryProviderMap,
				domainNamespace,
				applicationNamespace,
				databaseSettingsInterface,
				databaseSettingsInterfaceUsing
			);

			var repositorySourceName = $"{domainNamespace}.{model.ClassificationKey.ToPlural()}.{model.ClassificationKey}QueryRepository.g.cs";

			factoryResult.AddSource(repositorySourceName, repository);

			dependencyInjections.Add(new DependencyInjection
			{
				Interface = $"I{model.ClassificationKey}QueryRepository",
				InterfaceUsing = $"{domainNamespace}.{model.ClassificationKey.ToPlural()}",
				Class = $"{model.ClassificationKey}QueryRepository",
				ClassUsing = $"{domainNamespace}.{model.ClassificationKey.ToPlural()}"
			});
		}

		private static void AddTransformProfiles(
			FactoryResult factoryResult,
			string infrastructureProjectNamespace,
			string applicationProjectNamespace,
			string domain,
			Dictionary<string, Dictionary<string, string>> infrastructureModels,
			IEnumerable<ApplicationUseCase> useCases,
			List<DependencyInjection> dependencyInjections,
			bool addAssemblyCommentToFiles
		)
		{
			var transformationProfile = TransformProfileTemplate.GetTransformProfile(
				infrastructureProjectNamespace,
				applicationProjectNamespace,
				domain,
				infrastructureModels,
				useCases,
				addAssemblyCommentToFiles
			);

			if (transformationProfile.IsNullOrEmpty())
			{
				return;
			}

			var transformationProfileSourceName = $"{infrastructureProjectNamespace}.{domain}.{domain}AutoGenTransformProfile.g.cs";

			factoryResult.AddSource(transformationProfileSourceName, transformationProfile);

			dependencyInjections.Add(new DependencyInjection
			{
				Class = $"{domain}AutoGenTransformProfile",
				ClassUsing = $"{infrastructureProjectNamespace}.{domain}"
			});
		}

		private static void AddCreationBag(
			FactoryResult factoryResult,
			InfrastructureModel model,
			string domain,
			string domainNamespace,
			ReferenceMap domainModelReferenceMap,
			bool addAssemblyCommentToFiles
		)
		{
			if (!model.CreateCreationBag)
			{
				return;
			}

			if (!domainModelReferenceMap.TryGetDomainModelByDataModel(domain, model.Name, out var domainModels))
			{
				domainModels = Array.Empty<ReferenceDomainModelMap>();
			}

			if (domainModels.Any())
			{
				foreach (var domainModel in domainModels)
				{
					AddCreationBag(
						factoryResult,
						model,
						domain,
						domainNamespace,
						domainModel,
						addAssemblyCommentToFiles
					);
				}
			}
		}

		private static void AddCreationBag(
			FactoryResult factoryResult,
			InfrastructureModel model,
			string domain,
			string domainNamespace,
			ReferenceDomainModelMap domainModel,
			bool addAssemblyCommentToFiles
		)
		{
			var creationBag = CreationBagTemplate.GetCreationBag(model, domainModel, domainNamespace, addAssemblyCommentToFiles);
			var creationBagSourceName = $"{domainNamespace}.{model.ClassificationKey.ToPlural()}.{domainModel.DomainModelName}CreationBag.g.cs";

			factoryResult.AddSource(creationBagSourceName, creationBag);
		}

		private static void CheckReferenceProperties(IEnumerable<InfrastructureModel> models)
		{
			foreach (var model in models)
			{
				var foreignKeyProperties = model.Properties.Where(p => p.IsReference).ToList();
				foreach (var foreignKeyProperty in foreignKeyProperties)
				{
					if (model.Properties.FirstOrDefault(p => p.ReferencePropertyName == foreignKeyProperty.Name) is null)
					{
						var propertyName = foreignKeyProperty.Name.EndsWith("Id")
							? foreignKeyProperty.Name.Substring(0, foreignKeyProperty.Name.Length - 2)
							: foreignKeyProperty.Name + "Reference";

						model.Properties.Add(new InfrastructureModelProperty
						{
							Type = foreignKeyProperty.ReferenceType,
							Name = propertyName,
							ReferencePropertyName = foreignKeyProperty.Name,
							ReferenceType = foreignKeyProperty.ReferenceType,
							ReferenceDomain = foreignKeyProperty.ReferenceDomain,
							SkipFromDomainModel = true
						});
					}
				}
			}
		}
	}
}