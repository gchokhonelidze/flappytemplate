namespace FlappyTemplate
{
    /// <summary>The two things a player is given separate switches for. Every clip the package plays belongs
    /// to one of them, and the pair of settings behind them - <c>sound</c> and <c>music</c> - are the same two
    /// the web front keeps, so a player who muted the music there arrives here with it muted.</summary>
    public enum ESoundChannel
    {
        /// <summary>Everything that answers something the player did: a press, a win, a coin landing. Played
        /// as one-shots, as many at a time as there are voices for.</summary>
        Sound,

        /// <summary>The one thing playing underneath all of it. One clip at a time, looped, and faded rather
        /// than cut when it changes.</summary>
        Music,
    }
}
