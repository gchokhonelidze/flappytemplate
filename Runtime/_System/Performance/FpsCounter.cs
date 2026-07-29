using UnityEngine;

/// <summary>
/// On-screen FPS readout. Drop on any GameObject in the scene - it draws itself with
/// IMGUI, so it needs no Canvas and no wiring. Pairs with <see cref="FrameRateLimiter"/>.
/// </summary>
public class FpsCounter : MonoBehaviour
{
	public enum EScreenCorner
	{
		TopLeft,
		TopRight,
		BottomLeft,
		BottomRight
	}

	[SerializeField]
	private EScreenCorner corner = EScreenCorner.TopLeft;

	[Tooltip("Seconds between readout updates. Averages every frame in between.")]
	[SerializeField]
	private float updateInterval = 0.5f;

	[Tooltip("Font size at 1080p. Scales with screen height so it stays readable on mobile.")]
	[SerializeField]
	private int fontSize = 28;

	[SerializeField]
	private bool showFrameTime = true;

	[Tooltip("Shows the render buffer size. On WebGL this is CSS size x device pixel " +
		"ratio, so it reveals whether the template's DPR clamp took effect.")]
	[SerializeField]
	private bool showResolution = true;

	[Tooltip("Shows fastest/slowest frame in the window plus the active cap. A large " +
		"min-max spread means uneven pacing; min close to max means real CPU/GPU load.")]
	[SerializeField]
	private bool showFrameSpread = true;

	private int frames;
	private float elapsed;
	private float minDelta = float.MaxValue;
	private float maxDelta;
	private int lineCount = 1;
	private string readout = "-- fps";
	private GUIStyle style;

	private void Update()
	{
		// Unscaled, so a paused or slowed Time.timeScale does not skew the reading.
		float delta = Time.unscaledDeltaTime;
		elapsed += delta;
		frames++;
		minDelta = Mathf.Min(minDelta, delta);
		maxDelta = Mathf.Max(maxDelta, delta);

		if (elapsed < updateInterval)
			return;

		float fps = frames / elapsed;
		readout = showFrameTime
			? $"{fps:F0} fps  ({elapsed / frames * 1000f:F1} ms)"
			: $"{fps:F0} fps";

		lineCount = 1;
		if (showFrameSpread)
		{
			// The average hides the shape of the problem: a loop that alternates
			// 16 ms / 50 ms averages the same as one that sits flat at 33 ms.
			readout += $"\nmin {minDelta * 1000f:F1}  max {maxDelta * 1000f:F1}  cap {Application.targetFrameRate}";
			lineCount++;
		}

		if (showResolution)
		{
			readout += $"\n{Screen.width}x{Screen.height}";
			lineCount++;
		}

		frames = 0;
		elapsed = 0f;
		minDelta = float.MaxValue;
		maxDelta = 0f;
	}

	private void OnGUI()
	{
		if (Event.current.type != EventType.Repaint)
			return;

		int scaledFont = Mathf.Max(10, Mathf.RoundToInt(fontSize * Screen.height / 1080f));

		style ??= new GUIStyle(GUI.skin.label);
		style.fontSize = scaledFont;
		style.fontStyle = FontStyle.Bold;
		style.alignment = IsRightCorner(corner) ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
		// IMGUI clips to the rect, so keep the box generous and never wrap.
		style.wordWrap = false;

		float padding = scaledFont * 0.5f;
		float width = scaledFont * 16f;
		float height = scaledFont * 1.4f * lineCount;
		float x = IsRightCorner(corner) ? Screen.width - width - padding : padding;
		float y = IsBottomCorner(corner) ? Screen.height - height - padding : padding;
		Rect rect = new Rect(x, y, width, height);

		// Cheap drop shadow so the text stays legible over any background.
		style.normal.textColor = Color.black;
		GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), readout, style);

		style.normal.textColor = Color.white;
		GUI.Label(rect, readout, style);
	}

	private static bool IsRightCorner(EScreenCorner c) =>
		c is EScreenCorner.TopRight or EScreenCorner.BottomRight;

	private static bool IsBottomCorner(EScreenCorner c) =>
		c is EScreenCorner.BottomLeft or EScreenCorner.BottomRight;
}
