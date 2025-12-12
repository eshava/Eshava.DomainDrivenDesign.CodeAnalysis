using System.Collections.Generic;
using System.Linq;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Api;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Domain;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Extensions
{
	public static class IEnumerableExtensions
	{
		public static ApiRoutes Merge(this IEnumerable<ApiRoutes> apiRoutesConfigs)
		{
			var apiRoutes = new ApiRoutes();

			if (!(apiRoutesConfigs?.Any() ?? false))
			{
				return apiRoutes;
			}

			foreach (var apiRoutesConfig in apiRoutesConfigs)
			{
				foreach (var route in apiRoutesConfig.Routes)
				{
					var existingRoute = apiRoutes.Routes.FirstOrDefault(r => r.Namespace == route.Namespace && r.Name == route.Name);
					if (existingRoute is null)
					{
						apiRoutes.Routes.Add(route);

						continue;
					}

					foreach (var endpoint in route.Endpoints)
					{
						var existingEndpoint = existingRoute.Endpoints.FirstOrDefault(uc => uc.HttpMethod == endpoint.HttpMethod && uc.Route == endpoint.Route && uc.UseCase.UseCaseName == endpoint.UseCase.UseCaseName);
						if (existingEndpoint is not null)
						{
							continue;
						}

						existingRoute.Endpoints.Add(endpoint);
					}
				}
			}

			return apiRoutes;
		}

		public static ApplicationUseCases Merge(this IEnumerable<ApplicationUseCases> applicationUseCasesConfigs)
		{
			var applicationUseCases = new ApplicationUseCases();

			if (!(applicationUseCasesConfigs?.Any() ?? false))
			{
				return applicationUseCases;
			}

			foreach (var applicationUseCasesConfig in applicationUseCasesConfigs)
			{
				foreach (var @namespace in applicationUseCasesConfig.Namespaces)
				{
					var existingNamespace = applicationUseCases.Namespaces.FirstOrDefault(ns => ns.Domain == @namespace.Domain);
					if (existingNamespace is null)
					{
						applicationUseCases.Namespaces.Add(@namespace);

						continue;
					}

					foreach (var useCase in @namespace.UseCases)
					{
						var referenceModelName = GetReferenceModelName(useCase);

						var existingUseCase = existingNamespace.UseCases
							.FirstOrDefault(uc => uc.UseCaseName == useCase.UseCaseName
								&& uc.ClassificationKey == useCase.ClassificationKey
								&& uc.FeatureName == useCase.FeatureName
								&& GetReferenceModelName(uc) == referenceModelName
							);

						if (existingUseCase is not null)
						{
							continue;
						}

						existingNamespace.UseCases.Add(useCase);
					}
				}
			}

			return applicationUseCases;
		}

		public static DomainModels Merge(this IEnumerable<DomainModels> domainModelsConfigs)
		{
			var domainModels = new DomainModels();

			if (!(domainModelsConfigs?.Any() ?? false))
			{
				return domainModels;
			}

			foreach (var domainModelsConfig in domainModelsConfigs)
			{
				foreach (var @namespace in domainModelsConfig.Namespaces)
				{
					var existingNamespace = domainModels.Namespaces.FirstOrDefault(ns => ns.Domain == @namespace.Domain);
					if (existingNamespace is null)
					{
						domainModels.Namespaces.Add(@namespace);

						continue;
					}

					foreach (var model in @namespace.Models)
					{
						var existingModel = existingNamespace.Models.FirstOrDefault(m => m.Name == model.Name && m.FeatureName == model.FeatureName);
						if (existingModel is not null)
						{
							continue;
						}

						existingNamespace.Models.Add(model);
					}

					foreach (var enumeration in @namespace.Enumerations)
					{
						var existingEnumeration = existingNamespace.Enumerations.FirstOrDefault(e => e.Name == enumeration.Name && e.FeatureName == enumeration.FeatureName);
						if (existingEnumeration is not null)
						{
							continue;
						}

						existingNamespace.Enumerations.Add(enumeration);
					}
				}
			}

			return domainModels;
		}

		public static InfrastructureModels Merge(this IEnumerable<InfrastructureModels> infrastructureModelsConfigs)
		{
			var infrastructureModels = new InfrastructureModels();

			if (!(infrastructureModelsConfigs?.Any() ?? false))
			{
				return infrastructureModels;
			}

			foreach (var infrastructureModelsConfig in infrastructureModelsConfigs)
			{
				foreach (var @namespace in infrastructureModelsConfig.Namespaces)
				{
					var existingNamespace = infrastructureModels.Namespaces.FirstOrDefault(ns => ns.Domain == @namespace.Domain);
					if (existingNamespace is null)
					{
						infrastructureModels.Namespaces.Add(@namespace);

						continue;
					}

					foreach (var model in @namespace.Models)
					{
						var existingModel = existingNamespace.Models.FirstOrDefault(m => m.Name == model.Name);
						if (existingModel is not null)
						{
							continue;
						}

						existingNamespace.Models.Add(model);
					}
				}
			}

			return infrastructureModels;
		}

		public static string GetReferenceModelName(ApplicationUseCase useCase)
		{
			switch (useCase.Type)
			{
				case ApplicationUseCaseType.Read:
				case ApplicationUseCaseType.Search:
				case ApplicationUseCaseType.SearchCount:
				case ApplicationUseCaseType.Unique:
				case ApplicationUseCaseType.Suggestions:
				case ApplicationUseCaseType.Create:
				case ApplicationUseCaseType.Update:

					return useCase.Dtos.First().ReferenceModelName;
				case ApplicationUseCaseType.Delete:
					return useCase.DomainModelReference;

				default:
					return null;
			}
		}

		/// <summary>
		/// Create a ";" statement with the comments above it
		/// </summary>
		/// <param name="comments">Contains only the comment text without "//"</param>
		/// <returns></returns>
		public static StatementSyntax CreateCommentStatement(this IEnumerable<string> comments)
		{
			var triviaList = new List<SyntaxTrivia>();
			foreach (var comment in comments)
			{
				triviaList.Add(SyntaxFactory.Comment("// " + comment));
				triviaList.Add(SyntaxFactory.LineFeed);
			}

			var emptyStatement = SyntaxFactory.EmptyStatement();
			var semicolonToken = emptyStatement.SemicolonToken;
			semicolonToken = semicolonToken.WithLeadingTrivia(SyntaxFactory.TriviaList(triviaList.ToArray()));

			return emptyStatement.WithSemicolonToken(semicolonToken);
		}
	}
}