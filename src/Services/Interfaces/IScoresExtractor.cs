namespace ScoresStandingsHtmlConverter.Services
{
	public interface IScoresExtractor
	{
		Task<IEnumerable<GameScore>> GetScores(string division);
	}
}