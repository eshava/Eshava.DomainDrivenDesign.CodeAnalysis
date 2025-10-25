using System;
using System.Collections.Generic;
using Eshava.Core.Linq.Interfaces;
using Eshava.DomainDrivenDesign.Domain.Enums;
using Eshava.DomainDrivenDesign.Domain.Interfaces;
using Eshava.DomainDrivenDesign.Infrastructure.Interfaces;
using Eshava.DomainDrivenDesign.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using Eshava.Example.Application.Settings;

namespace Eshava.Example.Infrastructure
{
	internal abstract class AbstractExampleDomainModelRepository<TDomain, TData, TIdentifier, TScopedSettings> : AbstractDomainModelRepository<TDomain, TData, TIdentifier, TScopedSettings>
		where TDomain : class, IEntity<TDomain, TIdentifier>
		where TData : AbstractExampleDatabaseModel<TIdentifier>, new()
		where TIdentifier : struct
		where TScopedSettings : ExampleScopedSettings
	{
		public AbstractExampleDomainModelRepository(
		   IDatabaseSettings databaseSettings,
		   TScopedSettings scopedSettings,
		   ITransformQueryEngine transformQueryEngine,
		   ILogger logger
		) : base(databaseSettings, scopedSettings, transformQueryEngine, logger)
		{

		}

		protected sealed override void AdjustDatabaseModelForCreate(TData data)
		{
			data.CreatedAtUtc = DateTime.UtcNow;
			data.CreatedByUserId = ScopedSettings.UserId;
			data.ModifiedAtUtc = DateTime.UtcNow;
			data.ModifiedByUserId = ScopedSettings.UserId;
			data.Status = Status.Active;
		}

		protected sealed override void AdjustDatabaseModelForPatch(IDictionary<string, object> changes)
		{
			changes.Add(nameof(AbstractExampleDatabaseModel<TIdentifier>.ModifiedAtUtc), DateTime.UtcNow);
			changes.Add(nameof(AbstractExampleDatabaseModel<TIdentifier>.ModifiedByUserId), ScopedSettings.UserId);
		}
	}
}