using System;
using System.IO;
using Unity.Notifications;
using UnityEditor;
using UnityEngine;

namespace FatCat.Slothy.Tools
{
    /// <summary>Creates and registers Bakery-specific Android reminder icons from the game's own logo.</summary>
    public static class BakeryReminderIconInstaller
    {
        const string Source = "Assets/INART GAMES/Art/Textures/Slothy'sBakeryIcon.png";
        const string OutputDir = "Assets/Art/Notifications";
        const string SmallPath = OutputDir + "/bakery_notif_small.png";
        const string LargePath = OutputDir + "/bakery_notif_large.png";
        const string SmallId = "bakery_notif_small";
        const string LargeId = "bakery_notif_large";

        [MenuItem("FatCat/Slothy Bakery/Install Reminder Icons")]
        public static void Run()
        {
            var importer = AssetImporter.GetAtPath(Source) as TextureImporter;
            if (importer == null) throw new FileNotFoundException("Bakery logo not found or not a texture importer.", Source);

            bool wasReadable = importer.isReadable;
            if (!wasReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }

            try
            {
                var logo = AssetDatabase.LoadAssetAtPath<Texture2D>(Source);
                if (logo == null) throw new InvalidOperationException("Could not load Bakery logo pixels.");
                Directory.CreateDirectory(OutputDir);
                WriteIcon(logo, 96, SmallPath, silhouette: true);
                WriteIcon(logo, 256, LargePath, silhouette: false);
            }
            finally
            {
                if (!wasReadable)
                {
                    importer.isReadable = false;
                    importer.SaveAndReimport();
                }
            }

            var small = ImportIcon(SmallPath);
            var large = ImportIcon(LargePath);
            NotificationSettings.AndroidSettings.RemoveDrawableResource(SmallId);
            NotificationSettings.AndroidSettings.RemoveDrawableResource(LargeId);
            NotificationSettings.AndroidSettings.AddDrawableResource(SmallId, small, NotificationIconType.Small);
            NotificationSettings.AndroidSettings.AddDrawableResource(LargeId, large, NotificationIconType.Large);
            AssetDatabase.SaveAssets();
            Debug.Log("[BakeryReminderIconInstaller] Registered Bakery Android reminder icons.");
        }

        static void WriteIcon(Texture2D source, int size, string path, bool silhouette)
        {
            var output = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float scale = Mathf.Min(size / (float)source.width, size / (float)source.height);
            float drawnWidth = source.width * scale;
            float drawnHeight = source.height * scale;
            float left = (size - drawnWidth) * 0.5f;
            float bottom = (size - drawnHeight) * 0.5f;

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f - left) / drawnWidth;
                float v = (y + 0.5f - bottom) / drawnHeight;
                Color pixel = u < 0 || u > 1 || v < 0 || v > 1
                    ? Color.clear
                    : source.GetPixelBilinear(u, v);
                output.SetPixel(x, y, silhouette ? new Color(1, 1, 1, pixel.a) : pixel);
            }

            output.Apply();
            File.WriteAllBytes(path, output.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(output);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        static Texture2D ImportIcon(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Generated notification icon did not import: " + path);
            importer.textureType = TextureImporterType.Default;
            importer.isReadable = true;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        public static void RunCi()
        {
            try { Run(); EditorApplication.Exit(0); }
            catch (System.Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
