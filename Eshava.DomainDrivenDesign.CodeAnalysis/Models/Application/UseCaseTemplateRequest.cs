using System.Collections.Generic;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application
{
	public class UseCaseTemplateRequest
	{
		public string ApplicationProjectNamespace { get; set; }
		public string DomainProjectNamespace { get; set; }
		public string ScopedSettingsClass { get; set; }
		public string ScopedSettingsUsing { get; set; }
		public string UseCaseNamespace { get; set; }
		public string Domain { get; set; }
		public ApplicationUseCase UseCase { get; set; }
		public ReferenceMap DomainModelReferenceMap { get; set; }
		public DtoReferenceMap DtoReferenceMap { get; set; }
		public UseCasesMap UseCasesMap { get; set; }
		public List<UseCaseCodeSnippet> CodeSnippets { get; set; }
		public bool AddAssemblyCommentToFiles { get; set; }
	}
}