namespace FlappyTemplate
{
    /// <summary>One readable value out of a <see cref="StatisticsDto"/>.</summary>
    // The money fields arrive from the server as strings, because a decimal that has been through a float
    // is no longer the number that was wagered. They are kept as strings all the way to the label and only
    // parsed to decide whether a value is above or below zero - see EStatTint.
    public enum EStatField
    {
        /// <summary>Wager - everything staked.</summary>
        Wager,

        /// <summary>WagerWon - the part of it that came back.</summary>
        WagerWon,

        /// <summary>WagerLost - the part of it that did not.</summary>
        WagerLost,

        /// <summary>GrossWin - won before the stake is taken off.</summary>
        GrossWin,

        /// <summary>NetProfit - won after it is.</summary>
        NetProfit,

        /// <summary>Payouts.</summary>
        Payouts,

        /// <summary>Luck.</summary>
        Luck,

        /// <summary>BetCount.</summary>
        BetCount,

        /// <summary>WinCount.</summary>
        WinCount,

        /// <summary>LoseCount.</summary>
        LoseCount,

        /// <summary>All three counts on one line, each in its own colour: bets / wins / losses.</summary>
        Counts,
    }
}
