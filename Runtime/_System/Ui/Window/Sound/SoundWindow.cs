using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FlappyTemplate
{
    // The sound dialog, built into a UiWindow: one card per channel - effects and music - with a switch that
    // turns it on and off and a slider that says how loud it is.
    //
    //     var sound = SoundWindow.Create(canvas);
    //     sound.Window.Open();
    //
    // Or leave it to the navbar, which finds one in the scene or builds it - see Ui/Navbar.
    //
    // There is nothing to fill in and nothing to wire up. Every control reads and writes <see cref="Sounds"/>,
    // which is the same thing the game plays through - so a slider moved here is heard on the next click, and
    // the choice is remembered: it goes into `MainState.Settings` at once and out as SETTING, so it follows the
    // player to their next session and to the web front. A change made anywhere else - the other tab, a second
    // device - arrives as ON_SETTING and the dialog follows it while it is open.
    //
    // **A slider sends once, not per frame.** Dragging one moves the volume immediately - that is the point of
    // a volume slider - but the SETTING that keeps the choice is held until the drag has been still for
    // <see cref="SendDelay"/>, and flushed when the window closes. Sixty messages a second up a socket for one
    // finger movement is the thing this exists to avoid.
    //
    // A scene with no template running in it works exactly the same way, on the local defaults - both channels
    // on, both at full - so the dialog can be laid out and styled from a menu rather than from a running game.
    [AddComponentMenu("UI/Sound Window")]
    [RequireComponent(typeof(UiWindow))]
    public class SoundWindow : MonoBehaviour
    {
        // What each card answers to in the content grid's layout. One word each, and that is a requirement
        // rather than a habit: a layout is stored as text, so a name with a space in it comes back out as two
        // cells.
        private const string SoundArea = "effects";
        private const string MusicArea = "music";

        [SerializeField]
        private SoundWindowStyle style = new SoundWindowStyle();

        [Header("Labels")]
        // Serialized rather than translated, the same as the windows next door: the template has no opinion
        // about the game's language, and a game that has one already knows where its strings live. Both go
        // through Translator.Label, so a caption left as it came translates anyway.
        [SerializeField]
        private string soundLabel = "Sound effects";

        [SerializeField]
        private string musicLabel = "Music";

        [Header("Blocks")]
        [Tooltip("The effects card: the switch behind every click, coin and win the game plays.")]
        [SerializeField]
        private bool showSound = true;

        [Tooltip("The music card: the one looping bed under the game.")]
        [SerializeField]
        private bool showMusic = true;

        [Tooltip("The volume slider on each card. Off leaves the switches alone, for a game that offers on and off and nothing in between.")]
        [SerializeField]
        private bool showSliders = true;

        [Header("Behaviour")]
        [Tooltip("Follow the settings while the window is open, so a change made in another tab moves the controls here.")]
        [SerializeField]
        private bool followState = true;

        [Tooltip("Resize the window to exactly the cards it is showing.")]
        [SerializeField]
        private bool fitWindowHeight = true;

        [Tooltip("Seconds a slider has to be still before the choice is sent. Nought sends on every frame it moves, which is a message a frame up the socket.")]
        [Min(0f)]
        [SerializeField]
        private float sendDelay = 0.35f;

        [Header("Events")]
        [Tooltip("The effects switch, carrying what it has just been set to.")]
        public UnityEvent<bool> OnSoundToggled = new UnityEvent<bool>();

        [Tooltip("The music switch, carrying what it has just been set to.")]
        public UnityEvent<bool> OnMusicToggled = new UnityEvent<bool>();

        // Parts, and the flag saying they exist, are deliberately not serialized - the same choice the windows
        // next door make. Everything is found by name before it is made, so a rebuild after a script reload
        // finds the hierarchy that is already there rather than building a second one beside it.
        private UiWindow window;

        private Card sound;
        private Card music;

        private readonly List<GridTrack> contentRows = new List<GridTrack>();

        private bool built;
        private bool listening;

        // True while the controls are being written from the settings, so the change events they raise on the
        // way past are not read back as the player having moved something.
        private bool writing;

        // A slider that has moved and not yet been sent, and when it may be. See the note above.
        private bool pendingSound;
        private bool pendingMusic;
        private float sendAt;

        // Frames left to check the fit on after a refresh - see the end of Refresh.
        private int settle;

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

        /// <summary>Colours, sizes and fonts of the cards, the switches and the sliders. Edit and call
        /// <see cref="Rebuild"/>, or assign a whole new one.</summary>
        public SoundWindowStyle Style
        {
            get => style;
            set
            {
                style = value ?? new SoundWindowStyle();
                Rebuild();
            }
        }

        /// <summary>Seconds a slider has to be still before the setting is sent.</summary>
        public float SendDelay
        {
            get => sendDelay;
            set => sendDelay = Mathf.Max(0f, value);
        }

        /// <summary>The effects switch, for a game that would rather drive it from its own control.</summary>
        public Button SoundButton
        {
            get
            {
                EnsureBuilt();
                return sound.Switch;
            }
        }

        /// <summary>The music switch.</summary>
        public Button MusicButton
        {
            get
            {
                EnsureBuilt();
                return music.Switch;
            }
        }

        /// <summary>The effects volume slider, 0-1.</summary>
        public Slider SoundSlider
        {
            get
            {
                EnsureBuilt();
                return sound.Bar;
            }
        }

        /// <summary>The music volume slider, 0-1.</summary>
        public Slider MusicSlider
        {
            get
            {
                EnsureBuilt();
                return music.Bar;
            }
        }

        /// <summary>Builds the whole thing - window, cards and all - under a parent.</summary>
        // Added before the object wakes, for the reason UiWindowBuilder.Add exists: a component on an active
        // object runs its Awake there and then, and the cards would go into a window that had already put
        // itself away.
        public static SoundWindow Create(Transform parent, string name = "Sound", string title = "Sound")
        {
            UiWindowBuilder.Create(parent, name)
                .Size(460f, 340f)
                .Title(title)
                .Add(out SoundWindow made)
                .Done();

            // Awake has done this already in play mode. In the editor nothing else ever will, and a window
            // built from a context menu that came out empty would be a poor way to find that out.
            if (made != null)
                made.EnsureBuilt();

            return made;
        }

        void Awake()
        {
            EnsureBuilt();
        }

        void OnEnable()
        {
            EnsureBuilt();
            Listen(true);

            // Every caption goes through Translator.Label, so a language change is a repaint and nothing more.
            // Only while the dialog is open: a closed one refreshes on the way back in anyway.
            Translator.OnLocaleChanged += Refresh;

            Refresh();
        }

        void OnDisable()
        {
            Listen(false);
            Translator.OnLocaleChanged -= Refresh;

            // A window closed mid-drag has a volume the server has not been told about. Sent now rather than
            // dropped: the player set it, and the whole point of the setting is that it is remembered.
            Flush();
        }

        void LateUpdate()
        {
            if (pendingSound || pendingMusic)
            {
                if (Time.unscaledTime >= sendAt)
                    Flush();
            }

            if (settle <= 0)
                return;

            settle--;
            FitWindow();
        }

        /// <summary>Makes whatever is missing and lays it out. Safe to call as often as you like.</summary>
        public void EnsureBuilt()
        {
            if (built)
                return;

            Rebuild();
        }

        /// <summary>Builds the cards from scratch, then redraws. Call after changing the style from
        /// code.</summary>
        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            if (style == null)
                style = new SoundWindowStyle();

            var host = Window;
            if (host == null)
                return;

            host.EnsureBuilt();
            host.ApplyLayout();

            BuildParts(host.Content);

            built = true;
            Refresh();
        }

        /// <summary>Reads the settings again and writes them onto the controls. What a change arriving over
        /// the socket calls.</summary>
        public void Refresh()
        {
            if (!built)
                return;

            Layout();
            Write();

            // Two more passes over the next two frames. Everything the layout needs is measured inside Layout,
            // except what only the canvas can settle - a font that finished loading, a rect that had no width
            // yet because the window was activated this frame.
            settle = 2;
        }

        /// <summary>Sets every track, size and colour, then refits the window. Called by Refresh; separate
        /// because a game that has changed one style value wants this and not the rest.</summary>
        public void Layout()
        {
            if (!built)
                return;

            PaintContent();
            PaintCard(sound, soundLabel);
            PaintCard(music, musicLabel);
            Arrange();
            Tint();
            FitWindow();
        }

        /// <summary>Recolours both cards from what the settings now say, and nothing else. What a switch being
        /// pressed calls - relaying out the whole window per press would be felt.</summary>
        public void Tint()
        {
            if (!built)
                return;

            TintCard(sound);
            TintCard(music);
        }

        /// <summary>Flips a channel and repaints. What a switch does, and what a key bound to one should
        /// do.</summary>
        public void Toggle(ESoundChannel channel)
        {
            Sounds.Toggle(channel);

            bool on = Sounds.On(channel);

            // Ahead of the setting coming back over the socket. Sounds has already written it locally, so this
            // paints the answer the player is waiting for rather than the state they just left.
            Tint();

            if (channel == ESoundChannel.Music)
                OnMusicToggled.Invoke(on);
            else
                OnSoundToggled.Invoke(on);
        }

        /// <summary>Sends any volume a slider has moved and not yet reported. Called when the window closes,
        /// and by the delay running out.</summary>
        public void Flush()
        {
            if (pendingSound)
            {
                pendingSound = false;
                Sounds.Commit(ESoundChannel.Sound);
            }

            if (pendingMusic)
            {
                pendingMusic = false;
                Sounds.Commit(ESoundChannel.Music);
            }
        }

        // ------------------------------------------------------------------ the cards

        // One channel: a plate with a caption and a switch across the top, and a slider with its percentage
        // under it.
        private class Card
        {
            public ESoundChannel Channel;
            public RoundedBox Plate;
            public UiGrid Grid;
            public TextMeshProUGUI Caption;
            public RoundedBox Pill;
            public Button Switch;
            public RoundedBox Knob;
            public RectTransform Rail;
            public Slider Bar;
            public RoundedBox Track;
            public RectTransform FillArea;
            public RoundedBox Fill;
            public RectTransform HandleArea;
            public RoundedBox Handle;
            public TextMeshProUGUI Percent;
        }

        private void BuildParts(RectTransform content)
        {
            GridOn(content);

            sound = Adopt(content, "Effects", ESoundChannel.Sound, SoundArea);
            music = Adopt(content, "Music", ESoundChannel.Music, MusicArea);
        }

        // The parts of one card, found by name before they are made - so this is both how a card is built and
        // how one that came back from a prefab is picked up again.
        private Card Adopt(RectTransform content, string name, ESoundChannel channel, string area)
        {
            var plate = UiWindowParts.Box(content, name);

            var card = new Card
            {
                Channel = channel,
                Plate = plate,
                Grid = GridOn(plate.rectTransform),
            };

            card.Caption = UiWindowParts.Label(plate.transform, "Caption");

            card.Pill = UiWindowParts.Box(plate.transform, "Switch");
            card.Switch = card.Pill.GetComponent<Button>();
            if (card.Switch == null)
                card.Switch = card.Pill.gameObject.AddComponent<Button>();

            card.Switch.targetGraphic = card.Pill;
            card.Switch.transition = Selectable.Transition.None;

            card.Knob = UiWindowParts.Box(card.Pill.transform, "Knob");

            card.Rail = UiWindowParts.Rect(plate.transform, "Volume");
            card.Bar = card.Rail.GetComponent<Slider>();
            if (card.Bar == null)
                card.Bar = card.Rail.gameObject.AddComponent<Slider>();

            // The shape UGUI's Slider expects: a background, a fill inside an area inset by half the handle,
            // and the handle inside another area inset the same way. The Slider writes the fill's and the
            // handle's anchors as it moves, so nothing here sets those - only the areas they move inside.
            card.Track = UiWindowParts.Box(card.Rail, "Track");
            card.FillArea = UiWindowParts.Rect(card.Rail, "Fill Area");
            card.Fill = UiWindowParts.Box(card.FillArea, "Fill");
            card.HandleArea = UiWindowParts.Rect(card.Rail, "Handle Slide Area");
            card.Handle = UiWindowParts.Box(card.HandleArea, "Handle");

            card.Percent = UiWindowParts.Label(plate.transform, "Percent");

            // The name the content grid's layout knows the card by. Set once, here, where the parts are:
            // a name is a property of the panel, and Arrange only draws the picture that uses them.
            UiWindowParts.Name(plate.rectTransform, area);

            Hook(card);

            return card;
        }

        // Both handlers carry which channel they belong to, which a listener taken by name could not, so these
        // are closures - and a closure cannot be removed by comparing it to another one. Hence
        // RemoveAllListeners, which drops only the ones added from code: anything wired onto a generated
        // control in the inspector is a persistent call and survives it.
        private void Hook(Card card)
        {
            var channel = card.Channel;

            card.Switch.onClick.RemoveAllListeners();
            card.Switch.onClick.AddListener(() => Toggle(channel));

            card.Bar.onValueChanged.RemoveAllListeners();
            card.Bar.onValueChanged.AddListener(value => Moved(channel, value));
        }

        // A slider moved. The volume is applied at once - a volume slider that took a round trip to be heard
        // would be a volume slider nobody could set - and the setting is queued rather than sent.
        private void Moved(ESoundChannel channel, float value)
        {
            if (writing)
                return;

            Sounds.Preview(channel, value);

            if (channel == ESoundChannel.Music)
                pendingMusic = true;
            else
                pendingSound = true;

            sendAt = Time.unscaledTime + Mathf.Max(0f, sendDelay);

            // The number beside the slider, and the fill's colour if this was the move that took the channel
            // off nought. Only the colours - the sliders are where the player's finger is.
            Tint();
        }

        // ------------------------------------------------------------------ tracks and colours

        private void PaintContent()
        {
            var grid = GridOn(Window.Content);
            Columns(grid, GridTrack.Flexible());
            grid.RowGap = style.SectionGap;
            grid.ColumnGap = 0f;
            grid.padding = new RectOffset(0, 0, 0, 0);
        }

        // The shape of one card: the caption and the switch across the top, the slider and its percentage
        // under them. Said here rather than when the card was made, so a style edited from code reaches cards
        // that already exist.
        private void PaintCard(Card card, string caption)
        {
            int pad = Mathf.RoundToInt(style.CardPadding);

            card.Grid.padding = new RectOffset(pad, pad, pad, pad);
            card.Grid.ColumnGap = Mathf.Max(0f, style.SectionGap);
            card.Grid.RowGap = Mathf.Max(0f, style.RowGap);

            Columns(card.Grid, GridTrack.Flexible(), GridTrack.Fixed(RightColumn()));

            if (showSliders)
                Rows(card.Grid, GridTrack.Fixed(style.TitleHeight), GridTrack.Fixed(style.SliderHeight));
            else
                Rows(card.Grid, GridTrack.Fixed(style.TitleHeight));

            card.Plate.SetBorderSize(0f);
            card.Plate.SetCornerRadius(Mathf.Max(0f, style.CardCornerRadius));
            card.Plate.EdgeSoftness = 1.25f;
            card.Plate.raycastTarget = false;

            Put(card.Caption.rectTransform, 0, 0);
            Label(card.Caption, style.LabelFont, style.LabelSize, style.LabelColor, style.LabelStyle);
            card.Caption.alignment = TextAlignmentOptions.Left;
            card.Caption.overflowMode = TextOverflowModes.Ellipsis;
            card.Caption.text = Translator.Label(caption);

            PaintSwitch(card);
            PaintSlider(card);
        }

        // A pill with a knob at one end of it. The corner radius is half the height on both, so the two stay a
        // pill and a circle whatever size the style makes them.
        private void PaintSwitch(Card card)
        {
            var size = new Vector2(Mathf.Max(8f, style.SwitchSize.x), Mathf.Max(8f, style.SwitchSize.y));

            Middle(card.Pill.rectTransform, 1, 0, size);

            card.Pill.SetBorderSize(0f);
            card.Pill.SetCornerRadius(size.y * 0.5f);
            card.Pill.EdgeSoftness = 1.25f;
            card.Pill.raycastTarget = true;

            float inset = Mathf.Clamp(style.KnobInset, 0f, size.y * 0.4f);
            float knob = Mathf.Max(2f, size.y - inset * 2f);

            // Pinned to the left edge of the pill and moved along it by Tint, which is the one thing about a
            // switch that changes when it is pressed.
            card.Knob.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            card.Knob.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            card.Knob.rectTransform.pivot = new Vector2(0f, 0.5f);
            card.Knob.rectTransform.sizeDelta = new Vector2(knob, knob);

            card.Knob.SetBorderSize(0f);
            card.Knob.SetCornerRadius(knob * 0.5f);
            card.Knob.EdgeSoftness = 1.25f;
            card.Knob.raycastTarget = false;
        }

        private void PaintSlider(Card card)
        {
            bool on = showSliders;

            // Out of the grid's hands entirely rather than hidden with SetActive: a grid re-asserts what it
            // places, and an ignored child is neither placed, sized, shown nor hidden by it.
            UiWindowParts.Ignore(card.Rail, !on);
            UiWindowParts.Ignore(card.Percent.rectTransform, !on || !style.ShowPercent);

            card.Rail.gameObject.SetActive(on);
            card.Percent.gameObject.SetActive(on && style.ShowPercent);

            if (!on)
                return;

            Put(card.Rail, 0, 1);
            Put(card.Percent.rectTransform, 1, 1);

            float handle = Mathf.Max(4f, style.HandleSize);
            float half = handle * 0.5f;
            float track = Mathf.Max(1f, style.TrackHeight);

            // The rail is as tall as the row; the bar itself is a strip down the middle of it, so a thin track
            // is still comfortably draggable - the whole row catches the pointer. Measured off the style
            // rather than off the rect, which on the pass that builds the window has not been given a height
            // yet and would put the track at the very top of a row of nothing.
            float band = Mathf.Max(0f, (style.SliderHeight - track) * 0.5f);

            UiWindowParts.Stretch(card.Track.rectTransform, half, band, half, band);
            UiWindowParts.Stretch(card.FillArea, half, band, half, band);
            UiWindowParts.Stretch(card.HandleArea, half, 0f, half, 0f);

            // The fill fills its area and the handle is a circle in its own; both have their horizontal
            // anchors written by the Slider on every value, which is what moves them.
            card.Fill.rectTransform.offsetMin = Vector2.zero;
            card.Fill.rectTransform.offsetMax = Vector2.zero;

            card.Handle.rectTransform.anchorMin = new Vector2(0f, 0f);
            card.Handle.rectTransform.anchorMax = new Vector2(0f, 1f);
            card.Handle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            card.Handle.rectTransform.sizeDelta = new Vector2(handle, 0f);
            card.Handle.rectTransform.anchoredPosition = Vector2.zero;

            Paint(card.Track, style.TrackFill, track * 0.5f);
            Paint(card.Fill, style.FillColor, track * 0.5f);
            Paint(card.Handle, style.HandleFill, handle * 0.5f);

            // The track catches the pointer as well as the handle, so a tap anywhere along it jumps the
            // volume there - which is what a player expects of a bar and what a handle alone will not do.
            card.Track.raycastTarget = true;
            card.Handle.raycastTarget = true;

            card.Bar.fillRect = card.Fill.rectTransform;
            card.Bar.handleRect = card.Handle.rectTransform;
            card.Bar.targetGraphic = card.Handle;
            card.Bar.transition = Selectable.Transition.None;
            card.Bar.direction = Slider.Direction.LeftToRight;
            card.Bar.minValue = 0f;
            card.Bar.maxValue = 1f;
            card.Bar.wholeNumbers = false;

            Label(card.Percent, style.PercentFont, style.PercentSize, style.PercentColor, style.PercentStyle);
            card.Percent.alignment = TextAlignmentOptions.Right;
        }

        // What a card's colours say: the switch is green when the channel is on and grey when it is off, the
        // knob sits at the end that matches, and everything under a switched-off channel fades. Split out of
        // PaintCard because this is the half a press calls.
        private void TintCard(Card card)
        {
            if (card == null || card.Plate == null)
                return;

            bool on = Sounds.On(card.Channel);
            float volume = Sounds.Volume(card.Channel);

            card.Plate.FillGradientMode = EFillGradient.None;
            card.Plate.FillColor = on ? style.CardFill : style.CardOffFill;

            card.Caption.color = on ? style.LabelColor : style.LabelOffColor;

            card.Pill.FillGradientMode = EFillGradient.None;
            card.Pill.FillColor = on ? style.SwitchOnFill : style.SwitchOffFill;

            card.Knob.FillGradientMode = EFillGradient.None;
            card.Knob.FillColor = style.KnobFill;

            float inset = Mathf.Clamp(style.KnobInset, 0f, card.Pill.rectTransform.sizeDelta.y * 0.4f);
            float travel = Mathf.Max(0f, card.Pill.rectTransform.sizeDelta.x - card.Knob.rectTransform.sizeDelta.x - inset * 2f);
            card.Knob.rectTransform.anchoredPosition = new Vector2(inset + (on ? travel : 0f), 0f);

            if (!showSliders)
                return;

            card.Fill.FillGradientMode = EFillGradient.None;
            card.Fill.FillColor = on ? style.FillColor : style.FillOffColor;

            card.Handle.FillGradientMode = EFillGradient.None;
            card.Handle.FillColor = on ? style.HandleFill : style.HandleOffColor;

            if (!style.ShowPercent)
                return;

            card.Percent.color = on ? style.PercentColor : style.PercentOffColor;
            card.Percent.text = Mathf.RoundToInt(volume * 100f).ToString(CultureInfo.InvariantCulture) + "%";
        }

        // ------------------------------------------------------------------ what is showing

        // Says the arrangement as a layout - a picture of the grid, one area name per cell - rather than by
        // switching cards on and off. A UiGrid takes a layout as the whole truth about which of its children
        // are showing and re-asserts it every time it is enabled, so a card hidden with SetActive would come
        // back the next time the window opens.
        private void Arrange()
        {
            var layout = UiGridLayout.Build().Columns(GridTrack.Flexible());

            // Built per pass rather than kept: a gap between two tracks is there whether or not anything is in
            // them, so a card that is switched off has to take its row away with it.
            var rows = contentRows;
            rows.Clear();

            if (showSound)
            {
                layout.Row(SoundArea);
                rows.Add(GridTrack.Fixed(CardHeight()));
            }

            if (showMusic)
            {
                layout.Row(MusicArea);
                rows.Add(GridTrack.Fixed(CardHeight()));
            }

            // Neither card showing is a window with nothing in it, which is a fair thing to ask for from the
            // inspector and not a fair thing to hand a grid: a layout with no rows at all has no shape. One
            // empty row keeps it a rectangle.
            if (rows.Count == 0)
                rows.Add(GridTrack.Flexible());

            GridOn(Window.Content).SetLayout(layout.Rows(rows.ToArray()).Done());
        }

        // How tall one card comes to: its rows and the padding round them.
        private float CardHeight()
        {
            float rows = style.TitleHeight;

            if (showSliders)
                rows += style.RowGap + style.SliderHeight;

            return rows + style.CardPadding * 2f;
        }

        // The column the switch and the percentage share, which is as wide as the wider of the two.
        private float RightColumn()
        {
            float width = Mathf.Max(8f, style.SwitchSize.x);

            if (showSliders && style.ShowPercent)
                width = Mathf.Max(width, style.PercentWidth);

            return width;
        }

        // ------------------------------------------------------------------ the settings

        // The controls written from what the settings now say. Guarded, because writing a slider's value
        // raises its change event, and reading that back as a drag would send a SETTING for a move nobody
        // made.
        private void Write()
        {
            writing = true;

            try
            {
                if (sound != null && sound.Bar != null)
                    sound.Bar.SetValueWithoutNotify(Sounds.Volume(ESoundChannel.Sound));

                if (music != null && music.Bar != null)
                    music.Bar.SetValueWithoutNotify(Sounds.Volume(ESoundChannel.Music));
            }
            finally
            {
                writing = false;
            }

            Tint();
        }

        private void Listen(bool on)
        {
            if (!followState)
                return;

            if (on && !listening)
            {
                Sounds.OnChanged += Follow;
                listening = true;
            }
            else if (!on && listening)
            {
                Sounds.OnChanged -= Follow;
                listening = false;
            }
        }

        // A setting moved. While a slider is under a finger that is this window's own doing and the control is
        // already where it should be - writing it again mid-drag would fight the drag.
        private void Follow()
        {
            if (pendingSound || pendingMusic)
            {
                Tint();
                return;
            }

            Write();
        }

        // ------------------------------------------------------------------ the window around it

        // The window is sized from its contents out rather than the other way round: how tall the dialog has
        // to be depends on which cards it is showing.
        //
        // Twice, and that is not belt and braces. A label reports the height it needs at the width it has, and
        // its width is what the first pass settles - so on the way into a window that has just been activated
        // a wrapped caption measures as one line, and the card under it is left hanging below the panel.
        private void FitWindow()
        {
            var host = Window;
            if (!fitWindowHeight || host == null || !isActiveAndEnabled)
                return;

            host.Fit();
            host.Fit();
        }

        // ------------------------------------------------------------------ small change

        // LayoutGroup keeps its own rect to itself, so a grid's rect is reached through its transform.
        private static UiGrid GridOn(RectTransform rect)
        {
            var grid = rect.GetComponent<UiGrid>();
            if (grid == null)
                grid = rect.gameObject.AddComponent<UiGrid>();

            return grid;
        }

        /// <summary>Puts something in a cell, stretched to fill it.</summary>
        private static void Put(RectTransform rect, int column, int row)
        {
            var item = UiWindowParts.Item(rect);
            item.PlaceAt(column, row);
            item.Span(1, 1);
            item.OverrideAlign = false;
        }

        /// <summary>Puts something of its own size in the middle of a cell. For the switch, which a stretch
        /// would pull out of shape.</summary>
        private static void Middle(RectTransform rect, int column, int row, Vector2 size)
        {
            var item = UiWindowParts.Item(rect);
            item.PlaceAt(column, row);
            item.Span(1, 1);
            item.OverrideAlign = true;
            item.HorizontalAlign = EGridAlign.End;
            item.VerticalAlign = EGridAlign.Center;
            UiWindowParts.Measured(rect, size);
        }

        private static void Columns(UiGrid grid, params GridTrack[] tracks)
        {
            grid.Columns.Clear();
            grid.Columns.AddRange(tracks);
            grid.Rebuild();
        }

        private static void Rows(UiGrid grid, params GridTrack[] tracks)
        {
            grid.Rows.Clear();
            grid.Rows.AddRange(tracks);
            grid.Rebuild();
        }

        private static void Paint(RoundedBox box, Color fill, float radius)
        {
            box.FillGradientMode = EFillGradient.None;
            box.FillColor = fill;
            box.SetBorderSize(0f);
            box.SetCornerRadius(radius);
            box.EdgeSoftness = 1.25f;
            box.raycastTarget = false;
        }

        private static void Label(TextMeshProUGUI label, TMP_FontAsset font, float size, Color color, FontStyles fontStyle)
        {
            label.font = font != null ? font : label.font;
            label.fontSize = size;
            label.color = color;
            label.fontStyle = fontStyle;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
        }
    }
}
