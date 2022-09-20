using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Sheets.v4.Data;

namespace ScoresStandingsHtmlConverter.Services
{
	public static class Helpers
	{
		public static System.Text.RegularExpressions.Regex RoundNumRegex = new System.Text.RegularExpressions.Regex(@"ROUND (\d+)");

		public static int GetRoundNumberFromCellData(CellData cellData)
		{
			string cellValue = cellData.EffectiveValue.StringValue;
			var match = RoundNumRegex.Match(cellValue);
			int roundNum = int.Parse(match.Groups.Values.Last().Value);
			return roundNum;
		}

		public static bool CellDataContainsRoundNumber(CellData cellData)
		{
			string cellValue = cellData.EffectiveValue.StringValue;
			return RoundNumRegex.IsMatch(cellValue);
		}
	}
}
