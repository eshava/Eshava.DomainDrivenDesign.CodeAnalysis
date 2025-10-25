using Eshava.Core.Linq.Interfaces;
using Eshava.DomainDrivenDesign.Application.Dtos;
using Eshava.DomainDrivenDesign.Domain.Enums;
using Eshava.DomainDrivenDesign.Infrastructure.Interfaces;
using Eshava.DomainDrivenDesign.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace Eshava.Example.Infrastructure
{
	internal class AbstractExampleQueryRepository<TData, TIdentifier> : AbstractQueryRepository<TIdentifier>
		where TIdentifier : struct
		where TData : AbstractExampleDatabaseModel<TIdentifier>
	{
		public AbstractExampleQueryRepository(
			IDatabaseSettings databaseSettings,
			ITransformQueryEngine transformQueryEngine,
			ILogger logger
		) : base(databaseSettings, transformQueryEngine, logger)
		{

		}

		protected virtual FilterRequestDto<TData> AddStatusQueryConditions(FilterRequestDto<TData> filterRequest)
		{
			filterRequest.Where.Add(d => d.Status == Status.Active);

			return filterRequest;
		}
	}
}