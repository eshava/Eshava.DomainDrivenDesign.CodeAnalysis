namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure
{
	public enum InfrastructureAlternativeClassType
	{
		None = 0,
		DomainModelRepository = 1,
		ChildDomainModelRepository = 2,
		QueryRepository = 3,
		ProviderService = 4,
		AggregateProviderService = 5,
		QueryProviderService = 6
	}
}