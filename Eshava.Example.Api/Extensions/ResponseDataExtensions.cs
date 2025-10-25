using System;
using System.Net;
using System.Threading.Tasks;
using Eshava.Core.Models;
using Eshava.DomainDrivenDesign.Domain.Constants;
using Eshava.Example.Api.Dtos;
using Microsoft.AspNetCore.Http;

namespace Eshava.Example.Api.Extensions
{
	public static class ResponseDataExtensions
	{
		public static async Task<IResult> ToResultAsync<T>(this Task<ResponseData<T>> responseData)
		{
			var response = await responseData;

			return response.ToResult();
		}

		public static IResult ToResult<T>(this ResponseData<T> responseData)
		{
			if (responseData.IsFaulty)
			{
				if (responseData.StatusCode == (int)HttpStatusCode.NotFound || responseData.Message == MessageConstants.NOTEXISTINGERROR)
				{
					return Results.NotFound();
				}

				return Results.Json(
					new ErrorResponseDto
					{
						Message = responseData.Message,
						ValidationErrors = responseData.ValidationErrors
					},
					statusCode: responseData.StatusCode
				);
			}

			if (responseData.StatusCode == (int)HttpStatusCode.Created)
			{
				return Results.Created((Uri)null, responseData.Data);
			}

			if (responseData.StatusCode == (int)HttpStatusCode.NoContent)
			{
				return Results.NoContent();
			}

			return Results.Ok(responseData.Data);
		}
	}
}