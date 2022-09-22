using Microsoft.Extensions.Logging;

namespace ScoresStandingsHtmlConverter.Services
{
	public class FileWriter : IFileWriter
	{
		private readonly AppSettings _appSettings;
		private readonly ILogger<FileWriter> _logger;

		public FileWriter(AppSettings settings, ILogger<FileWriter> log)
		{
			_appSettings = settings;
			_logger = log;
		}

		public async Task WriteFile(string filename, string output)
		{
			string folderPath = $"{_appSettings.FileOutputPath}\\{_appSettings.DateOfRound:yyyy-MM-dd}";
			if (!Directory.Exists(folderPath))
			{
				_logger.LogInformation($"Creating folder at {folderPath}...");
				Directory.CreateDirectory(folderPath);
			}
			string filePath = $"{folderPath}\\{filename}";
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
