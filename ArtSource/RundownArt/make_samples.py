"""Sample chapter-rundown pages from in-game renders.

Input: renders made by Assets/Editor/RundownStudio.cs in Temp/Shots:
  rd_<shot>.png          the scene on a green background
  rd_<shot>_id.png       flat colours: red = Alice, blue = the stranger, yellow = the held prop, grey = furniture
  rd_<shot>_anchors.json where the screens, the phone and the faces are in the picture (pixels)
Output: Assets/Sprites/Rundown/<threat id>_risky.png / _safer.png (the pictures the rundown pages show, transparent)
and ArtSource/RundownArt/samples/*.png (whole-page mock-ups, 1600x900).

Style: soft silhouettes (only broad light and shade), no added lights or glows. The "what you did" half is dark
charcoal with red accents, the "do this" half is light with green accents. What happens is told with small
notification-style cards (the network's name, the money sent, the call) and, for something moving between people,
a trail of characters (her password drifting to the stranger, coins leaving the phone).
"""
import json
import math
import os

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SHOTS = os.path.join(ROOT, "Temp", "Shots")
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "samples")
FONT = os.path.join(ROOT, "Assets", "TextMesh Pro", "Fonts", "LiberationSans.ttf")
UI = "C:/Windows/Fonts/segoeui.ttf"
UI_BOLD = "C:/Windows/Fonts/seguisb.ttf"
ICONS = "C:/Windows/Fonts/SegoeIcons.ttf"

BG = np.array((13, 15, 18), np.float32)
RED = (240, 88, 76)
GREEN = (92, 217, 133)
DIM = (138, 147, 163)
SS = 2                     # effects are drawn at 2x and scaled down, for smooth edges

TONES = {
    "wrong": {"alice": ((52, 55, 61), (104, 108, 116)), "stranger": ((58, 20, 20), (104, 34, 32)),
              "prop": ((150, 50, 45), (229, 83, 75)), "other": ((22, 24, 28), (34, 37, 42))},
    "right": {"alice": ((150, 155, 162), (222, 225, 230)), "stranger": ((70, 74, 80), (110, 114, 120)),
              "prop": ((210, 214, 200), (250, 250, 240)), "other": ((36, 39, 44), (58, 62, 68))},
}
IDS = {"alice": (255, 0, 0), "stranger": (0, 0, 255), "prop": (255, 255, 0), "other": (128, 128, 128)}
PAGE_SIZE = (1600, 900)


def font(path, size):
    return ImageFont.truetype(path, size)


# ── Silhouette ──

def silhouette(shot, mood):
    """(rgb, alpha) of one render at its own size: soft-shaded silhouettes, background transparent."""
    beauty = np.asarray(Image.open(os.path.join(SHOTS, f"rd_{shot}.png")).convert("RGB")).astype(np.float32)
    ids = np.asarray(Image.open(os.path.join(SHOTS, f"rd_{shot}_id.png")).convert("RGB")).astype(np.float32)
    r, g, b = beauty[..., 0], beauty[..., 1], beauty[..., 2]
    key = np.clip((g - np.maximum(r, b)) / 200.0, 0, 1)  # green screen: 1 = background
    alpha = 1 - key
    lum = (0.299 * r + 0.587 * g * (1 - key) + 0.114 * b) / 255.0
    lum = np.asarray(Image.fromarray((lum * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(5))).astype(np.float32) / 255.0
    rgb = np.zeros(beauty.shape, np.float32)
    names = list(IDS)
    owner = np.argmin(np.stack([np.linalg.norm(ids - np.array(IDS[n], np.float32), axis=-1) for n in names]), axis=0)
    for i, n in enumerate(names):
        lo, hi = (np.array(c, np.float32) for c in TONES[mood][n])
        m = owner == i
        rgb[m] = lo + (hi - lo) * (0.45 + 0.55 * np.clip(lum[m] * 1.6, 0, 1))[:, None]
    return rgb, alpha, (owner == 0) * alpha  # last: Alice, for things that pass behind her


# ── Light and ink ──

class Panel:
    """One half of a page, built at the render's size: the silhouettes and the ink (trails and cards, drawn at 2x)."""

    def __init__(self, shot, mood):
        self.rgb, self.alpha, alice = silhouette(shot, mood)
        self.h, self.w = self.alpha.shape
        self.alice = Image.fromarray((alice * 255).astype(np.uint8)).resize((self.w * SS, self.h * SS), Image.BILINEAR)
        with open(os.path.join(SHOTS, f"rd_{shot}_anchors.json")) as f:
            self.at = {k: np.array(v, np.float32) for k, v in json.load(f).items()}
        self.ink = Image.new("RGBA", (self.w * SS, self.h * SS), (0, 0, 0, 0))

    def mask(self):
        return Image.new("L", self.ink.size, 0)

    def emit(self, mask, colour, opacity=1.0, behind_alice=False):
        """Crisp ink from an L mask (2x). behind_alice: hidden where Alice stands in front."""
        m = np.asarray(mask).astype(np.float32) / 255.0
        if behind_alice:
            m *= 1 - 0.92 * np.asarray(self.alice).astype(np.float32) / 255.0
            mask = Image.fromarray((m * 255).astype(np.uint8))
        layer = Image.new("RGBA", self.ink.size, colour + (0,))
        layer.putalpha(Image.fromarray((m * 255 * opacity).astype(np.uint8)))
        self.ink.alpha_composite(layer)

    def image(self):
        # Transparent outside the figures, so the game can show it on any background.
        a = self.alpha[..., None]
        img = Image.fromarray(np.dstack([np.clip(self.rgb, 0, 255), a * 255]).astype(np.uint8), "RGBA")
        img.alpha_composite(self.ink.resize((self.w, self.h), Image.LANCZOS))
        # Crop to what's drawn (figures and ink), with a margin.
        drawn = (a[..., 0] > 0.05) | (np.asarray(self.ink.resize((self.w, self.h)).getchannel("A")) > 8)
        ys, xs = np.nonzero(drawn)
        pad = 24
        return img.crop((max(0, xs.min() - pad), max(0, ys.min() - pad), min(self.w, xs.max() + pad), min(self.h, ys.max() + pad)))


def bezier(p0, p1, p2, t):
    return (1 - t) ** 2 * p0 + 2 * (1 - t) * t * p1 + t ** 2 * p2


def stream(p, start, end, lift, text, colour, size=(9, 17)):
    """Characters drifting along a soft arc from start to end (fading out as they arrive)."""
    p0, p2 = np.array(start), np.array(end)
    p1 = (p0 + p2) / 2 + np.array((0, -lift))
    # A faint path underneath.
    m = p.mask()
    d = ImageDraw.Draw(m)
    pts = [tuple(bezier(p0, p1, p2, t) * SS) for t in np.linspace(0, 1, 60)]
    for k in range(len(pts) - 1):
        d.line((pts[k], pts[k + 1]), fill=int(110 * (1 - k / len(pts)) + 40), width=int(1.6 * SS))
    p.emit(m.filter(ImageFilter.GaussianBlur(0.6 * SS)), colour)
    # The characters themselves.
    m = p.mask()
    d = ImageDraw.Draw(m)
    n = len(text)
    rng = np.random.default_rng(7)
    for k, ch in enumerate(text):
        t = 0.06 + 0.9 * k / max(1, n - 1)
        x, y = bezier(p0, p1, p2, t) + rng.normal(0, 4, 2)
        s = size[1] - (size[1] - size[0]) * t
        alpha = 0.95 - 0.7 * t
        d.text((x * SS, y * SS), ch, font=font(UI_BOLD, int(s * SS)), fill=int(255 * alpha), anchor="mm")
    p.emit(m, colour)


def card(p, xy, lines, accent, avatar=None, icon_glyph=None):
    """A small frosted notification card: optional round avatar, a grey first line, an accent second line."""
    x, y = xy
    d = ImageDraw.Draw(p.ink)
    title, body = lines
    k = 1.5  # cards are scaled down with the picture; this keeps them readable on the page
    f1, f2 = font(UI, int(15 * k * SS)), font(UI_BOLD, int(20 * k * SS))
    tw = max(d.textlength(title, font=f1), d.textlength(body, font=f2))
    left = (14 + (40 if avatar or icon_glyph else 0)) * k
    w, h = left + tw / SS + 18 * k, 62 * k
    x, y = min(max(x, 8), p.w - w - 8), max(y, 8)  # always inside the picture
    box = (x * SS, y * SS, (x + w) * SS, (y + h) * SS)
    d.rounded_rectangle(box, radius=int(12 * k * SS), fill=(24, 27, 32, 225), outline=accent + (170,), width=int(1.5 * SS))
    if avatar:
        cx, cy = (x + 30 * k) * SS, (y + h / 2) * SS
        d.ellipse((cx - 16 * k * SS, cy - 16 * k * SS, cx + 16 * k * SS, cy + 16 * k * SS), fill=accent + (60,), outline=accent + (200,), width=SS)
        d.text((cx, cy), avatar, font=font(UI_BOLD, int(16 * k * SS)), fill=accent + (255,), anchor="mm")
    elif icon_glyph:
        d.text(((x + 30 * k) * SS, (y + h / 2) * SS), icon_glyph, font=font(ICONS, int(22 * k * SS)), fill=accent + (255,), anchor="mm")
    d.text(((x + left) * SS, (y + 12 * k) * SS), title, font=f1, fill=(160, 168, 180, 255))
    d.text(((x + left) * SS, (y + 30 * k) * SS), body, font=f2, fill=accent + (255,))


# ── The four pictures ──

def wifi_wrong():
    p = Panel("wifi_wrong", "wrong")
    a = p.at
    # His laptop runs the open copy of the café's network...
    card(p, a["stranger_screen"] + (-120, -300), ("Caf\u00e9 Wi-Fi", "Open, no password"), RED, icon_glyph="")
    # ...and her password travels to him.
    stream(p, a["alice_screen"] + (-10, -50), a["stranger_screen"] + (14, -30), 330, "• • • • • • • •", RED, size=(24, 36))
    return p.image()


def wifi_right():
    p = Panel("wifi_right", "right")
    a = p.at
    card(p, a["alice_screen"] + (-30, -260), ("Caf\u00e9 Wi-Fi", "Password protected"), GREEN, icon_glyph="")
    return p.image()


def kai_wrong():
    p = Panel("kai_wrong", "wrong")
    a = p.at
    card(p, a["phone"] + (10, -150), ("Sent to a friend", "\u20b11,000"), RED, icon_glyph="\uE724")
    # The money leaving: a few coins drifting away from the phone.
    stream(p, a["phone"] + (20, 0), a["phone"] + (260, 30), 70, "\u20b1 \u20b1 \u20b1", RED, size=(16, 26))
    return p.image()


def kai_right():
    p = Panel("kai_right", "right")
    a = p.at
    card(p, a["phone"] + (-330, -150), ("Calling...", "Your friend"), GREEN, icon_glyph="\uE77B")
    return p.image()


# ── Page ──

def wrap(text, f, width):
    words, lines, line = text.split(), [], ""
    probe = ImageDraw.Draw(Image.new("RGB", (1, 1)))
    for w in words:
        test = (line + " " + w).strip()
        if probe.textlength(test, font=f) > width and line:
            lines.append(line)
            line = w
        else:
            line = test
    return lines + [line]


def page(name, number, title, outcome, left, right, rule):
    """left/right: (picture, mood, (lead, text))."""
    img = Image.new("RGBA", PAGE_SIZE, tuple(int(c) for c in BG) + (255,))
    d = ImageDraw.Draw(img)
    big = font(FONT, 34)
    d.text((80, 60), title, font=big, fill=(232, 232, 232))
    d.text((80 + d.textlength(title, font=big) + 20, 64), number, font=font(FONT, 26), fill=DIM)
    colour = {"COMPROMISED": RED, "RECOVERED": (227, 179, 65), "SAFE": GREEN}[outcome]
    f = font(FONT, 20)
    bw = d.textlength(outcome, font=f) + 32
    d.rectangle((1520 - bw, 62, 1520, 100), outline=colour, width=2)
    d.text((1520 - bw / 2, 81), outcome, font=f, fill=colour, anchor="mm")
    d.line((800, 170, 800, 650), fill=(58, 63, 70), width=2)
    for side, (art, mood, (lead, text)) in enumerate((left, right)):
        x0, y0, x1, y1 = 90 + side * 800, 140, 710 + side * 800, 660
        scale = min((x1 - x0) / art.width, (y1 - y0) / art.height)
        art = art.resize((int(art.width * scale), int(art.height * scale)), Image.LANCZOS)
        img.alpha_composite(art, (x0 + ((x1 - x0) - art.width) // 2, y1 - art.height))
        cx = 400 + side * 800
        f = font(FONT, 26)
        y = 690
        for i, line in enumerate(wrap(f"{lead} {text}", f, 600)):
            x = cx - d.textlength(line, font=f) / 2
            if i == 0:
                d.text((x, y), lead, font=f, fill=RED if mood == "wrong" else GREEN)
                d.text((x + d.textlength(lead + " ", font=f), y), line[len(lead) + 1:], font=f, fill=(225, 225, 225))
            else:
                d.text((x, y), line, font=f, fill=(225, 225, 225))
            y += 36
    d.text((800, 790), rule, font=font(FONT, 24), fill=(200, 205, 214), anchor="mm")
    d.line((80, 830, 1520, 830), fill=(44, 48, 54), width=1)
    d.text((80, 858), "Details and source", font=font(FONT, 18), fill=DIM, anchor="lm")
    d.text((1520, 858), "PREVIOUS      " + number + "      NEXT", font=font(FONT, 18), fill=DIM, anchor="rm")
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, f"{name}.png")
    img.convert("RGB").save(path)
    return path


SPRITES = os.path.join(ROOT, "Assets", "Sprites", "Rundown")


# The other pages: one scene per page (renders by Assets/Editor/RundownShots.cs), the same scene on both sides with
# its own card, so every page looks different from the next.
#   threat: (shot for risky, shot for safer, anchor, card offset from it,
#            risky (title, body, glyph), safer (title, body, glyph), risky trail (to, lift, text) or None)
PAGES = {
    "login_alert": ("bed_phone", "bed_phone", "face", (90, -40),
                    ("Sign-in from another city", "Yes, it's me", ""),
                    ("Sign-in from another city", "No, it's not me", ""), None),
    "fake_update": ("desk_front", "desk_front", "face", (150, -70),
                    ("Browser out of date!", "Update now", ""),
                    ("Settings › Update", "You're up to date", ""),
                    ((585, 330), 60, "• • •")),
    "ransomware": ("desk_back", "desk_back", "screen", (-330, -230),
                   ("Your files are locked", "Pay ₱5,000", ""),
                   ("Restoring from backup", "Your files are back", ""),
                   ((880, 250), 110, "₱ ₱ ₱")),
    "backup": ("desk_side_plain", "desk_side", "screen", (-60, -280),
               ("thesis_final.docx", "Only copy: this laptop", ""),
               ("Saved in two places", "Flash drive + cloud", ""), None),
    "password_reuse": ("bed_laptop", "bed_laptop", "face", (90, -60),
                       ("New shop account", "Same old password", ""),
                       ("Password manager", "New password saved", ""), None),
    "online_selling_scam": ("sofa_phone", "sofa_phone", "face", (-500, -60),
                            ("Calculator · ₱99 · no reviews", "Paid by e-wallet", ""),
                            ("Official store · 4.9 stars", "Paid at checkout", ""),
                            ((880, 270), 80, "₱ ₱ ₱")),
    "mfa": ("stand_phone", "stand_phone", "face", (110, -20),
            ("Your account", "Password only", ""),
            ("Authenticator", "482 913", ""), None),
}
CAPTIONS = {
    "login_alert": ("A sign-in you didn't start", "tapping “Yes, it's me” on a sign-in request you didn't make.",
                    "denying it, then changing your password right away.", "Didn't start it? Deny it."),
    "fake_update": ("Fake browser update", "downloading and running an “update” from a pop-up.",
                    "closing the pop-up and updating from your system's own settings.", "Updates come from your system, not from web pages."),
    "ransomware": ("Files locked for ransom", "paying the attackers to unlock your files.",
                   "resetting the computer and restoring from your backup.", "Never pay. Restore from a backup."),
    "backup": ("Back up your files", "keeping only one copy of your work.",
               "keeping two backups: one on a flash drive, one in the cloud.", "Two copies, two places."),
    "password_reuse": ("Reused password", "using the same password on a new shopping site.",
                       "making a new, long password for every account.", "One account, one password."),
    "online_selling_scam": ("Too-good-to-be-true seller", "paying a new seller with no reviews up front by e-wallet.",
                            "buying from a trusted store and paying through checkout.", "Too cheap with no reviews? Walk away."),
    "mfa": ("Two-factor authentication", "protecting your account with a password only.",
            "turning on 2FA with an authenticator app.", "A stolen password alone shouldn't be enough."),
}


def picture(threat, side):
    shot_r, shot_s, anchor, offset, risky, safer, trail = PAGES[threat]
    bad = side == "risky"
    p = Panel(shot_r if bad else shot_s, "wrong" if bad else "right")
    title, body, glyph = risky if bad else safer
    xy = p.at[anchor] + offset
    if bad and trail:
        to, lift, text = trail
        start = p.at.get("screen", p.at.get("phone", p.at[anchor]))
        stream(p, start, to, lift, text, RED, size=(16, 26))
    card(p, xy, (title, body), RED if bad else GREEN, icon_glyph=glyph)
    return p.image()


if __name__ == "__main__":
    # The pictures the game uses (one per side of each threat's page, named after the ThreatCatalog id).
    pictures = {"evil_twin": (wifi_wrong(), wifi_right()), "friend_scam": (kai_wrong(), kai_right())}
    for threat in PAGES:
        pictures[threat] = (picture(threat, "risky"), picture(threat, "safer"))
        title, risky, safer, rule = CAPTIONS[threat]
        print(page(f"sample_page_{threat}", "1 / 1", title, "COMPROMISED",
                   (pictures[threat][0], "wrong", ("Risky:", risky)), (pictures[threat][1], "right", ("Safer:", safer)), rule))
    os.makedirs(SPRITES, exist_ok=True)
    for threat, (risky, safer) in pictures.items():
        risky.save(os.path.join(SPRITES, f"{threat}_risky.png"))
        safer.save(os.path.join(SPRITES, f"{threat}_safer.png"))
        print("sprites:", threat)
    # Mock-ups of whole pages, for reviewing the look outside the game.
    print(page("sample_cafe_wifi", "3 / 4", "Fake café Wi-Fi", "COMPROMISED",
               (pictures["evil_twin"][0], "wrong", ("Risky:", "typing your email password into a Wi-Fi sign-in page.")),
               (pictures["evil_twin"][1], "right", ("Safer:", "joining the café's secured network with the password from the staff.")),
               "Wi-Fi never needs your email password."))
    print(page("sample_friend_money", "2 / 5", "A friend asks for money", "SAFE",
               (pictures["friend_scam"][0], "wrong", ("Risky:", "replying in the same chat and sending the money.")),
               (pictures["friend_scam"][1], "right", ("Safer:", "calling your friend on another channel first.")),
               "A hacked account can't answer a phone call."))
