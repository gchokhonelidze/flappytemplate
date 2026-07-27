#nullable enable

using System;
using UnityEngine;

namespace FlappyTemplate
{
	[Serializable]
	public class ChipBalanceIdleDto
	{
		[field: SerializeField]
		public string Value { get; set; } = string.Empty;
	}
}
