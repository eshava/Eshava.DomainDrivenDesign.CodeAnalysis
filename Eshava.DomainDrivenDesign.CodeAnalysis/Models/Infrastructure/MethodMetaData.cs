using System.Collections.Generic;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure
{
	public class MethodMetaData
	{
		public MethodMetaData(string className, string interfaceName, IEnumerable<InfrastructureModelPropertyCodeSnippet> codeSnippets)
		{
			ClassName = className;
			InterfaceName = interfaceName;
			CodeSnippets = codeSnippets;
		}

		public string ClassName { get; }
		public string InterfaceName { get; }
		public IEnumerable<InfrastructureModelPropertyCodeSnippet> CodeSnippets { get; }
		public string MethodName { get; private set; }

		public MethodMetaData Clone(string methodName)
		{
			var instance = new MethodMetaData(ClassName, InterfaceName, CodeSnippets)
			{
				MethodName = methodName
			};

			return instance;
		}
	}
}