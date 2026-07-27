#nullable enable
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace FlappyTemplate
{
	public record TransactionPublic
	{
		public string Id { get; set; } = string.Empty;
		public string BetAmount { get; set; } = "0";
		public BetDto[] Increases { get; set; } = Array.Empty<BetDto>();
		public string WinAmount { get; set; } = "0";
		public string Payout { get; set; } = "0";
		public bool Win { get; set; }
		public string Currency { get; set; } = string.Empty;
		public string? CurrencyImage { get; set; }
		public int DecimalPoints { get; set; }
		public string GameName { get; set; } = string.Empty;
		public string? GameImg { get; set; }
		public string? VerifyUrl { get; set; }
		public string IPlayerId { get; set; } = string.Empty;
		public string? CImg { get; set; }
		public string? IPlayerName { get; set; }
		public string Nonce { get; set; } = string.Empty;
		public int? InGameNonce { get; set; }
		public string ClientSalt { get; set; } = string.Empty;
		public string? ServerSeed { get; set; }
		public string? ServerSeedSha512 { get; set; }
		public string? Hash { get; set; }
		public Dictionary<string, JToken>? Custom { get; set; }
		public Dictionary<string, JToken>? Outcome { get; set; }
		public EGameType GameType { get; set; }
		public bool Finished { get; set; }
		public string HouseEdge { get; set; } = "0";
		public long CreatedAt { get; set; }
	}
}
