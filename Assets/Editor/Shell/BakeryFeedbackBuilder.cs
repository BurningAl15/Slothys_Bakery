using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FatCat.Feedback;
using TMPro;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace FatCat.Slothy.Tools
{
    /// <summary>
    /// Builds the Feedback Kit prefab for Slothy's Bakery (Assets/Resources/FatCatFeedback/FeedbackStamp.prefab)
    /// using Bakery's option-A pastel kit, icons, and Slothy portrait. Idempotent.
    /// </summary>
    public static class BakeryFeedbackBuilder
    {
        const string ArtDir = "Assets/Art/Feedback";
        const string BakeryUiDir = "Assets/Resources/BakeryUI";
        const string FontsDir = "Assets/Resources/Fonts";
        const string PrefabPath = "Assets/Resources/" + FeedbackStamp.ResourcePath + ".prefab";
        static readonly Color Plum = new Color32(0x58, 0x35, 0x53, 0xFF);
        static readonly Color Cream = new Color32(0xFF, 0xF8, 0xEE, 0xFF);

        public static readonly (string key, string es, string en)[] Texts =
        {
            ("feedback.streak", "¡Racha ×{0}!", "Streak ×{0}!"),
            ("feedback.right", "¡Muy bien!", "Well done!"),
            ("feedback.try_again", "¡Inténtalo otra vez!", "Try again!"),
            ("feedback.title", "¡Muy bien!", "Well done!"),
            ("feedback.no_mistakes", "Sin errores", "No mistakes"),
            ("feedback.one_mistake", "1 error", "1 mistake"),
            ("feedback.mistakes", "{0} errores", "{0} mistakes"),
            ("feedback.continue", "Continuar", "Continue"),
        };

        [MenuItem("FatCat/Slothy Bakery/Build Feedback Kit")]
        public static void Build()
        {
            Directory.CreateDirectory(ArtDir);
            var check = Icon("feedback_check", (x, y) => Stroke(x, y, 0.13f, new Vector2(0.24f, 0.52f), new Vector2(0.43f, 0.71f), new Vector2(0.78f, 0.31f)));
            var cross = Icon("feedback_cross", (x, y) => Mathf.Max(Stroke(x, y, 0.13f, new Vector2(0.28f, 0.28f), new Vector2(0.72f, 0.72f)), Stroke(x, y, 0.13f, new Vector2(0.72f, 0.28f), new Vector2(0.28f, 0.72f))));
            var star = Icon("feedback_star", (x, y) => StarFill(x, y, 5, 0.47f, 0.2f), outline: true);
            var sparkle = Icon("feedback_sparkle", (x, y) => StarFill(x, y, 4, 0.5f, 0.13f));

            var circle = AssetDatabase.LoadAssetAtPath<Sprite>($"{BakeryUiDir}/ui_circle.png");
            var rounded = AssetDatabase.LoadAssetAtPath<Sprite>($"{BakeryUiDir}/ui_rounded.png");
            var berryButton = AssetDatabase.LoadAssetAtPath<Sprite>($"{BakeryUiDir}/ui_bakery_btn_berry.png");
            var slothy = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/INART GAMES/Art/Textures/UI/perezoso_repostero.png");
            var flame = AssetDatabase.LoadAssetAtPath<Sprite>($"{BakeryUiDir}/icon_flame.png");

            var fallbackPath = AssetDatabase.FindAssets("LiberationSans SDF t:TMP_FontAsset",
                                   new[] { "Assets/INART GAMES/TextMesh Pro" })
                                   .Select(AssetDatabase.GUIDToAssetPath)
                                   .FirstOrDefault(p => !p.Contains("Fallback") && !p.Contains("Outline") && !p.Contains("Shadow"))
                               ?? "Assets/INART GAMES/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
            var fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fallbackPath);
            var display = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{FontsDir}/LilitaOne-Regular SDF.asset") ?? fallback;
            var body = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{FontsDir}/Fredoka-VariableFont_wdth,wght SDF.asset") ?? fallback;

            var root = new GameObject("FeedbackStamp", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            try
            {
                root.layer = LayerMask.NameToLayer("UI");
                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 550;
                var scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1024, 576);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

                var layer = Node("Layer", root.transform);
                Stretch(layer);

                var sparkles = new List<Image>();
                var sparkRoot = Node("Sparkles", layer);
                Stretch(sparkRoot);
                for (int i = 0; i < 28; i++)
                {
                    var s = Img(Node("Spark" + i, sparkRoot), sparkle, Color.white);
                    Center((RectTransform)s.transform, new Vector2(22, 22));
                    sparkles.Add(s);
                    s.gameObject.SetActive(false);
                }

                var peek = Node("Peek", layer);
                peek.anchorMin = peek.anchorMax = new Vector2(0, 0);
                peek.pivot = new Vector2(0.5f, 1f);
                peek.sizeDelta = new Vector2(120, 160);
                peek.anchoredPosition = new Vector2(90, 0);
                Img(peek, slothy, Color.white).preserveAspect = true;

                var badge = Node("Badge", layer);
                Center(badge, new Vector2(84, 84));
                badge.gameObject.AddComponent<CanvasGroup>();
                var badgeBg = Img(badge, circle, new Color32(0x5F, 0x9B, 0x62, 0xFF));
                var ring = badge.gameObject.AddComponent<Outline>();
                ring.effectColor = Plum;
                ring.effectDistance = new Vector2(4, -4);
                var icon = Img(Node("Icon", badge), check, Color.white);
                Center((RectTransform)icon.transform, new Vector2(52, 52));

                var pill = Node("Message", layer);
                pill.anchorMin = pill.anchorMax = pill.pivot = new Vector2(1f, 1f);
                pill.sizeDelta = new Vector2(250, 90);
                pill.anchoredPosition = new Vector2(-18, -94);
                pill.gameObject.AddComponent<CanvasGroup>();
                var frame = Img(Node("Frame", pill), rounded, Plum);
                if (rounded != null) { frame.type = Image.Type.Sliced; frame.pixelsPerUnitMultiplier = 2.0f; }
                Stretch((RectTransform)frame.transform);
                var pillFill = Img(Node("Fill", pill), rounded, Cream);
                if (rounded != null) { pillFill.type = Image.Type.Sliced; pillFill.pixelsPerUnitMultiplier = 2.0f; }
                Stretch((RectTransform)pillFill.transform, 4, 4, 4, 4);
                var msg = Text(Node("Text", pill), body, 19, Plum);
                msg.fontStyle = FontStyles.Bold;
                msg.enableAutoSizing = true;
                msg.fontSizeMin = 13;
                msg.fontSizeMax = 19;
                msg.textWrappingMode = TextWrappingModes.Normal;
                Stretch((RectTransform)msg.transform, 14, 14, 6, 6);

                var chip = Node("Streak", layer);
                chip.anchorMin = chip.anchorMax = chip.pivot = new Vector2(1f, 1f);
                chip.sizeDelta = new Vector2(170, 42);
                chip.anchoredPosition = new Vector2(-42, -194);
                chip.gameObject.AddComponent<CanvasGroup>();
                var chipBg = Img(chip, rounded, Cream);
                if (rounded != null) { chipBg.type = Image.Type.Sliced; chipBg.pixelsPerUnitMultiplier = 2.0f; }
                var co = chip.gameObject.AddComponent<Outline>();
                co.effectColor = Plum;
                co.effectDistance = new Vector2(3, -3);
                var chipRow = chip.gameObject.AddComponent<HorizontalLayoutGroup>();
                chipRow.childAlignment = TextAnchor.MiddleCenter;
                chipRow.spacing = 6;
                chipRow.childControlWidth = chipRow.childControlHeight = false;
                chipRow.childForceExpandWidth = chipRow.childForceExpandHeight = false;
                var fl = Img(Node("Flame", chip), flame, Color.white);
                fl.preserveAspect = true;
                ((RectTransform)fl.transform).sizeDelta = new Vector2(24, 24);
                var chipText = Text(Node("Text", chip), display, 21, Plum);
                ((RectTransform)chipText.transform).sizeDelta = new Vector2(120, 34);

                var cel = Node("Celebration", layer);
                Stretch(cel);
                var dimImg = Img(cel, null, new Color(Plum.r, Plum.g, Plum.b, 0.55f));
                dimImg.raycastTarget = true;
                var dim = cel.gameObject.AddComponent<CanvasGroup>();
                var card = Node("Card", cel);
                Center(card, new Vector2(470, 320));
                var cardBg = Img(card, rounded, Cream);
                if (rounded != null) { cardBg.type = Image.Type.Sliced; cardBg.pixelsPerUnitMultiplier = 1.6f; }
                var cardO = card.gameObject.AddComponent<Outline>();
                cardO.effectColor = Plum;
                cardO.effectDistance = new Vector2(5, -5);
                var title = Text(Node("Title", card), display, 40, Plum);
                Place((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(420, 54), new Vector2(0, -26));
                var starRow = Node("Stars", card);
                Place(starRow, new Vector2(0.5f, 1f), new Vector2(300, 80), new Vector2(0, -88));
                var srow = starRow.gameObject.AddComponent<HorizontalLayoutGroup>();
                srow.spacing = 16;
                srow.childAlignment = TextAnchor.MiddleCenter;
                srow.childControlWidth = srow.childControlHeight = false;
                srow.childForceExpandWidth = srow.childForceExpandHeight = false;
                var starImgs = new Image[3];
                for (int i = 0; i < 3; i++)
                {
                    starImgs[i] = Img(Node("Star" + i, starRow), star, Color.white);
                    starImgs[i].preserveAspect = true;
                    ((RectTransform)starImgs[i].transform).sizeDelta = new Vector2(76, 76);
                }
                var sub = Text(Node("Subtitle", card), body, 20, Plum);
                sub.fontStyle = FontStyles.Bold;
                Place((RectTransform)sub.transform, new Vector2(0.5f, 1f), new Vector2(420, 30), new Vector2(0, -176));
                var btn = Node("Continue", card);
                Place(btn, new Vector2(0.5f, 0f), new Vector2(216, 60), new Vector2(0, 22));
                var btnImg = Img(btn, berryButton, Color.white);
                if (berryButton != null) { btnImg.type = Image.Type.Sliced; btnImg.pixelsPerUnitMultiplier = 1.6f; }
                btnImg.raycastTarget = true;
                var button = btn.gameObject.AddComponent<Button>();
                button.targetGraphic = btnImg;
                var btnText = Text(Node("Text", btn), display, 26, Color.white);
                Stretch((RectTransform)btnText.transform, 10, 10, 4, 8);

                var stamp = root.AddComponent<FeedbackStamp>();
                var so = new SerializedObject(stamp);
                so.FindProperty("badge").objectReferenceValue = badge;
                so.FindProperty("badgeBackground").objectReferenceValue = badgeBg;
                so.FindProperty("badgeIcon").objectReferenceValue = icon;
                so.FindProperty("rightIcon").objectReferenceValue = check;
                so.FindProperty("wrongIcon").objectReferenceValue = cross;
                so.FindProperty("pill").objectReferenceValue = pill;
                so.FindProperty("pillFrame").objectReferenceValue = frame;
                so.FindProperty("pillText").objectReferenceValue = msg;
                so.FindProperty("peek").objectReferenceValue = peek;
                so.FindProperty("peekRise").floatValue = 138f;
                so.FindProperty("streakChip").objectReferenceValue = chip;
                so.FindProperty("streakText").objectReferenceValue = chipText;
                var sp = so.FindProperty("sparkles");
                sp.arraySize = sparkles.Count;
                for (int i = 0; i < sparkles.Count; i++)
                    sp.GetArrayElementAtIndex(i).objectReferenceValue = sparkles[i];
                so.ApplyModifiedPropertiesWithoutUndo();

                var celebration = cel.gameObject.AddComponent<FeedbackCelebration>();
                var cso = new SerializedObject(celebration);
                cso.FindProperty("dim").objectReferenceValue = dim;
                cso.FindProperty("card").objectReferenceValue = card;
                cso.FindProperty("title").objectReferenceValue = title;
                cso.FindProperty("subtitle").objectReferenceValue = sub;
                cso.FindProperty("buttonText").objectReferenceValue = btnText;
                var sa = cso.FindProperty("stars");
                sa.arraySize = 3;
                for (int i = 0; i < 3; i++)
                    sa.GetArrayElementAtIndex(i).objectReferenceValue = starImgs[i];
                cso.FindProperty("continueButton").objectReferenceValue = button;
                cso.ApplyModifiedPropertiesWithoutUndo();

                Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            AddTexts();
            AssetDatabase.SaveAssets();
            Debug.Log("[BakeryFeedbackBuilder] Built " + PrefabPath);
        }

        static void AddTexts()
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection("BakeryShell");
            if (collection == null) return;
            foreach (var table in collection.StringTables)
            {
                bool es = table.LocaleIdentifier.Code.StartsWith("es", StringComparison.OrdinalIgnoreCase);
                foreach (var (key, esText, enText) in Texts)
                    table.AddEntry(key, es ? esText : enText);
                EditorUtility.SetDirty(table);
            }
            EditorUtility.SetDirty(collection.SharedData);
        }

        static Sprite Icon(string name, Func<float, float, float> coverage, bool outline = false)
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float a = 0f;
                    for (int sy = 0; sy < 3; sy++)
                        for (int sx = 0; sx < 3; sx++)
                            a += coverage((x + (sx + 0.5f) / 3f) / size, 1f - (y + (sy + 0.5f) / 3f) / size);
                    a /= 9f;
                    var c = Color.white;
                    if (outline)
                    {
                        float inner = 0f;
                        for (int sy = 0; sy < 3; sy++)
                            for (int sx = 0; sx < 3; sx++)
                                inner += InnerCoverage(coverage, (x + (sx + 0.5f) / 3f) / size, 1f - (y + (sy + 0.5f) / 3f) / size, 0.05f);
                        inner /= 9f;
                        c = Color.Lerp(Plum, Color.white, inner);
                    }
                    c.a = a;
                    tex.SetPixel(x, y, c);
                }
            tex.Apply();
            var path = $"{ArtDir}/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static float InnerCoverage(Func<float, float, float> f, float x, float y, float r)
        {
            float m = 1f;
            for (int k = 0; k < 8; k++)
            {
                float a = k * Mathf.PI / 4f;
                m = Mathf.Min(m, f(x + Mathf.Cos(a) * r, y + Mathf.Sin(a) * r));
            }
            return m;
        }

        static float Stroke(float x, float y, float width, params Vector2[] pts)
        {
            var p = new Vector2(x, y);
            float d = float.MaxValue;
            for (int i = 0; i < pts.Length - 1; i++)
            {
                var a = pts[i];
                var b = pts[i + 1];
                var ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                d = Mathf.Min(d, Vector2.Distance(p, a + ab * t));
            }
            return d <= width * 0.5f ? 1f : 0f;
        }

        static float StarFill(float x, float y, int points, float outer, float inner)
        {
            var poly = new Vector2[points * 2];
            for (int i = 0; i < poly.Length; i++)
            {
                float a = -Mathf.PI / 2f + i * Mathf.PI / points;
                float r = i % 2 == 0 ? outer : inner;
                poly[i] = new Vector2(0.5f + Mathf.Cos(a) * r, 0.52f + Mathf.Sin(a) * r);
            }
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                if ((poly[i].y > y) != (poly[j].y > y) && x < (poly[j].x - poly[i].x) * (y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                    inside = !inside;
            return inside ? 1f : 0f;
        }

        static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = LayerMask.NameToLayer("UI") };
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static void Stretch(RectTransform rt, float l = 0, float r = 0, float t = 0, float b = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b);
            rt.offsetMax = new Vector2(-r, -t);
        }

        static void Center(RectTransform rt, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
        }

        static void Place(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 pos)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }

        static Image Img(Transform t, Sprite sprite, Color color)
        {
            var img = t.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static TextMeshProUGUI Text(Transform t, TMP_FontAsset font, float size, Color color)
        {
            var x = t.gameObject.AddComponent<TextMeshProUGUI>();
            x.font = font;
            x.fontSize = size;
            x.color = color;
            x.alignment = TextAlignmentOptions.Center;
            x.raycastTarget = false;
            x.textWrappingMode = TextWrappingModes.NoWrap;
            return x;
        }

        public static void RunCi()
        {
            try { Build(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
