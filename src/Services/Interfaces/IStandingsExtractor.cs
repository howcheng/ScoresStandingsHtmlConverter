namespace ScoresStandingsHtmlConverter.Services
{
	public interface IStandingsExtractor
	{
		Task<IEnumerable<StandingsRow>> GetStandings(string division);
	}
}