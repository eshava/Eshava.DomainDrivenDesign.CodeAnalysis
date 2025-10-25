using Eshava.CodeAnalysis;
using Eshava.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Extensions
{
	public static class ExpressionSyntaxExtensions
	{
		public static ExpressionSyntax CreateFaultyResponse(this ExpressionSyntax type, string message, params (string PropertyName, string ErrorType, ExpressionSyntax Value)[] validationErrors)
		{
			return CreateFaultyResponse(type, message.ToLiteralString(), validationErrors);
		}

		public static ExpressionSyntax CreateFaultyResponse(this ExpressionSyntax type, ExpressionSyntax message, params (string PropertyName, string ErrorType, ExpressionSyntax Value)[] validationErrors)
		{
			ExpressionSyntax faultyResult = type
				.Access(Constants.CommonNames.Extensions.CREATEFAULTYRESPONSE)
				.Call(message.ToArgument());

			foreach (var error in validationErrors)
			{
				faultyResult = faultyResult
					.AddValidationError(error.PropertyName, error.ErrorType, error.Value);
			}

			return faultyResult;
		}

		public static ExpressionSyntax AddValidationError(this ExpressionSyntax responseData, string propertyName, string errorType, ExpressionSyntax value)
		{
			var addvalidationExpression = responseData
				.Access(Constants.CommonNames.Extensions.ADDVALIDATIONERROR);

			if (value is null)
			{
				return addvalidationExpression.Call(propertyName.ToLiteralString().ToArgument(), errorType.ToLiteralString().ToArgument());
			}

			return addvalidationExpression.Call(propertyName.ToLiteralString().ToArgument(), errorType.ToLiteralString().ToArgument(), value.ToArgument());
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="result">Expression with the type "ResponseData<T>"</param>
		/// <param name="returnType">If not null, "ConvertTo" will be called on the result</param>
		/// <param name="asTask">If true, the extension call "ToTask" will be added</param>
		/// <returns></returns>
		public static StatementSyntax ToFaultyCheck(this ExpressionSyntax result, TypeSyntax returnType = null, bool asTask = false)
		{
			ExpressionSyntax expression = null;

			if (returnType is null)
			{
				expression = result;
			}
			else
			{
				expression = result
					.Access(
						"ConvertTo"
						.AsGeneric(returnType)
					)
					.Call();
			}

			if (asTask)
			{
				expression = expression
					.Access("ToTask")
					.Call();
			}

			return result
				.Access("IsFaulty")
				.If(expression.Return());
		}

		public static ReturnStatementSyntax Return(this ExpressionSyntax statement, bool asTask)
		{
			return SyntaxHelper.ToReturn(asTask
				? statement.Access("ToTask").Call()
				: statement
			);
		}
	}
}