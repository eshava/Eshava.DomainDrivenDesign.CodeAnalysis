using Eshava.DomainDrivenDesign.Application.UseCases;
using Eshava.DomainDrivenDesign.Domain.Models;
using Eshava.Example.Application.Settings;
using Microsoft.Extensions.Options;
namespace Eshava.Example.Application.Common
{
	internal class AbstractExampleDeactivateUseCase<TDomain, TIdentifier> : AbstractDeactivateUseCase<TDomain, TIdentifier>
		where TDomain : AbstractEntity<TDomain, TIdentifier>
		where TIdentifier : struct
	{
		private readonly IOptions<AppSettings> _appSettings;

		public AbstractExampleDeactivateUseCase(
			IOptions<AppSettings> appSettings
		)
		{
			_appSettings = appSettings;
		}
	}
}