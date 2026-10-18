using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class BuildVersionProcessor : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    private const string INITIAL_VERSION = "1.0.1";
    private const string CONFIG_FILE_PATH = "Assets/Editor/BuildConfig.json";
    private const string LOCAL_CONFIG_FILE_PATH = "Assets/Editor/BuildConfig.local.json";
    
    [SerializeField] private bool isMajorUpdate = false;
    
    public void OnPreprocessBuild(BuildReport report)
    {
        Debug.Log("[BuildReport] === Build Version Processor Started ===");
        
        if (ShouldSkipVersionBump())
        {
            Debug.Log("[BuildReport] Version bump skipped by FATCAT_SKIP_VERSION_BUMP.");
        }
        else
        {
            // 1. Update version string
            string currentVersion = FindCurrentVersion();
            UpdateVersion(currentVersion, isMajorUpdate);
            
            // 2. Build numbers are platform-specific; never alter Android's code for an iOS export.
            if (report.summary.platform == BuildTarget.Android)
                UpdateBundleVersionCode();
            else if (report.summary.platform == BuildTarget.iOS)
                UpdateIosBuildNumber();
        }
        
        // 3. Configure Android signing (if Android build)
        if (report.summary.platform == BuildTarget.Android)
        {
            ConfigureAndroidSigning();
        }
        
        // 4. Validate build settings
        ValidateBuildSettings(report.summary.platform);
        
        Debug.Log("[BuildReport] === Build Version Processor Completed ===");
    }

    private string FindCurrentVersion()
    {
        string currentVersion = PlayerSettings.bundleVersion;
        int open = currentVersion.IndexOf('[');
        int close = currentVersion.IndexOf(']', open + 1);
        if (open >= 0 && close > open)
            currentVersion = currentVersion.Substring(open + 1, close - open - 1);

        string[] parts = currentVersion.Split('.');
        if (parts.Length < 3 ||
            !int.TryParse(parts[0], out _) ||
            !int.TryParse(parts[1], out _) ||
            !int.TryParse(parts[2], out _))
            return INITIAL_VERSION;
        return string.Join(".", parts, 0, 3);
    }

    private bool ShouldSkipVersionBump()
    {
        string value = Environment.GetEnvironmentVariable("FATCAT_SKIP_VERSION_BUMP");
        return value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    private void UpdateVersion(string version, bool isMajorUpdate = false)
    {
        string[] versionParts = version.Split('.');
        
        if (isMajorUpdate)
        {
            versionParts[1] = (int.Parse(versionParts[1]) + 1).ToString();
            versionParts[2] = "0";
        }
        else
        {
            versionParts[2] = (int.Parse(versionParts[2]) + 1).ToString();
        }
        
        string newVersion = string.Join(".", versionParts);
        PlayerSettings.bundleVersion = newVersion;
        
        Debug.Log($"[BuildReport] ✅ Version updated: {PlayerSettings.bundleVersion}");
    }
    
    private void UpdateBundleVersionCode()
    {
        // Auto-increment Android bundle version code
        int currentBundleCode = PlayerSettings.Android.bundleVersionCode;
        PlayerSettings.Android.bundleVersionCode = currentBundleCode + 1;
        
        Debug.Log($"[BuildReport] ✅ Bundle Version Code updated: {currentBundleCode} → {PlayerSettings.Android.bundleVersionCode}");
    }

    private void UpdateIosBuildNumber()
    {
        int currentBuildNumber = int.TryParse(PlayerSettings.iOS.buildNumber, out int parsed) ? parsed : 0;
        PlayerSettings.iOS.buildNumber = (currentBuildNumber + 1).ToString();
        Debug.Log($"[BuildReport] iOS build number updated: {currentBuildNumber} -> {PlayerSettings.iOS.buildNumber}");
    }
    
    private void ConfigureAndroidSigning()
    {
        BuildConfig config = LoadBuildConfig(out string configPath);
        
        if (config == null)
        {
            Debug.LogWarning("[BuildReport] ⚠️ BuildConfig.local.json and BuildConfig.json not found. Creating default legacy config...");
            CreateDefaultBuildConfig();
            config = LoadBuildConfig(out configPath);
        }

        if (config == null)
        {
            Debug.LogError("[BuildReport] ❌ Android signing config could not be loaded.");
            return;
        }

        Debug.Log($"[BuildReport] 🔐 Using Android signing config: {configPath}");
        
        // Validate keystore file exists
        if (!string.IsNullOrEmpty(config.keystorePath))
        {
            string fullPath = Path.GetFullPath(config.keystorePath);
            
            if (File.Exists(fullPath))
            {
                PlayerSettings.Android.keystoreName = fullPath;
                Debug.Log($"[BuildReport] ✅ Keystore path set: {fullPath}");
            }
            else
            {
                Debug.LogError($"[BuildReport] ❌ Keystore file not found at: {fullPath}");
                Debug.LogError("[BuildReport] Build will fail without valid keystore!");
            }
        }
        
        // Set passwords from config (or environment variables)
        PlayerSettings.Android.keystorePass = GetKeystorePassword(config);
        PlayerSettings.Android.keyaliasName = config.keyAlias;
        PlayerSettings.Android.keyaliasPass = GetAliasPassword(config);
        
        Debug.Log($"[BuildReport] ✅ Keystore configured:");
        Debug.Log($"[BuildReport]    - Alias: {(string.IsNullOrEmpty(config.keyAlias) ? "❌ NOT SET" : "✅ SET")}");
        Debug.Log($"[BuildReport]    - Keystore Pass: {(string.IsNullOrEmpty(PlayerSettings.Android.keystorePass) ? "❌ NOT SET" : "✅ SET")}");
        Debug.Log($"[BuildReport]    - Alias Pass: {(string.IsNullOrEmpty(PlayerSettings.Android.keyaliasPass) ? "❌ NOT SET" : "✅ SET")}");
    }
    
    private string GetKeystorePassword(BuildConfig config)
    {
        // Priority 1: Environment variable
        string envPassword = Environment.GetEnvironmentVariable("UNITY_KEYSTORE_PASS");
        if (!string.IsNullOrEmpty(envPassword))
        {
            Debug.Log("[BuildReport] 🔐 Using keystore password from environment variable");
            return envPassword;
        }
        
        // Priority 2: Config file
        if (!string.IsNullOrEmpty(config.keystorePassword))
        {
            Debug.Log("[BuildReport] 🔐 Using keystore password from config file");
            return config.keystorePassword;
        }
        
        Debug.LogWarning("[BuildReport] ⚠️ Keystore password not configured!");
        return "";
    }
    
    private string GetAliasPassword(BuildConfig config)
    {
        // Priority 1: Environment variable
        string envPassword = Environment.GetEnvironmentVariable("UNITY_ALIAS_PASS");
        if (!string.IsNullOrEmpty(envPassword))
        {
            Debug.Log("[BuildReport] 🔐 Using alias password from environment variable");
            return envPassword;
        }
        
        // Priority 2: Config file
        if (!string.IsNullOrEmpty(config.aliasPassword))
        {
            Debug.Log("[BuildReport] 🔐 Using alias password from config file");
            return config.aliasPassword;
        }
        
        Debug.LogWarning("[BuildReport] ⚠️ Alias password not configured!");
        return "";
    }
    
    private void ValidateBuildSettings(BuildTarget target)
    {
        Debug.Log($"[BuildReport] 📋 Validating build settings for {target}...");
        
        if (target == BuildTarget.Android)
        {
            // Validate company name
            if (string.IsNullOrEmpty(PlayerSettings.companyName))
            {
                Debug.LogWarning("[BuildReport] ⚠️ Company name is empty!");
            }
            
            // Validate product name
            if (string.IsNullOrEmpty(PlayerSettings.productName))
            {
                Debug.LogWarning("[BuildReport] ⚠️ Product name is empty!");
            }
            
            // Validate package name
            if (string.IsNullOrEmpty(PlayerSettings.applicationIdentifier))
            {
                Debug.LogError("[BuildReport] ❌ Package name (Bundle Identifier) is empty!");
            }
            
            // Validate minimum API level
            if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel24)
            {
                Debug.LogWarning($"[BuildReport] ⚠️ Minimum API Level is {PlayerSettings.Android.minSdkVersion}. Consider using API 24+ for better compatibility.");
            }
            
            Debug.Log("[BuildReport] ✅ Build settings validation complete");
        }
    }
    
    private BuildConfig LoadBuildConfig(out string configPath)
    {
        if (File.Exists(LOCAL_CONFIG_FILE_PATH))
        {
            configPath = LOCAL_CONFIG_FILE_PATH;
            string json = File.ReadAllText(LOCAL_CONFIG_FILE_PATH);
            return JsonUtility.FromJson<BuildConfig>(json);
        }

        if (File.Exists(CONFIG_FILE_PATH))
        {
            configPath = CONFIG_FILE_PATH;
            string json = File.ReadAllText(CONFIG_FILE_PATH);
            return JsonUtility.FromJson<BuildConfig>(json);
        }

        configPath = "";
        return null;
    }
    
    private void CreateDefaultBuildConfig()
    {
        BuildConfig defaultConfig = new BuildConfig
        {
            keystorePath = "path/to/your/keystore.keystore",
            keystorePassword = "YOUR_KEYSTORE_PASSWORD",
            keyAlias = "YOUR_KEY_ALIAS",
            aliasPassword = "YOUR_ALIAS_PASSWORD"
        };
        
        string json = JsonUtility.ToJson(defaultConfig, true);
        File.WriteAllText(CONFIG_FILE_PATH, json);
        AssetDatabase.Refresh();
        
        Debug.LogWarning($"[BuildReport] ⚠️ Default BuildConfig.json created at: {CONFIG_FILE_PATH}");
        Debug.LogWarning("[BuildReport] ⚠️ Please configure your keystore settings in this file!");
    }
    
    [Serializable]
    public class BuildConfig
    {
        public string keystorePath;
        public string keystorePassword;
        public string keyAlias;
        public string aliasPassword;
    }
}
