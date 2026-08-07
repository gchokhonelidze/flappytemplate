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

        [Tooltip("How many across before the next row. Enough of them that a single row would run off any screen.")]
        [SerializeField]
        private int columns = 4;

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

            // Four side colours, light above and left. They blend across the corner between them, which is
            // what keeps a bevel from breaking into four separate strips.
            RoundedBoxBuilder.Create(parent, "Bevel")
                .Size(cardSize)
                .At(Slot(slot++))
                .Fill(new Color(0.23f, 0.25f, 0.29f))
                .Corners(14f)
                .Border(3f, Color.white)
                .BorderColors(new Color(0.93f, 0.94f, 0.96f), new Color(0.93f, 0.94f, 0.96f),
                              new Color(0.42f, 0.45f, 0.5f), new Color(0.42f, 0.45f, 0.5f))
                .Done();

            // A square rect and Pill is a circle - the radius is held to the box, so it never has to be
            // kept in step with the size.
            RoundedBoxBuilder.Create(parent, "Avatar Ring")
                .Size(cardSize.y, cardSize.y)
                .At(Slot(slot++))
                .Fill(new Color(0.16f, 0.17f, 0.2f))
                .Pill()
                .Border(4f, Color.white)
                .BorderGradient(Ramp(new Color(1f, 0.82f, 0.4f), new Color(0.48f, 0.36f, 1f)), EBorderGradient.Around)
                .Done();

            // One side thick and the other three at nothing. The two Border overloads compose: the colour
            // goes on all four, then the thickness only where it belongs.
            RoundedBoxBuilder.Create(parent, "Banner")
                .Size(cardSize)
                .At(Slot(slot++))
                .Fill(new Color(0.16f, 0.14f, 0.1f))
                .Corners(10f)
                .Border(6f, new Color(0.94f, 0.63f, 0.24f))
                .Border(6f, 0f, 0f, 0f)
                .Done();

            // The same trick on the bottom edge, with the rule left square so it reads as a rule.
            RoundedBoxBuilder.Create(parent, "Field")
                .Size(cardSize.x, 56f)
                .At(Slot(slot++))
                .Fill(new Color(1f, 1f, 1f, 0.06f))
                .Corners(8f, 8f, 2f, 2f)
                .Border(2f, new Color(0.4f, 0.7f, 1f))
                .Border(0f, 0f, 0f, 2f)
                .Done();

            // Two boxes. The inner one is stretched to the track and inset, so the fill keeps its margin
            // whatever the track is resized to.
            var track = RoundedBoxBuilder.Create(parent, "Track")
                .Size(cardSize.x, 22f)
                .At(Slot(slot++))
                .Fill(Color.clear)
                .Pill()
                .Border(2f, new Color(0.35f, 0.38f, 0.45f))
                .Done();

            RoundedBoxBuilder.Create(track.transform, "Track Fill")
                .Stretch(3f)
                .Fill(Ramp(new Color(0.27f, 0.82f, 0.66f), new Color(0.18f, 0.55f, 1f)), 0f)
                .Pill()
                .Done();

            // A fill carries its alpha, so a panel can sit over artwork without a sprite behind it. The
            // hairline border is what stops it reading as a smudge.
            RoundedBoxBuilder.Create(parent, "Glass")
                .Size(cardSize)
                .At(Slot(slot++))
                .Fill(new Color(1f, 1f, 1f, 0.14f))
                .Corners(16f)
                .Border(1f, new Color(1f, 1f, 1f, 0.42f))
                .Done();

            // Mixed signs: one corner bitten out and the rest rounded off.
            RoundedBoxBuilder.Create(parent, "Bookmark")
                .Size(cardSize)
                .At(Slot(slot++))
                .Fill(new Color(0.18f, 0.49f, 0.42f))
                .Corners(12f, 12f, -22f, 12f)
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

        // Laid out from the top left going right, wrapping down. Anchored positions are measured from the
        // parent's own anchor, so this is a grid of offsets rather than anything the layout system knows
        // about - which is the point: none of these boxes need a layout group to be worth looking at.
        private Vector2 Slot(int index)
        {
            int across = Mathf.Max(1, columns);
            int column = index % across;
            int row = index / across;

            return new Vector2(column * (cardSize.x + spacing), -row * (cardSize.y + spacing));
        }

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
