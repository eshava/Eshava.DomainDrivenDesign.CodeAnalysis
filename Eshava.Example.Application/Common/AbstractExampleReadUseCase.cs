using Eshava.DomainDrivenDesign.Application.UseCases;
using Eshava.Example.Application.Settings;
using Microsoft.Extensions.Options;

namespace Eshava.Example.Application.Common
{
	internal class AbstractExampleReadUseCase<TRequest, TDto> : AbstractReadUseCase<TRequest, TDto>
		where TRequest : class
		where TDto : class
	{
		private readonly IOptions<AppSettings> _appSettings;

		public AbstractExampleReadUseCase(
			IOptions<AppSettings> appSettings
		)
		{
			_appSettings = appSettings;
		}
	}
}