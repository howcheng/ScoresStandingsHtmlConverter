﻿namespace ScoresStandingsHtmlConverter.Services
{
	public class GameScore
	{
		public int RoundNumber { get; set; }
		public DateTime DateOfRound { get; set; }
		public string HomeTeam { get; set; } = string.Empty;
		public string AwayTeam { get; set; } = string.Empty;
		public int? HomeScore { get; set; }
		public int? AwayScore { get; set; }
		public bool Cancelled { get; set; }
		public bool Friendly { get; set; }
	}
}
