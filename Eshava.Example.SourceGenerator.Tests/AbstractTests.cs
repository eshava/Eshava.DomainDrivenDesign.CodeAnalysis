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
#pragma warning disable RS1035 // Do not use APIs banned for analyzers
			var apiProjectJson = System.IO.File.ReadAllText(@"..\..\..\..\SourceGenerator\api.project.json", System.Text.Encoding.UTF8);
			var apiRoutesOrderingOrderJson = System.IO.File.ReadAllText(@"..\..\..\..\SourceGenerator\api.routes.ordering.orders.json", System.Text.Encoding.UTF8);
			var apiRoutesOrderingOrderPositionJson = System.IO.File.ReadAllText(@"..\..\..\..\SourceGenerator\api.routes.ordering.orderpositions.json", System.Text.Encoding.UTF8);
			var apiRoutesOrderingProductJson = System.IO.File.ReadAllText(@"..\..\..\..\SourceGenerator\api.routes.ordering.products.json", System.Text.Encoding.UTF8);
			
			var applicationProjectJson = System.IO.File.ReadAllText(@"..\..\..\..\SourceGenerator\application.project.json", System.Text.Encoding.UTF8);
			var applicationUseCasesOrderingJson = System.IO.File.ReadAllText(@"..\..\..\..\SourceGenerator\application.usecases.ordering.json", System.Text.Encoding.UTF8);
			var applicationUseCasesOrganizationsJson = System.IO.File.ReadAllText(@"..\..\..\..\SourceGenerator\application.usecases.organizations.json", System.Text.Encoding.UTF8);
			
			var domainProjectJson = System.IO.File.ReadAllText(@"..\..\..\..\SourceGenerator\domain.project.json", System.Text.Encoding.UTF8);
			var domainModelsOrderingJson = System.IO.File.ReadAllText(@"..\..\..\..\SourceGenerator\domain.models.ordering.json", System.Text.Encoding.UTF8);
			var domainModelsOrganizationsJson = System.IO.File.ReadAllText(@"..\..\..\..\SourceGenerator\domain.models.organizations.json", System.Text.Encoding.UTF8);
			
			var infrastructurProjectJson = System.IO.File.ReadAllText(@"..\..\..\..\SourceGenerator\infrastructure.project.json", System.Text.Encoding.UTF8);
			var infrastructurModelsOrderingJson = System.IO.File.ReadAllText(@"..\..\..\..\SourceGenerator\infrastructure.models.ordering.json", System.Text.Encoding.UTF8);
			var infrastructurModelsOrganizationsJson = System.IO.File.ReadAllText(@"..\..\..\..\SourceGenerator\infrastructure.models.organizations.json", System.Text.Encoding.UTF8);
			var infrastructurModelsAccountingJson = System.IO.File.ReadAllText(@"..\..\..\..\SourceGenerator\infrastructure.models.accounting.json", System.Text.Encoding.UTF8);
#pragma warning restore RS1035 // Do not use APIs banned for analyzers

			var apiProjectConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<ApiProject>(apiProjectJson);
			var apiRoutesConfigs = new[]
			{
				Newtonsoft.Json.JsonConvert.DeserializeObject<ApiRoutes>(apiRoutesOrderingOrderJson),
				Newtonsoft.Json.JsonConvert.DeserializeObject<ApiRoutes>(apiRoutesOrderingOrderPositionJson),
				Newtonsoft.Json.JsonConvert.DeserializeObject<ApiRoutes>(apiRoutesOrderingProductJson)
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
	}
}