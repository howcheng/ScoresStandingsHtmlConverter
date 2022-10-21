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
		public float RefPoints { get; set; }
		public float TotalPoints { get; set; }
		public int Rank { get; set; }
	}
}
