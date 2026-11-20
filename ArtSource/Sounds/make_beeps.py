"""Car horns up close, made in code (no recordings, so no licence to check), for the city's traffic:
  Resources/City/beep_<n>.wav   a quick tap, a beep, a double beep-beep, an impatient long honk...
Played now and then from a real car near the player, in 3D (Ambience.cs), so it comes from the street.
Small cars beep higher, vans and trucks lower. A horn is two tones a few notes apart, pushed hard (square-ish
and a little distorted), which is what makes it sound like a horn and not a beeper.
  py -3.11 ArtSource/Sounds/make_beeps.py
Also writes ArtSource/Sounds/preview/beeps.wav: every take in order.
"""
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import pick_kenney as pk  # noqa: E402

R = pk.RATE
CITY = os.path.join(os.path.dirname(pk.OUT), "City")

# (tones in Hz, pattern of (seconds on, seconds off) ...)
TAKES = [
    ((440, 554), [(0.09, 0)]),                         # a light tap: "go on"
    ((420, 530), [(0.22, 0)]),                         # one beep
    ((410, 515), [(0.12, 0.09), (0.14, 0)]),           # beep-beep
    ((330, 415), [(0.18, 0.1), (0.18, 0)]),            # a van's double
    ((450, 565), [(0.7, 0)]),                          # an impatient long honk
    ((300, 380), [(0.45, 0)]),                         # a truck
    ((430, 540), [(0.1, 0.07), (0.1, 0.07), (0.1, 0)]),  # beep-beep-beep
    ((400, 500), [(0.15, 0.35), (0.55, 0)]),           # beep... beeeep
]


def take(tones, pattern):
    parts = []
    for on, off in pattern:
        t = np.arange(int(R * on)) / R
        y = sum(np.tanh(4 * np.sin(2 * np.pi * f * t)) for f in tones)  # pushed hard: square-ish, a bit gritty
        f = int(R * 0.012)
        y[:f] *= np.linspace(0, 1, f)
        y[-f:] *= np.linspace(1, 0, f)
        parts += [y, np.zeros(int(R * off))]
    y = pk.lowpass(np.concatenate(parts), 3200)  # behind a car's grille
    return pk.darken(y, pitch=1.0, cutoff=4000, room=0.35, room_seconds=0.9)  # a street's short echo


if __name__ == "__main__":
    takes = [take(*t) for t in TAKES]
    for i, y in enumerate(takes):
        pk.write(os.path.join(CITY, f"beep_{i}.wav"), y, peak=0.6)
    gap = np.zeros(int(R * 0.8))
    pk.write(os.path.join(pk.PREVIEW, "beeps.wav"), np.concatenate([p for y in takes for p in (y, gap)]), peak=0.6)
    print("%d beeps written" % len(takes))
