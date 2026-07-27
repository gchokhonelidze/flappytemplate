#nullable enable
using System.Collections.Generic;

namespace FlappyTemplate
{
	public class TransactionChunks
	{
		public Dictionary<string, TransactionChunk> Chunks { get; set; } = new();
	}
}
