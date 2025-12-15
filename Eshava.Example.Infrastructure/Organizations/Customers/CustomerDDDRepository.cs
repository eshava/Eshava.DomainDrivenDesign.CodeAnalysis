using System;
using System.Collections.Generic;

namespace Eshava.Example.Infrastructure.Organizations.Customers
{
	internal partial class CustomerDDDRepository
	{
		static CustomerDDDRepository()
		{
			PropertyValueToDataMappings = new Dictionary<string, Func<object, object>>
			{
				{ "Example", domainValue => domainValue }
			};
		}
	}
}