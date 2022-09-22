using CommandLine;

namespace ScoresStandingsHtmlConverter.Console
{
	public class Arguments
	{
		[Option("divisions", Required = false, Separator = ',')]
		public IEnumerable<string>? Divisions { get; set; }
		[Option("date", Required = false)]
		public DateTime? DateOfRound { get; set; }
		[Option("no-standings", Required = false)]
		public bool NoStandings { get; set; }
		[Option("no-scores", Required = false)]
		public bool NoScores { get; set; }
	}
}
