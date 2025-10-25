using System;
using Eshava.Core.Linq;
using Eshava.Core.Linq.Interfaces;
using Eshava.Core.Linq.Models;
using Eshava.Core.Validation;
using Eshava.Core.Validation.Interfaces;
using Eshava.Example.Application.Common;
using Eshava.Example.Application.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Eshava.Example.Application.Extensions
{
	public static partial class ServiceCollectionExtensions
	{
		public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration, IHostEnvironment hostEnvironment)
		{
			RegisterSettings(services, configuration);
			RegisterServices(services);
			AddGeneratedUseCases(services);

			return services;
		}

		public static IServiceCollection RegisterHostedServices(this IServiceCollection services, IConfiguration configuration)
		{
			var appSettings = configuration.GetSection("AppSettings").Get<AppSettings>();
			
			//services.AddHostedService<DummyHostedService>();

			return services;
		}

		private static IServiceCollection RegisterServices(IServiceCollection services)
		{
			services
				.AddSingleton<ITransformQueryEngine, TransformQueryEngine>()
				.AddSingleton<IWhereQueryEngine, WhereQueryEngine>()
				.AddSingleton<ISortingQueryEngine, SortingQueryEngine>()
				.AddSingleton<IValidationRuleEngine, ValidationRuleEngine>()
				;

			services
				.AddScoped<IApplicationPreparationService, ApplicationPreparationService>()
				;

			return services;
		}

		private static void RegisterSettings(IServiceCollection services, IConfiguration configuration)
		{
			services.AddSingleton(new WhereQueryEngineOptions
			{
				UseUtcDateTime = true,
				ContainsSearchSplitBySpace = true
			});

			services.Configure<AppSettings>(configuration.GetSection("AppSettings"));

			services.AddScoped(provider =>
			{
				var settings = provider.GetService<IOptions<AppSettings>>().Value;
				var scopedSettings = new ExampleScopedSettings
				{
					UserId = DateTime.Now.Millisecond
				};

				return scopedSettings;
			});
		}
	}
}