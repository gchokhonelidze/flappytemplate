#nullable enable
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace FlappyTemplate
{
    [Serializable]
    public class MultiMyRoomDto
    {
        public MultiRoomDto MultiRoomInfo = new();
        public Dictionary<string, JToken>? PubState;
        public Dictionary<string, JToken>? IndState;
        public string? RoundId;
        public bool Running;
        public int? Dealer;
        public int? Turn;
        public List<long> TurnPlayerIds = new();
        public GenericDictionary<int, PlayerDto> SeatsTaken = new();
        public GenericDictionary<int, PlayerDto> SeatsInRound = new();
    }
}
