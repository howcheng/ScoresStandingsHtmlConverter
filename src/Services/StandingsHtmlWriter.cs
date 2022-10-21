using System.Web.UI;
using Microsoft.Extensions.Logging;

namespace ScoresStandingsHtmlConverter.Services
{
	public class StandingsHtmlWriter : IStandingsHtmlWriter, IDisposable
	{
		private static Dictionary<string, int> s_playoffPlaces = new Dictionary<string, int>
		{
			{ Constants.DIV_10UB, 2 },
			{ Constants.DIV_10UG, 2 },
			{ Constants.DIV_12UB, 1 }, // -1 because tournament winner gets a playoff spot
			{ Constants.DIV_12UG, 1 },
			{ Constants.DIV_14UB, 1 },
			{ Constants.DIV_14UG, 1 },
		};

		private readonly StringWriter _stringWriter;
		private readonly HtmlTextWriter _htmlWriter;
		private readonly AppSettings _appSettings;
		private readonly IFileWriter _fileWriter;
		private readonly ILogger<StandingsHtmlWriter> _logger;

		private bool _disposed;

		public StandingsHtmlWriter(AppSettings settings, IFileWriter fileWriter, ILogger<StandingsHtmlWriter> log)
		{
			_stringWriter = new StringWriter();
			_htmlWriter = new HtmlTextWriter(_stringWriter);
			_appSettings = settings;
			_fileWriter = fileWriter;
			_logger = log;
		}

		public async Task WriteStandingsToFile(string division, IEnumerable<StandingsRow> standingsRows)
		{
			_logger.LogInformation($"Beginning standings file for {division}...");

			_htmlWriter.AddAttribute(HtmlTextWriterAttribute.Class, "standings");
			_htmlWriter.RenderBeginTag(HtmlTextWriterTag.Table);
			_htmlWriter.RenderBeginTag(HtmlTextWriterTag.Colgroup);
			RenderColTag(_htmlWriter, 45);
			RenderColTag(_htmlWriter, 257);
			RenderColTag(_htmlWriter, 45);
			RenderColTag(_htmlWriter, 45);
			RenderColTag(_htmlWriter, 45);
			RenderColTag(_htmlWriter, 45);
			RenderColTag(_htmlWriter, 45);
			RenderColTag(_htmlWriter, 45);
			RenderColTag(_htmlWriter, 45);
			_htmlWriter.RenderEndTag();
			_htmlWriter.RenderBeginTag(HtmlTextWriterTag.Thead);
			_htmlWriter.RenderBeginTag(HtmlTextWriterTag.Tr);
			RenderThTag(_htmlWriter, "Rank");
			RenderThTag(_htmlWriter, "Team name");
			RenderThTag(_htmlWriter, "Games");
			RenderThTag(_htmlWriter, "Wins");
			RenderThTag(_htmlWriter, "Losses");
			RenderThTag(_htmlWriter, "Draws");
			RenderThTag(_htmlWriter, "Points");
			RenderThTag(_htmlWriter, "<a href=\"/Default.aspx?tabid=855285\"><strong>Ref points</strong></a>");
			RenderThTag(_htmlWriter, "Total");
			_htmlWriter.RenderEndTag();
			_htmlWriter.RenderEndTag();
			_htmlWriter.RenderBeginTag(HtmlTextWriterTag.Tbody);

			List<StandingsRow> standings = standingsRows.ToList();
			standings.Sort(new StandingsComparer());

			int index = 0;
			int howManyQualifyForPlayoffs = s_playoffPlaces[division];
			foreach (StandingsRow standingsRow in standings)
			{
				string? rowClass = null;
				bool useAltClass = (index % 2) == 1;
				if (_appSettings.CurrentRound >= 6 && standingsRow.Rank <= howManyQualifyForPlayoffs)
					rowClass = useAltClass ? "playoffsalt" : "playoffs";
				else if (useAltClass)
					rowClass = "alt";

				if (!string.IsNullOrEmpty(rowClass))
					_htmlWriter.AddAttribute(HtmlTextWriterAttribute.Class, rowClass);
				_htmlWriter.RenderBeginTag(HtmlTextWriterTag.Tr);

				bool isTiedWithNext = false;
				StandingsRow? next = null, prev = null;
				if (index != (standings.Count - 1))
				{
					next = standings[index + 1];
					isTiedWithNext = standingsRow.Rank == next.Rank;
				}
				bool isTiedWithPrev = false;
				if (index > 0)
				{
					prev = standings[index - 1];
					isTiedWithPrev = standingsRow.Rank == prev.Rank;
				}
				string rank = standingsRow.Rank.ToString();
				if (isTiedWithNext || isTiedWithPrev)
				{
					rank = $"T{rank}";
				}
				RenderTdTag(_htmlWriter, rank);
				RenderTdTag(_htmlWriter, standingsRow.TeamName!);
				RenderTdTag(_htmlWriter, standingsRow.GamesPlayed.ToString());
				RenderTdTag(_htmlWriter, standingsRow.Wins.ToString());
				RenderTdTag(_htmlWriter, standingsRow.Losses.ToString());
				RenderTdTag(_htmlWriter, standingsRow.Draws.ToString());
				RenderTdTag(_htmlWriter, standingsRow.GamePoints.ToString());
				RenderTdTag(_htmlWriter, standingsRow.RefPoints.ToString("0.0"));
				RenderTdTag(_htmlWriter, standingsRow.TotalPoints.ToString("0.0"));

				_htmlWriter.RenderEndTag();
				index += 1;
			}

			_htmlWriter.RenderEndTag();
			_htmlWriter.RenderEndTag();

			// write the file
			string filename = $"{division} standings.html";
			await _fileWriter.WriteFile(filename, _stringWriter.ToString()!);
		}

		private void RenderColTag(HtmlTextWriter _htmlWriter, int width)
		{
			_htmlWriter.AddAttribute(HtmlTextWriterAttribute.Width, width.ToString());
			_htmlWriter.RenderBeginTag(HtmlTextWriterTag.Col);
			_htmlWriter.RenderEndTag();
		}
		private void RenderThTag(HtmlTextWriter _htmlWriter, string content)
		{
			_htmlWriter.RenderBeginTag(HtmlTextWriterTag.Th);
			_htmlWriter.Write(content);
			_htmlWriter.RenderEndTag();
		}
		private void RenderTdTag(HtmlTextWriter _htmlWriter, string content)
		{
			_htmlWriter.RenderBeginTag(HtmlTextWriterTag.Td);
			_htmlWriter.Write(content);
			_htmlWriter.RenderEndTag();
		}

		private class StandingsComparer : IComparer<StandingsRow>
		{
			public int Compare(StandingsRow? x, StandingsRow? y)
			{
				if (x == null && y == null)
					return 0;
				if (x == null && y != null)
					return -1;
				if (x != null && y == null)
					return 1;
				if (x!.Rank < y!.Rank)
					return -1;
				if (x!.Rank > y!.Rank)
					return 1;
				return string.Compare(x.TeamName, y.TeamName);
			}
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					_stringWriter.Dispose();
					_htmlWriter.Dispose();
				}

				_disposed = true;
			}
		}

		~StandingsHtmlWriter()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: false);
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
