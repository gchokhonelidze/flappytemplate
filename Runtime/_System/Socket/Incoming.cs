using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace FlappyTemplate
{
	[RequireComponent(typeof(StateManager))]
	public class Incoming : MonoBehaviour
	{
		StateManager stateManager;

		void Awake()
		{
			stateManager = GetComponent<StateManager>();
		}

		public void OnData(string msg)
		{
			// Debug.Log($"Received data: {msg}");
			try
			{
				var payload = JObject.Parse(msg);
				var eventName = payload.Value<string>(nameof(PayloadDto.EventName));
				var data = payload[nameof(PayloadDto.Data)];
				if (string.IsNullOrEmpty(eventName) || data is null)
					return;
				// Debug.Log(msg);
				var dataJson = data.ToString(Formatting.None);
				if (eventName == nameof(EGameEvent.ON_GROUP))
				{
					var values = data[nameof(PayloadGroupDto.Values)] as JArray;
					if (values is null)
						return;
					foreach (var element in values.OfType<JObject>())
					{
						var groupEventName = element.Value<string>(nameof(PayloadGroupElementDto.E));
						var groupData = element[nameof(PayloadGroupElementDto.O)];
						if (string.IsNullOrEmpty(groupEventName) || groupData is null)
							continue;
						ProcessDataEvent(groupEventName, groupData.ToString(Formatting.None));
					}
				}
				else
				{
					ProcessDataEvent(eventName, dataJson);
				}
			}
			catch (System.Exception ex)
			{
				Debug.LogError($"Failed to deserialize payload: {ex.Message}");
			}
		}

		private void ProcessDataEvent(string eventName, string dataJson)
		{
			try
			{
				switch (eventName)
				{
					case nameof(EGameEvent.ON_G):
					{
						// TODO: Handle ON_G payload.
						break;
					}
					case nameof(EGameEvent.ON_ERROR):
					{
						stateManager.MainState.Error ??= new ErrorDto();
						stateManager.MainState.Error.ApplyPatch(dataJson);
						stateManager.Events.OnError?.Invoke(stateManager.MainState.Error);
						break;
					}
					case nameof(EGameEvent.ON_BALANCE):
					{
						stateManager.MainState.BalanceState ??= new BalanceDto();
						stateManager.MainState.BalanceState.ApplyPatch(dataJson);
						stateManager.Events.OnBalance?.Invoke(stateManager.MainState.BalanceState);
						break;
					}
					case nameof(EGameEvent.ON_BALANCE_WIN):
					{
						// TODO: Handle ON_BALANCE_WIN payload. ShowWinToast
						stateManager.MainState.BalanceState ??= new BalanceDto();
						stateManager.MainState.BalanceState.ApplyPatch(dataJson);
						stateManager.Events.OnBalance?.Invoke(stateManager.MainState.BalanceState);
						break;
					}
					case nameof(EGameEvent.ON_SYSTEM):
					{
						stateManager.MainState.SystemState ??= new SystemDto();
						stateManager.MainState.SystemState.ApplyPatch(dataJson);
						stateManager.Events.OnSystem?.Invoke(stateManager.MainState.SystemState);
						break;
					}
					case nameof(EGameEvent.ON_STATE):
					{
						// Debug.Log($"Received state data: {dataJson}");
						var partialState = Utils.Deserialize<Dictionary<string, JToken>>(dataJson);
						stateManager.MainState.GameState = stateManager.MainState.GameState.MergeFull(partialState);
						stateManager.MainState._GameState = stateManager.MainState.GameState.ToGeneric();
						stateManager.Events.OnState?.Invoke(stateManager.MainState.GameState);
						break;
					}
					case nameof(EGameEvent.ON_IND_STATE):
					{
						var partialState = Utils.Deserialize<Dictionary<string, JToken>>(dataJson);
						stateManager.MainState.IndState = stateManager.MainState.IndState.MergeFull(partialState);
						stateManager.Events.OnIndState?.Invoke(stateManager.MainState.IndState);
						break;
					}
					case nameof(EGameEvent.ON_BET_INFO):
					{
						var patchArray = Utils.Deserialize<Dictionary<string, BetInfoDto>>(dataJson);
						if (patchArray is null || patchArray.Count == 0)
							return;

						foreach (var (id, betInfoDto) in patchArray!)
						{
							StateManager.Inst.MainState.BetInfos[id] = betInfoDto;
							stateManager.Events.OnBetInfo?.Invoke(betInfoDto);
						}
						break;
					}
					case nameof(EGameEvent.ON_BET_INFO_PAYOUT):
					{
						var patchArray = Utils.Deserialize<TransactionChunks>(dataJson);
						foreach (var (id, chunk) in patchArray.Chunks)
						{
							if (!StateManager.Inst.MainState.BetInfos.TryGetValue(id, out var betInfo))
								continue;
							var bet = betInfo.Bets.FirstOrDefault(el => el.BetId == chunk.BetId);
							if (bet is null)
								continue;
							bet.Payout = chunk.Payout;
							bet.Win = true;
							stateManager.Events.OnBetInfoPayout?.Invoke(betInfo);
						}
						break;
					}
					case nameof(EGameEvent.ON_BET_INFO_ID):
					{
						var betInfoById = Utils.Deserialize<TransactionPublic>(dataJson);
						stateManager.MainState.BetInfoById = betInfoById;
						stateManager.Events.OnBetInfoById?.Invoke(betInfoById);
						break;
					}
					case nameof(EGameEvent.ON_BET_INFO_INCREASE):
					{
						var betInfoIncreaseDto = Utils.Deserialize<BetInfoIncreaseDto>(dataJson);
						if (betInfoIncreaseDto is null)
							return;
						var id = betInfoIncreaseDto.Id.Split('-')[0];
						if (!StateManager.Inst.MainState.BetInfos.TryGetValue(id, out var betInfo))
							return;
						var bet = betInfo.Bets.FirstOrDefault(el => el.BetId == betInfoIncreaseDto.BetId);
						if (bet is null)
							return;
						bet.Increases.Add(betInfoIncreaseDto.Amount);
						stateManager.Events.OnBetInfoIncrease?.Invoke(betInfo);
						break;
					}
					case nameof(EGameEvent.ON_SEED):
					{
						var seedDto = Utils.Deserialize<SeedDto>(dataJson);
						if (seedDto is null)
							return;
						stateManager.MainState.Seeds = seedDto;
						stateManager.Events.OnSeed?.Invoke(seedDto);
						break;
					}
					case nameof(EGameEvent.ON_STAT):
					{
						var statsDto = Utils.Deserialize<StatsDto>(dataJson);
						if (statsDto is null)
							return;
						stateManager.MainState.Statistics = statsDto;
						stateManager.Events.OnStatistics?.Invoke(statsDto);
						break;
					}
					case nameof(EGameEvent.ON_FREESPIN):
					{
						stateManager.MainState.FreespinInfo ??= new FreespinDto();
						stateManager.MainState.FreespinInfo.ApplyPatch(dataJson);
						if (stateManager.MainState.FreespinInfo.Left == 0)
						{
							stateManager.MainState.FreespinInfo = null;
						}
						else
						{
							stateManager.Events.OnFreespin?.Invoke(stateManager.MainState.FreespinInfo);
						}
						break;
					}
					case nameof(EGameEvent.ON_HISTORY):
					{
						var arr = Utils.Deserialize<HistoryDto[]>(dataJson);
						if (arr is null || arr.Length == 0)
							return;
						foreach (var historyDto in arr)
						{
							historyDto._Outcome = historyDto.Outcome?.ToGeneric();
							stateManager.MainState.History.Add(historyDto);
							stateManager.Events.OnHistory?.Invoke(historyDto);
							if (stateManager.MainState.History.Count > 15)
							{
								stateManager.MainState.History.RemoveAt(0);
							}
						}

						break;
					}
					case nameof(EGameEvent.ON_GAME_HISTORY):
					{
						var arr = Utils.Deserialize<GameHistoryDto[]>(dataJson);
						if (arr is null || arr.Length == 0)
							return;
						foreach (var gameHistoryDto in arr)
						{
							gameHistoryDto._Outcome = gameHistoryDto.Outcome?.ToGeneric();
							stateManager.MainState.GameHistory.Add(gameHistoryDto);
							stateManager.Events.OnGameHistory?.Invoke(gameHistoryDto);
							if (stateManager.MainState.History.Count > 99)
							{
								stateManager.MainState.History.RemoveAt(0);
							}
						}
						break;
					}
					case nameof(EGameEvent.ON_GAME_HISTORY_ID):
					{
						var dto = Utils.Deserialize<GameHistoryByIdDto>(dataJson);
						foreach (var (key, value) in dto.Transactions)
						{
							dto._Transactions[key] = value;
						}
						dto._Outcome = dto.Outcome?.ToGeneric();
						stateManager.MainState.GameHistoryById = dto;
						stateManager.Events.OnGameHistoryById?.Invoke(dto);
						break;
					}
					case nameof(EGameEvent.ON_SETTING):
					{
						var dto = Utils.Deserialize<Dictionary<string, JToken>>(dataJson);
						stateManager.MainState.Settings = stateManager.MainState.Settings.MergeFull(dto);
						stateManager.Events.OnSettings?.Invoke(stateManager.MainState.Settings);
						break;
					}
					// case nameof(EGameEvent.ON_CHAT):
					// {
					// 	// TODO: Handle ON_CHAT payload.
					// 	break;
					// }
					case nameof(EGameEvent.ON_SHARED_DATA):
					{
						var dto = Utils.Deserialize<Dictionary<string, JToken>>(dataJson) ?? new Dictionary<string, JToken>();
						stateManager.MainState.SharedData = stateManager.MainState.SharedData.MergeFull(dto);
						stateManager.Events.OnSharedData?.Invoke(stateManager.MainState.SharedData);
						break;
					}
					case nameof(EGameEvent.ON_SHARED_DATA_ALL):
					{
						var dto = Utils.Deserialize<Dictionary<string, JToken>>(dataJson) ?? new Dictionary<string, JToken>();
						stateManager.MainState.SharedData = dto;
						stateManager.Events.OnSharedData?.Invoke(stateManager.MainState.SharedData);
						break;
					}
					// case nameof(EGameEvent.ON_BATTLE_GAME_LIST):
					// {
					// 	// TODO: Handle ON_BATTLE_GAME_LIST payload.
					// 	break;
					// }
					// case nameof(EGameEvent.ON_BATTLE_GAME_FSIDS):
					// {
					// 	// TODO: Handle ON_BATTLE_GAME_FSIDS payload.
					// 	break;
					// }
					// case nameof(EGameEvent.ON_BATTLE_CREATE):
					// {
					// 	// TODO: Handle ON_BATTLE_CREATE payload.
					// 	break;
					// }
					// case nameof(EGameEvent.ON_BATTLE_LIST):
					// {
					// 	// TODO: Handle ON_BATTLE_LIST payload.
					// 	break;
					// }
					// case nameof(EGameEvent.ON_BATTLE_SCORES):
					// {
					// 	// TODO: Handle ON_BATTLE_SCORES payload.
					// 	break;
					// }
					// case nameof(EGameEvent.ON_BATTLE):
					// {
					// 	// TODO: Handle ON_BATTLE payload.
					// 	break;
					// }
					// case nameof(EGameEvent.ON_BATTLE_LIST_UPDATE):
					// {
					// 	// TODO: Handle ON_BATTLE_LIST_UPDATE payload.
					// 	break;
					// }
					// case nameof(EGameEvent.ON_TICK):
					// {
					// 	// TODO: Handle ON_TICK payload. Battle tick
					// 	break;
					// }
					// case nameof(EGameEvent.ON_TOAST):
					// {
					// 	// TODO: Handle ON_TOAST payload. //battle toast
					// 	break;
					// }
					case nameof(EGameEvent.ON_MULTI_INIT):
					{
						Debug.Log($"Received multi init data: {dataJson}");
						var dto = Utils.Deserialize<MultiInitDto>(dataJson);
						if (dto is null)
						{
							Debug.LogError("Failed to deserialize MultiInitDto");
							break;
						}
						var initPayload = JObject.Parse(dataJson);
						stateManager.MainState.BalanceState = dto.BalanceDto;
						stateManager.MainState.SystemState = dto.SystemDto;
						stateManager.MultiState.MultiTurnIndex = dto.TurnIndex;
						stateManager.MultiState.MultiChipBalance = dto.ChipBalance;
						stateManager.MultiState.MultiLocation = dto.Location;
						stateManager.MultiState.MultiRooms = dto.Rooms.ToArray();
						Debug.Log($"ON_MULTI_INIT rooms count: {stateManager.MultiState.MultiRooms?.Length ?? 0}");
						stateManager.MultiState.MultiMySeatIndex = dto.SeatIndex;
						stateManager.MultiState.MultiTurnId = dto.TurnId;
						if (dto.MyRoom != null)
						{
							stateManager.MultiState.SetMyRoom(dto.MyRoom);
						}
						//invoke events:
						stateManager.Events.OnBalance?.Invoke(stateManager.MainState.BalanceState);
						stateManager.Events.OnSystem?.Invoke(stateManager.MainState.SystemState);

						stateManager.Events.OnMultiRoom?.Invoke();
						stateManager.Events.OnMultiLocation?.Invoke();
						stateManager.Events.OnMultiChipBalance?.Invoke(dto.ChipBalance);

						break;
					}
					case nameof(EGameEvent.ON_MULTI_JOIN_TABLE):
					{
						var dto = Utils.Deserialize<MultiJoinTableDto>(dataJson);
						stateManager.MultiState.MultiLocation = dto.Location;
						stateManager.MultiState.MultiMySeatIndex = dto.SeatIndex;
						if (dto.MyRoom != null)
						{
							stateManager.MultiState.SetMyRoom(dto.MyRoom);
						}
						stateManager.Events.OnMultiLocation?.Invoke();
						break;
					}
					case nameof(EGameEvent.ON_MULTI_LEAVE_TABLE):
					{
						var dto = Utils.Deserialize<MultiLeaveTableDto>(dataJson);
						if (dto.IPlayerId != stateManager.MainState.SystemState?.Me)
						{
							var seatsTaken = new GenericDictionary<int, PlayerDto>();
							seatsTaken.AddRange(stateManager.MultiState.MultiSeatsTaken);
							// stateManager.MultiState.MultiSeatsTaken
							if (dto.LeftSeatId is not null)
							{
								seatsTaken.Remove(dto.LeftSeatId.Value);
							}
							stateManager.MultiState.MultiSeatsTaken = seatsTaken;
						}
						else
						{
							stateManager.MultiState.MultiPrevRoomId = dto.TableId;
							stateManager.MultiState.MultiLocation = "LOBBY";
							stateManager.MultiState.MultiMyRoom = null;
							stateManager.MultiState.MultiMySeatIndex = null;
							stateManager.MainState.GameState = null;
							stateManager.MainState._GameState.Clear();
							stateManager.MainState.IndState = null;
							stateManager.MultiState.MultiDealer = null;
							stateManager.MultiState.MultiTurn = null;
							stateManager.MultiState.MultiSeatsTaken = new();
							stateManager.MultiState.MultiSeatsInRound = new();
							stateManager.MultiState.MultiRoundId = null;
							stateManager.MultiState.MultiRunning = false;
							stateManager.Events.OnMultiLocation?.Invoke();
						}

						stateManager.Events.OnMultiSeatsChanged?.Invoke();
						break;
					}
					case nameof(EGameEvent.ON_MULTI_TAKE_SEAT):
					{
						var dto = Utils.Deserialize<MultiTakeSeatDto>(dataJson);
						var iPlayerId = stateManager.MainState.SystemState?.Me;
						int? multiMySeatIndex = null;
						foreach (var (seatIndex, player) in dto.SeatsTaken)
						{
							if (player.IPlayerId == iPlayerId)
							{
								multiMySeatIndex = seatIndex;
								break;
							}
						}
						stateManager.MultiState.MultiSeatsTaken = dto.SeatsTaken;
						stateManager.MultiState.MultiMySeatIndex = multiMySeatIndex;
						stateManager.Events.OnMultiSeatsChanged?.Invoke();
						break;
					}
					case nameof(EGameEvent.ON_MULTI_LEAVE_SEAT):
					{
						var dto = Utils.Deserialize<MultiLeaveSeatDto>(dataJson);
						stateManager.MultiState.MultiSeatsTaken.Remove(dto.LeftSeatId);
						if (dto.IPlayerId == stateManager.MainState.SystemState?.Me)
						{
							stateManager.MultiState.MultiMySeatIndex = null;
						}
						stateManager.Events.OnMultiSeatsChanged?.Invoke();
						break;
					}
					case nameof(EGameEvent.ON_MULTI_TURN):
					{
						var dto = Utils.Deserialize<MultiTurnUpdateDto>(dataJson);
						stateManager.MainState.GameState = stateManager.MainState.GameState.MergeFull(dto.SPubPartial);
						stateManager.MainState._GameState = stateManager.MainState.GameState.ToGeneric();
						stateManager.MultiState.MultiTurnIndex = dto.TurnIndex;
						stateManager.MultiState.MultiTurnId = dto.TurnId;
						stateManager.MultiState.MultiTurnPlayerIds = dto.TurnPlayerIds.ToHashSet();
						stateManager.MultiState.MultiTurnPlayers = dto
							.TurnPlayerIds.Select(id =>
							{
								var seatIndex = stateManager.MultiState.MultiSeatsInRound.Keys.FirstOrDefault(seatIndex =>
									stateManager.MultiState.MultiSeatsInRound[seatIndex].Id == id
								);
								return new MultiTurnPlayerDto
								{
									SeatIndex = seatIndex,
									Player = stateManager.MultiState.MultiSeatsInRound.TryGetValue(seatIndex, out var player) ? player : null,
								};
							})
							.ToList();
						if (dto.NextDealer != null)
							stateManager.MultiState.MultiDealer = dto.NextDealer;
						if (dto.NextTurn != null)
							stateManager.MultiState.MultiTurn = dto.NextTurn;
						if (dto.RoundEnd)
						{
							stateManager.MultiState.MultiRunning = false;
							stateManager.MultiState.MultiLeftInTurn = null;
							stateManager.MultiState.MultiSeatsInRound = new();
							stateManager.Events.OnMultiSeatsChanged?.Invoke();
						}
						else
						{
							stateManager.MultiState.MultiRunning = true;
						}
						stateManager.Events.OnMultiTurn?.Invoke(dto);
						break;
					}
					case nameof(EGameEvent.ON_MULTI_LEFT):
					{
						var dto = Utils.Deserialize<MultiLeftDto>(dataJson);
						stateManager.MultiState.MultiLeftInTurn = dto;
						stateManager.Events.OnMultiLeft?.Invoke(dto);
						break;
					}
					case nameof(EGameEvent.ON_MULTI_START_ROUND):
					{
						var dto = Utils.Deserialize<MultiStartRoundDto>(dataJson);
						stateManager.MultiState.MultiTurnIndex = dto.TurnIndex;
						stateManager.MultiState.MultiSeatsTaken = dto.SeatsTaken;
						stateManager.MultiState.MultiSeatsInRound = dto.SeatsInRound;
						stateManager.MultiState.MultiRunning = true;
						stateManager.Events.OnMultiSeatsChanged?.Invoke();
						break;
					}
					case nameof(EGameEvent.ON_CHIP_BALANCE):
					{
						var dto = Utils.Deserialize<ChipBalanceDto>(dataJson);
						stateManager.MultiState.MultiChipBalance = dto;
						stateManager.Events.OnMultiChipBalance?.Invoke(dto);
						break;
					}
					case nameof(EGameEvent.ON_CHIP_BALANCE_INGAME):
					{
						var dto = Utils.Deserialize<ChipBalanceInGameDto>(dataJson);
						if (stateManager.MultiState.MultiChipBalance is not null)
						{
							stateManager.MultiState.MultiChipBalance = new ChipBalanceDto
							{
								Idle = stateManager.MultiState.MultiChipBalance.Idle,
								InGame = dto.Value,
								Total = stateManager.MultiState.MultiChipBalance.Idle + dto.Value,
							};
							stateManager.Events.OnMultiChipBalance?.Invoke(stateManager.MultiState.MultiChipBalance);
						}

						break;
					}
					case nameof(EGameEvent.ON_CHIP_BALANCE_IDLE):
					{
						var dto = Utils.Deserialize<ChipBalanceIdleDto>(dataJson);
						if (stateManager.MultiState.MultiChipBalance is not null)
						{
							stateManager.MultiState.MultiChipBalance = new ChipBalanceDto
							{
								Idle = dto.Value,
								InGame = stateManager.MultiState.MultiChipBalance.InGame,
								Total = stateManager.MultiState.MultiChipBalance.InGame + dto.Value,
							};
							stateManager.Events.OnMultiChipBalance?.Invoke(stateManager.MultiState.MultiChipBalance);
						}
						break;
					}
					default:
						Debug.Log($"Unhandled event: {eventName}");
						break;
				}
			}
			catch (System.Exception ex)
			{
				Debug.LogError($"Failed to process event {eventName}: {ex.Message}. Json: {dataJson}");
			}
		}
	}
}
