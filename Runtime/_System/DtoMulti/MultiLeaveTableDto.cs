#nullable enable
using System;

namespace FlappyTemplate
{
    [Serializable]
    public class MultiLeaveTableDto
    {
        public string IPlayerId = string.Empty;
        public long TableId;
        public int? LeftSeatId;
    }
}
