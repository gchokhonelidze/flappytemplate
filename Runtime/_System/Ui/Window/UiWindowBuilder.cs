using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace FlappyTemplate
{
    // A whole window in one chain, the way RoundedBoxBuilder does a box.
    //
    //     UiWindowBuilder.Create(canvas, "Settings")
    //         .Size(360f, 480f)
    //         .Title("Settings")
    //         .Panel(box => box.FillColor = new Color(.2f, .18f, .35f))
    //         .Draggable()
    //         .Backdrop()
    //         .Open();
    //
    // The steps here say where things go and how the window behaves. What it looks like is the parts' own:
    // Panel, Caption, Title and Close each hand back the component to set as you like, which is the same
    // thing selecting it in the hierarchy does.
    //
    // A struct holding one reference, so a chain allocates nothing, and a step against a window that has
    // been destroyed is passed over rather than thrown.
    //
    // Create leaves the new object switched off until the chain ends. That is not a detail: a MonoBehaviour
    // added to an active object runs Awake there and then, and a window that woke up in the middle of the
    // chain would have built and hidden itself before it had been told how big it is. Done switches it on,
    // and the window wakes with every answer already in place.
    public readonly struct UiWindowBuilder
    {
        private readonly UiWindow window;

        private UiWindowBuilder(UiWindow window)
        {
            this.window = window;
        }

        /// <summary>The window being built. Null only if it was destroyed part way through.</summary>
        public UiWindow Window => window;

        /// <summary>Starts on a window that already exists. Steps apply to it as they are called.</summary>
        public static UiWindowBuilder For(UiWindow existing) => new UiWindowBuilder(existing);

        /// <summary>Makes a new window under a parent - usually a canvas, or a panel inside one.</summary>
        public static UiWindowBuilder Create(Transform parent, string name = "Window")
        {
            var created = new GameObject(name, typeof(RectTransform));
            created.SetActive(false);

            var rect = (RectTransform)created.transform;

            if (parent != null)
            {
                rect.SetParent(parent, false);
                created.layer = parent.gameObject.layer;
            }

            // Centred by default: a dialog that lands in the middle of its parent is right far more often
            // than one that lands wherever the anchors happened to be, and it is what the slide transitions
            // and the drag clamp both assume.
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(360f, 480f);

            return new UiWindowBuilder(created.AddComponent<UiWindow>());
        }

        /// <summary>The finished window, closed and waiting for Open unless Start Open was asked for.</summary>
        public UiWindow Done()
        {
            Wake();
            return window;
        }

        /// <summary>Finishes and opens it, with the transition.</summary>
        public UiWindow Open()
        {
            Wake();

            if (window != null)
                window.Open();

            return window;
        }

        /// <summary>Finishes and shows it with no transition at all.</summary>
        public UiWindow OpenInstant()
        {
            Wake();

            if (window != null)
                window.OpenInstant();

            return window;
        }

        public static implicit operator UiWindow(UiWindowBuilder builder) => builder.window;

        public UiWindowBuilder Size(float width, float height) => Size(new Vector2(width, height));

        public UiWindowBuilder Size(Vector2 size)
        {
            if (window != null)
                window.Rect.sizeDelta = size;

            return this;
        }

        /// <summary>Scales the whole window, chrome and contents alike. The transition is measured against
        /// this, so opening does not undo it.</summary>
        public UiWindowBuilder Scale(float uniform)
        {
            if (window != null)
                window.SetScale(uniform);

            return this;
        }

        public UiWindowBuilder At(Vector2 anchoredPosition)
        {
            if (window != null)
                window.Rect.anchoredPosition = anchoredPosition;

            return this;
        }

        public UiWindowBuilder Title(string text)
        {
            if (window != null)
                window.Title = text;

            return this;
        }

        /// <summary>Height of the header block. What it looks like is the Caption box's own business - the
        /// chain hands it back with <see cref="Caption()"/> for that.</summary>
        public UiWindowBuilder Caption(float height)
        {
            if (window != null)
                window.CaptionHeight = height;

            return this;
        }

        /// <summary>No header at all - content starts at the top of the panel. The close button stays.</summary>
        public UiWindowBuilder NoCaption()
        {
            if (window != null)
                window.ShowCaption = false;

            return this;
        }

        /// <summary>The panel, for styling it: colour, corners, border, gradient - a RoundedBox like any
        /// other. The window never paints over what is set here.</summary>
        //
        //     .Panel(box => { box.FillColor = navy; box.SetCornerRadius(20f); })
        //
        // A callback rather than a step per colour: what a box can be told is RoundedBox's business, and a
        // chain that mirrored all of it would be a second copy of that inspector.
        public UiWindowBuilder Panel(Action<RoundedBox> style)
        {
            if (window != null && style != null)
                style(window.Panel);

            return this;
        }

        /// <summary>The header block, for styling it. Its height is <see cref="Caption(float)"/>.</summary>
        public UiWindowBuilder Caption(Action<RoundedBox> style)
        {
            if (window != null && style != null)
                style(window.Caption);

            return this;
        }

        /// <summary>The title label, for font, size, colour and alignment.</summary>
        public UiWindowBuilder Title(Action<TextMeshProUGUI> style)
        {
            if (window != null && style != null)
                style(window.TitleText);

            return this;
        }

        /// <summary>The close button's box. The cross inside it is two rotated boxes under Close/Cross, and
        /// an Image with a sprite dropped on the button does just as well.</summary>
        public UiWindowBuilder Close(Action<RoundedBox> style)
        {
            if (window != null && style != null)
                style(window.CloseBox);

            return this;
        }

        /// <summary>Inset of the content area from the panel edges. Top is measured from the bottom of the
        /// caption rather than the top of the panel.</summary>
        public UiWindowBuilder Padding(float left, float top, float right, float bottom)
        {
            if (window != null)
                window.SetContentPadding(left, top, right, bottom);

            return this;
        }

        /// <summary>The same inset on all four sides.</summary>
        public UiWindowBuilder Padding(float all) => Padding(all, all, all, all);

        public UiWindowBuilder CloseButton(bool shown = true)
        {
            if (window != null)
                window.ShowCloseButton = shown;

            return this;
        }

        public UiWindowBuilder Draggable(bool anywhere = false, bool clampToParent = true)
        {
            if (window == null)
                return this;

            window.DragAnywhere = anywhere;
            window.ClampToParent = clampToParent;
            window.Draggable = true;
            return this;
        }

        /// <summary>Nailed down. The drag handle is taken off rather than ignored.</summary>
        public UiWindowBuilder Fixed()
        {
            if (window != null)
                window.Draggable = false;

            return this;
        }

        /// <summary>A sheet across the parent behind the window, which is what makes it modal.</summary>
        public UiWindowBuilder Backdrop(bool closeOnClick = true)
        {
            if (window == null)
                return this;

            window.CloseOnBackdropClick = closeOnClick;
            window.ShowBackdrop = true;
            return this;
        }

        /// <summary>The same, and the sheet's colour with it. Only its colour is the sheet's own - the fade
        /// in and out goes through a group beside it, so this is not written over.</summary>
        public UiWindowBuilder Backdrop(Color color, bool closeOnClick = true)
        {
            Backdrop(closeOnClick);

            if (window != null && window.Backdrop != null)
                window.Backdrop.color = color;

            return this;
        }

        public UiWindowBuilder NoBackdrop()
        {
            if (window != null)
                window.ShowBackdrop = false;

            return this;
        }

        /// <summary>Draws the window on a canvas of its own, above everything sorting below this order -
        /// other canvases and the game's own sprites alike. On by default; this is for changing the order.</summary>
        public UiWindowBuilder OnTop(int order = 100, string layer = null)
        {
            if (window == null)
                return this;

            window.SortingOrder = order;

            if (layer != null)
                window.SortingLayer = layer;

            window.AlwaysOnTop = true;
            return this;
        }

        /// <summary>Sorted by hierarchy position like any other UI object.</summary>
        public UiWindowBuilder InLine()
        {
            if (window != null)
                window.AlwaysOnTop = false;

            return this;
        }

        public UiWindowBuilder Transition(EWindowTransition kind, float openDuration = 0f, float closeDuration = 0f)
        {
            if (window == null)
                return this;

            window.Transition = kind;

            if (openDuration > 0f)
                window.OpenDuration = openDuration;

            if (closeDuration > 0f)
                window.CloseDuration = closeDuration;

            return this;
        }

        public UiWindowBuilder Easing(Ease open, Ease close)
        {
            if (window == null)
                return this;

            window.OpenEase = open;
            window.CloseEase = close;
            return this;
        }

        /// <summary>Leaves the window on screen when the chain ends, instead of waiting for Open.</summary>
        public UiWindowBuilder StartOpen()
        {
            if (window != null)
                window.StartClosed = false;

            return this;
        }

        /// <summary>For a window built for one message - it takes itself down with it.</summary>
        public UiWindowBuilder DestroyOnClose()
        {
            if (window != null)
                window.DestroyOnClose = true;

            return this;
        }

        public UiWindowBuilder OnOpened(UnityAction handler)
        {
            if (window != null && handler != null)
                window.OnOpened.AddListener(handler);

            return this;
        }

        public UiWindowBuilder OnClosed(UnityAction handler)
        {
            if (window != null && handler != null)
                window.OnClosed.AddListener(handler);

            return this;
        }

        /// <summary>Adds a component to the window while it is still switched off, so its Awake runs at the
        /// end of the chain along with the window's own rather than in the middle of it. This is how
        /// StatisticsWindow gets onto a window built from code.</summary>
        public UiWindowBuilder Add<T>(out T added) where T : Component
        {
            added = window != null ? window.gameObject.AddComponent<T>() : null;
            return this;
        }

        /// <summary>Whatever the window is for, parented into its content area.</summary>
        public UiWindowBuilder With(RectTransform child)
        {
            if (window == null || child == null)
                return this;

            Wake();
            child.SetParent(window.Content, false);
            return this;
        }

        // Only for a window this chain created, which is the one case where the object is still switched
        // off. A window handed to For is left as it was found - reactivating a closed one here would open
        // it behind the animation's back.
        //
        // Building it by hand afterwards is for the editor, where Awake does not run at all: a chain called
        // from a context menu has to leave a window that can be seen. In play mode Awake has already been
        // and gone by this line, and both calls find nothing left to do.
        private void Wake()
        {
            if (window == null || window.IsBuilt)
                return;

            if (!window.gameObject.activeSelf)
                window.gameObject.SetActive(true);

            window.EnsureBuilt();
            window.ApplyLayout();
        }
    }
}
