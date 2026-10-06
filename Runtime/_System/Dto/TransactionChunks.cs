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
    public class TransactionChunks
    {
        /// <summary>
        /// Reads an ON_BET_INFO_PAYOUT payload into one map of transaction id to the bets of that transaction
        /// that won. The server sends <c>{ id: [ { Id, BetId, Payout }, ... ], ... }</c>, the way the web front
        /// reads it. Anything unreadable is logged and left out.
        /// </summary>
        public static Dictionary<string, TransactionChunk[]> ReadMap(string json)
        {
            var result = new Dictionary<string, TransactionChunk[]>();
            JToken token;
            try
            {
                token = JToken.Parse(json);
            }
            catch (JsonException ex)
            {
                Debug.LogError($"Failed to parse bet payout JSON: {ex.Message}");
                return result;
            }

            if (token is not JObject map)
            {
                Debug.LogWarning($"Discarded bet payout that is not an object: {json}");
                return result;
            }

            foreach (var (id, value) in map)
            {
                if (value is not JArray list)
                {
                    Debug.LogWarning($"Discarded bet payout for {id} that is not a list: {value}");
                    continue;
                }
                var chunks = list.OfType<JObject>()
                    .Select(entry => Utils.TryDeserialize<TransactionChunk>(entry.ToString(Formatting.None)))
                    .Where(chunk => chunk != null)
                    .Select(chunk => chunk!)
                    .ToArray();
                if (chunks.Length > 0)
                    result[id] = chunks;
            }
            return result;
        }
    }
}
