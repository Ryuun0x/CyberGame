"""The city's sound, made in code (no recordings, so no licence to check). Kept soft: a lived-in street, not a busy one.
  Resources/Ambience/city.ogg   distant traffic: a low road roar with cars swelling past now and then (Ambience.cs)
  Resources/City/engine.wav     one car's quiet engine and tyres, looped; every CityCar plays it in 3D
  Resources/City/bird_<n>.wav   short bird phrases from the trees, now and then (Ambience.cs)
  Resources/City/horn_<n>.wav   a horn far off, rarely (Ambience.cs)
Noise is shaped in the frequency domain (numpy FFT), which makes it repeat exactly, so the loops have no seam.
  py -3.11 ArtSource/Sounds/make_city.py
Also writes ArtSource/Sounds/preview/city_preview.wav: 40 s of street with two cars driving past.
"""
import os
import sys

import numpy as np
import soundfile as sf

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import pick_kenney as pk  # noqa: E402

R = pk.RATE
RES = os.path.dirname(pk.OUT)  # Assets/Resources
rng = np.random.default_rng(11)


def noise(seconds, low, high, tilt=1.0):
    """Noise between `low` and `high` Hz with soft edges, falling `tilt` per octave (1 = pink-ish, 2 = brown-ish).
    It repeats exactly every `seconds`."""
    n = int(R * seconds)
    f = np.fft.rfftfreq(n, 1 / R)
    f[0] = 1
    shape = f ** (-tilt / 2) / (1 + (low / f) ** 4) / (1 + (f / high) ** 4)
    y = np.fft.irfft(shape * np.exp(2j * np.pi * rng.random(len(f))), n)
    return y / np.sqrt(np.mean(y ** 2))


def loop_hum(seconds, f0, harmonics):
    """A low engine hum: harmonics of f0 (rounded to whole cycles, so it loops), slowly wavering in level."""
    t = np.arange(int(R * seconds)) / R
    f0 = round(f0 * seconds) / seconds
    out = np.zeros(len(t))
    for k, a in enumerate(harmonics, 1):
        out += a * np.sin(2 * np.pi * k * f0 * t + rng.random() * 6.28)
    return out * (1 + 0.15 * np.sin(2 * np.pi * t * 3 / seconds))


def wrap_bumps(n, count, width, rng):
    """`count` soft bumps (Gaussian, `width` seconds) at random places, wrapped round so the envelope loops."""
    t = np.arange(n) / R
    total = n / R
    env = np.zeros(n)
    for c in rng.uniform(0, total, count):
        d = np.minimum(np.abs(t - c), total - np.abs(t - c))
        w = width * rng.uniform(0.7, 1.4)
        env += rng.uniform(0.4, 1.0) * np.exp(-0.5 * (d / w) ** 2)
    return env


def traffic(seconds=40):
    """Distant traffic: a steady low roar, cars swelling past on the next streets, a faint hum of engines."""
    n = int(R * seconds)
    roar = noise(seconds, 40, 500, tilt=1.6)
    passes = noise(seconds, 120, 1400, tilt=1.0) * wrap_bumps(n, 9, 1.6, rng)
    hum = loop_hum(seconds, 46, [0.5, 0.3, 0.15]) * 0.25
    return roar + 0.8 * passes + hum


def engine(seconds=4):
    """One car going by slowly: tyres on the road (most of what you hear) and a soft low engine underneath."""
    tyres = noise(seconds, 150, 1100, tilt=1.2)
    t = np.arange(int(R * seconds)) / R
    tyres *= 1 + 0.12 * np.sin(2 * np.pi * t * 2 / seconds)  # a little unevenness in the road
    motor = loop_hum(seconds, 38, [0.6, 0.45, 0.3, 0.2, 0.12, 0.06])
    motor += 0.3 * noise(seconds, 60, 300, tilt=1.5)
    return tyres + 0.7 * motor / np.sqrt(np.mean(motor ** 2))


def bird(seed):
    """A short phrase of 2-5 chirps: quick pitch sweeps with a flutter, from a little way off."""
    r = np.random.default_rng(seed)
    base = r.uniform(2800, 4200)
    out = [np.zeros(int(R * 0.05))]
    for _ in range(r.integers(2, 6)):
        dur = r.uniform(0.05, 0.13)
        t = np.arange(int(R * dur)) / R
        a, b = base * r.uniform(0.85, 1.25), base * r.uniform(0.85, 1.4)
        freq = a + (b - a) * t / dur + 180 * np.sin(2 * np.pi * r.uniform(25, 60) * t)
        chirp = np.sin(2 * np.pi * np.cumsum(freq) / R) * np.sin(np.pi * t / dur) ** 2
        out += [chirp * r.uniform(0.6, 1.0), np.zeros(int(R * r.uniform(0.04, 0.16)))]
    y = np.concatenate(out + [np.zeros(int(R * 0.3))])
    return pk.darken(y, pitch=1.0, cutoff=7000, room=0.2, room_seconds=0.5)


def horn(seed):
    """A car horn a few streets away: two tones, mostly the echo off the buildings."""
    r = np.random.default_rng(seed)
    parts = []
    for i in range(r.integers(1, 3)):  # one beep, or a quick double
        dur = r.uniform(0.25, 0.45) if i == 0 else 0.15
        t = np.arange(int(R * dur)) / R
        y = sum(np.sign(np.sin(2 * np.pi * f * t)) * 0.5 for f in (r.uniform(380, 420), r.uniform(480, 520)))
        f = int(R * 0.02)
        y[:f] *= np.linspace(0, 1, f)
        y[-f:] *= np.linspace(1, 0, f)
        parts += [y, np.zeros(int(R * 0.12))]
    y = pk.lowpass(np.concatenate(parts), 900)
    return pk.darken(y, pitch=1.0, cutoff=1200, room=0.9, room_seconds=1.4)


def level(y, rms=0.1):
    y = y * (rms / (np.sqrt(np.mean(y ** 2)) + 1e-9))
    return y * min(1.0, 0.9 / np.max(np.abs(y)))


def write_ogg(path, y):
    # In small blocks: libsndfile's Vorbis encoder overflows the stack if handed a long file in one write.
    with sf.SoundFile(path, "w", R, 1, format="OGG", subtype="VORBIS") as f:
        for i in range(0, len(y), 4096):
            f.write(y[i:i + 4096])


if __name__ == "__main__":
    city = os.path.join(RES, "City")
    os.makedirs(city, exist_ok=True)
    bed = level(traffic())
    write_ogg(os.path.join(RES, "Ambience", "city.ogg"), bed)
    car = level(engine())
    pk.write(os.path.join(city, "engine.wav"), car, peak=float(np.max(np.abs(car))))
    birds = [bird(s) for s in range(6)]
    horns = [horn(s) for s in (3, 8)]
    for i, y in enumerate(birds):
        pk.write(os.path.join(city, f"bird_{i}.wav"), y, peak=0.5)
    for i, y in enumerate(horns):
        pk.write(os.path.join(city, f"horn_{i}.wav"), y, peak=0.5)
    print("city bed, engine, %d birds, %d horns" % (len(birds), len(horns)))

    # Preview at about the game's levels (Ambience: city 0.3, birds 0.25, horn 0.12; a car passing 2 m away).
    total = 40
    mix = np.tile(bed, 2)[:R * total] * 0.3
    for start, y, gain in [(3, birds[0], 0.25), (9, birds[3], 0.2), (17, birds[1], 0.25), (24, horns[0], 0.12),
                           (29, birds[5], 0.22), (35, birds[2], 0.2)]:
        y = y / np.max(np.abs(y)) * 0.5 * gain
        mix[R * start:R * start + len(y)] += y[:len(mix) - R * start]
    for start in (6, 21):  # a car passing at 7 m/s, 2 m from the kerb: louder and a touch higher as it comes
        n = R * 8
        t = np.arange(n) / R - 4
        dist = np.sqrt(2 ** 2 + (7 * t) ** 2)
        gain = 0.35 * np.clip((30 - dist) / 28, 0, 1)  # Unity's linear roll-off, 2 m to 30 m
        idx = np.cumsum(1 - 0.5 * 7 * (7 * t / dist) / 343)  # half-strength Doppler
        y = np.interp(idx % len(car), np.arange(len(car)), car) * gain
        mix[R * start:R * start + n] += y[:len(mix) - R * start]
    pk.write(os.path.join(pk.HERE, "preview", "city_preview.wav"), mix, peak=float(min(0.95, np.max(np.abs(mix)))))
    print("preview:", os.path.join(pk.HERE, "preview", "city_preview.wav"))
