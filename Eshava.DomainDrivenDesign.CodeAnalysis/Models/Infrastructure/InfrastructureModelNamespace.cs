using System.Collections.Generic;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure
{
	public class InfrastructureModelNamespace
	{
		public InfrastructureModelNamespace()
		{
			Models = [];
		}

		public string Domain { get; set; }
		public string DatabaseSchema { get; set; }
		public string DatabaseSettingsInterface { get; set; }
		public string DatabaseSettingsInterfaceUsing { get; set; }
		public List<InfrastructureModel> Models { get; set; }
	}
}