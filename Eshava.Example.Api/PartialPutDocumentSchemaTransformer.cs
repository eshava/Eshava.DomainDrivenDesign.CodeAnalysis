using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Eshava.DomainDrivenDesign.Application.PartialPut;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Eshava.Example.Api
{
	public class PartialPutDocumentSchemaTransformer : IOpenApiSchemaTransformer
	{
		private static readonly Type _partialPutDocumentType = typeof(PartialPutDocument<>);

		public async Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
		{
			var partialPutDocumentProperty = context.JsonTypeInfo.Type
				.GetProperties()
				.FirstOrDefault(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == _partialPutDocumentType);

			if (partialPutDocumentProperty is not null)
			{
				var fieldName = partialPutDocumentProperty.Name.ToLower()[0] + partialPutDocumentProperty.Name.Substring(1);
				var fieldSchema = schema.Properties.First(p => p.Key == fieldName);

				var innerType = partialPutDocumentProperty.PropertyType.GenericTypeArguments[0];
				var innerSchema = await context.GetOrCreateSchemaAsync(innerType, null, cancellationToken);

				context.Document.AddComponent(innerSchema.Metadata["x-schema-id"].ToString(), innerSchema);
				schema.Properties.Remove(fieldSchema);
				schema.Properties.Add(fieldName, innerSchema);
			}
		}
	}
}