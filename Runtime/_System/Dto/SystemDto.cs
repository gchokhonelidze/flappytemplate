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
        public string HouseEdge = string.Empty;
        public bool Running;
        public bool IsDemo;
        public EGameType? GameType;
        public long? GameId;
        public long? ProviderId;
        public string? ProviderName;
        public int? DecimalPoints;
        public string? MinBet;
        public string? MaxBet;
        public string? MaxWin;

        [SerializeField]
        private string _MeId = string.Empty;
        private bool MeIdHasValue;
        public long? MeId
        {
            get => MeIdHasValue ? long.Parse(_MeId) : null;
            set
            {
                MeIdHasValue = value.HasValue;
                _MeId = value.HasValue ? value.Value.ToString() : string.Empty;
            }
        }
        public string? Me; //IPlayerId

        [SerializeField]
        private string _Left = string.Empty;
        private bool LeftHasValue;
        public int? Left
        {
            get => LeftHasValue ? int.Parse(_Left) : null;
            set
            {
                LeftHasValue = value.HasValue;
                _Left = value.HasValue ? value.Value.ToString() : string.Empty;
            }
        }

        [SerializeField]
        private string _Ms = string.Empty;
        private bool MsHasValue;
        public int? Ms
        {
            get => MsHasValue ? int.Parse(_Ms) : null;
            set
            {
                MsHasValue = value.HasValue;
                _Ms = value.HasValue ? value.Value.ToString() : string.Empty;
            }
        }
        public string? Language;
        public string? ReturnUrl;
        public string? Version;
        public bool? BattleReady;

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
