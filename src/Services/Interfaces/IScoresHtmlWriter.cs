namespace ScoresStandingsHtmlConverter.Services
{
	public interface IScoresHtmlWriter
	{
		Task WriteScoresToFile(string division, IEnumerable<GameScore> scores);
	}
}