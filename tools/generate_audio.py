"""Render the original Ember Gate foley and ambience using only Python's standard library."""

import array
import math
import random
import wave
from pathlib import Path


DEST = Path(__file__).resolve().parents[1] / "game/scripts/audio/samples"
RATE = 22050
TAU = 2 * math.pi


def render(name, seconds, seed):
    rng = random.Random(seed)
    count = round(seconds * RATE)
    frames = []
    low = mid = previous = crack = 0.0
    modes = ((431, .40), (697, .27), (1099, .23), (1813, .13), (2461, .07))
    for i in range(count):
        t = i / RATE
        white = rng.uniform(-1, 1)
        low += .017 * (white - low)
        mid += .19 * (white - mid)
        high = white - previous
        previous = white
        if name == "step":
            heel = math.exp(-32 * t)
            toe = math.exp(-47 * abs(t - .085))
            s = heel * (.53 * math.sin(TAU * (68 * t + .8 * (1 - math.exp(-55*t)))) + .43 * mid)
            s += toe * (.54 * mid + .05 * high) + .045 * math.sin(TAU*1270*t) * math.exp(-28*t)
        elif name == "attack":
            sweep = math.sin(math.pi * min(t/.32, 1)) ** 2
            s = sweep * (1.18 * mid + .09 * high + .16 * math.sin(TAU * (112*t + 625*t*t)))
            s += .085 * math.sin(TAU * (798*t - 170*t*t)) * math.exp(-11*abs(t-.21))
        elif name == "impact":
            s = .8 * mid * math.exp(-30*t) + .68 * math.sin(TAU*(67*t + .8*(1-math.exp(-40*t)))) * math.exp(-17*t)
            s += sum(level * math.sin(TAU*f*t) * math.exp(-(6+f/310)*t) for f, level in modes)
            s += .19 * high * math.exp(-75*t)
        elif name == "dodge":
            s = mid * math.sin(math.pi*min(t/.43,1))**2 + .13 * high * math.exp(-23*abs(t-.18))
            s += .14 * math.sin(TAU*(96*t - 46*t*t)) * math.exp(-8*abs(t-.21))
        elif name == "fire":
            if rng.random() < .00045:
                crack = rng.uniform(.32, .86)
            crack *= .982
            s = (low * 1.2 + mid*.17) * (.66 + .18*math.sin(TAU*3*t/seconds)) + crack*(.55*white+.25)
        elif name == "portal":
            swell = .72 + .22*math.sin(TAU*t/seconds)
            s = swell*(.29*math.sin(TAU*59*t + .18*math.sin(TAU*2*t/seconds)) +
                       .18*math.sin(TAU*89*t) + .08*math.sin(TAU*177*t + .8*math.cos(TAU*t/seconds)))
            s += .38*low
        else:
            breeze = .54 + .26*math.sin(TAU*t/seconds) + .13*math.cos(TAU*3*t/seconds)
            s = breeze * (low*1.6 + .13*mid)
            s += .13*math.sin(TAU*44*t + .2*math.sin(TAU*t/seconds)) + .072*math.sin(TAU*66*t)
            s += .035*math.sin(TAU*111*t + .4*math.cos(TAU*t/seconds))
        frames.append(s)

    loop = name in {"fire", "portal", "ambient"}
    if loop:
        # Cross-fade only the stochastic boundary. Tonal components span whole cycles.
        blend = int(.028*RATE)
        for i in range(blend):
            frames[i] = frames[i]*(i/blend) + frames[-blend+i]*(1-i/blend)
        frames = frames[:-blend]
    else:
        for i in range(int(.018*RATE)):
            frames[-1-i] *= i/(.018*RATE)
        for i in range(24):
            frames[i] *= i/24
    level = .84/max(abs(s) for s in frames)
    pcm = array.array("h", (round(32767*level*s) for s in frames))
    DEST.mkdir(parents=True, exist_ok=True)
    with wave.open(str(DEST / (name + ".wav")), "wb") as wav:
        wav.setparams((1, 2, RATE, 0, "NONE", "not compressed"))
        wav.writeframes(pcm.tobytes())


if __name__ == "__main__":
    for index, (effect, duration) in enumerate([
        ("step", .25), ("attack", .42), ("impact", .69), ("dodge", .44),
        ("fire", 4), ("portal", 5), ("ambient", 8)
    ]):
        render(effect, duration, 6041 + index)
