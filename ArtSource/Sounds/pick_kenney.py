"""Turns recorded CC0 sounds (Kenney packs in ArtSource/Sounds/kenney) into the game's UI sounds.

Each game sound (the names Assets/Script/Core/Sfx.cs plays) comes from one recording, tuned to the game's dark
style: pitched a little lower, the top end softened, a touch of room. Nothing is synthesized here.
  python ArtSource/Sounds/pick_kenney.py preview   candidates for every sound in one file per sound, to choose from
  python ArtSource/Sounds/pick_kenney.py           writes the chosen ones (CHOSEN) to Assets/Resources/Sfx/*.wav
"""
import glob
import os
import sys
import wave

import numpy as np
import soundfile as sf

RATE = 44100
HERE = os.path.dirname(os.path.abspath(__file__))
KENNEY = os.path.join(HERE, "kenney")
OUT = os.path.join(os.path.dirname(os.path.dirname(HERE)), "Assets", "Resources", "Sfx")
PREVIEW = os.path.join(HERE, "preview")
rng = np.random.default_rng(4)

# Candidates per game sound: (pack, file name without .ogg). The first one is the current pick.
CANDIDATES = {
    "click": [("ui-audio", "click3"), ("interface-sounds", "click_002"), ("interface-sounds", "select_004"),
              ("interface-sounds", "tick_002"), ("ui-audio", "mouseclick1")],
    "toggle_on": [("interface-sounds", "toggle_002"), ("ui-audio", "switch7"), ("interface-sounds", "switch_003"),
                  ("rpg-audio", "metalClick")],
    "toggle_off": [("interface-sounds", "toggle_003"), ("ui-audio", "switch8"), ("interface-sounds", "switch_004"),
                   ("rpg-audio", "metalLatch")],
    "pause": [("rpg-audio", "bookClose"), ("rpg-audio", "cloth1"), ("impact-sounds", "impactSoft_medium_000"),
              ("interface-sounds", "minimize_005")],
    "resume": [("rpg-audio", "bookOpen"), ("rpg-audio", "cloth2"), ("interface-sounds", "maximize_005"),
               ("rpg-audio", "bookFlip1")],
    "phone_open": [("rpg-audio", "clothBelt"), ("rpg-audio", "cloth3"), ("interface-sounds", "open_002"),
                   ("rpg-audio", "beltHandle1")],
    "phone_close": [("rpg-audio", "clothBelt2"), ("rpg-audio", "cloth4"), ("interface-sounds", "close_002"),
                    ("rpg-audio", "beltHandle2")],
    "objective": [("interface-sounds", "confirmation_002"), ("interface-sounds", "pluck_001"),
                  ("rpg-audio", "bookPlace1"), ("interface-sounds", "glass_002")],
}
CHOSEN = {name: options[0] for name, options in CANDIDATES.items()}


def find(pack, name):
    hits = glob.glob(os.path.join(KENNEY, f"kenney_{pack}", "**", name + ".ogg"), recursive=True)
    if not hits:
        raise FileNotFoundError(f"{pack}/{name}.ogg")
    return hits[0]


def load(path):
    y, rate = sf.read(path, always_2d=True)
    y = y.mean(axis=1)
    if rate != RATE:  # resample (linear is fine for short UI sounds)
        y = np.interp(np.arange(0, len(y), rate / RATE), np.arange(len(y)), y)
    return y


def lowpass(y, cutoff):
    k = 1 - np.exp(-2 * np.pi * cutoff / RATE)
    out = np.empty_like(y)
    acc = 0.0
    for i, v in enumerate(y):
        acc += k * (v - acc)
        out[i] = acc
    return out


def darken(y, pitch=0.88, cutoff=5500, room=0.12, room_seconds=0.35):
    """Lower (slowed playback, like a slightly bigger object), softer top, a small dark room."""
    y = np.interp(np.arange(0, len(y), pitch), np.arange(len(y)), y)
    y = 0.6 * lowpass(y, cutoff) + 0.4 * lowpass(y, cutoff * 0.4)
    n = int(RATE * room_seconds)
    ir = lowpass(rng.normal(0, 1, n), 2500) * np.exp(-np.arange(n) / (RATE * room_seconds / 5))
    ir /= np.sqrt(np.sum(ir ** 2)) + 1e-9
    wet = np.convolve(y, ir)
    out = np.zeros(len(wet))
    out[:len(y)] += y
    out += wet * room
    loud = np.nonzero(np.abs(out) > 0.002 * np.max(np.abs(out)))[0]
    out = out[:loud[-1] + 1]
    fade = min(len(out), int(RATE * 0.01))
    out[-fade:] *= np.linspace(1, 0, fade)
    return out


def write(path, y, peak=0.45):
    y = y / (np.max(np.abs(y)) + 1e-9) * peak
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes((y * 32767).astype(np.int16).tobytes())


if __name__ == "__main__":
    if sys.argv[1:] == ["preview"]:
        # One file per game sound: its candidates in order (numbered 1, 2, 3...), each twice, with gaps.
        for name, options in CANDIDATES.items():
            parts = []
            for pack, file in options:
                y = darken(load(find(pack, file)))
                y = y / (np.max(np.abs(y)) + 1e-9) * 0.45
                parts += [y, np.zeros(int(RATE * 0.4)), y, np.zeros(int(RATE * 1.0))]
            write(os.path.join(PREVIEW, f"choose_{name}.wav"), np.concatenate(parts))
            print(name, ", ".join(f"{i + 1}={p}/{f}" for i, (p, f) in enumerate(options)))
    else:
        for name, (pack, file) in CHOSEN.items():
            write(os.path.join(OUT, name + ".wav"), darken(load(find(pack, file))))
            print(name, "<-", pack, file)
