# Estado de ASHEN
Hito `v0.3-ascii-v2`. El Osario y el laboratorio son jugables en Godot 4.7.2 .NET. V2 abre por defecto. El modo 3 conserva el shader original y el 5 usa profundidad, normales y la rampa calibrada en GPU. ASCII Studio conserva perfiles JSON y permite comparar ambos.

Verificado en la RTX 4060 a 1920 × 1080. V2 en recorrido obtuvo 450,71 FPS medios sin VSync y 2,79 ms p95. Pasaron compilación, pruebas headless y GPU, captura de modos, combate, perfiles, pausa y pantalla completa. Pasó la ejecución del EXE exportado en el Osario y en el laboratorio. Arte y pruebas reproducibles en docs/evidence/ascii-v2 y scripts/validate.ps1. El ZIP Windows se genera en build/ y queda fuera de Git.

Archivos relevantes. `AsciiRenderer.cs`, `AsciiGeometry.cs`, `ascii_v2.gdshader`, `ascii_geometry.glsl`, `GraphicsLab.tscn` y `tools/build_atlas.py`.

La cuadrícula vive en pantalla. Los contornos finos pueden cambiar de celda con movimientos rápidos. Próxima tarea concreta. Medir una trayectoria repetida de cámara rápida para decidir si implementar reproyección de profundidad y comparar estabilidad con el mismo encuadre.
