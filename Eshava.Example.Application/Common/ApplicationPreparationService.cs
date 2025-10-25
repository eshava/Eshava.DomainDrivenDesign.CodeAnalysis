using System.Threading.Tasks;
using Eshava.Core.Extensions;
using Eshava.Core.Models;

namespace Eshava.Example.Application.Common
{
	public class ApplicationPreparationService : IApplicationPreparationService
	{
		public Task<ResponseData<bool>> PrepareAsync(int applicationId)
		{
			return true.ToResponseDataAsync();
		}
	}
}