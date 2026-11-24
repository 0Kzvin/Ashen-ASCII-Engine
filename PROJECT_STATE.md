# Estado de ASHEN
Hito `v0.4-ascii-v3` en `experimental/ascii-v3`, a partir de `v0.3-ascii-v2` (`1cd5a64`). V3 abre por defecto en la rama. V2 continúa disponible como modo 5 y en main. V3 proyecta glifos en el metal, tela y espada del protagonista, recorta la llama en espacio de mundo y compone el Osario por pantalla con menos tinta sobre el pavimento.

Verificado en Forward+ Vulkan sobre RTX 4060 a 1920 × 1080. V3 exportado dio 452,47 FPS medios sin VSync, p95 2,65 ms y p99 2,84 ms. Pasaron las pruebas headless y gráficas de juego, los siete modos, perfiles, resolución y la exportación. Las comparativas con cámara fija y los giros del laboratorio están en docs/evidence/ascii-v3. ZIP y video quedan en build/ fuera de Git.

Archivos principales. `ascii_v3.gdshader`, `artistic_surface.gdshader`, `artistic_mask.glsl`, `AsciiRenderer.cs`, `GraphicsLab.cs` y `DemoValidation.cs`.

Próxima prueba. Cargar una malla artística que use las bandas de rugosidad de V3 y evaluar un búfer de identificadores explícito si la coincidencia afecta la composición. Los trazos del escenario todavía se vuelven a muestrear al mover la cámara.
