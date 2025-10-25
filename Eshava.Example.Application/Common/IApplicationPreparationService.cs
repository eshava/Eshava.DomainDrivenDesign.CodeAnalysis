using System.Threading.Tasks;
using Eshava.Core.Models;

namespace Eshava.Example.Application.Common
{
	public interface IApplicationPreparationService
	{
		Task<ResponseData<bool>> PrepareAsync(int applicationId);
	}
}