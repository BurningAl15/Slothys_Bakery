#if UNITY_ANDROID
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor.Android;
using UnityEditor;

namespace FatCatGameStudio.BuildAutomation
{
    // API 37 is distributed as android-37.0. Unity 6.6's integer-only Gradle
    // template requests android-37 instead. Keep targetSdk unchanged and select
    // the installed stable minor level explicitly (AGP 9 bundled with Unity).
    public sealed class AndroidSdk37Postprocessor : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 1000;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            if ((int)PlayerSettings.Android.targetSdkVersion != 37) return;
            string root = Directory.GetParent(path).FullName;
            foreach (string file in Directory.GetFiles(root, "build.gradle", SearchOption.AllDirectories))
            {
                string before = File.ReadAllText(file);
                string after = Regex.Replace(before,
                    @"(?m)^(\s*)compileSdk(?:Version)?\s*(?:=\s*)?37\s*$",
                    "$1compileSdk { version = release(37) { minorApiLevel = 0 } }");
                after = after.Replace("compileSdk getProperty(\"unity.compileSdkVersion\") as int",
                    "compileSdk { version = release(37) { minorApiLevel = 0 } }");
                after = after.Replace("compileSdkVersion getCompileSdk(unityLib)",
                    "compileSdk { version = release(37) { minorApiLevel = 0 } }");
                if (before != after) File.WriteAllText(file, after);
            }
        }
    }
}
#endif
