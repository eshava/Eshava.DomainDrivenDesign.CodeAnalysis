using Eshava.Example.Application.Settings;
using Microsoft.Extensions.Options;

namespace Eshava.Example.Application.Common
{
	internal class AbstractExampleSuggestionsUseCase
	{
		private readonly IOptions<AppSettings> _appSettings;

		public AbstractExampleSuggestionsUseCase(
			IOptions<AppSettings> appSettings
		) 
		{
			_appSettings = appSettings;
		}
	}
}