using CommandLine;

namespace ScoresStandingsHtmlConverter.Console
{
	public class Arguments
	{
		[Option("divisions", Required = false, Separator = ',')]
		public IEnumerable<string>? Divisions { get; set; }
		[Option("date", Required = false)]
		public DateTime? DateOfRound { get; set; }
		[Option("standings", Required = false, Default = true)]
		public bool DoStandings { get; set; }
		[Option("scores", Required = false, Default = true)]
		public bool DoScores { get; set; }
	}
}
