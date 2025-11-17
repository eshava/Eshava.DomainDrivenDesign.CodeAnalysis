using Eshava.DomainDrivenDesign.Domain.Models;
using Eshava.DomainDrivenDesign.Infrastructure.Interfaces;
using Eshava.DomainDrivenDesign.Infrastructure.Interfaces.Repositories;
using Eshava.DomainDrivenDesign.Infrastructure.Providers;
using Eshava.Example.Application.Settings;
using Microsoft.Extensions.Options;

namespace Eshava.Example.Infrastructure
{
	internal class AbstractExampleAggregateInfrastructureProviderService<TDomain, TIdentifier> : AbstractAggregateInfrastructureProvider<TDomain, TIdentifier>
		where TDomain : AbstractAggregate<TDomain, TIdentifier>
		where TIdentifier : struct
	{
		private readonly AppSettings _appSettings;

		protected AbstractExampleAggregateInfrastructureProviderService(
			IDatabaseSettings databaseSettings,
			IAbstractDomainModelRepository<TDomain, TIdentifier> repository,
			IOptions<AppSettings> appSettings
		) : base(databaseSettings, repository)
		{
			_appSettings = appSettings.Value;
		}
	}
}