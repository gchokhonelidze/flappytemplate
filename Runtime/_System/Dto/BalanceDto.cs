#nullable enable

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace FlappyTemplate
{
    [Serializable]
    public class BalanceDto
    {
        public string? TransactionId;
        public string Currency = null!;
        public string Balance = string.Empty;
        public string RateUsd = string.Empty;
        public string? BetAmount;
        public string? WinAmount;
        public string? CurrencyImage;

        public BalanceDto ApplyPatch(string json)
        {
            var patch = Utils.Deserialize<Dictionary<string, JToken>>(json);
            if (patch is null)
                return this;

            foreach (var kv in patch)
            {
                switch (kv.Key)
                {
                    case nameof(TransactionId):
                        TransactionId = kv.Value.GetString_Nullable();
                        break;
                    case nameof(Currency):
                        Currency = kv.Value.GetString()!;
                        break;
                    case nameof(Balance):
                        Balance = kv.Value.GetString()!;
                        break;
                    case nameof(RateUsd):
                        RateUsd = kv.Value.GetDouble().ToString();
                        break;
                    case nameof(BetAmount):
                    {
                        BetAmount = kv.Value.GetString_Nullable();
                        break;
                    }
                    case nameof(WinAmount):
                        WinAmount = kv.Value.GetString_Nullable();
                        break;
                    case nameof(CurrencyImage):
                        CurrencyImage = kv.Value.GetString();
                        break;
                    default:
                        // Ignore unknown fields.
                        break;
                }
            }
            return this;
        }
    }
}
