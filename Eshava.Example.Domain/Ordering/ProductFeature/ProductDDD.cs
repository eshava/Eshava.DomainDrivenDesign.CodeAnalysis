using System.Collections.Generic;
using Eshava.Core.Extensions;
using Eshava.Core.Models;
using Eshava.DomainDrivenDesign.Domain.Models;

namespace Eshava.Example.Domain.Ordering.ProductFeature
{
	public partial class ProductDDD
	{
		public ResponseData<bool> Patch(IList<Patch<ProductDDD>> patches)
		{
			if ((patches?.Count ?? 0) <= 0)
			{
				return true.ToResponseData();
			}

			// Add custom code here

			var areAllPatchesAllowedResult = AreAllPatchesAllowed(patches);
			if (areAllPatchesAllowedResult.IsFaulty)
			{
				return areAllPatchesAllowedResult;
			}

			return Update(patches);
		}
	}
}