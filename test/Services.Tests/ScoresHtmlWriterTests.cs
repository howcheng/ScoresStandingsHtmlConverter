using AutoFixture;
using HtmlAgilityPack;

namespace ScoresStandingsHtmlConverter.Services.Tests
{
	public class ScoresHtmlWriterTests
	{
		private List<GameScore> CreateGameScores(bool hasFriendly, bool hasCancellation)
		{
			Fixture fixture = new Fixture();
			Func<string> createTeamName = () => $"{fixture.Create<string>()} ({fixture.Create<string>()})"; // "Team 01 (Smith)"
			int counter = 0;
			List<GameScore> scores = fixture.Build<GameScore>()
				.With(x => x.RoundNumber, 1)
				.With(x => x.DateOfRound, DateTime.Today)
				.With(x => x.HomeTeam, createTeamName())
				.With(x => x.AwayTeam, createTeamName())
				.With(x => x.Friendly, () => hasFriendly ? counter == 2 : false)
				.With(x => x.Cancelled, () =>
				{
					bool ret = hasCancellation ? counter == 2 : false;
					counter += 1;
					return ret;
				})
				.CreateMany(3)
				.ToList();
			return scores;
		}

		[Theory]
		[InlineData(true, true)]
		[InlineData(false, true)]
		[InlineData(true, false)]
		[InlineData(false, false)]
		public async Task CanWriteScores(bool hasFriendly, bool hasCancellation)
		{
			// create some data
			const string DIVISION = "14U Girls";
			List<GameScore> scores = CreateGameScores(hasFriendly, hasCancellation);

			Mock<IFileWriter> mockFileWriter = new Mock<IFileWriter>();
			string? filename = null, html = null;
			Action<string, string> callback = (fn, output) =>
			{
				filename = fn;
				html = output;
			};
			mockFileWriter.Setup(x => x.WriteFile(It.IsAny<string>(), It.IsAny<string>())).Callback(callback);

			using (ScoresHtmlWriter writer = new ScoresHtmlWriter(mockFileWriter.Object))
 			{
				await writer.WriteScoresToFile(DIVISION, scores);

				Assert.StartsWith(scores.First().DateOfRound.ToString("M/d"), filename);
				Assert.Contains(DIVISION, filename);
				Assert.NotNull(html);
				HtmlDocument document = new HtmlDocument();
				document.LoadHtml(html);

				HtmlNode table = document.DocumentNode.FirstChild;
				Assert.Equal("scores", table.Attributes["class"].Value);

				HtmlNode? tbody = table.ChildNodes.SingleOrDefault(x => x.Name == "tbody");
				Assert.NotNull(tbody);
				IEnumerable<HtmlNode> rows = tbody!.ChildNodes.Where(x => x.Name == "tr");
				Assert.Equal(scores.Count, rows.Count());
				Assert.Collection(rows
					, x => AssertScoreRow(scores[0], x)
					, x => AssertScoreRow(scores[1], x)
					, x => AssertScoreRow(scores[2], x, hasFriendly, hasCancellation)
					);
			}
		}

		private void AssertScoreRow(GameScore score, HtmlNode tr, bool isFriendly = false, bool isCancelled = false)
		{
			IEnumerable<HtmlNode> cells = tr.ChildNodes.Where(x => x.Name == "td");
			Assert.Equal(3, cells.Count()); // 3 cells in each row: home team, score, away team
			if (isFriendly || isCancelled)
			{
				Assert.NotNull(tr.Attributes["class"]);
				string rowClass = tr.Attributes["class"].Value;
				if (isFriendly)
					Assert.Contains("friendly", rowClass);
				if (isCancelled)
					Assert.Contains("cancelled", rowClass);
			}	

			string homeTeam = cells.ElementAt(0).InnerText.Trim();
			string gameScore = cells.ElementAt(1).InnerText.Trim();
			string awayTeam = cells.ElementAt(2).InnerText.Trim();

			// we stripped the coach name from the team name for the HTML
			Assert.StartsWith(homeTeam, score.HomeTeam);
			Assert.NotEqual(score.HomeTeam, homeTeam);
			Assert.StartsWith(awayTeam, score.AwayTeam);
			Assert.NotEqual(score.AwayTeam, awayTeam);

			if (isCancelled)
				Assert.Equal("cancelled", gameScore);
			else
				Assert.Equal($"{score.HomeScore}&ndash;{score.AwayScore}", gameScore);
		}
	}
}
