using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Fatcat.Slothy.Bakery.Tests
{
    /// <summary>
    /// Isolates PlayerPrefs across EditMode and EnterPlayMode/ExitPlayMode domain reloads
    /// so QA test runs never read or overwrite real local editor/player saves.
    /// </summary>
    internal static class BakeryTestPrefsScope
    {
        [Serializable]
        private sealed class PrefEntry
        {
            public string key;
            public int kind; // 0 = missing, 1 = string, 2 = int, 3 = float
            public string strValue;
            public int intValue;
            public float floatValue;
        }

        [Serializable]
        private sealed class PrefSnapshot
        {
            public List<PrefEntry> entries = new();
        }

        private static readonly (string key, int type)[] KnownKeys =
        {
            ("slothys_bakery_progress", 1),
            ("cfg_audio_music", 3),
            ("cfg_audio_sfx", 3),
            ("cfg_gameplay_vib", 2),
            ("cfg_notif_enabled", 2),
            ("cfg_app_language", 1),
            ("selected-locale", 1),
            ("bakery.language_chosen", 2),
        };

        private static string BackupPath =>
            Path.Combine(Path.GetTempPath(), "slothys_bakery_p0_prefs_backup.json");

        public static void BeginIsolatedProfile()
        {
            if (!File.Exists(BackupPath))
            {
                var snap = new PrefSnapshot();
                foreach (var (key, type) in KnownKeys)
                {
                    var entry = new PrefEntry { key = key, kind = 0 };
                    if (PlayerPrefs.HasKey(key))
                    {
                        entry.kind = type;
                        if (type == 1) entry.strValue = PlayerPrefs.GetString(key, "");
                        else if (type == 2) entry.intValue = PlayerPrefs.GetInt(key, 0);
                        else if (type == 3) entry.floatValue = PlayerPrefs.GetFloat(key, 0f);
                    }
                    snap.entries.Add(entry);
                }
                File.WriteAllText(BackupPath, JsonUtility.ToJson(snap));
            }

            ClearProfileKeys();
        }

        public static void ClearProfileKeys()
        {
            foreach (var (key, _) in KnownKeys)
                PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }

        public static void RestoreProfile()
        {
            if (!File.Exists(BackupPath)) return;
            try
            {
                var snap = JsonUtility.FromJson<PrefSnapshot>(File.ReadAllText(BackupPath));
                if (snap?.entries != null)
                {
                    foreach (var entry in snap.entries)
                    {
                        if (entry.kind == 0) PlayerPrefs.DeleteKey(entry.key);
                        else if (entry.kind == 1) PlayerPrefs.SetString(entry.key, entry.strValue ?? "");
                        else if (entry.kind == 2) PlayerPrefs.SetInt(entry.key, entry.intValue);
                        else if (entry.kind == 3) PlayerPrefs.SetFloat(entry.key, entry.floatValue);
                    }
                    PlayerPrefs.Save();
                }
            }
            finally
            {
                if (File.Exists(BackupPath))
                    File.Delete(BackupPath);
            }
        }
    }
}
