using System.Collections.Generic;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Api
{
	public class ApiRouteNamespace
	{
		public ApiRouteNamespace()
		{
			Endpoints = [];
		}

		public string Namespace { get; set; }
		public string Name { get; set; }
		public List<ApiRoute> Endpoints { get; set; }
	}
}