#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlappyTemplate
{
	[Serializable]
	public class MultiStartRoundDto
	{
		// 	TurnIndex: number;
		//   SeatsTaken: Record<number, TPlayer>;
		//   SeatsInRound: Record<number, TPlayer>;
		[field: SerializeField]
		public int TurnIndex { get; set; }

		[field: SerializeField]
		public GenericDictionary<int, PlayerDto> SeatsTaken { get; set; } = new();

		[field: SerializeField]
		public GenericDictionary<int, PlayerDto> SeatsInRound { get; set; } = new();
	}
}
