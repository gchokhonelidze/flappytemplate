#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FlappyTemplate
{
	[Serializable]
	public class MultiState
	{
		//MULTI:
		[SerializeField]
		public string? MultiLocation;

		[SerializeField]
		public ChipBalanceDto? MultiChipBalance;

		[SerializeField]
		public MultiRoomDto[]? MultiRooms;

		[SerializeField]
		public MultiRoomDto? MultiMyRoom;

		[SerializeField]
		public long? MultiPrevRoomId;

		[SerializeField]
		public int? MultiMySeatIndex;

		[SerializeField]
		public int? MultiDealer;

		[SerializeField]
		public string? MultiTurnId;

		[SerializeField]
		public MultiLeftDto? MultiLeftInTurn;

		[SerializeField]
		public int? MultiTurn;

		[SerializeField]
		public int MultiTurnIndex;

		[SerializeField]
		public HashSet<long> MultiTurnPlayerIds = new();

		[SerializeField]
		public List<MultiTurnPlayerDto> MultiTurnPlayers = new();

		[SerializeField]
		public bool MultiRunning;

		[SerializeField]
		public string? MultiRoundId;

		[SerializeField]
		public GenericDictionary<int, PlayerDto> MultiSeatsTaken = new();

		[SerializeField]
		public GenericDictionary<int, PlayerDto> MultiSeatsInRound = new();

		// [SerializeField]
		// public GenericDictionary<int, PlayerDto> _MultiSeatsTaken = new();

		// [SerializeField]
		// public GenericDictionary<int, PlayerDto> _MultiSeatsInRound = new();

		public void SetMyRoom(MultiMyRoomDto myRoom)
		{
			MultiMyRoom = myRoom.MultiRoomInfo;
			StateManager.Inst.MainState.GameState = myRoom.PubState;
			StateManager.Inst.MainState.IndState = myRoom.IndState;
			MultiDealer = myRoom.Dealer;
			MultiTurn = myRoom.Turn;
			MultiSeatsTaken = myRoom.SeatsTaken ?? new();
			MultiSeatsInRound = myRoom.SeatsInRound ?? new();
			MultiRoundId = myRoom.RoundId;
			MultiRunning = myRoom.Running;
			MultiTurnPlayerIds = myRoom.SeatsInRound?.Count == 0 ? new() : myRoom.TurnPlayerIds.ToHashSet() ?? new();
			MultiTurnPlayers = new List<MultiTurnPlayerDto>();
			if (myRoom.SeatsInRound is not null)
			{
				foreach (var (seatIndex, player) in myRoom.SeatsInRound)
				{
					if (MultiTurnPlayerIds.Contains(player.Id))
					{
						MultiTurnPlayers.Add(new MultiTurnPlayerDto { SeatIndex = seatIndex, Player = player });
					}
				}
			}
		}
	}
}
