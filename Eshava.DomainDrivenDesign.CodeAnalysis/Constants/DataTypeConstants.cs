using System.Collections.Generic;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Constants
{
	public static class DataTypeConstants
	{
		public static readonly HashSet<string> NotNullableTypes =
		[
			"byte",
			"short",
			"int",
			"uint",
			"long",
			"ulong",
			"decimal",
			"Single",
			"double",
			"float",
			"DateTime",
			"bool"
		];

		public static readonly HashSet<string> NumerableTypes =
		[
			"IEnumerable<",
			"List<",
			"Collection<",
			"HashSet<",
			"Dictionary<",
		];		
	}
}