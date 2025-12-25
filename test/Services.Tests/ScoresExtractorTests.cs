using Google.Apis.Sheets.v4.Data;
using GoogleSheetsHelper;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;
using Color = System.Drawing.Color;

namespace ScoresStandingsHtmlConverter.Services.Tests
{
	public class ScoresExtractorTests
	{
		private readonly ILogger<ScoresExtractor> _logger;

        public ScoresExtractorTests(ITestOutputHelper helper)
		{
            _logger = helper.BuildLoggerFor<ScoresExtractor>();
        }

        private List<RowData> CreateScoreRowData(bool hasFriendly, bool hasUnknownScore)
		{
			// create two rounds of data
			List<RowData> rows = new List<RowData>();
			rows.AddRange(CreateScoreRowDataForRound(1, hasFriendly, hasUnknownScore));
			rows.AddRange(CreateScoreRowDataForRound(2, hasFriendly, hasUnknownScore));
			return rows;
		}

		private List<RowData> CreateScoreRowDataForRound(int roundNum, bool hasFriendly, bool hasCancellation)
		{
			List<RowData> rows = new List<RowData>();
			rows.Add(new RowData
			{
				// round number row
				Values = new List<CellData>
				{
					CreateCellDataForText($"ROUND {roundNum}: {TestHelper.CreateRoundDateFromNumber(roundNum)}"),
				},
			});
			rows.Add(new RowData
			{
				// subheader row
				Values = new List<CellData>
				{
					CreateCellDataForText("HOME"),
					CreateCellDataForText("HG"),
					CreateCellDataForText("AG"),
					CreateCellDataForText("AWAY"),
				},
			});
			// three rows of scores
			rows.Add(new RowData
			{
				Values = new List<CellData>
				{
					CreateCellDataForText("Team 1 (Smith)"),
					CreateCellDataForNumber(1),
					CreateCellDataForNumber(0),
					CreateCellDataForText("Team 2 (Jones)"),
				},
			});
			rows.Add(new RowData
			{
				Values = new List<CellData>
				{
					CreateCellDataForText("Team 3 (Chan)"),
					CreateCellDataForNumber(4),
					CreateCellDataForNumber(2),
					CreateCellDataForText("Team 4 (Kirilenko)" ),
				},
			});
			rows.Add(new RowData
			{
				Values = new List<CellData>
				{
					CreateCellDataForText("Team 5 (Nkunku)", hasFriendly),
					CreateCellDataForNumber(1, hasFriendly, hasCancellation),
					CreateCellDataForNumber(2, hasFriendly, hasCancellation),
					CreateCellDataForText("Team 6 (Jorgensen)", hasFriendly)
				},
			});

			// some blank rows at the end
			rows.Add(new RowData { Values = new List<CellData> { CreateCellDataForText(string.Empty) } });
			rows.Add(new RowData { Values = new List<CellData> { CreateCellDataForText(string.Empty) } });
			rows.Add(new RowData { Values = new List<CellData> { CreateCellDataForText(string.Empty) } });

			return rows;
		}

		private CellData CreateCellDataForText(string text, bool isFriendly = false)
			=> new CellData
			{
				EffectiveValue = new ExtendedValue { StringValue = text },
				EffectiveFormat = CreateCellFormat(isFriendly),
			};

		private CellData CreateCellDataForNumber(int? number, bool isFriendly = false, bool isCancelled = false)
			=> new CellData
			{
				EffectiveValue = isCancelled ? null : new ExtendedValue { NumberValue = number },
				EffectiveFormat = CreateCellFormat(isFriendly),
			};

		private CellFormat CreateCellFormat(bool isFriendly = false)
			=> new CellFormat { TextFormat = new TextFormat { ForegroundColorStyle = new ColorStyle { RgbColor = (isFriendly ? Color.Red : Color.Black).ToGoogleColor() } } };

		[Theory]
		[InlineData(1, true, true)]
		[InlineData(2, true, true)]
		[InlineData(1, false, true)]
		[InlineData(2, false, true)]
		[InlineData(1, true, false)]
		[InlineData(2, true, false)]
		[InlineData(1, false, false)]
		[InlineData(2, false, false)]
		public async Task CanGetScores(int roundNum, bool hasFriendly, bool hasUnknownScore)
		{
			// create some data
			List<RowData> rowData = CreateScoreRowData(hasFriendly, hasUnknownScore);

			// mock the sheets client
			Mock<ISheetsClient> mockClient = new Mock<ISheetsClient>();
			mockClient.Setup(x => x.GetRowData(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(rowData);

			AppSettings settings = new AppSettings
			{
				DateOfRound = TestHelper.CreateRoundDateFromNumber(roundNum),
				Divisions = new[] { Constants.DIV_14UG },
			};

			ScoresExtractor service = new ScoresExtractor(settings, mockClient.Object, _logger);
			IEnumerable<GameScore> scores = await service.GetScores(settings.Divisions.First());

			Assert.True(scores.Any());
			Assert.All(scores, x => Assert.Equal(roundNum, x.RoundNumber));
			Assert.Equal(3, scores.Count());
			Assert.Collection(scores
				, x => AssertScoresAreCorrect(x, rowData[2])
				, x => AssertScoresAreCorrect(x, rowData[3])
				, x => AssertScoresAreCorrect(x, rowData[4], hasFriendly, hasUnknownScore)
			);
		}

		private void AssertScoresAreCorrect(GameScore score, RowData rowData, bool isFriendly = false, bool isUnknown = false)
		{
			string homeTeam = rowData.Values[0].EffectiveValue.StringValue;
			string awayTeam = rowData.Values[3].EffectiveValue.StringValue;

			Assert.Equal(isFriendly, score.Friendly);
			Assert.Equal(isUnknown, score.Unknown);
			Assert.Equal(homeTeam, score.HomeTeam);
			if (isUnknown)
			{
				Assert.Null(score.HomeScore);
				Assert.Null(score.AwayScore);
			}
			else
			{
				int? homeScore = (int?)rowData.Values[1].EffectiveValue.NumberValue;
				int? awayScore = (int?)rowData.Values[2].EffectiveValue.NumberValue;
				Assert.Equal(homeScore, score.HomeScore);
				Assert.Equal(awayScore, score.AwayScore);
			}
			Assert.Equal(awayTeam, score.AwayTeam);
		}
	}
}