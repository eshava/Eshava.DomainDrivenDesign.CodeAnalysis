using Microsoft.Extensions.Logging;

namespace Eshava.Example.Application.Logging
{
	public class DummyLoggerProvider : ILoggerProvider
	{
		private DummyLogEngine _logEngine;

		public DummyLoggerProvider()
		{
			Initialize();
		}

		public ILogger CreateLogger(string categoryName)
		{
			return _logEngine;
		}

		public void Dispose()
		{
			_logEngine = null;
		}

		private void Initialize()
		{
			_logEngine = new DummyLogEngine(LogLevel.Information);
		}
	}
}