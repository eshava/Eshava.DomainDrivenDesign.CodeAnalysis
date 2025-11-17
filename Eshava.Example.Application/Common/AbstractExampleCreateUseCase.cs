using Eshava.Core.Validation.Interfaces;
using Eshava.DomainDrivenDesign.Application.UseCases;
using Eshava.DomainDrivenDesign.Domain.Interfaces;
using Eshava.Example.Application.Settings;
using Microsoft.Extensions.Options;

namespace Eshava.Example.Application.Common
{
	internal class AbstractExampleCreateUseCase<TDto, TDomain, TIdentifier> : AbstractCreateUseCase<TDto, TDomain, TIdentifier>
		 where TDto : class
		 where TDomain : class, IEntity<TDomain, TIdentifier>
		 where TIdentifier : struct
	{
		private readonly IOptions<AppSettings> _appSettings;

		public AbstractExampleCreateUseCase(
			IValidationRuleEngine validationConfiguration,
			IOptions<AppSettings> appSettings
		) : base(validationConfiguration)
		{
			_appSettings = appSettings;
		}
	}
}