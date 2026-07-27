#nullable enable

using System;
using UnityEngine;

namespace FlappyTemplate
{
	[Serializable]
	public class CoinInfoDto
	{
		[field: SerializeField]
		public long Id { get; set; } = 0;

		[field: SerializeField]
		public string Symbol { get; set; } = string.Empty;

		[field: SerializeField]
		public int DecimalPoints { get; set; }

		[field: SerializeField]
		public string RateUsd { get; set; } = "0";

		[field: SerializeField]
		public string? Image { get; set; }
	}
}
