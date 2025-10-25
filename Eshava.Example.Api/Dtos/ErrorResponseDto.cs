using System.Collections.Generic;
using Eshava.Core.Models;

namespace Eshava.Example.Api.Dtos
{
	internal class ErrorResponseDto
	{
		public string Message { get; set; }
		public IEnumerable<ValidationError> ValidationErrors { get; set; }
	}
}