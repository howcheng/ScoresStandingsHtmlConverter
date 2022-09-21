using GoogleSheetsHelper;

namespace ScoresStandingsHtmlConverter.Services
{
	public class StandingsExtractor : IStandingsExtractor
	{
		private readonly AppSettings _appSettings;
		private readonly ISheetsClient _sheetsClient;

		public StandingsExtractor(AppSettings appSettings, ISheetsClient sheetsClient)
		{
			_appSettings = appSettings;
			_sheetsClient = sheetsClient;
		}

		public async Task<IEnumerable<StandingsRow>> GetStandings(string division)
		{
			// figure out which round this is so we can get the correct round's standings
			string range = $"'{division}'!A1:A";
			IList<string> values = (await _sheetsClient.GetValues(range)).First().Cast<string>().ToList();
			int headerRowIdx = 0;
			DateTime roundDate = DateTime.MinValue;

			// figure out which round we are doing and the index of the row
			for (int i = 0; i < values.Count; i++)
			{
				string value = values[i];
				if (string.IsNullOrEmpty(value))
					continue;

				if (Helpers.IsRoundHeaderCell(value))
				{
					roundDate = Helpers.GetDateOfRoundFromCellValue(value);
					if (roundDate == _appSettings.DateOfRound)
					{
						headerRowIdx = i;
						break;
					}
				}
			}

			// figure out how many teams are in the division by counting the number of rows in round 1
			int numTeams = 0;
			for (int i = 0; i < values.Count; i++)
			{
				string value = values[i];
				if (Helpers.IsRoundHeaderCell(value) || value == "HOME")
				{
					if (numTeams > 0)
						break; // we've reached the next round and we know how many teams there are; in the last round we'll run out of data

					continue;
				}
				numTeams += 1; // if it's not a round header or subheader row, count it
			}

			int startRow = headerRowIdx + 3; // +2 to get to the first row, and then +1 more because ranges are not zero-based
			int endRow = startRow + numTeams;

			// now we make the request for the standings data
			range = $"'{division}'!F{startRow}:N{endRow}";
			IList<IList<object>> standingsData = await _sheetsClient.GetValues(range);

			List<StandingsRow> standings = new List<StandingsRow>();
			foreach (IList<object> dataRow in standingsData)
			{
				standings.Add(new StandingsRow
				{
					TeamName = (string)dataRow[0],
					GamesPlayed = (int)dataRow[1],
					Wins = (int)dataRow[2],
					Losses = (int)dataRow[3],
					Draws = (int)dataRow[4],
					GamePoints = (int)dataRow[5],
					RefPoints = (int)dataRow[6],
					TotalPoints = (int)dataRow[7],
					Rank = (int)dataRow[8],
				});
			}

			return standings;
		}
	}
}
