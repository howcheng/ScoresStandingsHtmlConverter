using Microsoft.Extensions.Logging;
using Google.Apis.Sheets.v4.Data;
using GoogleSheetsHelper;

namespace ScoresStandingsHtmlConverter.Services
{
	public class StandingsExtractor : IStandingsExtractor
	{
		private readonly AppSettings _appSettings;
		private readonly ISheetsClient _sheetsClient;
		private readonly ILogger<StandingsExtractor> _logger;

		public StandingsExtractor(AppSettings appSettings, ISheetsClient sheetsClient, ILogger<StandingsExtractor> log)
		{
			_appSettings = appSettings;
			_sheetsClient = sheetsClient;
			_logger = log;
		}

		public async Task<IEnumerable<StandingsRow>> GetStandings(string division)
		{
			// figure out which round this is so we can get the correct round's standings
			string range = $"'{division}'!A1:A";
			IList<RowData> rows = await _sheetsClient.GetRowData(range); // don't use GetValues() here because it skips null values
			int headerRowIdx = 0;

			Func<RowData, string?> getText = rd => rd.Values?.FirstOrDefault()?.EffectiveValue?.StringValue;

			// figure out which round we are doing and the index of the row
			// note: can't use a LINQ Select() query to get the string values because some rows have no CellData children, which will cause a NullReferenceException
			int roundNum = 0;
			for (int i = 0; i < rows.Count(); i++)
			{
				string? value = getText(rows.ElementAt(i));
				if (string.IsNullOrEmpty(value))
					continue;

				if (Helpers.IsRoundHeaderCell(value))
				{
					DateTime roundDate = Helpers.GetDateOfRoundFromCellValue(value);
					if (roundDate == _appSettings.DateOfRound)
					{
						_appSettings.CurrentRound = roundNum = Helpers.GetRoundNunberFromCellValue(value); // even though we set this in the ScoresExtractor, this is in case we generate standings only
						headerRowIdx = i;
						break;
					}
				}
			}

			_logger.LogInformation($"Getting standings for {division} in round {roundNum}...");

			// figure out how many teams are in the division by counting the number of rows in round 1
			int numTeams = 0;
			for (int i = 0; i < rows.Count(); i++)
			{
				string? value = getText(rows.ElementAt(i));
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
			_logger.LogDebug($"Making request for data in range {range} ({numTeams} teams)");
			IList<IList<object>> standingsData = await _sheetsClient.GetValues(range);

			List<StandingsRow> standings = new List<StandingsRow>();
			foreach (IList<object> dataRow in standingsData)
			{
				standings.Add(new StandingsRow
				{
					TeamName = (string)dataRow[0],
					GamesPlayed = Convert.ToInt32(dataRow[1]),
					Wins = Convert.ToInt32(dataRow[2]),
					Losses = Convert.ToInt32(dataRow[3]),
					Draws = Convert.ToInt32(dataRow[4]),
					GamePoints = Convert.ToInt32(dataRow[5]),
					RefPoints = Convert.ToSingle(dataRow[6]),
					TotalPoints = Convert.ToSingle(dataRow[7]),
					Rank = Convert.ToInt32(dataRow[8]),
				});
			}

			return standings;
		}
	}
}
