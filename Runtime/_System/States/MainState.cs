#nullable enable

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace FlappyTemplate
{
    [Serializable]
    public class MainState
    {
        public SystemDto? SystemState;
        public BalanceDto? BalanceState;
        public Dictionary<string, JToken>? GameState;
        public GenericDictionary<string, string> _GameState = new();
        public Dictionary<string, JToken>? IndState;
        public GenericDictionary<string, string> _IndState = new();
        public long ServerTime = 0;
        public ELocale Locale = ELocale.en_US;
        public ErrorDto? Error;
        public FreespinDto? FreespinInfo;
        public Dictionary<string, BetInfoDto> BetInfos = new();
        public TransactionPublic? BetInfoById;
        public List<HistoryDto> History = new();
        public List<GameHistoryDto> GameHistory = new();
        public GameHistoryByIdDto? GameHistoryById;
        public SeedDto? Seeds;
        public StatsDto? Statistics;
        public Dictionary<string, JToken> Settings = new();
        public GenericDictionary<string, string> _Settings = new();
        public Dictionary<string, JToken> SharedData = new();
        public GenericDictionary<string, string> _SharedData = new();
    }
}
