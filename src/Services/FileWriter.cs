namespace ScoresStandingsHtmlConverter.Services
{
	public class FileWriter : IFileWriter
	{
		private readonly AppSettings _appSettings;

		public FileWriter(AppSettings settings)
		{
			_appSettings = settings;
		}

		public async Task WriteFile(string filename, string output)
		{
			string filePath = $"{_appSettings.DateOfRound}\\{filename}";
			if (File.Exists(filePath))
				File.Delete(filePath);

			using (FileStream stream = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Write))
			using (StreamWriter streamWriter = new StreamWriter(stream))
			{
				await streamWriter.WriteAsync(output);
			}
		}
	}
}
