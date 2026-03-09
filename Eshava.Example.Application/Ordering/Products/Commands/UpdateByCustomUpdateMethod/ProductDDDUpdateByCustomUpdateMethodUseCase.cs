using System.Collections.Generic;
using System.Threading.Tasks;
using Eshava.Core.Models;
using Eshava.DomainDrivenDesign.Domain.Models;
using Eshava.Example.Domain.Ordering.ProductFeature;

namespace Eshava.Example.Application.Ordering.Products.Commands.UpdateByCustomUpdateMethod
{
	internal partial class ProductDDDUpdateByCustomUpdateMethodUseCase
	{
		private async Task<ResponseData<bool>> UpdateProductAsync(ProductDDD product, IList<Patch<ProductDDD>> patches)
		{
			var constraintsResult = await CheckValidationConstraintsAsync(product, patches);
			if (constraintsResult.IsFaulty)
			{
				return constraintsResult.ConvertTo<bool>();
			}

			// Custom code to update the product

			return product.Patch(patches);
		}
	}
}