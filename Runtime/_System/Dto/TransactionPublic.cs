#nullable enable
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace FlappyTemplate
{
    [Serializable]
    public class TransactionPublic
    {
        public string Id = string.Empty;
        public string BetAmount = "0";
        public BetDto[] Increases = Array.Empty<BetDto>();
        public string WinAmount = "0";
        public string Payout = "0";
        public bool Win;
        public string Currency = string.Empty;
        public string? CurrencyImage;
        public int DecimalPoints;
        public string GameName = string.Empty;
        public string? GameImg;
        public string? VerifyUrl;
        public string IPlayerId = string.Empty;
        public string? CImg;
        public string? IPlayerName;
        public string Nonce = string.Empty;
        public int? InGameNonce;
        public string ClientSalt = string.Empty;
        public string? ServerSeed;
        public string? ServerSeedSha512;
        public string? Hash;
        public Dictionary<string, JToken>? Custom;
        public GenericDictionary<string, string> _Custom = new();
        public Dictionary<string, JToken>? Outcome;
        public GenericDictionary<string, string> _Outcome = new();
        public EGameType GameType;
        public bool Finished;
        public string HouseEdge = "0";
        public long CreatedAt;
    }
}
