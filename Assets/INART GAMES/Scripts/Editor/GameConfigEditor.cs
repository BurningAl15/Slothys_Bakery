using UnityEngine;
using UnityEditor;
using INART.SlothyBakery.Base;

namespace INART.SlothyBakery.Editor
{
    /// <summary>
    /// Custom editor for GameConfig to add helpful buttons and validation.
    /// </summary>
    [CustomEditor(typeof(GameConfig))]
    public class GameConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            GameConfig config = (GameConfig)target;

            EditorGUILayout.HelpBox(
                "GameConfig centralizes all game parameters. " +
                "Legacy code can still use hardcoded values if needed. " +
                "To use this config, access GameConfig.Instance in your scripts.",
                MessageType.Info
            );

            EditorGUILayout.Space();

            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

            if (GUILayout.Button("Reset to Default Values"))
            {
                if (EditorUtility.DisplayDialog(
                    "Reset GameConfig",
                    "Are you sure you want to reset all values to defaults?",
                    "Yes", "Cancel"))
                {
                    ResetToDefaults(config);
                }
            }

            if (GUILayout.Button("Validate Configuration"))
            {
                ValidateConfig(config);
            }
        }

        private void ResetToDefaults(GameConfig config)
        {
            Undo.RecordObject(config, "Reset GameConfig");
            
            config.canvasAnimationDuration = 1f;
            config.timeScaler = 1f;
            config.waitUntilNextLetter = 0.05f;
            config.waitUntilPrevious = 0.5f;
            config.fractionMenuMaxTime = 0.5f;
            config.defaultMusicVolume = 0.7f;
            config.defaultSFXVolume = 0.8f;
            config.totalStoryLevels = 18;
            config.dailyChallengesPerDay = 3;

            EditorUtility.SetDirty(config);
            Debug.Log("[GameConfig] Reset to default values.");
        }

        private void ValidateConfig(GameConfig config)
        {
            bool isValid = true;
            System.Text.StringBuilder warnings = new System.Text.StringBuilder();

            if (config.canvasAnimationDuration <= 0)
            {
                warnings.AppendLine("• Canvas animation duration must be > 0");
                isValid = false;
            }

            if (config.waitUntilNextLetter <= 0)
            {
                warnings.AppendLine("• Letter delay must be > 0");
                isValid = false;
            }

            if (config.totalStoryLevels <= 0)
            {
                warnings.AppendLine("• Total story levels must be > 0");
                isValid = false;
            }

            if (isValid)
            {
                EditorUtility.DisplayDialog("Validation", "✓ Configuration is valid!", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Validation Errors", warnings.ToString(), "OK");
            }
        }
    }
}
