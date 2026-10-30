#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace FlappyTemplate
{
	public class Emitter
	{
		public static Emitter Inst { get; private set; } = null!;

		// The calls sent through G that are still waiting for their ON_G, by callback id. See Call.
		private readonly Dictionary<string, Action<JObject>> calls = new();
		private int lastCall;

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

		// Takes back a bet of the player's that the round has not paid any of yet, by the transaction id it was placed
		// under - the key it has in MainState.BetInfos. UNIVERSAL games only (SystemState.GameType.CanRollback()).
		// The answer is ON_BET_ROLLBACK, which drops the bet from BetInfos and raises Events.OnBetRollback; a bet that
		// cannot be taken back - a freespin, partly paid, or refused by the game - comes back as ON_ERROR with
		// cannot_rollback_bet and stays in the round as it was.
		public void OnBetRollback(string TransactionId)
		{
			Socket.Inst.Send(nameof(EPlayerEvent.BET_ROLLBACK), new { TransactionId });
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

		/// <summary>Asks for the current seed pair. The answer arrives as ON_SEED, the same way a pair that
		/// has just been changed does.</summary>
		public void OnSeedInfo()
		{
			Socket.Inst.Send(nameof(EPlayerEvent.SEED_INFO), new { });
		}

		// Sent through G so that every answer can be matched to its request. The round itself still arrives as
		// ON_GAME_HISTORY_ID and raises Events.OnGameHistoryById, as it always has. But for a round the server no
		// longer keeps - aggback holds 100 per game - ON_GAME_HISTORY_ID comes with Data null and no id. ON_G
		// still comes with this call's id and {}, and that answer raises Events.OnGameHistoryByIdMissing with the
		// id that was asked for. So does a request the server refused, which comes back as an error.
		public void OnGameHistoryInfo(string Id)
		{
			Call(nameof(EPlayerEvent.GAME_HISTORY_INFO), new { Id }, answer =>
			{
				// A round has an id. {} and an error's { Message } do not.
				if (!string.IsNullOrEmpty(answer.Value<string>(nameof(GameHistoryDto.Id))))
					return;

				StateManager.Inst?.Events.OnGameHistoryByIdMissing?.Invoke(Id);
			});
		}

		/// <summary>
		/// Sends an event wrapped in G, which the server answers with ON_G carrying the same callback id: whatever
		/// the event came to as Data, {} when it came to nothing, or the error's { Message } when it was refused.
		/// The event's own answer, if it has one, is still sent as well and before the ON_G.
		/// </summary>
		public void Call(string EventName, object Data, Action<JObject> OnAnswer)
		{
			var Id = (++lastCall).ToString(CultureInfo.InvariantCulture);
			calls[Id] = OnAnswer;
			Socket.Inst.Send(nameof(EPlayerEvent.G), new { Id, E = EventName, Data });
		}

		/// <summary>Hands an ON_G answer to the call that is waiting for it. Incoming calls it.</summary>
		public void Answer(string? Id, JObject Data)
		{
			if (string.IsNullOrEmpty(Id) || !calls.Remove(Id, out var onAnswer))
				return;

			onAnswer(Data);
		}

		public void OnSharedData(Dictionary<string, object> Data)
		{
			Socket.Inst.Send(nameof(EPlayerEvent.SHARED_DATA), Data);
		}

		public void OnBalanceCall()
		{
			Socket.Inst.Send(nameof(EPlayerEvent.BAL), new { });
		}

		/// <summary>
		/// Asks the server to run the init over again. It answers with the same ON_GROUP snapshot a
		/// fresh connection gets - system, balance, state, bet info, stats, freespins, history and
		/// settings - so it is safe to call whenever the client suspects it missed the first one.
		/// </summary>
		public void OnReload()
		{
			Socket.Inst.Send(nameof(EPlayerEvent.RELOAD), new { });
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
