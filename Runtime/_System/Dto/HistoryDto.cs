#nullable enable
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace FlappyTemplate
{
	[Serializable]
	public class HistoryDto
	{
		[field: SerializeField]
		public string Id { get; set; } = string.Empty;

		[field: SerializeField]
		public string Sha512Pre { get; set; } = string.Empty;
		public string IPlayerId { get; set; } = string.Empty;
		public string? IPlayerName { get; set; }
		public string? CImg { get; set; }
		public string GameName { get; set; } = string.Empty;

		[field: SerializeField]
		public string BetAmount { get; set; } = "0";

		[field: SerializeField]
		public string WinAmount { get; set; } = "0";

		[field: SerializeField]
		public string Currency { get; set; } = string.Empty;

		[field: SerializeField]
		public string RateUsd { get; set; } = "0";

		[field: SerializeField]
		public int? N { get; set; }

		[field: SerializeField]
		public GenericDictionary<string, string>? _Outcome { get; set; }
		public Dictionary<string, JToken>? Outcome { get; set; }
		public long CreatedAt { get; set; } = 0;
	}
}
