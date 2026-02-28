using System.IO;

namespace Eshava.Example.Application.Common
{
	public class FileStreamDto
	{
		public Stream Data { get; set; }
		public string TypeOfTheFileContent { get; set; }
		public string NameOfTheFile { get; set; }
	}
}