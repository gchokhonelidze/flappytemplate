using System;
using UnityEngine;
using UnityEngine.Events;

namespace FlappyTemplate
{
    /// <summary>One slot in the navbar: what it does, what it is called, and whether it is in the row at
    /// all.</summary>
    // A class rather than a struct, so the inspector's list can be reordered without the events being
    // copied around, and so a game holding a reference to one keeps holding the same one.
    [Serializable]
    public class NavbarButton
    {
        [Tooltip("What the button does, and which glyph it is drawn with.")]
        public ENavbarButton Kind = ENavbarButton.Custom;

        [Tooltip("Shown under the icon when the style is drawing labels, and translated on the way - see Translations. A key, or the en_US wording of one, or a word of your own that is left alone.")]
        public string Label = "";

        [Tooltip("Off leaves the button out of the row entirely rather than drawing it greyed. Nothing is placed for it and the buttons after it close the gap.")]
        public bool Enabled = true;

        [Tooltip("Drawn instead of the glyph the kind comes with. Tinted with the style's icon colour and fitted to the icon's square.")]
        public Sprite Icon;

        [Tooltip("This button, pressed. The bar's own OnPressed fires as well, and for the built-in kinds so does whatever the kind does.")]
        public UnityEvent OnPressed = new UnityEvent();

        public NavbarButton()
        {
        }

        public NavbarButton(ENavbarButton kind, string label = "")
        {
            Kind = kind;
            Label = label;
        }
    }
}
