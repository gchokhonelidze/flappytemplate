#nullable enable
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace FlappyTemplate
{
	public class Emitter
	{
		public static Emitter Inst { get; private set; } = null!;

		public Emitter()
		{
			Inst = this;
		}

		public void OnSettingSet(SettingDto settingDto)
		{
			var el = new Dictionary<string, string> { [settingDto.Name] = Utils.Serialize(settingDto.Value) };
			Socket.Inst.Send(nameof(EPlayerEvent.SETTING), el);
		}

		public void OnSettingsSet(List<SettingDto> settingDtos)
		{
			var el = new Dictionary<string, string>();
			foreach (var setting in settingDtos)
			{
				el[setting.Name] = Utils.Serialize(setting.Value);
			}
			Socket.Inst.Send(nameof(EPlayerEvent.SETTING), el);
		}

		public void OnBet(BetDto[] bets, object? Custom)
		{
			Socket.Inst.Send(nameof(EPlayerEvent.BET), new { Bets = bets, Custom });
		}

		public void OnCustom(string EventName, object? Custom)
		{
			Socket.Inst.Send(EventName, Custom ?? new { });
		}

		public void OnBetIncrease(BetDto bet, object? Custom)
		{
			Socket.Inst.Send(nameof(EPlayerEvent.BET_INCREASE), new { Bet = bet, Custom });
		}

		public void OnBetRepeat(object? Custom)
		{
			Socket.Inst.Send(nameof(EPlayerEvent.BET_REPEAT), new { Custom });
		}

		public void OnCashout(object? Custom)
		{
			Socket.Inst.Send(nameof(EPlayerEvent.BET_CASHOUT), new { Custom });
		}

		public void OnFreespin()
		{
			Socket.Inst.Send(nameof(EPlayerEvent.FREESPIN), new { });
		}

		public void OnBetInfo(string Id)
		{
			Socket.Inst.Send(nameof(EPlayerEvent.BET_INFO), new { Id });
		}

		public void OnStatReset()
		{
			Socket.Inst.Send(nameof(EPlayerEvent.STAT_RESET), new { });
		}

		public void OnRandomize(string? ClientSalt)
		{
			Socket.Inst.Send(nameof(EPlayerEvent.RANDOMIZE), new { ClientSalt });
		}

		public void OnRandomizeClientSaltOnly(string ClientSalt)
		{
			Socket.Inst.Send(nameof(EPlayerEvent.RANDOMIZE_CLIENTSALT_ONLY), new { ClientSalt });
		}

		public void OnGameHistoryInfo(string Id)
		{
			Socket.Inst.Send(nameof(EPlayerEvent.GAME_HISTORY_INFO), new { Id });
		}

		public void OnSharedData(Dictionary<string, object> Data)
		{
			Socket.Inst.Send(nameof(EPlayerEvent.SHARED_DATA), Data);
		}

		public void OnBalanceCall()
		{
			Socket.Inst.Send(nameof(EPlayerEvent.BAL), new { });
		}

		public void OnMultiJoinTable(string RoomId)
		{
			Socket.Inst.Send(nameof(EPlayerEvent.MULTI_JOIN_TABLE), new { GameTableId = RoomId });
		}

		public void OnMultiLeaveTable()
		{
			Socket.Inst.Send(nameof(EPlayerEvent.MULTI_LEAVE_TABLE), new { });
		}

		public void OnMultiTakeSeat(int SeatId, string BetAmount)
		{
			Socket.Inst.Send(nameof(EPlayerEvent.MULTI_TAKE_SEAT), new { SeatId, BetAmount });
		}

		public void OnMultiLeaveSeat()
		{
			Socket.Inst.Send(nameof(EPlayerEvent.MULTI_LEAVE_SEAT), new { });
		}

		public void OnMultiCustom(MultiCustomDto multiCustomDto)
		{
			Socket.Inst.Send(nameof(EPlayerEvent.MULTI_CUSTOM), multiCustomDto);
		}
	}
}
