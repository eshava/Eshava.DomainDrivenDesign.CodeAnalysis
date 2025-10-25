using Eshava.Core.Validation;
using Eshava.Core.Validation.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Eshava.Example.Domain.Extensions
{
	public static partial class ServiceCollectionExtensions
	{
		public static IServiceCollection AddDomain(this IServiceCollection services, IConfiguration configuration, IHostEnvironment hostEnvironment)
		{
			return services
				.AddSingleton<IValidationEngine, ValidationEngine>()
				;
		}
	}
}