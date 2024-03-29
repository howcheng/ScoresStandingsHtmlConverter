﻿using System.Text.RegularExpressions;
using AutoFixture;
using FluentAssertions;
using Google.Apis.Sheets.v4.Data;
using GoogleSheetsHelper;
using Microsoft.Extensions.Logging;

namespace ScoresStandingsHtmlConverter.Services.Tests
{
	public class StandingsExtractorTests
	{
		[Theory]
		[InlineData(1)]
		[InlineData(2)]
		public async Task CanGetStandings(int roundNum)
		{
			// let's have 6 teams in this division
			Fixture fixture = new Fixture();
			const int COUNT = 6;
			int rank = COUNT;
			IEnumerable<StandingsRow> standings = fixture.Build<StandingsRow>()
				.With(x => x.Rank, () => rank--)
				.CreateMany(rank);

			DateTime round1Dt = TestHelper.CreateRoundDateFromNumber(1);
			DateTime round2Dt = TestHelper.CreateRoundDateFromNumber(2);

			// create mocks
			Mock<ISheetsClient> mockClient = new Mock<ISheetsClient>();
			mockClient.Setup(x => x.GetRowData(It.Is<string>(s => s.EndsWith("A1:A")), It.IsAny<CancellationToken>())).ReturnsAsync(() =>
			{
				// setup for returning column A: round header, subheader, game rows
				List<RowData> ret = new List<RowData>((COUNT + 2) * 2);
				for (int i = 0; i < 2; i++)
				{
					DateTime roundDate = i == 0 ? round1Dt : round2Dt;
					ret.Add(CreateSingleCellRow($"ROUND {i + 1}: {roundDate:M/d}" ));
					ret.Add(CreateSingleCellRow("HOME"));
					// rows for games
					for (int j = 0; j < standings.Count(); j += 2)
					{
						ret.Add(CreateSingleCellRow(standings.ElementAt(j).TeamName!));
					}
					// blank rows
					for (int j = 0; j < standings.Count(); j += 2)
					{
						ret.Add(CreateSingleCellRow(string.Empty));
					}
				}
				return ret;
			});
			string? range = null;
			Action<string, CancellationToken> callback = (s, token) => range = s;
			mockClient.Setup(x => x.GetValues(It.Is<string>(s => Regex.IsMatch(s, @"F\d+:N\d+")), It.IsAny<CancellationToken>())).Callback(callback).ReturnsAsync(() =>
			{
				// setup for returning the standings
				List<IList<object>> ret = new List<IList<object>>(standings.Count());
				foreach (StandingsRow row in standings)
				{
					List<object> list = new List<object>
					{
						row.TeamName!,
						row.GamesPlayed,
						row.Wins,
						row.Losses,
						row.Draws,
						row.GamePoints,
						row.RefPoints,
						row.TotalPoints,
						row.Rank
					};
					ret.Add(list);
				}
				return ret;
			});

			AppSettings settings = new AppSettings
			{
				DateOfRound = roundNum == 1 ? round1Dt : round2Dt,
				Divisions = new[] { Constants.DIV_14UG },
			};

			StandingsExtractor service = new StandingsExtractor(settings, mockClient.Object, Mock.Of<ILogger<StandingsExtractor>>());
			IEnumerable<StandingsRow> result = await service.GetStandings(settings.Divisions.First());

			Assert.NotNull(range);
			Assert.StartsWith($"'{Constants.DIV_14UG}'!", range);
			int startRow = (roundNum - 1) * standings.Count() + (roundNum * 2) + 1; // *2 for the header and subheader, +1 to convert to row num from index
			int endRow = startRow + standings.Count();
			Assert.EndsWith($"F{startRow}:N{endRow}", range);

			Assert.NotNull(result);
			Assert.Equal(standings.Count(), result.Count());
			Assert.All(result, x =>
			{
				StandingsRow match = standings.Single(s => s.TeamName == x.TeamName);
				x.Should().BeEquivalentTo(match);
			});
		}

		private RowData CreateSingleCellRow(string? text)
			=> new RowData
			{
				Values = text == null ? null : new List<CellData>
				{
					CreateCellDataForText(text)
				}
			};

		private CellData CreateCellDataForText(string text)
			=> new CellData
			{
				EffectiveValue = new ExtendedValue { StringValue = text },
			};

		private CellData CreateCellDataForNumber(int? number)
			=> new CellData
			{
				EffectiveValue = new ExtendedValue { NumberValue = number },
			};
	}
}
