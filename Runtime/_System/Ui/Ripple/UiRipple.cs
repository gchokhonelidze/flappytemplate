using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FlappyTemplate
{
    // A wave round a control when it is pressed: a ring that starts on the control's own outline, runs outwards
    // and fades, with a soft glow inside it, while the control itself dips a little and springs back. The thing
    // a player's finger is on answers before anything else does.
    //
    //     UiRipple.On(button.gameObject);                       // waves round the button on every press
    //     UiRipple.On(slider.gameObject, handle).Play();        // round the handle, once, now
    //
    // The ring is the control's own shape grown outwards - a pill stays a pill, a circle a circle - and is drawn
    // just behind it, as a sibling, so it comes out from under the control rather than across its face. A
    // sibling inside a grid or a layout group is taken out of its hands, the same way a window's close button
    // is, so nothing around the control moves.
    //
    // Play On Press is the usual case. A switch turns it off and calls Play from whatever flips it - a press, a
    // hotkey, a setting arriving from another tab - and passes the colour of the state it flipped to.
    [AddComponentMenu("UI/Ripple")]
    [DisallowMultipleComponent]
    public class UiRipple : MonoBehaviour, IPointerDownHandler
    {
        private const string RingName = "Ripple";

        [Tooltip("What the wave runs round. Empty is this object.")]
        [SerializeField]
        private RectTransform target;

        [Tooltip("Start a wave whenever this object is pressed. Off leaves it to Play, for a control that should answer to what it did rather than to the press.")]
        [SerializeField]
        private bool playOnPress = true;

        [Tooltip("The ring, and the glow inside it. Its alpha is where the wave starts; it fades from there to nothing.")]
        [SerializeField]
        private Color color = new Color(1f, 1f, 1f, 0.55f);

        [Tooltip("How far past the control's edge the wave runs before it is gone.")]
        [Min(0f)]
        [SerializeField]
        private float spread = 14f;

        [Tooltip("Seconds from the press to the last ring fading out.")]
        [Min(0.05f)]
        [SerializeField]
        private float duration = 0.6f;

        [Tooltip("Width of the ring as it sets off. It thins as it travels.")]
        [Min(0.5f)]
        [SerializeField]
        private float thickness = 3f;

        [Tooltip("The soft fill inside the ring, as a share of the ring's own alpha. Nought leaves a bare ring.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float glow = 0.35f;

        [Tooltip("Rings per wave, each setting off a little after the one before.")]
        [Range(1, 3)]
        [SerializeField]
        private int rings = 2;

        [Tooltip("Seconds between one ring of a wave and the next.")]
        [Min(0f)]
        [SerializeField]
        private float stagger = 0.12f;

        [Tooltip("How far the control dips on a press, as a share of its size. Nought keeps it still.")]
        [Range(0f, 0.3f)]
        [SerializeField]
        private float punch = 0.06f;

        // A ring in flight: the box that draws it, when it set off, and its colour - which is per wave, so a
        // switch can send a green one on and a grey one off without the two repainting each other.
        private struct Wave
        {
            public RoundedBox Box;
            public float Start;
            public Color Color;
        }

        // Not serialized: rings are made at runtime and only live as long as the scene does. A scene saved from
        // play mode has nothing of this in it.
        private readonly List<Wave> waves = new List<Wave>();
        private readonly List<RoundedBox> idle = new List<RoundedBox>();

        private Vector3 restScale = Vector3.one;
        private float punchStart = -1f;

        /// <summary>What the wave runs round. Null is this object.</summary>
        public RectTransform Target
        {
            get => target != null ? target : transform as RectTransform;
            set => target = value;
        }

        public bool PlayOnPress
        {
            get => playOnPress;
            set => playOnPress = value;
        }

        public Color Color
        {
            get => color;
            set => color = value;
        }

        public float Spread
        {
            get => spread;
            set => spread = Mathf.Max(0f, value);
        }

        public float Duration
        {
            get => duration;
            set => duration = Mathf.Max(0.05f, value);
        }

        public float Punch
        {
            get => punch;
            set => punch = Mathf.Clamp(value, 0f, 0.3f);
        }

        /// <summary>Puts a ripple on an object, or hands back the one already there. A target, when given, is
        /// what the wave runs round in place of the object itself.</summary>
        public static UiRipple On(GameObject host, RectTransform around = null)
        {
            if (host == null)
                return null;

            var ripple = host.GetComponent<UiRipple>();
            if (ripple == null)
                ripple = host.AddComponent<UiRipple>();

            if (around != null)
                ripple.target = around;

            return ripple;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!playOnPress || eventData.button != PointerEventData.InputButton.Left)
                return;

            // A control that cannot be used does not answer: a wave off a greyed-out button would say the press
            // did something.
            var selectable = GetComponent<Selectable>();
            if (selectable != null && !selectable.IsInteractable())
                return;

            Play();
        }

        /// <summary>Sends a wave out in this ripple's own colour.</summary>
        public void Play() => Play(color);

        /// <summary>Sends a wave out in a colour of its own - a switch's on colour, say. The alpha is where
        /// it starts.</summary>
        public void Play(Color tint)
        {
            // Rings are scene objects, so none are made outside play mode: a switch flipped from an editor
            // script would otherwise leave them saved into the scene.
            if (!Application.isPlaying || !isActiveAndEnabled)
                return;

            var around = Target;
            if (around == null || around.parent == null)
                return;

            float now = Time.unscaledTime;

            for (int i = 0; i < Mathf.Clamp(rings, 1, 3); i++)
            {
                var box = Take(around);
                if (box == null)
                    return;

                waves.Add(new Wave { Box = box, Start = now + i * stagger, Color = tint });
                Draw(waves[waves.Count - 1], now);
            }

            if (punch > 0f)
            {
                // The size it rests at is taken only while it is resting, so a second press mid-dip springs
                // back to the real size rather than to the dip.
                if (punchStart < 0f)
                    restScale = around.localScale;

                punchStart = now;
            }
        }

        /// <summary>Stops every wave and puts the control back at its own size.</summary>
        public void Stop()
        {
            for (int i = 0; i < waves.Count; i++)
                Park(waves[i].Box);

            waves.Clear();

            if (punchStart >= 0f && Target != null)
                Target.localScale = restScale;

            punchStart = -1f;
        }

        void OnDisable()
        {
            Stop();
        }

        void OnDestroy()
        {
            Stop();

            // The rings are siblings rather than children, so they do not go with this object on their own.
            for (int i = 0; i < idle.Count; i++)
            {
                if (idle[i] != null)
                    Destroy(idle[i].gameObject);
            }

            idle.Clear();
        }

        void LateUpdate()
        {
            float now = Time.unscaledTime;

            for (int i = waves.Count - 1; i >= 0; i--)
            {
                var wave = waves[i];

                if (wave.Box == null || now - wave.Start >= duration)
                {
                    Park(wave.Box);
                    waves.RemoveAt(i);
                    continue;
                }

                Draw(wave, now);
            }

            if (punchStart < 0f)
                return;

            var around = Target;
            if (around == null)
            {
                punchStart = -1f;
                return;
            }

            // In and out over the first half of the wave: a quick dip that is over by the time the ring is
            // properly on its way, so the control is back where the eye left it.
            float half = Mathf.Max(0.05f, duration * 0.45f);
            float u = (now - punchStart) / half;

            if (u >= 1f)
            {
                around.localScale = restScale;
                punchStart = -1f;
                return;
            }

            around.localScale = restScale * (1f - punch * Mathf.Sin(Mathf.PI * u));
        }

        // One frame of one ring. It eases out - fast off the edge, slowing as it fades - since a ring that ran at
        // an even pace would read as something sliding rather than as a wave spending itself.
        private void Draw(Wave wave, float now)
        {
            var box = wave.Box;
            var around = Target;
            if (box == null || around == null)
                return;

            float t = Mathf.Clamp01((now - wave.Start) / duration);

            // Not started yet: a staggered ring waits, invisible, at the edge.
            bool waiting = now < wave.Start;

            float ease = 1f - Mathf.Pow(1f - t, 3f);
            float fade = waiting ? 0f : Mathf.Pow(1f - t, 2f);
            float grow = spread * ease;

            // Measured off the rest size, not the dipping one, so the ring does not shrink with the punch.
            var size = Vector2.Scale(around.rect.size, Abs(punchStart >= 0f ? restScale : around.localScale));
            float radius = Mathf.Min(Radius(around), Mathf.Min(size.x, size.y) * 0.5f);

            var rect = box.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size + Vector2.one * (grow * 2f);
            rect.position = around.TransformPoint(around.rect.center);
            rect.localRotation = around.localRotation;

            var ring = wave.Color;
            ring.a *= fade;

            var fill = wave.Color;
            fill.a *= fade * glow * (1f - ease);

            box.FillColor = fill;
            box.SetCornerRadius(radius + grow);
            box.SetBorderSize(Mathf.Lerp(thickness, thickness * 0.25f, ease));
            box.SetBorderColor(ring);
        }

        // A ring that is not in use, or a new one. Placed just behind the target every time, since what is
        // around it may have been reordered since the last wave.
        private RoundedBox Take(RectTransform around)
        {
            RoundedBox box = null;

            while (idle.Count > 0 && box == null)
            {
                box = idle[idle.Count - 1];
                idle.RemoveAt(idle.Count - 1);
            }

            if (box == null)
            {
                var made = new GameObject(RingName, typeof(RectTransform), typeof(CanvasRenderer), typeof(RoundedBox), typeof(LayoutElement));

                // Out of the layout's hands: a grid or a layout group would otherwise place the ring as one more
                // cell, and everything beside the control would jump on every press.
                made.GetComponent<LayoutElement>().ignoreLayout = true;

                box = made.GetComponent<RoundedBox>();
                box.FillGradientMode = EFillGradient.None;
                box.EdgeSoftness = 1.25f;
                box.raycastTarget = false;
            }

            var rect = box.rectTransform;
            if (rect.parent != around.parent)
            {
                rect.SetParent(around.parent, false);
                rect.gameObject.layer = around.gameObject.layer;
                rect.localScale = Vector3.one;
            }

            // Just before the target. A ring that is already somewhere before it takes one off the index, since
            // moving it out of its old place shifts the target down by one first.
            int at = around.GetSiblingIndex();
            rect.SetSiblingIndex(rect.GetSiblingIndex() < at ? at - 1 : at);
            box.gameObject.SetActive(true);
            return box;
        }

        private void Park(RoundedBox box)
        {
            if (box == null)
                return;

            box.gameObject.SetActive(false);
            idle.Add(box);
        }

        // The control's own corner, so the ring is its shape grown outwards. A plain graphic has square
        // corners, which is what it draws.
        private static float Radius(RectTransform around)
        {
            var box = around.GetComponent<RoundedBox>();
            if (box == null)
                return 0f;

            return Mathf.Max(Mathf.Max(box.RadiusTopLeft, box.RadiusTopRight), Mathf.Max(box.RadiusBottomLeft, box.RadiusBottomRight));
        }

        private static Vector2 Abs(Vector3 scale) => new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
    }
}
