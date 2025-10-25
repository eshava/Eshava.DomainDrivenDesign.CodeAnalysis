using System;

namespace Eshava.Example.Application.Common.Attributes
{
	[AttributeUsage(AttributeTargets.Class)]
	public class UseCaseClassAttribute : Attribute
	{
		public UseCaseClassAttribute(string name)
		{
			Name = name;
		}

		public string Name { get; }
	}
}