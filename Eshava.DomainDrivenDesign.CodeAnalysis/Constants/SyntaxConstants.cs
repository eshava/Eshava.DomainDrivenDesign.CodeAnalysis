using Eshava.CodeAnalysis;
using Eshava.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Constants
{
	public static class SyntaxConstants
	{
		public static TypeSyntax ResponseDataBool => SyntaxHelper.CreateGenericName(CommonNames.RESPONSEDATA, Eshava.CodeAnalysis.SyntaxConstants.Bool);
		public static TypeSyntax TaskResponseDataBool => "Task".AsGeneric(SyntaxConstants.ResponseDataBool);
	}
}