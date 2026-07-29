using UnityEngine;

/// <summary>
/// Pins the render frame rate to <see cref="TargetFrameRate"/> in the Editor and in builds.
/// Physics is unaffected - FixedUpdate keeps running at Time.fixedDeltaTime.
/// </summary>
public static class FrameRateLimiter
{
	// Do not lower this to 30 for WebGL. Measured on an iPhone 15 Pro: a 30 cap averaged
	// 22 fps, the same build at 60 averages 37. Raising a cap cannot raise throughput on
	// a device that is saturated, so the 30 cap was destroying frames, not limiting them.
	// WebGL presents only on a display refresh tick, so a frame that misses its budget
	// does not cost the overrun - it costs a whole extra 16.7 ms tick. Capping anywhere
	// near the current frame cost therefore loses frames rather than saving them.
	private const int TargetFrameRate = 60;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	public static void Apply()
	{
		// targetFrameRate is ignored while vSync is on, and the quality levels in
		// this project do not agree on vSyncCount, so clear it first.
		QualitySettings.vSyncCount = 0;
		Application.targetFrameRate = TargetFrameRate;
	}
}
