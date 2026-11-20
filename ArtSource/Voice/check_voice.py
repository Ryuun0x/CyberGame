"""Listens to every voice file with a speech recogniser (Whisper small.en, on the CPU) and lists the lines whose words
don't come through as written: mispronounced, mumbled, skipped or made-up words.
  ArtSource/Voice/.venv/Scripts/python.exe ArtSource/Voice/check_voice.py [speaker]
Each line gets a score (100 = every word heard as written); below 90 is marked "!!" with what was heard.
Remake flagged lines with make_voice.py <key> <key> ...
"""
import os
import sys

import soundfile as sf

from lines import LINES
from make_voice import OUT, heard_score, key

if __name__ == "__main__":
    from transformers import pipeline
    asr = pipeline("automatic-speech-recognition", model="openai/whisper-small.en", device="cpu")
    only = sys.argv[1] if len(sys.argv) > 1 else None
    flagged = []
    for text, speaker, how, read in LINES:
        if only and speaker != only:
            continue
        y, sr = sf.read(os.path.join(OUT, key(text) + ".ogg"))
        score, heard = heard_score(asr, y, sr, read)
        print(f"{'!!' if score < 90 else '  '} {score:3d} {key(text)} {speaker} {how}: {read}")
        if score < 100:
            print(f"         heard: {heard}")
        if score < 90:
            flagged.append(key(text))
    print(f"{len(flagged)} line(s) below 90:", " ".join(flagged))
