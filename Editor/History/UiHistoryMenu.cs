using UnityEditor;
using UnityEngine;

namespace FlappyTemplate.Editor
{
    // Puts History in GameObject > UI, next to Panel, and makes it behave like it: a canvas and an EventSystem
    // appear if the scene has none, the object lands under whatever was right-clicked, and one undo takes the
    // whole lot back out.
    //
    // The canvas-finding and reparenting are RoundedBoxMenu's - the same three steps UGUI's own menu runs, and
    // there is no reason for a second copy of them.
    //
    // The strip is built here rather than left to Awake because Awake does not run in the editor. One created
    // from this menu has to arrive looking like a history strip: it comes up filled with sample bets, which are
    // replaced the moment a server is talking.
    public static class UiHistoryMenu
    {
        // Window sits at 2021 and the two dialogs after it. This is the same kind of thing one rung along.
        private const int MenuPriority = 2024;

        private static readonly Vector2 DefaultSize = new Vector2(620f, 64f);

        [MenuItem("GameObject/UI (Canvas)/History", false, MenuPriority)]
        private static void CreateHistory(MenuCommand command)
        {
            var parent = RoundedBoxMenu.ResolveParent(command.context as GameObject);

            // ObjectFactory rather than new GameObject: it registers the creation for undo and applies whatever
            // component defaults the project has set up.
            var created = ObjectFactory.CreateGameObject("History", typeof(RectTransform), typeof(UiHistory));
            ((RectTransform)created.transform).sizeDelta = DefaultSize;

            var history = created.GetComponent<UiHistory>();
            history.Rebuild();

            // Built before it is parented, so the layer pass inside Parent reaches the elements rather than only
            // the object the menu made.
            GameObjectUtility.EnsureUniqueNameForSibling(created);
            RoundedBoxMenu.Parent(created, parent);

            // Sampling is what Awake's Seed would have done in play mode. Without it the strip arrives as an
            // empty rect, which reads as a component that did not work.
            history.Sample(11);

            Undo.SetCurrentGroupName("Create History");
            Selection.activeGameObject = created;
        }
    }
}
