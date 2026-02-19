using Eshava.DomainDrivenDesign.CodeAnalysis.Enums;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Extensions
{
	public static class EnumExtensions
	{
		public static string Map(this OperationType operantion)
		{
			return operantion switch
			{
				OperationType.Equal => "=",
				OperationType.NotEqual => "!=",
				OperationType.In => "IN",
				_ => "=",
			};
		}
	}
}