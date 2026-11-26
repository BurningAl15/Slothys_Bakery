# Slothy's Bakery — Agent Rules

Antes de tocar cualquier archivo en `SlothysBakery/`, lee y sigue obligatoriamente:

1. `../AGENTS.md` (reglas de secretos, acciones protegidas, FastTask y handoff).
2. `../docs/AGENT_WORKING_METHOD.md` (ciclo de trabajo, builders idempotentes, capturas 16:9/20:9 y estándar UI/DOTween).
3. `Documentation/CLAUDE_HANDOFF_NEXT.md` y `FastTask/tasks.json`.

## Reglas obligatorias anti-trabajo a medias

- **Un bloque a la vez (freno duro):** Prohibido implementar una nueva feature si la anterior no ha compilado en Unity 6000.6.0f1 ni ha ejecutado sus builders (`BakeryShellBuilder`, `BakeryMinigameTextLocalizer`, `BakeryReminderIconInstaller`). Si Unity falla por licencia o porque `Temp/UnityLockfile` indica que el Editor está abierto, detente y repórtalo de inmediato.
- **Sin UI genérica:** Toda tarjeta, fila, botón y overlay debe usar el kit de `Assets/Resources/BakeryUI/` (generado por `scripts/art/bakery_ui_kit.py`), fuentes redondeadas (`Assets/Resources/Fonts/LilitaOne-Regular SDF` y `Fredoka-VariableFont_wdth,wght SDF` con fallback completo ES/EN) y animaciones `DOTween` (`SetUpdate(true)`).
- **Localización protegida:** Usa `BakeryPromptLocalization.GetLocalizedOrFallback` / `IsMissingTranslation` para no mostrar `"No translation found for ..."` en pantalla.
- **Mockup + Capturas reales:** Si el diseño sigue abierto, prepara HTML en `../docs/` y espera la elección; si el dueño ya especificó o aprobó el diseño, continúa sin pedir otra elección. Antes de cerrar un cambio visual, abre las capturas 16:9 y 20:9 de Play Mode.

## Relación con el método compartido

`../docs/AGENT_WORKING_METHOD.md` define el proceso común; este archivo define únicamente las particularidades de este juego. Si un ejemplo o regla general no coincide con el `AGENTS.md` local, la arquitectura o el handoff activo, sigue primero las reglas del workspace y del proyecto y deja documentada la discrepancia. No copies builders, fuentes, UI kits ni comandos de otro juego por similitud de nombre.

## Inicio de cada tarea

- Lee `FastTask/project.json` y `FastTask/tasks.json`; identifica la tarea existente y abre su `handoffPath`. Revisa `git status` y `git log -5` antes de editar.
- Si el JSON está inválido, conserva el archivo, reporta el error de parseo y no inventes ni cambies estados manualmente.
- No pises cambios sin commit ni archivos que el handoff atribuye a otro agente.
