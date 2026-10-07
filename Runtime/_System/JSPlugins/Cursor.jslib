var CursorLib = {
	// Sets the CSS cursor on the canvas the game draws into - the hand, the arrow, the I-beam - which is the
	// only way a WebGL build has to show the browser's own cursors. On the canvas rather than the body, so
	// the page around an iframe or an embed keeps its own.
	SetCursorJS: function (cssPtr) {
		var css = UTF8ToString(cssPtr) || "default";
		var canvas = (typeof Module !== "undefined" && Module["canvas"]) || document.querySelector("canvas");
		if (!canvas) {
			return;
		}

		if (canvas.style.cursor !== css) {
			canvas.style.cursor = css;
		}
	},
};

mergeInto(LibraryManager.library, CursorLib);
