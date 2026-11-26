#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace FatCatGameStudio.BuildAutomation
{
    /// <summary>
    /// Info.plist entries the App Store asks for on every upload. The game uses no encryption beyond the
    /// system's own (HTTPS), so declaring it here skips the export-compliance question in App Store Connect
    /// for each TestFlight build.
    /// </summary>
    public static class IosPlistPostprocessor
    {
        [PostProcessBuild(1000)]
        public static void OnPostprocessBuild(BuildTarget target, string path)
        {
            if (target != BuildTarget.iOS) return;
            string plistPath = Path.Combine(path, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
            plist.WriteToFile(plistPath);

            // com.fatcat.mobile 0.4.0 compiles its store review (StoreKit) and tracking (AppTrackingTransparency)
            // code into UnityFramework but links ATT only when the game has a tracking text. Bakery is for kids and
            // never asks for tracking, so link both here (ATT weak) or the archive fails with undefined symbols.
            string projPath = PBXProject.GetPBXProjectPath(path);
            var proj = new PBXProject();
            proj.ReadFromFile(projPath);
            string framework = proj.GetUnityFrameworkTargetGuid();
            proj.AddFrameworkToProject(framework, "StoreKit.framework", false);
            proj.AddFrameworkToProject(framework, "AppTrackingTransparency.framework", true);
            proj.WriteToFile(projPath);
        }
    }
}
#endif
