#nullable enable
using System;
using UnityEngine;

namespace FlappyTemplate
{
    [Serializable]
    public class MultiStartRoundDto
    {
        // 	TurnIndex: number;
        //   SeatsTaken: Record<number, TPlayer>;
        //   SeatsInRound: Record<number, TPlayer>;
        public int TurnIndex;
        public GenericDictionary<int, PlayerDto> SeatsTaken = new();
        public GenericDictionary<int, PlayerDto> SeatsInRound = new();
    }
}
