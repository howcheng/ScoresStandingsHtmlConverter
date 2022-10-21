namespace ScoresStandingsHtmlConverter.Services
{
	public class AppSettings
	{ 
		private static DateTime s_DefaultDate;

		static AppSettings()
		{
			// default to last Saturday
			DateTime today = DateTime.Today;
			DateTime lastSaturday = today.AddDays(-((int)today.DayOfWeek + 1));
			s_DefaultDate = lastSaturday;
		}

		private IEnumerable<string>? _divisions;
		public IEnumerable<string>? Divisions 
		{
			get => _divisions ?? Constants.ALL_DIVISIONS;
			set
			{
				if (value != null && value.Any())
					_divisions = value;
			}
		}

		private DateTime? _dateOfRound;
		public DateTime? DateOfRound 
		{ 
			get => _dateOfRound ?? s_DefaultDate;
			set
			{
				if (value != null)
					_dateOfRound = value;
			}
		}

		// These two are opposite of how I normally like to do them because it's counterintuitive, but this is forced upon us by the CommandLine package; see https://github.com/commandlineparser/commandline/issues/702
		public bool NoStandings { get; set; } = false;
		public bool NoScores { get; set; } = false;

		public int CurrentRound { get; set; } // this value is extracted from the Google sheet based on the date (populated in ScoresExtractor and used in StandingsHtmlWriter for playoff places)

		public string? SheetId { get; set; } // this value comes from the appSettings.json file

		public string? FileOutputPath { get; set; } // this value comes from the appSettings.json file

		public string? ClientSecretsPath { get; set; } // this value comes from the appSettings.json file
	}
}
