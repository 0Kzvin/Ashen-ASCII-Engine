# Rendimiento de la demo
Medición del 23 de noviembre de 2026. Ryzen 7 5700X, RTX 4060, Godot 4.7.2 .NET, Forward+ Vulkan, pantalla completa 1920 × 1080, escala 3D al 100 %, VSync desactivado. El recorrido mueve la cámara en el Osario. Tras 160 cuadros de calentamiento, cada modo se midió durante ocho segundos sin capturas ni lectura de píxeles.

| Modo | Cuadros | FPS medios | Mediana ms | p95 ms | p99 ms | GPU p95 ms |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Escena 3D | 4.181 | 522,57 | 1,88 | 2,49 | 2,75 | 1,62 |
| ASCII original | 3.812 | 476,47 | 2,10 | 2,64 | 2,84 | 1,78 |
| ASCII V2 | 3.606 | 450,71 | 2,21 | 2,79 | 3,00 | 1,89 |

Los tiempos de cuadro son intervalos de pared del motor. La GPU se mide con el temporizador del viewport. El presupuesto de 60 FPS es 16,67 ms por cuadro. Se trata de una muestra corta en una sola GPU. Ejecuta `./scripts/validate.ps1 -Benchmark` para repetirla.

V2 genera profundidad y normales por celda y reserva la composición final para una pasada de pantalla. La versión original y el modo 3D desactivan el efecto de cálculo. Los modos de baja calidad reducen la escala 3D y aumentan el tamaño de celda. Para el prototipo aislado anterior, consulta [Renderizado](RENDERING.md).
