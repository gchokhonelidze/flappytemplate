var ClipboardLib = {
	// A browser only lets a page write the clipboard while the player is doing something - inside the key
	// press or the click itself on Safari, within a few seconds of it elsewhere - and a cross-origin iframe,
	// which is what a game embedded on an operator's site is, gets no navigator.clipboard at all unless the
	// page around it allows it. A hidden textarea and execCommand("copy") work in all of those, as long as it
	// happens inside the event. Unity only sees an event a frame after the browser has finished with it, so the
	// game says ahead of time what would be copied, and this copies it in the browser's own event:
	//
	//   - Ctrl+C: offered while text is selected, copied in the keydown.
	//   - a copy button: offered when it is pressed, copied in the mouseup or touchend that ends the press.
	$Clipboard: {
		pending: "",
		pendingClick: "",
		installed: false,

		write: function (text) {
			var copied = false;

			try {
				var focused = document.activeElement;
				var area = document.createElement("textarea");
				area.value = text;
				area.setAttribute("readonly", "");
				area.style.position = "fixed";
				area.style.top = "-1000px";
				area.style.opacity = "0";
				document.body.appendChild(area);
				area.select();
				area.setSelectionRange(0, text.length);
				copied = document.execCommand("copy");
				document.body.removeChild(area);

				// The canvas had the keyboard; the textarea took it to copy. Handed back, or the next key the
				// player presses goes nowhere.
				if (focused && typeof focused.focus === "function") {
					focused.focus();
				}
			} catch (e) {
				copied = false;
			}

			if (!copied && navigator.clipboard && navigator.clipboard.writeText) {
				navigator.clipboard.writeText(text).catch(function () {});
				copied = true;
			}

			return copied;
		},

		install: function () {
			if (Clipboard.installed) {
				return;
			}

			Clipboard.installed = true;

			// Capture on the window, so this runs before the canvas's own listener whatever that does with the
			// event. e.code as well as e.key, because a Cyrillic or Greek layout reports another letter for C.
			window.addEventListener("keydown", function (e) {
				if (!Clipboard.pending || !(e.ctrlKey || e.metaKey) || e.altKey || e.shiftKey) {
					return;
				}

				if (e.key !== "c" && e.key !== "C" && e.code !== "KeyC") {
					return;
				}

				Clipboard.write(Clipboard.pending);
			}, true);

			// The release of a press on a copy button. Taken once: a touch is followed by a mouseup the browser
			// makes up for it, and that one finds nothing left to copy.
			var release = function () {
				if (!Clipboard.pendingClick) {
					return;
				}

				var text = Clipboard.pendingClick;
				Clipboard.pendingClick = "";
				Clipboard.write(text);
			};

			window.addEventListener("mouseup", release, true);
			window.addEventListener("touchend", release, true);
		},
	},

	// What the next Ctrl+C or Cmd+C copies. Empty string for nothing, which leaves the key to the page.
	OfferCopyJS: function (textPtr) {
		Clipboard.install();
		Clipboard.pending = textPtr ? UTF8ToString(textPtr) : "";
	},

	// What the release of the press now going on copies - a copy button, pressed. Empty string withdraws it:
	// the pointer left the button, or the press turned into a scroll.
	OfferClickCopyJS: function (textPtr) {
		Clipboard.install();
		Clipboard.pendingClick = textPtr ? UTF8ToString(textPtr) : "";
	},

	// Copies now. Only works close to a press or a click, and not at all on Safari outside the event itself.
	CopyTextJS: function (textPtr) {
		Clipboard.install();
		return Clipboard.write(UTF8ToString(textPtr)) ? 1 : 0;
	},
};

autoAddDeps(ClipboardLib, "$Clipboard");
mergeInto(LibraryManager.library, ClipboardLib);
