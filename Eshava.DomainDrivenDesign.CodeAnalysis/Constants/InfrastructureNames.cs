using Eshava.CodeAnalysis;
using Eshava.CodeAnalysis.Extensions;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Constants
{
	public static class InfrastructureNames
	{
		public const string ABSTRACTDATABASEMODEL = "AbstractDatabaseModel"; 
		public const string ABSTRACTDOMAINMODELREPOSITORY = "AbstractDomainModelRepository"; 
		public const string ABSTRACTQUERYREPOSITORY = "AbstractQueryRepository"; 
		public const string ABSTRACTCHILDDOMAINMODELREPOSITORY = "AbstractChildDomainModelRepository";

		public const string INTERFACEDOMAINMODELREPOSITORY = "IAbstractDomainModelRepository"; 
		public const string INTERFACECHILDDOMAINMODELREPOSITORY = "IAbstractChildDomainModelRepository";

		public const string ABSTRACTINFRASTRUCTUREPROVIDER = "AbstractInfrastructureProvider";
		public const string ABSTRACTAGGREGATEINFRASTRUCTUREPROVIDER = "AbstractAggregateInfrastructureProvider";

		public static class Transform
		{
			public const string SETTINGS = "transformQueryEngine";
			public const string SETTINGSTYPE = "ITransformQueryEngine";

			public static NameAndType Parameter => new(SETTINGS, SETTINGSTYPE.ToType());
		}
	}
}