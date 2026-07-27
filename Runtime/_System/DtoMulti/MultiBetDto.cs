#nullable enable

using System;
using UnityEngine;

namespace FlappyTemplate
{
	[Serializable]
	public class MultiBetDto
	{
		[field: SerializeField]
		public string BetId { get; set; } = string.Empty;

		[field: SerializeField]
		public string Amount { get; set; } = "0";
	}
}
