namespace FlappyTemplate
{
    /// <summary>The mouse cursors <see cref="UiCursor"/> can show - the browser's own, by their CSS names.</summary>
    public enum ECursor
    {
        /// <summary>The arrow. Over anything that does nothing when clicked.</summary>
        Default,

        /// <summary>The hand. Over anything that does something when clicked.</summary>
        Pointer,

        /// <summary>The I-beam. Over a text field.</summary>
        Text,

        /// <summary>An open hand, for something that can be picked up and dragged.</summary>
        Grab,

        /// <summary>A closed hand, for something being dragged.</summary>
        Grabbing,

        /// <summary>A circle with a bar through it, for something that would be clickable but is not now.</summary>
        NotAllowed,
    }
}
