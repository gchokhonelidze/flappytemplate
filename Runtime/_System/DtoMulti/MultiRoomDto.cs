#nullable enable

using System;
using UnityEngine;

namespace FlappyTemplate
{
	[Serializable]
	public class MultiRoomDto
	{
		[field: SerializeField]
		public int Id { get; set; }

		[field: SerializeField]
		public string TypeName { get; set; } = string.Empty;

		[field: SerializeField]
		public bool Demo { get; set; }

		[field: SerializeField]
		public CoinInfoDto CoinInfo { get; set; } = new();

		[field: SerializeField]
		public int MaxThinkDurationMs { get; set; }

		[field: SerializeField]
		public int MinPlayers { get; set; }

		[field: SerializeField]
		public int MaxPlayers { get; set; }
	}
}
