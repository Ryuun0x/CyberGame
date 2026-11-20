"""UI sounds "inside a pipe": real recorded taps and air (Kenney CC0, ArtSource/Sounds/kenney) sent through a tuned
pipe echo (a feedback delay darkened on each round trip), so they come out hollow and booming.
  python ArtSource/Sounds/make_pipe.py   writes ArtSource/Sounds/preview/pipe_<sound>.wav (options to choose from)
DEPTH scales every pipe: lower = a bigger, deeper pipe.
"""
import sys

import numpy as np

sys.path.insert(0, __import__("os").path.dirname(__import__("os").path.abspath(__file__)))
import pick_kenney as pk  # noqa: E402

R = pk.RATE
DEPTH = 0.5  # 1.0 = the first pipe set; 0.5 = an octave deeper
rng = np.random.default_rng(11)


def pipe(x, freq, feedback=0.93, damp=0.35, tail=1.0):
    """A waveguide: the sound bounces along the pipe, each pass a little darker."""
    freq *= DEPTH
    D = int(R / freq)
    n = len(x) + int(R * tail)
    xx = np.zeros(n)
    xx[:len(x)] = x
    y = np.zeros(n)
    lp = 0.0
    for i in range(n):
        back = y[i - D] if i >= D else 0.0
        lp += damp * (back - lp)
        y[i] = xx[i] + feedback * lp
    return y


def norm(y):
    return y / (np.max(np.abs(y)) + 1e-9)


def tap(pack, name, pitch=0.8):
    y = pk.load(pk.find(pack, name))
    y = np.interp(np.arange(0, len(y), pitch), np.arange(len(y)), y)
    return norm(y)


def air(seconds, rise, fall, cutoff=900):
    n = int(R * seconds)
    x = np.arange(n) / R
    y = pk.lowpass(rng.normal(0, 1, n), cutoff)
    return y * np.minimum(x / rise, 1) * np.exp(-np.maximum(x - rise, 0) / fall)


def finish(y, room=0.2):
    y = pk.darken(y, pitch=1.0, cutoff=2600, room=room, room_seconds=1.0)
    return norm(y) * 0.45


def gap(s):
    return np.zeros(int(R * s))


def options():
    soft = lambda i, p=0.8: tap("impact-sounds", f"impactSoft_medium_00{i}", p)
    wood = lambda i, p=0.75: tap("impact-sounds", f"impactWood_light_00{i}", p)
    return {
        "click": [finish(pipe(soft(0), 140, 0.9, 0.3, 0.5), 0.1),
                  finish(pipe(wood(0), 110, 0.92, 0.3, 0.6), 0.1),
                  finish(pipe(tap("interface-sounds", "tick_002", 0.7), 95, 0.9, 0.25, 0.5), 0.1),
                  finish(pipe(tap("impact-sounds", "impactGeneric_light_000", 0.75), 80, 0.93, 0.3, 0.7), 0.1)],
        "toggle": [finish(pipe(np.concatenate([soft(1), gap(0.06), soft(2, 0.92)]), 110, 0.9, 0.3, 0.6), 0.12),
                   finish(pipe(np.concatenate([wood(1), gap(0.06), wood(2, 0.86)]), 95, 0.9, 0.3, 0.6), 0.12)],
        "pause": [finish(pipe(air(0.9, 0.35, 0.25, 700), 55, 0.95, 0.3, 1.4), 0.3),
                  finish(pipe(air(0.8, 0.25, 0.25, 500), 70, 0.94, 0.25, 1.2), 0.3)],
        "resume": [finish(pipe(air(0.7, 0.05, 0.3, 700), 70, 0.95, 0.3, 1.2), 0.3),
                   finish(pipe(air(0.6, 0.04, 0.25, 500), 85, 0.94, 0.25, 1.1), 0.3)],
        "objective": [finish(pipe(tap("impact-sounds", "impactWood_heavy_000", 0.7), 65, 0.96, 0.25, 1.8), 0.35),
                      finish(pipe(tap("impact-sounds", "impactSoft_heavy_000", 0.7), 55, 0.97, 0.22, 2.0), 0.35)],
    }


def final_set():
    """The game's set: one family of sounds, all in the same deep pipe. Frequent sounds (click, toggle) are short
    and quiet so they don't tire; rare ones (pause, objective, notify) ring longer."""
    soft = lambda i, p=0.8: tap("impact-sounds", f"impactSoft_medium_00{i}", p)
    knock = lambda i, p=0.75: tap("impact-sounds", f"impactWood_medium_00{i}", p)
    return {
        "click": (finish(pipe(soft(0), 140, 0.88, 0.3, 0.3), 0.08), 0.3),
        "toggle_on": (finish(pipe(np.concatenate([soft(1), gap(0.06), soft(2, 0.92)]), 110, 0.88, 0.3, 0.4), 0.1), 0.34),
        "toggle_off": (finish(pipe(np.concatenate([soft(2, 0.92), gap(0.06), soft(1)]), 110, 0.88, 0.3, 0.4), 0.1), 0.34),
        "pause": (finish(pipe(air(0.9, 0.35, 0.25, 700), 55, 0.95, 0.3, 1.4), 0.3), 0.42),
        "resume": (finish(pipe(air(0.7, 0.05, 0.3, 700), 70, 0.95, 0.3, 1.2), 0.3), 0.42),
        "phone_open": (finish(pipe(air(0.3, 0.08, 0.1, 900), 95, 0.9, 0.3, 0.5), 0.15), 0.34),
        "phone_close": (finish(pipe(air(0.28, 0.02, 0.12, 700), 80, 0.9, 0.3, 0.5), 0.15), 0.3),
        "notify": (finish(pipe(np.concatenate([knock(0), gap(0.16), knock(1, 0.7)]), 75, 0.94, 0.26, 1.2), 0.25), 0.45),
        "objective": (finish(pipe(tap("impact-sounds", "impactSoft_heavy_000", 0.7), 55, 0.97, 0.22, 2.0), 0.35), 0.42),
    }


if __name__ == "__main__":
    if sys.argv[1:] == ["final"]:
        # Into the game (Assets/Resources/Sfx), plus one preview with all of them in order.
        parts = []
        for name, (y, peak) in final_set().items():
            pk.write(f"{pk.OUT}/{name}.wav", y, peak)
            parts += [norm(y) * peak] * (3 if name in ("click", "toggle_on", "toggle_off") else 1)
            parts = parts[:-1] + [parts[-1], gap(1.0)]
            print(name)
        spaced = []
        for p in parts:
            spaced += [p, gap(0.35)]
        pk.write(f"{pk.PREVIEW}/pipe_final_set.wav", np.concatenate(spaced), 0.45)
        sys.exit()
    for key, opts in options().items():
        parts = []
        for y in opts:
            for _ in range(3 if key in ("click", "toggle") else 2):
                parts += [y, gap(0.4)]
            parts.append(gap(1.0))
        pk.write(f"{pk.PREVIEW}/pipe_{key}.wav", np.concatenate(parts))
        print(key, len(opts))
