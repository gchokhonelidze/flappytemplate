using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FlappyTemplate.Editor
{
    // Puts Window, Statistics Window and Bet Info Window in GameObject > UI, next to Panel, and makes them
    // behave like it: a
    // canvas and an EventSystem appear if the scene has none, the object lands under whatever was
    // right-clicked, and one undo takes the whole lot back out.
    //
    // The canvas-finding and reparenting are RoundedBoxMenu's - the same three steps UGUI's own menu runs,
    // and there is no reason for a second copy of them.
    //
    // The window is built here rather than left to Awake because Awake does not run in the editor. A window
    // created from this menu has to arrive looking like a window: something that appeared as an empty rect
    // and only became a dialog on pressing play would be read as broken.
    public static class UiWindowMenu
    {
        // Panel sits at 2020. Landing just after it puts these in its group rather than off in a section of
        // their own - a window is the same kind of thing, one rung up.
        private const int MenuPriority = 2021;

        private static readonly Vector2 DefaultSize = new Vector2(420f, 320f);
        private static readonly Vector2 StatisticsSize = new Vector2(300f, 560f);
        private static readonly Vector2 BetInfoSize = new Vector2(560f, 620f);

        [MenuItem("GameObject/UI (Canvas)/Window", false, MenuPriority)]
        private static void CreateWindow(MenuCommand command)
        {
            var created = Create("Window", DefaultSize, command, out var window);
            window.Title = "Window";

            Undo.SetCurrentGroupName("Create Window");
            Selection.activeGameObject = created;
        }

        [MenuItem("GameObject/UI (Canvas)/Statistics Window", false, MenuPriority + 1)]
        private static void CreateStatisticsWindow(MenuCommand command)
        {
            var created = Create("Statistics", StatisticsSize, command, out var window, typeof(StatisticsWindow));
            window.Title = "Statistics";

            // Builds the tabs, the rows and the reset button, and takes the window's height from what it
            // ends up showing. Its own Awake would do this, and will not until the scene is played.
            created.GetComponent<StatisticsWindow>().Rebuild();

            Undo.SetCurrentGroupName("Create Statistics Window");
            Selection.activeGameObject = created;
        }

        [MenuItem("GameObject/UI (Canvas)/Bet Info Window", false, MenuPriority + 2)]
        private static void CreateBetInfoWindow(MenuCommand command)
        {
            var created = Create("Bet Info", BetInfoSize, command, out var window, typeof(BetInfoWindow));
            window.Title = "Bet info";

            // Builds the card, the fields and the buttons, and fills them in from the sample transaction - a
            // window created from this menu has to look like the dialog rather than like a row of dots waiting
            // for a server that is not running.
            created.GetComponent<BetInfoWindow>().Rebuild();

            Undo.SetCurrentGroupName("Create Bet Info Window");
            Selection.activeGameObject = created;
        }

        private static GameObject Create(string name, Vector2 size, MenuCommand command, out UiWindow window, params System.Type[] extras)
        {
            var parent = RoundedBoxMenu.ResolveParent(command.context as GameObject);

            // ObjectFactory rather than new GameObject: it registers the creation for undo and applies
            // whatever component defaults the project has set up. CanvasRenderer and RoundedBox are named
            // outright rather than left to the window to add - without a CanvasRenderer a graphic has
            // nothing to draw through, and it cannot be put back from the inspector afterwards.
            var types = new System.Type[4 + extras.Length];
            types[0] = typeof(RectTransform);
            types[1] = typeof(CanvasRenderer);
            types[2] = typeof(RoundedBox);
            types[3] = typeof(UiWindow);
            extras.CopyTo(types, 4);

            var created = ObjectFactory.CreateGameObject(name, types);
            ((RectTransform)created.transform).sizeDelta = size;

            window = created.GetComponent<UiWindow>();
            window.EnsureBuilt();
            window.ApplyStyle();

            // Built before it is parented, so the layer pass inside Parent reaches the caption and the rest
            // of it rather than only the object the menu made.
            GameObjectUtility.EnsureUniqueNameForSibling(created);
            RoundedBoxMenu.Parent(created, parent);

            return created;
        }
    }
}
