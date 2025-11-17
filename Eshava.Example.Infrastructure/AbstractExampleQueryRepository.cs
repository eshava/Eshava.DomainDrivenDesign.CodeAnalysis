using Eshava.Core.Linq.Interfaces;
using Eshava.DomainDrivenDesign.Application.Dtos;
using Eshava.DomainDrivenDesign.Domain.Enums;
using Eshava.DomainDrivenDesign.Infrastructure.Interfaces;
using Eshava.DomainDrivenDesign.Infrastructure.Repositories;
using Eshava.Example.Application.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Eshava.Example.Infrastructure
{
	internal class AbstractExampleQueryRepository : AbstractQueryRepository
	{
		private readonly ExampleScopedSettings _scopedSettings;
		private readonly AppSettings _appSettings;

		public AbstractExampleQueryRepository(
			ExampleScopedSettings scopedSettings,
			IDatabaseSettings databaseSettings,
			ITransformQueryEngine transformQueryEngine,
			IOptions<AppSettings> appSettings,
			ILogger logger
		) : base(databaseSettings, transformQueryEngine, logger)
		{
			_scopedSettings = scopedSettings;
			_appSettings = appSettings.Value;
		}

		protected virtual FilterRequestDto<TData> AddStatusQueryConditions<TData, TIdentifier>(FilterRequestDto<TData> filterRequest)
			where TIdentifier : struct
			where TData : AbstractExampleDatabaseModel<TIdentifier>
		{
			filterRequest.Where.Add(d => d.Status == Status.Active);

			return filterRequest;
		}
	}
}