# Incorporación de arte
La escala de autoría es de un metro por unidad, con Y como eje vertical. Usar el suelo en y=0 y conservar las siluetas a la distancia de la cámara de juego. Abrir ASCII Studio y comparar la escena 3D con cine para comprobar materiales, bordes y exposición.
## Geometría y animación
Exportar desde Blender a GLB con transformaciones aplicadas, texturas PNG y animaciones incluidas. Conservar el BLEND de trabajo en art_source/ cuando se incorpore arte externo. Validar en Godot las normales, materiales y orientación del modelo. Para mecanismos de sustitución y posiciones concretas, consultar las notas técnicas del mundo y de los personajes.
## Color
Usar azul gris para piedra, grises neutros para metal, verde para follaje, rojo para tela, ámbar para fuego, azul saturado para agua y cian o violeta para magia. El color resultante depende de la luz. Ver docs/RENDERING.md para editar las filas de caracteres.
## Audio
tools/generate_audio.py sintetiza los siete WAV del juego con Python. Los recursos en game/scripts/audio/samples se pueden sustituir manteniendo nombres. Para música o ambientes OGG, importar el AudioStreamOggVorbis y asignarlo al reproductor correspondiente. Mantener efectos de posición en mono y revisar el empalme de los bucles.
## Interfaz y licencias
La marca geométrica original está en game/assets/ui/sigil.svg. Godot importa SVG y PNG como texturas. Barlow Medium y Cormorant Garamond dan forma a la interfaz. Ambas familias llevan su licencia SIL Open Font License en el mismo directorio. Se pueden reemplazar por TTF/OTF y reasignar en InterfaceStyle. El atlas bitmap de gameplay tiene su propia fuente y licencia en game/assets/fonts.
- [Barlow](https://github.com/google/fonts/tree/main/ofl/barlow)
- [Cormorant Garamond](https://github.com/google/fonts/tree/main/ofl/cormorantgaramond)
