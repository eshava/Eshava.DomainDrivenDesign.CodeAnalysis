using System.Collections.Generic;
using System.Threading.Tasks;
using Eshava.Core.Extensions;
using Eshava.Core.Models;
using Eshava.DomainDrivenDesign.Application.PartialPut;
using Eshava.DomainDrivenDesign.Domain.Models;
using Eshava.Example.Domain.Organizations.CustomerFeature;

namespace Eshava.Example.Application.Organizations.CustomerFeature.Customers.Commands.UpdateWithSingleOffice
{
	internal partial class CustomerDDDUpdateWithSingleOfficeUseCase
	{
		private async Task<ResponseData<BuildingDDD>> UpdateBuildingAsync(LocationDDD location, KeyValuePair<int, IList<Patch<BuildingDDD>>> buildingPatches, PartialPutDocumentLayer buildingDocumentLayer)
		{
			// Custom code to update the building

			return location.GetBuildingDDD(buildingPatches.Key).Data.ToResponseData();
		}
	}
}