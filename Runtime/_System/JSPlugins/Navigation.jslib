var NavigationLib = {
	$navvars: {
		// True when the build runs inside an iframe. Reading window.top throws on a cross-origin
		// parent, which is itself the answer - the same test Socket.jslib makes.
		inIframe: function () {
			try {
				return window.self !== window.top;
			} catch (err) {
				return true;
			}
		},
		// The envelope the aggregator front already speaks (see Socket.jslib), so a host page that
		// keeps top navigation to itself has something to listen for. Sent whether or not the
		// attempts below go on to work: both ways lead to the same address, and there is no way to
		// ask a browser afterwards which of them it honoured.
		tellParent: function (url) {
			var packet = { EventName: "NAVIGATE", Data: { Url: url } };
			try {
				window.parent.postMessage(packet, "*");
			} catch (err) {
				console.warn("Navigation.jslib: postMessage to parent failed: " + err.toString());
			}
			try {
				if (window.chrome && window.chrome.webview && typeof window.chrome.webview.postMessage === "function") {
					window.chrome.webview.postMessage(packet);
				}
			} catch (err) {
				console.warn("Navigation.jslib: postMessage to webview failed: " + err.toString());
			}
		},
		// Location.href is writable across origins, so this is allowed to be tried from inside a
		// frame whose parent we cannot otherwise read. It throws only where the sandbox refuses
		// outright; where the sandbox refuses quietly, nothing here can tell, which is why the
		// parent was told first.
		setTop: function (url) {
			try {
				window.top.location.href = url;
				return true;
			} catch (err) {
				return false;
			}
		},
		// A link with target="_top", clicked. The one route a sandboxed frame is given by
		// allow-top-navigation-by-user-activation, and a press on a Unity canvas is that activation.
		clickTop: function (url) {
			try {
				var link = document.createElement("a");
				link.href = url;
				link.target = "_top";
				link.style.display = "none";
				document.body.appendChild(link);
				link.click();
				document.body.removeChild(link);
				return true;
			} catch (err) {
				return false;
			}
		},
		openTop: function (url) {
			try {
				return window.open(url, "_top") != null;
			} catch (err) {
				return false;
			}
		},
	},

	// Sends the whole page to a URL rather than the canvas's own frame. Pointing the frame at a lobby
	// would draw that lobby inside the game's rectangle, with the operator's chrome still around it,
	// which is not what leaving the game means.
	NavigateTopJS: function (urlPtr) {
		var url = UTF8ToString(urlPtr);
		if (!url) {
			return;
		}

		if (!navvars.inIframe()) {
			window.location.href = url;
			return;
		}

		navvars.tellParent(url);

		if (navvars.setTop(url)) {
			return;
		}

		if (navvars.clickTop(url)) {
			return;
		}

		if (navvars.openTop(url)) {
			return;
		}

		// Every route to the top window was refused outright. Taking the frame there is not what was
		// asked for - the operator's page stays wrapped around it - but it is the destination, and a
		// button that does nothing at all is worse.
		console.warn("Navigation.jslib: top navigation refused, leaving the frame instead.");
		window.location.href = url;
	},
};

autoAddDeps(NavigationLib, "$navvars");
mergeInto(LibraryManager.library, NavigationLib);
