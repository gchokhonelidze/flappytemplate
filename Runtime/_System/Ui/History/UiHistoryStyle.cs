using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace FlappyTemplate
{
    // What a history strip looks like: how big one element is, how far apart they sit, what the built-in chip
    // is made of, and one entry per case the game can tell apart.
    //
    // Split the way the rest of this package splits: shape and spacing here, because they are one look for the
    // whole strip, and colours in the scenarios, because they are what says which bet went which way. A game
    // handing the same style to two strips gets two strips that match; a game that wants one of them in its own
    // palette calls Clone first.
    [Serializable]
    public class UiHistoryStyle
    {
        [Header("Strip")]
        [Tooltip("One element, in canvas units. Zero on an axis leaves that side to the element itself - to a prefab's own Layout Element, or to the width its text asks for. Clamp needs this set along the flow: it is what the room is divided by.")]
        public Vector2 ElementSize = new Vector2(96f, 56f);

        [Tooltip("Between one element and the next.")]
        [Min(0f)]
        public float Gap = 8f;

        // Four floats rather than a RectOffset, for the reason UiWindowStyle gives at the same place: a
        // RectOffset is a handle onto a native object, so building one in a field initialiser throws before the
        // component exists, and it is a class, so a copied style would go on sharing it.
        [Tooltip("Inset of the elements from the left of the strip.")]
        public float PaddingLeft = 0f;

        public float PaddingTop = 0f;

        public float PaddingRight = 0f;

        public float PaddingBottom = 0f;

        [Header("Element")]
        [Tooltip("Corners of the built-in chip. Ignored when a prefab is given - a prefab is its own shape.")]
        [Min(0f)]
        public float CornerRadius = 8f;

        [Tooltip("Width of the fade that smooths the corner arcs. About a pixel is right.")]
        [Min(0f)]
        public float EdgeSoftness = 1.25f;

        [Tooltip("Border of the chip, for every scenario that does not ask for its own.")]
        [Min(0f)]
        public float BorderSize = 1.5f;

        public TMP_FontAsset Font;

        [Min(1f)]
        public float TextSize = 26f;

        public FontStyles TextStyle = FontStyles.Bold;

        public TextAlignmentOptions TextAlignment = TextAlignmentOptions.Center;

        [Tooltip("Inset of the text from the edges of the chip. Enough that a long value wraps rather than touching the border.")]
        [Min(0f)]
        public float TextInset = 6f;

        [Tooltip("Shrink the text on an element it does not fit rather than letting it wrap or spill. Off keeps every element's text the same size, which reads better on a strip of short values.")]
        public bool ShrinkText = true;

        [Header("Accent")]
        [Tooltip("How far the accent bar is held off the sides of the element.")]
        [Min(0f)]
        public float AccentInset = 10f;

        [Tooltip("How far up from the bottom edge it sits. Negative hangs it below the element.")]
        public float AccentOffset = -2f;

        [Header("Scenarios")]
        [Tooltip("The look of an element whose scenario is not in the list below - and of every element, on a game that never classifies anything.")]
        public UiHistoryScenario Default = new UiHistoryScenario
        {
            Name = "default",
            TextColor = Color.white,
        };

        [Tooltip("One entry per case the game can tell apart. The names are matched against what Classify, Scenario Key or the amounts say.")]
        public List<UiHistoryScenario> Scenarios = new List<UiHistoryScenario>
        {
            new UiHistoryScenario
            {
                Name = "win",
                TextColor = new Color(0.388f, 1f, 0.58f),
            },
            new UiHistoryScenario
            {
                Name = "push",
                TextColor = new Color(0.8f, 0.8f, 0.86f),
            },
            new UiHistoryScenario
            {
                Name = "loss",
                TextColor = new Color(1f, 0.388f, 0.388f),
            },
        };

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

        /// <summary>The scenario of that name, or <see cref="Default"/> when there is none.</summary>
        // Never returns null, and that is worth relying on: an element painted from a scenario that turned out
        // not to exist would be an element with no colours at all, which looks like a broken atlas rather than
        // like a missing entry in a list.
        public UiHistoryScenario Find(string name)
        {
            if (string.IsNullOrEmpty(name) || Scenarios == null)
                return Default ?? new UiHistoryScenario();

            for (int i = 0; i < Scenarios.Count; i++)
            {
                var scenario = Scenarios[i];
                if (scenario != null && string.Equals(scenario.Name, name, StringComparison.OrdinalIgnoreCase))
                    return scenario;
            }

            return Default ?? new UiHistoryScenario();
        }

        /// <summary>A copy, for a strip that wants its own colours without editing the shared style.</summary>
        // The scenarios are copied rather than shared, unlike everything else here: they are the part a game
        // reaches for when it wants one strip to differ, and a shallow copy would have it editing both.
        public UiHistoryStyle Clone()
        {
            var copy = (UiHistoryStyle)MemberwiseClone();
            copy.Default = Default?.Clone();

            if (Scenarios == null)
                return copy;

            copy.Scenarios = new List<UiHistoryScenario>(Scenarios.Count);
            for (int i = 0; i < Scenarios.Count; i++)
                copy.Scenarios.Add(Scenarios[i]?.Clone());

            return copy;
        }
    }
}
