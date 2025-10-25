using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Eshava.Core.Models;
using Eshava.DomainDrivenDesign.Application.PartialPut;
using Eshava.DomainDrivenDesign.Domain.Constants;
using Eshava.DomainDrivenDesign.Domain.Extensions;
using Eshava.Example.Api.Dtos;
using Eshava.Example.Application.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Eshava.Example.Api.Middleware
{
	public class LogExceptionMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly ILogger<LogExceptionMiddleware> _logger;
		private readonly AppSettings _appSettings;
		
		public LogExceptionMiddleware(RequestDelegate next, ILogger<LogExceptionMiddleware> logger, IOptions<AppSettings> appSettings)
		{
			_next = next;
			_logger = logger;
			_appSettings = appSettings.Value;
		}

		public async Task InvokeAsync(HttpContext context)
		{
			try
			{
				if (context.Request.ContentLength.HasValue && context.Request.ContentLength.Value > 0)
				{
					context.Request.EnableBuffering();
				}

				await _next(context);
			}
#pragma warning disable CS0168 // Variable is declared but never used
			catch (TaskCanceledException exception)
#pragma warning restore CS0168 // Variable is declared but never used
			{

			}
			catch (Exception exception)
			{
				var scopedSettings = new ExampleScopedSettings
				{
					UserId = 0
				};

				ErrorResponseDto errorResponse;
				if (exception is PartialPutDocumentConverterNewtonsoftJsonException converterException)
				{
					_logger.LogError(this, scopedSettings, "Could not convert put patch document", exception, additional: new
					{
						Type = converterException.ObjectType?.Name,
						Object = converterException.Value
					});

					errorResponse = CreateErrorResponse(converterException, _appSettings);
				}
				else
				{
					_logger.LogError(this, scopedSettings, "An unexpected error occurred", exception);

					errorResponse = CreateErrorResponse();
				}

				await HandleExceptionAsync(context, errorResponse);
			}
		}

		private static ErrorResponseDto CreateErrorResponse()
		{
			return new ErrorResponseDto
			{
				Message = MessageConstants.UNEXPECTEDERROR
			};
		}

		private static ErrorResponseDto CreateErrorResponse(PartialPutDocumentConverterNewtonsoftJsonException converterException, AppSettings appSettings)
		{
			List<ValidationError> validationErrors = null;
			if (!appSettings.IsProduction)
			{
				validationErrors = new List<ValidationError>
				{
					new ValidationError
					{
						PropertyName = converterException.Message,
						Value = converterException.StackTrace
					},
					new ValidationError
					{
						PropertyName = converterException.InnerException?.Message ?? "No InnerException",
						Value = converterException.InnerException?.StackTrace
					}
				};
			}

			return new ErrorResponseDto
			{
				Message = $"Could not process {converterException.ObjectType?.Name ?? "object"}",
				ValidationErrors = validationErrors
			};
		}

		private async Task HandleExceptionAsync(HttpContext context, ErrorResponseDto errorResponse)
		{
			var result = Results.Json(
				errorResponse,
				statusCode: (int)HttpStatusCode.InternalServerError
			);

			await result.ExecuteAsync(context);
		}
	}
}