#nullable enable
using System;
using System.Collections.Generic;

namespace FlappyTemplate
{
    [Serializable]
    public class TransactionChunks
    {
        public Dictionary<string, TransactionChunk> Chunks { get; set; } = new();
    }
}
