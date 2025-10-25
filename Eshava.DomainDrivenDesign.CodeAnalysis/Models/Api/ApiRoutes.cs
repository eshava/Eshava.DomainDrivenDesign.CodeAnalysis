using System.Collections.Generic;
using System.Linq;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Api
{
	public class ApiRoutes
	{
		public ApiRoutes()
		{
			Routes = new List<ApiRouteNamespace>();
		}
		public List<ApiRouteNamespace> Routes { get; set; }
	}

	public class ApiRouteNamespace
	{
		public ApiRouteNamespace()
		{
			Endpoints = new List<ApiRoute>();
		}
		public string Namespace { get; set; }
		public string Name { get; set; }
		public List<ApiRoute> Endpoints { get; set; }

	}

	public class ApiRoute
	{
		public ApiRoute()
		{
			Parameters = new List<ApiRouteParameter>();
			AuthorizationPolicies = new List<ApiRouteAuthorizationPolicy>();
			ApiRouteEndpointFilters = new List<ApiRouteEndpointFilter>();
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

	public class ApiRouteUseCase
	{
		public string Domain { get; set; }
		public string ClassificationKey { get; set; }
		public string ReferenceModel { get; set; }
		public string UseCaseName { get; set; }
		public string MethodToCall { get; set; }
		public bool IsAsync { get; set; }
	}

	public class ApiRouteEndpointFilter
	{
		public string Name { get; set; }
		public string Using { get; set; }
	}

	public class ApiRouteAuthorizationPolicy
	{
		public bool IsString { get; set; }
		public string Name { get; set; }
		public string Using { get; set; }
	}

	public class ApiRouteParameter
	{
		/// <summary>
		/// Route, Query, Header, Form
		/// </summary>
		public string ParameterType { get; set; }
		/// <summary>
		/// Used in combination with <see cref="ParameterType"/> Query or Header
		/// </summary>
		public string ParameterName { get; set; }

		public string Type { get; set; }
		public string UsingForType { get; set; }
		public string Name { get; set; }

		/// <summary>
		/// Name of the property of the use case request to which the parameter is to be mapped
		/// </summary>
		public string RequestPropertyName { get; set; }

		/// <summary>
		/// If activated, the <see cref="RequestPropertyName"/> will be mapped to the request dto instead to the request itself
		/// </summary>
		public bool MapToDtoProperty { get; set; }
	}
}
