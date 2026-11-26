# Bakery — progreso y siguiente bloque

Fecha: 2026-11-26. Tarea FastTask: `e5a8dded-fc07-47b6-b2ee-24336181d5ad` (`b41c92f0-7c18-4f2d-9b1e-3a72f5c018d4` en `testing`).

## Preparación iOS y verificación de Xcode / TestFlight (Antigravity, 2026-11-26)

- **Paridad iOS completada en Bakery:**
  - Creado `Assets/Editor/IosPlistPostprocessor.cs` (+ `.meta`): declara `ITSAppUsesNonExemptEncryption = false` en `Info.plist` y enlaza `StoreKit.framework` + `AppTrackingTransparency.framework` (weak) en `UnityFramework` (obligatorio por `com.fatcat.mobile` 0.4.0).
  - Creado `ProjectSettings/NativeShare.json`: texto bilingüe ES/EN para `NSPhotoLibraryUsageDescription` y `NSPhotoLibraryAddUsageDescription` al guardar el diploma en Fotos.
  - Actualizado `Assets/Editor/BuildVersionProcessor.cs`: soporte de `FATCAT_SKIP_MARKETING_VERSION_BUMP` para mantener alineada la versión pública en lanzamientos conjuntos Android+iOS.
  - Actualizado `CurrentlyWorkin/scripts/fastlane/Fastfile`: alinea firma con `SlothysCafe/fastlane/Fastfile` usando la cuenta Apple firmada en Xcode por defecto (`IOS_SIGN_WITH_API_KEY=1` opcional) para evitar `Cloud signing permission error` con la API key `R6DA76UX3G`.
- **Verificación iOS ejecutada:**
  - `scripts/build-ios.sh slothys-bakery` -> **exit 0** (`Result: Succeeded`, `Errors: 0`; `1.0.2`, iOS build `7`; `Icon-Store-1024.png` `1024x1024` sin alfa; `StoreKit`, `AppTrackingTransparency` y `UserNotifications` enlazados; plist con compliance y permisos de Fotos).
  - `scripts/upload-ios-testflight.sh slothys-bakery --archive-only` (corrido previo al ajuste de `Fastfile`): Xcode compiló y enlazó hasta **`** ARCHIVE SUCCEEDED **`**; `-exportArchive` falló por falta de perfil App Store para `com.FatCatGameStudio.SlothysBakery` (la app aún no existe en Apple Developer / App Store Connect) y permiso de cloud signing en la API key.
- **Bloqueos externos para publicar en iOS:** crear los App IDs y fichas en Apple Developer / App Store Connect para `com.FatCatGameStudio.SlothysBakery` y `com.FatCatGameStudio.PizzaMaster`, verificar cuenta Team Admin iniciada en Xcode (`Settings > Accounts`, Team `4UBGTX27U5`), y luego correr `scripts/upload-ios-testflight.sh slothys-bakery` (nota: `SlothysPizzaMaster` tiene en paralelo `fastlane/Fastfile` sin el guardia `IOS_SIGN_WITH_API_KEY=1` y sin `ProjectSettings/NativeShare.json`; no se tocó por trabajo concurrente).

## Diploma, FinalCutscene, FeedbackStamp y polish MG1–MG3 (Antigravity, 2026-11-25)

- **Diploma compartible:** `diploma_seal`/`diploma_star`/`diploma_play` generados vía `generate_slothy_ui_kit.py`; `SlothyDiplomaView` + `SlothyShellBuilderKit.BuildDiplomaPrefab` en `com.fatcat.slothy.core` (sin acoplar `DOTween`/`Unity.Localization`); `BakeryDiploma.cs`, `SlothyDotweenBinder.cs` y enlace en `Card_Story` (`actionKey: "diploma.open"` para no quedar sobrescrito con `"Ver logros"`).
- **Escena final (`5. FinalCutscene.unity`) y diálogo:** `FinalCutscene.cs` (guardias nulos + `[FormerlySerializedAs("light"/"anim")]` + `RewardDim` + `BakeryStats.AtLeast("story.finished", 1)` + `FeedbackStamp.Sparkle` + `DiplomaButton`) e instalador idempotente `BakeryFinalSceneInstaller.cs`; añadido `!HasPhrases(phraseType)` en `CanvasAnimator.cs` para no mostrar bocadillo vacío cuando `_introDialogSo` es nulo.
- **MG1–MG3:** `BakeryFeedbackHooks.ShowAnswerStamp` conectado en `Minigame1_Logic.cs`, `Minigame2.cs` y `Minigame3_B.cs`; `BakeryMinigameTextLocalizer.cs` ampliado a 24 etiquetas en `BakeryShell` (incluyendo tarjetas de esquina `MessageCanvas`, explicaciones/ejemplos MG1 y `feedback.*`), fallback `LiberationSans SDF` y botones pastel con padding interno + auto-size.
- **Estado de verificación, build y base para Codex:**
  - `BakeryFeedbackBuilder.RunCi`, `BakeryShellBuilder.RunCi`, `BakeryFinalSceneInstaller.RunCi`, `BakeryMinigameTextLocalizer.RunCi` y `BakeryReminderIconInstaller.RunCi` ejecutados con salida `0`; suite `Fatcat.Slothy.Bakery.Tests` en **12/12 Passed** con capturas 16:9 y 20:9 inspeccionadas tras la última edición de código (`CanvasAnimator`, `SceneTransitionController` `BakeryFadeOverlay` 999, `SaveLoadState.HasSavedProgress`, `InitialMenuManager`, `BakeryPauseOverlay`, `BakeryLanguageGate`, `Minigame2`, `BakeryModesView`, `ProgressionCanvas` `[FormerlySerializedAs("Bars"/"Locks")]`, `ParentalGate 960`).
  - Añadido `"com.yasirkula.nativeshare"` a `Packages/manifest.json`.
  - **Build Android (`1.0.2`, `versionCode 11`):** `SlothysBakery.aab` generado (`75M`) y **upload aceptado por Google Play en track `internal`** (`slothys-bakery-android-internal-20261126-030831.log`); procesamiento en Play Console, disponibilidad para testers y QA físico en dispositivo siguen pendientes de verificación.
  - Todo el pipeline reutilizable quedó explicado en la **Sección 8** de `CurrentlyWorkin/docs/AGENT_WORKING_METHOD.md` para replicarlo en **Slothy's Pizza Master**.

## Paridad Café + Automatización Compartida + Validación Final Unity 6000.6.0f1 (Antigravity, 2026-11-25)

- **Kit UI compartido:** Creado `CurrentlyWorkin/scripts/art/generate_slothy_ui_kit.py` reutilizado por `SlothysBakery/scripts/art/bakery_ui_kit.py` (38 PNGs 9-slice, iconos incluyendo `icon_sun` y medallas en `Assets/Resources/BakeryUI/`), además de `SlothyShellPanel`, `SlothyAchievementRowView`, `SlothyDashboardCardView` y `SlothyShellBuilderKit` en `SharedPackages/com.fatcat.slothy.core` (sin acoplar `DOTween` ni `Unity.Localization`) + `SlothyDotweenBinder.cs` en Bakery.
- **Paridad visual con Slothy's Café:** Migrado `BakeryShellBuilder.cs` y `BakeryPauseOverlay.cs` a bordes suaves `ui_rounded_light` (cabecera montada en Pausa + botones bold `LilitaOne`), iconos en Ajustes incluyendo **Ideas de docentes / Teacher feedback** (`shortcuts = 6` + `BakerySettingsBackend`), par circular top-right `HomeProgressButton` (`Mi progreso`) + `SoundBtn` (`icon_gear`, ocultando `LegacySoundBtn`), repisa `BakeryFriendsShelf` (**Amigos de Slothy** en Inicio), insignia `¡Hoy!` en `HomeModesDock`, `Turno de tarde` con `icon_sun` y `disabledColor` claro, **`PracticeScreen` dedicado únicamente a Práctica por tema**, `AchievementsScreen` scrollable con prefab `BakeryAchievementRow.prefab` y 10 medallas pastel, `ProgressScreen` de 6 tarjetas y `MoreGamesScreen` con `Assets/Data/BakeryCrossPromo.asset` + `BakeryGameCard.prefab` + iconos reales en `Assets/Art/OtherGames/`.
- **Fix de distribución vertical y estabilidad en runtime/tests:** `BakeryModesView.cs` valida `SaveLoadState.Instance.Progress.completions > 0`, corrige singular/plural en Turno (`Jugado 1 vez`), organiza en pila vertical centrada `Continuar` (con subtítulo `"{capítulo} · {round+1}/6"`) + `Nueva partida` (o `Jugar` sin guardado), ajusta `InitialMenuManager.titleImg_EndX = -355` despejando `Amigos de Slothy` en 16:9/20:9, y limpia la corrutina `CrossPromo.Refresh` en `CloseMoreGames()` / `OnDestroy()` (además de omitir fetch remoto en `Application.isBatchMode`) para evitar `Curl error 42: Callback aborted` durante recargas de dominio en PlayMode tests.
- **Validación tras la última edición (`2026-11-25`):**
  - `BakeryShellBuilder.RunCi`, `BakeryMinigameTextLocalizer.RunCi`, `BakeryReminderIconInstaller.RunCi` ejecutados con salida `0` en Unity `6000.6.0f1`.
  - Suite `Fatcat.Slothy.Bakery.Tests`: **12/12 Passed, 0 Failed, 0 Skipped** (`FINAL_EXIT=0`, `/tmp/bakery_edit_results.xml`).
  - Inspección visual completa de 30 capturas 16:9 y 20:9 (`/tmp/bakery_cafe_parity_captures/` y artefacto `bakery-opta-preview.md`).

## Rediseño Opción A + Builders + Validación Unity 6000.6.0f1 (Antigravity, 2026-11-25)

- Aprobada **Opción A · Vitrina Pastel** en `docs/BAKERY_UI_MOCKUP.html`.
- Kit propio 9-sliced generado por `scripts/art/bakery_ui_kit.py` (`Documentation/ArtSource/ui/*.svg` → `Assets/Resources/BakeryUI/*.png`) + tipografías redondeadas `LilitaOne-Regular SDF` y `Fredoka-VariableFont_wdth,wght SDF` en `Assets/Resources/Fonts/` con respaldo `LiberationSans SDF`.
- Animaciones `DOTween` (`SetUpdate(true)`) instaladas en `BakeryAnimatedPanel.cs`, `BakeryModesView.cs` (con `HomeModesDock` integrado en `1. Main Screen`), `BakeryPauseOverlay.cs`, `BakeryParentalGate.cs` y `BakeryAchievementToast.cs`.
- Corregido el fallback en `BakeryPromptLocalization.cs` (`IsMissingTranslation` / `GetLocalizedOrFallback`) para descartar `"No translation found for ..."` y unificado en 7 puntos.
- **Builders ejecutados (exit 0):** `BakeryShellBuilder.RunCi`, `BakeryMinigameTextLocalizer.RunCi` (9 etiquetas instaladas) y `BakeryReminderIconInstaller.RunCi` (`bakery_notif_small` / `bakery_notif_large` registrados).
- **Suite y capturas:** `Fatcat.Slothy.Bakery.Tests` **12/12 Passed, 0 Failed, 0 Skipped** (`UNITY_TEST_EXIT=0`). Capturas 16:9 y 20:9 revisadas visualmente (`/tmp/bakery_optA_captures2/`): cero glifos `□`, cero `"No translation found..."`, Pausa 100% opaca sobre MG2, contraste y márgenes limpios en ambos aspectos.

## Capturas actuales de Unity Editor Play Mode (2026-11-25)

Copiadas 5 capturas recientes del propietario en `Documentation/evidence/2026-11-25/` (Unity 6000.6.0f1 Play Mode, locale `English (United States) (en-US)`):

- `Documentation/evidence/2026-11-25/01_main_screen_language_picker.png`: `1. Main Screen` con `Language` (`Español` / `English`, `Continue`), `Modes` y `Continue` detrás.
- `Documentation/evidence/2026-11-25/02_main_screen_game_modes.png`: modal `Game Modes` abierto (`Practice by topic`: `Step comparison`, `Choose sign`, `Write fraction`; `Challenges`: `Daily challenge`, `Shift`, `Chef Mode`).
- `Documentation/evidence/2026-11-25/03_minigame1_prompt0_missing_translation.png`: `2. Minigame1` muestra `No translation found for 'minigame.mg1.prompt.0' in BakeryShell` en el cuadro principal de pregunta (`Yes` / `No` / `?` / `||`).
- `Documentation/evidence/2026-11-25/04_minigame1_prompt1_missing_translation.png`: `2. Minigame1` paso 2 muestra `No translation found for 'minigame.mg1.prompt.1' in BakeryShell` (`Fraction must have equal numerator and denominator values`, `Examples: 2/2, 3/3, 4/4...`, `Submit`, `Reset`).
- `Documentation/evidence/2026-11-25/05_minigame1_pause_overlay.png`: overlay `Paused` (`Continue`, `Restart`, `Main Menu`) sobre `2. Minigame1`.

Nota para el siguiente paso: el fallback serializado no protegió frente al texto `"No translation found for '...' in BakeryShell"` devuelto por Unity Localization mientras no exista la entrada en la tabla o no se haya re-ejecutado el builder de tablas/localización en Unity.

## Implementación actual Codex — 2026-11-24

- La tarea FastTask `e5a8dded-fc07-47b6-b2ee-24336181d5ad` sigue abierta en `testing`; implementación estática lista, falta verificación dentro de Unity.
- `UI_Transition_Button` conserva sus métodos públicos/eventos, pero traduce aliases legacy a nombres reales (`2. Minigame1`, `3. Minigame2`, `4. Minigame3`, `5. FinalCutscene`, menú). `Continue` deriva destino de `SaveLoadState.GetActualState()` sin mutar el formato de progreso.
- `SceneUtils.ToNextScene()` usa rutas explícitas entre las escenas authored; `Minigame3_B` carga `FinalCutscene` directamente. Build Settings inspeccionado: splash índice 0, menú índice 1; `DEV_ModularMinigame` está deshabilitada y no forma parte del flujo distribuible.
- `InitialMenuManager` conecta el botón de ajustes con `SettingsScreenView` cuando el shell está instalado, conservando el panel legacy como fallback. El shell builder y `Assets/Scripts/Shell/BakerySettingsBackend` ya existían; no se duplicó ni se reescribió esa implementación.
- No se ejecutó Unity, compilación, Play Mode, pruebas, build ni revisión visual. La disponibilidad de paquetes locales `com.fatcat.settings-ui` / `com.fatcat.localization-ui` debe confirmarse en Unity antes de marcar la navegación/settings como validada. No afirmar todavía que Continue conserva progreso en runtime.
- Se preservó Pizza Master: sigue con Antigravity y no forma parte del cambio de Bakery.

### Estado actual y próxima acción

El bloque de checkpoints y modos ya está implementado y registrado en FastTask `b41c92f0-7c18-4f2d-9b1e-3a72f5c018d4`: progreso por ronda y escena, reanudación, HUD de pausa, aislamiento de Historia frente a Práctica/Reto diario/Turno/Chef, resumen/logros y puerta para adultos. También se reforzaron los guardas de las entradas de modo y se deshabilita el reto diario al completarlo. FastTask conserva evidencia previa de Antigravity (12/12 y capturas 16:9/20:9); estos guardas añadidos después todavía requieren revalidación en Unity.

Siguiente: revalidar compile y suite en Unity 6000.6.0f1; luego QA táctil en dispositivo. La primera pasada de localización de prompts dinámicos conecta seis preguntas de MG1/MG3 a la tabla BakeryShell, con fallback serializado y escritura compatible con Febucci TextAnimator mediante SyncText. Quedan etiquetas/instrucciones estáticas localizadas en el inventario FastTask. Después avanzar integración de feedback/NarratorFace, Diploma, Recordatorios y Repisa. No duplicar la implementación de modos.

## Continuación Codex — localización estática Bakery, 2026-11-24

- Se creó `Assets/Scripts/Shell/BakeryLocalizedSceneText.cs` para actualizar textos fijos de TMP al cambiar el idioma y usar `TextAnimator.SyncText(text, false)` cuando corresponda.
- Se creó `Assets/Editor/Shell/BakeryMinigameTextLocalizer.cs`, instalador idempotente para 9 etiquetas ES/EN en MG1–MG3: botones Reset/Understood/Yes/No/Submit, pregunta y Check de MG2, Submit y placeholder de MG3. Registra claves en la tabla `BakeryShell`; el serializado español queda como fallback visual.
- El instalador no llegó a ejecutar: Unity 6000.6.0f1 falló antes de importar el proyecto con un error del canal `Unity.Licensing.Client.Pipe` y mensajes de base de datos de solo lectura. Por tanto, no se confirma que las escenas o tablas estén instaladas ni que el código compile. No se corrieron pruebas. La tarea `b41c92f0-7c18-4f2d-9b1e-3a72f5c018d4` sigue en `testing`.
- Próximo paso concreto: resolver el arranque/licencia del Editor en esta máquina, correr `FatCat.Slothy.Tools.BakeryMinigameTextLocalizer.RunCi`, revisar los diffs de tablas/escenas y entonces hacer la validación completa de modos y proporciones.

## Continuación Codex — recordatorios de Bakery, 2026-11-24

- `NotificationManager` se ajustó al flujo de Café: solo pide permiso tras activar el toggle o completar el primer Reto diario; cancela al volver o desactivar; limita las notificaciones a tres fechas (17:30 en días 1, 2 y 4); evita una notificación ese día si el reto ya se completó; toma título y variantes ES/EN de la tabla `BakeryShell`.
- `ProjectSettings/NotificationsSettings.asset` desactiva `Request Authorization on App Launch`; esto es necesario porque la opción activa solicita permiso al iniciar antes del opt-in. La guía de Unity dice que la opción automática dispara el cuadro al lanzamiento y que, al desactivarla, el juego debe iniciar `AuthorizationRequest` manualmente.
- Se añadió `BakeryReminderIconInstaller` que deriva iconos Android del logo propio de Bakery y los registra como `bakery_notif_small` / `bakery_notif_large`; aún debe ejecutarse en Unity para crear PNGs y registrar `NotificationsSettings`.
- No se ejecutaron pruebas ni se logró importar/compilar en Unity. La tarea `b41c92f0-7c18-4f2d-9b1e-3a72f5c018d4` permanece en `testing`.
- El estado actual del Unity Pipeline sigue sin una instancia/editor conectada; el builder no puede considerarse instalado. Cloud FastTask no está disponible en esta sesión, por lo que solo se actualizó su copia local.
- Pendiente inmediata: recuperar Unity/licencia, ejecutar los builders `BakeryMinigameTextLocalizer.RunCi` y `BakeryReminderIconInstaller.RunCi`, revisar los cambios generados y validar la suite completa, capturas 16:9/20:9 y recepción en Android/iOS. Después continuar integración del Feedback Kit/narrador, Diploma y repisa/catálogo.

## Antes de tocar archivos

1. Lee `AGENTS.md`, `../docs/AGENT_WORKING_METHOD.md`, este handoff y las revisiones `REVISION_ACADEMY_2026-11-24.md` y `REVISION_MECANICAS_Y_MODOS.md`.
2. Revisa `git status` y `git diff`. La rama ya contiene trabajo local extenso de shell, paquetes, build y documentación. **No lo reviertas, sobrescribas ni limpies**; integra cambios pequeños encima de lo que existe.
3. Actualiza la tarea FastTask que corresponda a `doing` antes de implementar. (Estado de modos/checkpoints actual: `testing`; los guardas recientes esperan revalidación.)

## Alcance P0: inicio y continuación

Primero confirma en Unity qué funciona realmente en **Jugar/Nueva partida/Continuar**. La revisión estática encontró discrepancias entre nombres de escenas usados por `UI_Transition_Button` y nombres con prefijo de las escenas, una escena DEV incluida en Build Settings, y `BakeryShellBuilder.cs` que referencia `BakerySettingsBackend` sin definición localizada. Son pistas por verificar, no autorización para reescribir el shell.

Resuelve solo lo necesario para que el shell compile y las rutas principales abran las escenas correctas de forma estable, sin depender de índices frágiles y sin alterar el formato ni el progreso legacy. Evita que la escena DEV sea destino o escena accidental del flujo distribuible. Usa un perfil de QA aislado para probar partida nueva y continuación.

No modernices todavía los minijuegos ni conectes los modos. Mantén intactas sus mecánicas salvo que una corrección de navegación lo requiera. Los checkpoints y el aislamiento de Historia/Práctica/Diario/Turno/Chef son el siguiente bloque, descrito en `REVISION_MECANICAS_Y_MODOS.md`.

## Criterio de cierre

- Unity compila sin errores nuevos y el menú, nueva partida y continuar se verifican en Editor/Play Mode con evidencia reproducible.
- Confirmar que continuar no pierde ni sobrescribe el progreso previo; no usar datos reales para QA.
- Revisar capturas a 16:9 y 20:9; inspeccionar visualmente, no solo generar los archivos.
- Revisar el diff completo, actualizar `HANDOFF`/FastTask con resultados y límites, y dejar el repositorio sin cambios accidentales.
- No crear ni subir builds de tienda como parte de esta tarea.

## Estado de otros juegos

Pizza Master tiene trabajo local activo de Antigravity y queda fuera de este cambio. Slothy’s Café: el propietario confirma que el envío actual está cerrado/enviado; el build adicional para feedback de docentes y adultos se subirá cuando acepten iOS y Android. Ese estado es un reporte del propietario, no una verificación de consola en esta sesión.

## Implementación de minijuegos — 2026-11-24

FastTask adicional: `58597408-1a77-4a59-83b2-ca67ae21798a` (`testing`). Codex comenzó el bloque solicitado: MG1 valida contenido por lista, curva de animación y entrada del factor, resetea su multiplicador; MG2 valida el contenido por ronda, resetea su estado, bloquea doble respuesta, registra posiciones por instancia y decide drop por bounds con SyncTransforms; MG3 valida rondas, fracciones, curva/velocidad de animación y bloquea respuesta repetida durante transición. Detalle y verificación pendiente: `Documentation/IMPLEMENTACION_MINIJUEGOS_DINAMICOS_2026-11-24.md`. No se ejecutó Unity ni pruebas. Próximo paso de ese bloque: QA de los tres minijuegos (incluye arrastre/liberación y reinicio de MG2). Checkpoints y modos ya están implementados bajo `b41c92f0-7c18-4f2d-9b1e-3a72f5c018d4`; requieren revalidación tras los guardas recientes.

## Continuación Codex — logros Bakery, 2026-11-24

- Añadido `BakeryAchievements`: ocho metas de hitos (primer pedido, 10 minijuegos, una partida perfecta, 1/5 retos diarios, turno de tarde, Modo Chef y 3 días jugados), progreso derivado de `BakeryStats`, guardado del desbloqueo en PlayerPrefs y evento `Unlocked` para futura notificación/toast.
- `BakeryStats.Add/AtLeast` evalúa el catálogo; completar minijuego registra `chapters.completed`, y practicar incrementa `practice.finished`.
- El panel actual de logros muestra desbloqueados/total y el progreso de cada meta; se agregaron los nombres y descripciones ES/EN al `BakeryShellBuilder` para que el builder los incluya en las tablas existentes. No se rediseñó el panel ni se creó todavía un toast.
- **Sin validación de Unity**: no se importó/compiló, no se corrieron pruebas ni se verificó la tabla generada. El trabajo queda `testing`. Confirmar tras restaurar Unity y no dar por instalada la UI de logros/localización hasta correr los builders y revisar los assets resultantes.

## Continuación Codex — toast de logros Bakery, 2026-11-24

- Se añadió `BakeryAchievementToast`, que escucha el evento de desbloqueo, mantiene una cola entre escenas, espera que `SceneTransitionController` termine y muestra el hito con un fade/slide breve. Sus textos consultan el locale actual con fallback seguro.
- `BakeryShellBuilder` ahora genera idempotentemente `Assets/Resources/UI/BakeryAchievementToast.prefab` con el estilo cálido de Bakery y añade el encabezado localizado `achievement.toast`. Se conecta la clase al prefab generado; no se editó YAML de escena a mano.
- No se ejecutó el builder, Unity, pruebas ni capturas. El prefab aún no existe/está confirmado como generado a partir de este cambio, y la tarea continúa en `testing`. Próximo paso: recuperar importación de Unity, ejecutar el builder y revisar prefab/diff; luego probar que los hitos aparezcan después de las transiciones en Story y modos alternos.

## Continuación Codex — hooks base de Feedback Kit, 2026-11-24

- Añadido `BakeryFeedbackHooks`: configura `FatcatHaptics.EnabledProvider` desde el toggle de Bakery, conecta FeedbackStamp con vibración y racha traducida, CheckBell con volumen/haptic, y FeedbackCelebration con el SFX Success disponible.
- `BakerySession.RecordAnswer` envía respuestas correctas/incorrectas a la ruta háptica común, por lo que los tres minijuegos comparten el mismo ajuste de vibración.
- Esto no instala aún la estampilla/celebración visual ni cambia los mensajes antiguos de los minijuegos. Falta generar un prefab FeedbackStamp con arte de Bakery, decidir mensajes de ayuda específicos por mecánica y conectarlo a las respuestas sin duplicar MessageManager; falta integrar/revisar una reacción expresiva del narrador. Sin Unity no se validó importación ni respuesta táctil; FastTask permanece `testing`.

## Continuación Codex — logros de Práctica y migración, 2026-11-25

- Se amplió el catálogo a diez logros: primera práctica y completar Práctica en los tres minijuegos.
- `BakeryStats.RecordPractice` registra por tema una sola vez y mantiene `practice.finished` / `practice.topics`. Antes de sumar, migra los contadores legacy `bakery.practice.mg1/mg2/mg3.count`; los jugadores con prácticas anteriores conservan sus hitos.
- Añadidas las claves ES/EN al builder. Unity Pipeline sigue sin encontrar un Editor conectado (comprobado el 2026-11-25); el cambio no se compiló ni probó. Validar migración usando PlayerPrefs QA aislado y que la lista de diez filas quepa en el modal.
