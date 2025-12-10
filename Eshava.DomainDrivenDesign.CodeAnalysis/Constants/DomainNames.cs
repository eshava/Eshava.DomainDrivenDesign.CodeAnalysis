using Eshava.CodeAnalysis;
using Eshava.CodeAnalysis.Extensions;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Constants
{
	public static class DomainNames
	{
		public const string ABSTRACTDOMAINMODEL = "AbstractDomainModel";
		public const string ABSTRACTAGGREGATE = "AbstractAggregate";
		public const string ABSTRACTCHILDDOMAINMODEL = "AbstractChildDomainModel";
		public const string ABSTRACTVALUEOBJECT = "AbstractValueObject";

		public const string CALLBACK = "actionCallback";
		public const string CALLBACKFIELD = "_" + CALLBACK;

		public static class VALIDATION
		{
			public const string ENGINE = "validationEngine";
			public const string ENGINETYPE = "IValidationEngine";

			public static NameAndType Parameter => new(ENGINE, ENGINETYPE.ToType());
		}
	}
}