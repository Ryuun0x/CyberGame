"""The game's background loops, from the user's AudioClip download (ArtSource/Sounds/packs/AudioClip).
Each is softened, levelled to the same loudness, and made seamless (its end crossfaded into its start), then written
to Assets/Resources/Ambience/<name>.ogg, which Assets/Script/Core/Ambience.cs plays.
  py -3.11 ArtSource/Sounds/make_ambience.py
"""
import os
import sys

import numpy as np
import soundfile as sf

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import pick_kenney as pk  # noqa: E402

SRC = os.path.join(pk.HERE, "packs", "AudioClip")
OUT = os.path.join(os.path.dirname(pk.OUT), "Ambience")
R = pk.RATE
# game name: (source clip, seconds to use, low-pass cutoff)
LOOPS = {
    "room": ("room-ambience", 27, 4000),        # a quiet room tone: home, and under the café
    "wind": ("wind", 36, 2500),                 # low wind: outside in the city, and faintly at home at night
    "cafe_hum": ("fluorescent", 23, 2500),      # the café's lights humming (its hiss taken down)
    "laptop": ("computer-idle", 30, 3000),      # the laptop's fan while it's in use
}


def loop(y, crossfade=2.0):
    """Seamless: the last `crossfade` seconds fade into the first ones (equal-power, for noisy sounds)."""
    n = int(R * crossfade)
    out = y[:len(y) - n].copy()
    t = np.linspace(0, np.pi / 2, n)
    out[:n] = y[:n] * np.sin(t) + y[len(y) - n:] * np.cos(t)
    return out


def menu(seconds=24):
    """The start menu: a slow, dark pad (A minor, then F) over a deep rumble. Every frequency and swell repeats a
    whole number of times in `seconds`, so the pad loops exactly; the rumble is made seamless like the others."""
    t = np.arange(R * seconds) / R
    q = lambda f: round(f * seconds) / seconds  # whole cycles per loop
    def chord(freqs):
        out = np.zeros(len(t))
        for i, f in enumerate(freqs):
            swell = 0.6 + 0.4 * np.sin(2 * np.pi * t * (i + 1) / seconds + i)
            for detune in (-3, 0, 3):  # three voices a hair apart: a slow shimmer
                g = q(f) + detune / seconds
                out += swell * (np.sin(2 * np.pi * g * t) + 0.25 * np.sin(4 * np.pi * g * t))
        return out
    a = 0.5 + 0.5 * np.cos(2 * np.pi * t / seconds)  # A minor at the start and end, F in the middle
    pad = a * chord([110, 164.81, 261.63]) + (1 - a) * chord([87.31, 130.81, 220])
    pad = pk.lowpass(np.tile(pad, 2), 1200)[len(pad):]  # filtered twice round, so it starts already settled
    rumble = pk.load(os.path.join(SRC, "deep-rumble-ambience.ogg"))[:R * (seconds + 2)]
    rumble = loop(pk.lowpass(rumble - np.mean(rumble), 1500))
    rms = lambda y: np.sqrt(np.mean(y ** 2))
    return pad / rms(pad) + 0.7 * rumble / rms(rumble)


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    loops = {name: loop(pk.lowpass(y - np.mean(y), cutoff)) for name, (clip, seconds, cutoff) in LOOPS.items()
             for y in [pk.load(os.path.join(SRC, clip + ".ogg"))[:R * seconds]]}
    loops["menu"] = menu()
    for name, y in loops.items():
        y *= 0.1 / (np.sqrt(np.mean(y ** 2)) + 1e-9)  # same loudness for all; Ambience.cs sets each one's level
        y *= min(1.0, 0.9 / np.max(np.abs(y)))
        assert abs(y[-1] - y[0]) < 0.2, name  # no jump at the loop point
        # In small blocks: libsndfile's Vorbis encoder overflows the stack if handed a long file in one write.
        with sf.SoundFile(os.path.join(OUT, name + ".ogg"), "w", R, 1, format="OGG", subtype="VORBIS") as f:
            for i in range(0, len(y), 4096):
                f.write(y[i:i + 4096])
        print(name, f"{len(y) / R:.1f}s")
