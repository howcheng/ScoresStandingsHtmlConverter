namespace ScoresStandingsHtmlConverter.Services
{
	public interface IFileWriter
	{
		Task WriteFile(string filename, string output);
	}
}