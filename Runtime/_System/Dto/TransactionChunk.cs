#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlappyTemplate
{
	public class TransactionChunk
	{
		public string Id { get; set; } = string.Empty;
		public string BetId { get; set; } = string.Empty;
		public string Payout { get; set; } = "0";
	}
}
