#nullable enable
using System;

namespace FlappyTemplate
{
    [Serializable]
    public class MultiTurnPlayerDto
    {
        public int SeatIndex;
        public PlayerDto Player = new();
    }
}
