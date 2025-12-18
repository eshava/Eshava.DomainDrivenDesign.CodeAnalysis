using System.Collections.Generic;
using System.Linq;
using Eshava.Core.Extensions;
using Eshava.Core.Models;

namespace Eshava.Example.Domain.Organizations.CustomerFeature
{
	public partial class AddressVO
	{
		public override IEnumerable<ValidationError> Validate()
		{
			if (!StreetNumber.IsNullOrEmpty())
			{
				var streetNumberParts = StreetNumber.ToCharArray();
				var isValid = streetNumberParts.All(c => System.Char.IsDigit(c) || c == '-' || c == ' ');
				if (!isValid)
				{
					return [new ValidationError
					{
						PropertyName = nameof(StreetNumber),
						ErrorType = "InvalidFormat",
						Value = StreetNumber
					}];
				}
			}

			return base.Validate();
		}
	}
}