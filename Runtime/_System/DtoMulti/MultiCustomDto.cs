#nullable enable
using System;

namespace FlappyTemplate
{
    [Serializable]
    public class MultiCustomDto
    {
        public string TurnId = string.Empty;
        public string EventName = string.Empty;
        public object CustomData = new();
        public MultiBetDto[]? Bets = null;
    }
}
