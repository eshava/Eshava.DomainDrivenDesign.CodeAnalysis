using System.Collections.Generic;
using System.Linq;
using Eshava.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Templates
{
	public static class AttributeTemplate
	{
		public static List<AttributeSyntax> CreateAttributes(IEnumerable<AttributeDefinition> attributeDefinitions)
		{
			var attributes = new List<AttributeSyntax>();
			if (!(attributeDefinitions?.Any() ?? false))
			{
				return attributes;
			}

			foreach (var attribute in attributeDefinitions)
			{
				var attributeSyntax = attribute.Name.ToAttribute();

				if (attribute.Parameters?.Any() ?? false)
				{
					var parameterList = new List<AttributeArgumentSyntax>();
					foreach (var parameter in attribute.Parameters)
					{
						AttributeArgumentSyntax argument = null;
						switch (parameter.Type)
						{
							case "bool":
								argument = parameter.Value.ToLiteralBool().ToAttributeArgument();

								break;
							case "int":
								argument = parameter.Value.ToLiteralInt().ToAttributeArgument();

								break;
							case "string":
								argument = parameter.Value.ToLiteralString().ToAttributeArgument();

								break;
							case "constant":
								argument = parameter.Value.ToConstantExpression().ToAttributeArgument();

								break;

							case "enum":
								var enumValue = parameter.Value.Split('.');
								argument = enumValue[0].ToIdentifierName().Access(enumValue[1].ToIdentifierName()).ToAttributeArgument();

								break;
						}

						if (argument is not null)
						{
							if (!parameter.Name.IsNullOrEmpty())
							{
								argument = argument.WithNameColon(parameter.Name.ToColon());
							}

							parameterList.Add(argument);
						}
					}

					attributeSyntax = attributeSyntax.WithArguments(parameterList.ToArray());
				}

				attributes.Add(attributeSyntax);
			}

			return attributes;
		}
	}
}