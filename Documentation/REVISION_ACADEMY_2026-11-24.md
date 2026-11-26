# Revisión de Slothy's Bakery frente a Slothy's Café

Fecha 2026-11-24. Revisión estática del checkout local en commit base 93a7657, con cambios locales sin commit. Referencias: skill slothys-academy-product, guía de Café SLOTHY_LIBRARIES.md, FastTask y handoff de Bakery. No se ejecutó Unity ni se modificaron escenas, scripts o paquetes. El estado de las tiendas y de los builds instalados no se consultó.

## Resumen

Bakery tiene tres escenas de minijuego originales, Unity 6000.6, URP, localización y varios sistemas previos. Hay una base parcial del shell y del kit de feedback de Academy. Las rutas activas de juego y el shell requieren correcciones y pruebas antes de evaluar el producto como listo.

## Hallazgos con evidencia

### 1. Inicio y escenas — alta, confirmado estáticamente

El menú serializa `BtnIntroToScene` de `UI_Transition_Button`. Ese método usa el progreso para construir nombres `Minigame1`, `Minigame2` o `Minigame3`, mientras las escenas habilitadas se llaman `2. Minigame1`, `3. Minigame2` y `4. Minigame3`. La búsqueda por nombre no coincide. Verificar el botón real y su inicialización en Play Mode, y corregir usando referencias o IDs estables, preservando partidas existentes.

Build Settings también habilita `DEV_ModularMinigame` entre el menú y MG1. `InitialMenuManager.ToNextScene` y `UI_Transition_Button.ToScene()` avanzan al índice siguiente. Si alguno de esos caminos se usa desde el menú, abriría la escena DEV. Trazar cada botón y los scripts de build antes de modificar el orden.

### 2. Shell y Ajustes — alta, confirmado en fuente, compilación sin verificar

`Assets/Editor/Shell/BakeryShellBuilder.cs` está presente y crea tarjetas de Logros, Calificar, Compartir y Más juegos. El builder referencia `BakerySettingsBackend`, sin definición encontrada en Assets ni Packages del checkout, y la lista de la carpeta Shell contiene solo el builder. Se requiere verificar resolución de dependencias y compilación real en Unity, e implementar o recuperar el backend correspondiente. La presencia del builder no demuestra que el prefab haya sido generado, instalado ni conectado.

El builder configura el backend, cierre, canvas y tema, pero no se observaron enlaces de volúmenes, switches ni shortcuts. La escena contiene ajustes antiguos y eventos serializados. Verificar qué UI se muestra antes de retirar o reemplazar nada.

### 3. Guardado, modos y minijuegos — alta, conexión parcial

Las escenas activas referencian `Minigame1_B`, `Minigame2` y `Minigame3_B`. `Minigame1_Logic`, `Minigame2` y `Minigame3_B` escriben en `SaveLoadState`. Hay un `DailyChallengeManager` y los minijuegos notifican progreso, pero el código localizado no demuestra que exista la experiencia de Café de una ruta diaria con selección de capítulos, resumen y aislamiento de historia.

`GameFlowManager` se encontró directamente en la escena DEV, no en las tres escenas principales. La existencia de este prototipo no acredita que Historia, Práctica, Diario, Turno y Chef lo utilicen. Revisar cada controlador de principio a fin, rutas de éxito/error, reinicio, pausa, persistencia y salida. Aislar el guardado de Historia cuando se integren otros modos.

### 4. Feedback, animación y narrador — media, integración sin confirmar

`Packages/com.fatcat.feedback` contiene `UiChoreography`, `FeedbackStamp`, `FeedbackCelebration`, `CheckBellButton` y `IFeedbackFace`. La búsqueda de usos en scripts de Assets no encontró los ganchos equivalentes a Café. Las escenas originales contienen `CanvasAnimator` y transiciones antiguas, por lo que deben inspeccionarse en ejecución.

Adaptar personaje, explicaciones, acierto/error, sonidos, vibración y celebración a Bakery. Revisar el arrastre propio contra los aprendizajes de `DragItem` y comprobar que el feedback no oculte la actividad.

### 5. Progreso, social y contenido — media, parcialmente inventariado

Se encontraron `RatingManager` y `NotificationManager` serializados en el menú, además de lógica propia de retos. No se verificaron tablero de progreso, logros, práctica, rachas, diploma, compartir, repisa de amigos y puerta de adulto equivalentes a Café. Inventariar sus referencias y datos efectivos, adaptar las habilidades a Bakery y comprobar la protección de acciones externas.

Unity Localization está instalado y hay locales ES/EN y variantes. La cobertura de textos visibles, instrucciones, narración y continuidad del idioma no se ha probado.

### 6. Paquetes, render y distribución — media, verificación pendiente

URP está asignado en Graphics Settings y el manifest declara Unity Localization, Core y los cuatro paquetes FatCat compartidos. El Core usa ruta absoluta local. El kit de feedback está embebido; contrastar con la guía histórica de Café, que contiene afirmaciones contradictorias sobre su extracción.

Existen artefactos Android locales y ProjectSettings declara versión 1.0.1, código Android 10. Su procedencia, contenido, firma, subida, procesamiento y disponibilidad no se verificaron. No se encontró evidencia suficiente aquí para afirmar un build iOS disponible. No comparar números con tiendas sin consultar los registros actuales.

## Comparación con Pizza Master

Pizza cuenta con cuatro controladores activos, ruta de historia por ID, protección parcial de la sesión fuera de historia y pruebas aisladas de entrada en cuatro escenas y preservación del guardado. Antigravity sigue trabajando en él, por lo que hay que consultar su estado antes de una comparación definitiva.

Bakery tiene tres minijuegos propios, URP ya configurado y sistemas legacy de reto, rating y notificaciones. En el checkout revisado su ruta de inicio presenta nombres que no coinciden, el prototipo DEV está habilitado en Build Settings y el shell parece incompleto. Respecto a la integración y verificación reciente de Pizza, Bakery está más atrás en navegación y shell. La calidad jugable de ambos requiere pruebas equivalentes en dispositivo.

## Orden de trabajo propuesto

1. Resolver compilación del shell y reproducir los botones de inicio y continuación en un perfil aislado. Corregir rutas y acceso a DEV sin alterar progreso existente.
2. Documentar los tres minijuegos con el mismo ciclo: entrada, contenido, gesto, error, éxito, nivel, guardado, pausa, reinicio y salida. Probarlos en Historia.
3. Adaptar contexto de sesión y resultados a Práctica, Diario, Turno y Chef. Probar que las sesiones ajenas a Historia no escriban en su guardado.
4. Conectar UI, localización, narrador y feedback compartido, y después progreso, logros, diploma y acciones externas. Generar prefabs con builders y verificar 16:9 y 20:9.
5. Probar campañas y modos en Android e iOS, preparar builds y contrastar cada hito con las consolas. Registrar evidencia separada de generación, subida, procesamiento y dispositivo.

## Límites de esta revisión

El análisis se basa en scripts, referencias serializadas, manifest, Build Settings, archivos de proyecto y documentación. No se ejecutó compilación, Play Mode, comparación visual ni pruebas de tienda o hardware. La ausencia de una referencia directa no descarta instanciación por prefab o código; investigar los flujos antes de concluir que una función falta.
