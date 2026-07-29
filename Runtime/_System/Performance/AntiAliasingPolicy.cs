using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Enables MSAA on desktop browsers and leaves it off on touch devices.
/// Phones already anti-alias through sheer pixel density (devicePixelRatio 3), so MSAA
/// there costs bandwidth for no visible gain; desktop runs at 1x and needs the help.
/// </summary>
public static class AntiAliasingPolicy
{
	private const int DesktopMsaaSamples = 4;
	private const int MobileMsaaSamples = 1;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	public static void Apply()
	{
		// Writing to the URP asset in the Editor persists straight into the .asset file
		// on disk, so this only ever runs in a player. The Editor keeps whatever the
		// asset is serialized with.
#if UNITY_WEBGL && !UNITY_EDITOR
		if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset urp)
			return;

		urp.msaaSampleCount = IsMobileBrowserJS() == 1 ? MobileMsaaSamples : DesktopMsaaSamples;
#endif
	}

#if UNITY_WEBGL && !UNITY_EDITOR
	[DllImport("__Internal")]
	private static extern int IsMobileBrowserJS();
#endif
}
