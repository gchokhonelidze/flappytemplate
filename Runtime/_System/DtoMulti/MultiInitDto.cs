#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlappyTemplate
{
	[Serializable]
	public class MultiInitDto
	{
		[field: SerializeField]
		public int TurnIndex { get; set; }

		[field: SerializeField]
		public string? TurnId { get; set; }

		[field: SerializeField]
		public ChipBalanceDto ChipBalance { get; set; } = new();

		[field: SerializeField]
		public string Location { get; set; } = string.Empty; //"LOBBY" | "ROOM" | "SEAT";

		[field: SerializeField]
		public MultiMyRoomDto? MyRoom { get; set; }

		[field: SerializeField]
		public List<MultiRoomDto>? Rooms { get; set; }

		[field: SerializeField]
		public int? SeatIndex { get; set; }

		[field: SerializeField]
		public SystemDto SystemDto { get; set; } = new();

		[field: SerializeField]
		public BalanceDto BalanceDto { get; set; } = new();
	}
}
