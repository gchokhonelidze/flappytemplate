using System.Collections.Generic;
using UnityEngine;

namespace FlappyTemplate
{
    // One of each kind of gradient and border, built as a grid under whatever this sits on. Drop it on an
    // empty RectTransform inside a canvas and press play, or use Build Now from the component's context menu
    // to see it without leaving the editor.
    //
    // It is here to be read as much as run: every graphic below is one chain, and between them they cover
    // the whole of what SpriteGradientBuilder can say.
    //
    // With no sprite handed to it, it draws its own - a star and a ring, painted into a texture at startup -
    // so that it can be dropped into an empty scene and still show something.
    [AddComponentMenu("UI/Sprite Gradient Example")]
    [RequireComponent(typeof(RectTransform))]
    public class SpriteGradientExample : MonoBehaviour
    {
        [Tooltip("The picture to run through every variant. Left empty, a star is drawn at startup instead.")]
        [SerializeField]
        private Sprite shape = null;

        [Tooltip("A second picture with a hole in it, to show that a border follows every loop of a silhouette rather than only its outside. Left empty, a ring is drawn instead.")]
        [SerializeField]
        private Sprite holedShape = null;

        [SerializeField]
        private Vector2 cellSize = new Vector2(140f, 140f);

        [SerializeField]
        private float spacing = 24f;

        [Tooltip("How many across before the next row.")]
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

            var star = shape != null ? shape : MakeStar();
            var ring = holedShape != null ? holedShape : MakeRing();

            var warm = Ramp(new Color(1f, 0.83f, 0.35f), new Color(0.95f, 0.35f, 0.25f));
            var cool = Ramp(new Color(0.35f, 0.55f, 1f), new Color(0.7f, 0.35f, 0.95f));
            var metal = Ramp(new Color(1f, 0.95f, 0.7f), new Color(0.55f, 0.4f, 0.1f));

            int slot = 0;
            var parent = transform;

            // No gradient at all. The component is an Image with a longer reach, and this is it being one.
            SpriteGradientBuilder.Create(parent, "Flat")
                .Picture(star, true)
                .Size(cellSize)
                .At(Slot(slot++))
                .Fill(new Color(1f, 0.78f, 0.2f))
                .Done();

            // The usual case: a ramp painted through the sprite, bottom to top.
            SpriteGradientBuilder.Create(parent, "Gradient")
                .Picture(star, true)
                .Size(cellSize)
                .At(Slot(slot++))
                .Fill(warm)
                .Done();

            // The same ramp turned. 0 runs left to right and the angle goes anticlockwise from there; it
            // always spans the sprite, so the ends stay on its edges whatever the angle is.
            SpriteGradientBuilder.Create(parent, "Turned")
                .Picture(star, true)
                .Size(cellSize)
                .At(Slot(slot++))
                .Fill(cool, 30f)
                .Done();

            // Out from the middle instead of across. The ramp is measured to the corner of the sprite, so it
            // is a circle spreading over the picture rather than a rectangle following its edge.
            SpriteGradientBuilder.Create(parent, "Radial")
                .Picture(star, true)
                .Size(cellSize)
                .At(Slot(slot++))
                .RadialFill(Ramp(Color.white, new Color(0.2f, 0.45f, 0.9f)))
                .Done();

            // A border in one colour, grown outward off the silhouette so the picture is left whole.
            SpriteGradientBuilder.Create(parent, "Border")
                .Picture(star, true)
                .Size(cellSize)
                .At(Slot(slot++))
                .Fill(warm)
                .Border(4f, new Color(0.25f, 0.12f, 0f))
                .Done();

            // The ramp on the border rather than the fill, run around the outline.
            SpriteGradientBuilder.Create(parent, "Border Around")
                .Picture(star, true)
                .Size(cellSize)
                .At(Slot(slot++))
                .Fill(new Color(0.11f, 0.12f, 0.16f))
                .Border(5f, cool)
                .Done();

            // Across the thickness of the border instead of along it - the moulding of a picture frame.
            SpriteGradientBuilder.Create(parent, "Border Frame")
                .Picture(star, true)
                .Size(cellSize)
                .At(Slot(slot++))
                .Fill(new Color(0.11f, 0.12f, 0.16f))
                .Border(9f, metal, EBorderGradient.Frame)
                .Done();

            // Grown inward instead: the shape keeps its size and the border is painted over the edge of the
            // picture. Drawn last of everything, or the sprite would cover it.
            SpriteGradientBuilder.Create(parent, "Border Inside")
                .Picture(star, true)
                .Size(cellSize)
                .At(Slot(slot++))
                .Fill(warm)
                .Border(6f, new Color(1f, 1f, 1f, 0.85f))
                .Placement(ESpriteBorderPlacement.Inside)
                .Done();

            // A shape with a hole in it. The trace comes back with a loop for the outside and another for
            // the hole, wound the other way, and the border is grown off both.
            SpriteGradientBuilder.Create(parent, "Hole")
                .Picture(ring, true)
                .Size(cellSize)
                .At(Slot(slot++))
                .Fill(cool, 45f)
                .Border(4f, Color.white)
                .Done();

            // Tint over a gradient: the fill colour multiplies the ramp rather than replacing it, which is
            // how a whole graphic is faded or dimmed without touching its keys.
            SpriteGradientBuilder.Create(parent, "Dimmed")
                .Picture(star, true)
                .Size(cellSize)
                .At(Slot(slot++))
                .Fill(warm)
                .FillTint(new Color(0.45f, 0.45f, 0.5f))
                .Border(4f, new Color(0.3f, 0.3f, 0.35f))
                .Done();
        }

        [ContextMenu("Clear")]
        public void Clear()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }
        }

        private Vector2 Slot(int index)
        {
            int wide = Mathf.Max(1, columns);
            int column = index % wide;
            int row = index / wide;

            return new Vector2(column * (cellSize.x + spacing), -row * (cellSize.y + spacing));
        }

        private static Gradient Ramp(Color from, Color to)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(from, 0f), new GradientColorKey(to, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });

            return gradient;
        }

        // ---- the stand-in pictures ---------------------------------------------------------------------

        private const int TextureSize = 256;

        private static Sprite MakeStar()
        {
            var points = new List<Vector2>();
            for (int i = 0; i < 10; i++)
            {
                // Starting a quarter turn round so the star stands up rather than lying on its side.
                float angle = Mathf.PI * 0.5f + i * Mathf.PI / 5f;
                float radius = (i % 2 == 0 ? 0.47f : 0.2f) * TextureSize;
                points.Add(new Vector2(
                    TextureSize * 0.5f + Mathf.Cos(angle) * radius,
                    TextureSize * 0.5f + Mathf.Sin(angle) * radius));
            }

            return Paint((x, y) => Coverage(points, x, y), "Star");
        }

        private static Sprite MakeRing()
        {
            float outer = TextureSize * 0.46f;
            float inner = TextureSize * 0.22f;
            var center = new Vector2(TextureSize * 0.5f, TextureSize * 0.5f);

            return Paint((x, y) =>
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);

                // A pixel of fade either side of both edges, which is all it takes for the trace to find a
                // clean threshold to follow rather than a staircase.
                return Mathf.Min(
                    Mathf.Clamp01(outer - distance),
                    Mathf.Clamp01(distance - inner));
            }, "Ring");
        }

        // Four samples per pixel, which is enough of an edge for a silhouette to be traced off cleanly.
        private static float Coverage(List<Vector2> polygon, int x, int y)
        {
            int hits = 0;
            for (int sy = 0; sy < 2; sy++)
            {
                for (int sx = 0; sx < 2; sx++)
                {
                    if (Contains(polygon, new Vector2(x + 0.25f + sx * 0.5f, y + 0.25f + sy * 0.5f)))
                        hits++;
                }
            }

            return hits * 0.25f;
        }

        private static bool Contains(List<Vector2> polygon, Vector2 point)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                var a = polygon[i];
                var b = polygon[j];
                if (a.y > point.y != b.y > point.y &&
                    point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        private static Sprite Paint(System.Func<int, int, float> alphaAt, string name)
        {
            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
            {
                name = name,
                hideFlags = HideFlags.DontSave,
                wrapMode = TextureWrapMode.Clamp,
            };

            var pixels = new Color32[TextureSize * TextureSize];
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(alphaAt(x, y)) * 255f);

                    // White throughout, so the colour the component paints through it arrives unchanged.
                    pixels[y * TextureSize + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, TextureSize, TextureSize), new Vector2(0.5f, 0.5f));
            sprite.name = name;
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }
    }
}
