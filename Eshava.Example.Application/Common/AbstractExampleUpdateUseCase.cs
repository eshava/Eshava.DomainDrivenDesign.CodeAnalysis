using Eshava.Core.Validation.Interfaces;
using Eshava.DomainDrivenDesign.Application.UseCases;
using Eshava.DomainDrivenDesign.Domain.Interfaces;
using Eshava.Example.Application.Settings;
using Microsoft.Extensions.Options;

namespace Eshava.Example.Application.Common
{
	internal class AbstractExampleUpdateUseCase<TDomain, TDto, TIdentifier> : AbstractUpdateUseCase<TDomain, TDto, TIdentifier>
		where TDomain : class, IEntity<TDomain, TIdentifier>
		where TDto : class
		where TIdentifier : struct
	{
		private readonly IOptions<AppSettings> _appSettings;

		public AbstractExampleUpdateUseCase(
			IValidationRuleEngine validationConfiguration,
			IOptions<AppSettings> appSettings
		) : base(validationConfiguration)
		{
			_appSettings = appSettings;
		}
	}
}