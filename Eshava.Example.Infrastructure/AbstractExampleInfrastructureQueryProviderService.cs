using Eshava.Example.Application.Settings;
using Microsoft.Extensions.Options;

namespace Eshava.Example.Infrastructure
{
	internal class AbstractExampleInfrastructureQueryProviderService
	{
		private readonly AppSettings _appSettings;

		public AbstractExampleInfrastructureQueryProviderService(
			IOptions<AppSettings> appSettings
		)
		{
			_appSettings = appSettings.Value;
		}
	}
}