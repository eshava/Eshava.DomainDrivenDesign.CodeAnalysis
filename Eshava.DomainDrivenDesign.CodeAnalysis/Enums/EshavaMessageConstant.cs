using Eshava.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Enums
{
	public enum EshavaMessageConstant
	{
		CreateDataError = 1,
		DeleteDataError = 2,
		ImmutableError = 3,
		InvalidDataError = 4,
		NoChangesError = 5,
		NotExistingError = 6,
		UnexpectedError = 7,
		ReadDataError = 8,
		UpdateDataError = 9,
		AlreadyExisting = 10,
		AutoPatchBlocked = 11,
		NotExisting = 12,
		StillAssigned = 13
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

				case EshavaMessageConstant.ImmutableError:
					return Constants.CommonNames.MESSAGECONSTANTS.Access("IMMUTABLEERROR");

				case EshavaMessageConstant.InvalidDataError:
					return Constants.CommonNames.MESSAGECONSTANTS.Access("INVALIDDATAERROR");

				case EshavaMessageConstant.NoChangesError:
					return Constants.CommonNames.MESSAGECONSTANTS.Access("NOCHANGESERROR");

				case EshavaMessageConstant.NotExistingError:
					return Constants.CommonNames.MESSAGECONSTANTS.Access("NOTEXISTINGERROR");

				case EshavaMessageConstant.UpdateDataError:
					return Constants.CommonNames.MESSAGECONSTANTS.Access("UPDATEDATAERROR");

				case EshavaMessageConstant.AlreadyExisting:
					return Constants.CommonNames.MESSAGECONSTANTS.Access("ALREADYEXISTING");

				case EshavaMessageConstant.AutoPatchBlocked:
					return Constants.CommonNames.MESSAGECONSTANTS.Access("AUTOPATCHBLOCKED");

				case EshavaMessageConstant.NotExisting:
					return Constants.CommonNames.MESSAGECONSTANTS.Access("NOTEXISTING");

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