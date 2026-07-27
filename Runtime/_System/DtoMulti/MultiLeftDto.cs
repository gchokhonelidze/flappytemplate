#nullable enable

using System;
using UnityEngine;

namespace FlappyTemplate
{
	[Serializable]
	public class MultiLeftDto
	{
		[field: SerializeField]
		public int Total { get; set; }

		[field: SerializeField]
		public int Left { get; set; } // how mant ms left in turn

		[field: SerializeField]
		public int LeftMs { get; set; }
	}
}
