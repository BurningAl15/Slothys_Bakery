# Bakery — revisión de mecánicas, guardado y modalidades

Revisión estática del checkout del 24 de noviembre de 2026. Se siguieron las referencias serializadas de las escenas activas, sus controladores y el guardado. No se ejecutaron las mecánicas en Unity ni se modificaron scripts o escenas. Consultar también REVISION_ACADEMY_2026-11-24.md y el estado actual de FastTask.

## Cómo se revisó

1. Se leyó Build Settings y se identificaron las escenas de menú, los tres juegos y el final, junto con la escena DEV habilitada.
2. Para cada escena se buscaron los GUID de sus controladores y se siguieron inicialización, interacción, validación, avance, guardado y salida.
3. Se compararon las escrituras y la navegación con PracticeSession, CafeRun y CafeDaily de Café.
4. Se revisó el contenido serializado de rondas en MG2 y MG3 y las llamadas a analytics y reto diario.
5. Los resultados siguientes son conclusiones de código y configuración. Las pruebas de jugabilidad, táctiles, visuales, de disco y dispositivos están pendientes.

## Flujo transversal y guardado

### Lo existente

- `SaveLoadState` almacena JSON en PlayerPrefs. `BakeryProgressData` contiene `unlockedState`, completions y fecha, sin índice de ronda ni resultados por minijuego.
- Al inicializar, MG1 escribe estado 0 en `Minigame1_Logic.Init`, MG2 escribe 1 en `Minigame2.Start` y MG3 escribe 2 en `Minigame3_B.Init`. El menú consulta ese estado para continuar.
- Cada escena registra las rondas completadas mediante AnalyticsManager y llama a `DailyChallengeManager.AddProgress("complete_rounds", 1)`. El gestor diario guarda un contador y requiere definiciones configuradas. Hay que confirmar cómo se instancia en cada escena y sus definiciones.
- `GameFlowManager` tiene otra representación de progreso en PlayerPrefs y está referenciado directamente en DEV_ModularMinigame. Su presencia no demuestra que controle los tres minijuegos reales.

### Correcciones y decisiones

1. Corregir los nombres del menú de continuar. `UI_Transition_Button` construye Minigame1/2/3 y las escenas habilitadas llevan prefijos numéricos. La escena DEV está habilitada antes del primer minijuego y algunos caminos usan el siguiente índice.
2. Definir checkpoint con ID estable de minijuego y ronda, conservando y migrando el JSON existente. Al completar una ronda registrar el siguiente punto de reanudación sin perder un resultado previo, incluyendo el cierre durante animaciones y el cierre tras la última ronda.
3. Separar el contexto de Historia de Práctica, Diario, Turno y Chef. Las llamadas actuales escriben al entrar, de modo que un modo independiente podría cambiar el punto de continuación. Las sesiones ajenas a Historia deben empezar con el contenido del modo y preservar el guardado de Historia en todas las salidas.
4. Recoger resultados de aciertos y errores reales para resumen, estrellas y logros sin contar dos veces una pulsación o una repetición. El contador `complete_rounds` actual es un reto acumulado, no prueba la ruta diaria de Café.
5. En Café, Práctica ejecuta un tema y retorna al menú; Diario selecciona capítulos para el día; Turno y Chef encadenan capítulos y resumen. Café usa fecha local para su diario. Adaptar los tres juegos de Bakery con sus propios contenidos y respetar el progreso y las reglas que correspondan a cada modo.
6. Revisar que al volver al menú, reiniciar y abandonar se limpie el contexto adecuado y que una sesión incompleta no se marque como terminada. Un reinicio de capítulo durante un modo debe conservar el modo.

## MG1 — comparar fracciones paso a paso

**Escena activa:** `2. Minigame1.unity`. **Controladores:** `Minigame1_B` y `Minigame1_Logic`.

- La lista `compareFractions` contiene las rondas. Se presentan dos fracciones y el signo correspondiente.
- La lógica contiene etapas de comparación de denominadores, multiplicación para igualarlos y selección del signo. La respuesta se comprueba con `CheckDenominators`, `CheckInputValue` y `CheckSignInput`.
- `minigame1_Index` avanza en memoria después de una ronda correcta. Se completan la barra y el reto, pero no se encontró una escritura del índice de ronda en el guardado de campaña.
- La última ronda ejecuta narración/fade y `SceneUtils.ToNextScene`, que depende del índice de Build Settings.

**Corregir y validar:** reanudar el índice y restaurar de forma coherente elementos, fracciones, barra y etapa inicial de la ronda; comprobar denominadores iguales y distintos, operaciones inválidas y signo; evitar múltiples registros durante animaciones; probar pausa y repetición. El resultado de un modo debe indicar rondas y errores reales sin modificar Historia.

## MG2 — elegir el signo entre dos fracciones

**Escena activa:** `3. Minigame2.unity`. **Controlador:** `Minigame2`.

- Una lista de `Minigame4_Round` contiene objetos izquierdo y derecho, textos de fracción y `correctAnswer`. La escena tiene seis entradas, incluidos casos menor, mayor y equivalentes.
- El jugador selecciona un signo mediante `SignManager` y `ValueManager`; `CheckValue` compara el valor con la respuesta de la ronda, da feedback y, si acierta, inicia la transición.
- El índice es una variable privada inicializada en cero. `Start` guarda el capítulo 1. `Change` registra la ronda, limpia el signo y pasa a la siguiente. La última usa `SceneUtils.ToNextScene`.

**Corregir y validar:** reanudar ronda y elementos visuales; comprobar que cada respuesta serializada corresponda a las fracciones y al mapeo de signos; probar soltar dentro y fuera, selección vacía, errores y múltiples pulsaciones; validar limpieza del signo entre rondas y bloqueo de colliders durante transiciones. El resultado debe transportar aciertos/errores y el contexto de modo.

## MG3 — introducir una fracción según la comparación

**Escena activa:** `4. Minigame3.unity`. **Controlador:** `Minigame3_B`.

- Cada entrada de `optionsGroup` define el pan, tipo de pregunta (igual, mayor o menor) y valor de referencia. Hay seis entradas serializadas.
- `Option` lee un TMP_InputField, separa numerador y denominador por espacios o barra, calcula el valor y lo compara con `currentAnswer`. En igualdad usa tolerancia 0,01.
- El índice avanza en memoria; `Init` guarda capítulo 2. La última ronda carga el siguiente índice de escena.

**Defectos confirmados por lectura, reproducción pendiente:**
1. Una entrada que no se puede interpretar devuelve 0. En preguntas de «menor que» con referencia positiva, una entrada vacía o inválida puede evaluarse como correcta.
2. No se comprueba el denominador cero. La división en double puede producir Infinity o NaN, que luego entran en las comparaciones.
3. `Option` no comprueba `currentCoroutine` antes de evaluar. El coroutine impide iniciar otro avance, pero el sonido, feedback y analytics pueden repetirse mientras la respuesta es evaluada de nuevo.
4. La condición adicional `Math.Abs(...) > -0.01` es redundante. La tolerancia de 0,01 puede aceptar fracciones diferentes; decidir y probar si la regla educativa requiere igualdad racional exacta, respetando referencias y contenido existentes.

**Corregir y validar:** parseo con resultado explícito válido/inválido, denominador no cero, formato y espacios; fracciones equivalentes, mayores, menores y muy próximas; interacción durante transición; input, teclado móvil y localización. Al reanudar restaurar ronda y estado de la pregunta.

## Menú, transiciones y UI compartida

- Confirmar con botones reales la ruta de Jugar/Continuar y el estado de escenas. Evitar depender del orden de Build Settings.
- El fade actual usa `SceneTransitionController`; revisar cobertura y revelar tras cada escena bajo URP. No sustituir los gestos y animaciones existentes sin comprobar equivalencia.
- Integrar pausas y ayudas en cada juego. Pausar debe bloquear la interacción y continuar debe restaurar la ronda. Salir de una sesión independiente no debe cerrar ni completar la historia.
- Evaluar narrador, escritura, feedback de acierto/error, campana, sonidos y celebración con el estándar de Café, conservando recursos de Bakery y sin tapar la actividad. Revisar ES/EN y proporciones 16:9/20:9.

## Secuencia de implementación y pruebas

1. Reproducir entrada y navegación en copia de QA aislada. Corregir ruta de menú, DEV y migración de guardado, con pruebas de partidas existentes.
2. Corregir parseo y control de interacción de MG3. Añadir pruebas con entrada inválida, cero, equivalencias y doble toque.
3. Integrar checkpoint de ronda en Historia y probar cierre/reapertura en los tres juegos.
4. Integrar sesión por modalidad y resultado común, sin escribir en Historia. Probar cada juego por separado y una secuencia de varios capítulos, incluyendo abandono y reinicio.
5. Conectar las pantallas y reglas de Práctica, Diario, Turno y Chef, junto con resultados y estadísticas reales.
6. Revisar visuales, narrador y localización en Unity y dispositivos. Generar builds de prueba Android/iOS y registrar cada hito por separado.

## Matriz mínima de aceptación

Para cada minijuego: respuesta correcta e incorrecta, reintento, pausa/continuar, salir, reiniciar, cerrar/reabrir, última ronda y siguiente destino. Repetir en Historia y sesión fuera de Historia con una partida existente. Comparar el guardado antes y después de las sesiones independientes. Probar ES/EN, táctil, accesibilidad visual y formatos de pantalla. Registrar qué se jugó, qué se simuló y qué se probó en dispositivo.
