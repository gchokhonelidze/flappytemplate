using System;
using DG.Tweening;
using UnityEngine;

namespace FlappyTemplate
{
    // What is left of a history strip's look once the elements own theirs: the gap between one and the next, how
    // a new one arrives, and how the strip scrolls.
    //
    // There is nothing here about what an element is made of, and that is the whole design rather than an
    // omission. A bet's colours, its size and what it says about the round are the element prefab's - the strip
    // hands it the whole HistoryDto and stays out of it - so a setting here would only be a second opinion about
    // something the prefab has already answered.
    //
    // No padding either. The elements sit in the middle of the strip, and a strip is anchored where it belongs
    // rather than inset from inside itself.
    [Serializable]
    public class UiHistoryStyle
    {
        [Header("Strip")]
        [Tooltip("Between one element and the next. How big an element is comes from the prefab, so this is the whole of the spacing.")]
        [Min(0f)]
        public float Gap = 8f;

        [Header("Arrival")]
        [Tooltip("How long a new element takes to arrive. Zero puts it there with no animation at all.")]
        [Min(0f)]
        public float AppearDuration = 0.26f;

        [Tooltip("Scale it starts at. One skips the growing part.")]
        [Min(0f)]
        public float AppearScale = 0.7f;

        public bool AppearFade = true;

        public Ease AppearEase = Ease.OutBack;

        [Tooltip("Animate on unscaled time, so the strip still moves on a game that has paused itself by setting Time.timeScale to nothing.")]
        public bool UnscaledTime = true;

        [Tooltip("How long the strip takes to slide the newest element back into view while scrolling. Zero jumps.")]
        [Min(0f)]
        public float FollowDuration = 0.25f;

        [Header("Scrolling")]
        [Tooltip("Canvas units per notch of the wheel.")]
        [Min(1f)]
        public float ScrollSensitivity = 28f;

        [Tooltip("Let a flick carry on after the finger has left. What a touch screen expects, and WebGL on a phone is a touch screen.")]
        public bool ScrollInertia = true;

        [Tooltip("How quickly a flick runs out. Lower stops sooner; 0.135 is UGUI's own.")]
        [Range(0.01f, 0.99f)]
        public float ScrollDeceleration = 0.135f;

        /// <summary>A copy, for a strip that wants its own feel without editing the shared style.</summary>
        // Every field here is a value, so a shallow copy is a whole copy. Anything added later that is not has to
        // be copied by hand.
        public UiHistoryStyle Clone() => (UiHistoryStyle)MemberwiseClone();
    }
}
