#nullable enable

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace FlappyTemplate
{
	[Serializable]
	public class MultiTurnUpdateDto
	{
		[field: SerializeField]
		public int TurnIndex { get; set; }

		[field: SerializeField]
		public string TurnId { get; set; } = string.Empty;

		[field: SerializeField]
		public List<long> TurnPlayerIds { get; set; } = new();

		[field: SerializeField]
		public int? NextDealer { get; set; }

		[field: SerializeField]
		public int? NextTurn { get; set; }
		public Dictionary<string, JToken> SPubPartial { get; set; } = new();

		[field: SerializeField]
		public bool RoundEnd { get; set; }
	}
}
