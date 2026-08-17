using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace FlappyTemplate
{
    // The keyboard drawn across the top of the hotkeys window: every cap in HotkeyCaps.Rows, with the bound ones
    // in the accent colour and whichever is held down lit. It is what turns a list of key names into something a
    // player reads at a glance - "the gold ones do something, and the one under my finger is the one I pressed".
    //
    // Built once and repainted, rather than rebuilt: sixty caps is sixty rounded boxes and sixty labels, and a
    // key going down happens in the middle of a round. Every part is found by the name it was made under, so a
    // window that came back from a prefab or a script reload picks up the caps that are already there instead of
    // making a second set beside them.
    //
    // A row is a grid of flexible tracks weighted by each cap's width, so every row fills the window whatever it
    // is made of and a table with a numpad added to it needs nothing here.
    internal class HotkeyKeyboard
    {
        private const string RowArea = "row";
        private const string CapArea = "c";

        private UiGrid board;

        // Flat, with the row boundaries kept beside it: the caps are painted in one pass and the rows are only
        // needed while the layout is being said.
        private readonly List<RoundedBox> plates = new List<RoundedBox>();
        private readonly List<TextMeshProUGUI> faces = new List<TextMeshProUGUI>();
        private readonly List<HotkeyCaps.Cap> caps = new List<HotkeyCaps.Cap>();
        private readonly List<UiGrid> lines = new List<UiGrid>();

        // Row lists rebuilt per pass rather than held: a track list is the shape of the layout, and the layout is
        // said again whenever the style changes.
        private readonly List<GridTrack> rowTracks = new List<GridTrack>();
        private readonly List<GridTrack> capTracks = new List<GridTrack>();

        /// <summary>Whether the caps exist yet.</summary>
        public bool IsBuilt => board != null;

        /// <summary>Makes the rows and the caps under a holder, or picks up the ones already there.</summary>
        public void Build(RectTransform holder)
        {
            if (holder == null)
                return;

            board = UiWindowParts.Grid(holder);

            plates.Clear();
            faces.Clear();
            caps.Clear();
            lines.Clear();

            var rows = HotkeyCaps.Rows;

            for (int r = 0; r < rows.Length; r++)
            {
                string number = r.ToString(CultureInfo.InvariantCulture);
                var line = UiWindowParts.Grid(UiWindowParts.Rect(holder, "Row " + number));

                UiWindowParts.Name((RectTransform)line.transform, RowArea + number);
                lines.Add(line);

                var row = rows[r];
                for (int c = 0; c < row.Length; c++)
                {
                    var plate = UiWindowParts.Box(line.transform, "Cap " + number + "_" + c.ToString(CultureInfo.InvariantCulture));
                    var face = UiWindowParts.Label(plate.transform, "Face");

                    UiWindowParts.Name(plate.rectTransform, CapArea + c.ToString(CultureInfo.InvariantCulture));

                    plates.Add(plate);
                    faces.Add(face);
                    caps.Add(row[c]);
                }
            }
        }

        /// <summary>How tall the whole thing wants to be, so the window can give it a row.</summary>
        public float Height(HotkeysWindowStyle style)
        {
            if (style == null)
                return 0f;

            int rows = HotkeyCaps.Rows.Length;
            if (rows == 0)
                return 0f;

            return rows * Mathf.Max(1f, style.KeyHeight)
                + (rows - 1) * Mathf.Max(0f, style.KeyRowGap)
                + Mathf.Max(0f, style.KeyboardPadding) * 2f;
        }

        /// <summary>Says the tracks and writes the faces. Called when the style changes rather than per frame -
        /// the colours are <see cref="Tint"/>'s job.</summary>
        public void Layout(HotkeysWindowStyle style)
        {
            if (board == null || style == null)
                return;

            int pad = Mathf.RoundToInt(Mathf.Max(0f, style.KeyboardPadding));
            board.padding = new RectOffset(pad, pad, pad, pad);
            board.RowGap = Mathf.Max(0f, style.KeyRowGap);
            board.ColumnGap = 0f;

            var rows = HotkeyCaps.Rows;

            // One row per line of caps, each exactly as tall as a cap. Said as a layout rather than by placing
            // the rows: a UiGrid takes its layout as the whole truth about which of its children are showing and
            // re-asserts it every time it is enabled, so a row placed behind its back would come back hidden.
            rowTracks.Clear();
            var layout = UiGridLayout.Build().Columns(GridTrack.Flexible());

            for (int r = 0; r < rows.Length; r++)
            {
                layout.Row(RowArea + r.ToString(CultureInfo.InvariantCulture));
                rowTracks.Add(GridTrack.Fixed(Mathf.Max(1f, style.KeyHeight)));
            }

            board.SetLayout(layout.Rows(rowTracks.ToArray()).Done());

            for (int r = 0; r < rows.Length && r < lines.Count; r++)
                LayoutRow(lines[r], rows[r], style);

            for (int i = 0; i < faces.Count && i < caps.Count; i++)
            {
                var face = faces[i];

                // Inset a hair so a wide word - `backspace` on a two-unit cap - has somewhere to shrink into
                // rather than running to the rounded corners.
                UiWindowParts.Stretch(face.rectTransform, 2f, 0f, 2f, 0f);

                face.text = caps[i].Face;
                face.font = style.KeyFont != null ? style.KeyFont : face.font;
                face.fontSize = style.KeyTextSize;
                face.fontStyle = style.KeyTextStyle;
                face.alignment = TextAlignmentOptions.Center;
                face.raycastTarget = false;

                // A long face on a narrow cap shrinks rather than spilling over the one beside it. The floor
                // keeps it readable - past that it is better clipped than pretended to be legible.
                face.enableAutoSizing = true;
                face.fontSizeMin = Mathf.Max(6f, style.KeyTextSize * 0.6f);
                face.fontSizeMax = style.KeyTextSize;
                face.textWrappingMode = TextWrappingModes.NoWrap;
                face.overflowMode = TextOverflowModes.Overflow;
            }
        }

        // One row: a flexible track per cap, weighted by how wide the cap is against a plain letter. Weights
        // rather than fixed widths, so the row fills the window at any size and the caps keep their proportions.
        private void LayoutRow(UiGrid line, HotkeyCaps.Cap[] row, HotkeysWindowStyle style)
        {
            if (line == null || row == null)
                return;

            line.padding = new RectOffset(0, 0, 0, 0);
            line.RowGap = 0f;
            line.ColumnGap = Mathf.Max(0f, style.KeyGap);

            capTracks.Clear();
            var names = new string[row.Length];

            for (int c = 0; c < row.Length; c++)
            {
                capTracks.Add(GridTrack.Flexible(row[c].Units));
                names[c] = CapArea + c.ToString(CultureInfo.InvariantCulture);
            }

            line.SetLayout(UiGridLayout.Build()
                .Columns(capTracks.ToArray())
                .Rows(GridTrack.Flexible())
                .Row(names)
                .Done());
        }

        /// <summary>Colours every cap from what is bound and what is held down. The cheap half, and what a key
        /// going down calls.</summary>
        // The bindings are handed in rather than read from the registry, so the picture and the list underneath it
        // are always showing the same thing - including the sample list a scene with nothing running in it is
        // styled against, which is not in the registry at all.
        public void Tint(HotkeysWindowStyle style, Dictionary<KeyCode, Hotkey> bindings)
        {
            if (board == null || style == null)
                return;

            bool on = Hotkeys.Enabled;

            for (int i = 0; i < plates.Count && i < caps.Count; i++)
            {
                var plate = plates[i];
                if (plate == null)
                    continue;

                Hotkey binding = null;
                if (bindings != null)
                    bindings.TryGetValue(caps[i].Key, out binding);

                // Held down beats bound beats plain. A cap only lights while hotkeys are switched on, because a
                // key that lights and then does nothing is worse than one that does neither - and the whole
                // reason the footer button is on the window is that the two states have to be told apart.
                bool live = on && binding != null && binding.Enabled;
                bool held = live && binding.IsDown;

                var fill = held ? style.KeyDownFill : live ? style.KeyBoundFill : style.KeyFill;
                var ink = held ? style.KeyDownTextColor : live ? style.KeyBoundTextColor : style.KeyTextColor;

                plate.FillGradientMode = EFillGradient.None;
                plate.FillColor = fill;
                plate.SetBorderSize(live ? 0f : Mathf.Max(0f, style.KeyBorderSize));
                plate.SetBorderColor(style.KeyBorderColor);
                plate.SetCornerRadius(Mathf.Max(0f, style.KeyCornerRadius));
                plate.EdgeSoftness = 1.25f;

                // Nothing on the picture catches a click. It is a picture of a keyboard, not a keyboard - a cap
                // that took a press would look like a button that does nothing.
                plate.raycastTarget = false;

                if (i < faces.Count && faces[i] != null)
                    faces[i].color = ink;
            }
        }
    }
}
