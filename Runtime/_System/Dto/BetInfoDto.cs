#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

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

        /// <summary>
        /// Reads an ON_BET_INFO payload into one map of transaction id to bet. The server sends a list of
        /// one-entry objects, <c>[ { id: bet }, ... ]</c>; the older form, <c>{ id: bet, ... }</c>, is read too,
        /// the way the web front does. Anything unreadable is logged and left out.
        /// </summary>
        public static Dictionary<string, BetInfoDto> ReadMap(string json)
        {
            var result = new Dictionary<string, BetInfoDto>();
            JToken token;
            try
            {
                token = JToken.Parse(json);
            }
            catch (JsonException ex)
            {
                Debug.LogError($"Failed to parse bet info JSON: {ex.Message}");
                return result;
            }

            void Add(JObject entries)
            {
                foreach (var (id, value) in entries)
                {
                    if (value is null)
                        continue;
                    var dto = Utils.TryDeserialize<BetInfoDto>(value.ToString(Formatting.None));
                    if (dto != null)
                        result[id] = dto;
                }
            }

            switch (token)
            {
                case JArray list:
                    foreach (var entry in list.OfType<JObject>())
                        Add(entry);
                    break;
                case JObject map:
                    Add(map);
                    break;
                default:
                    Debug.LogWarning($"Discarded bet info that is neither a list nor an object: {json}");
                    break;
            }
            return result;
        }

        public void ApplyPatch(string json)
        {
            var patchArray = ReadMap(json);
            if (patchArray.Count == 0)
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
