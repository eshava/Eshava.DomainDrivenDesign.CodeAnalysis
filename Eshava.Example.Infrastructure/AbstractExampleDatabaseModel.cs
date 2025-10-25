using System;
using Eshava.DomainDrivenDesign.Domain.Enums;
using Eshava.DomainDrivenDesign.Infrastructure.Models;

namespace Eshava.Example.Infrastructure
{
	internal class AbstractExampleDatabaseModel<TIdentifier> : AbstractDatabaseModel<TIdentifier>
		where TIdentifier : struct
	{
		public DateTime CreatedAtUtc { get; set; }
		public DateTime ModifiedAtUtc { get; set; }
		public int CreatedByUserId { get; set; }
		public int ModifiedByUserId { get; set; }
		public Status Status { get; set; }
	}
}