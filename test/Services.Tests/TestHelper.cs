using Google.Apis.Sheets.v4.Data;
using HtmlAgilityPack;

namespace ScoresStandingsHtmlConverter.Services.Tests
{
	public static class TestHelper
	{
		public static DateTime CreateRoundDateFromNumber(int roundNum)
		{
			// Use a fixed date for testing: first Saturday of September
			// This represents the typical season start and avoids year boundary issues
			DateTime september1 = new DateTime(DateTime.Today.Year, 9, 1);
			int daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)september1.DayOfWeek + 7) % 7;
			DateTime firstSaturday = september1.AddDays(daysUntilSaturday);
			
			return firstSaturday.AddDays((roundNum - 1) * 7);
		}

		public static string GetHtmlCellContent(HtmlNode tr, int idx)
			=> GetHtmlCellContent(tr.ChildNodes.Where(x => x.Name == "td").ElementAt(idx));

		public static string GetHtmlCellContent(HtmlNode td)
			=> td.InnerText.Trim();
	}
}
