using System;

namespace FlappyTemplate
{
	[Serializable]
	public enum EGameEvent
	{
		ON_G,
		ON_GROUP,
		ON_ERROR,
		ON_BALANCE,
		ON_BALANCE_WIN,
		ON_STATE,
		ON_IND_STATE,

		// ON_PLAYER,
		ON_BET_INFO,
		ON_BET_INFO_PAYOUT,
		ON_BET_INFO_ID,
		ON_BET_INFO_INCREASE,
		ON_SYSTEM,
		ON_SEED,
		ON_STAT,
		ON_FREESPIN,
		ON_HISTORY,
		ON_GAME_HISTORY,
		ON_GAME_HISTORY_ID,
		ON_SETTING,
		ON_CHAT,
		ON_SHARED_DATA,
		ON_SHARED_DATA_ALL,
		ON_BATTLE_GAME_LIST,
		ON_BATTLE_GAME_FSIDS,
		ON_BATTLE_CREATE,
		ON_BATTLE_LIST,
		ON_BATTLE_SCORES,
		ON_BATTLE, //certain battle
		ON_BATTLE_LIST_UPDATE, //info on all battles
		ON_TICK, //server time
		ON_TOAST,
		ON_MULTI_INIT, //multi init data

		// ON_MULTI_ROOMS, //multi rooms for current game
		ON_MULTI_JOIN_TABLE, //player joined multi room
		ON_MULTI_LEAVE_TABLE, //player left multi room
		ON_MULTI_TAKE_SEAT, //seats for specific multi room
		ON_MULTI_LEAVE_SEAT, //player left seat in multi room
		ON_MULTI_TURN, //multi turn result
		ON_MULTI_LEFT, //multi left countdown untill next turn
		ON_MULTI_START_ROUND, //multi round started
		ON_CHIP_BALANCE,
		ON_CHIP_BALANCE_INGAME,
		ON_CHIP_BALANCE_IDLE,
	}
}
