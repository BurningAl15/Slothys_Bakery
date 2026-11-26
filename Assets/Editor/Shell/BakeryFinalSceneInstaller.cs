using System;
using System.Linq;
using Fatcat.Slothy.Bakery.Shell;
using INART.SlothyBakery.Base;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace FatCat.Slothy.Tools
{
    /// <summary>
    /// Refines 5. FinalCutscene.unity cleanly in Slothy's Bakery option A pastel style:
    /// - Fixes serialized _light/_anim references on FinalCutscene
    /// - Restyles Star Assistant title with LilitaOne Plum & BakeryShell localization (no rainbow/EN-only text)
    /// - Installs RewardDim, centered localized FinishedLine, Back to start ("Volver al inicio"), and "Ver mi diploma" button
    /// - Tames confetti particle emitters behind the star. Idempotent.
    /// Batch: -executeMethod FatCat.Slothy.Tools.BakeryFinalSceneInstaller.RunCi
    /// </summary>
    public static class BakeryFinalSceneInstaller
    {
        const string Scene = "Assets/INART GAMES/Scenes/5. FinalCutscene.unity";
        const string Table = "BakeryShell";
        const string BakeryUiDir = "Assets/Resources/BakeryUI";
        const string FontsDir = "Assets/Resources/Fonts";
        static readonly Color Plum = new Color32(0x58, 0x35, 0x53, 0xFF);
        static readonly Color Cream = new Color32(0xFF, 0xF8, 0xEE, 0xFF);

        static readonly (string key, string es, string en)[] Texts =
        {
            ("final.title",    "¡Asistente estrella!",                        "Star assistant!"),
            ("final.subtitle", "¡Terminaste la historia de Slothy's Bakery!", "You finished Slothy's Bakery story!"),
            ("final.back",     "Volver al inicio",                            "Back to start"),
            ("diploma.open",   "Ver mi diploma",                              "See my diploma"),
        };

        [MenuItem("FatCat/Slothy Bakery/Refine Final Scene")]
        public static void Run()
        {
            AddTexts();
            var fallbackPath = AssetDatabase.FindAssets("LiberationSans SDF t:TMP_FontAsset",
                                   new[] { "Assets/INART GAMES/TextMesh Pro" })
                                   .Select(AssetDatabase.GUIDToAssetPath)
                                   .FirstOrDefault(p => !p.Contains("Fallback") && !p.Contains("Outline") && !p.Contains("Shadow"))
                               ?? "Assets/INART GAMES/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
            var fallbackFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fallbackPath);
            var display = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{FontsDir}/LilitaOne-Regular SDF.asset") ?? fallbackFont;
            var body = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{FontsDir}/Fredoka-VariableFont_wdth,wght SDF.asset") ?? fallbackFont;
            var berrySprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{BakeryUiDir}/ui_bakery_btn_berry.png");
            var lightSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{BakeryUiDir}/ui_rounded_light.png");

            var scene = EditorSceneManager.OpenScene(Scene, OpenSceneMode.Additive);
            try
            {
                var finalCutscene = UnityEngine.Object.FindObjectsByType<FinalCutscene>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .FirstOrDefault(f => f.gameObject.scene == scene);
                if (finalCutscene == null)
                {
                    Debug.LogWarning("[BakeryFinalSceneInstaller] FinalCutscene component not found.");
                    return;
                }

                // 1. Star & title refinement
                var roots = scene.GetRootGameObjects();
                var starRoot = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                    .FirstOrDefault(t => t.name == "starAssistant" && t.GetComponent<SpriteRenderer>() != null);
                if (starRoot != null)
                {
                    starRoot.localScale = Vector3.one * 0.80f;
                    var pos = starRoot.position;
                    starRoot.position = new Vector3(pos.x, Mathf.Max(pos.y, 0.55f), pos.z);

                    var titleTmp = starRoot.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault();
                    if (titleTmp != null)
                    {
                        titleTmp.font = display;
                        titleTmp.fontSharedMaterial = display.material;
                        titleTmp.color = Plum;
                        titleTmp.enableVertexGradient = false;
                        titleTmp.enableAutoSizing = true;
                        titleTmp.fontSizeMin = 2f;
                        titleTmp.fontSizeMax = 8.5f;
                        titleTmp.alignment = TextAlignmentOptions.Center;
                        titleTmp.text = Texts[0].es;
                        Localize(titleTmp.gameObject, "final.title");
                        EditorUtility.SetDirty(titleTmp);
                    }
                    EditorUtility.SetDirty(starRoot.gameObject);
                }

                // 2. Canvas container (BackgroundImg / ResetButton)
                var resetBtn = roots.SelectMany(r => r.GetComponentsInChildren<Button>(true))
                    .FirstOrDefault(b => b.name == "ResetButton");
                var canvasParent = resetBtn != null ? (RectTransform)resetBtn.transform.parent : null;
                if (canvasParent == null)
                {
                    var bgCanvas = roots.FirstOrDefault(r => r.name == "BackgroundCanvas");
                    canvasParent = bgCanvas != null ? (RectTransform)bgCanvas.transform : null;
                }

                CanvasGroup rewardDimGroup = null;
                TMP_Text finishedTmp = null;
                Button diplomaBtn = null;

                if (canvasParent != null)
                {
                    // Dark translucent dim behind star text & CTAs
                    var dimRt = canvasParent.Find("RewardDim") as RectTransform;
                    if (dimRt == null)
                    {
                        var dimGo = new GameObject("RewardDim", typeof(RectTransform), typeof(Image), typeof(CanvasGroup))
                        {
                            layer = canvasParent.gameObject.layer
                        };
                        dimRt = (RectTransform)dimGo.transform;
                        dimRt.SetParent(canvasParent, false);
                    }
                    dimRt.SetSiblingIndex(0);
                    dimRt.anchorMin = Vector2.zero;
                    dimRt.anchorMax = Vector2.one;
                    dimRt.offsetMin = Vector2.zero;
                    dimRt.offsetMax = Vector2.zero;
                    var dimImg = dimRt.GetComponent<Image>();
                    dimImg.color = new Color(0.14f, 0.06f, 0.14f, 0.44f);
                    dimImg.raycastTarget = false;
                    rewardDimGroup = dimRt.GetComponent<CanvasGroup>();
                    EditorUtility.SetDirty(dimRt.gameObject);

                    // FinishedLine above the buttons
                    var subRt = canvasParent.Find("FinishedLine") as RectTransform;
                    if (subRt == null)
                    {
                        var sgo = new GameObject("FinishedLine", typeof(RectTransform), typeof(TextMeshProUGUI))
                        {
                            layer = canvasParent.gameObject.layer
                        };
                        subRt = (RectTransform)sgo.transform;
                        subRt.SetParent(canvasParent, false);
                    }
                    subRt.anchorMin = subRt.anchorMax = subRt.pivot = new Vector2(0.5f, 0f);
                    subRt.sizeDelta = new Vector2(680f, 44f);
                    subRt.anchoredPosition = new Vector2(0f, 114f);
                    finishedTmp = subRt.GetComponent<TextMeshProUGUI>();
                    finishedTmp.font = body;
                    finishedTmp.fontStyle = FontStyles.Bold;
                    finishedTmp.color = Cream;
                    finishedTmp.alignment = TextAlignmentOptions.Center;
                    finishedTmp.enableAutoSizing = true;
                    finishedTmp.fontSizeMin = 16f;
                    finishedTmp.fontSizeMax = 26f;
                    finishedTmp.raycastTarget = false;
                    finishedTmp.text = Texts[1].es;
                    var ol = subRt.GetComponent<Outline>() ?? subRt.gameObject.AddComponent<Outline>();
                    ol.effectColor = new Color(Plum.r, Plum.g, Plum.b, 0.9f);
                    ol.effectDistance = new Vector2(2f, -2f);
                    Localize(subRt.gameObject, "final.subtitle");
                    EditorUtility.SetDirty(subRt.gameObject);

                    // Restyle ResetButton ("Back to start") centered-left
                    if (resetBtn != null)
                    {
                        var brt = (RectTransform)resetBtn.transform;
                        brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(0.5f, 0f);
                        brt.sizeDelta = new Vector2(276f, 68f);
                        brt.anchoredPosition = new Vector2(-154f, 26f);
                        if (resetBtn.TryGetComponent<Image>(out var bImg) && berrySprite != null)
                        {
                            bImg.sprite = berrySprite;
                            bImg.type = Image.Type.Sliced;
                            bImg.pixelsPerUnitMultiplier = 1.6f;
                            bImg.color = Color.white;
                        }
                        var bLbl = resetBtn.GetComponentInChildren<TMP_Text>(true);
                        if (bLbl != null)
                        {
                            bLbl.font = display;
                            bLbl.fontSharedMaterial = display.material;
                            bLbl.color = Color.white;
                            bLbl.enableAutoSizing = true;
                            bLbl.fontSizeMin = 16f;
                            bLbl.fontSizeMax = 24f;
                            bLbl.alignment = TextAlignmentOptions.Center;
                            bLbl.text = Texts[2].es;
                            var lrt = (RectTransform)bLbl.transform;
                            lrt.anchorMin = Vector2.zero;
                            lrt.anchorMax = Vector2.one;
                            lrt.offsetMin = new Vector2(14f, 6f);
                            lrt.offsetMax = new Vector2(-14f, -4f);
                            Localize(bLbl.gameObject, "final.back");
                        }
                        EditorUtility.SetDirty(resetBtn.gameObject);
                    }

                    // Install DiplomaButton centered-right
                    var dipRt = canvasParent.Find("DiplomaButton") as RectTransform;
                    if (dipRt != null) UnityEngine.Object.DestroyImmediate(dipRt.gameObject);
                    var dgo = new GameObject("DiplomaButton", typeof(RectTransform), typeof(Image), typeof(Button))
                    {
                        layer = canvasParent.gameObject.layer
                    };
                    dipRt = (RectTransform)dgo.transform;
                    dipRt.SetParent(canvasParent, false);
                    dipRt.anchorMin = dipRt.anchorMax = dipRt.pivot = new Vector2(0.5f, 0f);
                    dipRt.sizeDelta = new Vector2(276f, 68f);
                    dipRt.anchoredPosition = new Vector2(154f, 26f);
                    var dImg = dgo.GetComponent<Image>();
                    dImg.sprite = lightSprite;
                    dImg.type = lightSprite != null ? Image.Type.Sliced : Image.Type.Simple;
                    dImg.pixelsPerUnitMultiplier = 1.6f;
                    dImg.color = Color.white;
                    diplomaBtn = dgo.GetComponent<Button>();
                    diplomaBtn.targetGraphic = dImg;

                    var dlGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI))
                    {
                        layer = canvasParent.gameObject.layer
                    };
                    var dlRt = (RectTransform)dlGo.transform;
                    dlRt.SetParent(dipRt, false);
                    dlRt.anchorMin = Vector2.zero;
                    dlRt.anchorMax = Vector2.one;
                    dlRt.offsetMin = new Vector2(14f, 6f);
                    dlRt.offsetMax = new Vector2(-14f, -4f);
                    var dlTmp = dlGo.GetComponent<TextMeshProUGUI>();
                    dlTmp.font = display;
                    dlTmp.fontSharedMaterial = display.material;
                    dlTmp.color = Plum;
                    dlTmp.enableAutoSizing = true;
                    dlTmp.fontSizeMin = 16f;
                    dlTmp.fontSizeMax = 24f;
                    dlTmp.alignment = TextAlignmentOptions.Center;
                    dlTmp.text = Texts[3].es;
                    dlTmp.raycastTarget = false;
                    Localize(dlGo, "diploma.open");
                    EditorUtility.SetDirty(dgo);
                }

                // 3. Tame particles & draw behind star
                foreach (var ps in roots.SelectMany(r => r.GetComponentsInChildren<ParticleSystem>(true)))
                {
                    if (ps.name != "starAssistant" && (starRoot == null || !ps.transform.IsChildOf(starRoot)))
                    {
                        var main = ps.main;
                        main.maxParticles = Mathf.Min(main.maxParticles, 30);
                        var em = ps.emission;
                        em.rateOverTimeMultiplier = Mathf.Min(em.rateOverTimeMultiplier, 12f);
                        var ren = ps.GetComponent<ParticleSystemRenderer>();
                        if (ren != null) ren.sortingOrder = -1;
                        EditorUtility.SetDirty(ps.gameObject);
                    }
                }

                // 4. Wire FinalCutscene serialized references
                var animComp = starRoot != null ? starRoot.GetComponentInChildren<Animator>(true) : null;
                var lightComp = roots.SelectMany(r => r.GetComponentsInChildren<UnityEngine.Rendering.Universal.Light2D>(true))
                    .FirstOrDefault(l => l.lightType != UnityEngine.Rendering.Universal.Light2D.LightType.Global);
                var starPs = starRoot != null ? starRoot.GetComponentInChildren<ParticleSystem>(true) : null;
                var fso = new SerializedObject(finalCutscene);
                if (starRoot != null && starRoot.TryGetComponent<SpriteRenderer>(out var sr))
                    fso.FindProperty("_sprite").objectReferenceValue = sr;
                if (lightComp != null) fso.FindProperty("_light").objectReferenceValue = lightComp;
                if (animComp != null) fso.FindProperty("_anim").objectReferenceValue = animComp;
                if (starPs != null) fso.FindProperty("_starParticleSystem").objectReferenceValue = starPs;
                fso.FindProperty("finishedLine").objectReferenceValue = finishedTmp;
                fso.FindProperty("diplomaButton").objectReferenceValue = diplomaBtn;
                fso.FindProperty("backButton").objectReferenceValue = resetBtn;
                fso.FindProperty("rewardDim").objectReferenceValue = rewardDimGroup;
                fso.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(finalCutscene);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[BakeryFinalSceneInstaller] Refined 5. FinalCutscene.unity cleanly.");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        public static void RunCi()
        {
            try { Run(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        static void Localize(GameObject go, string key)
        {
            var l = go.GetComponent<BakeryLocalizedSceneText>() ?? go.AddComponent<BakeryLocalizedSceneText>();
            l.Configure(Table, key);
            EditorUtility.SetDirty(l);
        }

        static void AddTexts()
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(Table)
                ?? throw new InvalidOperationException("Missing table " + Table);
            foreach (var table in collection.StringTables)
            {
                bool es = table.LocaleIdentifier.Code.StartsWith("es", StringComparison.OrdinalIgnoreCase);
                foreach (var (key, esText, enText) in Texts)
                    table.AddEntry(key, es ? esText : enText);
                EditorUtility.SetDirty(table);
            }
            EditorUtility.SetDirty(collection.SharedData);
            AssetDatabase.SaveAssets();
        }
    }
}
