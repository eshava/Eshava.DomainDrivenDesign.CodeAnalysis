using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Eshava.Example.Api.Filters
{
	public class CustomEndpointFilter : IEndpointFilter
	{
		public ValueTask<object> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
		{
			return next(context);
		}
	}
}