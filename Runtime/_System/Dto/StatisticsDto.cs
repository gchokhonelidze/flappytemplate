#nullable enable
using System;
using UnityEngine;

namespace FlappyTemplate
{
	[Serializable]
	public class StatisticsDto
	{
		[field: SerializeField]
		public int BetCount { get; set; }

		[field: SerializeField]
		public int WinCount { get; set; }

		[field: SerializeField]
		public int LoseCount { get; set; }

		[field: SerializeField]
		public string Wager { get; set; } = "0";

		[field: SerializeField]
		public string WagerWon { get; set; } = "0";

		[field: SerializeField]
		public string WagerLost { get; set; } = "0";

		[field: SerializeField]
		public string NetProfit { get; set; } = "0";

		[field: SerializeField]
		public string GrossWin { get; set; } = "0";

		[field: SerializeField]
		public string Payouts { get; set; } = "0";

		[field: SerializeField]
		public string Luck { get; set; } = "0";
	}

	[Serializable]
	public class StatsDto
	{
		[field: SerializeField]
		public StatisticsDto Current { get; set; } = null!;

		[field: SerializeField]
		public StatisticsDto Overall { get; set; } = null!;
	}
}
