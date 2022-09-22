using System.Web.UI;

namespace ScoresStandingsHtmlConverter.Services
{
	public class ScoresHtmlWriter : IScoresHtmlWriter, IDisposable
	{
		private readonly StringWriter _stringWriter;
		private readonly HtmlTextWriter _htmlWriter;
		private readonly IFileWriter _fileWriter;
		private bool _disposed;

		public ScoresHtmlWriter(IFileWriter fileWriter)
		{
			_stringWriter = new StringWriter();
			_htmlWriter = new HtmlTextWriter(_stringWriter);
			_fileWriter = fileWriter;
		}

		public async Task WriteScoresToFile(string division, IEnumerable<GameScore> scores)
		{
			_htmlWriter.AddAttribute(HtmlTextWriterAttribute.Class, "scores");
			_htmlWriter.RenderBeginTag(HtmlTextWriterTag.Table);
			_htmlWriter.RenderBeginTag(HtmlTextWriterTag.Tbody);

			foreach (GameScore score in scores)
			{
				string homeTeam = Helpers.StripParenthesesFromTeamName(score.HomeTeam);
				string awayTeam = Helpers.StripParenthesesFromTeamName(score.AwayTeam);

				List<string> rowClasses = new List<string>();
				if (score.Cancelled)
					rowClasses.Add("cancelled");
				if (score.Friendly)
					rowClasses.Add("friendly");
				if (rowClasses.Count > 0)
					_htmlWriter.AddAttribute(HtmlTextWriterAttribute.Class, rowClasses.Aggregate((s1, s2) => $"{s1} {s2}"));
				_htmlWriter.RenderBeginTag(HtmlTextWriterTag.Tr);
				RenderTdTag(homeTeam, "home");
				RenderTdTag(score.Cancelled ? "cancelled" : $"{score.HomeScore}&ndash;{score.AwayScore}", "score");
				RenderTdTag(awayTeam, "away");
				_htmlWriter.RenderEndTag();
			}

			_htmlWriter.RenderEndTag();
			_htmlWriter.RenderEndTag();

			string filename = $"{division} scores.html";
			await _fileWriter.WriteFile(filename, _stringWriter.ToString()!);
		}

		private void RenderTdTag(string text, string cssClass)
		{
			_htmlWriter.AddAttribute(HtmlTextWriterAttribute.Class, cssClass);
			_htmlWriter.RenderBeginTag(HtmlTextWriterTag.Td);
			_htmlWriter.Write(text);
			_htmlWriter.RenderEndTag();
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

		~ScoresHtmlWriter()
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
