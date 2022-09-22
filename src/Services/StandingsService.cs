namespace ScoresStandingsHtmlConverter.Services
{
	public class StandingsService : IService
	{
		private readonly AppSettings _appSettings;
		private readonly IStandingsExtractor _extractor;
		private readonly IStandingsHtmlWriter _writer;

		public StandingsService(AppSettings appSettings, IStandingsExtractor extractor, IStandingsHtmlWriter writer)
		{
			_appSettings = appSettings;
			_extractor = extractor;
			_writer = writer;
		}

		public async Task Execute()
		{
			foreach (string division in _appSettings.Divisions)
			{
				IEnumerable<StandingsRow> standings = await _extractor.GetStandings(division);
				await _writer.WriteStandingsToFile(division, standings);
			}
		}
	}
}
