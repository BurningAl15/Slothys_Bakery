# Bakery — contenido dinámico y seguridad de entrada de minijuegos

Fecha: 2026-11-24. FastTask: `58597408-1a77-4a59-83b2-ca67ae21798a`.

## Propósito

Atender hallazgos estáticos de la revisión de los tres minijuegos propios de Bakery sin reemplazar sus mecánicas educativas ni convertirlos al minijuego modular. Esta entrega cubre el primer incremento; no declara paridad funcional completa.

## Cambios implementados

### MG3 — ingresar una fracción y compararla

- El parser acepta `numerador/denominador` o dos enteros separados por espacio y exige exactamente dos componentes.
- Campo vacío, sintaxis incompleta, tokens adicionales y denominador cero se consideran respuesta incorrecta; ya no se convierten en el valor 0.
- La respuesta correcta bloquea el campo y el callback mientras se ejecuta la transición. El evento correcto, la animación y el analytics no se disparan otra vez durante esa ronda.
- Al inicializar, el controlador valida la lista de preguntas, índices de texto, referencias usadas por las animaciones, curva/velocidad de animación y valores numéricos finitos. Si hay datos incompletos, deshabilita la interacción y deja un error identificable en Console.
- La comparación de equivalencia conserva la tolerancia existente de 0.01. Confirmar la regla pedagógica antes de cambiarla a equivalencia racional exacta.

### MG2 — rondas configurables y reinicialización del selector de signos

- `Minigame2` valida la lista de rondas, referencias visuales, waypoints, respuestas y curva/velocidad de animación antes de habilitar el flujo. Una referencia incompleta deja el botón deshabilitado y un error claro en Console, en vez de fallar al tocar.
- La inicialización reinicia el índice y el estado de respuesta, de modo que una nueva entrada empieza desde la primera ronda. El avance habilita una sola respuesta por ronda y reinicia esa protección al cambiar de ronda.
- `SignManager.Initialize()` reconstruye el registro sin duplicados y conserva la posición inicial por instancia de `SignSelectable`, no por enum de signo. Así admite piezas añadidas/reordenadas y dos instancias del mismo tipo. Los selectables nulos se omiten también en reset/colisiones.
- La decisión de soltar se toma en `OnMouseUp`: sincroniza los transforms antes de consultar física y acepta cuando los bounds de la pieza intersectan bounds de un collider de destino. Ya no depende de que el centro de la pieza caiga dentro de un círculo fijo. El asset de escena ya serializa los rigidbodies de signos como Kinematic/Never Sleep; no hubo que cambiarlo.

### MG1 — rondas autoradas por listas

- Antes de arrancar, MG1 comprueba que la lista de rondas, las preguntas de sus tres etapas, la curva de animación, los sprites por signo y las rutas de movimiento estén completos.
- Cada fracción valida denominador positivo, referencias visuales y cantidad suficiente de sprites para representar su numerador. Una lista de rondas puede cambiar de longitud sin que el controlador salga de rango; un dato imposible falla de forma explícita en vez de lanzar una excepción durante la animación.
- El render de cada fracción limita el bucle de pintado a las piezas que realmente tiene, como protección adicional.
- La entrada del factor ahora exige dos enteros positivos, iguales y completos; un campo inválido deja de convertirse en `(1,1)`. La inicialización resetea `multiplyWith` a 1 y el acierto de signo solo puede contabilizarse una vez.

## Evidencia y límites

- Confirmado por lectura: antes de la corrección, MG3 devolvía 0 ante una fracción no parseable, permitía dividir por cero y podía emitir feedback/analytics repetidos mientras `Change` aún no terminaba.
- Confirmado por lectura: `SignManager.Initialize()` hacía `Add` a la lista y diccionario en cada llamada sin limpiar ni conservar un estado inicial.
- Confirmado por lectura: MG2 asumía que todas las rondas y waypoints estaban asignados y consultaba `ValueManager` sin comprobarlo; además, la comprobación de respuesta no bloqueaba el callback hasta el avance de ronda. Su registro de posiciones era por enum, que no representa instancias repetidas, y el drop usaba un `OverlapCircle` fijo sin `SyncTransforms` explícito.
- Confirmado por lectura: MG1 indexaba `positions_*[0]`, la lista de prompts y cada sprite con supuestos implícitos, sin validar el tamaño del contenido serializado; el parser de factor reemplazaba entradas inválidas por `(1,1)`.
- No se ejecutó Unity, compilación, suite, Play Mode, prueba táctil ni build en este cambio. La corrección está implementada, pero queda pendiente validarla en Editor y dispositivo; FastTask permanece `doing`.

## Siguiente trabajo de minijuegos

1. Probar MG3 con fracciones correctas, incorrectas, equivalentes, campo vacío, `1/0`, `1//2`, texto extra y doble toque mientras transiciona.
2. Revisar MG1: configuración de fracciones y sprites con otras cantidades, transición para listas de distinto tamaño y reanudación al salir.
3. Revisar MG2: contenido/respuesta serializada por ronda, arrastre/liberación, listas de distinta longitud e inicialización de escena.
4. Validar MG2 en Unity: arrastrar a la zona y fuera, piezas repetidas del mismo signo, selector, rondas de distinta longitud y reinicio.
5. Diseñar checkpoint por ID de minijuego y ronda, luego aislar Historia de Práctica/Diario/Turno/Chef sin sobrescribir el guardado de Historia.
6. Ejecutar validación en Unity y revisar visualmente las proporciones requeridas cuando esté disponible el Editor.

Pizza Master queda fuera de este alcance y sigue asignado a Antigravity.
