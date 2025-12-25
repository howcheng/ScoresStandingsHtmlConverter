using AutoFixture;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace ScoresStandingsHtmlConverter.Services.Tests
{
	public class StandingsHtmlWriterTests
	{
		private readonly ILogger<StandingsHtmlWriter> _logger;

		public StandingsHtmlWriterTests(ITestOutputHelper helper)
		{
			_logger = helper.BuildLoggerFor<StandingsHtmlWriter>();
		}

		[Theory]
		[InlineData(1, true, false)]
		[InlineData(6, false, true)]
		[InlineData(6, true, true)]
		[InlineData(6, false, false)]
		public async Task CanWriteStandings(int roundNum, bool hasTie, bool leaderHasEnoughRefPts)
		{
			// create data for 6 teams
			const int COUNT = 6;
			int rank = COUNT;
			Fixture fixture = new Fixture();
			IEnumerable<StandingsRow> standings = fixture.Build<StandingsRow>()
				.With(x => x.RefPoints, () => leaderHasEnoughRefPts ? 6 : (rank < 3 ? 0 : 6))
				.With(x => x.Rank, () => rank--)
				.CreateMany(COUNT);

			if (hasTie)
				standings.First().Rank = standings.Last().Rank;
			bool hasPlayoff = roundNum == 6;

			Mock<IFileWriter> mockFileWriter = new Mock<IFileWriter>();
			string? filename = null, html = null;
			Action<string, string> callback = (fn, output) =>
			{
				filename = fn;
				html = output;
			};
			mockFileWriter.Setup(x => x.WriteFile(It.IsAny<string>(), It.IsAny<string>())).Callback(callback);

			AppSettings settings = new AppSettings
			{
				CurrentRound = roundNum,
				DateOfRound = TestHelper.CreateRoundDateFromNumber(roundNum),
			};

			using (StandingsHtmlWriter writer = new StandingsHtmlWriter(settings, mockFileWriter.Object, _logger))
			{
				await writer.WriteStandingsToFile(Constants.DIV_10UG, standings);

				Assert.StartsWith(Constants.DIV_10UG, filename);
				Assert.NotNull(html);
				HtmlDocument document = new HtmlDocument();
				document.LoadHtml(html);

				HtmlNode table = document.DocumentNode.FirstChild;
				Assert.Equal("standings", table.Attributes["class"].Value);

				HtmlNode? colgroup = table.ChildNodes.SingleOrDefault(x => x.Name == "colgroup");
				Assert.NotNull(colgroup);

				HtmlNode? thead = table.ChildNodes.SingleOrDefault(x => x.Name == "thead");
				Assert.NotNull(thead);

				HtmlNode? tbody = table.ChildNodes.SingleOrDefault(x => x.Name == "tbody");
				Assert.NotNull(tbody);
				IEnumerable<HtmlNode> rows = tbody!.ChildNodes.Where(x => x.Name == "tr");
				Assert.Equal(standings.Count(), rows.Count());

				int playoffTeamCount = 0;
				int? qualifyingRank = null;
				for (int i = 0; i < rows.Count(); i++)
				{
					HtmlNode row = rows.ElementAt(i);
					string teamName = TestHelper.GetHtmlCellContent(row.ChildNodes.Where(x => x.Name == "td").Skip(1).First());
					StandingsRow match = standings.Single(x => x.TeamName == teamName);
					AssertStandingsRow(match, row, i, hasTie, hasPlayoff, ref playoffTeamCount, ref qualifyingRank);
				}

				// check that the playoff rows are marked properly
				if (hasPlayoff && leaderHasEnoughRefPts)
				{
					// If there's a tie for first place, both teams should have playoffs class
					int expectedPlayoffTeams = hasTie ? 2 : 1;
					IEnumerable<HtmlNode> leaderRows = rows.Take(expectedPlayoffTeams);
					Assert.All(leaderRows, tr =>
					{
						HtmlAttribute? classAtt = tr.Attributes["class"];
						Assert.NotNull(classAtt);
						Assert.Contains("playoffs", classAtt!.Value);
					});
					Assert.All(rows.Skip(expectedPlayoffTeams), tr =>
					{
						HtmlAttribute? classAtt = tr.Attributes["class"];
						if (classAtt != null)
							Assert.DoesNotContain("playoffs", classAtt.Value);
					});
				}
				else if (hasPlayoff && !leaderHasEnoughRefPts)
				{
					// Teams at positions 2-3 (indices 2-3) should have playoffs class if they have enough ref points
					int expectedPlayoffTeams = hasTie ? 2 : 1;
					IEnumerable<HtmlNode> qualifiedRows = rows.Skip(2).Take(expectedPlayoffTeams);
					Assert.All(qualifiedRows, tr =>
					{
						HtmlAttribute? classAtt = tr.Attributes["class"];
						Assert.NotNull(classAtt);
						Assert.Contains("playoffs", classAtt!.Value);
					});
					Assert.All(rows.Except(qualifiedRows), tr =>
					{
						HtmlAttribute? classAtt = tr.Attributes["class"];
						if (classAtt != null)
							Assert.DoesNotContain("playoffs", classAtt.Value);
					});
				}
			}
		}

		private void AssertStandingsRow(StandingsRow data, HtmlNode tr, int idx, bool hasTie, bool hasPlayoff, ref int playoffTeamCount, ref int? qualifyingRank)
		{
			// check the CSS class
			bool alt = (idx % 2) == 1;
			HtmlAttribute? classAtt = tr.Attributes["class"];
			bool shouldUsePlayoffClass = false;
			if (hasPlayoff && data.RefPoints >= StandingsHtmlWriter.MIN_REF_PTS_FOR_PLAYOFFS)
			{
				// A team qualifies for playoffs if:
				// 1. No one has qualified yet (playoffTeamCount == 0), OR
				// 2. There's a tie with a team that already qualified (same rank as qualifyingRank)
				if (playoffTeamCount == 0)
				{
					playoffTeamCount += 1;
					qualifyingRank = data.Rank;
					shouldUsePlayoffClass = true;
				}
				else if (qualifyingRank.HasValue && data.Rank == qualifyingRank.Value)
				{
					// Team tied with the qualifying rank
					playoffTeamCount += 1;
					shouldUsePlayoffClass = true;
				}
			}
			if (shouldUsePlayoffClass)
			{
				string rowClass = $"playoffs{(alt ? nameof(alt) : string.Empty)}";
				Assert.NotNull(classAtt);
				Assert.Equal(rowClass, classAtt.Value);
			}
			else
			{
				if (alt)
					Assert.Equal(nameof(alt), classAtt!.Value);
				else
					Assert.Null(classAtt);
			}

			IEnumerable<HtmlNode> cells = tr.ChildNodes.Where(x => x.Name == "td");
			Assert.Equal(9, cells.Count()); // 9 cells: rank, team, games, W, L, D, pts, ref pts, total pts

			string rank = TestHelper.GetHtmlCellContent(tr, 0);
			if (hasTie && idx < 2)
				Assert.Equal($"T{data.Rank}", rank);
			else
				Assert.Equal(data.Rank.ToString(), rank);

			Assert.Equal(data.TeamName, TestHelper.GetHtmlCellContent(tr, 1));
			Assert.Equal(data.GamesPlayed.ToString(), TestHelper.GetHtmlCellContent(tr, 2));
			Assert.Equal(data.Wins.ToString(), TestHelper.GetHtmlCellContent(tr, 3));
			Assert.Equal(data.Losses.ToString(), TestHelper.GetHtmlCellContent(tr, 4));
			Assert.Equal(data.Draws.ToString(), TestHelper.GetHtmlCellContent(tr, 5));
			Assert.Equal(data.GamePoints.ToString(), TestHelper.GetHtmlCellContent(tr, 6));
			Assert.Equal(data.RefPoints.ToString(), TestHelper.GetHtmlCellContent(tr, 7));
			Assert.Equal(data.TotalPoints.ToString(), TestHelper.GetHtmlCellContent(tr, 8));
		}
	}
}
