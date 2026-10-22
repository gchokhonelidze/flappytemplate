#nullable enable
using System;

namespace FlappyTemplate
{
    /// <summary>Which sign a toast carries, and which palette it is drawn in.</summary>
    public enum EToastType
    {
        /// <summary>Charcoal, white ink, an i in a disc.</summary>
        Default,

        /// <summary>A dark red pill, red ink, a bang in a disc.</summary>
        Error,

        /// <summary>A dark olive pill, yellow ink, a bang on a triangle.</summary>
        Warning,

        /// <summary>A dark green pill, green ink, a tick in a disc.</summary>
        Success
    }

    /// <summary>One message for a toast: what it says, which kind it is, and how long it stays.</summary>
    public class ToastOptions
    {
        /// <summary>A bold line above the message. Empty leaves the message on its own, which is the usual case.</summary>
        public string? Title;

        public EToastType Type = EToastType.Default;

        public string Message = string.Empty;

        /// <summary>How long the toast stays once it has arrived, when AutoHide is on.</summary>
        public TimeSpan HideAfter = TimeSpan.FromSeconds(3);

        /// <summary>How long the slide in, and the slide out, take.</summary>
        public TimeSpan AnimationDuration = TimeSpan.FromSeconds(0.5);

        /// <summary>A cross on the right of the pill. Not in the design, so off by default; a toast that does not
        /// hide itself wants one.</summary>
        public bool ShowCloseButton = false;

        public bool AutoHide = true;
    }
}
