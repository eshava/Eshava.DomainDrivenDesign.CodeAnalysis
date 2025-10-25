using System.Collections;
using System.Collections.Generic;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Api;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Domain;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure;

namespace Eshava.Example.SourceGenerator.Tests
{
	public class DataContainer
	{
		public ApiProject ApiProject { get; set; }
		public IEnumerable<ApiRoutes> ApiRoutes { get; set; }
		public ApplicationProject ApplicationProject { get; set; }
		public IEnumerable<ApplicationUseCases> ApplicationUseCases { get; set; }
		public DomainProject DomainProject { get; set; }
		public IEnumerable<DomainModels> DomainModels { get; set; }
		public InfrastructureProject InfrastructureProject { get; set; }
		public IEnumerable<InfrastructureModels> InfrastructureModels { get; set; }
	}
}