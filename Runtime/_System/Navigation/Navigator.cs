#nullable enable

using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace FlappyTemplate
{
    // Leaving the game, which is a different thing from opening a link.
    //
    //     if (Navigator.CanReturn)
    //         Navigator.Home();
    //
    // A FlappyBet build is nearly always an iframe inside an operator's page, and the address the player
    // should end up at - SystemDto.ReturnUrl, sent over the socket - is a page on that operator's site, not
    // something to draw inside the canvas's own frame. So the navigation is aimed at the *top* window:
    // pointing the iframe at the lobby would put the lobby inside the game's rectangle and leave the
    // operator's chrome wrapped around it.
    //
    // Whether that is allowed is the browser's decision rather than ours, and it is not one that can be
    // asked about in advance - a cross-origin frame may write window.top.location without an error and be
    // refused all the same, since Location.href is writable across origins but the navigation itself is
    // held to the sandbox. Navigation.jslib therefore tries every way there is, in order, and tells the
    // parent page over postMessage as well, so a host that keeps top navigation to itself can act on it.
    // See the readme in Ui/Navbar.
    //
    // Outside WebGL - the editor, a standalone build - this is Application.OpenURL, which is the nearest
    // thing there is to leaving the page.
    public static class Navigator
    {
        /// <summary>The address the player leaves to, as the server last sent it. Null or empty means the
        /// operator did not send one, which is the usual case for a demo or a direct link, and means there
        /// is nowhere to go back to.</summary>
        public static string? ReturnUrl
        {
            get
            {
                var manager = StateManager.Inst;
                var system = manager != null && manager.MainState != null ? manager.MainState.SystemState : null;
                return system?.ReturnUrl;
            }
        }

        /// <summary>Whether there is somewhere to return to. What a Home button hides itself on.</summary>
        public static bool CanReturn => IsNavigable(ReturnUrl);

        /// <summary>Leaves for <see cref="ReturnUrl"/>. False means there was nothing to leave for, and
        /// nothing happened.</summary>
        public static bool Home() => LeaveTo(ReturnUrl);

        /// <summary>Sends the whole page - not the game's frame - to an address of your choosing. False
        /// means the address was empty or was not one this will navigate to.</summary>
        public static bool LeaveTo(string? url)
        {
            if (!IsNavigable(url))
                return false;

            var target = url!.Trim();

#if UNITY_WEBGL && !UNITY_EDITOR
            NavigateTopJS(target);
#else
            // No top window to speak of. OpenURL is what a player pressing Home in the editor should see
            // happen, and it is what a standalone build can do at all.
            Application.OpenURL(target);
#endif
            return true;
        }

        /// <summary>Whether an address is one this will send a player to: an http or https URL, or a path
        /// on the page the game is embedded in.</summary>
        // The address arrives over the socket rather than out of the scene, which makes it worth a look
        // before it is handed to a browser: javascript: and data: are addresses the same way an
        // instruction is an address, and a Home button that ran one would be running whatever the packet
        // said. Anything else unrecognised is refused rather than passed through, so the list below is the
        // whole of what can happen.
        public static bool IsNavigable(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            var value = url!.Trim();

            // "//host/path" - the page's own scheme, and a form operators do use.
            if (value.StartsWith("//"))
                return true;

            // A path or a query on the page the build is served from.
            if (value.StartsWith("/") || value.StartsWith("?") || value.StartsWith("#"))
                return true;

            int scheme = value.IndexOf(':');
            if (scheme < 0)
                return true;

            var name = value.Substring(0, scheme).ToLowerInvariant();
            return name == "http" || name == "https";
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void NavigateTopJS(string url);
#endif
    }
}
