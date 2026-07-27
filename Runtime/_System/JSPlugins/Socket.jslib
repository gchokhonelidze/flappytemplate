var SocketLib = {
	$vars: {
		objectName: null,
		instance: null,
		connection: null,
		domain: null,
		// True when the build runs inside an iframe. In that case there is no SignalR
		// connection at all — the parent page owns the socket and we talk to it over
		// postMessage instead (see startBridge/onParentMessage/postToParent).
		bridge: false,
		bridgeListener: null,
		// Mirrors the server hub options. Only the values the browser client can apply are sent
		// from Unity; handshake timeout and message size have no equivalent here.
		config: {
			serverTimeout: 30000,
			keepAliveInterval: 15000,
			transport: 1,
			skipNegotiation: true,
			// Delay before each reconnect attempt. Running off the end of the list means give up —
			// a fixed ladder rather than withAutomaticReconnect()'s indefinite retrying.
			retryDelays: [500, 2000, 5000, 10000],
		},
		// Set when the server sends "Disconnect": the drop is deliberate, so the ladder is abandoned.
		preventReconnect: false,
		// Packets coming from the aggregator front carry this marker; anything else on the
		// message bus (dev tools, other embeds) is ignored.
		senderIn: "PandoraAggFront",
		inIframe: function () {
			try {
				return window.self !== window.top;
			} catch (err) {
				// Cross-origin parent — reading window.top throws, which itself means we are embedded.
				return true;
			}
		},
		resolveInstance: function () {
			if (!vars.instance) {
				vars.instance = window.MyGameInstance || window.unityInstance || window.gameInstance;
			}
			return vars.instance;
		},
		sendToUnity: function (methodName, msg) {
			// console.log("JS sending:", msg);
			var gi = vars.resolveInstance();
			if (gi && typeof gi.SendMessage === "function") {
				gi.SendMessage(vars.objectName, methodName, msg);
			} else if (typeof SendMessage === "function") {
				SendMessage(vars.objectName, methodName, msg);
			} else {
				console.warn("Socket.jslib: Unity instance not found.");
			}
		},
		serialize: function (value) {
			if (value == null) {
				return "";
			}
			if (typeof value === "string") {
				return value;
			}
			if (typeof value === "object") {
				try {
					return JSON.stringify(value);
				} catch (err) {
					return String(value);
				}
			}
			return String(value);
		},
		connect: function () {
			if (!signalR) {
				console.error("Socket.jslib: signalR library not found. Make sure to include the SignalR JavaScript client library.");
				return "";
			}
			try {
				var currentUrl = new URL(window.location.href);
				var params = {};
				currentUrl.searchParams.forEach(function (value, key) {
					params[key] = value;
				});
				var domain = (vars.domain || "").replace(/\/+$/, "");
				var url = domain + "/sock?token=" + params.token;
				console.log("Connecting to Socket server at:", url);
				// WebSockets only — no transport fallback, so negotiate is skippable.
				var urlOptions = {
					transport: vars.config.transport,
					skipNegotiation: vars.config.skipNegotiation,
				};
				vars.preventReconnect = false;
				var retryPolicy = {
					nextRetryDelayInMilliseconds: function (ctx) {
						if (vars.preventReconnect) return null;
						var delays = vars.config.retryDelays || [];
						var next = delays[ctx.previousRetryCount];
						// null ends the reconnect loop for good.
						return typeof next === "number" ? next : null;
					},
				};
				vars.connection = new signalR.HubConnectionBuilder().withUrl(url, urlOptions).withAutomaticReconnect(retryPolicy).build();
				vars.connection.serverTimeoutInMilliseconds = vars.config.serverTimeout;
				vars.connection.keepAliveIntervalInMilliseconds = vars.config.keepAliveInterval;
				vars.connection.on("OnData", function (message) {
					console.log("Received from server:", message);
					vars.sendToUnity("OnData", vars.serialize(message));
				});
				vars.connection.on("Disconnect", function (reason) {
					console.log("Socket.jslib: Disconnected by server: " + reason);
					vars.preventReconnect = true;
					vars.connection.stop();
					vars.sendToUnity("OnSocketDisconnected", vars.serialize(reason));
				});
				vars.connection.onreconnecting(function (err) {
					console.warn("Socket.jslib: Reconnecting: " + (err ? err.toString() : ""));
				});
				vars.connection.onreconnected(function () {
					console.log("Socket.jslib: Reconnected.");
				});
				vars.connection.onclose(function (err) {
					console.warn("Socket.jslib: Connection closed: " + (err ? err.toString() : ""));
					vars.sendToUnity("OnSocketDisconnected", err ? err.toString() : "");
				});
				vars.connection
					.start()
					.then(function () {
						console.log("Socket.jslib: Connected to server.");
					})
					.catch(function (err) {
						console.error("Socket.jslib: Connection error: " + err.toString());
					});
				return url;
			} catch (err) {
				console.error("Socket.jslib: Failed to connect: " + err.toString());
				return "";
			}
		},

		// --- iframe bridge -------------------------------------------------------

		startBridge: function () {
			if (vars.bridgeListener) return;
			vars.bridgeListener = function (event) {
				vars.onParentMessage(event);
			};
			window.addEventListener("message", vars.bridgeListener);
			console.log("Socket.jslib: running in iframe — using postMessage bridge, SignalR disabled.");
		},
		stopBridge: function () {
			if (!vars.bridgeListener) return;
			window.removeEventListener("message", vars.bridgeListener);
			vars.bridgeListener = null;
		},
		onParentMessage: function (event) {
			var o = event && event.data;
			if (!o || typeof o !== "object" || o.Sender !== vars.senderIn) return;
			if (!o.EventName || !o.Data) return;
			if (o.EventName === "ON_H") {
				// Layout probe — the parent wants our current height, answered here so it
				// never round-trips through Unity.
				vars.postToParent("H", { H: vars.getHeight() });
				return;
			}
			// Same envelope the hub sends, including ON_GROUP with { Values: [{ E, O }] },
			// so it goes into Incoming.OnData untouched.
			vars.sendToUnity("OnData", vars.serialize({ EventName: o.EventName, Data: o.Data }));
		},
		postToParent: function (eventName, data) {
			var packet = { EventName: eventName, Data: data };
			try {
				window.parent.postMessage(packet, "*");
			} catch (err) {
				console.error("Socket.jslib: postMessage to parent failed: " + err.toString());
			}
			try {
				if (window.chrome && window.chrome.webview && typeof window.chrome.webview.postMessage === "function") {
					window.chrome.webview.postMessage(packet);
				}
			} catch (err) {
				console.error("Socket.jslib: postMessage to webview failed: " + err.toString());
			}
		},
		getHeight: function () {
			var el = document.getElementById("unity-container") || document.getElementById("unity-canvas") || document.querySelector("canvas");
			if (el && typeof el.getBoundingClientRect === "function") {
				return el.getBoundingClientRect().height || 0;
			}
			return document.documentElement ? document.documentElement.scrollHeight : 0;
		},
	},

	Init: function (_objectName, _domain, _configJson) {
		vars.objectName = UTF8ToString(_objectName);
		vars.domain = UTF8ToString(_domain);
		try {
			var cfg = JSON.parse(UTF8ToString(_configJson));
			if (cfg && typeof cfg === "object") {
				if (cfg.serverTimeout > 0) vars.config.serverTimeout = cfg.serverTimeout;
				if (cfg.keepAliveInterval > 0) vars.config.keepAliveInterval = cfg.keepAliveInterval;
				if (cfg.transport > 0) vars.config.transport = cfg.transport;
				if (typeof cfg.skipNegotiation === "boolean") vars.config.skipNegotiation = cfg.skipNegotiation;
				if (Object.prototype.toString.call(cfg.retryDelays) === "[object Array]") vars.config.retryDelays = cfg.retryDelays;
			}
		} catch (err) {
			console.warn("Socket.jslib: Invalid config, using defaults: " + err.toString());
		}
		vars.instance = window.MyGameInstance || window.unityInstance || window.gameInstance;
		vars.bridge = vars.inIframe();
		if (vars.bridge) {
			vars.startBridge();
		} else {
			vars.connect();
		}
	},

	SendJS: function (method, data) {
		var _json = UTF8ToString(data);
		var _method = UTF8ToString(method);
		var _data = JSON.parse(_json);
		if (vars.bridge) {
			vars.postToParent(_method, _data);
			return;
		}
		if (!vars.connection) {
			console.warn("Socket.jslib: Not connected to server. Cannot send message.");
			return;
		}

		vars.connection.invoke("Data", { Event: _method, Data: _data }).catch(function (err) {
			console.error("Socket.jslib: Send error: " + err.toString());
		});
	},

	IsBridgeJS: function () {
		return vars.bridge ? 1 : 0;
	},

	DisposeJS: function () {
		vars.stopBridge();
		vars.preventReconnect = true;
		if (vars.connection) {
			try {
				vars.connection.stop();
			} catch (err) {
				console.warn("Socket.jslib: Failed to stop connection: " + err.toString());
			}
			vars.connection = null;
		}
	},
};

autoAddDeps(SocketLib, "$vars");
mergeInto(LibraryManager.library, SocketLib);
