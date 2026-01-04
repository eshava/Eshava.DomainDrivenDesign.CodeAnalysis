using System;
using System.Collections.Generic;
using System.Linq;
using Eshava.Core.Models;

namespace Eshava.Example.Domain.Organizations.CustomerFeature
{
	public partial class MetaDataVO
	{
		public override IEnumerable<ValidationError> Validate()
		{
			if (Version != (Timestamps?.Count() ?? 0))
			{
				return [
					new ValidationError
					{
						PropertyName = nameof(Version),
						ErrorType = "VersionCount",
						Value = Version
					},
					new ValidationError
					{
						PropertyName = nameof(Timestamps),
						ErrorType = "VersionCount",
						Value = Timestamps?.Count() ?? 0
					}
				];
			}

			return base.Validate();
		}
	}
}