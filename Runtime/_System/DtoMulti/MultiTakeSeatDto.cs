#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlappyTemplate
{
	[Serializable]
	public class MultiTakeSeatDto
	{
		[field: SerializeField]
		public GenericDictionary<int, PlayerDto> SeatsTaken { get; set; } = new();
	}
}
