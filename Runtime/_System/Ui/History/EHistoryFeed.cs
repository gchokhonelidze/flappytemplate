namespace FlappyTemplate
{
    /// <summary>Which history the strip shows: the player's own bets, or the rounds of a shared game.</summary>
    // The two are different events, different payloads and different dialogs. A single-player game deals a bet
    // to one player at a time, and a chip is one of those bets; a shared game rolls once for the room, and a
    // chip is the round everybody bet on. So a shared strip is fed by ON_GAME_HISTORY rather than ON_HISTORY,
    // and a press opens the game history window rather than the bet info one.
    public enum EHistoryFeed
    {
        /// <summary>Whichever the server says the game is: the rounds on a shared game, the player's own bets
        /// on any other. What a strip should be left on unless a game shows both at once.</summary>
        Auto,

        /// <summary>The player's own bets - ON_HISTORY, and the bet info window on a press.</summary>
        Player,

        /// <summary>The rounds of a shared game - ON_GAME_HISTORY, and the game history window on a press.</summary>
        Shared,
    }
}
