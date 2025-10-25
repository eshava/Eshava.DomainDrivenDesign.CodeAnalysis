using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using System.Text.Json;
using System;

namespace Eshava.Example.SourceGenerator.Extensions
{
	public static class IncrementalGeneratorInitializationContextExtension
	{
		public static IncrementalValueProvider<ImmutableArray<ConfigurationFile>> ReadConfigurationFiles(
			this IncrementalGeneratorInitializationContext context,
			bool loadApi,
			bool loadApplication,
			bool loadDomain,
			bool loadInfrasttructure,
			ConfigurationFileTypes targetAssembly
		)
		{
			// additional files have to be defined in the consumer project
			var acceptedFiles = new List<(ConfigurationFileTypes Type, string Path)>();
			if (loadApi)
			{
				acceptedFiles.Add((ConfigurationFileTypes.ApiProject, System.IO.Path.Combine("SourceGenerator", "api.project.json")));
				acceptedFiles.Add((ConfigurationFileTypes.ApiRoutes, System.IO.Path.Combine("SourceGenerator", "api.routes.ordering.products.json")));
				acceptedFiles.Add((ConfigurationFileTypes.ApiRoutes, System.IO.Path.Combine("SourceGenerator", "api.routes.ordering.orders.json")));
				acceptedFiles.Add((ConfigurationFileTypes.ApiRoutes, System.IO.Path.Combine("SourceGenerator", "api.routes.ordering.orderpositions.json")));
			}

			if (loadApplication)
			{
				acceptedFiles.Add((ConfigurationFileTypes.ApplicationProject, System.IO.Path.Combine("SourceGenerator", "application.project.json")));
				acceptedFiles.Add((ConfigurationFileTypes.ApplicationUseCases, System.IO.Path.Combine("SourceGenerator", "application.usecases.ordering.json")));
				acceptedFiles.Add((ConfigurationFileTypes.ApplicationUseCases, System.IO.Path.Combine("SourceGenerator", "application.usecases.organizations.json")));
			}

			if (loadDomain)
			{
				acceptedFiles.Add((ConfigurationFileTypes.DomainProject, System.IO.Path.Combine("SourceGenerator", "domain.project.json")));
				acceptedFiles.Add((ConfigurationFileTypes.DomainModels, System.IO.Path.Combine("SourceGenerator", "domain.models.ordering.json")));
				acceptedFiles.Add((ConfigurationFileTypes.DomainModels, System.IO.Path.Combine("SourceGenerator", "domain.models.organizations.json")));
			}

			if (loadInfrasttructure)
			{
				acceptedFiles.Add((ConfigurationFileTypes.InfrastructureProject, System.IO.Path.Combine("SourceGenerator", "infrastructure.project.json")));
				acceptedFiles.Add((ConfigurationFileTypes.InfrastructureModels, System.IO.Path.Combine("SourceGenerator", "infrastructure.models.accounting.json")));
				acceptedFiles.Add((ConfigurationFileTypes.InfrastructureModels, System.IO.Path.Combine("SourceGenerator", "infrastructure.models.ordering.json")));
				acceptedFiles.Add((ConfigurationFileTypes.InfrastructureModels, System.IO.Path.Combine("SourceGenerator", "infrastructure.models.organizations.json")));
			}

			switch (targetAssembly)
			{

				case ConfigurationFileTypes.ApiProject:
					acceptedFiles.Add((ConfigurationFileTypes.Activator, System.IO.Path.Combine("SourceGenerator", "api.activator")));

					break;
				case ConfigurationFileTypes.ApplicationProject:
					acceptedFiles.Add((ConfigurationFileTypes.Activator, System.IO.Path.Combine("SourceGenerator", "application.activator")));

					break;
				case ConfigurationFileTypes.DomainProject:
					acceptedFiles.Add((ConfigurationFileTypes.Activator, System.IO.Path.Combine("SourceGenerator", "domain.activator")));

					break;
				case ConfigurationFileTypes.InfrastructureProject:
					acceptedFiles.Add((ConfigurationFileTypes.Activator, System.IO.Path.Combine("SourceGenerator", "infrastructure.activator")));

					break;
			}

			return context
				.AdditionalTextsProvider
				.Where(file => acceptedFiles.Any(af => file.Path.EndsWith(af.Path)))
				.Select((file, cancellationToken) =>
				{
					var match = acceptedFiles.Single(af => file.Path.EndsWith(af.Path));

					return new ConfigurationFile
					{
						Type = match.Type,
						Content = match.Type != ConfigurationFileTypes.Activator
						? file.GetText(cancellationToken).ToString()
						: null
					};
				})
				.Collect();
		}
	}

	public class ConfigurationFile
	{
		public ConfigurationFileTypes Type { get; set; }
		public string Content { get; set; }

		public T Parse<T>() where T : class
		{
			try
			{

				return JsonSerializer.Deserialize<T>(Content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
			}
			catch (Exception ex)
			{
				return null;
			}
		}
	}

	public enum ConfigurationFileTypes
	{
		None = 0,
		ApiProject = 1,
		ApiRoutes = 2,
		ApplicationProject = 3,
		ApplicationUseCases = 4,
		DomainProject = 5,
		DomainModels = 6,
		InfrastructureProject = 7,
		InfrastructureModels = 8,
		Activator = 9
	}
}
