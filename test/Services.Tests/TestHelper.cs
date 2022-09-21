namespace ScoresStandingsHtmlConverter.Services.Tests
{
	public static class TestHelper
	{
		public static DateTime CreateRoundDateFromNumber(int roundNum)
			=> DateTime.Today.AddDays((roundNum - 1) * 7);
	}
}
