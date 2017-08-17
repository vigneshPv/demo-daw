using UnityEngine;
using UnityEditor;

public class A1050055001aMenu : ScriptableObject
{
		private const string VERSION = "1.5";

		[MenuItem ("1050055001a/Export as Unity package")]
		static void Export ()
		{
				var path = EditorUtility.SaveFilePanel (
				"Export project as unity package...",
				"",
				"template-" + VERSION,
				"unitypackage");
				if (path.Length != 0) {
						AssetDatabase.ExportPackage ("Assets", path, ExportPackageOptions.Interactive | ExportPackageOptions.Recurse | ExportPackageOptions.IncludeLibraryAssets | ExportPackageOptions.IncludeDependencies);
				}
		}

		[MenuItem ("1050055001a/Dashboard")]
		static void VisitDashboard ()
		{
				Application.OpenURL ("http://plus.xloudia.net");
		}

		[MenuItem ("1050055001a/Support")]
		static void VisitSupport ()
		{
				Application.OpenURL ("http://support.xloudia.com");
		}

		[MenuItem ("1050055001a/About...")]
		static void About ()
		{
				EditorUtility.DisplayDialog ("About...",
                                    "Xloudia Unity template version " + VERSION + ".\nhttp://www.xloudia.com", "Ok", null);
		}
}
