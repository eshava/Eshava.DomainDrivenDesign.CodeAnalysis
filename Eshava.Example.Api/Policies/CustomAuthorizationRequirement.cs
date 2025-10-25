using Microsoft.AspNetCore.Authorization;

namespace Eshava.Example.Api.Policies
{
	public class CustomAuthorizationRequirement : IAuthorizationRequirement
	{
		public CustomAuthorizationRequirement(CustomAuthorizationPolicy policy)
		{
			Policy = policy;
			PolicyName = policy.ToString();
		}

		public string PolicyName { get; }
		public CustomAuthorizationPolicy Policy { get; }
	}
}