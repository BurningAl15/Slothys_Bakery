using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Batch build entry point used by CurrentlyWorkin/scripts/unity-build.sh (Android, iOS, WebGL, macOS, Windows).
/// Canonical copy: CurrentlyWorkin/scripts/unity/FatcatCliBuild.cs; install it in a game with
/// scripts/sync-cli-build.sh. Version 2 (2026-11-21): WebGL, DEV_ scenes always left out, scene list logged.
/// </summary>
public static class FatcatCliBuild
{
    public const int Version = 3;

    public static void Build()
    {
        string platform = GetArg("-fatcatPlatform", "android").ToLowerInvariant();
        string kind = GetArg("-fatcatBuildKind", DefaultKind(platform)).ToLowerInvariant();
        bool development = GetBoolArg("-fatcatDevelopment", false);
        string outputPath = GetArg("-fatcatOutput", DefaultOutputPath(platform, kind));

        BuildTarget target;
        BuildTargetGroup targetGroup;

        switch (platform)
        {
            case "android":
                target = BuildTarget.Android;
                targetGroup = BuildTargetGroup.Android;
                EditorUserBuildSettings.buildAppBundle = kind == "aab";
                outputPath = EnsureAndroidExtension(outputPath, kind);
                break;
            case "ios":
                target = BuildTarget.iOS;
                targetGroup = BuildTargetGroup.iOS;
                break;
            case "webgl":
                target = BuildTarget.WebGL;
                targetGroup = BuildTargetGroup.WebGL;
                break;
            case "macos":
                target = BuildTarget.StandaloneOSX;
                targetGroup = BuildTargetGroup.Standalone;
                ConfigureDesktopWindow();
                break;
            case "windows":
                target = BuildTarget.StandaloneWindows64;
                targetGroup = BuildTargetGroup.Standalone;
                ConfigureDesktopWindow();
                break;
            default:
                throw new ArgumentException($"Unsupported -fatcatPlatform '{platform}'. Use android, ios, webgl, macos or windows.");
        }

        // Test scenes (DEV_*) never ship, whatever Build Settings say.
        var enabled = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToList();
        var skipped = enabled.Where(IsDevScene).ToList();
        string[] scenes = enabled.Where(path => !IsDevScene(path)).ToArray();

        if (scenes.Length == 0)
            throw new InvalidOperationException("No enabled scenes found in Build Settings.");

        EnsureOutputDirectory(outputPath, platform);
        EditorUserBuildSettings.SwitchActiveBuildTarget(targetGroup, target);

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = target,
            targetGroup = targetGroup,
            options = development ? BuildOptions.Development : BuildOptions.None
        };

        Debug.Log($"[FatcatCliBuild] v{Version} Platform: {platform}");
        Debug.Log($"[FatcatCliBuild] Kind: {kind}");
        Debug.Log($"[FatcatCliBuild] Output: {outputPath}");
        Debug.Log($"[FatcatCliBuild] Development: {development}");
        Debug.Log($"[FatcatCliBuild] Version: {PlayerSettings.bundleVersion} (Android code {PlayerSettings.Android.bundleVersionCode}, iOS build {PlayerSettings.iOS.buildNumber})");
        for (int i = 0; i < scenes.Length; i++) Debug.Log($"[FatcatCliBuild] Scene {i}: {scenes[i]}");
        foreach (var path in skipped) Debug.Log($"[FatcatCliBuild] Skipped test scene: {path}");
        if (platform == "webgl")
            Debug.Log($"[FatcatCliBuild] WebGL compression: {PlayerSettings.WebGL.compressionFormat}, decompression fallback: {PlayerSettings.WebGL.decompressionFallback}");

        // Unity's IL2CPP cross-compilation is host-platform constrained. The Mac editor can emit
        // Windows players with Mono; keep the existing Standalone setting for macOS and restore it
        // after the Windows build so the two desktop targets do not silently inherit each other's backend.
        var previousStandaloneBackend = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Standalone);
        bool windowsBackendChanged = platform == "windows" && previousStandaloneBackend != ScriptingImplementation.Mono2x;
        if (windowsBackendChanged)
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
            Debug.Log("[FatcatCliBuild] Windows backend: Mono (cross-platform build from macOS).");
        }

        BuildReport report;
        try
        {
            report = BuildPipeline.BuildPlayer(options);
        }
        finally
        {
            if (windowsBackendChanged)
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, previousStandaloneBackend);
        }

        BuildSummary summary = report.summary;

        Debug.Log($"[FatcatCliBuild] Result: {summary.result}");
        Debug.Log($"[FatcatCliBuild] Size: {summary.totalSize}");
        Debug.Log($"[FatcatCliBuild] Errors: {summary.totalErrors}, warnings: {summary.totalWarnings}, time: {summary.totalTime}");

        if (summary.result != BuildResult.Succeeded)
            throw new Exception($"Build failed with result: {summary.result}");
    }

    private static bool IsDevScene(string path) =>
        Path.GetFileNameWithoutExtension(path).StartsWith("DEV_", StringComparison.OrdinalIgnoreCase);

    private static string DefaultKind(string platform) =>
        platform == "android" ? "aab" : platform == "ios" ? "xcode" : platform == "webgl" ? "web" : platform == "macos" ? "app" : "exe";

    private static string DefaultOutputPath(string platform, string kind)
    {
        string productName = PlayerSettings.productName.Replace(" ", "");
        switch (platform)
        {
            case "android":
                return $"Builds/Cli/Android/{productName}.{(kind == "apk" ? "apk" : "aab")}";
            case "webgl":
                return $"Builds/Cli/WebGL/{productName}";
            case "macos":
                return $"Builds/Cli/macOS/{productName}.app";
            case "windows":
                return $"Builds/Cli/Windows/{productName}.exe";
            default:
                return $"Builds/Cli/iOS/{productName}";
        }
    }

    private static string EnsureAndroidExtension(string outputPath, string kind)
    {
        string extension = kind == "apk" ? ".apk" : ".aab";
        if (!outputPath.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            return Path.ChangeExtension(outputPath, extension);
        return outputPath;
    }

    private static void EnsureOutputDirectory(string outputPath, string platform)
    {
        // macOS writes an .app bundle and iOS/WebGL write folders; Windows and Android write files.
        string directory = platform == "android" || platform == "windows" || platform == "macos"
            ? Path.GetDirectoryName(outputPath) : outputPath;
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
    }

    private static void ConfigureDesktopWindow()
    {
        // Game canvases use a 16:9 reference. Start desktop builds at 1600x900 and let players resize.
        PlayerSettings.defaultScreenWidth = 1600;
        PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
    }

    private static string GetArg(string name, string fallback)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == name && i + 1 < args.Length)
                return args[i + 1];
            if (args[i].StartsWith(name + "=", StringComparison.Ordinal))
                return args[i].Substring(name.Length + 1);
        }
        return fallback;
    }

    private static bool GetBoolArg(string name, bool fallback)
    {
        string value = GetArg(name, fallback ? "true" : "false");
        return value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);
    }
}
