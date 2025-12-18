using System.Collections.Generic;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Domain;
using Eshava.DomainDrivenDesign.CodeAnalysis.Templates.Domain;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Factories
{
	public static class DomainFactory
	{
		public static FactoryResult GenerateSourceCode(
			DomainProject domainProjectConfig,
			IEnumerable<DomainModels> domainModelsConfigs
		)
		{
			var domainModelsConfig = domainModelsConfigs.Merge();
			
			var factoryResult = new FactoryResult();
			var referenceMap = DependencyAnalysis.Analyse(domainModelsConfig, null);

			foreach (var domainModelMap in referenceMap.GetReferenceDomainModelMaps())
			{
				var domainModel = domainModelMap.IsValueObject
					? ValueObjectTemplate.GetValueObject(domainModelMap, domainProjectConfig, referenceMap)
					: DomainModelTemplate.GetDomainModel(domainModelMap, domainProjectConfig, referenceMap);

				var domainModelSourceName = $"{domainProjectConfig.FullQualifiedNamespace}.{domainModelMap.Domain}.{domainModelMap.DomainModel.NamespaceDirectory}.{domainModelMap.DomainModelName}.g.cs";

				factoryResult.AddSource(domainModelSourceName, domainModel);
			}

			foreach (var enumerationMap in referenceMap.GetReferenceEnumerationMaps())
			{
				var enumeration = EnumerationTemplate.GetEnumeration(enumerationMap, domainProjectConfig);
				var enumerationSourceName = $"{domainProjectConfig.FullQualifiedNamespace}.{enumerationMap.Namespace}.{enumerationMap.Name}.g.cs";

				factoryResult.AddSource(enumerationSourceName, enumeration);
			}

			return factoryResult;
		}
	}
}