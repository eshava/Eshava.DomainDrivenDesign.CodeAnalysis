using Eshava.Example.Application.Settings;
using Microsoft.Extensions.Options;

namespace Eshava.Example.Application.Common
{
	internal class AbstractExampleUniqueUseCase
	{
		private readonly IOptions<AppSettings> _appSettings;

		public AbstractExampleUniqueUseCase(
			IOptions<AppSettings> appSettings
		)
		{
			_appSettings = appSettings;
		}
	}
}