namespace ScoresStandingsHtmlConverter.Services
{
	public partial class ScoresService : IService
	{
		private readonly AppSettings _appSettings;
		private readonly IScoresExtractor _extractor;
		private readonly IScoresHtmlWriter _writer;

		public ScoresService(AppSettings settings, IScoresExtractor extractor, IScoresHtmlWriter writer)
		{
			_appSettings = settings;
			_extractor = extractor;
			_writer = writer;
		}
		
		public async Task Execute()
		{
			foreach (string division in _appSettings.Divisions!)
			{
				IEnumerable<GameScore> scores = await _extractor.GetScores(division);
				await _writer.WriteScoresToFile(division, scores);
			}
		}
	}
}
