using System.Collections.Generic;
using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models
{
	public class DependencyInjection
	{
		public string Interface { get; set; }
		public string InterfaceUsing { get; set; }

		public string Class { get; set; }
		public string ClassUsing { get; set; }

		public IEnumerable<string> GetUsings()
		{
			if (!InterfaceUsing.IsNullOrEmpty())
			{
				yield return InterfaceUsing;
			}

			if (!ClassUsing.IsNullOrEmpty())
			{
				if (InterfaceUsing.IsNullOrEmpty() || InterfaceUsing != ClassUsing)
				{
					yield return ClassUsing;
				}
			}

			yield break;
		}
	}
}