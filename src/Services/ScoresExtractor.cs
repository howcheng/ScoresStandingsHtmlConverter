using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Sheets.v4.Data;
using GoogleSheetsHelper;

namespace ScoresStandingsHtmlConverter.Services
{
	public class ScoresExtractor : IScoresExtractor
	{
		private readonly AppSettings _appSettings;
		private readonly ISheetsClient _sheetsClient;

		public ScoresExtractor(AppSettings settings, ISheetsClient sheetsClient)
		{
			_appSettings = settings;
			_sheetsClient = sheetsClient;
		}

		public async Task<IEnumerable<GameScore>> GetScores(string division)
		{
			string range = $"{division.Trim()}!A:D";
			IList<RowData> rows = await _sheetsClient.GetRowData(range);

			// extract the scores from the sheet
			List<GameScore> scores = new List<GameScore>();
			int roundNum = 0;
			foreach (RowData row in rows)
			{
				CellData firstCell = row.Values.First();
				string firstCellValue = firstCell.EffectiveValue.StringValue;
				if (Helpers.CellDataContainsRoundNumber(firstCell))
				{
					if (scores.Count > 0)
						break;

					roundNum = Helpers.GetRoundNumberFromCellData(firstCell);
					continue;
				}

				if (roundNum != _appSettings.CurrentRound)
					continue; // old scores
				if (firstCellValue == "HOME")
					continue; // subheader row
				if (string.IsNullOrEmpty(firstCellValue))
					continue; // placeholder for a game that did not take place this week (e.g., bye week instead of a friendly)

				bool gameCancelled = (row.Values[1].EffectiveValue == null && row.Values[2].EffectiveValue == null);
				bool friendly = Utilities.GoogleColorEquals(firstCell.EffectiveFormat.TextFormat.ForegroundColorStyle.RgbColor, System.Drawing.Color.Red);

				GameScore score = new GameScore
				{
					RoundNumber = roundNum,
					DateOfRound = _appSettings.DateOfRound,
					HomeTeam = firstCell.EffectiveValue.StringValue,
					HomeScore = (int?)row.Values[1].EffectiveValue?.NumberValue,
					AwayScore = (int?)row.Values[2].EffectiveValue?.NumberValue,
					AwayTeam = row.Values[3].EffectiveValue.StringValue,
					Cancelled = gameCancelled,
					Friendly = friendly,
				};
				scores.Add(score);
			}

			return scores;
		}
	}
}
