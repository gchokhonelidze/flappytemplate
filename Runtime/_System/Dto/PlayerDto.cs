#nullable enable

using System;

namespace FlappyTemplate
{
    [Serializable]
    public class PlayerDto
    {
        public long Id;
        public bool Demo;
        public string IPlayerId = string.Empty;
        public bool IsBot;
        public string? IPlayerName;
        public string? IPlayerImg;
        public string? CImg;
        public long ProviderId;
    }
}
