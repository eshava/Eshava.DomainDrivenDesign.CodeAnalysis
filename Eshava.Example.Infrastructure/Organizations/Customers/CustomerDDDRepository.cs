using System;
using System.Collections.Generic;
using Eshava.Example.Domain.Organizations.CustomerFeature;

namespace Eshava.Example.Infrastructure.Organizations.Customers
{
	internal partial class CustomerDDDRepository
	{
		static CustomerDDDRepository()
		{
			PropertyValueToDataMappings = new Dictionary<string, Func<object, object>>
			{
				{ "Example", domainValue => domainValue },
				{
				  /// Relates to <see cref="Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure.InfrastructureModel.UseCustomMapping"/>
				  "MetaData", domainValue =>
				  {
					  if(domainValue is null)
					  {
						  return null;
					  }

					  var metaData = domainValue as MetaDataVO;

					  return new MetaDataData
					  {
						  Version = metaData.Version,
						  Timestamps = metaData.Timestamps
					  };
				  }
				},
				/// Relates to <see cref="Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure.InfrastructureModel.UseCustomMapping"/>
				{ "MetaData.Version", domainValue => domainValue }
			};

			_customerDDDPropertyValueToDomainMappings = new Dictionary<string, Func<object, object>>
			{
				{ "Example", dataValue => dataValue },
				{
				  /// Relates to <see cref="Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure.InfrastructureModel.UseCustomMapping"/>
				  "MetaData", dataValue =>
				  {
					  if(dataValue is null)
					  {
						  return null;
					  }

					  var metaData = dataValue as MetaDataData;

					  return new MetaDataVO
					  (
						  metaData.Version,
						  metaData.Timestamps
					  );
				  }
				},
				/// Relates to <see cref="Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure.InfrastructureModel.UseCustomMapping"/>
				{ "MetaData.Version", domainValue => domainValue }
			};
		}
	}
}