#nullable enable

using System;
using UnityEngine;

namespace FlappyTemplate
{
	[Serializable]
	public class MultiTurnPlayerDto
	{
		[field: SerializeField]
		public int SeatIndex { get; set; }

		[field: SerializeField]
		public PlayerDto Player { get; set; } = new();
	}
}
