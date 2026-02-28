using System.Threading.Tasks;
using Eshava.Core.Extensions;
using Eshava.Core.Models;

namespace Eshava.Example.Application.Organizations.CustomerFeature.Offices.Queries.ExportCSV2
{
	internal class OfficeExportCSV2UseCase : IOfficeExportCSV2UseCase
	{
		public Task<ResponseData<OfficeExportCSV2Response>> ExportCSV2Async(OfficeExportCSV2Request request)
		{
			return new OfficeExportCSV2Response
			{
				Stream = new System.IO.MemoryStream(),
				FileName = "SomeFile",
				ContentType = "text/csv"
			}.ToResponseDataAsync();
		}
	}
}