#nullable enable

using System;
using UnityEngine;

namespace FlappyTemplate
{
    [Serializable]
    public class CoinInfoDto
    {
        public long Id = 0;
        public string Symbol = string.Empty;
        public int DecimalPoints;
        public string RateUsd = "0";
        public string? Image;
    }
}
