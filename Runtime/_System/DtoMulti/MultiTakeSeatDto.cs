#nullable enable
using System;

namespace FlappyTemplate
{
    [Serializable]
    public class MultiTakeSeatDto
    {
        public GenericDictionary<int, PlayerDto> SeatsTaken = new();
    }
}
