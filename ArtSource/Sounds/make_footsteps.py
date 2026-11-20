"""Footsteps for the player, from Kenney's Impact Sounds (CC0): five takes per floor, softened and levelled, written to
Assets/Resources/Footsteps/<floor>_<n>.wav, which Assets/Script/Core/Footsteps.cs plays.
  py -3.11 ArtSource/Sounds/make_footsteps.py
"""
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import pick_kenney as pk  # noqa: E402

OUT = os.path.join(os.path.dirname(pk.OUT), "Footsteps")
# game floor: (Kenney surface, low-pass cutoff, peak)
FLOORS = {
    "carpet": ("carpet", 4000, 0.5),      # home
    "wood": ("wood", 4500, 0.45),         # the café
    "concrete": ("concrete", 5000, 0.4),  # the city's pavements
}

if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    for floor, (surface, cutoff, peak) in FLOORS.items():
        for i in range(5):
            y = pk.lowpass(pk.load(pk.find("impact-sounds", f"footstep_{surface}_00{i}")), cutoff)
            loud = np.nonzero(np.abs(y) > 0.01 * np.max(np.abs(y)))[0]
            y = y[loud[0]:loud[-1] + 1]  # no silence before the step, so it lands on time
            y[-200:] *= np.linspace(1, 0, 200)
            pk.write(os.path.join(OUT, f"{floor}_{i}.wav"), y, peak)
        print(floor, "<-", surface)
