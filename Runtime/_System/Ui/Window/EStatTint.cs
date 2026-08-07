namespace FlappyTemplate
{
    /// <summary>What decides the colour a statistics value is printed in.</summary>
    // Where the line between the two colours falls is the whole of the difference between Signed and
    // Strict, and it is not the same line for every number. Nothing wagered yet is not a loss, so a profit
    // of zero is printed as the good colour; luck of zero is the worst there is, so it is printed as the
    // bad one.
    public enum EStatTint
    {
        /// <summary>The style's plain value colour, whatever the number is.</summary>
        Plain,

        /// <summary>Zero and above in the positive colour, below it in the negative one.</summary>
        Signed,

        /// <summary>Above zero in the positive colour, zero and below in the negative one.</summary>
        Strict,

        /// <summary>Bets, wins and losses each in their own colour on one line. Only means anything on
        /// <see cref="EStatField.Counts"/>.</summary>
        Counts,
    }
}
