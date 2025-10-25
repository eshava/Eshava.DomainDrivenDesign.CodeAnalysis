using System.Collections.Generic;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Factories
{
	public class FactoryResult
	{
		public FactoryResult()
		{
			SourceCode = new List<(string SourceName, string SourceCode)>();
		}

		public List<(string SourceName, string SourceCode)> SourceCode { get; set; }

		public void AddSource(string sourceName, string sourceCode)
		{
			SourceCode.Add((sourceName, sourceCode));
		}
	}
}