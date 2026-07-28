#nullable enable
using System;

namespace FlappyTemplate
{
    [Serializable]
    public class MultiRoomDto
    {
        public int Id;
        public string TypeName = string.Empty;
        public bool Demo;
        public CoinInfoDto CoinInfo = new();
        public int MaxThinkDurationMs;
        public int MinPlayers;
        public int MaxPlayers;
    }
}
