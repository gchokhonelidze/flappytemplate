using System;

namespace FlappyTemplate
{
	// In the server's order: SystemDto reads the type as a number, so a value added out of place would be read as
	// another type.
	[Serializable]
	public enum EGameType
	{
		SINGLE,
		SHARED,
		MULTI,

		// A shared round where a player may also take back a bet the round has not paid any of yet - see
		// Emitter.OnBetRollback. Everything else plays exactly as SHARED does.
		UNIVERSAL
	}

	public static class EGameTypeExtensions
	{
		/// <summary>One round for every player: SHARED, and UNIVERSAL, which is SHARED with bets that can be taken
		/// back. What to ask instead of comparing with SHARED.</summary>
		public static bool IsShared(this EGameType type) => type == EGameType.SHARED || type == EGameType.UNIVERSAL;

		/// <summary>Whether a player may take back a bet of theirs that the round has not paid yet.</summary>
		public static bool CanRollback(this EGameType type) => type == EGameType.UNIVERSAL;
	}
}
