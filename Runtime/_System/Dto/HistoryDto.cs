#nullable enable
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace FlappyTemplate
{
    [Serializable]
    public class HistoryDto
    {
        public string Id = string.Empty;
        public string Sha512Pre = string.Empty;
        public string IPlayerId = string.Empty;
        public string? IPlayerName;
        public string? CImg;
        public string GameName = string.Empty;
        public string BetAmount = "0";
        public string WinAmount = "0";
        public string Currency = string.Empty;
        public string RateUsd = "0";
        public int? N;
        public GenericDictionary<string, string> _Outcome = new();
        public Dictionary<string, JToken>? Outcome;
        public long CreatedAt = 0;
    }
}
