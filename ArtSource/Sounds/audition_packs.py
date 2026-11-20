"""Listening files for the downloaded packs in ArtSource/Sounds/packs: the sounds that could fit the game, grouped by
use, each played twice, in the numbered order printed. Writes preview/pack_<group>.wav.
  python ArtSource/Sounds/audition_packs.py
"""
import glob
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import pick_kenney as pk  # noqa: E402

PACKS = os.path.join(pk.HERE, "packs")
GROUPS = {
    # Menu clicks and switches (the "clicky" ones).
    "clicky": ["light_switch_on", "light_switch_off", "socket_switch_on", "socket_switch_off", "click_double_on",
               "click_double_off", "toggle_on", "toggle_off", "finger_click", "slide_and_click",
               "controller_button_press", "select_1", "select_3", "subtle_knock"],
    # Techy UI: could fit the hacker/laptop side.
    "techy": ["sci_fi_select", "sci_fi_confirm", "sci_fi_cancel", "sci_fi_error", "sci_fi_hover", "synth_confirmation",
              "synth_warning", "synth_error", "synth_process_complete", "synth_shut_down"],
    # Story stings: objective done, warnings, the consequence moment.
    "stings": ["grand_piano_mystery", "grand_piano_negative", "grand_piano_chime_positive", "music_box_mystery",
               "vibraphone_mystery", "synth_bass_mystery", "synth_bass_negative", "horror_sting"],
    # The world: things the game has no sound for yet.
    "world": ["keyboard_typing", "foley_footstep_concrete_1", "foley_footstep_carpet_1", "foley_footstep_vinyl_1",
              "door_open", "door_close", "lock_unlock", "zipper_up", "clothing_1", "page_turn", "clock_ticking",
              "human_sigh", "human_breathe_out", "ambient_wind"],
}


def find(name):
    hits = [h for ext in ("wav", "ogg") for h in glob.glob(os.path.join(PACKS, "**", f"{name}.{ext}"), recursive=True)]
    if not hits:
        raise FileNotFoundError(name)
    return hits[0]


if __name__ == "__main__":
    for group, names in GROUPS.items():
        parts = []
        for name in names:
            y = pk.load(find(name))[: pk.RATE * 6]  # long loops: the first 6 seconds
            y = y / (np.max(np.abs(y)) + 1e-9) * 0.45
            parts += [y, np.zeros(int(pk.RATE * 0.4)), y, np.zeros(int(pk.RATE * 1.0))]
        pk.write(f"{pk.PREVIEW}/pack_{group}.wav", np.concatenate(parts), 0.45)
        print(group + ":", ", ".join(f"{i + 1}={n}" for i, n in enumerate(names)))
