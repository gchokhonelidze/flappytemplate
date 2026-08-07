using UnityEngine;

namespace FlappyTemplate
{
    // Builds one of each kind of box at runtime, as a row under whatever this sits on. Drop it on an empty
    // RectTransform inside a canvas and press play, or use Build Now from the component's context menu to
    // see it without leaving the editor.
    //
    // It is here to be read as much as run: every box below is one chain, and between them they cover the
    // whole of what RoundedBoxBuilder can say.
    [AddComponentMenu("UI/Rounded Box Example")]
    [RequireComponent(typeof(RectTransform))]
    public class RoundedBoxExample : MonoBehaviour
    {
        [SerializeField]
        private Vector2 cardSize = new Vector2(200f, 130f);

        [SerializeField]
        private float spacing = 24f;

        void Start()
        {
            Build();
        }

        [ContextMenu("Build Now")]
        public void Build()
        {
            Clear();

            int slot = 0;
            var parent = transform;

            // A plain card. The three fields that carry most of the work.
            RoundedBoxBuilder.Create(parent, "Card")
                .Size(cardSize)
                .At(Slot(slot++))
                .Fill(Color.white)
                .Corners(18f)
                .Border(2f, new Color(0.85f, 0.86f, 0.9f))
                .Done();

            // An outline with nothing behind it. A transparent fill is a fill like any other.
            RoundedBoxBuilder.Create(parent, "Outline")
                .Size(cardSize)
                .At(Slot(slot++))
                .Fill(Color.clear)
                .Corners(18f)
                .Border(2f, new Color(0.4f, 0.7f, 1f))
                .Done();

            // Corners set one at a time - square at the bottom, rounded on top, which is what makes a tab.
            RoundedBoxBuilder.Create(parent, "Tab")
                .Size(cardSize)
                .At(Slot(slot++))
                .Fill(new Color(0.16f, 0.17f, 0.2f))
                .Corners(18f, 18f, 0f, 0f)
                .Done();

            // A gradient across the box. The angle runs anticlockwise from left-to-right.
            RoundedBoxBuilder.Create(parent, "Gradient")
                .Size(cardSize)
                .At(Slot(slot++))
                .Fill(Ramp(new Color(0.35f, 0.4f, 1f), new Color(0.75f, 0.35f, 0.95f)), 60f)
                .Corners(18f)
                .Done();

            // The same ramp on the border instead, run around the outline rather than across the box.
            RoundedBoxBuilder.Create(parent, "Gradient Border")
                .Size(cardSize)
                .At(Slot(slot++))
                .Fill(new Color(0.09f, 0.1f, 0.13f))
                .Corners(18f)
                .Border(3f, Color.white)
                .BorderGradient(Ramp(new Color(1f, 0.4f, 0.5f), new Color(0.4f, 0.85f, 1f)), EBorderGradient.Around)
                .Done();

            // Frame runs the ramp across the thickness of each side, so its direction turns with the edge.
            RoundedBoxBuilder.Create(parent, "Frame")
                .Size(cardSize)
                .At(Slot(slot++))
                .Fill(new Color(0.12f, 0.12f, 0.14f))
                .Corners(10f)
                .Border(12f, Color.white)
                .BorderGradient(Ramp(new Color(0.95f, 0.85f, 0.6f), new Color(0.45f, 0.32f, 0.15f)), EBorderGradient.Frame)
                .Done();

            // Negative corners, bitten out rather than rounded off.
            RoundedBoxBuilder.Create(parent, "Ticket")
                .Size(cardSize)
                .At(Slot(slot++))
                .Fill(new Color(0.97f, 0.95f, 0.88f))
                .Scoop(20f)
                .Done();

            // A radius larger than the box is held to it, which leaves a capsule whatever the size. The
            // radial fill runs from the middle out along the shape, so it follows the capsule round.
            RoundedBoxBuilder.Create(parent, "Pill")
                .Size(new Vector2(cardSize.x, 52f))
                .At(Slot(slot++))
                .RadialFill(Ramp(new Color(0.32f, 0.88f, 0.62f), new Color(0.08f, 0.42f, 0.32f)))
                .Pill()
                .Done();
        }

        [ContextMenu("Clear")]
        public void Clear()
        {
            // Backwards, because destroying a child moves every one after it down a place.
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }
        }

        private Vector2 Slot(int index) => new Vector2(index * (cardSize.x + spacing), 0f);

        private static Gradient Ramp(Color from, Color to)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(from, 0f), new GradientColorKey(to, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });

            return gradient;
        }
    }
}
