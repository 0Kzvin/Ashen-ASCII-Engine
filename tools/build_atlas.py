"""Rebuild the grayscale glyph atlas using only Python's standard library."""

import json
from pathlib import Path
import re
import struct
import zlib


FONT_DIR = Path(__file__).resolve().parents[1] / "game" / "assets" / "fonts"
TILE = 10  # 8 by 8 bitmap plus a one-texel transparent gutter on every side
DENSITY_COUNT = 16

# Supplemental 8px drawings. Each pair of hex digits describes one bitmap row,
# most significant bit on the left. All drawings use the same 8px em square.
SUPPLEMENT = {
    "ᛁ": "18 18 18 18 18 18 18 00",
    "ᛊ": "06 0c 18 30 18 30 60 00",
    "ᛏ": "18 3c 7e 5a 18 18 18 00",
    "ᛚ": "60 70 78 6c 60 60 60 00",
    "ᚲ": "0c 18 30 60 30 18 0c 00",
    "ᛉ": "42 5a 7e 3c 18 18 18 00",
    "ᚨ": "60 70 78 6c 78 6c 60 00",
    "ᚠ": "66 6c 78 66 6c 78 60 00",
    "ᛃ": "18 30 60 36 0c 18 30 00",
    "ᚱ": "78 6c 66 6c 78 6c 66 00",
    "ᚷ": "66 66 3c 18 3c 66 66 00",
    "ᛗ": "66 7e 7e 66 66 66 66 00",
    "ᛞ": "66 76 7e 7e 7e 6e 66 00",
    "ᚻ": "66 66 76 7e 6e 66 66 00",
    "─": "00 00 00 7e 7e 00 00 00",
    "╲": "40 60 30 18 0c 06 02 00",
    "│": "18 18 18 18 18 18 18 00",
    "╱": "02 06 0c 18 30 60 40 00",
}


def load_bitmaps():
    header = (FONT_DIR / "font8x8_basic.h").read_text(encoding="utf-8")
    glyphs = {}
    for row, code in re.findall(r"\{([^{}]+)\},?\s*// U\+([0-9A-Fa-f]{4})", header):
        # The upstream file stores the leftmost pixel in the least significant bit.
        data = re.findall(r"0x([0-9A-Fa-f]{2})", row)
        glyphs[chr(int(code, 16))] = [int(f"{int(n, 16):08b}"[::-1], 2) for n in data]
    glyphs.update({char: list(bytes.fromhex(rows)) for char, rows in SUPPLEMENT.items()})
    return glyphs


def chunk(name, data):
    return struct.pack(">I", len(data)) + name + data + struct.pack(">I", zlib.crc32(name + data))


def main():
    glyphs = load_bitmaps()
    sets = json.loads((FONT_DIR / "glyph_sets.json").read_text(encoding="utf-8"))
    bases = {family["name"]: family for family in sets}
    materials = json.loads((FONT_DIR / "material_sets.json").read_text(encoding="utf-8"))
    overrides = {item["material"]: item for item in materials}
    for material in ("tela", "magia", "piedra", "follaje", "agua", "metal", "fuego"):
        item = overrides[material]
        sets.append({**bases[item["base"]], **item, "name": material})
    width, height = (DENSITY_COUNT + 4) * TILE, len(sets) * TILE
    pixels = [bytearray(width) for _ in range(height)]
    for y, family in enumerate(sets):
        ramp, edges = family["density"], family["directions"]
        if len(ramp) != DENSITY_COUNT or len(edges) != 4 or " " not in ramp:
            raise ValueError(f"Invalid glyph count or missing space in {family['name']}")
        for char in ramp + edges:
            if len(glyphs.get(char, [])) != 8:
                raise ValueError(f"Missing 8px bitmap for {char!r}")
        ramp = sorted(ramp, key=lambda char: sum(row.bit_count() for row in glyphs[char]))
        for x, char in enumerate(ramp + list(edges)):
            for py, row in enumerate(glyphs[char]):
                for px in range(8):
                    pixels[y * TILE + py + 1][x * TILE + px + 1] = 255 if row & (128 >> px) else 0
    scanlines = b"".join(b"\0" + line for line in pixels)
    ihdr = struct.pack(">2I5B", width, height, 8, 0, 0, 0, 0)
    png = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", ihdr) + chunk(b"IDAT", zlib.compress(scanlines, 9))
    (FONT_DIR / "glyph_atlas.png").write_bytes(png + chunk(b"IEND", b""))
    print(f"glyph_atlas.png: {width} x {height}, {len(bases)} families + {len(overrides)} material rows")


if __name__ == "__main__":
    main()
