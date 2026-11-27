using System;
using System.Collections.Generic;
using System.Linq;
using Fatcat.Slothy.Bakery.Shell;
using TMPro;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace FatCat.Slothy.Tools
{
    /// <summary>
    /// Installs Bakery's fixed ES/EN minigame labels from BakeryShell and unifies option-A pastel
    /// buttons, instruction cards, and input fields across MG1-MG3. Idempotent and builder-driven.
    /// </summary>
    public static class BakeryMinigameTextLocalizer
    {
        const string Table = "BakeryShell";
        const string BakeryUiDir = "Assets/Resources/BakeryUI";
        const string FontsDir = "Assets/Resources/Fonts";
        static readonly Color Plum = new Color32(0x58, 0x35, 0x53, 0xFF);
        static readonly Color PlumHint = new Color32(0x58, 0x35, 0x53, 0x8C);

        static readonly (string scene, string original, string key, string es, string en)[] Bindings =
        {
            ("2. Minigame1", "Reset", "mg1.reset", "Reiniciar", "Reset"),
            ("2. Minigame1", "Understood", "mg1.understood", "Entendido", "Understood"),
            ("2. Minigame1", "Yes", "mg1.yes", "Sí", "Yes"),
            ("2. Minigame1", "No", "mg1.no", "No", "No"),
            ("2. Minigame1", "Submit", "mg1.submit", "Comprobar", "Submit"),
            ("2. Minigame1", "Examples: 2/2, 3/3, 4/4...", "mg1.examples", "Ejemplos: 2/2, 3/3, 4/4...", "Examples: 2/2, 3/3, 4/4..."),
            ("2. Minigame1", "Fraction must have equal numerator and denominator values", "mg1.equal_rule", "El numerador y el denominador deben ser iguales", "Fraction must have equal numerator and denominator values"),
            ("2. Minigame1", "When we want to compare fractions, we must check denominators", "mg1.exp_title", "Para comparar fracciones, primero miramos los denominadores", "When we want to compare fractions, we must check denominators"),
            ("2. Minigame1", "1. If they are equals, then we check the numerator", "mg1.exp_1", "1. Si son iguales, comparamos los numeradores", "1. If they are equals, then we check the numerator"),
            ("2. Minigame1", "2. If they are different, then we search for common denominators", "mg1.exp_2", "2. Si son distintos, buscamos un denominador común", "2. If they are different, then we search for common denominators"),
            ("2. Minigame1", "- Then we compare", "mg1.exp_then", "- Luego comparamos", "- Then we compare"),
            ("2. Minigame1", "Slice the desserts by dragging the mouse pointer all over the cutting line", "mg1.instructions", "<swing>1) Corta el postre deslizando sobre la línea punteada</swing>", "<swing>1) Slice the dessert along the cutting line</swing>"),
            ("2. Minigame1", "What fraction must we multiply the first fraction by in order to make the denominators comparable?", "mg1.multiply_prompt", "¿Por qué fracción debemos multiplicar la primera para igualar los denominadores?", "What fraction must we multiply the first fraction by to match denominators?"),
            ("3. Minigame2", "What is the equivalent value of ____?", "mg2.question", "¿Cuál es el valor equivalente de ____?", "What is the equivalent value of ____?"),
            ("3. Minigame2", "Check", "mg2.check", "Comprobar", "Check"),
            ("3. Minigame2", "Drag correct the sign in the plate", "mg2.instructions", "<swing>1) Arrastra el signo al plato\n2) Toca Comprobar</swing>", "<swing>1) Drag a sign to the plate\n2) Tap Check</swing>"),
            ("4. Minigame3", "Submit", "mg3.submit", "Comprobar", "Submit"),
            ("4. Minigame3", "Enter a fraction...", "mg3.placeholder", "Escribe una fracción...", "Enter a fraction..."),
            ("4. Minigame3", "Answer by writing the correct fraction", "mg3.instructions", "<swing>1) Escribe la fracción\n2) Toca Comprobar</swing>", "<swing>1) Write the fraction\n2) Tap Submit</swing>"),
            ("4. Minigame3", "<bounce>What is the equivalence of the pieces of bread showed?</bounce>", "mg3.initial_question", "<bounce>¿Cuál es la equivalencia de las piezas mostradas?</bounce>", "<bounce>What is the equivalence of the pieces shown?</bounce>"),
        };

        static readonly (string key, string es, string en)[] ExtraTexts =
        {
            ("feedback.right", "¡Correcto!", "Right!"),
            ("feedback.try_again", "Inténtalo otra vez", "Try again"),
        };

        static readonly HashSet<string> PrimaryButtonNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "YesButton", "SubmitButton", "UnderstoodButton"
        };

        static readonly HashSet<string> SecondaryButtonNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "NoButton", "ResetButton"
        };

        [MenuItem("FatCat/Slothy Bakery/Localize Minigame Labels")]
        public static void Run()
        {
            EnsureTable();
            var fallbackFont = LoadFallbackFont();
            var displayFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{FontsDir}/LilitaOne-Regular SDF.asset") ?? fallbackFont;
            var bodyFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{FontsDir}/Fredoka-VariableFont_wdth,wght SDF.asset") ?? fallbackFont;
            EnsureFallback(displayFont, fallbackFont);
            EnsureFallback(bodyFont, fallbackFont);

            var berrySprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{BakeryUiDir}/ui_bakery_btn_berry.png");
            var lightSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{BakeryUiDir}/ui_rounded_light.png");

            int boundCount = 0;
            foreach (var group in Bindings.GroupBy(b => b.scene))
            {
                var scene = EditorSceneManager.OpenScene($"Assets/INART GAMES/Scenes/{group.Key}.unity", OpenSceneMode.Additive);
                try
                {
                    var roots = scene.GetRootGameObjects();
                    var labels = roots
                        .SelectMany(root => root.GetComponentsInChildren<TMP_Text>(true))
                        .ToArray();

                    foreach (var binding in group)
                    {
                        var configured = labels
                            .Select(label => (label, component: label.GetComponent<BakeryLocalizedSceneText>()))
                            .Where(pair => pair.component != null && GetKey(pair.component) == binding.key)
                            .ToArray();

                        if (configured.Length >= 1)
                        {
                            foreach (var pair in configured)
                            {
                                ApplyLocalizedBinding(pair.label, pair.component, binding.key, binding.es);
                                boundCount++;
                            }
                            continue;
                        }

                        var matches = labels.Where(candidate => MatchesOriginal(candidate.text, binding.original)).ToArray();
                        if (matches.Length == 0)
                            throw new InvalidOperationException($"{group.Key}: expected at least one TMP label '{binding.original}', found 0.");

                        foreach (var label in matches)
                        {
                            var localized = label.GetComponent<BakeryLocalizedSceneText>()
                                            ?? label.gameObject.AddComponent<BakeryLocalizedSceneText>();
                            ApplyLocalizedBinding(label, localized, binding.key, binding.es);
                            boundCount++;
                        }
                    }

                    RestyleSceneChrome(roots, displayFont, bodyFont, berrySprite, lightSprite);

                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
                finally
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[BakeryMinigameTextLocalizer] Installed {boundCount} fixed labels across {Bindings.GroupBy(b => b.scene).Count()} scenes using '{Table}'.");
        }

        static bool MatchesOriginal(string actual, string expected)
        {
            if (actual == expected) return true;
            if (string.IsNullOrEmpty(actual) || string.IsNullOrEmpty(expected) || expected.Length < 15) return false;
            return actual.Contains(expected, StringComparison.Ordinal);
        }

        static void ApplyLocalizedBinding(TMP_Text label, BakeryLocalizedSceneText localized, string key, string defaultEs)
        {
            localized.Configure(Table, key);
            label.text = defaultEs;
            MarkDirty(label);
            MarkDirty(localized);
        }

        static void RestyleSceneChrome(
            GameObject[] roots,
            TMP_FontAsset displayFont,
            TMP_FontAsset bodyFont,
            Sprite berrySprite,
            Sprite lightSprite)
        {
            foreach (var button in roots.SelectMany(r => r.GetComponentsInChildren<Button>(true)))
            {
                bool isPrimary = PrimaryButtonNames.Contains(button.name) || HasKey(button, "mg2.check");
                bool isSecondary = SecondaryButtonNames.Contains(button.name);
                if (!isPrimary && !isSecondary) continue;

                var sprite = isPrimary ? berrySprite : lightSprite;
                var img = button.GetComponent<Image>();
                if (img != null && sprite != null)
                {
                    img.sprite = sprite;
                    img.type = Image.Type.Sliced;
                    img.pixelsPerUnitMultiplier = 2.0f;
                    img.color = Color.white;
                    MarkDirty(img);
                }

                if (button.name.Equals("ResetButton", StringComparison.OrdinalIgnoreCase) &&
                    button.transform is RectTransform rrt)
                {
                    rrt.sizeDelta = new Vector2(116f, 34f);
                    rrt.anchoredPosition = new Vector2(rrt.anchoredPosition.x, -26f);
                    MarkDirty(rrt);
                }
                else if (button.name.Equals("SubmitButton", StringComparison.OrdinalIgnoreCase) &&
                         button.transform is RectTransform srt)
                {
                    srt.sizeDelta = new Vector2(116f, 38f);
                    srt.anchoredPosition = new Vector2(srt.anchoredPosition.x, 16f);
                    MarkDirty(srt);
                }

                foreach (var tmp in button.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (displayFont != null)
                    {
                        tmp.font = displayFont;
                        tmp.fontSharedMaterial = displayFont.material;
                    }
                    tmp.color = isPrimary ? Color.white : Plum;
                    tmp.enableVertexGradient = false;
                    tmp.enableAutoSizing = true;
                    tmp.fontSizeMin = 13f;
                    tmp.fontSizeMax = button.name.Equals("ResetButton", StringComparison.OrdinalIgnoreCase) ? 19f : 24f;
                    tmp.textWrappingMode = TextWrappingModes.NoWrap;
                    tmp.alignment = TextAlignmentOptions.Center;
                    if (tmp.transform is RectTransform trt && trt != button.transform)
                    {
                        trt.anchorMin = Vector2.zero;
                        trt.anchorMax = Vector2.one;
                        trt.offsetMin = new Vector2(12f, 5f);
                        trt.offsetMax = new Vector2(-12f, -4f);
                    }
                    MarkDirty(tmp);
                }
            }

            foreach (var input in roots.SelectMany(r => r.GetComponentsInChildren<TMP_InputField>(true)))
            {
                var bg = input.GetComponent<Image>();
                if (bg != null && lightSprite != null)
                {
                    bg.sprite = lightSprite;
                    bg.type = Image.Type.Sliced;
                    bg.color = Color.white;
                    MarkDirty(bg);
                }

                if (input.textComponent != null)
                {
                    if (bodyFont != null)
                    {
                        input.textComponent.font = bodyFont;
                        input.textComponent.fontSharedMaterial = bodyFont.material;
                    }
                    input.textComponent.color = Plum;
                    input.contentType = TMP_InputField.ContentType.Custom;
                    input.inputType = TMP_InputField.InputType.Standard;
                    input.keyboardType = TouchScreenKeyboardType.NumbersAndPunctuation;
                    input.characterLimit = 7;
                    MarkDirty(input.textComponent);
                }

                if (input.placeholder is TMP_Text ph)
                {
                    if (bodyFont != null)
                    {
                        ph.font = bodyFont;
                        ph.fontSharedMaterial = bodyFont.material;
                    }
                    ph.color = PlumHint;
                    MarkDirty(ph);
                }
            }

            // Style MessageCanvas corner instruction card cleanly in option A light rounded panel
            foreach (var localized in roots.SelectMany(r => r.GetComponentsInChildren<BakeryLocalizedSceneText>(true)))
            {
                var key = GetKey(localized);
                if (key == null || !key.EndsWith(".instructions", StringComparison.Ordinal)) continue;

                var tmp = localized.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    if (bodyFont != null)
                    {
                        tmp.font = bodyFont;
                        tmp.fontSharedMaterial = bodyFont.material;
                    }
                    tmp.color = Plum;
                    tmp.enableVertexGradient = false;
                    tmp.enableAutoSizing = true;
                    tmp.fontSizeMin = 10f;
                    tmp.fontSizeMax = 15f;
                    tmp.margin = new Vector4(12f, 8f, 12f, 8f);
                    tmp.alignment = TextAlignmentOptions.Center;
                    MarkDirty(tmp);
                }

                var parent = localized.transform.parent;
                var cardImage = parent != null ? parent.GetComponent<Image>() : null;
                if (cardImage == null && parent != null && parent.parent != null)
                    cardImage = parent.parent.GetComponent<Image>();

                if (cardImage != null && lightSprite != null &&
                    (cardImage.gameObject.name.Contains("Message", StringComparison.OrdinalIgnoreCase) ||
                     cardImage.gameObject.name.Contains("Panel", StringComparison.OrdinalIgnoreCase) ||
                     cardImage.gameObject.name.Contains("Hint", StringComparison.OrdinalIgnoreCase) ||
                     cardImage.gameObject.name.Contains("Box", StringComparison.OrdinalIgnoreCase)))
                {
                    cardImage.sprite = lightSprite;
                    cardImage.type = Image.Type.Sliced;
                    cardImage.color = Color.white;
                    MarkDirty(cardImage);
                }
            }

            // Use Fredoka on explanatory text labels so Spanish accents and readable body copy never clip
            foreach (var localized in roots.SelectMany(r => r.GetComponentsInChildren<BakeryLocalizedSceneText>(true)))
            {
                var key = GetKey(localized);
                if (key == null || (!key.StartsWith("mg1.exp", StringComparison.Ordinal) &&
                                    key != "mg1.examples" && key != "mg1.equal_rule" && key != "mg1.multiply_prompt"))
                    continue;
                var tmp = localized.GetComponent<TMP_Text>();
                if (tmp == null) continue;
                if (bodyFont != null)
                {
                    tmp.font = bodyFont;
                    tmp.fontSharedMaterial = bodyFont.material;
                }
                MarkDirty(tmp);
            }
        }

        static bool HasKey(Component root, string expectedKey)
        {
            foreach (var loc in root.GetComponentsInChildren<BakeryLocalizedSceneText>(true))
                if (GetKey(loc) == expectedKey)
                    return true;
            return false;
        }

        static TMP_FontAsset LoadFallbackFont()
        {
            var fallbackPath = AssetDatabase.FindAssets("LiberationSans SDF t:TMP_FontAsset",
                                   new[] { "Assets/INART GAMES/TextMesh Pro" })
                                   .Select(AssetDatabase.GUIDToAssetPath)
                                   .FirstOrDefault(p => !p.Contains("Fallback") && !p.Contains("Outline") && !p.Contains("Shadow"))
                               ?? "Assets/INART GAMES/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fallbackPath);
        }

        static void EnsureFallback(TMP_FontAsset target, TMP_FontAsset fallback)
        {
            if (target == null || fallback == null || target == fallback) return;
            target.fallbackFontAssetTable ??= new List<TMP_FontAsset>();
            if (!target.fallbackFontAssetTable.Contains(fallback))
            {
                target.fallbackFontAssetTable.Add(fallback);
                EditorUtility.SetDirty(target);
            }
        }

        static void MarkDirty(UnityEngine.Object obj)
        {
            if (obj == null) return;
            EditorUtility.SetDirty(obj);
            if (PrefabUtility.IsPartOfPrefabInstance(obj))
                PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
        }

        static string GetKey(BakeryLocalizedSceneText component)
        {
            var serialized = new SerializedObject(component);
            return serialized.FindProperty("key")?.stringValue;
        }

        static void EnsureTable()
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(Table)
                             ?? LocalizationEditorSettings.CreateStringTableCollection(
                                 Table, "Assets/Localization/Tables", LocalizationEditorSettings.GetLocales());

            foreach (var locale in LocalizationEditorSettings.GetLocales())
                if (collection.GetTable(locale.Identifier) == null)
                    collection.AddNewTable(locale.Identifier);

            foreach (var table in collection.StringTables)
            {
                bool spanish = table.LocaleIdentifier.Code.StartsWith("es", StringComparison.OrdinalIgnoreCase);
                foreach (var binding in Bindings)
                    table.AddEntry(binding.key, spanish ? binding.es : binding.en);
                foreach (var extra in ExtraTexts)
                    table.AddEntry(extra.key, spanish ? extra.es : extra.en);
                EditorUtility.SetDirty(table);
            }

            EditorUtility.SetDirty(collection.SharedData);
            EditorUtility.SetDirty(collection);
            AssetDatabase.SaveAssets();
        }

        public static void RunCi()
        {
            try { Run(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
