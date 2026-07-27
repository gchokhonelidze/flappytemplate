#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlappyTemplate
{
	public class SeedDto
	{
		public string ClientSalt { get; set; } = string.Empty;
		public int Nonce { get; set; }
		public string ServerSeedSha512 { get; set; } = string.Empty;

		//prev:
		public string? PrevServerSeed { get; set; }
		public string? PrevClientSalt { get; set; }
		public int TotalBetsMade { get; set; }
	}
}
