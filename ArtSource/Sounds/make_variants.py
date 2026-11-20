"""More UI sound sets to choose from, each a different take on the game's theme. They use the game's sound names
(Assets/Script/Core/Sfx.cs), so any set can replace the one in the game.
  python ArtSource/Sounds/make_variants.py          previews: preview/variant_<set>.wav (every sound, in order)
  python ArtSource/Sounds/make_variants.py <set>    puts that set into the game (Assets/Resources/Sfx/*.wav)
Sets:
  night   a quiet room at night: muted felt-piano notes, fingertip taps, cloth (Life is Strange-like, intimate)
  signal  Ghostline listening in: radio static, low data chirps, a sub thump, the signal dropping and locking in
  glass   a dark phone screen: soft glass taps (deeper), dim bell tones, airy whooshes
  pulse   tension in the body: muffled heartbeat thumps, held and released breath
  subtle  barely there: quiet, short, dull (a fingertip, a light switch, cloth, one soft felt note)
  clips   the user's AudioClip download: a real click and switch, a diary, cloth, a chirp, a pencil scribble
  clicky  crisp and tactile: keyboard-like clicks with a low thock, real switches, latches, a ratchet
Also writes preview/variant_click_options.wav (other clicks for the clicky set) and
preview/variant_pause_options.wav (pause/resume pairs, PAUSE_OPTIONS).
"""
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import make_sounds as ms  # noqa: E402
import pick_kenney as pk  # noqa: E402
from make_theme import end_fade, gap, mix, norm, note, rec, seq, tape, warm  # noqa: E402

R = pk.RATE
rng = np.random.default_rng(33)
ORDER = ["click", "toggle_on", "toggle_off", "pause", "resume", "phone_open", "phone_close", "notify", "objective"]
PEAK = {"click": 0.3, "toggle_on": 0.32, "toggle_off": 0.32, "pause": 0.4, "resume": 0.4, "phone_open": 0.32,
        "phone_close": 0.3, "notify": 0.42, "objective": 0.4}


def room(y, amount=0.15, seconds=0.6, cutoff=2200):
    """A dark room tail (FFT convolution with decaying filtered noise)."""
    n = int(R * seconds)
    ir = pk.lowpass(rng.normal(0, 1, n), cutoff) * np.exp(-np.arange(n) / (R * seconds / 5))
    ir /= np.sqrt(np.sum(ir ** 2)) + 1e-9
    size = len(y) + n - 1
    f = 1 << (size - 1).bit_length()
    wet = np.fft.irfft(np.fft.rfft(y, f) * np.fft.rfft(ir, f), f)[:size]
    return mix(y, wet * amount)


def env(seconds, attack, release):
    return ms.env(int(R * seconds), attack, release)


def band(seconds, lo, hi):
    x = rng.normal(0, 1, int(R * seconds))
    return pk.lowpass(x, hi) - pk.lowpass(x, lo)


def sweep(y, f0, f1):
    """A low-pass whose cutoff glides from f0 to f1 (a whoosh opening or closing)."""
    k = 1 - np.exp(-2 * np.pi * np.geomspace(f0, f1, len(y)) / R)
    out = np.empty_like(y)
    acc = 0.0
    for i in range(len(y)):
        acc += k[i] * (y[i] - acc)
        out[i] = acc
    return out


def crush(y, bits=6, hold=3):
    """Lo-fi digital grit: fewer samples and fewer levels."""
    y = np.repeat(norm(y)[::hold], hold)[:len(y)]
    return np.round(y * 2 ** bits) / 2 ** bits


def cut(y, seconds):
    return end_fade(y[:int(R * seconds)], 0.01)


# ---- night: a quiet room at night -------------------------------------------------------------------------------
def night():
    felt = lambda f, s=1.0: pk.lowpass(ms.pluck(f, s, 0.986, 0.15), 1800)  # a muted, felt-damped piano-like string
    tap = lambda p=1.2: cut(rec("impact-sounds", "impactSoft_medium_000", p, 2500), 0.12)  # a fingertip on the desk
    roll = lambda notes, s, step=0.05: seq(*[felt(note(n), s) * (1 - 0.12 * i) for i, n in enumerate(notes)], overlap=s - step)
    return {
        "click": room(tap(), 0.08, 0.3),
        "toggle_on": room(mix(tap(1.3) * 0.5, roll([12, 19], 0.45, 0.07)), 0.12, 0.5),
        "toggle_off": room(mix(tap(1.1) * 0.5, roll([19, 12], 0.45, 0.07)), 0.12, 0.5),
        "pause": room(roll([0, 3, 7], 1.3, 0.09), 0.25, 1.0),
        "resume": room(roll([7, 12, 15], 1.0, 0.06), 0.22, 0.9),
        "phone_open": room(cut(rec("rpg-audio", "cloth1", 0.95, 4000), 0.35), 0.08, 0.3),
        "phone_close": room(cut(rec("rpg-audio", "cloth3", 0.9, 3500), 0.3), 0.08, 0.3),
        "notify": room(roll([19, 22], 1.2, 0.16), 0.25, 1.1),
        "objective": room(roll([0, 7, 12, 15, 19], 2.2, 0.11), 0.3, 1.4),
    }


# ---- signal: Ghostline listening in -----------------------------------------------------------------------------
def signal():
    def static(s, lo=600, hi=3200, crackle=0.004):
        y = band(s, lo, hi)
        pops = (rng.random(len(y)) < crackle) * rng.normal(0, 4, len(y))
        return norm(y + pk.lowpass(pops, 2500))

    def chirp(f0, f1, s):
        x = np.arange(int(R * s)) / R
        ph = 2 * np.pi * np.cumsum(np.geomspace(f0, f1, len(x))) / R
        return pk.lowpass(crush(np.sin(ph), 5, 4), 2600) * env(s, 0.004, s / 3)

    def sub(f=60, s=0.18):
        x = np.arange(int(R * s)) / R
        return np.sin(2 * np.pi * np.cumsum(f * (1 + np.exp(-x / 0.02))) / R) * env(s, 0.001, 0.05)

    chord = mix(warm(note(0), 1.0, 0.01, 0.5), warm(note(7), 1.0, 0.01, 0.45) * 0.6)
    return {
        "click": room(mix(static(0.035) * env(0.035, 0.001, 0.008) * 0.6, sub(80, 0.1) * 0.7), 0.05, 0.25),
        "toggle_on": room(mix(chirp(300, 700, 0.07), static(0.05) * env(0.05, 0.001, 0.01) * 0.3, sub(70, 0.1) * 0.5), 0.06, 0.3),
        "toggle_off": room(mix(chirp(700, 300, 0.07), static(0.05) * env(0.05, 0.001, 0.01) * 0.3, sub(60, 0.1) * 0.5), 0.06, 0.3),
        # Pause: the signal drops out (the chord slows and breaks up into static). Resume: static, then it locks in.
        "pause": room(end_fade(crush(mix(tape(chord, 1.0, 0.25), static(0.9) * env(0.9, 0.3, 0.3) * 0.35), 7, 3)), 0.12, 0.6),
        "resume": room(seq(static(0.35) * np.linspace(0, 1, int(R * 0.35)) ** 2 * 0.5, mix(sub(55, 0.3), warm(note(12), 0.6, 0.005, 0.25) * 0.6)), 0.12, 0.6),
        "phone_open": room(mix(chirp(180, 520, 0.16) * 0.7, static(0.16) * env(0.16, 0.05, 0.04) * 0.3), 0.08, 0.3),
        "phone_close": room(mix(chirp(520, 160, 0.14) * 0.7, static(0.14) * env(0.14, 0.005, 0.04) * 0.3), 0.08, 0.3),
        # Notify: two short radio blips over faint static, like a message intercepted.
        "notify": room(mix(seq(chirp(620, 600, 0.09), gap(0.06), chirp(620, 600, 0.09), gap(0.05)),
                           static(0.5) * env(0.5, 0.02, 0.2) * 0.2, sub(55, 0.25) * 0.6), 0.15, 0.8),
        # Objective: the static fades away and a clean low fifth comes through.
        "objective": room(mix(static(1.0) * env(1.0, 0.01, 0.25) * 0.3,
                              seq(gap(0.25), mix(warm(note(0), 1.6, 0.2, 0.7), warm(note(7), 1.6, 0.3, 0.6) * 0.6))), 0.18, 1.0),
    }


# ---- glass: a dark phone screen ---------------------------------------------------------------------------------
def glass():
    tap = lambda i=0, p=0.7: cut(rec("impact-sounds", f"impactGlass_light_00{i}", p, 5000), 0.18)
    bell = lambda f, s=1.4: pk.lowpass(ms.tone(f, s, s / 3, ((1, 1.0), (2.76, 0.2), (5.4, 0.06)), attack=0.002), 3200)
    whoosh = lambda s, f0, f1, a, r: sweep(band(s, 80, 6000), f0, f1) * env(s, a, r)
    return {
        "click": room(tap(0, 0.65), 0.1, 0.5),
        "toggle_on": room(seq(tap(1, 0.7), tap(2, 0.85), overlap=0.1), 0.12, 0.6),
        "toggle_off": room(seq(tap(2, 0.85), tap(1, 0.7), overlap=0.1), 0.12, 0.6),
        "pause": room(mix(norm(whoosh(0.7, 3000, 200, 0.05, 0.3)) * 0.6, seq(gap(0.35), bell(note(12), 1.0) * 0.4)), 0.3, 1.4),
        "resume": room(mix(norm(whoosh(0.5, 200, 3000, 0.3, 0.2)) * 0.6, seq(gap(0.3), bell(note(19), 0.8) * 0.35)), 0.3, 1.2),
        "phone_open": room(norm(whoosh(0.25, 300, 2500, 0.12, 0.06)) * 0.7, 0.15, 0.6),
        "phone_close": room(norm(whoosh(0.22, 2500, 300, 0.01, 0.07)) * 0.7, 0.15, 0.6),
        "notify": room(seq(bell(note(24)), bell(note(27)), overlap=1.25), 0.35, 1.6),
        "objective": room(seq(*[bell(note(n), 2.0) * (1 - 0.1 * i) for i, n in enumerate([12, 19, 24, 27])], overlap=1.88), 0.4, 1.8),
    }


# ---- pulse: tension in the body ---------------------------------------------------------------------------------
def pulse():
    def thump(f=55, s=0.25):
        x = np.arange(int(R * s)) / R
        body = np.sin(2 * np.pi * np.cumsum(f * (1 + 1.2 * np.exp(-x / 0.015))) / R) * env(s, 0.002, 0.06)
        skin = cut(rec("impact-sounds", "impactSoft_heavy_000", 0.5, 400), s)
        return norm(mix(body, skin * 0.4))

    breath = lambda s, a, r: norm(band(s, 250, 1600)) * env(s, a, r)
    beat = lambda f1=55, f2=68: seq(thump(f1), gap(0.0), thump(f2) * 0.8, overlap=0.13)  # lub-dub
    tick = lambda: cut(rec("interface-sounds", "select_004", 0.7, 2000), 0.1)
    return {
        "click": room(mix(thump(85, 0.12) * 0.7, tick() * 0.4), 0.05, 0.25),
        "toggle_on": room(beat(55, 70), 0.08, 0.35),
        "toggle_off": room(beat(70, 55), 0.08, 0.35),
        # Pause: a breath drawn in and held. Resume: let out.
        "pause": room(mix(breath(0.7, 0.55, 0.6) * 0.5, seq(gap(0.45), thump(50, 0.3) * 0.7)), 0.15, 0.7),
        "resume": room(mix(breath(0.6, 0.02, 0.2) * 0.5, thump(60, 0.3) * 0.6), 0.15, 0.7),
        "phone_open": room(cut(rec("rpg-audio", "cloth2", 0.85, 3000), 0.3), 0.08, 0.3),
        "phone_close": room(cut(rec("rpg-audio", "cloth4", 0.8, 3000), 0.28), 0.08, 0.3),
        # Notify: the heart skips (two quick beats), with a faint, uneasy high tone.
        "notify": room(mix(beat(60, 72), seq(gap(0.08), warm(note(31), 0.8, 0.02, 0.3) * 0.2)), 0.2, 0.9),
        # Objective: the heartbeat slows down and a warm swell settles it.
        "objective": room(mix(seq(beat(), gap(0.35), beat() * 0.7, gap(0.55), beat() * 0.45),
                              seq(gap(0.5), mix(warm(note(0), 1.6, 0.3, 0.7), warm(note(7), 1.6, 0.4, 0.6) * 0.6) * 0.6)), 0.2, 1.0),
    }


# ---- clicky: crisp, tactile, like a good keyboard and switches --------------------------------------------------
def key(pack, name, pitch=1.0, s=0.12):
    """A crisp recorded click with a short low "thock" under it, so it feels physical rather than thin."""
    return mix(cut(rec(pack, name, pitch, 9000), s), warm(note(14), 0.05, 0.001, 0.012) * 0.3)


CLICK_OPTIONS = [("ui-audio", f"click{i}") for i in range(1, 6)] + \
                [("interface-sounds", f"click_00{i}") for i in range(1, 6)] + [("ui-audio", "mouseclick1")]


def clicky():
    ticks = lambda names, step: seq(*[cut(rec("interface-sounds", n, 1.0, 9000), 0.08) * (1 - 0.1 * i) for i, n in enumerate(names)], overlap=0.08 - step)
    blip = lambda a, b: seq(warm(note(a), 0.06, 0.002, 0.02), warm(note(b), 0.08, 0.002, 0.03), overlap=0.03) * 0.25
    ratchet = seq(*[cut(rec("interface-sounds", "tick_002", 1.0 - 0.04 * i, 9000), 0.06) * (0.5 + 0.08 * i) for i in range(6)], overlap=0.0)
    return {
        "click": room(key("ui-audio", "click3", 0.95), 0.04, 0.2),
        "toggle_on": room(mix(key("ui-audio", "switch7", 0.95, 0.15), seq(gap(0.03), blip(12, 19))), 0.05, 0.25),
        "toggle_off": room(mix(key("ui-audio", "switch8", 0.95, 0.15), seq(gap(0.03), blip(19, 12))), 0.05, 0.25),
        # Pause: a firm double latch, then a low tone that slows to a stop. Resume: the latch, then it spins up.
        "pause": room(mix(seq(key("interface-sounds", "click_004", 0.8), gap(0.02), key("interface-sounds", "click_005", 0.7)),
                          seq(gap(0.1), end_fade(tape(warm(note(0), 0.7, 0.01, 0.35), 1.0, 0.3)) * 0.45)), 0.1, 0.5),
        "resume": room(mix(key("interface-sounds", "click_004", 0.85),
                           seq(gap(0.05), end_fade(tape(warm(note(0), 0.7, 0.01, 0.35), 0.4, 1.0)) * 0.45)), 0.1, 0.5),
        # Phone: two quick clicks to unlock; one firm click (the side button) to lock.
        "phone_open": room(seq(key("ui-audio", "mouseclick1", 1.0, 0.07), gap(0.04), key("ui-audio", "mouseclick1", 1.15, 0.08)), 0.05, 0.25),
        "phone_close": room(key("interface-sounds", "click_003", 0.75, 0.12), 0.05, 0.25),
        # Notify: a quick little tick-tick-tick, then two soft notes.
        "notify": room(mix(ticks(["tick_001", "tick_002", "tick_004"], 0.07),
                           seq(gap(0.22), warm(note(19), 0.35, 0.004, 0.15) * 0.5, warm(note(24), 0.5, 0.004, 0.22) * 0.5, overlap=0.2)), 0.12, 0.6),
        # Objective: a ratchet winding up, a firm click, and a warm low fifth.
        "objective": room(seq(ratchet, key("interface-sounds", "click_004", 0.8),
                              mix(warm(note(0), 1.3, 0.05, 0.6), warm(note(7), 1.3, 0.1, 0.5) * 0.6) * 0.6, overlap=0.05), 0.15, 0.8),
    }


# ---- subtle: barely there -------------------------------------------------------------------------------------
def soft(name, pitch=1.0, cutoff=2500, s=0.15):
    """A sound from the downloaded packs (ArtSource/Sounds/packs), starting right at its onset, dulled and short."""
    from audition_packs import find
    y = pk.load(find(name))
    y = y[np.argmax(np.abs(y) > 0.05 * np.max(np.abs(y))):]
    y = np.interp(np.arange(0, len(y), pitch), np.arange(len(y)), y)
    return cut(norm(pk.lowpass(y, cutoff)), s)


def subtle():
    """Quiet, short and dull: felt more than heard. No clicks (every sound fades in over a few milliseconds, so there
    is no sharp edge), no tones that sing out, almost no tails."""
    attack = lambda y, a=0.012: y * np.minimum(np.arange(len(y)) / (R * a), 1)
    felt = lambda n, s=0.6: attack(pk.lowpass(ms.pluck(note(n), s, 0.985, 0.1), 1200), 0.01)
    hush = lambda s, a, r: norm(band(s, 150, 900)) * env(s, a, r)
    # A soft low "hm": a rounded tone with a breath of air around it, no edge.
    hm = lambda n, s=0.14: pk.lowpass(mix(warm(note(n), s, 0.012, s / 3.5), hush(s, 0.015, s / 4) * 0.25), 1400)
    return {
        "click": room(hm(14, 0.12), 0.04, 0.2),
        "toggle_on": room(seq(hm(12, 0.1), hm(16, 0.14), overlap=0.05), 0.05, 0.25),
        "toggle_off": room(seq(hm(16, 0.1), hm(12, 0.14), overlap=0.05), 0.05, 0.25),
        "pause": room(mix(hush(0.35, 0.03, 0.1) * 0.5, warm(note(0), 0.35, 0.01, 0.12) * 0.5), 0.06, 0.3),
        "resume": room(mix(hush(0.3, 0.1, 0.08) * 0.5, warm(note(7), 0.3, 0.03, 0.1) * 0.5), 0.06, 0.3),
        "phone_open": room(attack(soft("clothing_1", 1.0, 2000, 0.22), 0.03), 0.04, 0.2),
        "phone_close": room(attack(soft("clothing_2", 0.95, 1800, 0.2), 0.03), 0.04, 0.2),
        "notify": room(seq(felt(19, 0.5), felt(24, 0.6) * 0.8, overlap=0.38), 0.08, 0.4),
        "objective": room(seq(felt(7, 0.7), felt(12, 0.9), overlap=0.55), 0.08, 0.5),
    }


# ---- clips: the user's AudioClip download (ArtSource/Sounds/packs/AudioClip), kept quiet and natural ---------------
def clips():
    soften = lambda y, a=0.004: y * np.minimum(np.arange(len(y)) / (R * a), 1)  # takes the sharpest edge off
    return {
        "click": room(soften(soft("click", 0.95, 3000, 0.12)), 0.04, 0.2),
        # A real switch, a little higher for on and lower for off.
        "toggle_on": room(soften(soft("fuse-box-switch", 1.1, 2600, 0.25)), 0.04, 0.2),
        "toggle_off": room(soften(soft("fuse-box-switch", 0.9, 2200, 0.25)), 0.04, 0.2),
        # Pause/resume: see PAUSE_OPTIONS (the diary's page flip was replaced).
        "pause": PAUSE_OPTIONS[PAUSE][0](),
        "resume": PAUSE_OPTIONS[PAUSE][1](),
        # The phone comes out of a pocket and goes back in.
        "phone_open": room(soft("cloth-grab-01", 1.0, 2500, 0.45), 0.04, 0.2),
        "phone_close": room(soft("cloth-rustle-01", 1.05, 2300, 0.35), 0.04, 0.2),
        "notify": room(soft("chirp", 0.95, 3500, 0.3), 0.06, 0.3),
        # An objective done: a quick pencil scribble, like ticking it off a list.
        "objective": room(soft("scribble", 1.0, 4000, 1.0), 0.05, 0.3),
    }


# Pause/resume pairs to choose from (preview/variant_pause_options.wav, in this order). PAUSE is the one in the game.
_hush = lambda s, a, r: norm(band(s, 150, 900)) * env(s, a, r)
_muffle = lambda f0, f1, s=0.4: room(norm(sweep(band(s, 80, 4000), f0, f1)) * env(s, 0.05, s / 3), 0.06, 0.3)
PAUSE_OPTIONS = {
    # a soft breath with a low warm tone: down for pause, a fifth up for resume (made here, no licence needed)
    "soft tone": (lambda: room(mix(_hush(0.35, 0.03, 0.1) * 0.5, warm(note(0), 0.35, 0.01, 0.12) * 0.5), 0.06, 0.3),
                  lambda: room(mix(_hush(0.3, 0.1, 0.08) * 0.5, warm(note(7), 0.3, 0.03, 0.1) * 0.5), 0.06, 0.3)),
    # the world going muffled, then opening again (made here)
    "muffle": (lambda: _muffle(2500, 150), lambda: _muffle(150, 2500)),
    "tech blip": (lambda: room(soft("sci_fi_deselect", 0.9, 3000, 0.35), 0.05, 0.3),
                  lambda: room(soft("sci_fi_select", 0.9, 3000, 0.35), 0.05, 0.3)),
    "double click": (lambda: room(soft("click_double_off", 0.9, 3000, 0.3), 0.04, 0.2),
                     lambda: room(soft("click_double_on", 0.9, 3000, 0.3), 0.04, 0.2)),
    "power down/up": (lambda: room(soft("synth_shut_down", 1.0, 2500, 0.6), 0.06, 0.3),
                      lambda: room(soft("synth_confirmation", 1.0, 2500, 0.5), 0.06, 0.3)),
    "computer switch": (lambda: room(soft("computerswitch", 0.9, 2500, 0.35), 0.04, 0.2),
                        lambda: room(soft("computerswitch", 1.05, 2800, 0.35), 0.04, 0.2)),
}
PAUSE = "soft tone"

SETS = {"night": night, "signal": signal, "glass": glass, "pulse": pulse, "clicky": clicky, "subtle": subtle,
        "clips": clips}
# Quieter sets: their own peak levels, so they sit under the game instead of on top of it.
QUIET = {"subtle": {"click": 0.16, "toggle_on": 0.18, "toggle_off": 0.18, "pause": 0.2, "resume": 0.2,
                    "phone_open": 0.18, "phone_close": 0.17, "notify": 0.24, "objective": 0.24}}
QUIET["clips"] = QUIET["subtle"]


def finish(y):
    return end_fade(ms.trim(y), 0.01)


if __name__ == "__main__":
    if sys.argv[1:] and sys.argv[1] in SETS:
        for name, y in SETS[sys.argv[1]]().items():
            pk.write(f"{pk.OUT}/{name}.wav", finish(y), QUIET.get(sys.argv[1], PEAK)[name])
        print("installed", sys.argv[1])
        sys.exit()
    for set_name, make in SETS.items():
        sounds = make()
        assert list(sounds) == ORDER, set_name
        peaks = QUIET.get(set_name, PEAK)
        parts = []
        for name in ORDER:
            y = norm(finish(sounds[name])) * peaks[name]
            for _ in range(3 if name in ("click", "toggle_on", "toggle_off") else 1):
                parts += [y, gap(0.35)]
            parts.append(gap(0.8))
        pk.write(f"{pk.PREVIEW}/variant_{set_name}.wav", np.concatenate(parts), max(peaks.values()))  # true level
        print(set_name, ", ".join(f"{n} {len(finish(sounds[n])) / R:.2f}s" for n in ORDER))
    # Other clicks to choose from for the clicky set, each three times.
    parts = []
    for pack, name in CLICK_OPTIONS:
        y = norm(finish(room(key(pack, name), 0.04, 0.2))) * PEAK["click"]
        parts += [y, gap(0.3)] * 3 + [gap(0.9)]
    pk.write(f"{pk.PREVIEW}/variant_click_options.wav", np.concatenate(parts), 0.45)
    print("click options:", ", ".join(f"{i + 1}={n}" for i, (_, n) in enumerate(CLICK_OPTIONS)))
    # Pause/resume pairs: each pause, then its resume, at the game's level.
    parts = []
    for pause, resume in PAUSE_OPTIONS.values():
        parts += [norm(finish(pause())), gap(0.7), norm(finish(resume())), gap(1.4)]
    pk.write(f"{pk.PREVIEW}/variant_pause_options.wav", np.concatenate(parts), QUIET["clips"]["pause"])
    print("pause options:", ", ".join(f"{i + 1}={n}" for i, n in enumerate(PAUSE_OPTIONS)))
