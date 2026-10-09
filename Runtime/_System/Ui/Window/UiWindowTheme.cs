using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace FlappyTemplate
{
    // Repaints a window that already exists in the look a new one is born with. A window is only ever styled
    // the moment its parts are made - see UiWindowSeed - and a window's style is saved with the scene, so one
    // made before the charcoal look stays violet however the defaults change. This is the one deliberate way
    // to bring it over:
    //
    //     UiWindowTheme.Charcoal(window);              // the frame and what is drawn inside it
    //     UiWindowTheme.Charcoal(window, false);       // the frame alone
    //
    // Or GameObject > UI (Canvas) > FlappyBet > Restyle Windows, which runs it with undo over the selection, or
    // over every window in the open scenes when nothing is selected.
    //
    // The frame is the panel, the caption, the title, the close button and the scrollbar - colours only, so a
    // close button a game has moved stays where it was put. Inside, it is each window's style: every colour,
    // text weight, size and corner radius goes back to the package's default, and everything else stays -
    // fonts, icons, number formats, which blocks are showing, heights and gaps a game has tuned.
    public static class UiWindowTheme
    {
        /// <summary>Repaints a window's frame in the charcoal look and, unless told not to, puts its contents'
        /// colours, sizes and corners back to the package defaults.</summary>
        public static void Charcoal(UiWindow window, bool contents = true)
        {
            if (window == null)
                return;

            window.EnsureBuilt();

            UiWindowSeed.Panel(window.Panel);
            UiWindowSeed.Caption(window.Caption);
            UiWindowSeed.TitleLook(window.TitleText);
            UiWindowSeed.CloseLook(window.CloseBox);
            UiWindowSeed.ScrollTrack(window.ScrollTrack);
            UiWindowSeed.ScrollHandle(window.ScrollHandle);

            // The cross is two bars under Close/Cross. Found by walking rather than by name, so a cross a game
            // has redrawn with more parts is recoloured whole.
            var cross = window.CloseBox.transform.Find("Cross");
            if (cross != null)
            {
                foreach (var bar in cross.GetComponentsInChildren<RoundedBox>(true))
                    UiWindowSeed.BarLook(bar);
            }

            window.CloseWave = true;

            if (contents)
            {
                foreach (var part in window.GetComponents<MonoBehaviour>())
                    ResetStyle(part);
            }
        }

        /// <summary>Every component that hangs a style off a window - the statistics, bet info, game history,
        /// fairness, hotkeys, sound and bet info sheet windows - put back to its default look. Returns false
        /// for anything that has no style to reset.</summary>
        public static bool ResetStyle(MonoBehaviour part)
        {
            if (part == null)
                return false;

            // The public Style property every one of them has. Assigned back rather than edited in place,
            // because its setter is what rebuilds the window with what it now says.
            var property = part.GetType().GetProperty("Style", BindingFlags.Public | BindingFlags.Instance);
            if (property == null || !property.CanRead || !property.CanWrite || !property.PropertyType.Name.EndsWith("Style"))
                return false;

            var style = property.GetValue(part);
            if (style == null)
                return false;

            object fresh;

            try
            {
                fresh = Activator.CreateInstance(property.PropertyType);
            }
            catch (MissingMethodException)
            {
                return false;
            }

            foreach (var field in Look(property.PropertyType))
                field.SetValue(style, field.GetValue(fresh));

            property.SetValue(part, style);
            return true;
        }

        private static readonly Dictionary<Type, List<FieldInfo>> looks = new Dictionary<Type, List<FieldInfo>>();

        // What counts as the look, as opposed to what the game has decided about the window: colours, how text
        // is weighted, and how big things are and how round. Heights and gaps are left - an outcome strip, a
        // list that is allowed to grow - since those are as often a game's layout as they are the design's.
        private static List<FieldInfo> Look(Type type)
        {
            if (looks.TryGetValue(type, out var found))
                return found;

            found = new List<FieldInfo>();

            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var kind = field.FieldType;

                if (kind == typeof(Color) || kind == typeof(FontStyles))
                {
                    found.Add(field);
                    continue;
                }

                if ((kind == typeof(float) || kind == typeof(Vector2)) && (field.Name.EndsWith("Size") || field.Name.EndsWith("Radius")))
                    found.Add(field);
            }

            looks[type] = found;
            return found;
        }
    }
}
