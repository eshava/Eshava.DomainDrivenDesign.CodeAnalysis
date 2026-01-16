using Eshava.Core.Extensions;
using Eshava.DomainDrivenDesign.Infrastructure.Interfaces;
using Eshava.DomainDrivenDesign.Infrastructure.Settings;
using Eshava.DomainDrivenDesign.Infrastructure.Storm;
using Eshava.Example.Infrastructure.Organizations.Customers;
using Eshava.Storm.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Eshava.Example.Infrastructure.Extensions
{
	public static partial class ServiceCollectionExtensions
	{
		public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, IHostEnvironment hostEnvironment)
		{
			var msSqlConnectionString = configuration.GetConnectionString("Default");
			if (!msSqlConnectionString.IsNullOrEmpty())
			{
				services.AddScoped<IDatabaseSettings>(_ => new DatabaseSettings(msSqlConnectionString));
			}

			AddDatabaseConfigurations();
			AddGeneratedProviderAndRepositories(services);
			AddGeneratedDbConfigurations(services);
			AddGeneratedTransformationProfiles();

			return services
				.AddRepositories()
				.AddProviders()
				;
		}

		private static IServiceCollection AddProviders(this IServiceCollection services)
		{
			return services
				;
		}

		private static IServiceCollection AddRepositories(this IServiceCollection services)
		{
			return services
				;
		}

		private static void AddDatabaseConfigurations()
		{
			// TypeHandler
			new DateTimeHandler().AddTypeHandler();
			new DateOnlyHandler().AddTypeHandler();
			new TimeOnlyHandler().AddTypeHandler();
			new MetaDataDataHandler().AddTypeHandler();

#if DEBUG
			Eshava.Storm.Settings.RestrictToRegisteredModels = true;
#endif

			// Register domain models

			// Register db configurations
		}
	}
}