#nullable enable

using System;
using UnityEngine;

namespace FlappyTemplate
{
	[Serializable]
	public class ChipBalanceInGameDto
	{
		[field: SerializeField]
		public string Value { get; set; } = string.Empty;
	}
}
