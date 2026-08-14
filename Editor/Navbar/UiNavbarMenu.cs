using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FlappyTemplate.Editor
{
    // Puts Navbar in GameObject > UI (Canvas) > FlappyBet, and makes it behave like Panel: a canvas and an
    // EventSystem appear if the scene has none, the object lands under whatever was right-clicked, and one
    // undo takes the whole lot back out.
    //
    // The canvas-finding and reparenting are RoundedBoxMenu's - the same three steps UGUI's own menu runs,
    // and there is no reason for a second copy of them.
    //
    // The bar is built here rather than left to Awake because Awake does not run in the editor. One created
    // from this menu has to arrive as a row of buttons: something that appeared as an empty rect and only
    // became a navbar on pressing play would be read as broken.
    public static class UiNavbarMenu
    {
        // After the windows, which end at MenuPriority + 3.
        private const int MenuPriority = FlappyBetMenu.Priority + 30;

        [MenuItem(FlappyBetMenu.Group + "Navbar", false, MenuPriority)]
        private static void CreateNavbar(MenuCommand command)
        {
            var parent = RoundedBoxMenu.ResolveParent(command.context as GameObject);

            // ObjectFactory rather than new GameObject: it registers the creation for undo and applies
            // whatever component defaults the project has set up. CanvasRenderer and RoundedBox are named
            // outright rather than left to the bar to add - without a CanvasRenderer a graphic has nothing
            // to draw through, and it cannot be put back from the inspector afterwards.
            var created = ObjectFactory.CreateGameObject(
                "Navbar",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RoundedBox),
                typeof(UiNavbar));

            ((RectTransform)created.transform).sizeDelta = new Vector2(220f, 72f);

            // Builds the strip, the buttons and the glyphs in them, and sizes the bar to what it ends up
            // showing. The bar is [ExecuteAlways] and its own Awake has already done this by the line above;
            // asked for again here so that a change to how one is created never depends on that.
            created.GetComponent<UiNavbar>().Rebuild();

            // Built before it is parented, so the layer pass inside Parent reaches the buttons rather than
            // only the object the menu made.
            GameObjectUtility.EnsureUniqueNameForSibling(created);
            RoundedBoxMenu.Parent(created, parent);

            Undo.SetCurrentGroupName("Create Navbar");
            Selection.activeGameObject = created;
        }
    }
}
