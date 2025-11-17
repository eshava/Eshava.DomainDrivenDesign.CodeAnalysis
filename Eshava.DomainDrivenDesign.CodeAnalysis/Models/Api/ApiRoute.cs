using System.Collections.Generic;
using System.Linq;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Api
{
	public class ApiRoute
	{
		public ApiRoute()
		{
			Parameters = [];
			AuthorizationPolicies = [];
			ApiRouteEndpointFilters = [];
		}

		public string HttpMethod { get; set; }
		public string Route { get; set; }

		public ApiRouteUseCase UseCase { get; set; }
		public List<ApiRouteParameter> Parameters { get; set; }
		public List<ApiRouteAuthorizationPolicy> AuthorizationPolicies { get; set; }
		public List<ApiRouteEndpointFilter> ApiRouteEndpointFilters { get; set; }

		public ApiRoute ConvertToCountRoute()
		{
			return new ApiRoute
			{
				HttpMethod = HttpMethod,
				Route = Route + "/count",
				UseCase = new ApiRouteUseCase
				{
					UseCaseName = UseCase.UseCaseName + "Count",
					ClassificationKey = UseCase.ClassificationKey,
					ReferenceModel = UseCase.ReferenceModel,
					Domain = UseCase.Domain
				},
				Parameters = Parameters
					.Select(p => new ApiRouteParameter
					{
						Name = p.Name,
						ParameterName = p.ParameterName,
						ParameterType = p.ParameterType,
						RequestPropertyName = p.RequestPropertyName,
						Type = p.Type,
						UsingForType = p.UsingForType,
						MapToDtoProperty = p.MapToDtoProperty
					})
					.ToList(),
				AuthorizationPolicies = AuthorizationPolicies
					.Select(p => new ApiRouteAuthorizationPolicy
					{
						IsString = p.IsString,
						Name = p.Name,
						Using = p.Using
					})
					.ToList(),
				ApiRouteEndpointFilters = ApiRouteEndpointFilters
					.Select(filter => new ApiRouteEndpointFilter
					{
						Name = filter.Name,
						Using = filter.Using,
					})
					.ToList()
			};
		}
	}
}