using Eshava.DomainDrivenDesign.Application.Settings;

namespace Eshava.Example.Application.Settings
{
	public class ExampleScopedSettings : AbstractScopedSettings
	{
		public ExampleScopedSettings()
		{

		}

		public int ApplicationId { get; set; }
		public int UserId { get; set; }

		public override object GetScopeInformationForLogging()
		{
			return new
			{
				UserId
			};
		}
	}
}