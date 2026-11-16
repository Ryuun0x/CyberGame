"""The game's UI sounds, made to fit its theme: a quiet cyber-thriller (a student, a hacker group called Ghostline,
dark screens at night). One family: soft real clicks (Kenney CC0 recordings) for touch, a warm low tone for weight,
and a touch of digital grit (a recorded glitch, tape-like slowdowns) for the "something is watching" feel.
Short and dry for frequent sounds, so menus stay snappy.
  python ArtSource/Sounds/make_theme.py   writes Assets/Resources/Sfx/*.wav and preview/theme_set.wav
"""
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import pick_kenney as pk  # noqa: E402

R = pk.RATE
rng = np.random.default_rng(21)


def norm(y):
    return y / (np.max(np.abs(y)) + 1e-9)


def gap(s):
    return np.zeros(int(R * s))


def mix(*parts):
    out = np.zeros(max(len(p) for p in parts))
    for p in parts:
        out[:len(p)] += p
    return out


def seq(*parts, overlap=0.0):
    out = np.zeros(0)
    for p in parts:
        start = max(0, len(out) - int(overlap * R))
        out = np.pad(out, (0, max(0, start + len(p) - len(out))))
        out[start:start + len(p)] += p
    return out


def rec(pack, name, pitch=1.0, cutoff=6000):
    """A recording, slowed by `pitch` (lower = deeper), its top softened."""
    y = pk.load(pk.find(pack, name))
    y = np.interp(np.arange(0, len(y), pitch), np.arange(len(y)), y)
    return norm(0.7 * pk.lowpass(y, cutoff) + 0.3 * pk.lowpass(y, cutoff * 0.3))


def warm(freq, seconds, attack, release, glide=0.0):
    """A low warm tone: a sine with a soft octave below, gently saturated like tape."""
    x = np.arange(int(R * seconds)) / R
    f = freq * (1 + glide * x / seconds)
    ph = 2 * np.pi * np.cumsum(f) / R
    y = np.sin(ph) + 0.45 * np.sin(ph / 2) + 0.12 * np.sin(ph * 2)
    y = np.tanh(1.6 * y) / np.tanh(1.6)
    env = np.minimum(x / attack, 1) * np.exp(-x / release)
    env[-int(R * 0.01):] *= np.linspace(1, 0, int(R * 0.01))
    return y * env


def tape(y, start, end):
    """Varispeed: playback speed slides from start to end (end < start: a tape slowing to a stop)."""
    speed = np.linspace(start, end, len(y))
    pos = np.cumsum(speed)
    pos = pos[pos < len(y) - 1]
    return np.interp(pos, np.arange(len(y)), y)


def room(y, amount=0.1, seconds=0.4):
    n = int(R * seconds)
    ir = pk.lowpass(rng.normal(0, 1, n), 2200) * np.exp(-np.arange(n) / (R * seconds / 5))
    ir /= np.sqrt(np.sum(ir ** 2)) + 1e-9
    return mix(y, np.convolve(y, ir) * amount)


def end_fade(y, seconds=0.02):
    n = min(len(y), int(R * seconds))
    y = y.copy()
    y[-n:] *= np.linspace(1, 0, n)
    return y


def note(semitones_from_a2):
    return 110.0 * 2 ** (semitones_from_a2 / 12)


def theme_set():
    tick = lambda p=0.75: rec("interface-sounds", "select_004", p, 4000)
    glitch = lambda i=1, p=0.8: rec("interface-sounds", f"glitch_00{i}", p, 3500)
    s = {}
    # Click: a soft recorded tick, a little lower, with a faint low body. Short and dry.
    s["click"] = (room(mix(tick() * 0.8, warm(note(12), 0.08, 0.002, 0.02) * 0.35), 0.06, 0.25), 0.3)
    # Toggle: tick + a short low blip that steps up (on) or down (off).
    blip = lambda a, b: seq(warm(note(a), 0.07, 0.003, 0.03), warm(note(b), 0.1, 0.003, 0.04), overlap=0.03)
    s["toggle_on"] = (room(mix(tick(0.8) * 0.6, blip(12, 19) * 0.5), 0.06, 0.25), 0.32)
    s["toggle_off"] = (room(mix(tick(0.7) * 0.6, blip(19, 12) * 0.5), 0.06, 0.25), 0.32)
    # Pause: a low chord that slows down like a tape stopping (time freezes). Resume: the tape spinning back up.
    chord = lambda: mix(warm(note(0), 0.9, 0.01, 0.5), warm(note(7), 0.9, 0.01, 0.45) * 0.6, warm(note(12), 0.9, 0.01, 0.4) * 0.35)
    s["pause"] = (room(end_fade(tape(chord(), 1.0, 0.15)), 0.12, 0.6), 0.4)
    s["resume"] = (room(end_fade(tape(chord(), 0.25, 1.05)), 0.12, 0.6), 0.4)
    # Phone: a short soft swipe of air with a tiny digital chirp from a recorded glitch.
    air = lambda rise, fall: pk.lowpass(rng.normal(0, 1, int(R * 0.22)), 1100) * np.minimum(np.arange(int(R * 0.22)) / (R * rise), 1) * np.exp(-np.arange(int(R * 0.22)) / (R * fall))
    s["phone_open"] = (room(mix(norm(air(0.06, 0.06)) * 0.6, glitch(1, 0.9)[:int(R * 0.12)] * 0.25), 0.08, 0.3), 0.32)
    s["phone_close"] = (room(mix(norm(air(0.01, 0.05)) * 0.6, glitch(2, 0.7)[:int(R * 0.1)] * 0.2), 0.08, 0.3), 0.28)
    # Notification: two soft, slightly uneasy notes (a minor second apart, like something is off), with a
    # faint glitch tail: a message you shouldn't fully trust.
    s["notify"] = (room(mix(seq(warm(note(19), 0.4, 0.004, 0.18), warm(note(20), 0.55, 0.004, 0.25), overlap=0.25),
                             np.concatenate([gap(0.3), glitch(3, 0.75) * 0.18])), 0.15, 0.7), 0.42)
    # Objective: a low warm swell that settles on a calm fifth: done, but the night isn't over.
    swell = mix(warm(note(0), 1.4, 0.08, 0.7), warm(note(7), 1.4, 0.15, 0.6) * 0.7, warm(note(19), 1.4, 0.25, 0.5) * 0.25)
    s["objective"] = (room(swell, 0.15, 0.8), 0.4)
    return s


if __name__ == "__main__":
    parts = []
    for name, (y, peak) in theme_set().items():
        pk.write(f"{pk.OUT}/{name}.wav", y, peak)
        clip = norm(y) * peak
        for _ in range(3 if name in ("click", "toggle_on", "toggle_off") else 1):
            parts += [clip, gap(0.35)]
        parts.append(gap(0.8))
        print(name, f"{len(y) / R:.2f}s")
    pk.write(f"{pk.PREVIEW}/theme_set.wav", np.concatenate(parts), 0.45)
