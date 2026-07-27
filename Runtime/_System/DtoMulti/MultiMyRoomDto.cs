#nullable enable

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace FlappyTemplate
{
	[Serializable]
	public class MultiMyRoomDto
	{
		[field: SerializeField]
		public MultiRoomDto MultiRoomInfo { get; set; } = new();

		[field: SerializeField]
		public Dictionary<string, JToken>? PubState { get; set; }

		[field: SerializeField]
		public Dictionary<string, JToken>? IndState { get; set; }

		[field: SerializeField]
		public string? RoundId { get; set; }

		[field: SerializeField]
		public bool Running { get; set; }

		[field: SerializeField]
		public int? Dealer { get; set; }

		[field: SerializeField]
		public int? Turn { get; set; }

		[field: SerializeField]
		public List<long> TurnPlayerIds { get; set; } = new();

		[field: SerializeField]
		public GenericDictionary<int, PlayerDto> SeatsTaken { get; set; } = new();

		[field: SerializeField]
		public GenericDictionary<int, PlayerDto> SeatsInRound { get; set; } = new();
	}
}
