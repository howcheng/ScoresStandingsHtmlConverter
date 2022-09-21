using AutoFixture;
using HtmlAgilityPack;

namespace ScoresStandingsHtmlConverter.Services.Tests
{
	public class StandingsHtmlWriterTests
	{
		[Theory]
		[InlineData(1, true)]
		[InlineData(6, false)]
		public async Task CanWriteStandings(int roundNum, bool hasTie)
		{
			// create data for 6 teams
			const int COUNT = 6;
			int rank = COUNT;
			Fixture fixture = new Fixture();
			IEnumerable<StandingsRow> standings = fixture.Build<StandingsRow>()
				.With(x => x.Rank, () => rank--)
				.CreateMany(COUNT);

			if (hasTie)
				standings.First().Rank = standings.Last().Rank;

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

			using (StandingsHtmlWriter writer = new StandingsHtmlWriter(settings, mockFileWriter.Object))
			{
				// picking 10UG because there are 2 playoff spots
				await writer.WriteStandingsToFile(Constants.DIV_10UG, standings);

				Assert.StartsWith(settings.DateOfRound.ToString("M/d"), filename);
				Assert.Contains(Constants.DIV_10UG, filename);
				Assert.NotNull(html);
				HtmlDocument document = new HtmlDocument();
				document.LoadHtml(html);

				HtmlNode table = document.DocumentNode.FirstChild;
				Assert.Equal("standings", table.Attributes["class"].Value);

				HtmlNode? tbody = table.ChildNodes.SingleOrDefault(x => x.Name == "tbody");
				Assert.NotNull(tbody);
				IEnumerable<HtmlNode> rows = tbody!.ChildNodes.Where(x => x.Name == "tr");
				Assert.Equal(standings.Count(), rows.Count());
				for (int i = 0; i < rows.Count(); i++)
				{
					HtmlNode row = rows.ElementAt(i);
					string teamName = TestHelper.GetHtmlCellContent(row.ChildNodes.Where(x => x.Name == "td").Skip(1).First());
					StandingsRow match = standings.Single(x => x.TeamName == teamName);
					AssertStandingsRow(match, row, i, hasTie, roundNum == 6);
				}
			}
		}

		private void AssertStandingsRow(StandingsRow data, HtmlNode tr, int idx, bool hasTie = false, bool hasPlayoff = false)
		{
			// check the CSS class
			bool alt = (idx % 2) == 1;
			HtmlAttribute classAtt = tr.Attributes["class"];
			if (hasPlayoff && idx < 2)
			{
				string rowClass = $"playoffs{(alt ? nameof(alt) : string.Empty)}";
				Assert.Equal(rowClass, classAtt.Value);
			}
			else
			{
				if (alt)
					Assert.Equal(nameof(alt), classAtt.Value);
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
