#nullable enable

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FlappyTemplate
{
	[Serializable]
	public class MainEvents
	{
		public static MainEvents Inst { get; private set; } = null!;

		public MainEvents()
		{
			Inst = this;
		}

		public UnityEvent<SystemDto> OnSystem = new();

		public UnityEvent<int> OnSystemLeft = new();

		public UnityEvent<bool> OnSystemRunning = new();

		public UnityEvent<BalanceDto> OnBalance = new();

		public UnityEvent<ErrorDto> OnError = new();

		public UnityEvent<Dictionary<string, JToken>> OnState = new();

		public UnityEvent<Dictionary<string, JToken>> OnIndState = new();

		public UnityEvent<FreespinDto> OnFreespin = new();

		public UnityEvent<BetInfoDto> OnBetInfo = new();

		public UnityEvent<BetInfoDto> OnBetInfoPayout = new();

		public UnityEvent<BetInfoDto> OnBetInfoIncrease = new();

		public UnityEvent<TransactionPublic> OnBetInfoById = new();

		public UnityEvent<HistoryDto> OnHistory = new();

		public UnityEvent<GameHistoryDto> OnGameHistory = new();

		public UnityEvent<GameHistoryByIdDto> OnGameHistoryById = new();

		public UnityEvent<SeedDto> OnSeed = new();

		public UnityEvent<StatsDto> OnStatistics = new();

		public UnityEvent<Dictionary<string, JToken>> OnSettings = new();

		public UnityEvent<Dictionary<string, JToken>> OnSharedData = new();

		public UnityEvent OnMultiRoom = new();
		public UnityEvent OnMultiLocation = new();
		public UnityEvent OnMultiSeatsChanged = new();
		public UnityEvent<MultiTurnUpdateDto> OnMultiTurn = new();
		public UnityEvent<MultiLeftDto> OnMultiLeft = new();
		public UnityEvent<ChipBalanceDto> OnMultiChipBalance = new();
	}
}
