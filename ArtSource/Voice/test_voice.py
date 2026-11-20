"""Try Chatterbox on a few real lines, each read with the emotion of its moment in the story, on the CPU.
Two ways to steer the emotion, written side by side so they can be compared:
  turbo     Chatterbox-Turbo with its emotion/sound tags in the text: [groan] [sigh] [gasp] [fear] [surprised]
            [whispering] [angry] [crying] [happy] [sarcastic] [dramatic] [narration] [sniff] [cough] [chuckle] ...
  original  the original Chatterbox, no tags: an `exaggeration` dial (0.25 calm .. 1+ intense) and `cfg`
            (lower = slower, more deliberate delivery), set per emotion in EMOTIONS
Writes ArtSource/Sounds/preview/voice_<n>_<model>.wav. Each speaker's voice comes from
  ArtSource/Voice/refs/<speaker>.wav   (10-20 s of clear speech by someone who agreed to it; else the built-in voice)
Run with Chatterbox's own Python (it needs older libraries than the sound scripts):
  ArtSource/Voice/.venv/Scripts/python.exe ArtSource/Voice/test_voice.py [turbo|original|both]   (default: turbo)
"""
import os
import re
import sys
import time

import torchaudio as ta

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "Sounds", "preview")
# emotion: (exaggeration, cfg) for the original model
EMOTIONS = {
    "groggy": (0.3, 0.5),     # just woke up, head hurts
    "panic": (0.95, 0.3),     # late, rushing
    "worried": (0.6, 0.4),    # quiet fear for a friend
    "shocked": (0.85, 0.3),   # bad news out of nowhere
    "stressed": (0.5, 0.45),  # tired but has to get going
    "anchor": (0.4, 0.5),     # a steady news voice
}
LINES = [  # (speaker, emotion, text with Turbo tags; the tags are dropped for the original model)
    ("alice", "groggy", "[groan] Ugh... my head."),                                 # waking up after the night
    ("alice", "panic", "[gasp] Wait, it's already nine?! [fear] The thesis is due at noon."),
    ("alice", "worried", "[sigh] Ghostline... [whispering] I hope Kai's okay."),     # alone in the dark, after the news
    ("alice", "shocked", "[surprised] Wait, what?? [fear] Are you okay?"),          # Kai says he got hacked
    ("alice", "stressed", "[groan] Ugh... [sigh] Thesis is due at noon. I better check if the file synced."),
    ("news", "anchor", "[narration] Police link the attacks to a group calling itself Ghostline."),
]


def load(kind):
    t0 = time.time()
    if kind == "original":
        from chatterbox.tts import ChatterboxTTS
        model = ChatterboxTTS.from_pretrained(device="cpu")
    else:
        from chatterbox.tts_turbo import ChatterboxTurboTTS
        model = ChatterboxTurboTTS.from_pretrained(device="cpu")
    print(f"{kind} loaded in {time.time() - t0:.0f}s", flush=True)
    return model


def say(model, kind, speaker, emotion, text):
    ref = os.path.join(HERE, "refs", speaker + ".wav")
    ref = ref if os.path.exists(ref) else None
    if kind == "original":
        exaggeration, cfg = EMOTIONS[emotion]
        text = re.sub(r"\[[^\]]+\]\s*", "", text)
        return model.generate(text, audio_prompt_path=ref, exaggeration=exaggeration, cfg_weight=cfg)
    return model.generate(text, audio_prompt_path=ref, temperature=0.9)


if __name__ == "__main__":
    which = sys.argv[1] if len(sys.argv) > 1 else "turbo"  # the user picked Turbo with tags (2026-11-18)
    os.makedirs(OUT, exist_ok=True)
    for kind in (["turbo", "original"] if which == "both" else [which]):
        model = load(kind)
        for i, (speaker, emotion, text) in enumerate(LINES, 1):
            t0 = time.time()
            wav = say(model, kind, speaker, emotion, text)
            ta.save(os.path.join(OUT, f"voice_{i}_{kind}.wav"), wav, model.sr)
            print(f"{kind} {i} ({emotion}): {wav.shape[-1] / model.sr:.1f}s in {time.time() - t0:.0f}s: {text}", flush=True)
