using System.Collections.Generic;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Infrastructure
{
	public class ApplicableInfrastructureModelChainItem
	{
		public ApplicableInfrastructureModelChainItem(string domain, InfrastructureModel model, ApplicableInfrastructureModelChainItem parent)
		{
			Domain = domain;
			Model = model;
			Parent = parent;
		}

		public string Domain { get; }
		public InfrastructureModel Model { get; }
		public ApplicableInfrastructureModelChainItem Parent { get; }

		public IEnumerable<ApplicableInfrastructureModelChainItem> GetItemsFromRoot()
		{
			var items = new Stack<ApplicableInfrastructureModelChainItem>();
			var currentItem = this;
			while (currentItem is not null)
			{
				items.Push(currentItem);
				currentItem = currentItem.Parent;
			}

			return items;
		}
	}
}