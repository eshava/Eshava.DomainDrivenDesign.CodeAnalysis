using System.Collections.Generic;
using System.Threading.Tasks;
using Eshava.Core.Models;
using Eshava.DomainDrivenDesign.Application.PartialPut;
using Eshava.DomainDrivenDesign.Domain.Models;
using Eshava.Example.Domain.Organizations.CustomerFeature;

namespace Eshava.Example.Application.Organizations.CustomerFeature.Customers.Commands.UpdateOfficeWithSingleLocationOfficeDDD
{
	internal partial class CustomerDDDUpdateOfficeWithSingleLocationOfficeDDDUseCase
	{
		private async Task<ResponseData<LocationDDD>> UpdateLocationAsync(OfficeDDD office, KeyValuePair<int, IList<Patch<LocationDDD>>> locationPatches, PartialPutDocumentLayer locationDocumentLayer)
		{
			var locationResult = office.GetLocationDDD(locationPatches.Key);
			if (locationResult.IsFaulty)
			{
				return locationResult;
			}

			var constraintsResult = await CheckValidationConstraintsAsync(locationResult.Data, locationPatches.Value);
			if (constraintsResult.IsFaulty)
			{
				return constraintsResult.ConvertTo<LocationDDD>();
			}

			// Custom code to update the location

			var locationPatchResult = locationResult.Data.Patch(locationPatches.Value);
			if (locationPatchResult.IsFaulty)
			{
				return locationPatchResult.ConvertTo<LocationDDD>();
			}

			if (locationDocumentLayer is not null)
			{
				var processBuildingsChangesResult = await ProcessBuildingChangesAsync(locationResult.Data, locationDocumentLayer);
				if (processBuildingsChangesResult.IsFaulty)
				{
					return processBuildingsChangesResult.ConvertTo<LocationDDD>();
				}
			}

			return locationResult;
		}
	}
}