using System.Threading.Tasks;
using Eshava.Core.Extensions;
using Eshava.Core.Models;

namespace Eshava.Example.Application.Organizations.CustomerFeature.Offices.Commands.WriteStatistics
{
	internal class OfficeWriteStatisticsUseCase : IOfficeWriteStatisticsUseCase
	{
		public Task<ResponseData<OfficeWriteStatisticsResponse>> WriteStatisticsAsync(OfficeWriteStatisticsRequest request)
		{
			return new OfficeWriteStatisticsResponse
			{
				StatisticItems = 0
			}.ToResponseDataAsync();
		}
	}
}