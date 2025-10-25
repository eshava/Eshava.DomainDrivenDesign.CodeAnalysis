using System;
using Eshava.DomainDrivenDesign.Domain.Dtos;
using Microsoft.Extensions.Logging;

namespace Eshava.Example.Application.Logging
{
	internal class DummyLogEngine: ILogger
	{
		private readonly LogLevel _logLevel;

		public DummyLogEngine(LogLevel logLevel)
		{
			_logLevel = logLevel;
		}

		public IDisposable BeginScope<TState>(TState state)
		{
			return null;
		}

		public bool IsEnabled(LogLevel logLevel)
		{
			return (int)_logLevel <= (int)logLevel;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="TState"><see cref="AdditionalInformation"/></typeparam>
		/// <param name="logLevel"></param>
		/// <param name="eventId">Name: <see cref="LogEntry.ApplicationId"/></param>
		/// <param name="state"><see cref="LogEntry.Additional"/></param>
		/// <param name="exception"></param>
		/// <param name="formatter">Not used</param>
		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
		{
			if (!IsEnabled(logLevel))
			{
				return;
			}

			var additionalInformation = state as LogInformationDto;			
		}
	}
}