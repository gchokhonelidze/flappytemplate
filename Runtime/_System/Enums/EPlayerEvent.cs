using UnityEngine;

namespace FlappyTemplate
{
	public enum EPlayerEvent
	{
		G,
		BET,
		BET_INCREASE,
		BET_REPEAT,
		BET_CASHOUT,
		FREESPIN,
		BET_INFO,
		GAME_HISTORY_INFO,
		RANDOMIZE,
		RANDOMIZE_CLIENTSALT_ONLY,
		SEED_INFO,
		STAT_RESET,
		SETTING,
		ROOM_ENTER,
		ROOM_LEAVE,
		ROOM_MESSAGE,
		SHARED_DATA,
		BATTLE_GAME_LIST,
		BATTLE_GAME_FSIDS,
		BATTLE_CREATE,
		BATTLE_LIST,
		BATTLE_SCORES,
		BATTLE_ENTER, //open battle window
		BATTLE_LEAVE, //stop battle score listening
		BATTLE_JOIN,
		BAL,
		MULTI_JOIN_TABLE,
		MULTI_LEAVE_TABLE,
		MULTI_TAKE_SEAT,
		MULTI_LEAVE_SEAT,
		MULTI_CUSTOM,

		// Asks the server to run the init again. The answer is the same ON_GROUP snapshot the
		// connection opens with, so it can be asked for at any point without side effects.
		RELOAD
	}
}
