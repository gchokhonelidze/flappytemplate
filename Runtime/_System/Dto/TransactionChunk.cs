#nullable enable
using System;

namespace FlappyTemplate
{
    [Serializable]
    public class TransactionChunk
    {
        public string Id = string.Empty;
        public string BetId = string.Empty;
        public string Payout = "0";
    }
}
