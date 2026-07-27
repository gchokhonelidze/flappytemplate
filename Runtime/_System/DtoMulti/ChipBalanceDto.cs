#nullable enable

using System;
using UnityEngine;

namespace FlappyTemplate
{
	[Serializable]
	public class ChipBalanceDto
	{
		[field: SerializeField]
		public string Total { get; set; } = string.Empty;

		[field: SerializeField]
		public string Idle { get; set; } = string.Empty;

		[field: SerializeField]
		public string InGame { get; set; } = string.Empty;
	}
}
