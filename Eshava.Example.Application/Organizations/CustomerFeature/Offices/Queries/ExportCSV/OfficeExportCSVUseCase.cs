using System.Threading.Tasks;
using Eshava.Core.Extensions;
using Eshava.Core.Models;

namespace Eshava.Example.Application.Organizations.CustomerFeature.Offices.Queries.ExportCSV
{
	internal class OfficeExportCSVUseCase : IOfficeExportCSVUseCase
	{
		public Task<ResponseData<OfficeExportCSVResponse>> ExportCSVAsync(OfficeExportCSVRequest request)
		{
			return new OfficeExportCSVResponse
			{
				CSV = new Common.FileStreamDto
				{
					Data = new System.IO.MemoryStream(),
					NameOfTheFile = "SomeFile",
					TypeOfTheFileContent = "text/csv"
				}
			}.ToResponseDataAsync();
		}
	}
}