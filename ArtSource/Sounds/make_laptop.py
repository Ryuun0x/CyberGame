"""Laptop carry sounds, from Kenney's RPG Audio (CC0), written to Assets/Resources/Sfx (played by LaptopCarry):
  laptop_pickup   the lid shuts (a soft clap), then the laptop is lifted (cloth)
  laptop_place    set down on the table (a light knock), then the lid's hinge as it opens
  py -3.11 ArtSource/Sounds/make_laptop.py
"""
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import pick_kenney as pk  # noqa: E402

R = pk.RATE
PEAK = 0.28  # a sound in the world, like the doors, a little quieter


def take(name, pitch, cutoff=5000, room=0.1):
    y = pk.darken(pk.load(pk.find("rpg-audio", name)), pitch=pitch, cutoff=cutoff, room=room)
    return y / (np.max(np.abs(y)) + 1e-9)


def mix(*parts):
    """(sound, start seconds, level) laid over each other."""
    end = max(int(R * start) + len(y) for y, start, _ in parts)
    out = np.zeros(end)
    for y, start, level in parts:
        i = int(R * start)
        out[i:i + len(y)] += y * level
    return out


if __name__ == "__main__":
    lid_shut = take("bookClose", pitch=1.15)            # smaller and lighter than a book
    lift = take("cloth1", pitch=1.0, cutoff=4000)
    knock = take("bookPlace1", pitch=1.1)
    hinge = take("creak1", pitch=1.6, cutoff=3500)      # quick and small: a laptop hinge, not a door
    pk.write(f"{pk.OUT}/laptop_pickup.wav", mix((lid_shut, 0, 1.0), (lift, 0.18, 0.45)), PEAK)
    pk.write(f"{pk.OUT}/laptop_place.wav", mix((knock, 0, 1.0), (hinge, 0.12, 0.25)), PEAK)
    print("laptop sounds written")
