namespace ScoresStandingsHtmlConverter.Services
{
	public interface IStandingsHtmlWriter
	{
		Task WriteStandingsToFile(string division, IEnumerable<StandingsRow> standingsRows);
	}
}