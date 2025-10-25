namespace Eshava.DomainDrivenDesign.CodeAnalysis.Enums
{
	public enum MethodType
	{
		None = 0,
		Read = 1,
		Search = 2,
		SearchCount = 3,
		Exists = 4,
		IsUnique = 5,
		IsUsedForeignKey = 6,
		ReadAggregateId = 7
	}
}