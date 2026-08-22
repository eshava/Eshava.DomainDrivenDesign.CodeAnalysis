﻿using System.Collections.Generic;
using Eshava.DomainDrivenDesign.CodeAnalysis.Constants;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Domain;
using Eshava.DomainDrivenDesign.CodeAnalysis.Templates.Application;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Eshava.Example.SourceGenerator.Tests
{
	/// <summary>
	/// A dto property takes over the attributes of the domain model property it maps to. Where the
	/// dto tells a json serializer to ignore the property, that takeover has to stop: a property
	/// that is required but can never be deserialized makes the schema generator of
	/// System.Text.Json reject the whole type, and the api documentation of every endpoint using it
	/// cannot be produced any more.
	/// </summary>
	[TestClass]
	public class DtoTemplateAttributeTests
	{
		private const string IGNORED_PROPERTY = "TenantId";
		private const string PLAIN_PROPERTY = "Name";

		[TestMethod]
		public void APropertyTheSerializerIgnoresTakesOverNoDomainModelAttributesTest()
		{
			var attributes = GetAttributesOf(GenerateDto(), IGNORED_PROPERTY);

			attributes.Should().Contain("[JsonIgnore]");
			attributes.Should().NotContain("[Required]");
			attributes.Should().NotContain("[Range(1, Int32.MaxValue)]");
		}

		[TestMethod]
		public void APropertyTheSerializerKeepsStillTakesThemOverTest()
		{
			// The rule is about the contradiction, not about the takeover as such
			var attributes = GetAttributesOf(GenerateDto(), PLAIN_PROPERTY);

			attributes.Should().Contain("[Required]");
		}

		[TestMethod]
		public void AnIgnoredPropertyIsRecognisedWhateverWayItIsWrittenTest()
		{
			// The generator itself writes the attribute fully qualified, a configuration usually
			// writes the type name alone, and either may carry the Attribute suffix
			var spellings = new[]
			{
				CommonNames.Attributes.JSONIGNORE,
				$"{CommonNames.Attributes.JSONIGNORE}{CommonNames.Attributes.SUFFIX}",
				$"{CommonNames.Namespaces.JSON}.{CommonNames.Attributes.JSONIGNORE}",
				$"{CommonNames.Namespaces.NEWTONSOFT}.{CommonNames.Attributes.JSONIGNORE}"
			};

			foreach (var spelling in spellings)
			{
				var attributes = GetAttributesOf(GenerateDto(spelling), IGNORED_PROPERTY);

				attributes.Should().NotContain("[Required]", $"the property is ignored, written as {spelling}");
			}
		}

		private static string GenerateDto(string jsonIgnoreSpelling = null)
		{
			var dtoMap = new ReferenceDtoMap
			{
				Domain = "Organizations",
				DtoName = "CustomerCreateCustomerDto",
				DomainModelName = "Customer",
				Dto = new ApplicationUseCaseDto
				{
					Name = "Customer",
					Properties =
					[
						new ApplicationUseCaseDtoProperty
						{
							Name = PLAIN_PROPERTY,
							Type = "string"
						},
						new ApplicationUseCaseDtoProperty
						{
							Name = IGNORED_PROPERTY,
							Type = "int",
							Attributes =
							[
								new AttributeDefinition
								{
									Name = jsonIgnoreSpelling ?? CommonNames.Attributes.JSONIGNORE,
									UsingForType = CommonNames.Namespaces.JSON
								}
							]
						}
					]
				}
			};

			var domainModel = new ReferenceDomainModelMap
			{
				Domain = "Organizations",
				DomainModel = new DomainModel
				{
					Name = "Customer",
					ClassificationKey = "Customer",
					Properties =
					[
						new DomainModelProperty
						{
							Name = PLAIN_PROPERTY,
							Type = "string",
							Attributes = [Required()]
						},
						new DomainModelProperty
						{
							Name = IGNORED_PROPERTY,
							Type = "int",
							Attributes = [Required(), Range()]
						}
					]
				}
			};

			return DtoTemplate.GetDto(dtoMap, "Eshava.Example.Application.Organizations.Customers.Commands.Create", domainModel, false);
		}

		private static AttributeDefinition Required()
		{
			return new AttributeDefinition
			{
				Name = "Required",
				UsingForType = "System.ComponentModel.DataAnnotations"
			};
		}

		private static AttributeDefinition Range()
		{
			return new AttributeDefinition
			{
				Name = "Range",
				UsingForType = "System.ComponentModel.DataAnnotations",
				Parameters =
				[
					new AttributeParameter { Value = "1" },
					new AttributeParameter { Value = "Int32.MaxValue" }
				]
			};
		}

		/// <summary>
		/// Everything the generated source carries between the accessors of the previous property and
		/// the name of the one asked for - which is exactly that property's attribute block.
		/// </summary>
		private static string GetAttributesOf(string sourceCode, string propertyName)
		{
			const string ACCESSORS = "{ get; set; }";

			var declarationIndex = sourceCode.IndexOf($" {propertyName} ");
			declarationIndex.Should().BeGreaterThan(-1, $"the generated dto has to declare {propertyName}, but it reads: {sourceCode}");

			var previousAccessorsIndex = sourceCode.LastIndexOf(ACCESSORS, declarationIndex);
			var blockStart = previousAccessorsIndex < 0
				? 0
				: previousAccessorsIndex + ACCESSORS.Length
				;

			return sourceCode.Substring(blockStart, declarationIndex - blockStart);
		}
	}
}