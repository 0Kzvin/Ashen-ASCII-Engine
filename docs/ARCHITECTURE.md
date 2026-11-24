# Arquitectura
`game/scenes/Main.tscn` arranca la raíz C# Main. Instancia Ruins, Soundscape, dos Knight y OrbitRig. Los cuerpos operan a frecuencia física fija. El mundo, audio, personajes y cámara usan ProcessMode.Pausable. La raíz permanece activa para gestionar la pausa.
El mundo termina de dibujarse antes de la capa CanvasLayer 2. AsciiRenderer compone esa imagen en GPU. La capa 5 dibuja HUD, diagnóstico, ASCII Studio y menú a resolución de interfaz. Solo las capturas explícitas de QA leen el viewport a CPU.
OrbitRig interpola foco, altura, distancia y orientación. Un rayo en la capa de escenario acorta la distancia contra obstáculos. El puntero se proyecta sobre el plano horizontal del personaje. El recorrido automatizado alterna foco entre el portal y el caballero.
ArtProfile agrupa ajustes serializables. Tres ranuras JSON en user:// permiten guardar y cargar. El cambio de calidad también modifica la escala 3D interna. El atlas conserva su tamaño de celda en píxeles de salida.
DemoValidation se activa exclusivamente con argumentos propios después de `--`. Los scripts de scripts/ invocan los mismos componentes que la escena interactiva.
