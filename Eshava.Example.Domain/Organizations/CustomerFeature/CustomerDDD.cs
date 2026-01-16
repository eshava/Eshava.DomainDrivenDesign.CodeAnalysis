using System;
using System.Collections.Generic;
using System.Linq;
using Eshava.Core.Extensions;
using Eshava.Core.Models;
using Eshava.DomainDrivenDesign.Domain.Models;

namespace Eshava.Example.Domain.Organizations.CustomerFeature
{
	public partial class CustomerDDD
	{
		protected override ResponseData<bool> Create(IList<Patch<CustomerDDD>> patches)
		{
			patches.Add(GetIncreaseVersionPatch());

			return base.Create(patches);
		}

		protected override ResponseData<bool> Update(IList<Patch<CustomerDDD>> patches)
		{
			if (!IsPropertyChanged(nameof(MetaData)))
			{
				patches.Add(GetIncreaseVersionPatch());
			}

			return Update(patches);
		}

		protected override ResponseData<bool> Validate()
		{
			// Check if the are multiple offices with the same name
			var duplicatedOffices = OfficeDDDs.GroupBy(o => o.Name).Where(o => o.Count() > 1).ToList();
			if (duplicatedOffices.Count > 0)
			{
				return ResponseData<bool>.CreateInvalidDataResponse()
					.AddValidationError("Office.Name", "DuplicateError", duplicatedOffices.Select(o => o.Key).ToList());
			}

			return base.Validate();
		}

		protected ResponseData<bool> CreatedOrChangedOfficeDDD(OfficeDDD office)
		{
			// Trigger customer validation, if there are customer/office validation rules
			var validationResult = Validate();
			if (validationResult.IsFaulty)
			{
				return validationResult;
			}

			IncreaseVersion();

			return validationResult;
		}

		protected ResponseData<bool> CreatedOrChangedBillingOfficeDDD(BillingOfficeDDD office)
		{
			// Trigger customer validation, if there are customer/office validation rules
			var validationResult = Validate();
			if (validationResult.IsFaulty)
			{
				return validationResult;
			}

			IncreaseVersion();

			return true.ToResponseData();
		}

		private void IncreaseVersion()
		{
			if (IsPropertyChanged(nameof(MetaData)))
			{
				return;
			}

			ApplyPatches([GetIncreaseVersionPatch()]);
		}

		private Patch<CustomerDDD> GetIncreaseVersionPatch()
		{
			MetaDataVO metaData;
			if (MetaData is null)
			{
				metaData = new MetaDataVO(1, [DateTime.UtcNow]);
			}
			else
			{
				var version = MetaData.Version + 1;
				var timestampes = new List<DateTime>(MetaData.Timestamps)
				{
					DateTime.UtcNow
				};

				metaData = new MetaDataVO(version, timestampes);
			}

			return Patch<CustomerDDD>.Create(p => p.MetaData, metaData);
		}
	}
}