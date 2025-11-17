namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Api
{
	public class ApiRouteParameter
	{
		/// <summary>
		/// Route, Query, Header, Form
		/// </summary>
		public string ParameterType { get; set; }
		/// <summary>
		/// Used in combination with <see cref="ParameterType"/> Query or Header
		/// </summary>
		public string ParameterName { get; set; }

		public string Type { get; set; }
		public string UsingForType { get; set; }
		public string Name { get; set; }

		/// <summary>
		/// Name of the property of the use case request to which the parameter is to be mapped
		/// </summary>
		public string RequestPropertyName { get; set; }

		/// <summary>
		/// If activated, the <see cref="RequestPropertyName"/> will be mapped to the request dto instead to the request itself
		/// </summary>
		public bool MapToDtoProperty { get; set; }
	}
}