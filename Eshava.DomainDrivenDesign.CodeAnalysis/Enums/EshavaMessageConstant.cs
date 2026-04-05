using Eshava.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Enums
{
	public enum EshavaMessageConstant
	{
		CreateDataError = 1,
		DeleteDataError = 2,
		Immutable = 3,
		InvalidData = 4,
		NoChanges = 5,
		NotExisting = 6,
		UnexpectedError = 7,
		ReadDataError = 8,
		UpdateDataError = 9,
		AlreadyExisting = 10,
		AutoPatchBlocked = 11,
		StillAssigned = 12
	}

	public static class EshavaMessageConstantExtensions
	{
		public static ExpressionSyntax Map(this EshavaMessageConstant message)
		{
			switch (message)
			{
				case EshavaMessageConstant.CreateDataError:
					return Constants.CommonNames.MESSAGECONSTANTS.Access("CREATEDATAERROR");

				case EshavaMessageConstant.DeleteDataError:
					return Constants.CommonNames.MESSAGECONSTANTS.Access("DELETEDATAERROR");

				case EshavaMessageConstant.Immutable:
					return Constants.CommonNames.MESSAGECONSTANTS.Access("IMMUTABLE");

				case EshavaMessageConstant.InvalidData:
					return Constants.CommonNames.MESSAGECONSTANTS.Access("INVALIDDATA");

				case EshavaMessageConstant.NoChanges:
					return Constants.CommonNames.MESSAGECONSTANTS.Access("NOCHANGES");

				case EshavaMessageConstant.NotExisting:
					return Constants.CommonNames.MESSAGECONSTANTS.Access("NOTEXISTING");

				case EshavaMessageConstant.UpdateDataError:
					return Constants.CommonNames.MESSAGECONSTANTS.Access("UPDATEDATAERROR");

				case EshavaMessageConstant.AlreadyExisting:
					return Constants.CommonNames.MESSAGECONSTANTS.Access("ALREADYEXISTING");

				case EshavaMessageConstant.AutoPatchBlocked:
					return Constants.CommonNames.MESSAGECONSTANTS.Access("AUTOPATCHBLOCKED");

				case EshavaMessageConstant.StillAssigned:
					return Constants.CommonNames.MESSAGECONSTANTS.Access("STILLASSIGNED");

				case EshavaMessageConstant.ReadDataError:
					return Constants.CommonNames.MESSAGECONSTANTS.Access("READDATAERROR");
				case EshavaMessageConstant.UnexpectedError:
				default:
					return Constants.CommonNames.MESSAGECONSTANTS.Access("UNEXPECTEDERROR");
			}
		}
	}
}