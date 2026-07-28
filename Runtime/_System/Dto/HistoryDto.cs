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
        public string? IPlayerName { get; set; }
        public string? CImg { get; set; }
        public string GameName = string.Empty;
        public string BetAmount = "0";
        public string WinAmount = "0";
        public string Currency = string.Empty;
        public string RateUsd = "0";
        public int? N { get; set; }
        public GenericDictionary<string, string>? _Outcome { get; set; }
        public Dictionary<string, JToken>? Outcome { get; set; }
        public long CreatedAt = 0;
    }
}
