using System.Collections.Generic;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Api
{
	public class ApiRoutes
	{
		public ApiRoutes()
		{
			Routes = [];
		}

		public List<ApiRouteNamespace> Routes { get; set; }
	}	
}