#nullable enable
using System;

namespace FlappyTemplate
{
    [Serializable]
    public class StatisticsDto
    {
        public int BetCount = 0;

        public int WinCount = 0;

        public int LoseCount = 0;

        public string Wager = "0";

        public string WagerWon = "0";

        public string WagerLost = "0";

        public string NetProfit = "0";

        public string GrossWin = "0";

        public string Payouts = "0";

        public string Luck = "0";
    }

    [Serializable]
    public class StatsDto
    {
        public StatisticsDto Current = null!;

        public StatisticsDto Overall = null!;
    }
}
