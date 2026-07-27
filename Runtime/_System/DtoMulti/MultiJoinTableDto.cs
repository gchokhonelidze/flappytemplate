#nullable enable

using System;
using UnityEngine;

namespace FlappyTemplate
{
	[Serializable]
	public class MultiJoinTableDto
	{
		[field: SerializeField]
		public string Location { get; set; } = string.Empty; //"LOBBY" | "ROOM" | "SEAT";

		[field: SerializeField]
		public int? SeatIndex { get; set; } // when Location is "SEAT", indicates which seat index was taken

		[field: SerializeField]
		public MultiMyRoomDto MyRoom { get; set; } = new();
	}
}
