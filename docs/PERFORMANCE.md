# Rendimiento de la demo

## V3 experimental
RTX 4060 y Ryzen 7 5700X, Godot 4.7.2 .NET, Forward+ Vulkan, 1920 × 1080, escala 3D del 100 %, VSync desactivado. El Osario usa el recorrido automático durante ocho segundos. El laboratorio usa el caballero en giro durante cinco segundos. Se descartan los primeros 160 cuadros.

| Escena y técnica | FPS medios | Mediana ms | p95 ms | GPU p95 ms |
| --- | ---: | ---: | ---: | ---: |
| Osario V2 | 454,73 | 2,22 | 2,73 | 1,88 |
| Osario V3 solo pantalla | 430,97 | 2,36 | 2,89 | 1,95 |
| Osario V3 híbrido, primera serie | 419,72 | 2,40 | 2,91 | 1,96 |
| Laboratorio V3 pantalla | 863,21 | 1,17 | 1,54 | 0,89 |
| Laboratorio V3 superficie | 1.094,71 | 0,90 | 1,29 | 0,70 |
| Laboratorio V3 híbrido | 851,00 | 1,18 | 1,54 | 0,90 |

Tras ajustar el tamaño de los emisores, el ejecutable V3 registró 3.620 cuadros en 8,004 s, **452,47 FPS** medios, 2,25 ms de mediana, **2,65 ms p95** y 2,84 ms p99. El intervalo de GPU p95 fue 1,89 ms. El presupuesto de 60 FPS es 16,67 ms. Las cifras de distintas tandas pueden variar. Repetir con `./scripts/validate.ps1 -Benchmark`.

## Referencia V2 del hito anterior
Medición del 23 de noviembre de 2026. Ryzen 7 5700X, RTX 4060, Godot 4.7.2 .NET, Forward+ Vulkan, pantalla completa 1920 × 1080, escala 3D al 100 %, VSync desactivado. El recorrido mueve la cámara en el Osario. Tras 160 cuadros de calentamiento, cada modo se midió durante ocho segundos sin capturas ni lectura de píxeles.

| Modo | Cuadros | FPS medios | Mediana ms | p95 ms | p99 ms | GPU p95 ms |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Escena 3D | 4.181 | 522,57 | 1,88 | 2,49 | 2,75 | 1,62 |
| ASCII original | 3.812 | 476,47 | 2,10 | 2,64 | 2,84 | 1,78 |
| ASCII V2 | 3.606 | 450,71 | 2,21 | 2,79 | 3,00 | 1,89 |

Los tiempos de cuadro son intervalos de pared del motor. La GPU se mide con el temporizador del viewport. El presupuesto de 60 FPS es 16,67 ms por cuadro. Se trata de una muestra corta en una sola GPU. Ejecuta `./scripts/validate.ps1 -Benchmark` para repetirla.

V2 genera profundidad y normales por celda y reserva la composición final para una pasada de pantalla. La versión original y el modo 3D desactivan el efecto de cálculo. Los modos de baja calidad reducen la escala 3D y aumentan el tamaño de celda. Para el prototipo aislado anterior, consulta [Renderizado](RENDERING.md).
