using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Api;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Domain;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure;

namespace Eshava.Example.SourceGenerator.Tests
{
	public abstract class AbstractTests
	{
		protected static DataContainer Init()
		{
			var apiProjectJson = ReadConfiguration("api.project.json");
			var apiRoutesOrderingOrderJson = ReadConfiguration("api.routes.ordering.orders.json");
			var apiRoutesOrderingOrderPositionJson = ReadConfiguration("api.routes.ordering.orderpositions.json");
			var apiRoutesOrderingProductJson = ReadConfiguration("api.routes.ordering.products.json");
			var apiRoutesOrganizationsJson = ReadConfiguration("api.routes.organizations.json");
			
			var applicationProjectJson = ReadConfiguration("application.project.json");
			var applicationUseCasesOrderingJson = ReadConfiguration("application.usecases.ordering.json");
			var applicationUseCasesOrganizationsJson = ReadConfiguration("application.usecases.organizations.json");
			
			var domainProjectJson = ReadConfiguration("domain.project.json");
			var domainModelsOrderingJson = ReadConfiguration("domain.models.ordering.json");
			var domainModelsOrganizationsJson = ReadConfiguration("domain.models.organizations.json");
			
			var infrastructurProjectJson = ReadConfiguration("infrastructure.project.json");
			var infrastructurModelsOrderingJson = ReadConfiguration("infrastructure.models.ordering.json");
			var infrastructurModelsOrganizationsJson = ReadConfiguration("infrastructure.models.organizations.json");
			var infrastructurModelsAccountingJson = ReadConfiguration("infrastructure.models.accounting.json");

			var apiProjectConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<ApiProject>(apiProjectJson);
			var apiRoutesConfigs = new[]
			{
				Newtonsoft.Json.JsonConvert.DeserializeObject<ApiRoutes>(apiRoutesOrderingOrderJson),
				Newtonsoft.Json.JsonConvert.DeserializeObject<ApiRoutes>(apiRoutesOrderingOrderPositionJson),
				Newtonsoft.Json.JsonConvert.DeserializeObject<ApiRoutes>(apiRoutesOrderingProductJson),
				Newtonsoft.Json.JsonConvert.DeserializeObject<ApiRoutes>(apiRoutesOrganizationsJson)
			};

			var applicationProjectConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<ApplicationProject>(applicationProjectJson);
			var applicationUseCasesConfigs = new[]
			{
				Newtonsoft.Json.JsonConvert.DeserializeObject<ApplicationUseCases>(applicationUseCasesOrderingJson),
				Newtonsoft.Json.JsonConvert.DeserializeObject<ApplicationUseCases>(applicationUseCasesOrganizationsJson)
			};

			var domainProjectConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<DomainProject>(domainProjectJson);
			var domainModelsConfigs = new[]
			{
				Newtonsoft.Json.JsonConvert.DeserializeObject<DomainModels>(domainModelsOrderingJson),
				Newtonsoft.Json.JsonConvert.DeserializeObject<DomainModels>(domainModelsOrganizationsJson)
			};

			var infrastructureProjectConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<InfrastructureProject>(infrastructurProjectJson);
			var infrastructureModelsConfigs = new[]{
				Newtonsoft.Json.JsonConvert.DeserializeObject<InfrastructureModels>(infrastructurModelsOrderingJson),
				Newtonsoft.Json.JsonConvert.DeserializeObject<InfrastructureModels>(infrastructurModelsOrganizationsJson),
				Newtonsoft.Json.JsonConvert.DeserializeObject<InfrastructureModels>(infrastructurModelsAccountingJson)
			};

			return new DataContainer
			{
				ApiProject = apiProjectConfig,
				ApiRoutes = apiRoutesConfigs,
				ApplicationProject = applicationProjectConfig,
				ApplicationUseCases = applicationUseCasesConfigs,
				DomainProject = domainProjectConfig,
				DomainModels = domainModelsConfigs,
				InfrastructureProject = infrastructureProjectConfig,
				InfrastructureModels = infrastructureModelsConfigs
			};
		}

		/// <summary>
		/// Reads a configuration file of the example from the SourceGenerator folder, relative to the
		/// test assembly. Built with Path.Combine so the tests run on Windows and Linux alike - a
		/// backslash in a literal path is part of the file name on Linux.
		/// </summary>
		private static string ReadConfiguration(string fileName)
		{
#pragma warning disable RS1035 // Do not use APIs banned for analyzers
			var path = System.IO.Path.Combine("..", "..", "..", "..", "SourceGenerator", fileName);

			return System.IO.File.ReadAllText(path, System.Text.Encoding.UTF8);
#pragma warning restore RS1035 // Do not use APIs banned for analyzers
		}
	}
}