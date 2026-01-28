using System.Threading.Tasks;
using Eshava.Core.Extensions;
using Eshava.Core.Models;

namespace Eshava.Example.Application.Organizations.CustomerFeature.Offices.Queries.GenerateOverview
{
	internal class OfficeGenerateOverviewUseCase : IOfficeGenerateOverviewUseCase
	{
		public Task<ResponseData<OfficeGenerateOverviewResponse>> GenerateOverviewAsync(OfficeGenerateOverviewRequest request)
		{
			return new OfficeGenerateOverviewResponse
			{
				Items = [],
				Occurrences = [],
				Summary = new OfficeGenerateOverviewOverviewSummaryDto
				{
					Total = 0
				}
			}.ToResponseDataAsync();
		}
	}
}