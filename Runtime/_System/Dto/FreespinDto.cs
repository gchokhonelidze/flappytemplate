#nullable enable
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace FlappyTemplate
{
	[Serializable]
	public class FreespinDto
	{
		public string Id { get; set; } = string.Empty;
		public int Left { get; set; }
		public string BetId { get; set; } = string.Empty;
		public string Amount { get; set; } = "0";
		public string Currency { get; set; } = string.Empty;
		public long GameId { get; set; }
		public string? Name { get; set; }
		public int Quantity { get; set; }
		public long ExpiresAfterMs { get; set; }
		public long CreatedAtMs { get; set; }

		public FreespinDto ApplyPatch(string json)
		{
			var patch = Utils.Deserialize<Dictionary<string, JToken>>(json);
			if (patch is null)
				return this;

			foreach (var kv in patch)
			{
				switch (kv.Key)
				{
					case nameof(Id):
						Id = kv.Value.GetString()!;
						break;
					case nameof(Left):
						Left = kv.Value.GetInt32();
						break;
					case nameof(BetId):
						BetId = kv.Value.GetString()!;
						break;
					case nameof(Amount):
						Amount = kv.Value.GetString_Nullable() ?? "0";
						break;
					case nameof(Currency):
						Currency = kv.Value.GetString()!;
						break;
					case nameof(GameId):
						GameId = kv.Value.GetInt64();
						break;
					case nameof(Name):
						Name = kv.Value.GetString();
						break;
					case nameof(Quantity):
						Quantity = kv.Value.GetInt32();
						break;
					case nameof(ExpiresAfterMs):
						ExpiresAfterMs = kv.Value.GetInt64();
						break;
					case nameof(CreatedAtMs):
						CreatedAtMs = kv.Value.GetInt64();
						break;
					default:
						// Ignore unknown fields.
						break;
				}
			}

			return this;
		}

		public static FreespinDto Empty(string currency) =>
			new()
			{
				Id = "",
				BetId = "",
				Left = 0,
				Amount = "0",
				Currency = currency,
				GameId = 0,
				Name = null,
				CreatedAtMs = 0,
				ExpiresAfterMs = 0,
				Quantity = 0,
			};
	}
}
