namespace ScoresStandingsHtmlConverter.Services
{
	public class StandingsService : IService
	{
		private readonly string _division;
		private readonly IStandingsExtractor _extractor;
		private readonly IStandingsHtmlWriter _writer;

		public StandingsService(string division, IStandingsExtractor extractor, IStandingsHtmlWriter writer)
		{
			_division = division;
			_extractor = extractor;
			_writer = writer;
		}

		public async Task Execute()
		{
			IEnumerable<StandingsRow> standings = await _extractor.GetStandings(_division);
			await _writer.WriteStandingsToFile(_division, standings);
		}
	}
}
