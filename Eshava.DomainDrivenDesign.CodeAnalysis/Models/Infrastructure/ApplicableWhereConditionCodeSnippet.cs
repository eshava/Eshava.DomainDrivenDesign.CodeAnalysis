namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure
{
	public class ApplicableWhereConditionCodeSnippet
	{
		public ApplicableWhereConditionCodeSnippet(
			InfrastructureModelPropertyCodeSnippet codeSnippet,
			InfrastructureModelProperty property,
			ApplicableInfrastructureModelChainItem modelChain
		)
		{
			CodeSnippet = codeSnippet;
			Property = property;
			ModelChain = modelChain;
		}

		public InfrastructureModelPropertyCodeSnippet CodeSnippet { get; }
		public InfrastructureModelProperty Property { get; }
		public ApplicableInfrastructureModelChainItem ModelChain { get; }
	}
}