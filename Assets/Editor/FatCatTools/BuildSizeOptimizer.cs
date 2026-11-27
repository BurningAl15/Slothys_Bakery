using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace FatCat.Slothy.Tools
{
    /// <summary>
    /// Compresses uncompressed textures (ASTC/ETC2 on Android) and sets IL2CPP code generation
    /// to OptimizeSize so Android AAB/APK sizes stay compact without losing visual crispness.
    /// Batch: -executeMethod FatCat.Slothy.Tools.BuildSizeOptimizer.RunCi
    /// </summary>
    public static class BuildSizeOptimizer
    {
        [MenuItem("FatCat/Slothy Bakery/Optimize Build Size")]
        public static void Run()
        {
            int changed = 0;
            foreach (var path in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath).Distinct())
            {
                if (path.StartsWith("Assets/INART GAMES/Art/Notifications/") ||
                    path.StartsWith("Assets/Art/Notifications/") ||
                    path.StartsWith("Assets/Resources/Share/"))
                    continue;
                if (path.Contains("/Editor/") || !(path.EndsWith(".png") || path.EndsWith(".jpg") || path.EndsWith(".jpeg") || path.EndsWith(".psd")))
                    continue;
                if (!(AssetImporter.GetAtPath(path) is TextureImporter imp)) continue;
                if (imp.textureType == TextureImporterType.NormalMap || imp.textureType == TextureImporterType.Lightmap) continue;

                bool dirty = false;
                if (imp.textureCompression == TextureImporterCompression.Uncompressed)
                {
                    imp.textureCompression = TextureImporterCompression.CompressedHQ;
                    dirty = true;
                }

                var android = imp.GetPlatformTextureSettings("Android");
                if (android.overridden && (android.format == TextureImporterFormat.RGBA32 || android.format == TextureImporterFormat.RGB24 || android.format == TextureImporterFormat.ARGB32))
                {
                    android.overridden = false;
                    imp.SetPlatformTextureSettings(android);
                    dirty = true;
                }

                if (dirty) { imp.SaveAndReimport(); changed++; }
            }

            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.Android, Il2CppCodeGeneration.OptimizeSize);
            AssetDatabase.SaveAssets();
            Debug.Log($"[BuildSizeOptimizer] Bakery textures compressed: {changed}; IL2CPP Android: OptimizeSize");
        }

        public static void RunCi()
        {
            try { Run(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
