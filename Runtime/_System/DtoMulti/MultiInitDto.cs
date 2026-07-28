#nullable enable
using System;
using System.Collections.Generic;

namespace FlappyTemplate
{
    [Serializable]
    public class MultiInitDto
    {
        public int TurnIndex;
        public string? TurnId;
        public ChipBalanceDto ChipBalance = new();
        public string Location = string.Empty; //"LOBBY" | "ROOM" | "SEAT";
        public MultiMyRoomDto? MyRoom;
        public List<MultiRoomDto>? Rooms;
        public int? SeatIndex;
        public SystemDto SystemDto = new();
        public BalanceDto BalanceDto = new();
    }
}
