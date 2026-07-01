#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using INART.SlothyBakery.Base;
using INART.SlothyBakery.SceneTransition;

namespace INART.SlothyBakery.DevTools
{
    /// <summary>
    /// In-game developer overlay. Visible only in Editor and Development Builds.
    ///
    /// Add this component to any persistent GameObject (e.g. the one that holds
    /// SoundManager/SettingsManager in a Loader scene with DontDestroyOnLoad).
    ///
    /// Features:
    ///   • Toggle overlay with Tab key (configurable)
    ///   • Jump to any scene by index or name with one click
    ///   • Next / Previous scene shortcuts
    ///   • Skip current minigame intro animation (calls GameManager.CallNextScene)
    ///   • Display current scene name and build index
    ///   • Clear PlayerPrefs (reset save)
    ///
    /// Keyboard shortcuts (in Play Mode):
    ///   Tab          — toggle overlay visibility
    ///   ]            — load next scene
    ///   [            — load previous scene
    ///   Backspace    — reload current scene
    /// </summary>
    public class DevToolsOverlay : MonoBehaviour
    {
        // ── Configuration ──────────────────────────────────────────────────────
        [Header("Overlay")]
        [Tooltip("Key to toggle overlay visibility.")]
        [SerializeField] private KeyCode toggleKey      = KeyCode.Tab;
        [Tooltip("Position of the overlay panel (normalized 0-1 from top-left).")]
        [SerializeField] private Vector2 panelAnchor    = new Vector2(0.01f, 0.01f);
        [Tooltip("Width of the overlay panel in pixels.")]
        [SerializeField] private float   panelWidth     = 220f;

        [Header("Scene Navigation")]
        [Tooltip("Use animated fade when jumping scenes (requires SceneTransitionController).")]
        [SerializeField] private bool    useFade        = true;

        // ── Runtime ────────────────────────────────────────────────────────────
        private bool      _visible        = true;
        private bool      _navigating     = false;
        private GUIStyle  _panelStyle;
        private GUIStyle  _btnStyle;
        private GUIStyle  _labelStyle;
        private GUIStyle  _headerStyle;
        private bool      _stylesBuilt    = false;

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Update()
        {
            // Toggle overlay
            if (Input.GetKeyDown(toggleKey))
                _visible = !_visible;

            if (_navigating) return;

            // Next scene
            if (Input.GetKeyDown(KeyCode.RightBracket))
                NavigateTo(SceneManager.GetActiveScene().buildIndex + 1);

            // Previous scene
            if (Input.GetKeyDown(KeyCode.LeftBracket))
                NavigateTo(SceneManager.GetActiveScene().buildIndex - 1);

            // Reload
            if (Input.GetKeyDown(KeyCode.Backspace))
                NavigateTo(SceneManager.GetActiveScene().buildIndex);
        }

        // ── IMGUI overlay ──────────────────────────────────────────────────────

        private void OnGUI()
        {
            if (!_visible) return;

            BuildStyles();

            int   totalScenes  = SceneManager.sceneCountInBuildSettings;
            int   currentIndex = SceneManager.GetActiveScene().buildIndex;
            float lineH        = 28f;
            float padding      = 8f;

            // Calculate panel height dynamically
            float panelHeight = padding * 2
                + lineH         // header
                + lineH         // scene info
                + lineH         // separator
                + lineH * 2     // prev/next/reload row
                + (lineH * totalScenes)  // scene buttons
                + lineH         // separator
                + lineH         // clear prefs
                + lineH;        // hide hint

            float x = Screen.width  * panelAnchor.x;
            float y = Screen.height * panelAnchor.y;

            // Shadow
            GUI.color = new Color(0, 0, 0, 0.3f);
            GUI.Box(new Rect(x + 2, y + 2, panelWidth, panelHeight), GUIContent.none);
            GUI.color = Color.white;

            // Panel background
            GUI.Box(new Rect(x, y, panelWidth, panelHeight), GUIContent.none, _panelStyle);

            float cx = x + padding;
            float cy = y + padding;
            float bw = panelWidth - padding * 2;

            // Header
            GUI.Label(new Rect(cx, cy, bw, lineH), "⚙  DEV TOOLS", _headerStyle);
            cy += lineH;

            // Current scene info
            string sceneName = SceneManager.GetActiveScene().name;
            GUI.Label(new Rect(cx, cy, bw, lineH),
                $"Scene {currentIndex}: {sceneName}", _labelStyle);
            cy += lineH + 4f;

            // Navigation row
            float thirdW = (bw - 4f) / 3f;

            bool hasPrev = currentIndex > 0;
            bool hasNext = currentIndex < totalScenes - 1;

            GUI.enabled = hasPrev && !_navigating;
            if (GUI.Button(new Rect(cx, cy, thirdW, lineH), "◀ Prev", _btnStyle))
                NavigateTo(currentIndex - 1);

            GUI.enabled = !_navigating;
            if (GUI.Button(new Rect(cx + thirdW + 2f, cy, thirdW, lineH), "↺ Reload", _btnStyle))
                NavigateTo(currentIndex);

            GUI.enabled = hasNext && !_navigating;
            if (GUI.Button(new Rect(cx + (thirdW + 2f) * 2f, cy, thirdW, lineH), "Next ▶", _btnStyle))
                NavigateTo(currentIndex + 1);

            GUI.enabled = true;
            cy += lineH + 6f;

            // Scene buttons — one per scene in build settings
            for (int i = 0; i < totalScenes; i++)
            {
#if UNITY_EDITOR
                string label = i < UnityEditor.EditorBuildSettings.scenes.Length
                    ? System.IO.Path.GetFileNameWithoutExtension(
                        UnityEditor.EditorBuildSettings.scenes[i].path)
                    : $"Scene {i}";
#else
                string label = $"Scene {i}";
#endif
                bool isActive = i == currentIndex;

                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = isActive
                    ? new Color(0.3f, 0.8f, 0.4f)   // green = current
                    : new Color(0.2f, 0.5f, 0.9f);   // blue  = other

                GUI.enabled = !isActive && !_navigating;
                if (GUI.Button(new Rect(cx, cy, bw, lineH - 2f), label, _btnStyle))
                    NavigateTo(i);

                GUI.backgroundColor = prev;
                GUI.enabled         = true;
                cy += lineH;
            }

            cy += 4f;

            // Clear PlayerPrefs
            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.85f, 0.3f, 0.3f);
            if (GUI.Button(new Rect(cx, cy, bw, lineH), "🗑  Clear PlayerPrefs", _btnStyle))
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                Debug.Log("[DevTools] PlayerPrefs cleared.");
            }
            GUI.backgroundColor = prevBg;
            cy += lineH + 4f;

            // Hide hint
            GUI.color = new Color(1, 1, 1, 0.5f);
            GUI.Label(new Rect(cx, cy, bw, lineH),
                $"[{toggleKey}] hide  |  [ ] prev  ] next", _labelStyle);
            GUI.color = Color.white;
        }

        // ── Navigation ─────────────────────────────────────────────────────────

        private void NavigateTo(int buildIndex)
        {
            int total = SceneManager.sceneCountInBuildSettings;
            buildIndex = Mathf.Clamp(buildIndex, 0, total - 1);

            if (_navigating) return;

            if (useFade && SceneTransitionController.Instance != null)
                StartCoroutine(NavigateWithFade(buildIndex));
            else
                SceneManager.LoadScene(buildIndex);
        }

        private IEnumerator NavigateWithFade(int buildIndex)
        {
            _navigating = true;
            yield return StartCoroutine(SceneTransitionController.Instance.FadeEffect_OUT());
            SceneManager.LoadScene(buildIndex);
            _navigating = false;
        }

        // ── Style builder ──────────────────────────────────────────────────────

        private void BuildStyles()
        {
            if (_stylesBuilt) return;
            _stylesBuilt = true;

            // Panel background
            _panelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTex(1, 1, new Color(0.08f, 0.08f, 0.12f, 0.92f)) }
            };

            // Buttons
            _btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize  = 11,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = Color.white }
            };

            // Labels
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 10,
                normal    = { textColor = new Color(0.8f, 0.8f, 0.8f) }
            };

            // Header
            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 12,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = new Color(0.4f, 0.9f, 1f) }
            };
        }

        private static Texture2D MakeTex(int w, int h, Color col)
        {
            var pix = new Color[w * h];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            var result = new Texture2D(w, h);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }
}
#endif
