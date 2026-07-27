using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace FlappyTemplate.Editor
{
	/// <summary>
	/// Keeps the FlappyBet WebGL template installed under Assets/WebGLTemplates and selected in the player settings.
	/// The sync runs on editor load (and on demand from the menu) because Unity resolves the WebGL template from the
	/// asset database before the build starts - doing it from a build callback is too late.
	/// </summary>
	public class WebGLTemplatePreProcessor : IPreprocessBuildWithReport
	{
		private const string TemplateName = "FlappyBet";

		private const string TemplateSetting = "PROJECT:" + TemplateName;

		private const string PackageName = "com.biznetx.flappytemplate";

		private const string DestinationFolder = "Assets/WebGLTemplates/" + TemplateName;

		public int callbackOrder => 0;

		/// <summary>
		/// Absolute path to the package root, wherever the package happens to live: embedded under Packages/, installed
		/// from a git url or registry into Library/PackageCache, or linked with a file: dependency. Never hard-code the
		/// folder - the only stable anchor is the assembly this code was compiled into.
		/// </summary>
		private static string PackageRoot
		{
			get
			{
				var assembly = typeof(WebGLTemplatePreProcessor).Assembly;

				var package = PackageInfo.FindForAssembly(assembly);

				if (package != null)
				{
					return package.resolvedPath;
				}

				// Not registered with the Package Manager - the folder is dropped straight into Assets/. Walk up from
				// the assembly definition that produced this code until the folder holding package.json turns up.
				var assemblyName = assembly.GetName().Name;

				foreach (var guid in AssetDatabase.FindAssets($"{assemblyName} t:AssemblyDefinitionAsset"))
				{
					var assetPath = AssetDatabase.GUIDToAssetPath(guid);

					if (Path.GetFileNameWithoutExtension(assetPath) != assemblyName)
					{
						continue;
					}

					for (var folder = Directory.GetParent(Path.GetFullPath(assetPath)); folder != null; folder = folder.Parent)
					{
						if (File.Exists(Path.Combine(folder.FullName, "package.json")))
						{
							return folder.FullName;
						}
					}
				}

				return Path.GetFullPath("Packages/" + PackageName);
			}
		}

		private static string SourceFolder => Path.Combine(PackageRoot, "WebGLTemplates", TemplateName);

		[InitializeOnLoadMethod]
		private static void SyncOnEditorLoad()
		{
			SyncTemplate();
		}

		[MenuItem("Tools/FlappyBet/Sync WebGL Template")]
		public static void SyncTemplate()
		{
			var sourceFolder = SourceFolder;

			if (!Directory.Exists(sourceFolder))
			{
				Debug.LogError($"WebGL template source folder not found at {sourceFolder}.");
				return;
			}

			var destinationFolder = Path.GetFullPath(DestinationFolder);

			if (CopyTemplate(sourceFolder, destinationFolder))
			{
				Debug.Log($"Copied WebGL template from {sourceFolder} to {destinationFolder}.");

				AssetDatabase.Refresh();
			}

			if (PlayerSettings.WebGL.template != TemplateSetting)
			{
				Debug.Log($"Setting webgl template, old was = {PlayerSettings.WebGL.template}");

				PlayerSettings.WebGL.template = TemplateSetting;

				AssetDatabase.SaveAssets();
			}
		}

		public void OnPreprocessBuild(BuildReport report)
		{
			if (report.summary.platform != BuildTarget.WebGL)
			{
				return;
			}

			SyncTemplate();

			if (!Directory.Exists(Path.GetFullPath(DestinationFolder)) || PlayerSettings.WebGL.template != TemplateSetting)
			{
				throw new BuildFailedException(
					$"WebGL template '{TemplateName}' is not installed. Run Tools/FlappyBet/Sync WebGL Template and build again.");
			}
		}

		/// <summary>
		/// Mirrors the template folder, skipping .meta files so the copies do not clash with the source asset GUIDs.
		/// Returns true when anything was written.
		/// </summary>
		private static bool CopyTemplate(string sourceFolder, string destinationFolder)
		{
			var copied = false;

			Directory.CreateDirectory(destinationFolder);

			foreach (var sourceFile in Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories))
			{
				if (sourceFile.EndsWith(".meta"))
				{
					continue;
				}

				var destinationFile = Path.Combine(destinationFolder, GetRelativePath(sourceFolder, sourceFile));

				if (File.Exists(destinationFile) && File.GetLastWriteTimeUtc(destinationFile) >= File.GetLastWriteTimeUtc(sourceFile))
				{
					continue;
				}

				Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));

				File.Copy(sourceFile, destinationFile, true);

				copied = true;
			}

			return copied;
		}

		private static string GetRelativePath(string folder, string file)
		{
			return file.Substring(folder.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		}
	}
}
