#nullable enable

using System;
using UnityEngine;

namespace FlappyTemplate
{
	[Serializable]
	public class PlayerDto
	{
		[field: SerializeField]
		public long Id { get; set; }

		[field: SerializeField]
		public bool Demo { get; set; }

		[field: SerializeField]
		public string IPlayerId { get; set; } = string.Empty;

		[field: SerializeField]
		public bool IsBot { get; set; }

		[field: SerializeField]
		public string? IPlayerName { get; set; }

		[field: SerializeField]
		public string? IPlayerImg { get; set; }

		[field: SerializeField]
		public string? CImg { get; set; }

		[field: SerializeField]
		public long ProviderId { get; set; }
	}
}
