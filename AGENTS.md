# ASHEN
Demo jugable de ruinas medievales con renderizado ASCII en GPU. Trabajar en Godot 4.7.2 .NET, C# y Forward+.

## Continuar
Leer PROJECT_STATE.md y las tareas activas de TASKS.md. Abrir únicamente el documento técnico del sistema afectado. Las pruebas gráficas se ejecutan con una GPU real.
## Organización
La escena principal compone mundo, personajes, cámara, sonido, renderizador y panel de arte. El HUD se dibuja después del shader. Recursos importables en game/assets, generadores en tools.
## Trabajo y validación
- Usar nombres descriptivos y mantener reemplazables mallas, audio y atlas.
- Ejecutar `./run.ps1 -BuildOnly` tras cambios C#. Ejecutar la validación en scripts y revisar capturas antes de marcar cambios visuales como listos.
- Mantener lecturas GPU a CPU fuera del juego, salvo captura pedida explícitamente.
- Trabajar en este repositorio privado, en main o ramas breves. Comprobar git status, seleccionar archivos explícitamente y etiquetar hitos probados.
- Actualizar PROJECT_STATE.md y TASKS.md al cerrar un hito. Excluir herramientas descargadas, cachés y credenciales.
## Referencias
VISION, ARCHITECTURE, RENDERING, ART_PIPELINE, TESTING, DECISIONS y PERFORMANCE se encuentran en docs/.
