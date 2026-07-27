#nullable enable

using System;
using UnityEngine;

namespace FlappyTemplate
{
	[Serializable]
	public class MultiLeaveTableDto
	{
		[field: SerializeField]
		public string IPlayerId { get; set; } = string.Empty;

		[field: SerializeField]
		public long TableId { get; set; }

		[field: SerializeField]
		public int? LeftSeatId { get; set; }
	}
}
