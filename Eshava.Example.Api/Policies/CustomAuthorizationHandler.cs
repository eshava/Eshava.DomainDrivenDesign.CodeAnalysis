using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Eshava.Example.Api.Policies
{
	public class CustomAuthorizationHandler : IAuthorizationHandler
	{
		public Task HandleAsync(AuthorizationHandlerContext context)
		{
			if (context == null)
			{
				return Task.CompletedTask;
			}

			var pendingRequirements = context.PendingRequirements.OfType<CustomAuthorizationRequirement>().ToList();
			if (pendingRequirements.Any())
			{
				var authorized = pendingRequirements.Any(pr => pr.Policy != CustomAuthorizationPolicy.None);
				if (authorized)
				{
					pendingRequirements.ForEach(context.Succeed);
				}
			}

			return Task.CompletedTask;
		}
	}
}