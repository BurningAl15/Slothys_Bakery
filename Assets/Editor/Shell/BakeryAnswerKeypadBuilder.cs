using System.Collections.Generic;
using System.Linq;
using Fatcat.Slothy.Bakery.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Fatcat.Slothy.Bakery.EditorTools
{
    /// <summary>
    /// Owner 2026-11-27: in the question minigames the Reset and Submit buttons overlapped and the answer field
    /// opened the phone keyboard (letters included). This puts an on-screen keypad (0-9, "/", backspace) next to
    /// the field and lays the row out cleanly. Idempotent: keys are rebuilt from the Submit button's style.
    /// MG1: [back] [field] [keypad 2x6] [Submit / Reset].  MG3: [field] [keypad 2x6] [Submit].
    /// Batch: -executeMethod Fatcat.Slothy.Bakery.EditorTools.BakeryAnswerKeypadBuilder.RunCi
    /// </summary>
    public static class BakeryAnswerKeypadBuilder
    {
        const string Mg1 = "Assets/INART GAMES/Scenes/2. Minigame1.unity";
        const string Mg3 = "Assets/INART GAMES/Scenes/4. Minigame3.unity";
        static readonly string[] Labels = { "1", "2", "3", "4", "5", "/", "6", "7", "8", "9", "0", "<" };

        [MenuItem("FatCat/Bakery/Build answer keypads (MG1, MG3)")]
        public static void Build()
        {
            BuildMg1();
            BuildMg3();
            Debug.Log("[BakeryAnswerKeypadBuilder] Keypads built in MG1 and MG3");
        }

        public static void RunCi()
        {
            Build();
            EditorApplication.Exit(0);
        }

        static void BuildMg1()
        {
            var scene = EditorSceneManager.OpenScene(Mg1, OpenSceneMode.Single);
            var group = Find(scene.GetRootGameObjects(), "MessageCanvas/WholeQuestionContainer/InputFieldGroup");
            var input = group.Find("InputField (TMP)") as RectTransform;
            var submit = group.Find("SubmitButton") as RectTransform;
            var reset = group.Find("ResetButton") as RectTransform;
            var back = group.Find("BackButton") as RectTransform;

            Place(back, new Vector2(-250f, 0f), new Vector2(44f, 30f));
            Place(input, new Vector2(-138f, 0f), new Vector2(170f, 62f));
            Place(submit, new Vector2(222f, 18f), new Vector2(100f, 32f));
            Place(reset, new Vector2(222f, -18f), new Vector2(100f, 32f));
            StyleField(input, "2/2");
            BuildKeypad(group, input.GetComponent<TMP_InputField>(), submit.GetComponent<Button>(), new Vector2(60f, 0f), 34f, 37f);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static void BuildMg3()
        {
            var scene = EditorSceneManager.OpenScene(Mg3, OpenSceneMode.Single);
            var group = Find(scene.GetRootGameObjects(), "ButtonCanvas/QuestionBubble/ButtonsCanvasGroup");
            var input = group.Find("InputField (TMP)") as RectTransform;
            var submit = group.Find("SubmitButton") as RectTransform;

            Place(input, new Vector2(-150f, 0f), new Vector2(170f, 58f));
            Place(submit, new Vector2(222f, 0f), new Vector2(100f, 36f));
            StyleField(input, "?/?");
            BuildKeypad(group, input.GetComponent<TMP_InputField>(), submit.GetComponent<Button>(), new Vector2(50f, 0f), 32f, 35f);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>2 rows x 6 keys centred at <paramref name="center"/> (group space), each a copy of Submit.</summary>
        static void BuildKeypad(Transform group, TMP_InputField field, Button template, Vector2 center, float size, float step)
        {
            var old = group.Find("Keypad");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var pad = new GameObject("Keypad", typeof(RectTransform)).GetComponent<RectTransform>();
            pad.SetParent(group, false);
            Place(pad, center, new Vector2(step * 6f, step * 2f));
            var keypad = pad.gameObject.AddComponent<FractionKeypad>();

            var keys = new List<FractionKeypad.Key>();
            for (int i = 0; i < Labels.Length; i++)
            {
                var go = Object.Instantiate(template.gameObject, pad);
                go.name = "Key_" + (Labels[i] == "<" ? "Back" : Labels[i] == "/" ? "Slash" : Labels[i]);
                var btn = go.GetComponent<Button>();
                while (btn.onClick.GetPersistentEventCount() > 0)
                    UnityEditor.Events.UnityEventTools.RemovePersistentListener(btn.onClick, 0);
                int col = i % 6, row = i / 6;
                Place((RectTransform)go.transform, new Vector2((col - 2.5f) * step, (row == 0 ? 0.5f : -0.5f) * step), new Vector2(size, size));
                var label = go.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    // Plain text so no localizer renames the key.
                    foreach (var c in label.GetComponents<MonoBehaviour>())
                        if (c != null && c.GetType().Name.Contains("Locali")) Object.DestroyImmediate(c);
                    label.text = Labels[i];
                    label.enableAutoSizing = true;
                    label.fontSizeMin = 12f;
                    label.fontSizeMax = 26f;
                    label.alignment = TextAlignmentOptions.Center;
                    var lr = (RectTransform)label.transform;
                    lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one; lr.offsetMin = new Vector2(2f, 2f); lr.offsetMax = new Vector2(-2f, -2f);
                }
                keys.Add(new FractionKeypad.Key { button = btn, value = Labels[i] == "<" ? string.Empty : Labels[i] });
            }

            var so = new SerializedObject(keypad);
            so.FindProperty("target").objectReferenceValue = field;
            var arr = so.FindProperty("keys");
            arr.arraySize = keys.Count;
            for (int i = 0; i < keys.Count; i++)
            {
                arr.GetArrayElementAtIndex(i).FindPropertyRelative("button").objectReferenceValue = keys[i].button;
                arr.GetArrayElementAtIndex(i).FindPropertyRelative("value").stringValue = keys[i].value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void StyleField(RectTransform input, string placeholder)
        {
            var field = input.GetComponent<TMP_InputField>();
            if (field == null) return;
            field.readOnly = true;
            field.shouldHideMobileInput = true;
            if (field.textComponent != null)
            {
                field.textComponent.enableAutoSizing = true;
                field.textComponent.fontSizeMin = 14f;
                field.textComponent.fontSizeMax = 40f;
                field.textComponent.alignment = TextAlignmentOptions.Center;
            }
            if (field.placeholder is TMP_Text ph)
            {
                ph.text = placeholder;
                ph.enableAutoSizing = true;
                ph.fontSizeMin = 14f;
                ph.fontSizeMax = 36f;
                ph.alignment = TextAlignmentOptions.Center;
                foreach (var c in ph.GetComponents<MonoBehaviour>())
                    if (c != null && c.GetType().Name.Contains("Locali")) Object.DestroyImmediate(c);
            }
        }

        static void Place(RectTransform rt, Vector2 pos, Vector2 size)
        {
            if (rt == null) return;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        static Transform Find(GameObject[] roots, string path)
        {
            var parts = path.Split('/');
            var root = roots.FirstOrDefault(r => r.name == parts[0]);
            if (root == null) throw new System.Exception("Missing root " + parts[0]);
            var t = root.transform;
            for (int i = 1; i < parts.Length; i++)
            {
                var next = t.Find(parts[i]);
                if (next == null) throw new System.Exception("Missing " + string.Join("/", parts.Take(i + 1)));
                t = next;
            }
            return t;
        }
    }
}
