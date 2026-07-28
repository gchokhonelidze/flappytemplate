#nullable enable
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace FlappyTemplate
{
    [Serializable]
    public class GameHistoryDto
    {
        public string Id = string.Empty;
        public string TotalBetAmountUsd = "0";
        public string TotalWinAmountUsd = "0";
        public int TotalBetCount = 0;
        public Dictionary<string, JToken> Outcome = new();
        public GenericDictionary<string, string> _Outcome = new();
    }

    [Serializable]
    public class GameHistoryByIdDto : GameHistoryDto
    {
        public Dictionary<string, BetInfoDto> Transactions = new();
        public GenericDictionary<string, BetInfoDto> _Transactions = new();
        public string? HouseEdge;
        public string? VerifyUrl;
        public string? ServerSeed;
        public string? ClientSalt;
        public string? ServerSeedSha512;
    }
}
