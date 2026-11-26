using System;
using System.Collections.Generic;
using System.Linq;
using Fatcat.Slothy.Bakery.Shell;
using Fatcat.Slothy.Core.Editor;
using Fatcat.Slothy.Core.ShellUI;
using FatCat.CrossPromo;
using FatCat.Localization.UI;
using FatCat.SettingsUI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace FatCat.Slothy.Tools
{
    /// <summary>
    /// Builds the Slothy's Bakery shell (landscape, 1024x576 reference) as a serialized prefab and installs it in
    /// the Main Screen scene: the Settings screen (com.fatcat.settings-ui) and the language picker
    /// (com.fatcat.localization-ui LocalePickerController), drawn with Bakery's warm bakery palette.
    /// Texts are Unity Localization entries in the "BakeryShell" string table. Idempotent.
    /// Menu: FatCat/Slothy Bakery/Build Shell and Install
    /// Batch: -executeMethod FatCat.Slothy.Tools.BakeryShellBuilder.RunCi
    /// </summary>
    public static class BakeryShellBuilder
    {
        const string PrefabPath   = "Assets/Editor/Shell/BakeryShell.prefab";
        const string Scene        = "Assets/INART GAMES/Scenes/1. Main Screen.unity";
        const string Table        = "BakeryShell";
        const string BakeryUiDir  = "Assets/Resources/BakeryUI";
        const string FontsDir     = "Assets/Resources/Fonts";
        const string Flags        = "Packages/com.fatcat.localization-ui/Runtime/LocalizationPicker";

        // ── Option A Palette: Vitrina Pastel (plum + berry + cream + caramel) ──
        static readonly Color
            Plum        = Hex("#583553"),   // primary border & headings
            PlumMuted   = Hex("#7F5A69"),   // secondary body / subtitles
            Berry       = Hex("#E47498"),   // primary pill / accents
            BerryDark   = Hex("#C54E76"),   // interactive labels / pressed
            Caramel     = Hex("#C87432"),   // warm secondary accents
            Cream       = Hex("#FFF9F0"),   // card fill
            Vanilla     = Hex("#FDECD7"),   // secondary tile fill
            Blush       = Hex("#FFEAF1"),   // active highlight row
            PinkLine    = Hex("#EFCCD4"),   // subtle border
            SwitchOn    = Hex("#5FA864"),   // mint green toggle on
            SwitchOff   = Hex("#C9B59A");   // greyed toggle off

        static TMP_FontAsset display, body;
        static Sprite bakeryCard, pillHeader, berryButton, rounded, roundedLight, roundedPlum, circle;
        static Sprite iconCalendar, iconBook, iconClock, iconChef, iconLock, iconTrophy, iconClose, iconCheck;
        static Sprite iconGear, iconProgress, iconMusic, iconSpeaker, iconStar, iconHeart, iconShare, iconGrid, iconFlame, iconSun;
        static Sprite topicMg1, topicMg2, topicMg3;
        static Sprite[] achIcons;
        static SlothyShellBuilderKit.Theme kitTheme;

        static readonly (string key, string es, string en)[] Texts =
        {
            ("settings.title",        "Ajustes",                   "Settings"),
            ("settings.sound",        "Sonido",                    "Sound"),
            ("settings.music",        "Música",                    "Music"),
            ("settings.effects",      "Efectos",                   "Effects"),
            ("settings.game",         "Juego",                     "Game"),
            ("settings.vibration",    "Vibración",                 "Vibration"),
            ("settings.reminders",    "Recordatorio diario",       "Daily reminder"),
            ("settings.language",     "Idioma",                    "Language"),
            ("settings.change",       "Cambiar",                   "Change"),
            ("settings.more",         "Más",                       "More"),
            ("settings.achievements", "Logros",                    "Achievements"),
            ("settings.rate",         "Calificar",                 "Rate us"),
            ("settings.share",        "Compartir",                 "Share"),
            ("settings.more_games",   "Más juegos",                "More games"),
            ("settings.educator_feedback", "Ideas de docentes",    "Teacher feedback"),
            ("friends.title",         "Amigos de Slothy",          "Slothy's friends"),
            ("friends.new",           "¡Nuevo!",                   "New!"),
            ("friends.see_all",       "Ver todos los juegos",      "See all games"),
            ("language.title",        "Idioma",                    "Language"),
            ("language.choose",       "Elige tu idioma",           "Choose your language"),
            ("language.continue",     "Continuar",                 "Continue"),
            ("home.play",             "Jugar",                     "Play"),
            ("home.continue",         "Continuar",                 "Continue"),
            ("home.settings",         "Ajustes",                   "Settings"),
            ("home.progress",         "Mi progreso",               "My progress"),
            ("home.new_badge",        "¡Hoy!",                     "Today!"),
            ("moregames.title",       "Más juegos",                "More games"),
            ("moregames.subtitle",    "Más juegos de FatCat Game Studio", "More games from FatCat Game Studio"),
            ("moregames.empty",       "Pronto habrá más juegos aquí.", "More games coming soon."),
            ("moregames.view",        "Ver",                       "View"),
            ("gate.title",            "Pídele ayuda a un adulto",  "Ask a grown-up"),
            ("gate.question",         "¿Cuánto es {0} × {1}?",    "What is {0} × {1}?"),
            ("gate.hint",             "Vamos a abrir una página fuera del juego.", "This opens a page outside the game."),
            ("gate.lock",             "Solo para adultos",          "Only for grown-ups"),
            ("modes.button",          "Modos",                      "Modes"),
            ("modes.title",           "Modos de juego",             "Game Modes"),
            ("modes.practice",        "Práctica por tema",          "Practice by topic"),
            ("modes.practice_short",  "Práctica",                   "Practice"),
            ("modes.runs",            "Desafíos",                   "Challenges"),
            ("modes.mg1",             "Comparar pasos",             "Step comparison"),
            ("modes.mg2",             "Elegir signo",               "Choose sign"),
            ("modes.mg3",             "Escribir fracción",          "Write fraction"),
            ("modes.daily",           "Reto diario",                "Daily challenge"),
            ("modes.afternoon",       "Turno de tarde",             "Afternoon shift"),
            ("modes.chef",            "Modo Chef",                  "Chef Mode"),
            ("practice.subtitle",     "Elige un tema para repasar sin prisas", "Pick a topic to practice at your own pace"),
            ("practice.mg1.desc",     "Compara denominadores antes de elegir el signo", "Compare denominators before picking a sign"),
            ("practice.mg2.desc",     "Elige mayor, menor o igual entre dos porciones", "Pick greater, less, or equal between two trays"),
            ("practice.mg3.desc",     "Arma la fracción equivalente de cada receta", "Build an equivalent fraction for each recipe"),
            ("progress.title",        "Mi progreso",                "My progress"),
            ("dash.t_story",          "Historia",                   "Story"),
            ("dash.t_afternoon",      "Turno de tarde",             "Afternoon shift"),
            ("dash.t_chef",           "Modo chef",                  "Chef mode"),
            ("dash.t_daily",          "Reto diario",                "Daily challenge"),
            ("dash.t_practice",       "Práctica",                   "Practice"),
            ("dash.t_achievements",   "Logros",                     "Achievements"),
            ("dash.story_done",       "¡Completada!",               "Finished!"),
            ("dash.story_now",        "Vas en: {0}",                "Now: {0}"),
            ("dash.times",            "Jugado {0} veces",           "Played {0} times"),
            ("dash.open",             "¡Abierto! Pruébalo",         "Open: try it!"),
            ("dash.locked_story",     "Termina la historia para abrirlo", "Finish the story to open it"),
            ("dash.locked_afternoon", "Termina el Turno de tarde para abrirlo", "Finish the afternoon shift to open it"),
            ("dash.achievements",     "{0} de {1} conseguidos",     "{0} of {1} earned"),
            ("dash.see_achievements", "Ver logros",                 "See achievements"),
            ("achievements.title",    "Logros",                     "Achievements"),
            ("achievements.done",     "¡Logrado!",                  "Earned!"),
            ("achievements.summary",  "{0} de {1} logros desbloqueados", "{0} of {1} achievements unlocked"),
            ("achievement.progress", "Logros desbloqueados",       "Achievements unlocked"),
            ("achievement.toast", "¡Logro desbloqueado!",          "Achievement unlocked!"),
            ("feedback.streak", "¡Racha ×{0}!",                    "Streak ×{0}!"),
            ("achievement.unlocked", "Desbloqueado",               "Unlocked"),
            ("achievement.first.title", "Primer pedido",           "First Order"),
            ("achievement.first.body", "Completa tu primer minijuego.", "Complete your first minigame."),
            ("achievement.orders.title", "Panadería en marcha",    "Bakery in Motion"),
            ("achievement.orders.body", "Completa 10 minijuegos.", "Complete 10 minigames."),
            ("achievement.perfect.title", "Pedido perfecto",        "Perfect Order"),
            ("achievement.perfect.body", "Completa un minijuego sin errores.", "Complete a minigame without mistakes."),
            ("achievement.practice.title", "Primera práctica",      "First Practice"),
            ("achievement.practice.body", "Completa una práctica por tema.", "Complete a topic practice."),
            ("achievement.practice3.title", "Prueba cada receta",   "Try Every Recipe"),
            ("achievement.practice3.body", "Practica los tres minijuegos.", "Practice all three minigames."),
            ("achievement.daily.title", "Hábito diario",           "Daily Baker"),
            ("achievement.daily.body", "Completa un reto diario.", "Complete a daily challenge."),
            ("achievement.daily5.title", "Cliente habitual",       "Regular Customer"),
            ("achievement.daily5.body", "Completa cinco retos diarios.", "Complete five daily challenges."),
            ("achievement.shift.title", "Turno de tarde",          "Afternoon Shift"),
            ("achievement.shift.body", "Completa un turno de tarde.", "Complete an afternoon shift."),
            ("achievement.chef.title", "A prueba de chef",          "Chef's Choice"),
            ("achievement.chef.body", "Completa una partida en Modo Chef.", "Complete a Chef Mode run."),
            ("achievement.days.title", "De vuelta en la panadería", "Back at the Bakery"),
            ("achievement.days.body", "Juega en tres días distintos.", "Play on three different days."),
            ("moregames.visit_store", "Ver en tienda",              "View in store"),
            ("summary.ok",            "Continuar",                  "Continue"),
            ("diploma.open",          "Ver mi diploma",             "See my diploma"),
            ("diploma.kicker",        "DIPLOMA DE",                 "CERTIFICATE"),
            ("diploma.title",         "Asistente estrella",         "Star assistant"),
            ("diploma.awarded",       "Otorgado a",                 "Awarded to"),
            ("diploma.name_hint",     "Toca y escribe tu nombre",   "Tap to write your name"),
            ("diploma.store",         "¡Juega con Slothy!",         "Play with Slothy!"),
            ("diploma.share",         "Compartir",                  "Share"),
            ("diploma.save",          "Guardar",                    "Save"),
            ("diploma.close",         "Cerrar",                     "Close"),
            ("diploma.share_text",    "¡Mi diploma de Slothy's Bakery!", "My Slothy's Bakery diploma!"),
            ("reminder.title",         "Slothy te espera en la panadería", "Slothy is waiting at the bakery"),
            ("reminder.daily",         "Hay una nueva actividad de fracciones para ti. ¿Te animas?", "There is a new fractions activity for you. Want to try it?"),
            ("reminder.miss",          "Slothy extraña ayudarte con los pedidos.", "Slothy misses your help with today's orders."),
            ("reminder.comeback",      "La panadería sigue abierta. Slothy guardó algo para ti.", "The bakery is still open. Slothy saved something for you."),
            ("reminder.channel",       "Un recordatorio amable para volver a jugar.", "A gentle reminder to come back and play."),
            ("minigame.mg1.prompt.0", "<bounce>¿Se pueden comparar sus denominadores?</bounce>", "<bounce>Are their denominators comparable?</bounce>"),
            ("minigame.mg1.prompt.1", "<bounce>¿Por qué fracción debemos multiplicar la primera para que los denominadores sean comparables?</bounce>", "<bounce>What fraction must we multiply the first fraction by to make the denominators comparable?</bounce>"),
            ("minigame.mg1.prompt.2", "<bounce>¿Cuál es el signo de comparación correcto?</bounce>", "<bounce>What is the correct comparison sign?</bounce>"),
            ("minigame.mg3.prompt.0", "<bounce>¿Qué fracción podría ser equivalente a las porciones de pan que se muestran?</bounce>", "<bounce>What fraction could be equivalent to the pieces of bread shown?</bounce>"),
            ("minigame.mg3.prompt.1", "<bounce>¿Qué fracción es mayor que la porción de postre que se muestra?</bounce>", "<bounce>What fraction is greater than the piece of dessert shown?</bounce>"),
            ("minigame.mg3.prompt.2", "<bounce>¿Qué fracción es menor que la porción de postre que se muestra?</bounce>", "<bounce>What fraction is less than the piece of dessert shown?</bounce>"),
        };

        static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

        [MenuItem("FatCat/Slothy Bakery/Build Shell and Install")]
        public static void BuildAndInstall()
        {
            PrepareAssets();
            EnsureStringTable();
            EnsurePlayerPrefSelector();
            BuildParentalGate();
            BuildAchievementToast();
            BuildDiploma();
            var prefab = BuildShell();
            Install(prefab);
            Debug.Log("[BakeryShellBuilder] Shell built and installed.");
        }

        static void BuildDiploma()
        {
            var portrait = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/INART GAMES/Art/Textures/UI/perezoso_repostero.png");
            var seal = Load<Sprite>($"{BakeryUiDir}/diploma_seal.png");
            var star = Load<Sprite>($"{BakeryUiDir}/diploma_star.png");
            var play = Load<Sprite>($"{BakeryUiDir}/diploma_play.png");
            SlothyShellBuilderKit.BuildDiplomaPrefab(
                kitTheme,
                "Assets/Resources/UI/BakeryDiploma.prefab",
                portrait,
                seal,
                star,
                play,
                new[] { topicMg1, topicMg2, topicMg3 },
                "Slothy's\nBakery",
                new[] { "Comparar pasos", "Elegir signo", "Escribir fracción" });
        }

        [MenuItem("FatCat/Slothy Bakery/Reset Local QA Progress (PlayerPrefs)")]
        public static void ResetLocalQaPrefs()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("[BakeryShellBuilder] Cleared all local PlayerPrefs for fresh QA launch.");
        }

        public static void RunCi()
        {
            try { BuildAndInstall(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        // ────────────────────────────────────────────── assets & localization

        static T Load<T>(string path) where T : UnityEngine.Object =>
            AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("Missing " + path);

        static void PrepareAssets()
        {
            ConfigureKitSprites();

            var fallbackPath = AssetDatabase.FindAssets("LiberationSans SDF t:TMP_FontAsset",
                                   new[] { "Assets/INART GAMES/TextMesh Pro" })
                                   .Select(AssetDatabase.GUIDToAssetPath)
                                   .FirstOrDefault(p => !p.Contains("Fallback") && !p.Contains("Outline") && !p.Contains("Shadow"))
                               ?? "Assets/INART GAMES/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
            var fallbackFont = Load<TMP_FontAsset>(fallbackPath);

            display = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{FontsDir}/LilitaOne-Regular SDF.asset") ?? fallbackFont;
            body = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{FontsDir}/Fredoka-VariableFont_wdth,wght SDF.asset") ?? fallbackFont;
            EnsureFallback(display, fallbackFont);
            EnsureFallback(body, fallbackFont);

            bakeryCard   = Load<Sprite>($"{BakeryUiDir}/ui_bakery_card.png");
            pillHeader   = Load<Sprite>($"{BakeryUiDir}/ui_pill_header.png");
            berryButton  = Load<Sprite>($"{BakeryUiDir}/ui_bakery_btn_berry.png");
            rounded      = Load<Sprite>($"{BakeryUiDir}/ui_rounded.png");
            roundedLight = Load<Sprite>($"{BakeryUiDir}/ui_rounded_light.png");
            roundedPlum  = Load<Sprite>($"{BakeryUiDir}/ui_rounded_plum.png");
            circle       = Load<Sprite>($"{BakeryUiDir}/ui_circle.png");
            iconCalendar = Load<Sprite>($"{BakeryUiDir}/icon_calendar.png");
            iconBook     = Load<Sprite>($"{BakeryUiDir}/icon_book.png");
            iconClock    = Load<Sprite>($"{BakeryUiDir}/icon_clock.png");
            iconChef     = Load<Sprite>($"{BakeryUiDir}/icon_chef.png");
            iconLock     = Load<Sprite>($"{BakeryUiDir}/icon_lock.png");
            iconTrophy   = Load<Sprite>($"{BakeryUiDir}/icon_trophy.png");
            iconClose    = Load<Sprite>($"{BakeryUiDir}/icon_close.png");
            iconCheck    = Load<Sprite>($"{BakeryUiDir}/icon_check.png");
            iconGear     = Load<Sprite>($"{BakeryUiDir}/icon_gear.png");
            iconProgress = Load<Sprite>($"{BakeryUiDir}/icon_progress.png");
            iconMusic    = Load<Sprite>($"{BakeryUiDir}/icon_music.png");
            iconSpeaker  = Load<Sprite>($"{BakeryUiDir}/icon_speaker.png");
            iconStar     = Load<Sprite>($"{BakeryUiDir}/icon_star.png");
            iconHeart    = Load<Sprite>($"{BakeryUiDir}/icon_heart.png");
            iconShare    = Load<Sprite>($"{BakeryUiDir}/icon_share.png");
            iconGrid     = Load<Sprite>($"{BakeryUiDir}/icon_grid.png");
            iconFlame    = Load<Sprite>($"{BakeryUiDir}/icon_flame.png");
            iconSun      = Load<Sprite>($"{BakeryUiDir}/icon_sun.png");
            topicMg1     = Load<Sprite>($"{BakeryUiDir}/topic_mg1.png");
            topicMg2     = Load<Sprite>($"{BakeryUiDir}/topic_mg2.png");
            topicMg3     = Load<Sprite>($"{BakeryUiDir}/topic_mg3.png");
            achIcons     = new[]
            {
                Load<Sprite>($"{BakeryUiDir}/ach_order.png"),
                Load<Sprite>($"{BakeryUiDir}/ach_tray.png"),
                Load<Sprite>($"{BakeryUiDir}/ach_perfect.png"),
                Load<Sprite>($"{BakeryUiDir}/ach_recipe.png"),
                Load<Sprite>($"{BakeryUiDir}/ach_all_topics.png"),
                Load<Sprite>($"{BakeryUiDir}/ach_daily.png"),
                Load<Sprite>($"{BakeryUiDir}/ach_daily5.png"),
                Load<Sprite>($"{BakeryUiDir}/ach_shift.png"),
                Load<Sprite>($"{BakeryUiDir}/ach_chef.png"),
                Load<Sprite>($"{BakeryUiDir}/ach_days.png"),
            };

            kitTheme = new SlothyShellBuilderKit.Theme
            {
                Ink = Plum,
                InkMuted = PlumMuted,
                Accent = Berry,
                AccentDark = BerryDark,
                Cream = Cream,
                Blush = Blush,
                Mint = SwitchOn,
                SwitchOff = SwitchOff,
                DisplayFont = display,
                BodyFont = body,
                CardSprite = bakeryCard,
                PillHeaderSprite = pillHeader,
                PrimaryButtonSprite = berryButton,
                SoftRowSprite = roundedLight,
                PlumRowSprite = roundedPlum,
                RoundedSprite = rounded,
                CircleSprite = circle,
                CloseIcon = iconClose,
            };
        }

        static void ConfigureKitSprites()
        {
            var borders = new Dictionary<string, Vector4>
            {
                { "ui_bakery_card.png", new Vector4(48, 48, 48, 48) },
                { "ui_rounded.png", new Vector4(30, 30, 30, 30) },
                { "ui_rounded_light.png", new Vector4(28, 28, 28, 28) },
                { "ui_rounded_plum.png", new Vector4(30, 32, 30, 30) },
                { "ui_pill_header.png", new Vector4(42, 24, 42, 24) },
                { "ui_bakery_btn_berry.png", new Vector4(58, 28, 36, 30) },
            };
            SlothyShellBuilderKit.ConfigureSingleSprites(BakeryUiDir, borders);
            SlothyShellBuilderKit.ConfigureSingleSprites("Assets/Art/OtherGames", null);
        }

        static void EnsureFallback(TMP_FontAsset font, TMP_FontAsset fallback)
        {
            if (font == null || fallback == null || font == fallback) return;
            if (font.fallbackFontAssetTable == null)
                font.fallbackFontAssetTable = new List<TMP_FontAsset>();
            if (!font.fallbackFontAssetTable.Contains(fallback))
            {
                font.fallbackFontAssetTable.Add(fallback);
                EditorUtility.SetDirty(font);
                AssetDatabase.SaveAssets();
            }
        }

        static void EnsureStringTable()
        {
            if (LocalizationEditorSettings.ActiveLocalizationSettings == null)
            {
                var locSettings = AssetDatabase.LoadAssetAtPath<UnityEngine.Localization.Settings.LocalizationSettings>(
                    "Assets/Localization/Spanish.asset");
                if (locSettings != null)
                    LocalizationEditorSettings.ActiveLocalizationSettings = locSettings;
            }
            var collection = LocalizationEditorSettings.GetStringTableCollection(Table);
            if (collection == null)
                collection = LocalizationEditorSettings.CreateStringTableCollection(
                    Table, "Assets/Localization/Tables", LocalizationEditorSettings.GetLocales());
            foreach (var locale in LocalizationEditorSettings.GetLocales())
                if (collection.GetTable(locale.Identifier) == null)
                    collection.AddNewTable(locale.Identifier);
            foreach (var table in collection.StringTables)
            {
                bool es = table.LocaleIdentifier.Code.StartsWith("es");
                foreach (var (key, esText, enText) in Texts)
                    table.AddEntry(key, es ? esText : enText);
                EditorUtility.SetDirty(table);
            }
            EditorUtility.SetDirty(collection.SharedData);
            EditorUtility.SetDirty(collection);
            AssetDatabase.SaveAssets();
        }

        static void EnsurePlayerPrefSelector()
        {
            var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
            if (settings == null) return;
            var selectors = settings.GetStartupLocaleSelectors();
            if (selectors.Any(s => s is PlayerPrefLocaleSelector)) return;
            int index = selectors.FindIndex(s => s is CommandLineLocaleSelector) + 1;
            selectors.Insert(Mathf.Max(0, index), new PlayerPrefLocaleSelector { PlayerPreferenceKey = "selected-locale" });
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        // ─────────────────────────────────────────────────────── primitives

        static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = LayerMask.NameToLayer("UI") };
            var rt = (RectTransform)go.transform;
            if (parent != null) rt.SetParent(parent, false);
            return rt;
        }

        static void Stretch(RectTransform rt, float l = 0, float r = 0, float t = 0, float b = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
        }

        static void Place(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 pos)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.sizeDelta = size; rt.anchoredPosition = pos;
        }

        static Image Paint(RectTransform rt, Color color, Sprite sprite = null, bool sliced = false, float ppu = 1f)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite; img.color = color; img.raycastTarget = false;
            if (sliced) { img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = ppu; }
            return img;
        }

        static LayoutElement Size(Component c, float w = -1, float h = -1, float flexW = -1)
        {
            var le = c.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            if (w >= 0) { le.minWidth = w; le.preferredWidth = w; }
            if (h >= 0) { le.minHeight = h; le.preferredHeight = h; }
            if (flexW >= 0) le.flexibleWidth = flexW;
            return le;
        }

        static VerticalLayoutGroup Column(RectTransform rt, float gap, RectOffset pad = null)
        {
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = gap; v.padding = pad ?? new RectOffset();
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            return v;
        }

        static HorizontalLayoutGroup Row(RectTransform rt, float gap, RectOffset pad = null,
            TextAnchor align = TextAnchor.MiddleLeft)
        {
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = gap; h.padding = pad ?? new RectOffset(); h.childAlignment = align;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = false; h.childForceExpandHeight = false;
            return h;
        }

        static TextMeshProUGUI Text(Transform parent, string name, string key, bool useDisplay, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Left, string plain = null)
        {
            var rt = Node(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = useDisplay ? display : body;
            t.fontSize = size; t.color = color; t.alignment = align; t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.enableAutoSizing = true; t.fontSizeMax = size; t.fontSizeMin = size * 0.6f;
            var entry = key != null ? Texts.First(x => x.key == key) : default;
            t.text = key != null ? entry.es : plain ?? "";
            if (key != null)
            {
                var ev = rt.gameObject.AddComponent<LocalizeStringEvent>();
                ev.StringReference.SetReference(Table, key);
                var setter = (UnityAction<string>)Delegate.CreateDelegate(
                    typeof(UnityAction<string>), t, "set_text");
                UnityEventTools.AddPersistentListener(ev.OnUpdateString, setter);
            }
            return t;
        }

        static Button MakeButton(GameObject go, Graphic target)
        {
            target.raycastTarget = true;
            var b = go.AddComponent<Button>();
            b.targetGraphic = target;
            var colors = b.colors;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            b.colors = colors;
            return b;
        }

        static RectTransform Section(Transform parent, string key)
        {
            var t = Text(parent, "Section_" + key, key, true, 19, BerryDark);
            Size(t, -1, 26);
            return (RectTransform)t.transform;
        }

        static RectTransform RowBox(Transform parent, string name, float height)
        {
            var rt = Node(name, parent);
            var bg = Paint(rt, Color.white, roundedLight ?? roundedPlum, true, 1.8f);
            Size(bg, -1, height);
            Row(rt, 12, new RectOffset(14, 14, 6, 6));
            return rt;
        }

        static Slider VolumeRow(Transform parent, string id, string key, Sprite icon = null)
        {
            var row = RowBox(parent, "Volume_" + id, 48);
            if (icon != null)
            {
                var badge = Node("IconBg", row);
                Size(badge.gameObject.AddComponent<LayoutElement>(), 30, 30);
                Paint(badge, Blush, circle).preserveAspect = true;
                var ico = Node("Icon", badge);
                Stretch(ico, 5, 5, 5, 5);
                Paint(ico, Color.white, icon).preserveAspect = true;
            }
            var label = Text(row, "Label", key, false, 16, Plum);
            Size(label, 84);
            var sl = Node("Slider", row);
            Size(sl.gameObject.AddComponent<LayoutElement>(), -1, 28, 1);
            var slider = sl.gameObject.AddComponent<Slider>();
            var track = Node("Track", sl);
            track.anchorMin = new Vector2(0, 0.5f); track.anchorMax = new Vector2(1, 0.5f);
            track.sizeDelta = new Vector2(0, 12);
            Paint(track, Blush, rounded, true, 3f);
            var fillArea = Node("FillArea", sl);
            fillArea.anchorMin = new Vector2(0, 0.5f); fillArea.anchorMax = new Vector2(1, 0.5f);
            fillArea.sizeDelta = new Vector2(0, 12);
            var fill = Node("Fill", fillArea);
            fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0, 1); fill.sizeDelta = Vector2.zero;
            Paint(fill, Berry, rounded, true, 3f);
            var handleArea = Node("HandleArea", sl); Stretch(handleArea, 12, 12);
            var handle = Node("Handle", handleArea);
            handle.anchorMin = new Vector2(0, 0); handle.anchorMax = new Vector2(0, 1);
            handle.sizeDelta = new Vector2(24, 0);
            var knob = Paint(handle, Plum, circle); knob.raycastTarget = true; knob.preserveAspect = true;
            var inner = Node("Inner", handle);
            Place(inner, new Vector2(0.5f, 0.5f), new Vector2(14, 14), Vector2.zero);
            Paint(inner, Color.white, circle).preserveAspect = true;
            slider.fillRect = fill; slider.handleRect = handle; slider.targetGraphic = knob;
            slider.minValue = 0; slider.maxValue = 1; slider.value = 0.7f;
            return slider;
        }

        static (Toggle toggle, RectTransform knob, Image track) SwitchRow(Transform parent, string id, string key)
        {
            var row = RowBox(parent, "Switch_" + id, 48);
            var label = Text(row, "Label", key, false, 17, Plum);
            Size(label, -1, -1, 1);
            var sw = Node("Switch", row);
            var track = Paint(sw, SwitchOn, rounded, true, 3f);
            Size(track, 54, 30);
            var toggle = sw.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = track; track.raycastTarget = true;
            toggle.transition = Selectable.Transition.None;
            var knob = Node("Knob", sw);
            Paint(knob, Color.white, circle).preserveAspect = true;
            Place(knob, new Vector2(1, 0.5f), new Vector2(22, 22), new Vector2(-4, 0));
            return (toggle, knob, track);
        }

        static Button Tile(Transform parent, string key, Sprite icon = null)
        {
            var t = Node("Tile_" + key, parent);
            var bg = Paint(t, Color.white, roundedLight ?? roundedPlum, true, 1.8f);
            Row(t, 10, new RectOffset(10, 10, 6, 6));
            var tileSize = Size(bg, -1, 48, 1);
            tileSize.minWidth = 0; tileSize.preferredWidth = 1;
            var b = MakeButton(t.gameObject, bg);
            if (icon != null)
            {
                var badge = Node("IconBg", t);
                Size(badge.gameObject.AddComponent<LayoutElement>(), 30, 30);
                Paint(badge, Blush, circle).preserveAspect = true;
                var ico = Node("Icon", badge);
                Stretch(ico, 5, 5, 5, 5);
                Paint(ico, Color.white, icon).preserveAspect = true;
            }
            var label = Text(t, "Label", key, false, 14, Plum);
            Size(label, -1, -1, 1);
            return b;
        }

        static Button CloseButton(Transform parent)
        {
            var rt = Node("CloseButton", parent);
            var bg = Paint(rt, Plum, circle); bg.preserveAspect = true;
            var inner = Node("Fill", rt); Stretch(inner, 3, 3, 3, 3);
            Paint(inner, Berry, circle).preserveAspect = true;
            var b = MakeButton(rt.gameObject, bg);
            var iconRt = Node("X", rt); Stretch(iconRt, 11, 11, 11, 11);
            Paint(iconRt, Color.white, iconClose).preserveAspect = true;
            return b;
        }

        static RectTransform Card(Transform parent, Vector2 size, float y)
        {
            var c = Node("Card", parent);
            Place(c, new Vector2(0.5f, 0.5f), size, new Vector2(0, y));
            var outer = Paint(c, Color.white, bakeryCard, true, 1.4f);
            outer.raycastTarget = true;
            return c;
        }

        static RectTransform HeaderPill(Transform parent, string key, float width, float y = -18, string plain = null)
        {
            var pill = Node("TitleBand", parent);
            Place(pill, new Vector2(0.5f, 1f), new Vector2(width, 52), new Vector2(0, y));
            Paint(pill, Color.white, pillHeader, true, 1.6f);
            var txt = Text(pill, "Title", key, true, 28, Color.white, TextAlignmentOptions.Center, plain);
            Stretch((RectTransform)txt.transform, 20, 20, 4, 6);
            return pill;
        }

        static Button BerryCtaButton(Transform parent, string name, string key, Vector2 anchor, Vector2 size, Vector2 pos, float fontSize = 21)
        {
            var rt = Node(name, parent);
            Place(rt, anchor, size, pos);
            var bg = Paint(rt, Color.white, berryButton, true, 1.6f);
            var btn = MakeButton(rt.gameObject, bg);
            var lbl = Text(rt, "Label", key, true, fontSize, Color.white, TextAlignmentOptions.Center);
            Stretch((RectTransform)lbl.transform, 14, 14, 4, 8);
            return btn;
        }

        // ──────────────────────────────────────────────────────── screens

        static GameObject BuildShell()
        {
            var root = Node("BakeryShellCanvas", null);
            var canvas = root.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = root.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1024, 576);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            root.gameObject.AddComponent<GraphicRaycaster>();

            var progressBtn = BuildTopCircleAction(root, "HomeProgressButton", -108f, iconProgress, "home.progress");
            var soundBtn    = BuildTopCircleAction(root, "SoundBtn",            -20f, iconGear,     "home.settings");
            var homeDock = BuildHomeModesDock(root);
            var picker = BuildLanguagePicker(root);
            var modesView = BuildModesAndModals(root, homeDock, progressBtn);
            BuildFriendsShelf(root, modesView, EnsureCrossPromoCatalog());
            var settingsView = BuildSettings(root, picker, modesView);
            UnityEventTools.AddPersistentListener(soundBtn.onClick, settingsView.Open);
            picker.transform.SetAsLastSibling();

            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PrefabPath));
            var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root.gameObject);
            return prefab;
        }

        static LocalePickerController BuildLanguagePicker(RectTransform root)
        {
            var s = Node("LanguagePickerScreen", root);
            Stretch(s);
            var dim = Paint(s, new Color(0.21f, 0.11f, 0.19f, 0.58f));
            dim.raycastTarget = true;
            var group = s.gameObject.AddComponent<CanvasGroup>();

            var card = Card(s, new Vector2(540, 360), 0);
            HeaderPill(card, "language.title", 280, -22);

            var subtitle = Text(card, "Subtitle", "language.choose", false, 18, PlumMuted, TextAlignmentOptions.Center);
            var srt = (RectTransform)subtitle.transform;
            Place(srt, new Vector2(0.5f, 1f), new Vector2(460, 28), new Vector2(0, -82));

            // Language list area
            var listArea = Node("LanguageList", card);
            Place(listArea, new Vector2(0.5f, 0.5f), new Vector2(460, 134), new Vector2(0, -4));
            Column(listArea, 12, new RectOffset(8, 8, 4, 4));
            var toggleGroup = listArea.gameObject.AddComponent<ToggleGroup>();
            toggleGroup.allowSwitchOff = false;

            var esFlag = AssetDatabase.LoadAssetAtPath<Sprite>($"{Flags}/Spanish.png");
            var enFlag = AssetDatabase.LoadAssetAtPath<Sprite>($"{Flags}/UK.png");
            var spanishToggle = LanguageToggle(listArea, "Spanish", "Español", esFlag, toggleGroup);
            var englishToggle = LanguageToggle(listArea, "English", "English", enFlag, toggleGroup);

            // Continue button
            var btnBtn = BerryCtaButton(card, "ContinueButton", "language.continue", new Vector2(0.5f, 0f), new Vector2(236, 54), new Vector2(0, 24), 22);

            var picker = s.gameObject.AddComponent<LocalePickerController>();
            var gate = root.gameObject.AddComponent<BakeryLanguageGate>();
            var gateSo = new SerializedObject(gate);
            gateSo.FindProperty("picker").objectReferenceValue = picker;
            gateSo.ApplyModifiedPropertiesWithoutUndo();

            var so = new SerializedObject(picker);
            so.FindProperty("panelRoot").objectReferenceValue = s.gameObject;
            so.FindProperty("toggleGroup").objectReferenceValue = toggleGroup;
            var toggles = so.FindProperty("toggles");
            toggles.arraySize = 2;
            toggles.GetArrayElementAtIndex(0).objectReferenceValue = spanishToggle;
            toggles.GetArrayElementAtIndex(1).objectReferenceValue = englishToggle;
            var languageKeys = so.FindProperty("languageKeys");
            languageKeys.arraySize = 2;
            languageKeys.GetArrayElementAtIndex(0).stringValue = "ES";
            languageKeys.GetArrayElementAtIndex(1).stringValue = "EN";
            so.ApplyModifiedPropertiesWithoutUndo();

            // Keep both LoadGame() and button clicks invoking gate.Confirm()
            var confirmMethod = typeof(BakeryLanguageGate).GetMethod(nameof(BakeryLanguageGate.Confirm));
            var onContinueField = typeof(LocalePickerController).GetField("onContinue",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var onContinueEvent = onContinueField?.GetValue(picker) as UnityEngine.Events.UnityEvent;
            if (onContinueEvent != null && confirmMethod != null)
            {
                UnityEventTools.AddPersistentListener(onContinueEvent,
                    (UnityAction)Delegate.CreateDelegate(typeof(UnityAction), gate, confirmMethod));
                EditorUtility.SetDirty(picker);
            }
            UnityEventTools.AddPersistentListener(btnBtn.onClick, picker.LoadGame);

            s.gameObject.SetActive(false);
            return picker;
        }

        static Toggle LanguageToggle(Transform parent, string id, string label, Sprite flag, ToggleGroup group)
        {
            var row = Node("Language_" + id, parent);
            var bg = Paint(row, Color.white, roundedLight ?? roundedPlum, true, 1.8f);
            bg.raycastTarget = true;
            Row(row, 12, new RectOffset(16, 16, 6, 6));
            Size(bg, -1, 56);

            var toggle = row.gameObject.AddComponent<Toggle>();
            toggle.group = group;
            toggle.targetGraphic = bg;
            toggle.transition = Selectable.Transition.ColorTint;
            var colors = toggle.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Blush;
            colors.pressedColor = PinkLine;
            colors.selectedColor = Blush;
            toggle.colors = colors;

            if (flag != null)
            {
                var flagRt = Node("Flag", row);
                var flagImg = Paint(flagRt, Color.white, flag);
                flagImg.preserveAspect = true;
                Size(flagImg, 36, 36);
            }

            var text = Text(row, "Label", null, false, 20, Plum, plain: label);
            Size(text, -1, -1, 1);

            var indicator = Node("Selected", row);
            var indImg = Paint(indicator, SwitchOn, rounded, true, 3f);
            indImg.raycastTarget = false;
            Size(indImg, 28, 28);
            var checkRt = Node("Check", indicator); Stretch(checkRt, 5, 5, 5, 5);
            Paint(checkRt, Color.white, iconCheck).preserveAspect = true;
            toggle.graphic = indImg;
            toggle.isOn = id == "Spanish";
            return toggle;
        }

        static SettingsScreenView BuildSettings(RectTransform root, LocalePickerController picker, BakeryModesView modesView)
        {
            var s = Node("SettingsScreen", root);
            Stretch(s);
            var dim = Paint(s, new Color(0.21f, 0.11f, 0.19f, 0.62f));
            dim.raycastTarget = true;
            var group = s.gameObject.AddComponent<CanvasGroup>();

            var card = Card(s, new Vector2(820, 456), -10);
            HeaderPill(card, "settings.title", 340, -20);

            var close = CloseButton(card);
            Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(44, 44), new Vector2(-16, -16));

            // Two-column layout
            var cols = Node("Columns", card);
            Stretch(cols, 44, 44, 76, 20);
            Row(cols, 28, null, TextAnchor.UpperLeft).childForceExpandHeight = false;
            var left = Node("Left", cols);
            Column(left, 8);
            Size(left.gameObject.AddComponent<LayoutElement>(), -1, -1, 1);
            var right = Node("Right", cols);
            Column(right, 7);
            Size(right.gameObject.AddComponent<LayoutElement>(), -1, -1, 1);

            // Left column: Sound + Game
            Section(left, "settings.sound");
            var music   = VolumeRow(left, "music",   "settings.music",   iconMusic);
            var effects = VolumeRow(left, "effects", "settings.effects", iconSpeaker);
            Section(left, "settings.game");
            var vib = SwitchRow(left, "vibration", "settings.vibration");
            var rem = SwitchRow(left, "reminders",  "settings.reminders");

            // Right column: Language + More
            Section(right, "settings.language");
            var langRow = RowBox(right, "LanguageRow", 52);
            var langButton = MakeButton(langRow.gameObject, langRow.GetComponent<Image>());
            var flagRt = Node("Flag", langRow);
            var esFlag = AssetDatabase.LoadAssetAtPath<Sprite>($"{Flags}/Spanish.png");
            var enFlag = AssetDatabase.LoadAssetAtPath<Sprite>($"{Flags}/UK.png");
            var flagImg = Paint(flagRt, Color.white, esFlag);
            flagImg.preserveAspect = true;
            Size(flagImg, 36, 36);
            var langName = Text(langRow, "Name", null, false, 17, Plum, plain: "Español");
            Size(langName, -1, -1, 1);
            var changeLbl = Text(langRow, "Change", "settings.change", false, 15, BerryDark, TextAlignmentOptions.Right);
            Size(changeLbl, 90);

            var langLabel = langRow.gameObject.AddComponent<BakeryLanguageLabel>();
            var lso = new SerializedObject(langLabel);
            lso.FindProperty("label").objectReferenceValue = langName;
            lso.FindProperty("flag").objectReferenceValue = flagImg;
            lso.FindProperty("spanishFlag").objectReferenceValue = esFlag;
            lso.FindProperty("englishFlag").objectReferenceValue = enFlag;
            lso.ApplyModifiedPropertiesWithoutUndo();

            Section(right, "settings.more");
            var grid = Node("Shortcuts", right);
            Column(grid, 8);
            var row1 = Node("Row1", grid); Row(row1, 12).childForceExpandWidth = true;
            var row2 = Node("Row2", grid); Row(row2, 12).childForceExpandWidth = true;
            var row3 = Node("Row3", grid); Row(row3, 12).childForceExpandWidth = true;
            var btnAch      = Tile(row1, "settings.achievements", iconTrophy);
            var btnRate     = Tile(row1, "settings.rate",         iconStar);
            var btnShare    = Tile(row2, "settings.share",        iconShare);
            var btnMore     = Tile(row2, "settings.more_games",   iconGrid);
            var btnEducator = Tile(row3, "settings.educator_feedback", iconHeart);

            var version = Text(right, "Version", null, false, 13, PlumMuted,
                TextAlignmentOptions.Right, "v" + Application.version);
            Size(version, -1, 20);

            // Wire SettingsScreenView + BakerySettingsBackend
            var view    = s.gameObject.AddComponent<SettingsScreenView>();
            var backend = s.gameObject.AddComponent<BakerySettingsBackend>();
            var gate    = root.GetComponent<BakeryLanguageGate>() ?? root.GetComponentInChildren<BakeryLanguageGate>(true);
            var bso = new SerializedObject(backend);
            bso.FindProperty("languagePicker").objectReferenceValue = picker;
            bso.FindProperty("languageGate").objectReferenceValue = gate;
            bso.FindProperty("modesView").objectReferenceValue = modesView;
            bso.ApplyModifiedPropertiesWithoutUndo();

            var vs = new SerializedObject(view);
            vs.FindProperty("backend").objectReferenceValue     = backend;
            vs.FindProperty("closeButton").objectReferenceValue = close;
            vs.FindProperty("canvasGroup").objectReferenceValue = group;
            vs.FindProperty("panel").objectReferenceValue       = card;
            vs.FindProperty("versionText").objectReferenceValue = version;

            var volumes = vs.FindProperty("volumes");
            volumes.arraySize = 2;
            SetVolumeBinding(volumes.GetArrayElementAtIndex(0), "music", music);
            SetVolumeBinding(volumes.GetArrayElementAtIndex(1), "effects", effects);

            var switches = vs.FindProperty("switches");
            switches.arraySize = 2;
            SetSwitchBinding(switches.GetArrayElementAtIndex(0), "vibration", vib.toggle, vib.knob, vib.track);
            SetSwitchBinding(switches.GetArrayElementAtIndex(1), "reminders", rem.toggle, rem.knob, rem.track);

            var shortcuts = vs.FindProperty("shortcuts");
            shortcuts.arraySize = 6;
            SetShortcutBinding(shortcuts.GetArrayElementAtIndex(0), "language", langButton);
            SetShortcutBinding(shortcuts.GetArrayElementAtIndex(1), "achievements", btnAch);
            SetShortcutBinding(shortcuts.GetArrayElementAtIndex(2), "rate", btnRate);
            SetShortcutBinding(shortcuts.GetArrayElementAtIndex(3), "share", btnShare);
            SetShortcutBinding(shortcuts.GetArrayElementAtIndex(4), "more_games", btnMore);
            SetShortcutBinding(shortcuts.GetArrayElementAtIndex(5), "educator_feedback", btnEducator);

            var th = vs.FindProperty("theme");
            th.FindPropertyRelative("switchOn").colorValue      = SwitchOn;
            th.FindPropertyRelative("switchOff").colorValue     = SwitchOff;
            th.FindPropertyRelative("selectedFill").colorValue  = BerryDark;
            th.FindPropertyRelative("idleText").colorValue      = Plum;
            vs.ApplyModifiedPropertiesWithoutUndo();

            s.gameObject.SetActive(false);
            return view;
        }

        static void SetVolumeBinding(SerializedProperty item, string id, Slider slider)
        {
            item.FindPropertyRelative("id").stringValue = id;
            item.FindPropertyRelative("slider").objectReferenceValue = slider;
        }

        static void SetSwitchBinding(SerializedProperty item, string id, Toggle toggle, RectTransform knob, Image track)
        {
            item.FindPropertyRelative("id").stringValue = id;
            item.FindPropertyRelative("toggle").objectReferenceValue = toggle;
            item.FindPropertyRelative("knob").objectReferenceValue = knob;
            item.FindPropertyRelative("track").objectReferenceValue = track;
        }

        static void SetShortcutBinding(SerializedProperty item, string id, Button button)
        {
            item.FindPropertyRelative("id").stringValue = id;
            item.FindPropertyRelative("button").objectReferenceValue = button;
        }

        struct HomeDockRefs
        {
            public CanvasGroup group;
            public RectTransform[] tiles;
            public Button dailyBtn, practiceBtn, afternoonBtn, chefBtn, achievementsBtn;
            public TMP_Text dailyStatus, practiceStatus, afternoonStatus, chefStatus;
            public GameObject afternoonLock, chefLock, dailyBadge;
        }

        static Button BuildTopCircleAction(RectTransform root, string name, float xOffset, Sprite icon, string labelKey)
        {
            var rt = Node(name, root);
            Place(rt, new Vector2(1f, 1f), new Vector2(82, 74), new Vector2(xOffset, -16));

            var disc = Node("Disc", rt);
            Place(disc, new Vector2(0.5f, 1f), new Vector2(50, 50), Vector2.zero);
            var discBg = Paint(disc, Color.white, circle);
            discBg.preserveAspect = true;
            var discOl = disc.gameObject.AddComponent<Outline>();
            discOl.effectColor = new Color(Plum.r, Plum.g, Plum.b, 0.55f);
            discOl.effectDistance = new Vector2(1.5f, -1.5f);

            var ico = Node("Icon", disc);
            Stretch(ico, 11, 11, 11, 11);
            Paint(ico, Color.white, icon).preserveAspect = true;

            var pill = Node("LabelPill", rt);
            Place(pill, new Vector2(0.5f, 0f), new Vector2(82, 20), Vector2.zero);
            Paint(pill, Color.white, roundedLight ?? roundedPlum, true, 2.2f);
            var lbl = Text(pill, "Label", labelKey, false, 11, Plum, TextAlignmentOptions.Center);
            Stretch((RectTransform)lbl.transform, 4, 4, 1, 2);

            return MakeButton(rt.gameObject, discBg);
        }

        static HomeDockRefs BuildHomeModesDock(RectTransform root)
        {
            var dock = Node("HomeModesDock", root);
            Place(dock, new Vector2(1f, 0f), new Vector2(592, 100), new Vector2(-18, 12));
            var group = dock.gameObject.AddComponent<CanvasGroup>();
            var row = Row(dock, 10, new RectOffset(0, 0, 0, 0), TextAnchor.LowerRight);
            row.childForceExpandWidth = false; row.childForceExpandHeight = false;

            (Button btn, TMP_Text status, GameObject lockBadge, GameObject badge, RectTransform rt) MakeTile(
                string name, string titleKey, Sprite icon, Color iconTint, bool withLock = false, bool withTodayBadge = false)
            {
                var t = Node(name, dock);
                var bg = Paint(t, Color.white, roundedLight ?? roundedPlum, true, 1.8f);
                Size(bg, 114, 92);
                var col = Column(t, 2, new RectOffset(6, 6, 6, 6));
                col.childAlignment = TextAnchor.MiddleCenter;
                col.childForceExpandWidth = true;

                var btn = MakeButton(t.gameObject, bg);
                var colors = btn.colors;
                colors.disabledColor = new Color(0.97f, 0.94f, 0.96f, 0.94f);
                btn.colors = colors;

                var icoBadge = Node("IconBg", t);
                Size(icoBadge.gameObject.AddComponent<LayoutElement>(), 34, 34);
                Paint(icoBadge, Blush, circle).preserveAspect = true;
                var ico = Node("Icon", icoBadge);
                Stretch(ico, 5, 5, 5, 5);
                Paint(ico, iconTint, icon).preserveAspect = true;

                var title = Text(t, "Label", titleKey, true, 13, Plum, TextAlignmentOptions.Center);
                title.textWrappingMode = TextWrappingModes.Normal;
                title.fontSizeMin = 9f;
                Size(title, -1, 24);
                var status = Text(t, "Status", null, false, 11, BerryDark, TextAlignmentOptions.Center, "");
                Size(status, -1, 15);

                GameObject lockObj = null;
                if (withLock)
                {
                    var lb = Node("Lock", t);
                    var le = lb.gameObject.AddComponent<LayoutElement>(); le.ignoreLayout = true;
                    Place(lb, new Vector2(1f, 1f), new Vector2(20, 20), new Vector2(-5, -5));
                    Paint(lb, Color.white, iconLock).preserveAspect = true;
                    lockObj = lb.gameObject;
                }

                GameObject badgeObj = null;
                if (withTodayBadge)
                {
                    var bb = Node("TodayBadge", t);
                    var le = bb.gameObject.AddComponent<LayoutElement>(); le.ignoreLayout = true;
                    Place(bb, new Vector2(1f, 1f), new Vector2(46, 20), new Vector2(-4, 5));
                    Paint(bb, Berry, rounded, true, 3.2f);
                    var bl = Text(bb, "BadgeText", "home.new_badge", true, 11, Color.white, TextAlignmentOptions.Center);
                    Stretch((RectTransform)bl.transform, 4, 4, 1, 2);
                    badgeObj = bb.gameObject;
                }
                return (btn, status, lockObj, badgeObj, t);
            }

            var d = MakeTile("HomeTile_Daily",     "modes.daily",          iconCalendar, Color.white, false, true);
            var p = MakeTile("HomeTile_Practice",  "modes.practice_short", iconBook,     Color.white);
            var a = MakeTile("HomeTile_Afternoon", "modes.afternoon",      iconSun,      Color.white, true);
            var c = MakeTile("HomeTile_Chef",      "modes.chef",           iconChef,     Color.white, true);

            // Compact achievements tile
            var achRt = Node("HomeTile_Achievements", dock);
            var achBg = Paint(achRt, Cream, roundedLight ?? roundedPlum, true, 1.8f);
            Size(achBg, 92, 92);
            var achCol = Column(achRt, 3, new RectOffset(6, 6, 8, 8));
            achCol.childAlignment = TextAnchor.MiddleCenter;
            var achBtn = MakeButton(achRt.gameObject, achBg);
            var achBadge = Node("IconBg", achRt);
            Size(achBadge.gameObject.AddComponent<LayoutElement>(), 36, 36);
            Paint(achBadge, Vanilla, circle).preserveAspect = true;
            var achIco = Node("Icon", achBadge);
            Stretch(achIco, 6, 6, 6, 6);
            Paint(achIco, Color.white, iconTrophy).preserveAspect = true;
            var achLbl = Text(achRt, "Label", "settings.achievements", true, 13, Plum, TextAlignmentOptions.Center);
            Size(achLbl, -1, 20);

            return new HomeDockRefs
            {
                group = group,
                tiles = new[] { d.rt, p.rt, a.rt, c.rt, achRt },
                dailyBtn = d.btn, dailyStatus = d.status, dailyBadge = d.badge,
                practiceBtn = p.btn, practiceStatus = p.status,
                afternoonBtn = a.btn, afternoonStatus = a.status, afternoonLock = a.lockBadge,
                chefBtn = c.btn, chefStatus = c.status, chefLock = c.lockBadge,
                achievementsBtn = achBtn,
            };
        }

        static SlothyShellPanel AttachShellPanel(RectTransform screen, RectTransform card,
            Button closeBtn = null, Button backdropBtn = null, params RectTransform[] items)
        {
            var group = screen.GetComponent<CanvasGroup>() ?? screen.gameObject.AddComponent<CanvasGroup>();
            var host = screen.gameObject.AddComponent<SlothyShellPanel>();
            var so = new SerializedObject(host);
            so.FindProperty("group").objectReferenceValue = group;
            so.FindProperty("card").objectReferenceValue = card;
            so.FindProperty("closeButton").objectReferenceValue = closeBtn;
            so.FindProperty("backdropButton").objectReferenceValue = backdropBtn;
            var arr = so.FindProperty("staggeredItems");
            arr.arraySize = items?.Length ?? 0;
            if (items != null)
                for (int i = 0; i < items.Length; i++)
                    arr.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            return host;
        }

        static Button BackdropButton(RectTransform screen)
        {
            var dim = Paint(screen, new Color(0.21f, 0.11f, 0.19f, 0.62f));
            dim.raycastTarget = true;
            var btn = MakeButton(screen.gameObject, dim);
            btn.transition = Selectable.Transition.None;
            return btn;
        }

        static CrossPromoCatalog EnsureCrossPromoCatalog()
        {
            const string CatalogPath = "Assets/Data/BakeryCrossPromo.asset";
            System.IO.Directory.CreateDirectory("Assets/Data");
            var catalog = AssetDatabase.LoadAssetAtPath<CrossPromoCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<CrossPromoCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            Sprite Art(string f)
            {
                string path = $"Assets/Art/OtherGames/{f}.png";
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti != null && ti.textureType != TextureImporterType.Sprite)
                {
                    ti.textureType = TextureImporterType.Sprite;
                    ti.spriteImportMode = SpriteImportMode.Single;
                    ti.SaveAndReimport();
                }
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            CrossPromoGame G(string id, string name, string pkg, string es, string en, string icon, int order) => new CrossPromoGame
            {
                id = id,
                displayName = name,
                androidPackage = pkg,
                genreEs = es,
                genreEn = en,
                taglineEs = es,
                taglineEn = en,
                icon = Art(icon),
                order = order,
            };

            catalog.games = new List<CrossPromoGame>
            {
                G("slothys-bakery",       "Slothy's Bakery",       "com.FatCatGameStudio.SlothysBakery",  "Fracciones en la panadería", "Fractions at the bakery", "SlothysBakery", 0),
                G("mix2word",             "Mix2Word",              "com.FatCatGame.Mix2Word",             "Juego de palabras",          "Word puzzle",             "Mix2Word", 1),
                G("pig-year-food-fight",  "Pig Year: Food Fight",  "com.FatCatGameStudio.PigYearFF",      "Arcade divertido",           "Chaotic arcade",          "PigYear", 2),
                G("slothys-pizza-master", "Slothy's Pizza Master", "com.FatCatGameStudio.PizzaMaster",    "Fracciones con pizza",       "Fractions with pizza",    "SlothysPizzaMaster", 3),
                G("slothys-cafe",         "Slothy's Café",         "com.FatCatGameStudio.SlothysCafe",    "Fracciones en la cafetería", "A cozy café",             "SlothysCafe", 4),
                G("lazy-lovers",          "Snuggle Sloths",        "com.FatCatGameStudio.LazyLovers",     "Habilidad y equilibrio",     "Skill and balance",       "SnuggleSloths", 5),
                G("match-the-runes",      "Memorama: Match the Runes", "com.FatCatGameStudio.MatchTheRunes", "Memoria y runas",        "Memory and runes",        "MatchTheRunes", 6),
            };
            catalog.playDeveloperName = "FatCat Game Studio";
            catalog.remoteCatalogUrl = "https://fatcat-app-registry.vercel.app/games.json";
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        static CrossPromoCardView BuildGameCardPrefab()
        {
            const string CardPath = "Assets/Editor/Shell/BakeryGameCard.prefab";
            var root = Node("BakeryGameCard", null);
            root.sizeDelta = new Vector2(640, 84);
            var bg = Paint(root, Color.white, roundedLight ?? roundedPlum, true, 1.8f);
            Size(bg, -1, 84);
            Row(root, 14, new RectOffset(12, 14, 8, 8));

            var iconSlot = Node("IconSlot", root);
            Size(iconSlot.gameObject.AddComponent<LayoutElement>(), 64, 64);

            var maskRt = Node("Mask", iconSlot);
            Stretch(maskRt);
            Paint(maskRt, Color.white, rounded, true, 3.8f);
            maskRt.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var icoRt = Node("Icon", maskRt);
            Stretch(icoRt);
            var iconImg = Paint(icoRt, Color.white);
            iconImg.preserveAspect = true;

            var ph = Node("Placeholder", iconSlot);
            Stretch(ph);
            var phBg = Paint(ph, Blush, rounded, true, 3.8f);
            var phInit = Text(ph, "Initials", null, true, 22, Plum, TextAlignmentOptions.Center, "FC");
            Stretch((RectTransform)phInit.transform);

            var texts = Node("Texts", root);
            Column(texts, 2).childAlignment = TextAnchor.MiddleLeft;
            var tle = texts.gameObject.AddComponent<LayoutElement>(); tle.flexibleWidth = 1; tle.minWidth = 0;
            var nameLbl = Text(texts, "Name", null, true, 20, Plum, TextAlignmentOptions.Left, "Mix2Word");
            Size(nameLbl, -1, 26);
            var genreLbl = Text(texts, "Genre", null, false, 13, PlumMuted, TextAlignmentOptions.Left, "Juego de palabras");
            genreLbl.textWrappingMode = TextWrappingModes.Normal;
            Size(genreLbl, -1, 24);

            var openRt = Node("OpenButton", root);
            Size(openRt.gameObject.AddComponent<LayoutElement>(), 104, 42);
            var openBg = Paint(openRt, Color.white, berryButton, true, 1.6f);
            var openBtn = MakeButton(openRt.gameObject, openBg);
            var openLbl = Text(openRt, "Label", "moregames.view", true, 17, Color.white, TextAlignmentOptions.Center);
            Stretch((RectTransform)openLbl.transform, 8, 8, 2, 5);

            var view = root.gameObject.AddComponent<CrossPromoCardView>();
            var so = new SerializedObject(view);
            so.FindProperty("icon").objectReferenceValue = iconImg;
            so.FindProperty("placeholder").objectReferenceValue = ph.gameObject;
            so.FindProperty("placeholderBackground").objectReferenceValue = phBg;
            so.FindProperty("placeholderInitials").objectReferenceValue = phInit;
            so.FindProperty("nameLabel").objectReferenceValue = nameLbl;
            so.FindProperty("genreLabel").objectReferenceValue = genreLbl;
            so.FindProperty("openButton").objectReferenceValue = openBtn;
            so.FindProperty("openLabel").objectReferenceValue = openLbl;
            so.ApplyModifiedPropertiesWithoutUndo();

            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(CardPath));
            var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, CardPath);
            UnityEngine.Object.DestroyImmediate(root.gameObject);
            return prefab.GetComponent<CrossPromoCardView>();
        }

        static SlothyDashboardCardView DashboardCard(Transform parent, string name, string titleKey, Sprite icon, bool withAction = false, string actionKey = null)
        {
            var rt = Node(name, parent);
            Paint(rt, Color.white, roundedLight ?? roundedPlum, true, 1.8f);
            var col = Column(rt, 6, new RectOffset(14, 14, 12, 12));
            col.childForceExpandWidth = true;
            col.childForceExpandHeight = false;

            var hdr = Node("Header", rt);
            Size(hdr.gameObject.AddComponent<LayoutElement>(), -1, 30);
            Row(hdr, 8, null, TextAnchor.MiddleLeft);
            var badge = Node("IconBg", hdr);
            Size(badge.gameObject.AddComponent<LayoutElement>(), 28, 28);
            Paint(badge, Blush, circle).preserveAspect = true;
            var ico = Node("Icon", badge);
            Stretch(ico, 5, 5, 5, 5);
            Paint(ico, Color.white, icon).preserveAspect = true;
            var titleLbl = Text(hdr, "Title", titleKey, true, 17, Plum);
            Size(titleLbl, -1, 26, 1);

            var bodyLbl = Text(rt, "Body", null, false, 13, PlumMuted, TextAlignmentOptions.TopLeft, "");
            bodyLbl.textWrappingMode = TextWrappingModes.Normal;
            bodyLbl.fontSizeMin = 10f;
            var bodyLe = bodyLbl.gameObject.AddComponent<LayoutElement>();
            bodyLe.flexibleHeight = 1f;
            bodyLe.minHeight = 44f;

            Button actionBtn = null;
            TMP_Text actionLbl = null;
            if (withAction)
            {
                var actRt = Node("ActionBtn", rt);
                Size(actRt.gameObject.AddComponent<LayoutElement>(), -1, 34);
                var actBg = Paint(actRt, Color.white, berryButton, true, 1.8f);
                actionBtn = MakeButton(actRt.gameObject, actBg);
                actionLbl = Text(actRt, "Label", actionKey, true, 14, Color.white, TextAlignmentOptions.Center, "");
                Stretch((RectTransform)actionLbl.transform, 10, 10, 2, 4);
            }

            var view = rt.gameObject.AddComponent<SlothyDashboardCardView>();
            var so = new SerializedObject(view);
            so.FindProperty("titleLabel").objectReferenceValue = titleLbl;
            so.FindProperty("bodyLabel").objectReferenceValue = bodyLbl;
            so.FindProperty("actionButton").objectReferenceValue = actionBtn;
            so.FindProperty("actionLabel").objectReferenceValue = actionLbl;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        static void BuildFriendsShelf(RectTransform root, BakeryModesView modesView, CrossPromoCatalog catalog)
        {
            var shelf = Node("FriendsShelf", root);
            // Place on the Home layer under all modal overlays.
            var dock = root.Find("HomeModesDock");
            if (dock != null) shelf.SetSiblingIndex(dock.GetSiblingIndex() + 1);
            Place(shelf, new Vector2(1f, 0.5f), new Vector2(212, 194), new Vector2(-16, 2));

            var title = Node("Title", shelf);
            Place(title, new Vector2(0.5f, 1f), new Vector2(210, 34), Vector2.zero);
            Paint(title, Cream, rounded, true, 3.2f);
            var to = title.gameObject.AddComponent<Outline>(); to.effectColor = Plum; to.effectDistance = new Vector2(2, -2);
            var tt = Text(title, "Label", "friends.title", true, 18, Plum, TextAlignmentOptions.Center);
            Stretch((RectTransform)tt.transform, 10, 10, 3, 5);

            var plank = Node("Plank", shelf);
            Place(plank, new Vector2(0.5f, 1f), new Vector2(212, 14), new Vector2(0, -118));
            Paint(plank, Hex("#C98A5B"), rounded, true, 3.6f);
            var po = plank.gameObject.AddComponent<Outline>(); po.effectColor = Plum; po.effectDistance = new Vector2(2, -2);
            var ps = plank.gameObject.AddComponent<Shadow>(); ps.effectColor = new Color(Plum.r, Plum.g, Plum.b, 0.3f); ps.effectDistance = new Vector2(0, -4);

            var baked = catalog.games
                .Where(g => g != null && !g.hidden && g.icon != null && g.id != "slothys-bakery")
                .OrderBy(g => g.order)
                .Select(g => g.icon)
                .ToList();

            var view = shelf.gameObject.AddComponent<BakeryFriendsShelf>();
            var so = new SerializedObject(view);
            so.FindProperty("catalog").objectReferenceValue = catalog;
            so.FindProperty("modesView").objectReferenceValue = modesView;
            var slots = so.FindProperty("slots");
            slots.arraySize = 3;
            GameObject ribbon = null;

            for (int i = 0; i < 3; i++)
            {
                var slot = Node("Game" + i, shelf);
                Place(slot, new Vector2(0.5f, 1f), new Vector2(60, 60), new Vector2((i - 1) * 68, -56));
                var body = Node("Body", slot);
                body.anchorMin = body.anchorMax = new Vector2(0.5f, 0f); body.pivot = new Vector2(0.5f, 0f);
                body.sizeDelta = new Vector2(60, 60); body.anchoredPosition = Vector2.zero;
                var frame = Paint(body, Color.white, rounded, true, 3.8f);
                var fo = body.gameObject.AddComponent<Outline>(); fo.effectColor = Plum; fo.effectDistance = new Vector2(2, -2);
                var fs = body.gameObject.AddComponent<Shadow>(); fs.effectColor = new Color(Plum.r, Plum.g, Plum.b, 0.28f); fs.effectDistance = new Vector2(0, -4);
                var button = MakeButton(body.gameObject, frame);
                button.transition = Selectable.Transition.None;

                var mask = Node("Mask", body); Stretch(mask, 3, 3, 3, 3);
                Paint(mask, Color.white, rounded, true, 4.2f);
                mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
                var icon = Node("Icon", mask); Stretch(icon);
                var iconImg = Paint(icon, Color.white);
                iconImg.preserveAspect = true;
                if (i < baked.Count) iconImg.sprite = baked[i];

                var e = slots.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("button").objectReferenceValue = button;
                e.FindPropertyRelative("icon").objectReferenceValue = iconImg;
                e.FindPropertyRelative("body").objectReferenceValue = body;

                if (i == 0)
                {
                    var r = Node("Ribbon", body);
                    Place(r, new Vector2(1f, 1f), new Vector2(62, 22), new Vector2(14, 10));
                    r.localRotation = Quaternion.Euler(0, 0, -12f);
                    Paint(r, Hex("#F5B83D"), rounded, true, 4.2f);
                    var ro = r.gameObject.AddComponent<Outline>(); ro.effectColor = Plum; ro.effectDistance = new Vector2(1.5f, -1.5f);
                    var rt = Text(r, "Label", "friends.new", true, 13, Plum, TextAlignmentOptions.Center);
                    Stretch((RectTransform)rt.transform, 4, 4, 1, 2);
                    ribbon = r.gameObject;
                }
            }
            so.FindProperty("ribbon").objectReferenceValue = ribbon;
            shelf.Find("Game0").SetSiblingIndex(shelf.Find("Game2").GetSiblingIndex());

            var all = Node("SeeAll", shelf);
            Place(all, new Vector2(0.5f, 1f), new Vector2(194, 28), new Vector2(0, -148));
            var allImg = Paint(all, new Color(Cream.r, Cream.g, Cream.b, 0.90f), rounded, true, 4.2f);
            var allBtn = MakeButton(all.gameObject, allImg);
            allBtn.transition = Selectable.Transition.None;
            var at = Text(all, "Label", "friends.see_all", false, 13, Plum, TextAlignmentOptions.Center);
            at.fontStyle = FontStyles.Bold | FontStyles.Underline;
            Stretch((RectTransform)at.transform, 6, 6, 2, 2);

            so.FindProperty("seeAll").objectReferenceValue = allBtn;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static BakeryModesView BuildModesAndModals(RectTransform root, HomeDockRefs dock, Button progressBtn)
        {
            // Focused Practice modal (Practice by topic ONLY — separated from Home dock challenges)
            var modesRoot = Node("PracticeScreen", root); Stretch(modesRoot);
            var modesBackdrop = BackdropButton(modesRoot);
            var modesCard = Card(modesRoot, new Vector2(740, 424), -10);
            HeaderPill(modesCard, "modes.practice", 360, -20);
            var modesClose = CloseButton(modesCard);
            Place((RectTransform)modesClose.transform, new Vector2(1f, 1f), new Vector2(44, 44), new Vector2(-16, -16));

            var pracSub = Text(modesCard, "PracticeSub", "practice.subtitle", false, 15, PlumMuted, TextAlignmentOptions.Center);
            Place((RectTransform)pracSub.transform, new Vector2(0.5f, 1f), new Vector2(620, 26), new Vector2(0, -68));

            var topicsCol = Node("Topics", modesCard);
            Stretch(topicsCol, 40, 40, 102, 26);
            Column(topicsCol, 12);
            var (p1Btn, p1Status, p1Rt) = PracticeTopicCard(topicsCol, "Practice_MG1", "modes.mg1", "practice.mg1.desc", topicMg1, 90);
            var (p2Btn, p2Status, p2Rt) = PracticeTopicCard(topicsCol, "Practice_MG2", "modes.mg2", "practice.mg2.desc", topicMg2, 90);
            var (p3Btn, p3Status, p3Rt) = PracticeTopicCard(topicsCol, "Practice_MG3", "modes.mg3", "practice.mg3.desc", topicMg3, 90);
            var modesHost = AttachShellPanel(modesRoot, modesCard, modesClose, modesBackdrop, p1Rt, p2Rt, p3Rt);
            modesRoot.gameObject.SetActive(false);

            // Run Summary modal
            var sumRoot = Node("RunSummaryScreen", root); Stretch(sumRoot);
            Paint(sumRoot, new Color(0.21f, 0.11f, 0.19f, 0.62f)).raycastTarget = true;
            var sumCard = Card(sumRoot, new Vector2(560, 360), -10);
            var sumPill = HeaderPill(sumCard, null, 380, -20, "¡Listo!");
            var sumTitle = sumPill.GetComponentInChildren<TMP_Text>();
            var sumBody = Text(sumCard, "Body", null, false, 18, Plum, TextAlignmentOptions.Center, "");
            sumBody.textWrappingMode = TextWrappingModes.Normal;
            Stretch((RectTransform)sumBody.transform, 36, 36, 88, 96);
            var sumOkBtn = BerryCtaButton(sumCard, "OkButton", "summary.ok", new Vector2(0.5f, 0f), new Vector2(226, 52), new Vector2(0, 24), 21);
            var sumHost = AttachShellPanel(sumRoot, sumCard, null, null, (RectTransform)sumOkBtn.transform);
            sumRoot.gameObject.SetActive(false);

            // Achievements modal (Scrollable pastry card list)
            var achRoot = Node("AchievementsScreen", root); Stretch(achRoot);
            var achBackdrop = BackdropButton(achRoot);
            var achCard = Card(achRoot, new Vector2(800, 454), -10);
            HeaderPill(achCard, "achievements.title", 300, -20);
            var achClose = CloseButton(achCard);
            Place((RectTransform)achClose.transform, new Vector2(1f, 1f), new Vector2(44, 44), new Vector2(-16, -16));
            var achSummary = Text(achCard, "AchievementsSummary", null, false, 15, PlumMuted, TextAlignmentOptions.Center, "0 de 10 logros desbloqueados");
            Place((RectTransform)achSummary.transform, new Vector2(0.5f, 1f), new Vector2(680, 26), new Vector2(0, -68));
            var (achScroll, achListRoot) = SlothyShellBuilderKit.ScrollList(kitTheme, achCard, 36, 36, 102, 26, 10f);
            var achRowPrefab = SlothyShellBuilderKit.BuildAchievementRowPrefab(kitTheme, "Assets/Editor/Shell/BakeryAchievementRow.prefab", "¡Logrado!");
            var achHost = AttachShellPanel(achRoot, achCard, achClose, achBackdrop);
            achRoot.gameObject.SetActive(false);

            // Progress Dashboard modal (6 cards)
            var progRoot = Node("ProgressScreen", root); Stretch(progRoot);
            var progBackdrop = BackdropButton(progRoot);
            var progCard = Card(progRoot, new Vector2(820, 444), -10);
            HeaderPill(progCard, "progress.title", 320, -20);
            var progClose = CloseButton(progCard);
            Place((RectTransform)progClose.transform, new Vector2(1f, 1f), new Vector2(44, 44), new Vector2(-16, -16));
            var gridRt = Node("Grid", progCard);
            Stretch(gridRt, 36, 36, 86, 26);
            var grid = gridRt.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(240, 156);
            grid.spacing = new Vector2(14, 14);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            var dStory = DashboardCard(gridRt, "Card_Story",        "dash.t_story",        iconFlame, true, "diploma.open");
            var dAft   = DashboardCard(gridRt, "Card_Afternoon",    "dash.t_afternoon",    iconSun);
            var dChef  = DashboardCard(gridRt, "Card_Chef",         "dash.t_chef",         iconChef);
            var dDaily = DashboardCard(gridRt, "Card_Daily",        "dash.t_daily",        iconCalendar);
            var dPrac  = DashboardCard(gridRt, "Card_Practice",     "dash.t_practice",     iconBook);
            var dAch   = DashboardCard(gridRt, "Card_Achievements", "dash.t_achievements", iconTrophy, true, "dash.see_achievements");
            var progHost = AttachShellPanel(progRoot, progCard, progClose, progBackdrop,
                (RectTransform)dStory.transform, (RectTransform)dAft.transform, (RectTransform)dChef.transform,
                (RectTransform)dDaily.transform, (RectTransform)dPrac.transform, (RectTransform)dAch.transform);
            progRoot.gameObject.SetActive(false);

            // More Games modal (CrossPromo catalog list)
            var promoCatalog = EnsureCrossPromoCatalog();
            var promoCardPrefab = BuildGameCardPrefab();
            var mgRoot = Node("MoreGamesScreen", root); Stretch(mgRoot);
            var mgBackdrop = BackdropButton(mgRoot);
            var mgCard = Card(mgRoot, new Vector2(760, 444), -10);
            HeaderPill(mgCard, "moregames.title", 300, -20);
            var mgClose = CloseButton(mgCard);
            Place((RectTransform)mgClose.transform, new Vector2(1f, 1f), new Vector2(44, 44), new Vector2(-16, -16));
            var mgSub = Text(mgCard, "Subtitle", "moregames.subtitle", false, 15, PlumMuted, TextAlignmentOptions.Center);
            Place((RectTransform)mgSub.transform, new Vector2(0.5f, 1f), new Vector2(640, 26), new Vector2(0, -68));
            var (mgScroll, mgListRoot) = SlothyShellBuilderKit.ScrollList(kitTheme, mgCard, 36, 36, 104, 76, 10f);
            var mgEmpty = Text(mgCard, "EmptyState", "moregames.empty", false, 16, PlumMuted, TextAlignmentOptions.Center);
            Place((RectTransform)mgEmpty.transform, new Vector2(0.5f, 0.5f), new Vector2(480, 60), Vector2.zero);
            mgEmpty.gameObject.SetActive(false);
            var mgStoreBtn = BerryCtaButton(mgCard, "StoreButton", "moregames.visit_store", new Vector2(0.5f, 0f), new Vector2(226, 44), new Vector2(0, 18), 18);
            var mgHost = AttachShellPanel(mgRoot, mgCard, mgClose, mgBackdrop, (RectTransform)mgStoreBtn.transform);
            mgRoot.gameObject.SetActive(false);

            var modesView = root.gameObject.AddComponent<BakeryModesView>();
            var mvso = new SerializedObject(modesView);
            mvso.FindProperty("openModesButton").objectReferenceValue = dock.practiceBtn;
            mvso.FindProperty("openProgressButton").objectReferenceValue = progressBtn;
            mvso.FindProperty("modesPanel").objectReferenceValue = modesRoot.gameObject;
            mvso.FindProperty("modesPanelHost").objectReferenceValue = modesHost;
            mvso.FindProperty("modesCloseButton").objectReferenceValue = modesClose;
            mvso.FindProperty("modesBackdropButton").objectReferenceValue = modesBackdrop;

            mvso.FindProperty("homeDockGroup").objectReferenceValue = dock.group;
            var dockArr = mvso.FindProperty("homeDockTiles");
            dockArr.arraySize = dock.tiles.Length;
            for (int i = 0; i < dock.tiles.Length; i++)
                dockArr.GetArrayElementAtIndex(i).objectReferenceValue = dock.tiles[i];
            mvso.FindProperty("homeDailyButton").objectReferenceValue = dock.dailyBtn;
            mvso.FindProperty("homeDailyStatus").objectReferenceValue = dock.dailyStatus;
            mvso.FindProperty("homeDailyBadge").objectReferenceValue = dock.dailyBadge;
            mvso.FindProperty("homePracticeButton").objectReferenceValue = dock.practiceBtn;
            mvso.FindProperty("homePracticeStatus").objectReferenceValue = dock.practiceStatus;
            mvso.FindProperty("homeAfternoonButton").objectReferenceValue = dock.afternoonBtn;
            mvso.FindProperty("homeAfternoonStatus").objectReferenceValue = dock.afternoonStatus;
            mvso.FindProperty("homeAfternoonLock").objectReferenceValue = dock.afternoonLock;
            mvso.FindProperty("homeChefButton").objectReferenceValue = dock.chefBtn;
            mvso.FindProperty("homeChefStatus").objectReferenceValue = dock.chefStatus;
            mvso.FindProperty("homeChefLock").objectReferenceValue = dock.chefLock;
            mvso.FindProperty("homeAchievementsButton").objectReferenceValue = dock.achievementsBtn;

            mvso.FindProperty("practiceMg1Button").objectReferenceValue = p1Btn;
            mvso.FindProperty("practiceMg1Status").objectReferenceValue = p1Status;
            mvso.FindProperty("practiceMg2Button").objectReferenceValue = p2Btn;
            mvso.FindProperty("practiceMg2Status").objectReferenceValue = p2Status;
            mvso.FindProperty("practiceMg3Button").objectReferenceValue = p3Btn;
            mvso.FindProperty("practiceMg3Status").objectReferenceValue = p3Status;
            mvso.FindProperty("dailyButton").objectReferenceValue = null;
            mvso.FindProperty("dailyStatus").objectReferenceValue = null;
            mvso.FindProperty("afternoonButton").objectReferenceValue = null;
            mvso.FindProperty("afternoonStatus").objectReferenceValue = null;
            mvso.FindProperty("chefButton").objectReferenceValue = null;
            mvso.FindProperty("chefStatus").objectReferenceValue = null;

            mvso.FindProperty("summaryPanel").objectReferenceValue = sumRoot.gameObject;
            mvso.FindProperty("summaryPanelHost").objectReferenceValue = sumHost;
            mvso.FindProperty("summaryTitle").objectReferenceValue = sumTitle;
            mvso.FindProperty("summaryBody").objectReferenceValue = sumBody;
            mvso.FindProperty("summaryOkButton").objectReferenceValue = sumOkBtn;

            mvso.FindProperty("achievementsPanel").objectReferenceValue = achRoot.gameObject;
            mvso.FindProperty("achievementsPanelHost").objectReferenceValue = achHost;
            mvso.FindProperty("achievementRowPrefab").objectReferenceValue = achRowPrefab;
            mvso.FindProperty("achievementsListRoot").objectReferenceValue = achListRoot;
            mvso.FindProperty("achievementsScroll").objectReferenceValue = achScroll;
            mvso.FindProperty("achievementsSummaryLabel").objectReferenceValue = achSummary;
            var iconArr = mvso.FindProperty("achievementIcons");
            iconArr.arraySize = achIcons.Length;
            for (int i = 0; i < achIcons.Length; i++)
                iconArr.GetArrayElementAtIndex(i).objectReferenceValue = achIcons[i];
            mvso.FindProperty("achievementsBody").objectReferenceValue = null;
            mvso.FindProperty("achievementsCloseButton").objectReferenceValue = achClose;

            mvso.FindProperty("progressPanel").objectReferenceValue = progRoot.gameObject;
            mvso.FindProperty("progressPanelHost").objectReferenceValue = progHost;
            mvso.FindProperty("dashStory").objectReferenceValue = dStory;
            mvso.FindProperty("dashAfternoon").objectReferenceValue = dAft;
            mvso.FindProperty("dashChef").objectReferenceValue = dChef;
            mvso.FindProperty("dashDaily").objectReferenceValue = dDaily;
            mvso.FindProperty("dashPractice").objectReferenceValue = dPrac;
            mvso.FindProperty("dashAchievements").objectReferenceValue = dAch;

            mvso.FindProperty("moreGamesPanel").objectReferenceValue = mgRoot.gameObject;
            mvso.FindProperty("moreGamesPanelHost").objectReferenceValue = mgHost;
            mvso.FindProperty("crossPromoCatalog").objectReferenceValue = promoCatalog;
            mvso.FindProperty("crossPromoCardPrefab").objectReferenceValue = promoCardPrefab;
            mvso.FindProperty("moreGamesListRoot").objectReferenceValue = mgListRoot;
            mvso.FindProperty("moreGamesScroll").objectReferenceValue = mgScroll;
            mvso.FindProperty("moreGamesEmptyState").objectReferenceValue = mgEmpty.gameObject;
            mvso.FindProperty("moreGamesStoreButton").objectReferenceValue = mgStoreBtn;
            mvso.FindProperty("moreGamesCloseButton").objectReferenceValue = mgClose;
            mvso.ApplyModifiedPropertiesWithoutUndo();
            return modesView;
        }

        static (Button button, TMP_Text status, RectTransform rt) PracticeTopicCard(
            Transform parent, string name, string titleKey, string descKey, Sprite art, float height = 86)
        {
            var rt = Node(name, parent);
            var bg = Paint(rt, Color.white, roundedLight ?? roundedPlum, true, 1.8f);
            Size(bg, -1, height);
            Row(rt, 14, new RectOffset(14, 16, 8, 8), TextAnchor.MiddleLeft);
            var btn = MakeButton(rt.gameObject, bg);

            var badge = Node("TopicArt", rt);
            Size(badge.gameObject.AddComponent<LayoutElement>(), 68, 68);
            Paint(badge, Blush, rounded, true, 3.6f);
            var ico = Node("Art", badge);
            Stretch(ico, 4, 4, 4, 4);
            Paint(ico, Color.white, art).preserveAspect = true;

            var textCol = Node("Texts", rt);
            Size(textCol.gameObject.AddComponent<LayoutElement>(), -1, -1, 1);
            Column(textCol, 2);
            var title = Text(textCol, "Title", titleKey, true, 20, Plum);
            Size(title, -1, 25);
            var desc = Text(textCol, "Desc", descKey, false, 13, PlumMuted);
            desc.textWrappingMode = TextWrappingModes.Normal;
            Size(desc, -1, 22);
            var status = Text(textCol, "Status", null, false, 13, BerryDark, plain: "");
            Size(status, -1, 19);
            return (btn, status, rt);
        }

        static (Button button, TMP_Text status, RectTransform rt) ModeCardRow(
            Transform parent, string name, string titleKey, Sprite icon, float height = 86)
        {
            var rt = Node(name, parent);
            var bg = Paint(rt, Color.white, roundedLight ?? roundedPlum, true, 1.8f);
            Size(bg, -1, height);
            Row(rt, 12, new RectOffset(14, 14, 10, 10), TextAnchor.MiddleLeft);
            var btn = MakeButton(rt.gameObject, bg);
            var colors = btn.colors;
            colors.disabledColor = new Color(0.97f, 0.94f, 0.96f, 0.94f);
            btn.colors = colors;

            var badge = Node("IconBg", rt);
            Size(badge.gameObject.AddComponent<LayoutElement>(), 46, 46);
            Paint(badge, Blush, circle).preserveAspect = true;
            var ico = Node("Icon", badge);
            Stretch(ico, 8, 8, 8, 8);
            Paint(ico, Color.white, icon).preserveAspect = true;

            var textCol = Node("Texts", rt);
            Size(textCol.gameObject.AddComponent<LayoutElement>(), -1, -1, 1);
            Column(textCol, 2);
            var title = Text(textCol, "Title", titleKey, true, 20, Plum);
            Size(title, -1, 26);
            var status = Text(textCol, "Status", null, false, 14, BerryDark, plain: "");
            Size(status, -1, 22);
            return (btn, status, rt);
        }

        static void BuildParentalGate()
        {
            var root = Node("ParentalGate", null);
            var canvas = root.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 960;
            var scaler = root.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1024, 576);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            root.gameObject.AddComponent<GraphicRaycaster>();
            var group = root.gameObject.AddComponent<CanvasGroup>();

            var dim = Paint(root, new Color(0.21f, 0.11f, 0.19f, 0.68f));
            dim.raycastTarget = true;
            var backdrop = MakeButton(root.gameObject, dim);
            backdrop.transition = Selectable.Transition.None;

            var card = Card(root, new Vector2(540, 310), 0);
            var close = CloseButton(card);
            Place((RectTransform)close.transform, new Vector2(1, 1), new Vector2(44, 44), new Vector2(-16, -16));

            TMP_Text Line(string n, bool disp, float sz, Color col, float y, float h, string key, string fallback)
            {
                var t = Text(card, n, key, disp, sz, col, TextAlignmentOptions.Center, fallback);
                var r = (RectTransform)t.transform;
                r.anchorMin = new Vector2(0, 1); r.anchorMax = new Vector2(1, 1); r.pivot = new Vector2(0.5f, 1);
                r.sizeDelta = new Vector2(-84, h); r.anchoredPosition = new Vector2(0, y);
                return t;
            }

            var lockLbl = Line("Lock", false, 14, BerryDark, -24, 22, "gate.lock", "Solo para adultos");
            var title = Line("Title", true, 26, Plum, -48, 36, "gate.title", "Pídele ayuda a un adulto");
            var question = Line("Question", true, 24, BerryDark, -94, 34, "gate.question", "¿Cuánto es 6 × 7?");
            var questionLoc = question.GetComponent<LocalizeStringEvent>();
            if (questionLoc != null) UnityEngine.Object.DestroyImmediate(questionLoc);

            var row = Node("Options", card);
            Place(row, new Vector2(0.5f, 1f), new Vector2(380, 68), new Vector2(0, -148));
            Row(row, 18, null, TextAnchor.MiddleCenter);

            var gateComp = root.gameObject.AddComponent<BakeryParentalGate>();
            var so = new SerializedObject(gateComp);
            var opts = so.FindProperty("options"); opts.arraySize = 3;
            var labels = so.FindProperty("optionLabels"); labels.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                var o = Node("Option" + i, row);
                var img = Paint(o, Color.white, roundedLight ?? roundedPlum, true, 1.8f);
                Size(img, 108, 58);
                var b = MakeButton(o.gameObject, img);
                var t = Text(o, "Label", null, true, 26, Plum, TextAlignmentOptions.Center, (40 + i).ToString());
                Stretch((RectTransform)t.transform, 4, 4, 2, 6);
                opts.GetArrayElementAtIndex(i).objectReferenceValue = b;
                labels.GetArrayElementAtIndex(i).objectReferenceValue = t;
            }

            var hint = Line("Hint", false, 14, PlumMuted, -242, 24, "gate.hint", "Vamos a abrir una página fuera del juego.");
            so.FindProperty("group").objectReferenceValue = group;
            so.FindProperty("card").objectReferenceValue = card;
            so.FindProperty("closeButton").objectReferenceValue = close;
            so.FindProperty("backdropButton").objectReferenceValue = backdrop;
            so.FindProperty("lockLabel").objectReferenceValue = lockLbl;
            so.FindProperty("titleLabel").objectReferenceValue = title;
            so.FindProperty("questionLabel").objectReferenceValue = question;
            so.FindProperty("hintLabel").objectReferenceValue = hint;
            so.ApplyModifiedPropertiesWithoutUndo();

            string dir = "Assets/Resources/UI";
            System.IO.Directory.CreateDirectory(dir);
            PrefabUtility.SaveAsPrefabAsset(root.gameObject, $"{dir}/ParentalGate.prefab");
            UnityEngine.Object.DestroyImmediate(root.gameObject);
        }

        static void BuildAchievementToast()
        {
            var root = Node("BakeryAchievementToast", null);
            var canvas = root.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            var scaler = root.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1024, 576);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            root.gameObject.AddComponent<GraphicRaycaster>();

            var banner = Node("Banner", root);
            banner.anchorMin = banner.anchorMax = new Vector2(0.5f, 1f);
            banner.pivot = new Vector2(0.5f, 1f);
            banner.sizeDelta = new Vector2(490, 96);
            banner.anchoredPosition = new Vector2(0, 126);
            Paint(banner, Color.white, bakeryCard, true, 1.4f);
            var group = banner.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0; group.blocksRaycasts = false; group.interactable = false;

            var icoRt = Node("Trophy", banner);
            Place(icoRt, new Vector2(0f, 0.5f), new Vector2(44, 44), new Vector2(42, 0));
            Paint(icoRt, Color.white, iconTrophy).preserveAspect = true;

            var heading = Text(banner, "Heading", "achievement.toast", true, 16, BerryDark, TextAlignmentOptions.Center);
            var headingRt = (RectTransform)heading.transform;
            headingRt.anchorMin = new Vector2(0, 0.56f); headingRt.anchorMax = Vector2.one;
            headingRt.offsetMin = new Vector2(74, 0); headingRt.offsetMax = new Vector2(-24, -14);
            var title = Text(banner, "Title", null, true, 24, Plum, TextAlignmentOptions.Center, "Primer pedido");
            var titleRt = (RectTransform)title.transform;
            titleRt.anchorMin = Vector2.zero; titleRt.anchorMax = new Vector2(1, 0.60f);
            titleRt.offsetMin = new Vector2(74, 14); titleRt.offsetMax = new Vector2(-24, 0);

            var toast = root.gameObject.AddComponent<BakeryAchievementToast>();
            var so = new SerializedObject(toast);
            so.FindProperty("banner").objectReferenceValue = banner;
            so.FindProperty("group").objectReferenceValue = group;
            so.FindProperty("heading").objectReferenceValue = heading;
            so.FindProperty("title").objectReferenceValue = title;
            so.ApplyModifiedPropertiesWithoutUndo();

            const string dir = "Assets/Resources/UI";
            System.IO.Directory.CreateDirectory(dir);
            PrefabUtility.SaveAsPrefabAsset(root.gameObject, $"{dir}/BakeryAchievementToast.prefab");
            UnityEngine.Object.DestroyImmediate(root.gameObject);
        }

        // ──────────────────────────────────────────────────────── install

        static void Install(GameObject prefab)
        {
            if (prefab == null) { Debug.LogError("[BakeryShellBuilder] Prefab is null; aborting install."); return; }
            if (!System.IO.File.Exists(Scene)) { Debug.LogWarning("[BakeryShellBuilder] Scene not found: " + Scene); return; }
            var scene = EditorSceneManager.OpenScene(Scene, OpenSceneMode.Additive);
            try
            {
                // Remove any old BakeryShell instances (idempotency)
                foreach (var root in scene.GetRootGameObjects())
                    if (root.name == "BakeryShellCanvas" || root.name == "BakeryShell"
                        || root.GetComponentInChildren<SettingsScreenView>(true) != null)
                        UnityEngine.Object.DestroyImmediate(root);

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);

                // Hide legacy speaker SoundBtn and make room on the right bay for FriendsShelf in both 16:9 and 20:9
                var view = instance.transform.Find("SettingsScreen")?.GetComponent<SettingsScreenView>();
                var shellSound = instance.transform.Find("SoundBtn")?.GetComponent<Button>();
                foreach (var b in scene.GetRootGameObjects()
                    .Where(r => r != instance)
                    .SelectMany(r => r.GetComponentsInChildren<Button>(true))
                    .Where(b => b.name == "SoundBtn" || b.name == "LegacySoundBtn")
                    .ToArray())
                {
                    b.gameObject.name = "LegacySoundBtn";
                    b.gameObject.SetActive(false);
                    EditorUtility.SetDirty(b.gameObject);
                }
                foreach (var mgr in scene.GetRootGameObjects()
                    .SelectMany(r => r.GetComponentsInChildren<INART.SlothyBakery.Base.InitialMenuManager>(true)))
                {
                    var mso = new SerializedObject(mgr);
                    mso.FindProperty("titleImg_EndX").floatValue = -355f;
                    mso.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(mgr);
                }
                foreach (var img in scene.GetRootGameObjects()
                    .Where(r => r != instance)
                    .SelectMany(r => r.GetComponentsInChildren<Image>(true))
                    .Where(i => i.name == "TitleImg"))
                {
                    var trt = img.GetComponent<RectTransform>();
                    trt.anchoredPosition = new Vector2(-355f, 124f);
                    trt.sizeDelta = new Vector2(338f, 184f);
                    EditorUtility.SetDirty(trt.gameObject);
                }
                foreach (var b in scene.GetRootGameObjects()
                    .Where(r => r != instance)
                    .SelectMany(r => r.GetComponentsInChildren<Button>(true)))
                {
                    if (b.name == "StartBtn" && b.GetComponent<RectTransform>() is var srt)
                    {
                        srt.anchoredPosition = new Vector2(-345f, -94f);
                        srt.sizeDelta = new Vector2(168f, 30f);
                        EditorUtility.SetDirty(srt.gameObject);
                    }
                    else if (b.name == "ContinueBtn" && b.GetComponent<RectTransform>() is var crt)
                    {
                        crt.anchoredPosition = new Vector2(-345f, -32f);
                        crt.sizeDelta = new Vector2(248f, 78f);
                        EditorUtility.SetDirty(crt.gameObject);
                    }
                }
                if (view != null && shellSound != null)
                {
                    while (shellSound.onClick.GetPersistentEventCount() > 0)
                        UnityEventTools.RemovePersistentListener(shellSound.onClick, 0);
                    UnityEventTools.AddPersistentListener(shellSound.onClick, view.Open);
                    EditorUtility.SetDirty(shellSound.gameObject);
                }

                EditorSceneManager.SaveScene(scene);
                Debug.Log("[BakeryShellBuilder] Installed BakeryShellCanvas in " + Scene);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
