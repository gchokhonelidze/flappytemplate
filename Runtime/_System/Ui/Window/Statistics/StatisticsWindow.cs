using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FlappyTemplate
{
    // The statistics panel, built into a UiWindow: two tabs over a column of values, and a reset button
    // under them that only ever clears the current session.
    //
    //     var stats = StatisticsWindow.Create(canvas);
    //     stats.Window.Open();
    //
    // Everything it shows comes from MainState.Statistics, which the server fills in and refreshes over the
    // socket. Nothing is polled: the window redraws when OnStatistics fires and when the player switches
    // tabs, and sits still the rest of the time.
    //
    // Which rows appear is a list in the inspector rather than anything fixed here. StatsDto carries ten
    // fields and no game wants all ten; the five in the defaults are the ones the design this was drawn
    // from asks for.
    [AddComponentMenu("UI/Statistics Window")]
    [RequireComponent(typeof(UiWindow))]
    public class StatisticsWindow : MonoBehaviour
    {
        // Height of a line of text as a multiple of its point size. TMP reports the real thing only after a
        // layout pass, and every row here is one line, so measuring is not worth a frame of waiting.
        private const float LineFactor = 1.25f;

        [SerializeField]
        private StatisticsWindowStyle style = new StatisticsWindowStyle();

        [Header("Tabs")]
        [SerializeField]
        private string currentLabel = "Current";

        [SerializeField]
        private string overallLabel = "Overall";

        [SerializeField]
        private EStatsTab tab = EStatsTab.Current;

        [Header("Rows")]
        [SerializeField]
        private List<StatisticsRow> rows = new List<StatisticsRow>
        {
            new StatisticsRow("Total wagered", EStatField.Wager),
            new StatisticsRow("Bets / Wins / Losses", EStatField.Counts, EStatTint.Counts),
            new StatisticsRow("Revenue", EStatField.GrossWin, EStatTint.Signed),
            new StatisticsRow("Total profit", EStatField.NetProfit, EStatTint.Signed),
            new StatisticsRow("Luck", EStatField.Luck, EStatTint.Strict),
        };

        [Header("Reset")]
        [SerializeField]
        private bool showReset = true;

        [Tooltip("Hide the button on the Overall tab. There is no such thing as resetting the overall figures, and a button that does nothing is worse than no button.")]
        [SerializeField]
        private bool resetOnCurrentOnly = true;

        [Tooltip("Keep the rows clear of the reset button. Off lets the button sit over the bottom of the column, which is tighter and how the design draws it.")]
        [SerializeField]
        private bool reserveResetRoom = true;

        [Header("Behaviour")]
        [Tooltip("Redraw when the server sends new statistics.")]
        [SerializeField]
        private bool followState = true;

        [Tooltip("Resize the window to exactly the rows it is showing.")]
        [SerializeField]
        private bool fitWindowHeight = true;

        [Header("Events")]
        [Tooltip("The reset button, before the request goes out.")]
        public UnityEvent OnResetRequested = new UnityEvent();

        public UnityEvent<EStatsTab> OnTabChanged = new UnityEvent<EStatsTab>();

        private UiWindow window;
        private RectTransform tabsRect;
        private RoundedBox currentTab;
        private RoundedBox overallTab;
        private TextMeshProUGUI currentTabLabel;
        private TextMeshProUGUI overallTabLabel;
        private RectTransform rowsRect;
        private RoundedBox resetBox;
        private Button resetButton;
        private RectTransform resetIcon;
        private RoundedBox resetRing;
        private RoundedBox resetGap;
        private RoundedBox resetHead;
        private Image resetSprite;

        private readonly List<TextMeshProUGUI> labels = new List<TextMeshProUGUI>();
        private readonly List<TextMeshProUGUI> values = new List<TextMeshProUGUI>();
        private readonly List<StatisticsRow> shown = new List<StatisticsRow>();

        private StatsDto preview;
        private bool built;
        private bool listening;

        /// <summary>The window this is drawn into. Open, close, drag and theme it through that.</summary>
        public UiWindow Window
        {
            get
            {
                if (window == null)
                    window = GetComponent<UiWindow>();

                return window;
            }
        }

        /// <summary>Which half of the figures is showing. Setting it moves the tabs and redraws.</summary>
        public EStatsTab Tab
        {
            get => tab;
            set => SetTab(value);
        }

        /// <summary>Colours, sizes and fonts of the tabs, rows and reset button. Edit and call
        /// <see cref="Rebuild"/>, or assign a whole new one.</summary>
        public StatisticsWindowStyle Style
        {
            get => style;
            set
            {
                style = value ?? new StatisticsWindowStyle();
                Rebuild();
            }
        }

        /// <summary>The rows, in the order they are drawn. Call <see cref="Rebuild"/> after changing it.</summary>
        public List<StatisticsRow> Rows => rows;

        /// <summary>The figures being drawn: the state's, or a sample when there is no StateManager in the
        /// scene at all - which is the case in a scene built to look at the window on its own.</summary>
        public StatisticsDto Showing
        {
            get
            {
                var stats = Source;
                if (stats == null)
                    return Empty;

                return (tab == EStatsTab.Current ? stats.Current : stats.Overall) ?? Empty;
            }
        }

        /// <summary>Builds the whole thing - window, statistics and all - under a parent.</summary>
        // Add before the object wakes, or UiWindow would build and hide itself between the two lines and
        // the statistics would go into a window that had already been put away.
        public static StatisticsWindow Create(Transform parent, string name = "Statistics", string title = "Statistics")
        {
            UiWindowBuilder.Create(parent, name)
                .Size(300f, 560f)
                .Title(title)
                .Add(out StatisticsWindow stats)
                .Done();

            // Awake has done this already in play mode. In the editor nothing else ever will, and a window
            // built from a context menu that came out empty would be a poor way to find that out.
            if (stats != null)
                stats.EnsureBuilt();

            return stats;
        }

        void Awake()
        {
            EnsureBuilt();
        }

        void OnEnable()
        {
            EnsureBuilt();
            Listen(true);
            Refresh();
        }

        void OnDisable()
        {
            Listen(false);
        }

        /// <summary>Makes whatever is missing and lays it out. Safe to call as often as you like.</summary>
        public void EnsureBuilt()
        {
            if (built)
                return;

            Rebuild();
        }

        /// <summary>Builds the tabs, the rows and the reset button from scratch, then redraws. Call after
        /// changing the style or the row list from code.</summary>
        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            if (style == null)
                style = new StatisticsWindowStyle();

            var host = Window;
            if (host == null)
                return;

            host.EnsureBuilt();
            host.ApplyLayout();

            var content = host.Content;

            BuildTabs(content);
            BuildRows(content);
            BuildReset(content);

            built = true;

            Layout();
            Refresh();
        }

        /// <summary>Switches tab. Nothing is asked of the server - both halves arrive together.</summary>
        public void SetTab(EStatsTab value)
        {
            tab = value;
            EnsureBuilt();
            ApplyTabs();
            Refresh();
            OnTabChanged.Invoke(value);
        }

        /// <summary>For wiring straight onto a button.</summary>
        public void ShowCurrent() => SetTab(EStatsTab.Current);

        public void ShowOverall() => SetTab(EStatsTab.Overall);

        /// <summary>Clears this session's figures, and only this session's - the overall ones are the
        /// account's history and the server would refuse anyway. Does nothing while the Overall tab is
        /// showing, whatever else is wired to it.</summary>
        // The reset goes out as a request and the figures come back as a broadcast; the window does not
        // blank itself in the meantime, so what is on screen is always what the server last said. Without a
        // socket - a scene with no template running - there is nobody to ask, so the sample is cleared
        // directly and the window has something to show.
        public void ResetCurrent()
        {
            if (tab != EStatsTab.Current)
                return;

            OnResetRequested.Invoke();

            if (Emitter.Inst != null)
            {
                Emitter.Inst.OnStatReset();
                return;
            }

            if (preview != null)
                preview.Current = new StatisticsDto();

            Refresh();
        }

        /// <summary>Reads the state again and repaints every row.</summary>
        public void Refresh()
        {
            if (!built)
                return;

            var data = Showing;

            for (int i = 0; i < shown.Count && i < values.Count; i++)
            {
                var row = shown[i];
                var value = values[i];

                if (row.Field == EStatField.Counts || row.Tint == EStatTint.Counts)
                {
                    // Every part of the line carries its own colour tag, so the label's own is only what
                    // shows through if the tags were ever dropped.
                    value.color = style.ValueColor;
                    value.text = CountsLine(data);
                    continue;
                }

                string text = Format(data, row);
                value.text = text;
                value.color = Tint(data, row);
            }

            ApplyResetVisibility();
        }

        private void BuildTabs(RectTransform content)
        {
            tabsRect = UiWindowParts.Rect(content, "Tabs");

            currentTab = UiWindowParts.Box(tabsRect, "Tab Current");
            currentTabLabel = UiWindowParts.Label(currentTab.transform, "Label");
            overallTab = UiWindowParts.Box(tabsRect, "Tab Overall");
            overallTabLabel = UiWindowParts.Label(overallTab.transform, "Label");

            Hook(currentTab, ShowCurrent);
            Hook(overallTab, ShowOverall);
        }

        private void Hook(RoundedBox box, UnityAction handler)
        {
            var button = box.GetComponent<Button>();
            if (button == null)
                button = box.gameObject.AddComponent<Button>();

            button.targetGraphic = box;
            button.onClick.RemoveListener(handler);
            button.onClick.AddListener(handler);
        }

        private void BuildRows(RectTransform content)
        {
            rowsRect = UiWindowParts.Rect(content, "Rows");

            shown.Clear();
            labels.Clear();
            values.Clear();

            int index = 0;
            foreach (var row in rows)
            {
                if (row == null || !row.Enabled)
                    continue;

                var holder = UiWindowParts.Rect(rowsRect, "Row " + index);
                var label = UiWindowParts.Label(holder, "Label");
                var value = UiWindowParts.Label(holder, "Value");

                label.text = row.Label;

                shown.Add(row);
                labels.Add(label);
                values.Add(value);
                index++;
            }

            // Rows that were built for a longer list last time. Left behind they would draw over the ones
            // that are still wanted, since nothing but this loop knows they are no longer in it.
            for (int i = rowsRect.childCount - 1; i >= index; i--)
                UiWindowParts.Discard(rowsRect.GetChild(i).gameObject);
        }

        private void BuildReset(RectTransform content)
        {
            resetBox = UiWindowParts.Box(content, "Reset");

            resetButton = resetBox.GetComponent<Button>();
            if (resetButton == null)
                resetButton = resetBox.gameObject.AddComponent<Button>();

            resetButton.targetGraphic = resetBox;
            resetButton.onClick.RemoveListener(ResetCurrent);
            resetButton.onClick.AddListener(ResetCurrent);

            resetIcon = UiWindowParts.Rect(resetBox.transform, "Arrow");
            resetRing = UiWindowParts.Box(resetIcon, "Ring");
            resetGap = UiWindowParts.Box(resetIcon, "Gap");
            resetHead = UiWindowParts.Box(resetIcon, "Head");
            resetSprite = UiWindowParts.Picture(resetBox.transform, "Icon");
        }

        /// <summary>Places everything and, if asked, resizes the window around it.</summary>
        public void Layout()
        {
            if (!built)
                return;

            float tabsHeight = style.TabHeight;
            float rowsHeight = MeasureRows();
            float bottomRoom = ResetRoom();

            // Anchored to both sides of the content, so widths are relative and nothing here needs to know
            // how wide the window ended up.
            UiWindowParts.TopStrip(tabsRect, tabsHeight, 0f, 0f);

            LayoutTab(currentTab.rectTransform, 0f, 0.5f, 0f, -style.TabSpacing * 0.5f);
            LayoutTab(overallTab.rectTransform, 0.5f, 1f, style.TabSpacing * 0.5f, 0f);

            LayoutTabLabel(currentTabLabel, currentLabel);
            LayoutTabLabel(overallTabLabel, overallLabel);

            rowsRect.anchorMin = new Vector2(0f, 0f);
            rowsRect.anchorMax = new Vector2(1f, 1f);
            rowsRect.pivot = new Vector2(0.5f, 1f);
            rowsRect.offsetMin = new Vector2(0f, bottomRoom);
            rowsRect.offsetMax = new Vector2(0f, -(tabsHeight + style.TabGap));

            float cursor = 0f;
            for (int i = 0; i < shown.Count; i++)
            {
                float labelHeight = style.LabelSize * LineFactor;
                float valueHeight = style.ValueSize * LineFactor;
                float height = labelHeight + style.LabelGap + valueHeight;

                var holder = (RectTransform)labels[i].transform.parent;
                UiWindowParts.TopStrip(holder, height, 0f, cursor);

                UiWindowParts.TopStrip(labels[i].rectTransform, labelHeight, 0f, 0f);
                UiWindowParts.TopStrip(values[i].rectTransform, valueHeight, 0f, labelHeight + style.LabelGap);

                Paint(labels[i], style.LabelFont, style.LabelSize, style.LabelColor, style.LabelStyle);
                Paint(values[i], style.ValueFont, style.ValueSize, style.ValueColor, style.ValueStyle);

                cursor += height + style.RowSpacing;
            }

            LayoutReset();

            if (fitWindowHeight)
                FitWindow(tabsHeight, rowsHeight, bottomRoom);

            ApplyTabs();
        }

        private void LayoutTab(RectTransform rect, float from, float to, float left, float right)
        {
            rect.anchorMin = new Vector2(from, 0f);
            rect.anchorMax = new Vector2(to, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, 0f);
            rect.offsetMax = new Vector2(right, 0f);
        }

        private void LayoutTabLabel(TextMeshProUGUI label, string text)
        {
            UiWindowParts.Stretch(label.rectTransform, 6f, 0f, 6f, 0f);
            label.text = text;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.font = style.TabFont != null ? style.TabFont : label.font;
            label.fontSize = style.TabSize;
            label.fontStyle = style.TabStyle;
        }

        private void LayoutReset()
        {
            UiWindowParts.Pin(resetBox.rectTransform, new Vector2(0f, 0f), style.ResetSize, style.ResetOffset);

            resetBox.FillGradientMode = EFillGradient.None;
            resetBox.FillColor = style.ResetFill;
            resetBox.SetBorderSize(0f);
            resetBox.SetCornerRadius(style.ResetCornerRadius);
            resetBox.EdgeSoftness = 1.25f;
            resetBox.raycastTarget = true;

            float span = Mathf.Min(style.ResetSize.x, style.ResetSize.y) * Mathf.Clamp01(style.ResetIconScale);
            bool useSprite = style.ResetIcon != null;

            resetSprite.gameObject.SetActive(useSprite);
            resetSprite.sprite = style.ResetIcon;
            resetSprite.color = style.ResetIconColor;
            resetSprite.preserveAspect = true;
            UiWindowParts.Pin(resetSprite.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(span, span), Vector2.zero);

            resetIcon.gameObject.SetActive(!useSprite);
            UiWindowParts.Pin(resetIcon, new Vector2(0.5f, 0.5f), new Vector2(span, span), Vector2.zero);

            // A circular arrow out of three boxes: a ring, a notch cut out of it in the button's own colour,
            // and a diamond for the head. Cheaper than an atlas entry and sharp at any size - the one thing
            // to know is that the notch is painted, not cut, so it only disappears against a flat button.
            float thickness = Mathf.Max(0.5f, style.ResetIconThickness);
            float radius = (span - thickness) * 0.5f;

            UiWindowParts.Pin(resetRing.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(span, span), Vector2.zero);
            resetRing.FillGradientMode = EFillGradient.None;
            resetRing.FillColor = Color.clear;
            resetRing.SetCornerRadius(100000f);
            resetRing.SetBorderSize(thickness);
            resetRing.SetBorderColor(style.ResetIconColor);
            resetRing.EdgeSoftness = 1f;
            resetRing.raycastTarget = false;

            var notch = new Vector2(Mathf.Cos(55f * Mathf.Deg2Rad), Mathf.Sin(55f * Mathf.Deg2Rad)) * radius;
            UiWindowParts.Pin(resetGap.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(thickness * 2.6f, thickness * 2.6f), notch);
            resetGap.FillGradientMode = EFillGradient.None;
            resetGap.FillColor = style.ResetFill;
            resetGap.SetBorderSize(0f);
            resetGap.SetCornerRadius(0f);
            resetGap.raycastTarget = false;

            UiWindowParts.Pin(resetHead.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(thickness * 2.1f, thickness * 2.1f), new Vector2(0f, radius));
            resetHead.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            resetHead.FillGradientMode = EFillGradient.None;
            resetHead.FillColor = style.ResetIconColor;
            resetHead.SetBorderSize(0f);
            resetHead.SetCornerRadius(1f);
            resetHead.raycastTarget = false;
        }

        private void ApplyTabs()
        {
            if (currentTab == null || overallTab == null)
                return;

            PaintTab(currentTab, currentTabLabel, tab == EStatsTab.Current);
            PaintTab(overallTab, overallTabLabel, tab == EStatsTab.Overall);
            ApplyResetVisibility();
        }

        private void PaintTab(RoundedBox box, TextMeshProUGUI label, bool active)
        {
            box.FillGradientMode = EFillGradient.None;
            box.FillColor = active ? style.TabActiveFill : style.TabIdleFill;
            box.SetBorderSize(0f);
            box.SetCornerRadius(style.TabCornerRadius);
            box.EdgeSoftness = 1.25f;
            box.raycastTarget = true;

            if (label != null)
                label.color = active ? style.TabActiveText : style.TabIdleText;
        }

        private void ApplyResetVisibility()
        {
            if (resetBox == null)
                return;

            bool visible = showReset && (!resetOnCurrentOnly || tab == EStatsTab.Current);
            resetBox.gameObject.SetActive(visible);
        }

        private void Paint(TextMeshProUGUI label, TMP_FontAsset font, float size, Color color, FontStyles fontStyle)
        {
            label.font = font != null ? font : label.font;
            label.fontSize = size;
            label.color = color;
            label.fontStyle = fontStyle;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
        }

        private float MeasureRows()
        {
            float total = 0f;

            for (int i = 0; i < shown.Count; i++)
            {
                total += style.LabelSize * LineFactor + style.LabelGap + style.ValueSize * LineFactor;

                if (i < shown.Count - 1)
                    total += style.RowSpacing;
            }

            return total;
        }

        private float ResetRoom()
        {
            bool visible = showReset && (!resetOnCurrentOnly || tab == EStatsTab.Current);

            if (!visible || !reserveResetRoom)
                return 0f;

            return style.ResetSize.y + style.ResetOffset.y + style.RowSpacing;
        }

        // The window is sized from the content out rather than the other way round: the rows are what they
        // are, and a fixed height either clips the last one or leaves a gap under it.
        //
        // The number is worked out here rather than measured by the window, because these rows are placed by
        // hand and a hand-placed rect reports no height of its own. Handed over rather than written straight
        // onto the rect, so that the window can hold it to what the screen has room for and scroll the rest.
        private void FitWindow(float tabsHeight, float rowsHeight, float bottomRoom)
        {
            var host = Window;
            if (host == null)
                return;

            // Chrome is the caption's row and the panel's border; the padding is what the content is inset
            // by inside that. The window works the first out for itself, border included, because the border
            // is read off the panel rather than held anywhere.
            float chrome = host.ChromeHeight + host.ContentPaddingTop + host.ContentPaddingBottom;

            host.FitTo(chrome + tabsHeight + style.TabGap + rowsHeight + bottomRoom);
        }

        private void Listen(bool on)
        {
            var manager = StateManager.Inst;
            if (manager == null || manager.Events == null || !followState)
                return;

            if (on && !listening)
            {
                manager.Events.OnStatistics.AddListener(HandleStatistics);
                listening = true;
            }
            else if (!on && listening)
            {
                manager.Events.OnStatistics.RemoveListener(HandleStatistics);
                listening = false;
            }
        }

        private void HandleStatistics(StatsDto stats)
        {
            Refresh();
        }

        private StatsDto Source
        {
            get
            {
                var manager = StateManager.Inst;
                if (manager != null)
                    return manager.MainState != null ? manager.MainState.Statistics : null;

                // No template running at all, which means a scene built to look at the window rather than to
                // play in. Sample figures beat five zeroes for seeing whether the colours work.
                return preview ??= Sample();
            }
        }

        // One shared set of zeroes for every window with nothing to show yet, rather than a fresh one per
        // redraw. Only ever read from.
        private static readonly StatisticsDto Empty = new StatisticsDto();

        private static StatsDto Sample() => new StatsDto
        {
            Current = new StatisticsDto
            {
                BetCount = 42,
                WinCount = 18,
                LoseCount = 24,
                Wager = "1250.50",
                WagerWon = "1180.00",
                WagerLost = "70.50",
                GrossWin = "1180.00",
                NetProfit = "-70.50",
                Payouts = "18",
                Luck = "0.94",
            },
            Overall = new StatisticsDto
            {
                BetCount = 1387,
                WinCount = 642,
                LoseCount = 745,
                Wager = "41902.75",
                WagerWon = "42533.10",
                WagerLost = "-630.35",
                GrossWin = "42533.10",
                NetProfit = "630.35",
                Payouts = "642",
                Luck = "1.02",
            },
        };

        private string CountsLine(StatisticsDto data)
        {
            // One string with the colours written into it rather than three labels to lay out: TMP prints
            // them, and the row stays one row however wide the numbers get.
            return string.Concat(
                Tag(data.BetCount.ToString(CultureInfo.InvariantCulture), style.CountColor),
                Tag(" / ", style.ValueColor),
                Tag(data.WinCount.ToString(CultureInfo.InvariantCulture), style.PositiveColor),
                Tag(" / ", style.ValueColor),
                Tag(data.LoseCount.ToString(CultureInfo.InvariantCulture), style.NegativeColor));
        }

        private static string Tag(string text, Color color) =>
            "<color=#" + Hex(color) + ">" + text + "</color>";

        private static string Hex(Color color) => ColorUtility.ToHtmlStringRGB(color);

        private string Format(StatisticsDto data, StatisticsRow row)
        {
            string raw = Read(data, row.Field);
            bool money = IsMoney(row.Field);
            string text = raw;

            // Money arrives as a string and is kept as one: a decimal that has been through a float is no
            // longer the number that was wagered. It is only reformatted when a decimal count says to, and
            // anything that will not parse is printed exactly as it came.
            if (style.Decimals >= 0 && decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var number))
                text = number.ToString((style.GroupDigits ? "N" : "F") + style.Decimals, CultureInfo.InvariantCulture);

            if (money && !string.IsNullOrEmpty(style.Prefix))
                text = style.Prefix + text;

            return text + row.Suffix;
        }

        private Color Tint(StatisticsDto data, StatisticsRow row)
        {
            if (row.Tint == EStatTint.Plain)
                return style.ValueColor;

            if (!decimal.TryParse(Read(data, row.Field), NumberStyles.Any, CultureInfo.InvariantCulture, out var number))
                return style.ValueColor;

            bool good = row.Tint == EStatTint.Strict ? number > 0m : number >= 0m;
            return good ? style.PositiveColor : style.NegativeColor;
        }

        private static string Read(StatisticsDto data, EStatField field)
        {
            switch (field)
            {
                case EStatField.Wager:
                    return data.Wager;
                case EStatField.WagerWon:
                    return data.WagerWon;
                case EStatField.WagerLost:
                    return data.WagerLost;
                case EStatField.GrossWin:
                    return data.GrossWin;
                case EStatField.NetProfit:
                    return data.NetProfit;
                case EStatField.Payouts:
                    return data.Payouts;
                case EStatField.Luck:
                    return data.Luck;
                case EStatField.BetCount:
                    return data.BetCount.ToString(CultureInfo.InvariantCulture);
                case EStatField.WinCount:
                    return data.WinCount.ToString(CultureInfo.InvariantCulture);
                case EStatField.LoseCount:
                    return data.LoseCount.ToString(CultureInfo.InvariantCulture);
                default:
                    return string.Empty;
            }
        }

        private static bool IsMoney(EStatField field)
        {
            switch (field)
            {
                case EStatField.Wager:
                case EStatField.WagerWon:
                case EStatField.WagerLost:
                case EStatField.GrossWin:
                case EStatField.NetProfit:
                    return true;
                default:
                    return false;
            }
        }
    }
}
