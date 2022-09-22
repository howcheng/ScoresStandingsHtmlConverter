using Google.Apis.Sheets.v4.Data;
using HtmlAgilityPack;

namespace ScoresStandingsHtmlConverter.Services.Tests
{
	public static class TestHelper
	{
		public static DateTime CreateRoundDateFromNumber(int roundNum)
			=> DateTime.Today.AddDays((roundNum - 1) * 7);

		public static string GetHtmlCellContent(HtmlNode tr, int idx)
			=> GetHtmlCellContent(tr.ChildNodes.Where(x => x.Name == "td").ElementAt(idx));

		public static string GetHtmlCellContent(HtmlNode td)
			=> td.InnerText.Trim();
	}
}
