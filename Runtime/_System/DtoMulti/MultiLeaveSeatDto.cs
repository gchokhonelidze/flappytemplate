#nullable enable

using System;
using UnityEngine;

namespace FlappyTemplate
{
	[Serializable]
	public class MultiLeaveSeatDto
	{
		[field: SerializeField]
		public string IPlayerId { get; set; } = string.Empty;

		[field: SerializeField]
		public int LeftSeatId { get; set; }
	}
}
