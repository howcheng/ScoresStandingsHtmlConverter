namespace ScoresStandingsHtmlConverter.Services
{
	public class StandingsRow
	{
		public string? TeamName { get; set; }
		public int GamesPlayed { get; set; }
		public int Wins { get; set; }
		public int Losses { get; set; }
		public int Draws { get; set; }
		public int GamePoints { get; set; }
		public int RefPoints { get; set; }
		public int TotalPoints { get; set; }
		public int Rank { get; set; }
	}
}
