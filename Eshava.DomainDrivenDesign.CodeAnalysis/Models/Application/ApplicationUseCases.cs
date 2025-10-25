using System.Collections.Generic;
using System.Linq;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application
{
	public class ApplicationUseCases
	{
		public ApplicationUseCases()
		{
			Namespaces = new List<ApplicationUseCaseNamespace>();
		}
		public List<ApplicationUseCaseNamespace> Namespaces { get; set; }
	}

	public class ApplicationUseCaseNamespace
	{
		public ApplicationUseCaseNamespace()
		{
			UseCases = new List<ApplicationUseCase>();
		}

		public string Domain { get; set; }
		public List<ApplicationUseCase> UseCases { get; set; }
	}

	public class ApplicationUseCase
	{
		public ApplicationUseCase()
		{
			AdditionalContructorParameter = new List<ApplicationUseCaseConstructorParameter>();
			Dtos = new List<ApplicationUseCaseDto>();
			ExcludedFromForeignKeyCheck = new List<DomainModelReference>();
			DeactivateBefore = new List<DomainModelReference>();
			Attributes = new List<AttributeDefinition>();
		}

		public ApplicationUseCaseType Type { get; set; }
		public string UseCaseName { get; set; }
		public string AbstractUseCaseClass { get; set; }
		public string ClassificationKey { get; set; }

		/// <summary>
		/// If set, overrides the <see cref="ClassificationKey"/>
		/// </summary>
		public string FeatureName { get; set; }

		/// <summary>
		/// Used only for delete use cases
		/// </summary>
		public string DomainModelReference { get; set; }

		/// <summary>
		/// Property that only exists in Data Model and defines the type identifier property for the domain model
		/// </summary>
		public string DataModelTypeProperty { get; set; }
		/// <summary>
		/// Value of <see cref="DataModelTypeProperty"/>
		/// </summary>
		public string DataModelTypePropertyValue { get; set; }


		/// <summary>
		/// Will be set during analyzing
		/// </summary>
		internal string NamespaceClassificationKey { get; set; }
		/// <summary>
		/// Will be set during analyzing
		/// </summary>
		internal string NamespaceDomainModelReference { get; set; }
		public bool CheckForeignKeyReferencesAutomatically { get; set; }
		public List<DomainModelReference> ExcludedFromForeignKeyCheck { get; set; }
		public List<DomainModelReference> DeactivateBefore { get; set; }
		public List<AttributeDefinition> Attributes { get; set; }
		public bool AddValidationConfigurationMethod { get; set; }
		public bool ValidationConfigurationAsTreeStructure { get; set; }
		public bool SkipUseCaseClass { get; set; }
		public bool SkipInfrastructureProviderServiceMethod { get; set; }
		public bool WarpInTransaction { get; set; }
		public bool ReadAggregateByChildId { get; set; }
		public bool UseCustomGroupDtoMethod { get; set; }

		/// <summary>
		/// Optional
		/// </summary>
		public List<ApplicationUseCaseConstructorParameter> AdditionalContructorParameter { get; set; }
		public List<ApplicationUseCaseDto> Dtos { get; set; }
		public string MainDto { get; set; }

		internal string ClassName => $"{UseCaseReferenceName()}{UseCaseName}UseCase";
		internal string RequestType => $"{UseCaseReferenceName()}{UseCaseName}Request";
		internal string ResponseType => $"{UseCaseReferenceName()}{UseCaseName}Response";
		internal string MethodName => $"{UseCaseName}Async";

		public string GetDomainModelReferenceName()
		{
			switch (Type)
			{
				case ApplicationUseCaseType.Create:
				case ApplicationUseCaseType.Update:
					return (Dtos.FirstOrDefault(dto => dto.Name == MainDto) ?? Dtos.First()).ReferenceModelName;

				case ApplicationUseCaseType.Read:
				case ApplicationUseCaseType.Delete:
					return DomainModelReference;

				default:
					return null;
			}
		}

		public ApplicationUseCase ConvertToCountUseCase()
		{
			return new ApplicationUseCase
			{
				Type = ApplicationUseCaseType.SearchCount,
				UseCaseName = UseCaseName + "Count",
				AbstractUseCaseClass = AbstractUseCaseClass,
				ClassificationKey = ClassificationKey,
				FeatureName = FeatureName,
				NamespaceClassificationKey = NamespaceClassificationKey,
				SkipUseCaseClass = SkipUseCaseClass,
				SkipInfrastructureProviderServiceMethod = SkipInfrastructureProviderServiceMethod,
				AdditionalContructorParameter = AdditionalContructorParameter
					.Select(p => new ApplicationUseCaseConstructorParameter
					{
						Name = p.Name,
						Type = p.Type,
						UsingForType = p.UsingForType
					})
					.ToList(),
				Dtos = Dtos
					.Select(dto => new ApplicationUseCaseDto
					{
						Name = dto.Name,
						ReferenceModelName = dto.ReferenceModelName,
						Properties = dto.Properties
							.Select(p => new ApplicationUseCaseDtoProperty
							{
								Name = p.Name,
								IsEnumerable = p.IsEnumerable,
								IsSearchable = p.IsSearchable,
								IsSortable = p.IsSortable,
								ReferenceProperty = p.ReferenceProperty,
								SearchOperations = p.SearchOperations?.ToList() ?? new List<string>(),
								Type = p.Type,
								UsingForType = p.UsingForType,
								Attributes = p.Attributes
									.Select(a => new AttributeDefinition
									{
										Name = a.Name,
										UsingForType = a.UsingForType,
										Parameters = a.Parameters
											.Select(ap => new AttributeParameter
											{
												Name = ap.Name,
												Type = ap.Type,
												Value = ap.Value
											})
											.ToList()
									})
									.ToList()
							})
							.ToList(),
					})
					.ToList(),
				Attributes = Attributes
				.Select(a => new AttributeDefinition
				{
					Name = a.Name,
					UsingForType = a.UsingForType,
					Parameters = a.Parameters
							.Select(ap => new AttributeParameter
							{
								Name = ap.Name,
								Type = ap.Type,
								Value = ap.Value
							})
							.ToList()
				})
					.ToList()
			};
		}

		public string UseCaseReferenceName()
		{
			switch (Type)
			{
				case ApplicationUseCaseType.Create:
				case ApplicationUseCaseType.Update:
				case ApplicationUseCaseType.Delete:
					return NamespaceDomainModelReference;

				case ApplicationUseCaseType.Read:
				case ApplicationUseCaseType.Search:
				case ApplicationUseCaseType.SearchCount:
				default:
					return NamespaceClassificationKey;
			}
		}
	}

	public class ApplicationUseCaseConstructorParameter
	{
		public string Name { get; set; }
		public string Type { get; set; }
		public string UsingForType { get; set; }
	}

	public class ApplicationUseCaseDto
	{
		public ApplicationUseCaseDto()
		{
			Properties = new List<ApplicationUseCaseDtoProperty>();
		}

		public string Name { get; set; }

		/// <summary>
		/// Domain or data model
		/// </summary>
		public string ReferenceModelName { get; set; }

		/// <summary>
		/// Optional
		/// </summary>
		public List<ApplicationUseCaseDtoProperty> Properties { get; set; }
		public List<ApplicationUseCaseDtoProperty> ValidationRuleProperties { get; set; }
	}

	public class ApplicationUseCaseDtoProperty
	{
		private bool _isVirtualProperty = false;

		public ApplicationUseCaseDtoProperty()
		{
			Attributes = new List<AttributeDefinition>();
			SearchOperations = new List<string>();
		}

		public ApplicationUseCaseDtoProperty(bool isVirtualProperty) : this()
		{
			_isVirtualProperty = isVirtualProperty;
		}

		public string Name { get; set; }
		public string Type { get; set; }
		public string UsingForType { get; set; }
		public bool IsEnumerable { get; set; }
		public bool IsGroupProperty { get; set; }

		public bool? IsSortable { get; set; }
		public bool? IsSearchable { get; set; }
		public List<string> SearchOperations { get; set; }

		public List<AttributeDefinition> Attributes { get; set; }

		/// <summary>
		/// Can be navigation path
		/// e. g.: <see cref="Name"/> is ProductName on dto OrderPosition, than <see cref="ReferencePropertyName"/> is Product.Name
		/// </summary>
		public string ReferenceProperty { get; set; }

		internal bool IsVirtualProperty => _isVirtualProperty;

		internal bool IsNullableType
		{
			get
			{
				return Type.EndsWith("?");
			}
		}

		internal string TypeWithUsing
		{
			get
			{
				return UsingForType.IsNullOrEmpty()
					? Type
					: $"{UsingForType}.{Type}";
			}
		}
	}

	public class DomainModelReference
	{
		public string Domain { get; set; }
		public string Name { get; set; }
	}

	public enum ApplicationUseCaseType
	{
		None = 0,
		Read = 1,
		Search = 2,
		SearchCount = 3,
		Create = 4,
		Update = 5,
		Delete = 6,
		Unique = 7,
		Suggestions = 8
	}
}
