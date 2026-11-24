"""Build the V3 monospaced atlas from the project's licensed 8x8 bitmaps."""
from build_atlas import FONT_DIR, load_bitmaps, write_png

# Rows are plate, cloth, flame, stone, foliage, magic, water and silhouettes.
# Each row shares 16 columns. The density increases approximately from left to right.
ROWS = (
    " /\\|+[]<>xX%O#@M",
    " :vᛁᛊᛚᚲᛉᚨᚠᛃᚱᚷᛗᛞᚻ",
    " +*^/\\sxa%o8#@&M",
    " ─│╱╲┌┐└┘├┤┬┴┼X#",
    " 'vryTYwVfYt%*#W",
    " +ᛁᛊᛚᛏᚲᛉᚨᚠᛃᚱᚷᛗᛞᚻ",
    " ~-=_─╱╲sxz≈≋+OX",
    " ─│╱╲┌┐└┘├┤┬┴┼X#",
)

BOX = {
    "─": "00 00 00 ff 00 00 00 00", "│": "10 10 10 10 10 10 10 10",
    "┌": "00 00 00 1f 10 10 10 10", "┐": "00 00 00 f0 10 10 10 10",
    "└": "10 10 10 1f 00 00 00 00", "┘": "10 10 10 f0 00 00 00 00",
    "├": "10 10 10 1f 10 10 10 10", "┤": "10 10 10 f0 10 10 10 10",
    "┬": "00 00 00 ff 10 10 10 10", "┴": "10 10 10 ff 00 00 00 00",
    "┼": "10 10 10 ff 10 10 10 10", "≈": "00 00 62 9c 00 62 9c 00",
    "≋": "00 62 9c 62 9c 62 9c 00",
}

def main():
    bitmaps = load_bitmaps()
    bitmaps.update({char: bytes.fromhex(bitmap) for char, bitmap in BOX.items()})
    assert all(len(row) == 16 for row in ROWS)
    pixels = [bytearray(16 * 10) for _ in range(len(ROWS) * 10)]
    for row, ramp in enumerate(ROWS):
        for col, char in enumerate(ramp):
            for y, bits in enumerate(bitmaps[char]):
                for x in range(8):
                    pixels[row*10 + y+1][col*10 + x+1] = 255 if bits & (128 >> x) else 0
    write_png(FONT_DIR / "artistic_atlas.png", 160, len(ROWS)*10, pixels)

if __name__ == "__main__":
    main()
