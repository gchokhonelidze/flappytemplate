#nullable enable
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace FlappyTemplate
{
	[Serializable]
	public class ErrorDto
	{
		[field: SerializeField]
		public bool Error { get; set; } = false;

		[field: SerializeField]
		public string Message { get; set; } = string.Empty;
		public string? Detail { get; set; }
		public string? Type { get; set; }

		public ErrorDto ApplyPatch(string json)
		{
			var patch = Utils.Deserialize<Dictionary<string, JToken>>(json);
			if (patch is null)
				return this;

			foreach (var kv in patch)
			{
				switch (kv.Key)
				{
					case nameof(Error):
						Error = kv.Value.GetBoolean();
						break;
					case nameof(Message):
						Message = kv.Value.GetString()!;
						if (Message == "freespins_expired")
						{
							StateManager.Inst.MainState.FreespinInfo = null;
						}
						break;
					case nameof(Detail):
						Detail = kv.Value.GetString();
						break;
					case nameof(Type):
						Type = kv.Value.GetString();
						break;
					default:
						// Ignore unknown fields.
						break;
				}
			}
			return this;
		}
	}
}
