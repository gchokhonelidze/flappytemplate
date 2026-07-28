#nullable enable

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace FlappyTemplate
{
    [Serializable]
    public class MultiTurnUpdateDto
    {
        public int TurnIndex;
        public string TurnId = string.Empty;
        public List<long> TurnPlayerIds = new();
        public int? NextDealer;
        public int? NextTurn;
        public Dictionary<string, JToken> SPubPartial = new();
        public bool RoundEnd;
    }
}
