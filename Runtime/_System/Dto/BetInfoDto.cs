#nullable enable
using System;
using System.Collections.Generic;

namespace FlappyTemplate
{
    [Serializable]
    public class BetChunkDto
    {
        /// <summary>game bet place id (ex.: reds, blacks, 0)</summary>
        public string BetId = string.Empty;
        public string Amount = "0";
        public string Payout = "0";
        public bool Win;
        public List<string> Increases = new();
    }

    [Serializable]
    public class BetInfoDto
    {
        /// <summary>IPlayerId</summary
        public string PlayerId = string.Empty;
        public string? PlayerName;
        public string? PlayerImage;
        public int? Nonce;
        public string Currency = string.Empty;
        public int DecimalPoints;
        public long BetMs;
        public string RateUsd = "0";
        public long? WinMs;
        public BetChunkDto[] Bets = null!;
        public string? CurrencyImage;
        public string TotalBetUsd = "0";
        public string TotalProfitUsd = "0";

        public void ApplyPatch(string json)
        {
            var patchArray = Utils.Deserialize<Dictionary<string, BetInfoDto>>(json);
            if (patchArray is null || patchArray.Count == 0)
                return;

            foreach (var (id, betInfoDto) in patchArray!)
            {
                StateManager.Inst.MainState.BetInfos[id] = betInfoDto;
            }

            // for (var i = 0; i < patchArray.Length; i++)
            // {
            // 	var patch = patchArray[i];

            // 	StateManager.Inst.MainState.BetInfos.TryGetValue(patch.k).Value.GetString() ?? string.Empty, out var existing);
            // 	foreach (var kv in patchArray[i])
            // 	{
            // 		switch (kv.Key)
            // 		{
            // 			case nameof(PlayerId):
            // 				patched.PlayerId = kv.Value.GetString()!;
            // 				break;
            // 			case nameof(PlayerName):
            // 				patched.PlayerName = kv.Value.GetString();
            // 				break;
            // 			case nameof(PlayerImage):
            // 				patched.PlayerImage = kv.Value.GetString();
            // 				break;
            // 			case nameof(Nonce):
            // 				patched.Nonce = kv.Value.GetInt32();
            // 				break;
            // 			case nameof(Currency):
            // 				patched.Currency = kv.Value.GetString()!;
            // 				break;
            // 			case nameof(DecimalPoints):
            // 				patched.DecimalPoints = kv.Value.GetInt32();
            // 				break;
            // 			case nameof(BetMs):
            // 				patched.BetMs = kv.Value.GetInt64();
            // 				break;
            // 			case nameof(RateUsd):
            // 				patched.RateUsd = kv.Value.GetDecimal();
            // 				break;
            // 			case nameof(WinMs):
            // 				patched.WinMs = kv.Value.GetInt64();
            // 				break;
            // 			case nameof(Bets):
            // 				patched.Bets = kv.Value.Deserialize<BetChunkDto[]>() ?? new BetChunkDto[0];
            // 				break;
            // 			case nameof(CurrencyImage):
            // 				patched.CurrencyImage = kv.Value.GetString();
            // 				break;
            // 			case nameof(TotalBetUsd):
            // 				patched.TotalBetUsd = kv.Value.GetDecimal();
            // 				break;
            // 			case nameof(TotalProfitUsd):
            // 				patched.TotalProfitUsd = kv.Value.GetDecimal();
            // 				break;
            // 			default:
            // 				// Ignore unknown fields.
            // 				break;
            // 		}
            // 	}
            // 	result[i] = patched;
            // }

            // return result;
        }
    }
}
