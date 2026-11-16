"""The game's UI sounds, synthesized (no samples, so no licence to track).

Dark and muffled, to suit a cyber-thriller UI rather than a bright console: low pitches, few high overtones,
everything low-pass filtered, and a soft room tail (reverb) so the sounds sit in a space instead of beeping.
  click        a felt "thock": a low knock with a muffled tap on top
  toggle_on/off two low knocks, the second a step up (on) or down (off)
  pause        a deep swell that sinks into the room; resume rises out of it
  phone_open/close a low, breathy swipe
  notify       a low muted bell, minor third, with a long dark tail
  buzz         the phone vibrating on a table (deep, pulsed)
  objective    a low, slow minor-ninth chord swell (resolved, not cheerful)
Output: Assets/Resources/Sfx/<name>.wav (44.1 kHz mono 16-bit), loaded by name by Assets/Script/Core/Sfx.cs.
Run: python ArtSource/Sounds/make_sounds.py   (also writes ArtSource/Sounds/preview/all_sounds_in_order.wav)
"""
import os
import wave

import numpy as np

RATE = 44100
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "Assets", "Resources", "Sfx")
PREVIEW = os.path.join(os.path.dirname(os.path.abspath(__file__)), "preview", "all_sounds_in_order.wav")
rng = np.random.default_rng(4)


def t(seconds):
    return np.arange(int(RATE * seconds)) / RATE


def env(n, attack=0.004, release=0.08):
    """Fade in, exponential fade out, and a tiny fade at the very end (no clicks)."""
    x = np.arange(n) / RATE
    a = np.clip(x / max(attack, 1e-4), 0, 1)
    r = np.exp(-x / max(release, 1e-4))
    tail = np.clip((n - np.arange(n)) / (RATE * 0.01), 0, 1)
    return a * r * tail


def lowpass(y, cutoff):
    """One-pole low-pass: takes the brightness (the "console beep") out."""
    k = 1 - np.exp(-2 * np.pi * cutoff / RATE)
    out = np.empty_like(y)
    acc = 0.0
    for i, v in enumerate(y):
        acc += k * (v - acc)
        out[i] = acc
    return out


def tone(freq, seconds, release, harmonics=((1, 1.0), (2, 0.15)), glide=0.0, attack=0.004):
    x = t(seconds)
    f = freq * (1 + glide * x / seconds)
    phase = 2 * np.pi * np.cumsum(f) / RATE
    y = sum(amp * np.sin(phase * h) for h, amp in harmonics)
    return y * env(len(x), attack, release)


def noise(seconds, release, cutoff, attack=0.002):
    y = rng.normal(0, 1, int(RATE * seconds))
    return lowpass(y, cutoff) * env(len(y), attack, release)


def knock(freq, release=0.05):
    """A muffled knock: a short low sine dropping slightly in pitch, plus a dull tap of filtered noise."""
    body = tone(freq, 0.12, release, ((1, 1.0), (2.3, 0.08)), glide=-0.18, attack=0.001)
    tap = noise(0.03, 0.006, 1400) * 0.35
    return mix(body, tap)


def reverb(y, seconds=0.6, amount=0.25, cutoff=1800):
    """A dark room: the dry sound plus a decaying, low-passed noise tail."""
    n = int(RATE * seconds)
    ir = lowpass(rng.normal(0, 1, n), cutoff) * np.exp(-np.arange(n) / (RATE * seconds / 5))
    ir /= np.sqrt(np.sum(ir ** 2)) + 1e-9
    wet = np.convolve(y, ir)
    return mix(y, wet * amount)


def mix(*parts):
    out = np.zeros(max(len(p) for p in parts))
    for p in parts:
        out[:len(p)] += p
    return out


def seq(*parts, gap=0.0):
    """Sounds one after another; gap may be negative to overlap."""
    out = np.zeros(0)
    for p in parts:
        start = max(0, len(out) + int(gap * RATE))
        end = start + len(p)
        if end > len(out):
            out = np.pad(out, (0, end - len(out)))
        out[start:end] += p
    return out


def trim(y, floor=0.002):
    """Cut the silent end of a reverb tail."""
    loud = np.nonzero(np.abs(y) > floor * np.max(np.abs(y)))[0]
    return y[:loud[-1] + 1] if len(loud) else y


SOUNDS = {}


def save(name, y, peak=0.5):
    y = trim(y)
    y = y / (np.max(np.abs(y)) + 1e-9) * peak
    os.makedirs(OUT, exist_ok=True)
    with wave.open(os.path.join(OUT, name + ".wav"), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes((y * 32767).astype(np.int16).tobytes())
    SOUNDS[name] = y
    print(name, f"{len(y) / RATE:.2f}s")


def preview(order):
    """All sounds in a row, the short ones three times, for listening outside the game."""
    parts = []
    for name in order:
        for _ in range(3 if name in ("click", "toggle_on", "toggle_off") else 1):
            parts += [SOUNDS[name], np.zeros(int(RATE * 0.35))]
        parts.append(np.zeros(int(RATE * 0.8)))
    os.makedirs(os.path.dirname(PREVIEW), exist_ok=True)
    with wave.open(PREVIEW, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes((np.concatenate(parts) * 32767).astype(np.int16).tobytes())
    print(PREVIEW)


def pluck(freq, seconds, damping=0.996, brightness=0.5):
    """Karplus-Strong plucked string: a short noise burst through a tuned, slowly damped delay line.
    Sounds like a real guitar string, not an oscillator."""
    n = int(RATE * seconds)
    period = int(RATE / freq)
    buf = lowpass(rng.uniform(-1, 1, period), 2000 + 6000 * brightness)
    out = np.empty(n)
    for i in range(n):
        j = i % period
        out[i] = buf[j]
        buf[j] = damping * 0.5 * (buf[j] + buf[(j + 1) % period])
    return out * env(n, 0.001, seconds / 2.5)


def wings(seconds=0.9, beats=7):
    """A butterfly's wing flutter: soft, breathy noise puffs that slow down and fade."""
    out = np.zeros(int(RATE * seconds))
    pos = 0.0
    for k in range(beats):
        puff = noise(0.06, 0.02, 1200, 0.01) * (1 - k / beats)
        s = int(pos * RATE)
        out[s:s + len(puff)] += puff[:len(out) - s]
        pos += 0.07 + 0.03 * k
        if pos >= seconds:
            break
    return out


def note(semitones_from_a2):
    return 110.0 * 2 ** (semitones_from_a2 / 12)


if __name__ == "__main__":
    DARK = ((1, 1.0), (2, 0.2), (3, 0.05))
    BELL = ((1, 1.0), (2.76, 0.18), (5.4, 0.05))  # inharmonic partials: a muted bell, not a beep

    save("click", reverb(knock(260, 0.03), 0.25, 0.12), 0.35)
    save("toggle_on", reverb(seq(knock(196, 0.03), knock(262, 0.04), gap=-0.07), 0.3, 0.15), 0.38)
    save("toggle_off", reverb(seq(knock(262, 0.03), knock(196, 0.04), gap=-0.07), 0.3, 0.15), 0.38)

    swell = lambda f, glide: mix(tone(f, 0.7, 0.22, DARK, glide=glide, attack=0.06), tone(f / 2, 0.7, 0.25, ((1, 1.0),), glide=glide, attack=0.08) * 0.6)
    save("pause", reverb(lowpass(mix(swell(note(5), -0.25), noise(0.5, 0.15, 500, 0.05) * 0.25), 900), 0.9, 0.35), 0.42)
    save("resume", reverb(lowpass(mix(swell(note(0), 0.3), noise(0.4, 0.12, 500, 0.05) * 0.25), 1000), 0.8, 0.3), 0.42)

    swipe = lambda f, glide: mix(noise(0.25, 0.07, 900, 0.03) * 0.6, tone(f, 0.22, 0.06, DARK, glide=glide, attack=0.02) * 0.5)
    save("phone_open", reverb(swipe(note(12), 0.35), 0.35, 0.2), 0.32)
    save("phone_close", reverb(swipe(note(15), -0.3), 0.35, 0.2), 0.3)

    bell = lambda f: lowpass(tone(f, 1.2, 0.35, BELL, attack=0.003), 1600)
    save("notify", reverb(seq(bell(note(17)), bell(note(20)), gap=-1.05), 1.2, 0.35), 0.42)

    buzz_t = t(0.26)
    hum = (np.sin(2 * np.pi * 118 * buzz_t) + 0.35 * np.sign(np.sin(2 * np.pi * 59 * buzz_t))) * env(len(buzz_t), 0.015, 1.0)
    save("buzz", reverb(lowpass(seq(hum, hum, gap=0.12), 700), 0.3, 0.12), 0.45)

    chord = [note(0), note(3), note(7), note(14)]  # A minor with an added ninth
    pad = mix(*[tone(f, 1.6, 0.6, DARK, attack=0.03 + 0.06 * i) * (0.9 - 0.15 * i) for i, f in enumerate(chord)])
    save("objective", reverb(lowpass(pad, 1400), 1.4, 0.35), 0.4)

    # Consequence (the butterfly notice): a breath swells in (a reversed room), then a soft guitar chord rolls
    # out string by string (E minor add9), with the wings fluttering on top.
    swell_in = reverb(noise(0.05, 0.02, 900), 1.2, 1.0)[::-1] * 0.35
    strings = [note(7), note(14), note(19), note(21), note(26)]  # E3 B3 E4 F#4 B4
    chord = seq(*[pluck(f, 3.0, 0.9965, 0.35) * (1 - 0.1 * i) for i, f in enumerate(strings)], gap=-2.93)
    sting = seq(swell_in, reverb(mix(chord, wings() * 0.25), 1.6, 0.4), gap=-0.05)
    save("consequence", lowpass(sting, 5000), 0.45)

    preview(["consequence", "click", "toggle_on", "toggle_off", "pause", "resume", "phone_open", "phone_close", "notify", "buzz", "objective"])
