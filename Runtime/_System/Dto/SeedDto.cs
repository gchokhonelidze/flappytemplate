#nullable enable
using System;

namespace FlappyTemplate
{
    [Serializable]
    public class SeedDto
    {
        public string ClientSalt = string.Empty;
        public int Nonce;
        public string ServerSeedSha512 = string.Empty;

        //prev:
        public string? PrevServerSeed;
        public string? PrevClientSalt;
        public int TotalBetsMade;
    }
}
