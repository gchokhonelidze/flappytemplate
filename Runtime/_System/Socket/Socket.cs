using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#else
using System.Collections.Concurrent;
using UnityWebGLSignalR;
#endif
namespace FlappyTemplate
{
	/// <summary>
	/// Client-side mirror of the server's AddHubOptions&lt;MessageHub&gt; configuration.
	/// Server pings every KeepAliveInterval; the peer considers the connection dead after
	/// its timeout with no message. Keep each timeout >= 2x the other side's keep-alive so a
	/// single missed/late ping (GC pause, mobile background, jitter) does not force a reconnect.
	/// </summary>
	[System.Serializable]
	public class SocketConfig
	{
		[Tooltip("Seconds. Mirrors server HandshakeTimeout. Editor/standalone only — the browser client has no handshake timeout.")]
		public float HandshakeTimeout = 15f;

		[Tooltip("Seconds. How often this client pings. Keep <= half the server's ClientTimeoutInterval (30s).")]
		public float KeepAliveInterval = 15f;

		[Tooltip("Seconds with no message before this client treats the server as gone. Keep >= 2x the server's KeepAliveInterval (15s).")]
		public float ServerTimeout = 30f;

		[Tooltip("Bytes. Mirrors server MaximumReceiveMessageSize. Larger payloads are dropped locally instead of killing the connection.")]
		public int MaximumSendMessageSize = 1024 * 32;

		[Tooltip(
			"WebSockets is the only transport, so the negotiate round trip is skipped by default. "
				+ "Turn this off if the server ever needs negotiate (sticky sessions, connection tokens). Note: ConnectionId is null when skipped."
		)]
		public bool SkipNegotiation = true;

		[Tooltip(
			"Milliseconds to wait before each reconnect attempt. The length of the list is the attempt cap — "
				+ "once it is exhausted the client gives up instead of retrying forever. Empty means never reconnect."
		)]
		public int[] RetryDelays = { 500, 2000, 5000, 10000 };

		public int HandshakeTimeoutMs => Mathf.RoundToInt(HandshakeTimeout * 1000f);
		public int KeepAliveIntervalMs => Mathf.RoundToInt(KeepAliveInterval * 1000f);
		public int ServerTimeoutMs => Mathf.RoundToInt(ServerTimeout * 1000f);

		private string RetryDelaysJson => "[" + string.Join(",", RetryDelays ?? System.Array.Empty<int>()) + "]";

		/// <summary>Only the values the browser SignalR client can actually apply. transport 1 = WebSockets.</summary>
		public string ToJson() =>
			$"{{\"serverTimeout\":{ServerTimeoutMs},\"keepAliveInterval\":{KeepAliveIntervalMs},"
			+ $"\"transport\":1,\"skipNegotiation\":{(SkipNegotiation ? "true" : "false")},"
			+ $"\"retryDelays\":{RetryDelaysJson}}}";
	}

	[RequireComponent(typeof(Incoming))]
	public class Socket : MonoBehaviour
	{
		public static Socket Inst { get; private set; } = null!;
		private Emitter Emitter;

		[SerializeField]
		private string Token;

		[SerializeField]
		private string Domain = "https://api-staging.flappy.live";

		[SerializeField]
		private SocketConfig Config = new();

		[SerializeField]
		[Tooltip(
			"Seconds between BAL polls. Only runs when we own the SignalR connection — inside an iframe "
				+ "the parent page already polls on its own connection and pushes ON_BALANCE down to us. 0 disables."
		)]
		private float BalancePollInterval = 60f;
		private Incoming incoming;

		/// <summary>
		/// True when the WebGL build runs inside an iframe. There is no SignalR connection in that
		/// case: the parent page owns the socket and we exchange the same envelopes over postMessage.
		/// Always false in the editor and standalone builds.
		/// </summary>
		public bool IsBridge { get; private set; }

#if UNITY_WEBGL && !UNITY_EDITOR
		[DllImport("__Internal")]
		private static extern void Init(string objectName, string domain, string configJson);

		[DllImport("__Internal")]
		private static extern void SendJS(string method, string json);

		[DllImport("__Internal")]
		private static extern int IsBridgeJS();

		[DllImport("__Internal")]
		private static extern void DisposeJS();
#else
		SignalR signalR;
		readonly ConcurrentQueue<string> _pendingMessages = new();
		bool _isConnected;
		volatile bool _serverDisconnect;
#endif

		void Awake()
		{
			incoming = GetComponent<Incoming>();
			Inst = this;
			Emitter = new Emitter();
			Application.runInBackground = true;
		}

		void Start()
		{
#if UNITY_WEBGL && !UNITY_EDITOR
			Init(gameObject.name, Domain.TrimEnd('/'), Config.ToJson());
			IsBridge = IsBridgeJS() != 0;
			Debug.Log(IsBridge ? "Socket: iframe detected, using the postMessage bridge." : "Socket: standalone page, using SignalR.");
			StartBalancePoll();
			return;
#else
			var url = $"{Domain.TrimEnd('/')}/sock?token={Token}";
			Debug.Log($"SignalR init url: {url}");
			signalR = new SignalR();
			signalR.ConnectionStarted += OnConnectionStarted;
			signalR.ConnectionClosed += OnConnectionClosed;
			signalR.Init(
				url,
				new SignalROptions
				{
					HandshakeTimeout = Config.HandshakeTimeoutMs,
					KeepAliveInterval = Config.KeepAliveIntervalMs,
					ServerTimeout = Config.ServerTimeoutMs,
					Transport = TransportType.WebSockets,
					SkipNegotiation = Config.SkipNegotiation,
					RetryDelays = Config.RetryDelays,
				}
			);
			signalR.On("OnData", (object payload) => _pendingMessages.Enqueue(payload.ToString()));
			// Server-initiated kick. Stopping has to happen on the main thread, so just raise the flag.
			signalR.On(
				"Disconnect",
				(object reason) =>
				{
					Debug.LogWarning($"SignalR disconnected by server: {reason}");
					_serverDisconnect = true;
				}
			);
			signalR.Connect();
			StartBalancePoll();
			return;
#endif
		}

		/// <summary>
		/// Mirrors the aggregator front's 60s BAL interval. That interval lives on whichever side owns
		/// the hub connection, so it must not run in bridge mode or the balance gets polled twice.
		/// </summary>
		void StartBalancePoll()
		{
			if (IsBridge || BalancePollInterval <= 0f)
				return;
			InvokeRepeating(nameof(PollBalance), BalancePollInterval, BalancePollInterval);
		}

		void PollBalance() => Emitter.OnBalanceCall();

		public void Send(string method, object data)
		{
			var json = Utils.Serialize(data);

			// The server's MaximumReceiveMessageSize applies to the whole hub frame, which is a
			// little larger than this payload — so this catches the obvious offenders early rather
			// than being exact. Exceeding it server-side tears down the connection.
			// The bridge posts to the parent page instead of the hub, so the limit does not apply.
			var size = System.Text.Encoding.UTF8.GetByteCount(json);
			if (!IsBridge && Config.MaximumSendMessageSize > 0 && size > Config.MaximumSendMessageSize)
			{
				Debug.LogError($"Message '{method}' is {size} bytes, over the {Config.MaximumSendMessageSize} byte limit. Not sending.");
				return;
			}

			Debug.Log($"Sending to js json: {json}");
#if UNITY_WEBGL && !UNITY_EDITOR
			SendJS(method, json);
#else

			Debug.Log($"Sending message: {method} with json: {json}");
			if (signalR == null)
			{
				Debug.LogWarning($"SignalR not initialized yet. ");
				return;
			}

			if (!_isConnected && !signalR.IsConnected)
			{
				Debug.LogWarning($"SignalR not connected yet. ");
				return;
			}

			signalR.Invoke("Data", new { Event = method, Data = data });
#endif
		}

		void Update()
		{
#if !UNITY_WEBGL || UNITY_EDITOR
			while (_pendingMessages.TryDequeue(out var msg))
				incoming.OnData(msg);

			if (_serverDisconnect)
			{
				// Stop() aborts any in-flight reconnect attempt, so the ladder is not walked after a
				// deliberate server-side kick — the equivalent of the front end's preventReconnect flag.
				_serverDisconnect = false;
				_isConnected = false;
				CancelInvoke(nameof(PollBalance));
				signalR?.Stop();
			}
#endif
		}

		public void OnData(string msg) => incoming.OnData(msg);

		/// <summary>
		/// Called from JS when the browser connection is gone for good — either the server sent
		/// "Disconnect" or the retry ladder ran out. Nothing left to poll at that point.
		/// </summary>
		public void OnSocketDisconnected(string reason)
		{
			Debug.LogWarning($"Socket closed: {reason}");
			CancelInvoke(nameof(PollBalance));
		}

		void OnDestroy()
		{
#if UNITY_WEBGL && !UNITY_EDITOR
			// Drops the window "message" listener so a reloaded scene does not end up with two
			// bridges feeding the same events into Incoming.
			DisposeJS();
#else
			if (signalR != null)
			{
				signalR.ConnectionStarted -= OnConnectionStarted;
				signalR.ConnectionClosed -= OnConnectionClosed;
				signalR.Stop();
				signalR.Dispose();
			}
#endif
		}

#if !UNITY_WEBGL || UNITY_EDITOR
		void OnConnectionStarted(object sender, ConnectionEventArgs args)
		{
			_isConnected = true;
			Debug.Log($"SignalR connected. ConnectionId: {args.ConnectionId}");
		}

		void OnConnectionClosed(object sender, ConnectionEventArgs args)
		{
			_isConnected = false;
			Debug.LogWarning($"SignalR disconnected. ConnectionId: {args.ConnectionId}");
		}

#endif
	}
}
