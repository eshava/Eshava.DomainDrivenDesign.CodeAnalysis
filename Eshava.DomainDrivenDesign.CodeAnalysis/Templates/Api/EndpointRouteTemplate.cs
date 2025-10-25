using System.Collections.Generic;
using System.Linq;
using Eshava.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Constants;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models;
using Eshava.DomainDrivenDesign.CodeAnalysis.Models.Api;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Templates.Api
{
	public class EndpointRouteTemplate
	{
		public static string GetRoute(List<ApiRoute> apiRoutes, string className, string routeNamespace, string responseDataExtensionsUsing, UseCasesMap useCasesMap, List<ApiRouteCodeSnippet> codeSnippets, bool addAssemblyCommentToFile)
		{
			var unitInformation = new UnitInformation(className, routeNamespace, addConstructor: false, addAssemblyComment: addAssemblyCommentToFile);
			unitInformation.AddClassModifier(SyntaxKind.InternalKeyword, SyntaxKind.StaticKeyword);
			unitInformation.AddContructorModifier(SyntaxKind.PublicKeyword);

			unitInformation.AddUsing(CommonNames.Namespaces.AspNetCore.BUILDER);
			unitInformation.AddUsing(CommonNames.Namespaces.AspNetCore.HTTP);
			unitInformation.AddUsing(CommonNames.Namespaces.AspNetCore.MVC);
			unitInformation.AddUsing(responseDataExtensionsUsing);
			unitInformation.AddUsing(CommonNames.Namespaces.TASKS);

			var apiRoutesToProcess = new List<(ApiRoute Route, UseCaseMap UseCaseMap, AdditionalApiRouteInformation Additional, List<ApiRouteCodeSnippet> CodeSnippets)>();
			foreach (var apiRoute in apiRoutes)
			{
				if (!useCasesMap.TryGetUseCase(apiRoute.UseCase.Domain, apiRoute.UseCase.ClassificationKey, apiRoute.UseCase.UseCaseName, apiRoute.UseCase.ReferenceModel, out var useCaseMap))
				{
					continue;
				}

				var filteredCodeSnippets = codeSnippets
					.Where(cs => cs.IsApplicable(useCaseMap.UseCase.Type))
					.ToList();

				filteredCodeSnippets.ForEach(cs =>
				{
					foreach (var @using in cs.AdditionalUsings)
					{
						unitInformation.AddUsing(@using);
					}

					cs.Parameters.ForEach(p => unitInformation.AddUsing(p.Using));
				});

				var additional = apiRoute.UseCase.MethodToCall.IsNullOrEmpty()
					? null
					: new AdditionalApiRouteInformation
					{
						IsAsync = apiRoute.UseCase.IsAsync,
						MethodToCall = apiRoute.UseCase.MethodToCall
					};

				unitInformation.AddUsing(useCaseMap.Namespace);
				apiRoutesToProcess.Add((apiRoute, useCaseMap, additional, filteredCodeSnippets));

				if (useCaseMap.UseCase.Type == Models.Application.ApplicationUseCaseType.Search)
				{
					var countApiRoute = apiRoute.ConvertToCountRoute();

					if (!useCasesMap.TryGetUseCase(countApiRoute.UseCase.Domain, countApiRoute.UseCase.ClassificationKey, countApiRoute.UseCase.UseCaseName, countApiRoute.UseCase.ReferenceModel, out var useCaseMapCount))
					{
						continue;
					}

					unitInformation.AddUsing(useCaseMapCount.Namespace);
					apiRoutesToProcess.Add((countApiRoute, useCaseMapCount, additional, filteredCodeSnippets));
				}
			}

			var filters = apiRoutesToProcess.SelectMany(api => api.Route.ApiRouteEndpointFilters).ToList();
			if (filters.Count > 0)
			{
				foreach (var filter in filters)
				{
					unitInformation.AddUsing(filter.Using);
				}
			}

			var parameters = apiRoutesToProcess.SelectMany(api => api.Route.Parameters).ToList();
			if (parameters.Count > 0)
			{
				foreach (var parameter in parameters)
				{
					unitInformation.AddUsing(parameter.UsingForType);
				}
			}

			var policies = apiRoutesToProcess.SelectMany(api => api.Route.AuthorizationPolicies).ToList();
			if (policies.Count > 0)
			{
				foreach (var policy in policies)
				{
					unitInformation.AddUsing(policy.Using);
				}
			}

			unitInformation.AddMethod(GetMapMethod(apiRoutesToProcess));

			foreach (var apiRoute in apiRoutesToProcess)
			{
				unitInformation.AddMethod(GetRouteMethod(apiRoute.Route, apiRoute.UseCaseMap, apiRoute.Additional, apiRoute.CodeSnippets));
			}

			return unitInformation.CreateCodeString();
		}

		private static (string Name, MethodDeclarationSyntax Method) GetMapMethod(List<(ApiRoute Route, UseCaseMap UseCaseMap, AdditionalApiRouteInformation Additional, List<ApiRouteCodeSnippet> CodeSnippets)> apiRoutes)
		{
			var statements = new List<StatementSyntax>();
			var app = "app";

			foreach (var apiRoute in apiRoutes)
			{
				statements.Add(GetMapCall(apiRoute.Route, apiRoute.Additional, app).ToExpressionStatement());
			}

			var methodName = "Map";
			var methodDeclaration = methodName.ToMethod(
				Eshava.CodeAnalysis.SyntaxConstants.Void,
				statements,
				SyntaxKind.PublicKeyword,
				SyntaxKind.StaticKeyword
			);

			methodDeclaration = methodDeclaration
				.WithParameter(
					app
					.ToParameter()
					.WithType("WebApplication".ToType())
				);

			return (methodName, methodDeclaration);
		}

		private static InvocationExpressionSyntax GetMapCall(ApiRoute apiRoute, AdditionalApiRouteInformation additional, string app)
		{
			var routeMethod = GetRouteMethodName(apiRoute.UseCase, apiRoute.HttpMethod.ToLower().ToPropertyName(), false);

			var mapCall = app
				.Access(GetHttpMethod(apiRoute.HttpMethod))
				.Call(apiRoute.Route.ToLiteralArgument(), routeMethod.ToArgument());

			if ((apiRoute.AuthorizationPolicies?.Count ?? 0) > 0)
			{
				foreach (var policy in apiRoute.AuthorizationPolicies)
				{
					var policyName = policy.IsString
						? policy.Name.ToLiteralArgument()
						: policy.Name.ToIdentifierName().Access("ToString").Call().ToArgument();

					mapCall = mapCall.Access("RequireAuthorization").Call(policyName);
				}
			}

			if ((apiRoute.ApiRouteEndpointFilters?.Count ?? 0) > 0)
			{
				foreach (var filter in apiRoute.ApiRouteEndpointFilters)
				{
					mapCall = mapCall.Access("AddEndpointFilter".AsGeneric(filter.Name)).Call();
				}
			}

			return mapCall;
		}

		private static string GetRouteMethodName(ApiRouteUseCase apiRouteUseCase, string httpMethod, bool isUseCaseCall)
		{
			if (apiRouteUseCase?.MethodToCall.IsNullOrEmpty() ?? true)
			{
				return isUseCaseCall
					? $"{apiRouteUseCase.UseCaseName}Async"
					: $"{httpMethod}{apiRouteUseCase.UseCaseName}Async";
			}

			var methodName = apiRouteUseCase.MethodToCall;
			var endWithAsync = methodName.EndsWith("Async");
			if (endWithAsync)
			{
				methodName = methodName.Substring(0, methodName.Length - 5);
			}

			if (!isUseCaseCall)
			{
				methodName += $"For{apiRouteUseCase.UseCaseName}";
			}

			if (apiRouteUseCase.IsAsync)
			{
				methodName += "Async";
			}

			return methodName;
		}

		private static (string Name, MethodDeclarationSyntax Method) GetRouteMethod(ApiRoute apiRoute, UseCaseMap useCaseMap, AdditionalApiRouteInformation additional, List<ApiRouteCodeSnippet> codeSnippets)
		{
			var httpMethod = apiRoute.HttpMethod.ToLower().ToPropertyName();
			var parameters = new List<ParameterSyntax>();
			var statements = new List<StatementSyntax>();
			var alternativeMethodCall = additional is not null && !additional.MethodToCall.IsNullOrEmpty();


			var useCaseInterface = $"I{useCaseMap.UseCase.ClassName}";
			var isAsync = !alternativeMethodCall || (alternativeMethodCall && additional.IsAsync);

			parameters.Add(
				useCaseMap.UseCase.ClassName
				.ToVariableName()
				.ToParameter()
				.WithType(useCaseInterface.ToType())
			);

			var codeSnippetParameters = codeSnippets.SelectMany(cs => cs.Parameters).ToList();
			if (!alternativeMethodCall)
			{
				foreach (var parameter in codeSnippetParameters)
				{
					parameters.Add(
						parameter.Name
						.ToParameter()
						.WithType(parameter.Type.ToType())
					);
				}
			}

			if ((apiRoute.Parameters?.Count ?? 0) > 0)
			{
				foreach (var parameter in apiRoute.Parameters)
				{
					var attributeType = GetParameterAttributeName(parameter.ParameterType);
					var attributes = new List<AttributeSyntax>();
					if (!attributeType.IsNullOrEmpty())
					{
						attributes.AddRange(
							AttributeTemplate.CreateAttributes([
								new AttributeDefinition
								{
									Name = attributeType,
									Parameters = parameter.ParameterName.IsNullOrEmpty()
										? []
										: [new AttributeParameter { Name = "Name", Value = parameter.ParameterName, Type = "string" }]
								}
							])
						);
					}

					parameters.Add(
						parameter.Name
						.EscapeReservedName()
						.ToParameter()
						.WithType(parameter.Type.ToType())
						.WithAttributes(attributes.ToArray())
					);
				}
			}

			var createRequestInstance = useCaseMap.UseCase.Type switch
			{
				Models.Application.ApplicationUseCaseType.Read => true,
				Models.Application.ApplicationUseCaseType.Delete => true,
				Models.Application.ApplicationUseCaseType.Unique when apiRoute.HttpMethod == "GET" => true,
				Models.Application.ApplicationUseCaseType.Suggestions when apiRoute.HttpMethod == "GET" => true,
				_ => false
			};

			var requestParameters = apiRoute.Parameters.Where(p => !p.RequestPropertyName.IsNullOrEmpty()).ToList();
			if (createRequestInstance)
			{
				var properties = requestParameters
					.Select(p =>
						p.RequestPropertyName
						.ToIdentifierName()
						.Assign(p.Name.EscapeReservedName().ToIdentifierName())
					)
					.Concat(
						codeSnippetParameters
							.Select(p =>
								p.RequestPropertyName
								.ToIdentifierName()
								.Assign(p.AssignExpression)
							)
					)
					.ToArray();

				statements.Add(
					"request".ToVariableStatement(
						useCaseMap.UseCase.RequestType
						.ToIdentifierName()
						.ToInstanceWithInitializer(properties)
					)
				);
			}
			else
			{
				if (!alternativeMethodCall)
				{
					parameters.Add(
						"request"
						.ToParameter()
						.WithType(useCaseMap.UseCase.RequestType.ToType())
						.WithAttributes(
							AttributeTemplate.CreateAttributes([
								new AttributeDefinition
								{
									Name = "FromBody"
								}
							])
							.ToArray()
						)
					);

					var requestParameterStatements = new List<StatementSyntax>();
					var requestDtoParameterStatements = new List<StatementSyntax>();

					foreach (var parameter in codeSnippetParameters)
					{
						requestParameterStatements.Add(
							"request"
							.Access(parameter.RequestPropertyName)
							.Assign(parameter.AssignExpression)
							.ToExpressionStatement()
						);
					}

					foreach (var parameter in requestParameters)
					{
						if (parameter.MapToDtoProperty)
						{
							requestDtoParameterStatements.Add(
								"request"
								.Access(useCaseMap.UseCase.ClassificationKey)
								.Access(parameter.RequestPropertyName)
								.Assign(parameter.Name.EscapeReservedName().ToIdentifierName())
								.ToExpressionStatement()
							);
						}
						else
						{
							requestParameterStatements.Add(
								"request"
								.Access(parameter.RequestPropertyName)
								.Assign(parameter.Name.EscapeReservedName().ToIdentifierName())
								.ToExpressionStatement()
							);
						}
					}

					if (requestDtoParameterStatements.Count > 0)
					{
						requestParameterStatements.Add(
							"request"
							.Access(useCaseMap.UseCase.ClassificationKey)
							.IsNotNull()
							.If(
								requestDtoParameterStatements
								.ToArray()
							)
						);
					}

					if (requestParameterStatements.Count > 0)
					{
						statements.Add(
							"request"
							.ToIdentifierName()
							.IsNotNull()
							.If(
								requestParameterStatements
								.ToArray()
							)
						);
					}
				}
			}

			var useCaseCall = useCaseMap.UseCase.ClassName
				.ToVariableName()
				.Access(GetRouteMethodName(apiRoute.UseCase, httpMethod, true));

			useCaseCall = alternativeMethodCall
				? useCaseCall.Call()
				: useCaseCall.Call("request".ToArgument());

			useCaseCall = isAsync
				? useCaseCall.Access("ToResultAsync").Call().Await()
				: useCaseCall.Access("ToResult").Call();

			statements.Add(useCaseCall.Return());

			var methodResultType = isAsync
				? "Task".AsGeneric("IResult")
				: "IResult".ToType();

			var methodModifiers = new List<SyntaxKind>
			{
				SyntaxKind.PrivateKeyword,
				SyntaxKind.StaticKeyword,
			};

			if (isAsync)
			{
				methodModifiers.Add(SyntaxKind.AsyncKeyword);
			}

			var methodName = GetRouteMethodName(apiRoute.UseCase, httpMethod, false);
			var methodDeclaration = methodName.ToMethod(
				methodResultType,
				statements,
				methodModifiers.ToArray()
			);

			return (methodName, methodDeclaration.WithParameter(parameters.ToArray()));
		}

		private static string GetParameterAttributeName(string parameterType)
		{
			return parameterType switch
			{
				"Route" => "FromRoute",
				"Header" => "FromHeader",
				"Form" => "FromForm",
				"Query" => "FromQuery",
				_ => "",
			};
		}

		private static string GetHttpMethod(string httpMethod)
		{
			return httpMethod switch
			{
				"GET" => "MapGet",
				"POST" => "MapPost",
				"PUT" => "MapPut",
				"DELETE" => "MapDelete",
				_ => null
			};
		}

		private class AdditionalApiRouteInformation
		{
			public bool IsAsync { get; set; }
			public string MethodToCall { get; set; }
		}
	}
}
