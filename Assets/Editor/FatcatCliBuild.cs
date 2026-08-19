using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class FatcatCliBuild
{
    public static void Build()
    {
        string platform = GetArg("-fatcatPlatform", "android").ToLowerInvariant();
        string kind = GetArg("-fatcatBuildKind", platform == "android" ? "aab" : "xcode").ToLowerInvariant();
        bool development = GetBoolArg("-fatcatDevelopment", false);
        string outputPath = GetArg("-fatcatOutput", DefaultOutputPath(platform, kind));

        BuildTarget target;
        BuildTargetGroup targetGroup;

        if (platform == "android")
        {
            target = BuildTarget.Android;
            targetGroup = BuildTargetGroup.Android;
            EditorUserBuildSettings.buildAppBundle = kind == "aab";
            outputPath = EnsureAndroidExtension(outputPath, kind);
        }
        else if (platform == "ios")
        {
            target = BuildTarget.iOS;
            targetGroup = BuildTargetGroup.iOS;
        }
        else
        {
            throw new ArgumentException($"Unsupported -fatcatPlatform '{platform}'. Use android or ios.");
        }

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            throw new InvalidOperationException("No enabled scenes found in Build Settings.");
        }

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

        Debug.Log($"[FatcatCliBuild] Platform: {platform}");
        Debug.Log($"[FatcatCliBuild] Kind: {kind}");
        Debug.Log($"[FatcatCliBuild] Output: {outputPath}");
        Debug.Log($"[FatcatCliBuild] Development: {development}");

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        Debug.Log($"[FatcatCliBuild] Result: {summary.result}");
        Debug.Log($"[FatcatCliBuild] Size: {summary.totalSize}");

        if (summary.result != BuildResult.Succeeded)
        {
            throw new Exception($"Build failed with result: {summary.result}");
        }
    }

    private static string DefaultOutputPath(string platform, string kind)
    {
        string productName = PlayerSettings.productName.Replace(" ", "");
        if (platform == "android")
        {
            string extension = kind == "apk" ? "apk" : "aab";
            return $"Builds/Cli/Android/{productName}.{extension}";
        }

        return $"Builds/Cli/iOS/{productName}";
    }

    private static string EnsureAndroidExtension(string outputPath, string kind)
    {
        string extension = kind == "apk" ? ".apk" : ".aab";
        if (!outputPath.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
        {
            return Path.ChangeExtension(outputPath, extension);
        }

        return outputPath;
    }

    private static void EnsureOutputDirectory(string outputPath, string platform)
    {
        string directory = platform == "ios" ? outputPath : Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static string GetArg(string name, string fallback)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == name && i + 1 < args.Length)
            {
                return args[i + 1];
            }

            if (args[i].StartsWith(name + "=", StringComparison.Ordinal))
            {
                return args[i].Substring(name.Length + 1);
            }
        }

        return fallback;
    }

    private static bool GetBoolArg(string name, bool fallback)
    {
        string value = GetArg(name, fallback ? "true" : "false");
        return value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);
    }
}
