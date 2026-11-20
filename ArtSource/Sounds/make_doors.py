"""Door sounds, from Kenney's RPG Audio (CC0), slightly darkened with a small room, written to Assets/Resources/Sfx:
  door_open, door_close   the café's swing door (CafeDoor)
  door_through            open, step through, shut: the house door between home and the city (Exit/DoorEntrance)
Also writes preview/door_options.wav: every Kenney door take (open 1-2, then close 1-4), to swap OPEN/CLOSE.
  py -3.11 ArtSource/Sounds/make_doors.py
"""
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import pick_kenney as pk  # noqa: E402

R = pk.RATE
OPEN, CLOSE = "doorOpen_1", "doorClose_2"
PEAK = 0.3  # a little above the quiet UI clicks: it's a sound in the world


def door(name):
    return pk.darken(pk.load(pk.find("rpg-audio", name)), pitch=0.95, cutoff=5000, room=0.15)


def norm(y):
    return y / (np.max(np.abs(y)) + 1e-9)


if __name__ == "__main__":
    o, c = norm(door(OPEN)), norm(door(CLOSE))
    through = np.zeros(len(o) + int(R * 0.6) + len(c))
    through[:len(o)] += o
    through[len(through) - len(c):] += c * 0.8  # shut behind you, a bit softer
    for name, y in {"door_open": o, "door_close": c, "door_through": through}.items():
        pk.write(f"{pk.OUT}/{name}.wav", y, PEAK)
    takes = [f"doorOpen_{i}" for i in (1, 2)] + [f"doorClose_{i}" for i in range(1, 5)]
    gap = np.zeros(int(R * 0.8))
    pk.write(f"{pk.PREVIEW}/door_options.wav", np.concatenate([p for t in takes for p in (norm(door(t)), gap)]), PEAK)
    print("door sounds written; options:", ", ".join(f"{i + 1}={t}" for i, t in enumerate(takes)))
