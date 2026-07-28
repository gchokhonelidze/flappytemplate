#nullable enable
using System;

namespace FlappyTemplate
{
    [Serializable]
    public class MultiJoinTableDto
    {
        public string Location = string.Empty; //"LOBBY" | "ROOM" | "SEAT";
        public int? SeatIndex; // when Location is "SEAT", indicates which seat index was taken
        public MultiMyRoomDto MyRoom = new();
    }
}
