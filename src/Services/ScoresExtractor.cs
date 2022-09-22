using Google.Apis.Sheets.v4.Data;
using GoogleSheetsHelper;
using Microsoft.Extensions.Logging;

namespace ScoresStandingsHtmlConverter.Services
{
	public class ScoresExtractor : IScoresExtractor
	{
		private readonly AppSettings _appSettings;
		private readonly ISheetsClient _sheetsClient;
		private readonly ILogger<ScoresExtractor> _logger;

		public ScoresExtractor(AppSettings settings, ISheetsClient sheetsClient, ILogger<ScoresExtractor> log)
		{
			_appSettings = settings;
			_sheetsClient = sheetsClient;
			_logger = log;
		}

		public async Task<IEnumerable<GameScore>> GetScores(string division)
		{
			string range = $"'{division.Trim()}'!A:D";
			IList<RowData> rows = await _sheetsClient.GetRowData(range);

			// extract the scores from the sheet
			List<GameScore> scores = new List<GameScore>();
			int roundNum = 0;
			DateTime roundDate = DateTime.MinValue;
			foreach (RowData row in rows)
			{
				if (row.Values == null)
					continue;
				CellData firstCell = row.Values.First();
				string? firstCellValue = firstCell.EffectiveValue?.StringValue;
				if (Helpers.IsRoundHeaderCell(firstCell))
				{
					if (scores.Count > 0)
						break; // we've reached the next round and we already have scores, so we must be done

					_appSettings.CurrentRound = roundNum = Helpers.GetRoundNumberFromCellData(firstCell);

					roundDate = Helpers.GetDateOfRoundFromCellValue(firstCellValue!);
					continue;
				}

				if (roundDate != _appSettings.DateOfRound)
					continue; // old scores
				if (firstCellValue == "HOME")
					continue; // subheader row
				if (string.IsNullOrEmpty(firstCellValue))
					continue; // blank row or a placeholder for a game that did not take place this week (e.g., bye week instead of a friendly)

				if (scores.Count == 0)
					_logger.LogInformation($"Getting scores for {division} in round {roundNum}...");

				bool gameCancelled = (row.Values[1].EffectiveValue == null && row.Values[2].EffectiveValue == null);
				bool friendly = firstCell.EffectiveFormat.TextFormat.ForegroundColorStyle.RgbColor.GoogleColorEquals(System.Drawing.Color.Red);

				GameScore score = new GameScore
				{
					RoundNumber = roundNum,
					DateOfRound = _appSettings.DateOfRound.Value,
					HomeTeam = firstCellValue!,
					HomeScore = (int?)row.Values[1].EffectiveValue?.NumberValue,
					AwayScore = (int?)row.Values[2].EffectiveValue?.NumberValue,
					AwayTeam = row.Values[3].EffectiveValue.StringValue,
					Cancelled = gameCancelled,
					Friendly = friendly,
				};
				string logMsg = $"Game {scores.Count + 1}: {score.HomeTeam} vs. {score.AwayTeam}";
				if (friendly)
					logMsg = $"{logMsg} (friendly)";
				if (gameCancelled)
					logMsg = $"{logMsg}, cancelled";
				else
					logMsg = $"{logMsg}: {score.HomeScore}–{score.AwayScore}";

				_logger.LogDebug(logMsg);
				scores.Add(score);
			}

			return scores;
		}
	}
}
