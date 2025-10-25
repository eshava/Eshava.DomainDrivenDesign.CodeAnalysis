using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure
{
	public class QueryAnalysisItem
	{
		public string ParentDomain { get; set; }
		public string Domain { get; set; }
		public InfrastructureModel ParentDataModel { get; set; }
		public InfrastructureModel DataModel { get; set; }
		public InfrastructureModelPropery ParentProperty { get; set; }
		public InfrastructureModelPropery Property { get; set; }
		public string ParentDtoName { get; set; }
		public string ParentDtoPropertyName { get; set; }
		public string DtoName { get; set; }
		public ApplicationUseCaseDtoProperty DtoProperty { get; set; }
		public bool IsEnumerable { get; set; }
		public bool IsGroupBy { get; set; }
		public bool IsRootModel { get; set; }
		public string TableAliasConstant { get; set; }
		public FieldDeclarationSyntax TableAliasField { get; set; }

		public string GetDataType(string referenceDomain, string referenceType)
		{
			var dataType = DataModel.Name;
			if (referenceType != DataModel.Name)
			{
				dataType = Domain == referenceDomain
				   ? $"{DataModel.ClassificationKey.ToPlural()}.{dataType}"
				   : $"{Domain}.{DataModel.ClassificationKey.ToPlural()}.{dataType}";
			}

			return dataType;
		}
	}
}