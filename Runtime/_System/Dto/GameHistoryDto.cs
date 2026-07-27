#nullable enable
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace FlappyTemplate
{
	[Serializable]
	public class GameHistoryDto
	{
		[field: SerializeField]
		public string Id { get; set; } = string.Empty;

		[field: SerializeField]
		public string TotalBetAmountUsd { get; set; } = "0";

		[field: SerializeField]
		public string TotalWinAmountUsd { get; set; } = "0";

		[field: SerializeField]
		public int TotalBetCount { get; set; } = 0;

		[field: SerializeField]
		public GenericDictionary<string, string> _Outcome { get; set; } = new();
		public Dictionary<string, JToken> Outcome { get; set; } = new();
	}

	[Serializable]
	public class GameHistoryByIdDto : GameHistoryDto
	{
		public GenericDictionary<string, BetInfoDto> _Transactions { get; set; } = new();
		public Dictionary<string, BetInfoDto> Transactions { get; set; } = new();
		public string? HouseEdge { get; set; }
		public string? VerifyUrl { get; set; }
		public string? ServerSeed { get; set; }
		public string? ClientSalt { get; set; }
		public string? ServerSeedSha512 { get; set; }
	}
}
