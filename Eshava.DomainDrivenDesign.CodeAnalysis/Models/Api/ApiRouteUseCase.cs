namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Api
{
	public class ApiRouteUseCase
	{
		public string Domain { get; set; }
		public string ClassificationKey { get; set; }
		public string ReferenceModel { get; set; }
		public string UseCaseName { get; set; }
		public string MethodToCall { get; set; }
		public bool IsAsync { get; set; }
	}
}