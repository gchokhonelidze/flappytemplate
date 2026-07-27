#nullable enable

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace FlappyTemplate
{
	[Serializable]
	public class SystemDto
	{
		[field: SerializeField]
		public string HouseEdge { get; set; } = string.Empty;

		[field: SerializeField]
		public bool Running { get; set; }

		[field: SerializeField]
		public bool IsDemo { get; set; }
		public EGameType? GameType { get; set; }

		[field: SerializeField]
		public long? GameId { get; set; }
		public long? ProviderId { get; set; }
		public string? ProviderName { get; set; }
		public int? DecimalPoints { get; set; }

		[field: SerializeField]
		public string? MinBet { get; set; }

		[field: SerializeField]
		public string? MaxBet { get; set; }

		[field: SerializeField]
		public string? MaxWin { get; set; }
		private bool hasMeId;

		[SerializeField]
		private long meIdValue;

		public long? MeId
		{
			get => hasMeId ? meIdValue : null;
			set
			{
				hasMeId = value.HasValue;
				meIdValue = value.GetValueOrDefault();
			}
		}

		[field: SerializeField]
		public string? Me { get; set; } //IPlayerId
		private bool LeftHasValue;

		[SerializeField]
		private int LeftValue;

		public int? Left
		{
			get => LeftHasValue ? LeftValue : null;
			set
			{
				LeftHasValue = value.HasValue;
				LeftValue = value.GetValueOrDefault();
			}
		}
		private bool MsHasValue;

		[SerializeField]
		private int MsValue;
		public int? Ms
		{
			get => MsHasValue ? MsValue : null;
			set
			{
				MsHasValue = value.HasValue;
				MsValue = value.GetValueOrDefault();
			}
		}
		public string? Language { get; set; }
		public string? ReturnUrl { get; set; }
		public string? Version { get; set; }
		public bool? BattleReady { get; set; }

		public SystemDto ApplyPatch(string json)
		{
			var patch = Utils.Deserialize<Dictionary<string, JToken>>(json);
			if (patch is null)
				return this;

			foreach (var kv in patch)
			{
				switch (kv.Key)
				{
					case nameof(HouseEdge):
						HouseEdge = kv.Value.GetString_Nullable()!;
						break;
					case nameof(Running):
					{
						var _running = kv.Value.GetBoolean();
						if (Running != _running)
						{
							Running = _running;
							StateManager.Inst.Events.OnSystemRunning?.Invoke(Running);
						}
						break;
					}
					case nameof(IsDemo):
						IsDemo = kv.Value.GetBoolean();
						break;
					case nameof(GameType):
						GameType = (EGameType)kv.Value.GetInt32();
						break;
					case nameof(GameId):
						GameId = kv.Value.GetInt64();
						break;
					case nameof(ProviderId):
						ProviderId = kv.Value.GetInt64();
						break;
					case nameof(ProviderName):
						ProviderName = kv.Value.GetString();
						break;
					case nameof(DecimalPoints):
						DecimalPoints = kv.Value.GetInt32();
						break;
					case nameof(MinBet):
						MinBet = kv.Value.GetString();
						break;
					case nameof(MaxBet):
						MaxBet = kv.Value.GetString();
						break;
					case nameof(MaxWin):
						MaxWin = kv.Value.GetString();
						break;
					case nameof(MeId):
						MeId = kv.Value.GetInt64();
						break;
					case nameof(Me):
						Me = kv.Value.GetString();
						break;
					case nameof(Left):
						Left = kv.Value.GetInt32_Nullable();
						if (Left is not null)
							StateManager.Inst.Events.OnSystemLeft?.Invoke(Left.Value);
						break;
					case nameof(Ms):
						Ms = kv.Value.GetInt32();
						break;
					case nameof(Language):
						Language = kv.Value.GetString();
						break;
					case nameof(ReturnUrl):
						ReturnUrl = kv.Value.GetString();
						break;
					case nameof(Version):
						Version = kv.Value.GetString();
						break;
					case nameof(BattleReady):
						BattleReady = kv.Value.GetBoolean();
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
