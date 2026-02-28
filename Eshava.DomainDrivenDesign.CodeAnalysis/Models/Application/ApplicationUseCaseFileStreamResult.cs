using Eshava.DomainDrivenDesign.CodeAnalysis.Extensions;

namespace Eshava.DomainDrivenDesign.CodeAnalysis.Models.Application
{
	public class ApplicationUseCaseFileStreamResult
	{
		private const string RESPONSE_OBEJCT_NAME = "File";
		private const string PROPERTY_STREAM = "Stream";
		private const string PROPERTY_CONTENT_TYPE = "ContentType";
		private const string PROPERTY_FILENAME = "FileName";

		public bool ProducesFileStreamResult { get; set; }
		public bool ReturnAsObject { get; set; }
		/// <summary>
		/// Only required if <see cref="ReturnAsObject"/> is set to true
		/// </summary>
		public string TypeForReturnObject { get; set; }
		/// <summary>
		/// Only required if <see cref="ReturnAsObject"/> is set to true
		/// </summary>
		public string UsingForTypeForReturnObject { get; set; }
		/// <summary>
		/// Only applied if <see cref="ReturnAsObject"/> is set to true
		/// Default: File
		/// </summary>
		public string PropertyNameForResponseObject { get; set; }

		/// <summary>
		/// Is used in response object and endpoint FileStreamResult
		/// Default: Stream
		/// </summary>
		public string PropertyNameForStream { get; set; }
		/// <summary>
		/// Is used in response object and endpoint FileStreamResult
		/// Default: ContentType
		/// </summary>
		public string PropertyNameForContentType { get; set; }
		/// <summary>
		/// Is used in response object and endpoint FileStreamResult
		/// Default: FileName
		/// </summary>
		public string PropertyNameForFileName { get; set; }

		public void Validate()
		{
			if (PropertyNameForResponseObject.IsNullOrEmpty())
			{
				PropertyNameForResponseObject = RESPONSE_OBEJCT_NAME;
			}

			if (PropertyNameForStream.IsNullOrEmpty())
			{
				PropertyNameForStream = PROPERTY_STREAM;
			}

			if (PropertyNameForContentType.IsNullOrEmpty())
			{
				PropertyNameForContentType = PROPERTY_CONTENT_TYPE;
			}

			if (PropertyNameForFileName.IsNullOrEmpty())
			{
				PropertyNameForFileName = PROPERTY_FILENAME;
			}
		}
	}
}