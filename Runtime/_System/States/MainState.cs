#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace FlappyTemplate
{
	[Serializable]
	public class MainState
	{
		// [ReadOnly]
		[SerializeField]
		public SystemDto? SystemState;

		[SerializeField]
		public BalanceDto? BalanceState;
		public Dictionary<string, JToken>? GameState;
		public GenericDictionary<string, string> _GameState = new();
		public Dictionary<string, JToken>? IndState;
		public long ServerTime = 0;
		public ELocale Locale = ELocale.en_US;

		[SerializeField]
		public ErrorDto? Error;

		[SerializeField]
		public FreespinDto? FreespinInfo;
		public Dictionary<string, BetInfoDto> BetInfos = new();
		public TransactionPublic? BetInfoById;

		[SerializeField]
		public List<HistoryDto> History = new();

		[SerializeField]
		public List<GameHistoryDto> GameHistory = new();

		[SerializeField]
		public GameHistoryByIdDto? GameHistoryById;

		[SerializeField]
		public SeedDto? Seeds;

		[SerializeField]
		public StatsDto? Statistics;
		public Dictionary<string, JToken> Settings = new();
		public Dictionary<string, JToken> SharedData = new();
	}
}
