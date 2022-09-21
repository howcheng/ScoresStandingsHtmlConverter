namespace ScoresStandingsHtmlConverter.Services
{
	public class AppSettings
	{ 
		private static string[] s_DefaultDivisions = new string[] { Constants.DIV_10UB, Constants.DIV_10UG, Constants.DIV_12UB, Constants.DIV_12UG, Constants.DIV_14UB, Constants.DIV_14UG };
		private static DateTime s_DefaultDate;

		static AppSettings()
		{
			// default to last Saturday
			DateTime today = DateTime.Today;
			DateTime lastSaturday = today.AddDays(-((int)today.DayOfWeek + 1));
			s_DefaultDate = lastSaturday;
		}

		private string[]? _divisions;
		public virtual string[] Divisions 
		{
			get => _divisions ?? s_DefaultDivisions;
			set => _divisions = value;
		}

		private DateTime? _dateOfRound;
		public virtual DateTime DateOfRound 
		{ 
			get => _dateOfRound ?? s_DefaultDate;
			set => _dateOfRound = value;
		}

		public virtual bool DoStandings { get; set; }
		public virtual bool DoScores { get; set; }

		public int CurrentRound { get; set; } // this value is extracted from the Google sheet based on the date (populated in ScoresExtractor and used in StandingsHtmlWriter for playoff places)

		public string? SheetId { get; set; } // this value comes from the appSettings.json file

		public string? FileOutputPath { get; set; } // this value comes from the appSettings.json file
	}
}
