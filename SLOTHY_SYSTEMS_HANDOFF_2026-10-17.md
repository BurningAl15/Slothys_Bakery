# Slothy's Bakery — sistemas reutilizables y siguiente corte

## Checkpoint posterior — 2026-10-17

- El trabajo previo está respaldado en `backup/bakery-pending-2026-10-17` (`64b6230`). Esta corrección vive en la rama nueva `fix/dynamic-question-ui-validation`; los artefactos `.utmp` modificados/generados se dejan locales y fuera de Git.
- `BakeryQuestionController` ya no acepta un payload con más opciones que botones de prefab, sin texto de pregunta, con alguna opción/botón nulo o sin texto de opción. En vez de dejar un nivel aparentemente activo pero imposible de responder, registra error y detiene el adaptador. No crea UI ni toca los minijuegos originales.
- Solo se revisó el código y `git diff --check`; no hay prefab/escena dinámica conectada para ejecutarlo y Unity Editor sigue inaccesible. Esta corrección reduce un bloqueo futuro, **no** vuelve jugable el daily challenge ni demuestra un build.
- El objetivo de prueba inmediata sigue siendo abrir Bakery 6000.6.0f1, revisar Console, juego legacy/rating y el estado de la escena DEV; la integración del adaptador en un prefab de Edit Mode es trabajo posterior.

Fecha de inspección: 2026-10-17. Rama creada para el siguiente cambio: `feat/bakery-prefab-rating`. Este documento registra estado y alcance; **no certifica un build ni una release**.

## Objetivo de producto indicado por el dueño

Los juegos Slothy actuales sirven como base y canal de audiencia para un futuro juego Slothy más grande y monetizable. El dueño reporta que los actuales tienen usuarios anuales, pero no generan dinero relevante; esta pasada no verificó cifras en analytics. El trabajo deseado es mejorarlos con anuncios, compra para quitar anuncios, daily challenge y localizaciones, y reutilizar los sistemas después en ese nuevo juego. También interesa orientar el producto hacia recomendaciones de profesores. No convertir estas metas en afirmaciones de validación comercial.

## Regla de UI acordada

Todo popup/panel visible debe existir como **prefab o escena editada en Edit Mode**. En ejecución se permite instanciar el prefab, asignar contenido/localización y enlazar callbacks; no crear Canvas, Button, Image o textos desde código. Probar en Play Mode solo después de guardar el asset, para no perder cambios al salir.

## Evidencia local actual

- `RatingManager.cs` es el único constructor de UI encontrado por búsqueda de `new GameObject` y `AddComponent` de componentes UI en los scripts de Bakery. Crea todo el popup en ejecución. El manager sí está en `1. Main Screen.unity`, pero `playStoreUrl` y `appStoreUrl` están vacías en esa escena; el botón de calificar no abre una ficha con esta configuración.
- `DailyChallengeManager` y dos definiciones (`DC_CompleteRounds`, `DC_AnswerCorrect`) existen, pero implementan una **meta diaria de progreso** (por ejemplo, completar rondas), no el modo diario multi-minijuego pedido por el dueño. No se encontró el GUID del manager en escenas/prefabs; tampoco se ha demostrado que esa meta esté activa o visible.
- Unity Localization y tablas `UI`, `GamePhrases` y `Notifications` con locales inglés/español existen. No se auditó cobertura de claves, textos hardcoded ni cambio de idioma en dispositivo.
- `SettingsManager` ya tiene flags `AdsConsent`, `PersonalizedAdsEnabled` y `AdsRemoved` en PlayerPrefs. No se encontró proveedor activo de ads, Billing/RevenueCat ni flujo de compra/restore; esos flags **no** equivalen a monetización funcional.
- `AnalyticsManager` tiene eventos de sesión, respuestas, rondas, completado y rating; la recepción real y el consentimiento no fueron validados aquí.
- `Packages/manifest.json` ahora declara `com.fatcat.slothy.core` por ruta absoluta local, en contradicción con el handoff anterior que decía que no se había añadido. Hay dos scripts `DynamicMinigames` sin seguimiento en Git (`BakeryQuestionController`, `BakeryQuestionLevelPayloadSO`), sin referencias encontradas en escenas/prefabs. Tratar como piloto experimental, no como reemplazo del flujo legacy ni como sistema portable demostrado.
- La rama heredó modificaciones preexistentes de Unity 6.6, URP, paquetes y ProjectSettings. El último AAB encontrado es del 2026-08-31 y antecede a estos cambios; no valida el estado actual.
- Se intentó abrir Unity 6.6: el Editor no expuso un servidor Pipeline alcanzable. Un batch con sandbox falló al abrir sockets locales; fuera del sandbox Package Manager conectó, pero la inicialización de licencia falló/reintentó y se detuvo. El control de UI confirmó que la Mac está bloqueada y no pudo desbloquearla automáticamente. No se creó prefab ni se modificó el código en esta pasada.

## Orden de implementación propuesto para evitar rehacer trabajo

1. **Bakery, primer corte:** convertir rating a un prefab editable, enlazar sus tres botones/localización desde `RatingManager`, configurar URL Android verificada y comprobar el flujo en Edit Mode, Play Mode y dispositivo. No aceptar un cambio solo de código sin asset funcional.
2. **Daily challenge:** construir un modo que seleccione y ejecute una secuencia de retos procedentes de varios minijuegos dinámicos. El manager existente de metas diarias puede reutilizarse solo como componente auxiliar de progreso/recompensa; conectarlo a una vista no satisface por sí solo el modo solicitado. Mantener las reglas reutilizables separadas de la presentación de Bakery; probar selección, transición, persistencia, cambio de día, repetición y premio.
3. **Ads + quitar ads:** elegir/validar proveedor y crear adaptadores por plataforma/tienda; una fuente fiable del estado de compra debe controlar ads, con restore donde aplique. No usar solo `PlayerPrefs` como prueba de propiedad. Pantallas de compra/consentimiento como prefabs. Validar test ads, compra, restore, modo offline y eventos recibidos antes de producción.
4. **Localización:** auditar cobertura de las tablas actuales y extraer textos visibles faltantes; el contenido del prefab debe obtenerse de claves con fallbacks. Probar al menos inglés/español y tamaños de texto en teléfono.
5. **Reutilización:** mover contratos/reglas genéricas al paquete Slothy Core solo después de demostrar un consumidor Bakery y otro juego aislado. Cada juego conserva prefabs, arte, catálogo/IDs de tienda y reglas específicas. No migrar gameplay legacy de varios juegos en una sola release.

El trabajo de los puntos 2–5 no está implementado ni probado por este documento. Publicación y revisión de tiendas son gates separados. El objetivo de recomendaciones por profesores requiere evidencia de uso/feedback y no bloquea el primer corte técnico.

## Aclaración del dueño y auditoría de sistemas dinámicos — 2026-10-17

**Decisión del dueño:** no reemplazar ni refactorizar destructivamente los minijuegos originales. Usar sus mecánicas, fades, animación de Slothy, typewriting e instrucciones como referencia para **sistemas nuevos reutilizables** y variantes dinámicas que corran en una o varias escenas nuevas. Los originales deben seguir funcionando mientras se prueban los nuevos. No llamar «hecho» al sistema por la mera existencia de clases o ScriptableObjects: necesita prefab/escena conectada, gameplay completo, tests y QA visual/dispositivo.

Estado comprobado por inspección estática (sin Play Mode ni build actual):

| Área | Qué existe | Qué falta demostrar |
| --- | --- | --- |
| Bakery — flujo original | `CanvasAnimator` mueve a Slothy, hace fade del globo, typewriting y llama a la explicación; `InstructionManager`/`ExplanationManager` usan DOTween. `CanvasAnimator` está serializado en Minigame1 y en el prefab `Animation and Core Package` usado por Minigame2/3. `SceneTransitionController` existe y está referenciado en escenas/prefab. | Mejorar/reutilizar esas capacidades sin tocar la ruta original; probar visualmente timings, interrupción, input y movimiento reducido. |
| Bakery — presentadores modulares | `DialogPresenter`, `TypeWriterPresenter`, `SpeechBubblePresenter` y otros componentes están en código. `DEV_ModularMinigame.unity` usa `GameFlowManager` y `ModularProgressionCanvas`. | No se encontraron los GUID de los tres presentadores de diálogo en escenas/prefabs, ni una escena dinámica que consuma el nuevo `BakeryQuestionController`. La escena DEV modular no prueba las variantes dinámicas pedidas. |
| Bakery — variantes dinámicas | `BakeryQuestionController` y `BakeryQuestionLevelPayloadSO` existen sin seguimiento en Git y el manifest local declara Slothy Core mediante una ruta absoluta. | No se hallaron assets de payload/definición ni prefab/escena que referencien ese controlador. No hay demostración de minijuegos dinámicos jugables en Bakery. |
| Slothy Core compartido | Contiene `TransitionService`/`PrefabTransitionView`, `NarrativeService`, secuencias, componentes de presentación pensados para prefabs de personaje/globo, reglas de flujo y payloads. | Son piezas de framework, no una integración completa de Bakery. `PrefabNarrativePresenter` implementa texto para Dialogue/Explanation; `CharacterCue` y `AnimationCue` aún no ejecutan una animación/instrucción específica allí. Faltan prefabs de juego y validación de un consumidor completo. |
| Cafe — variantes dinámicas | Hay launcher/resolver con seed, controladores de cutting/drag-drop/comparison/quiz, payloads, dos prefabs dinámicos encontrados (MG3 y MG4) y escenas DEV con `DynamicChallengeLauncher`. Existe una prueba PlayMode escrita para MG4 y pruebas EditMode de reglas. | No se corrieron esas pruebas en esta pasada. MG4 tenía validación automatizada reportada en handoff del 2026-09-25, pero seguía pendiente la paridad y el pointer manual. No se verificó una campaña dinámica completa ni todas las variantes en dispositivo. |
| Pizza Master — variantes dinámicas | Hay `RefactoredCuttingGame`, `RefactoredDragDropGame`, `RefactoredQuestionGame` y un `PizzaQuestionController` experimental sin seguimiento. | El flujo activo sigue siendo legacy según handoff; no se halló un prefab/escena conectado al nuevo controlador. No se demostró un segundo consumidor portable del contrato común. |

**Conclusión:** el trabajo está **parcialmente construido**, sobre todo en Cafe y en el paquete común; **no está terminado en Bakery** ni es todavía una base demostrada para todas las variaciones y el futuro juego. No sustituir un minijuego original hasta completar en una escena nueva un ciclo payload → intro/instrucciones → interacción → resultado → cleanup → segundo payload, con sus prefabs y pruebas. Los documentos `workspace/audits/slothy_three_projects_refactor_alignment_2026-09-25.md` y `slothy_pizza_second_consumer_audit_2026-09-25.md` citados por el handoff general no se localizaron en esta inspección; no asumir que están disponibles para otra sesión.

## Aclaración de producto: modo daily challenge multi-minijuego — 2026-10-17

**Pedido del dueño:** cada día debe ofrecer una sesión que saque aleatoriamente *algunos retos de los minijuegos*. Por ello, las nuevas variantes dinámicas deben poder recibir distintos contenidos/reglas y participar en una secuencia común. No confundir este modo con una misión pasiva tipo «completa N rondas». Tampoco reemplazar destructivamente los minijuegos originales.

**Brecha comprobada en código:** `DailyChallengeManager` de Bakery elige una sola definición de meta y acumula progreso por clave. En Cafe, `DynamicChallengeResolver.Resolve` elige un solo `MinigameDefinitionSO` y un solo `LevelDefinitionSO`; `DynamicChallengeLauncher.LoadToday` lo carga mediante `GameHost.LoadMinigame`. `GameHost` puede formar una playlist de niveles **dentro de un minijuego**, pero no se encontró un coordinador de una secuencia diaria **entre varios minijuegos**. Además, el launcher de Cafe no pasa a `GameHost` el `levelId` que el resolver eligió; se debe verificar la correspondencia entre el reto resuelto y el nivel efectivamente jugado.

**Contrato objetivo, no implementación existente:** una receta diaria ordenada con fecha/seed/versión y varias entradas `{minigameId, levelId o payload, dificultad, reglas}`, seleccionadas entre variantes compatibles; un orquestador que inicie cada entrada, reciba su resultado, limpie la instancia y avance a la siguiente; persistencia de receta/progreso para que el reto de un día no cambie a mitad de partida; intro, instrucciones, resultados y recompensas mediante prefabs/escenas editados en Edit Mode. Cada variante dinámica necesita aceptar una entrada de contenido y devolver un resultado de forma consistente. La cantidad de entradas, política de repetición, dificultad y si la selección diaria será igual para todos o específica por jugador siguen **sin decidir**. No afirmar que este modo está listo hasta probar una sesión completa en Unity y dispositivo.

**Continuación del 2026-10-17:** se inició un prototipo multi-minijuego en Cafe; detalles y validaciones pendientes en `../SlothysCafe/DAILY_CHALLENGE_HANDOFF_2026-10-17.md`. Está cableado solo en escena DEV, sin Play Mode validado ni UI final; no trasladar todavía a Bakery como feature terminada.
