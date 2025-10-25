using System.Threading.Tasks;
using Eshava.Core.Models;
using Eshava.Example.Infrastructure.Organizations.Locations;
using Eshava.Example.Infrastructure.Organizations.Offices;
using Eshava.Core.Extensions;

namespace Eshava.Example.Infrastructure.Organizations.Customers
{
	internal partial class CustomerDDDInfrastructureProviderService
	{
		private Task<ResponseData<bool>> ByPassActionForCreateAsync(Domain.Organizations.CustomerFeature.CustomerDDD customer)
		{
			return true.ToResponseDataAsync();
		}

		private Task<ResponseData<bool>> ByPassActionForUpdateAsync(Domain.Organizations.CustomerFeature.CustomerDDD customer)
		{
			return true.ToResponseDataAsync();
		}

		private Task<ResponseData<bool>> ByPassActionForDeleteAsync(Domain.Organizations.CustomerFeature.CustomerDDD customer)
		{
			return true.ToResponseDataAsync();
		}

		private Task<ResponseData<bool>> ByPassActionForCreateAsync(Domain.Organizations.CustomerFeature.OfficeDDD office, CustomerDDDCreationBag customerDDDCreation)
		{
			return true.ToResponseDataAsync();
		}

		private Task<ResponseData<bool>> ByPassActionForUpdateAsync(Domain.Organizations.CustomerFeature.OfficeDDD office, CustomerDDDCreationBag customerDDDCreation)
		{
			return true.ToResponseDataAsync();
		}

		private Task<ResponseData<bool>> ByPassActionForDeleteAsync(Domain.Organizations.CustomerFeature.OfficeDDD office)
		{
			return true.ToResponseDataAsync();
		}

		private Task<ResponseData<bool>> ByPassActionForCreateAsync(Domain.Organizations.CustomerFeature.BillingOfficeDDD office, CustomerDDDCreationBag customerDDDCreation)
		{
			return true.ToResponseDataAsync();
		}

		private Task<ResponseData<bool>> ByPassActionForUpdateAsync(Domain.Organizations.CustomerFeature.BillingOfficeDDD office, CustomerDDDCreationBag customerDDDCreation)
		{
			return true.ToResponseDataAsync();
		}

		private Task<ResponseData<bool>> ByPassActionForDeleteAsync(Domain.Organizations.CustomerFeature.BillingOfficeDDD office)
		{
			return true.ToResponseDataAsync();
		}

		private Task<ResponseData<bool>> ByPassActionForCreateAsync(Domain.Organizations.CustomerFeature.LocationDDD location, OfficeDDDCreationBag officeDDDCreationBag)
		{
			return true.ToResponseDataAsync();
		}

		private Task<ResponseData<bool>> ByPassActionForUpdateAsync(Domain.Organizations.CustomerFeature.LocationDDD location, OfficeDDDCreationBag officeDDDCreationBag)
		{
			return true.ToResponseDataAsync();
		}

		private Task<ResponseData<bool>> ByPassActionForDeleteAsync(Domain.Organizations.CustomerFeature.LocationDDD location)
		{
			return true.ToResponseDataAsync();
		}

		private Task<ResponseData<bool>> ByPassActionForCreateAsync(Domain.Organizations.CustomerFeature.BuildingDDD building, LocationDDDCreationBag locationDDDCreationBag)
		{
			return true.ToResponseDataAsync();
		}

		private Task<ResponseData<bool>> ByPassActionForUpdateAsync(Domain.Organizations.CustomerFeature.BuildingDDD building, LocationDDDCreationBag locationDDDCreationBag)
		{
			return true.ToResponseDataAsync();
		}

		private Task<ResponseData<bool>> ByPassActionForDeleteAsync(Domain.Organizations.CustomerFeature.BuildingDDD building)
		{
			return true.ToResponseDataAsync();
		}
	}
}
