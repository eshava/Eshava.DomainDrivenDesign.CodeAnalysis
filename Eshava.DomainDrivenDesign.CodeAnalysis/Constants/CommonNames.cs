using Eshava.CodeAnalysis;
using Eshava.CodeAnalysis.Extensions;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Constants
{
	public static class CommonNames
	{
		public const string SCOPEDSETTINGS = "scopedSettings";
		public const string RESPONSEDATA = "ResponseData";
		public const string MESSAGECONSTANTS = "MessageConstants";

		public static class DatabaseSettings
		{
			public const string SETTINGS = "databaseSettings";
			public const string SETTINGSTYPE = "IDatabaseSettings";

			public static NameAndType Parameter => new(SETTINGS, SETTINGSTYPE.ToType());
		}

		public static class Instrastructure
		{
			public const string PROVIDER = "InfrastructureProviderService";
			public const string QUERYPROVIDER = "QueryInfrastructureProviderService";
			public const string REPOSITORY = "Repository";
			public const string QUERYREPOSITORY = "QueryRepository";
		}

		public static class Application
		{
			public class Abstracts
			{
				public const string CREATEUSECASE = "AbstractCreateUseCase";
				public const string DEACTIVATEUSECASE = "AbstractDeactivateUseCase";
				public const string READUSECASE = "AbstractReadUseCase";
				public const string SEARCHUSECASE = "AbstractSearchUseCase";
				public const string UPDATEUSECASE = "AbstractUpdateUseCase";
			}
		}

		public static class Namespaces
		{
			public const string SYSTEM = "System";
			public const string SYSTEMNET = "System.Net";
			public const string GENERIC = "System.Collections.Generic";
			public const string LINQ = "System.Linq";
			public const string EXPRESSION = "System.Linq.Expressions";
			public const string LOGGING = "Microsoft.Extensions.Logging";
			public const string DEPENDENCYINJECTION = "Microsoft.Extensions.DependencyInjection";
			public const string TASKS = "System.Threading.Tasks";
			public const string NEWTONSOFT = "Newtonsoft.Json";
			public const string JSON = "System.Text.Json.Serialization";
			public static class AspNetCore
			{
				public const string BUILDER = "Microsoft.AspNetCore.Builder";
				public const string HTTP = "Microsoft.AspNetCore.Http";
				public const string MVC = "Microsoft.AspNetCore.Mvc";
			}

			public static class Eshava
			{
				public static class Core
				{
					public const string MODELS = "Eshava.Core.Models";
					public const string EXTENSIONS = "Eshava.Core.Extensions";

					public static class VALIDATION
					{
						public const string INTERFACES = "Eshava.Core.Validation.Interfaces";
					}
					public static class Linq
					{
						public const string ATTRIBUTES = "Eshava.Core.Linq.Attributes";
						public const string ENUMS = "Eshava.Core.Linq.Enums";
						public const string INTERFACES = "Eshava.Core.Linq.Interfaces";
						public const string MODELS = "Eshava.Core.Linq.Models";
						public const string NAME = "Eshava.Core.Linq";
					}
				}

				public static class DomainDrivenDesign
				{
					public static class Application
					{
						public const string DTOS = "Eshava.DomainDrivenDesign.Application.Dtos";
						public const string EXTENSIONS = "Eshava.DomainDrivenDesign.Application.Extensions";
						public const string PARTIALPUT = "Eshava.DomainDrivenDesign.Application.PartialPut";
						public const string USECAES = "Eshava.DomainDrivenDesign.Application.UseCases";

						public class Interfaces
						{
							public const string PROVIDERS = "Eshava.DomainDrivenDesign.Application.Interfaces.Providers";
						}
					}

					public static class Domain
					{
						public const string EXTENSIONS = "Eshava.DomainDrivenDesign.Domain.Extensions";
						public const string MODELS = "Eshava.DomainDrivenDesign.Domain.Models";
						public const string CONSTANTS = "Eshava.DomainDrivenDesign.Domain.Constants";
						public const string ENUMS = "Eshava.DomainDrivenDesign.Domain.Enums";
					}

					public static class Infrastructure
					{
						public const string MODELS = "Eshava.DomainDrivenDesign.Infrastructure.Models";
						public const string REPOSITORIES = "Eshava.DomainDrivenDesign.Infrastructure.Repositories";
						public const string INTERFACES = "Eshava.DomainDrivenDesign.Infrastructure.Interfaces";
						public const string INTERFACESREPOSITORIES = "Eshava.DomainDrivenDesign.Infrastructure.Interfaces.Repositories";
						public const string PROVIDERS = "Eshava.DomainDrivenDesign.Infrastructure.Providers";
					}
				}

				public static class Storm
				{
					public const string NAME = "Eshava.Storm";
					public static class MetaData
					{
						public const string NAME = "Eshava.Storm.MetaData";
						public const string BUILDERS = "Eshava.Storm.MetaData.Builders";
						public const string INTERFACES = "Eshava.Storm.MetaData.Interfaces";
					}

					public static class Linq
					{
						public const string MODELS = "Eshava.Storm.Linq.Models";
					}
				}
			}
		}

		public static class Extensions
		{
			public const string ADDVALIDATIONERROR = "AddValidationError";
			public const string CREATEFAULTYRESPONSE = "CreateFaultyResponse";
			public const string TORESPONSEDATA = "ToResponseData";
			public const string TORESPONSEDATAASYNC = "ToResponseDataAsync";
			public const string TOIENUMERABLERESPONSEDATA = "ToIEnumerableResponseData";
			public const string TOIENUMERABLERESPONSEDATAASYNC = "ToIEnumerableResponseDataAsync";
		}
	}
}