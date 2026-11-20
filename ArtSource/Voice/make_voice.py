"""Voices every line in lines.py with Chatterbox-Turbo and writes Assets/Resources/Voice/<key>.ogg, which
Assets/Script/Core/Voice.cs plays when the subtitle shows. <key> is a hash of the subtitle text (same as Voice.Key).
  ArtSource/Voice/.venv/Scripts/python.exe ArtSource/Voice/make_voice.py          only new or changed lines
  ArtSource/Voice/.venv/Scripts/python.exe ArtSource/Voice/make_voice.py all      every line again
  ArtSource/Voice/.venv/Scripts/python.exe ArtSource/Voice/make_voice.py <key>..  these lines again (keys as printed)
Each take is checked with a speech recogniser (Whisper small.en): a take whose words don't come through clearly is
made again, up to 6 times, keeping the clearest. check_voice.py runs the same check over every existing file.
Also writes ArtSource/Sounds/preview/voice_all.wav: every line in order, to listen through.
"""
import json
import os
import re
import sys
import time

import numpy as np
import soundfile as sf
import torch
from scipy.signal import lfilter

from lines import LINES

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "..", "Assets", "Resources", "Voice")
PREVIEW = os.path.join(HERE, "..", "Sounds", "preview", "voice_all.wav")
MADE = os.path.join(HERE, "made.json")  # what each file was made from, so unchanged lines are skipped


def key(text):
    """FNV-1a (32 bit) of the subtitle text without rich-text tags. Must match Voice.Key in Voice.cs."""
    h = 0x811C9DC5
    for b in re.sub(r"<[^>]*>", "", text).strip().encode("utf-8"):
        h = ((h ^ b) * 0x01000193) & 0xFFFFFFFF
    return f"{h:08x}"


def lowpass(y, cutoff, sr):
    k = 1 - np.exp(-2 * np.pi * cutoff / sr)
    return lfilter([k], [1, k - 1], y)


def trim(y, sr):
    """Cut the silence before and after the speech (keeping a breath of it), with short fades."""
    loud = np.nonzero(np.abs(y) > 0.02 * np.max(np.abs(y)))[0]
    y = y[max(loud[0] - int(sr * 0.03), 0):loud[-1] + int(sr * 0.12)].copy()
    f = int(sr * 0.01)
    y[:f] *= np.linspace(0, 1, f)
    y[-f:] *= np.linspace(1, 0, f)
    return y


def inner(y, sr):
    """Alice's thoughts: softer and closer, the top rounded off, a faint airy space around the voice."""
    rng = np.random.default_rng(7)
    n = int(sr * 0.45)
    ir = lowpass(rng.normal(0, 1, n), 3500, sr) * np.exp(-np.arange(n) / (sr * 0.09))
    ir /= np.sqrt(np.sum(ir ** 2))
    wet = np.convolve(y, ir)[:len(y) + n]
    out = np.zeros(len(wet))
    out[:len(y)] = lowpass(y, 6500, sr)
    return out + 0.16 * wet


def level(y):
    y = y * (0.08 / (np.sqrt(np.mean(y ** 2)) + 1e-9))  # every line equally loud
    return y * min(1.0, 0.9 / np.max(np.abs(y)))


def sane(seconds, words):
    """Turbo sometimes rambles past the text or stops early; such a take is made again."""
    n = len(re.sub(r"\[[^\]]+\]", "", words).split())
    return 0.15 * n + 0.3 < seconds < 0.75 * n + 3


# Spellings a speech recogniser writes differently from the script, though the words are the same.
SAME = {"wi-fi": "wifi", "wi fi": "wifi", "café": "cafe", "2fa": "two f a", "okay": "ok", "₱": "",
        "ghost line": "ghostline", "should've": "should have", "1,000": "one thousand", "9": "nine",
        "gadget.deals.ph": "gadget dot deals dot p h", "gadgetdeals.ph": "gadget dot deals dot p h",
        "gadget deals.ph": "gadget dot deals dot p h"}


def words(text):
    text = re.sub(r"\[[^\]]+\]", " ", text.lower())
    for a, b in SAME.items():
        text = text.replace(a, b)
    return re.sub(r"[^a-z0-9' ]+", " ", text).replace("'", "").split()


def heard_score(asr, y, sr, read):
    """How well a speech recogniser understands the take: 100 = every word heard as written. Catches mumbled,
    mispronounced, skipped and made-up words (an emotion tag like [whispering] can swallow a word)."""
    import librosa
    heard = asr(librosa.resample(y.astype(np.float32), orig_sr=sr, target_sr=16000))["text"].strip()
    import difflib
    return round(100 * difflib.SequenceMatcher(None, words(read), words(heard)).ratio()), heard


if __name__ == "__main__":
    from chatterbox.tts_turbo import ChatterboxTurboTTS
    redo = sys.argv[1:] == ["all"]
    made = json.load(open(MADE)) if os.path.exists(MADE) and not redo else {}
    for k in sys.argv[1:]:  # keys of lines to make again (e.g. ones check_voice.py flagged)
        made.pop(k, None)
    os.makedirs(OUT, exist_ok=True)
    keys = [key(text) for text, *_ in LINES]
    assert len(set(keys)) == len(keys), "two lines have the same text"

    model, sr, asr = None, None, None
    for i, (text, speaker, how, read) in enumerate(LINES, 1):
        k = key(text)
        ref = os.path.join(HERE, "refs", speaker + ".wav")
        ref = ref if os.path.exists(ref) else None
        recipe = [speaker, how, read, os.path.getmtime(ref) if ref else None]
        path = os.path.join(OUT, k + ".ogg")
        if made.get(k) == recipe and os.path.exists(path):
            continue
        if model is None:
            model = ChatterboxTurboTTS.from_pretrained(device="cpu")
            sr = model.sr
            from transformers import pipeline
            asr = pipeline("automatic-speech-recognition", model="openai/whisper-small.en", device="cpu")
        t0 = time.time()
        # Up to 6 takes; the first one the recogniser understands word for word (score 95+) is kept,
        # otherwise the best understood of them.
        best = None
        for attempt in range(6):
            torch.manual_seed(int(k, 16) + attempt)  # the same takes every run, unless the line changes
            wav = model.generate(read, audio_prompt_path=ref, temperature=0.8 if how == "thought" else 0.9)
            y = trim(wav.squeeze(0).numpy().astype(np.float64), sr)
            if not sane(len(y) / sr, read):
                continue
            score, heard = heard_score(asr, y, sr, read)
            if best is None or score > best[0]:
                best = (score, heard, y)
            if score >= 95:
                break
        score, heard, y = best if best else (0, "", y)
        if how == "thought":
            y = inner(y, sr)
        y = level(y)
        # In small blocks: libsndfile's Vorbis encoder overflows the stack if handed a long file in one write.
        with sf.SoundFile(path, "w", sr, 1, format="OGG", subtype="VORBIS") as f:
            for j in range(0, len(y), 4096):
                f.write(y[j:j + 4096])
        made[k] = recipe
        json.dump(made, open(MADE, "w"), indent=1)
        print(f"{i}/{len(LINES)} {k} {speaker} {how} {len(y) / sr:.1f}s ({time.time() - t0:.0f}s, take {attempt + 1}, "
              f"heard {score}%{'' if score >= 90 else ' CHECK: ' + heard}): {text}", flush=True)

    # Files for lines that are gone.
    for name in os.listdir(OUT):
        if name.endswith(".ogg") and name[:-4] not in keys:
            os.remove(os.path.join(OUT, name))
            if os.path.exists(os.path.join(OUT, name + ".meta")):
                os.remove(os.path.join(OUT, name + ".meta"))
            print("removed", name)

    parts = []
    for k in keys:
        y, sr = sf.read(os.path.join(OUT, k + ".ogg"))
        parts += [y, np.zeros(int(sr * 0.9))]
    sf.write(PREVIEW, np.concatenate(parts), sr)
    print("all lines:", os.path.normpath(PREVIEW))
