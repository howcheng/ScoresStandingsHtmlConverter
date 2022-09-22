namespace ScoresStandingsHtmlConverter.Services
{
	public partial class ScoresService : IService
	{
		private readonly string _division;
		private readonly IScoresExtractor _extractor;
		private readonly IScoresHtmlWriter _writer;

		public ScoresService(string division, IScoresExtractor extractor, IScoresHtmlWriter writer)
		{
			_division = division;
			_extractor = extractor;
			_writer = writer;
		}
		
		public async Task Execute()
		{
			IEnumerable<GameScore> scores = await _extractor.GetScores(_division);
			await _writer.WriteScoresToFile(_division, scores);
		}
	}
}
