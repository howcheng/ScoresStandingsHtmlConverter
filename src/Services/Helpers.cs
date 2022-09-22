using System.Text.RegularExpressions;
using Google.Apis.Sheets.v4.Data;

namespace ScoresStandingsHtmlConverter.Services
{
	public static class Helpers
	{
		public static Regex RoundNumRegex = new Regex(@"ROUND (\d+): (\d+/\d+)");

		public static int GetRoundNumberFromCellData(CellData cellData)
		{
			string cellValue = cellData.EffectiveValue.StringValue;
			Match match = RoundNumRegex.Match(cellValue);
			int roundNum = int.Parse(match.Groups.Values.ElementAt(1).Value);
			return roundNum;
		}

		public static bool IsRoundHeaderCell(CellData cellData)
		{
			string? cellValue = cellData.EffectiveValue?.StringValue;
			if (cellValue == null)
				return false;
			return RoundNumRegex.IsMatch(cellValue);
		}

		public static bool IsRoundHeaderCell(string value) => RoundNumRegex.IsMatch(value);

		public static DateTime GetDateOfRoundFromCellValue(string value)
		{
			Match match = RoundNumRegex.Match(value);
			DateTime dt = DateTime.Parse($"{DateTime.Today.Year}/{match.Groups.Values.Last().Value}");
			return dt;
		}

		public static string StripParenthesesFromTeamName(string teamName)
		{
			int idx = teamName.IndexOf("(");
			if (idx > -1)
				teamName = teamName.Substring(0, idx - 1);
			return teamName;
		}
	}
}
