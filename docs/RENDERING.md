# Renderizado ASCII

La escena 3D ilumina los materiales en Forward+. Un `ColorRect` de pantalla completa lee el color de esa escena mediante `hint_screen_texture` y compone los caracteres en la GPU. El HUD y los controles de arte se dibujan en capas posteriores. La clase `AsciiRenderer` actualiza parámetros al cambiar el perfil y carece de bucle por fotograma.

## Modos

| Índice | Modo | Lectura de la imagen |
| --- | --- | --- |
| 0 | 3D | Oculta el rectángulo y deja visible la escena iluminada. |
| 1 | Monocromo | Convierte la luminancia en densidad y brillo de tinta sobre un fondo oscuro. |
| 2 | Color | Conserva el color medio de cada celda y atenúa el fondo para destacar los caracteres. |
| 3 | Cinematográfico | Combina glifos con el color continuo del escenario. Conserva la niebla y las siluetas finas. Con el conjunto Clásico elige caracteres por material. |
| 4 | Diagnóstico | Muestra la densidad en gris. Marca contornos horizontales en ámbar, verticales en turquesa, diagonales descendentes en violeta y ascendentes en rosa. |

## Cómo se forma una celda

1. El ancho se redondea a píxeles físicos y la altura equivale aproximadamente a 1,36 veces el ancho. La cuadrícula parte de la esquina superior izquierda del viewport.
2. El shader lee el centro y cuatro puntos adyacentes. Obtiene el color medio y calcula la luminancia, con una contribución del canal más intenso para conservar emisiones saturadas.
3. Exposición y contraste ajustan la señal. Una matriz Bayer de 4 × 4 celdas suaviza los saltos entre los 16 niveles de densidad. Cada umbral depende de la posición de celda y se mantiene fijo en el tiempo.
4. Las diferencias de luminosidad entre izquierda y derecha, y entre arriba y abajo, dan la dirección del gradiente. En un contorno fuerte el shader selecciona un trazo tangente entre cuatro orientaciones. Una transición gradual mezcla el trazo con el glifo de densidad.
5. En cine, la composición conserva parte del color en la coordenada original del píxel y aplica un viñeteado suave. Los otros modos trabajan con las muestras por celda. El shader reutiliza coordenadas de celda, pero el hardware ejecuta el fragmento por píxel de salida.

El control de estabilidad aumenta el área de muestreo y reduce el tramado. Es un filtro espacial. Un detalle que cruza el límite de una celda puede cambiar de carácter durante el movimiento. El coste del pase crece principalmente con la resolución de salida.

## Parámetros de arte

| Parámetro | Intervalo | Efecto |
| --- | --- | --- |
| Celda | 4–16 px | Ancho físico del carácter. Para inspeccionar su dibujo, usar 9–12 px. |
| Densidad | 0–2 | Desplaza la selección hacia caracteres con más o menos tinta. |
| Contraste | 0–2 | Expande o comprime la escala de densidad alrededor de 0,26. |
| Brillo | 0–2 | Escala la intensidad de la composición final. |
| Saturación | 0–2 | Ajusta el color de los modos 2 y 3. |
| Exposición | 0–2 | Escala la señal que alimenta la selección de glifos y la tinta en color. |
| Bordes | 0–2 | Intensidad de la detección de contornos. Cero mantiene los glifos de densidad. |
| Estabilidad | 0–1 | Ajusta la huella de muestreo y la amplitud de Bayer. |
| Paleta | Ceniza, Lunar, Cobre | Colorea el papel y la tinta. En cine el tinte es suave para conservar los colores del mundo. |
| Conjunto | Clásico, Rúnico, Piedra, Follaje, Agua, Metal, Brasas | En cine, Clásico activa la selección por material. Los demás conjuntos fuerzan una familia en toda la escena. |

## Editar los alfabetos

Las siete filas globales se editan en `game/assets/fonts/glyph_sets.json`. Cada entrada contiene un nombre, una cadena `density` de 16 caracteres con un espacio, y cuatro caracteres en `directions` en el orden horizontal, diagonal descendente, vertical y diagonal ascendente. El generador ordena los 16 caracteres por superficie de tinta y conserva el orden de entrada en los empates.

El modo cine estima el material a partir del color medio previamente iluminado. Distingue piedra gris azulada, follaje verde, agua azul, metal claro con baja saturación, fuego ámbar, magia violeta o cian y tela carmesí. Luces de color o materiales con colores similares pueden activar la misma fila.

Cada material tiene su propia fila en `game/assets/fonts/material_sets.json`. Para cambiar la escritura del agua, por ejemplo, edita su campo `base` o añade una cadena `density` de 16 caracteres. Se puede sobrescribir también `directions`. Las filas de tela y magia se pueden editar de forma independiente. Los nombres de bases corresponden a los nombres de las siete familias globales.

Desde la raíz del repositorio, con Python 3.10 o posterior, ejecuta:

```powershell
python tools/build_atlas.py
```

El resultado es `game/assets/fonts/glyph_atlas.png`, una imagen en escala de grises de 200 × 140 px. Cada glifo de 8 × 8 ocupa un cuadro de 10 × 10 con margen libre. El proyecto importa la imagen sin pérdida y el shader muestrea el canal rojo. El generador usa exclusivamente la biblioteca estándar de Python. El juego distribuido carga el PNG ya generado.

La fuente latina procede de **font8x8** de Daniel Hepper, basada en las fuentes VGA de IBM y el trabajo de Marcel Sondaar. La revisión y la declaración de dominio público se conservan en `game/assets/fonts/LICENSE.txt` y en el archivo fuente. Las runas y los cuatro trazos adicionales se dibujaron para la demo y están disponibles bajo CC0. [Fuente original y licencia](https://github.com/dhepper/font8x8/blob/8e279d2d864e79128e96188a6b9526cfa3fbfef9/font8x8_basic.h).

## Prueba reproducible de GPU

Ejecuta `./tools/test_renderer.ps1` en Windows con una GPU y el editor .NET. La prueba compila una escena independiente con el shader, el atlas y la clase del juego. Genera siete muestras de material bajo iluminación fija, comprueba seis capturas distintas y escribe PNG, registro y CSV en `.local/render-poc/captures`. El parámetro `-Godot` permite seleccionar el editor. El registro indica la resolución efectiva.

El 23 de noviembre de 2026, Godot 4.7.2 .NET y Vulkan Forward+ sobre una RTX 4060 dibujaron la prueba a 1920 × 1080 a pantalla completa, con sincronización vertical desactivada. Se descartaron 48 fotogramas por modo y se midieron los 180 siguientes con el temporizador GPU del viewport. La captura ocurrió después de cada intervalo. La celda medía 9 px de ancho, con densidad 1,06 y estabilidad 0,75. Estas cifras corresponden a toda la escena de muestras. El rendimiento del nivel jugable se mide por separado.

| Modo | GPU p50 (ms) | GPU p95 (ms) |
| --- | ---: | ---: |
| 3D | 0,415 | 0,436 |
| Monocromo | 0,496 | 0,514 |
| Color | 0,497 | 0,512 |
| Cinematográfico | 0,515 | 0,527 |
| Diagnóstico | 0,495 | 0,504 |
| Cine con runas | 0,508 | 0,520 |

La composición de seis capturas muestra los cambios de modo. El recorte a escala nativa conserva las formas de tela, runas, piedra y follaje. [Comparación](evidence/render-poc/modes.png) · [Detalle nativo](evidence/render-poc/cinematic-detail.png) · [Muestras de GPU y tiempo entre fotogramas](evidence/render-poc/samples.csv).
