using Eshava.CodeAnalysis;
using Eshava.CodeAnalysis.Extensions;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Constants
{
	public static class ApplicationNames
	{
		public static class Engines
		{
			public const string WHERE = "whereQueryEngine";
			public const string WHERETYPE = "IWhereQueryEngine";

			public const string SORTING = "sortingQueryEngine";
			public const string SORTINGTYPE = "ISortingQueryEngine";

			public const string VALIDATIONRULE = "validationConfiguration";
			public const string VALIDATIONRULETYPE = "IValidationRuleEngine";

			public const string VALIDATIONENGINE = "validationEngine";
			public const string VALIDATIONENGINETYPE = "IValidationEngine";

			public static NameAndType Where => new(WHERE, WHERETYPE.ToType());
			public static NameAndType Sorting => new(SORTING, SORTINGTYPE.ToType());
			public static NameAndType ValidationRule => new(VALIDATIONRULE, VALIDATIONRULETYPE.ToType());
		}
	}
}