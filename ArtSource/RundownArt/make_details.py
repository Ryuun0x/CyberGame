"""Sample "Details" page for the chapter rundown: how the trick works, as a flow on the dark page.

Clean line sketches of only what matters (two networks with the same name, a sign-in form, a password box), a few
curvy arrows to follow, and short notes placed along the way instead of captions under pictures.
Red is the trap, green the safe move. Real-world wording only: no names from the game.
Output: Assets/Sprites/Rundown/<threat id>_details.png (the flow alone, transparent; the game adds the title, the
source and CLOSE) and ArtSource/RundownArt/samples/sample_details_wifi.png (whole-page mock-up, 1600x900).
"""
import math
import os

import numpy as np
from PIL import Image, ImageDraw

from make_samples import BG, GREEN, ICONS, OUT, RED, bezier, font, wrap

SS = 2
TEXT = "C:/Windows/Fonts/segoeui.ttf"
BOLD = "C:/Windows/Fonts/seguisb.ttf"
CHALK = (226, 226, 220)
DIM = (130, 136, 148)
BODY = (200, 205, 214)
DARK = (30, 33, 38)
ROUGH = 0.8               # how far lines wander (px): a little hand-drawn, not scribbly
rng = np.random.default_rng(3)


class Sketch:
    """The page, drawn at 2x. Coordinates are 1x."""

    def __init__(self, transparent=False):
        # Transparent keeps the page colour in the clear pixels, so soft edges don't darken on the game's background.
        self.img = Image.new("RGBA", (1600 * SS, 900 * SS), tuple(int(c) for c in BG) + (0 if transparent else 255,))
        self.d = ImageDraw.Draw(self.img)

    # ── Lines ──

    def stroke(self, pts, colour=CHALK, width=2.6, rough=ROUGH):
        """A line through pts, nudged sideways by a slow wobble of about `rough` pixels: drawn, not ruled."""
        pts = np.array(pts, np.float32)
        cum = np.concatenate([[0], np.cumsum(np.hypot(*np.diff(pts, axis=0).T))])
        n = max(3, int(cum[-1] / 4))
        t = np.linspace(0, cum[-1], n)
        x, y = np.interp(t, cum, pts[:, 0]), np.interp(t, cum, pts[:, 1])
        if rough:
            wobble = np.convolve(rng.normal(0, 1, n + 24), np.ones(25) / 5, "valid")[:n] * rough
            wobble *= np.clip(np.minimum(t, cum[-1] - t) / 20, 0, 1)  # still at the ends, so boxes close cleanly
            gx, gy = np.gradient(x), np.gradient(y)
            g = np.hypot(gx, gy) + 1e-6
            x, y = x - gy / g * wobble, y + gx / g * wobble
        w = width * SS
        self.d.line([(a * SS, b * SS) for a, b in zip(x, y)], fill=colour, width=int(w), joint="curve")
        for a, b in ((x[0], y[0]), (x[-1], y[-1])):  # round ends
            self.d.ellipse((a * SS - w / 2, b * SS - w / 2, a * SS + w / 2, b * SS + w / 2), fill=colour)

    @staticmethod
    def outline(x0, y0, x1, y1, r, turns=1.0, drift=0.0):
        """Points around a rounded box, from the top-left, for `turns` times round; drift moves outwards as it goes."""
        corners = ((x1 - r, y0 + r, -90), (x1 - r, y1 - r, 0), (x0 + r, y1 - r, 90), (x0 + r, y0 + r, 180))
        lap = [(cx + r * math.cos(math.radians(a + k * 9)), cy + r * math.sin(math.radians(a + k * 9)))
               for cx, cy, a in corners for k in range(11)]
        pts = (lap * 2)[:int(len(lap) * turns) + 1]
        mx, my = (x0 + x1) / 2, (y0 + y1) / 2
        return [(px + (px - mx) / max(1, abs(px - mx)) * drift * i / len(pts), py + (py - my) / max(1, abs(py - my)) * drift * i / len(pts))
                for i, (px, py) in enumerate(pts)]

    def box(self, x0, y0, x1, y1, r=14, colour=CHALK, width=2.6, fill=None):
        if fill:
            self.d.rounded_rectangle([v * SS for v in (x0, y0, x1, y1)], radius=r * SS, fill=fill)
        self.stroke(self.outline(x0, y0, x1, y1, r), colour, width, ROUGH if r > 6 else 0.25)

    def ring(self, box, colour, r=12):
        """The thing to notice: drawn round once and a little past where it started, so the ends overlap."""
        self.stroke(self.outline(*box, r, 1.08, 3), colour, 2.6, ROUGH * 1.3)

    def arc(self, cx, cy, rx, ry, a0, a1, colour=CHALK, width=2.6, rough=ROUGH):
        th = np.linspace(math.radians(a0), math.radians(a1), 40)
        self.stroke(list(zip(cx + rx * np.cos(th), cy + ry * np.sin(th))), colour, width, rough)

    def arrow(self, start, end, bend, colour, width=3.2):
        p0, p2 = np.array(start, np.float32), np.array(end, np.float32)
        u = (p2 - p0) / np.linalg.norm(p2 - p0)
        p1 = (p0 + p2) / 2 + np.array((u[1], -u[0])) * bend
        line = [bezier(p0, p1, p2, t) for t in np.linspace(0, 1, 50)]
        self.stroke(line, colour, width)
        tip = line[-1]
        back = (line[-4] - tip) / np.linalg.norm(line[-4] - tip)
        for a in (-0.5, 0.5):
            c, s = math.cos(a), math.sin(a)
            self.stroke([tip, tip + np.array((back[0] * c - back[1] * s, back[0] * s + back[1] * c)) * 18], colour, width)
        return p0, p1, p2

    def wifi(self, cx, cy, s=1.0, colour=CHALK):
        for r in (6, 12, 18):
            self.arc(cx, cy, r * s, r * s, -135, -45, colour, 2.2, 0.2)
        self.d.ellipse(((cx - 2.2 * s) * SS, (cy - 2.2 * s) * SS, (cx + 2.2 * s) * SS, (cy + 2.2 * s) * SS), fill=colour)

    def lock(self, cx, cy, s=1.0, colour=CHALK):
        self.box(cx - 8 * s, cy - 2 * s, cx + 8 * s, cy + 11 * s, 3 * s, colour, 2.2)
        self.arc(cx, cy - 2 * s, 5 * s, 7 * s, 180, 360, colour, 2.2, 0.2)

    def person(self, cx, cy, r=18, colour=CHALK):
        """A round profile picture: head and shoulders in a circle."""
        self.arc(cx, cy, r, r, 0, 360, colour, 2.2, 0.3)
        self.arc(cx, cy - r * 0.18, r * 0.32, r * 0.32, 0, 360, colour, 2.0, 0)
        self.arc(cx, cy + r * 0.62, r * 0.6, r * 0.42, 200, 340, colour, 2.0, 0)

    def bubble(self, x0, y0, x1, y1, colour=CHALK):
        """A chat bubble with its tail at the bottom left (a message received)."""
        self.box(x0, y0, x1, y1, 16, colour)
        self.stroke([(x0 + 18, y1 - 1), (x0 - 6, y1 + 12), (x0 + 34, y1 - 1)], colour, 2.6, 0.3)

    def hacker(self, cx, top, sign=None, sign_face=BOLD):
        """A hooded person behind a laptop; the lid shows a Wi-Fi sign, or `sign` (text) if given."""
        hx, hy = cx, top + 60
        self.d.ellipse(((hx - 46) * SS, (hy - 50) * SS, (hx + 46) * SS, (hy + 52) * SS), fill=DARK)
        self.stroke(list(map(tuple, [bezier(np.array(a), np.array(b), np.array(c), t) for t in np.linspace(0, 1, 20)
                                     for a, b, c in [((cx - 95, top + 190), (cx - 85, top + 112), (cx - 40, top + 106))]])))
        self.stroke(list(map(tuple, [bezier(np.array(a), np.array(b), np.array(c), t) for t in np.linspace(0, 1, 20)
                                     for a, b, c in [((cx + 95, top + 190), (cx + 85, top + 112), (cx + 40, top + 106))]])))
        self.d.ellipse(((hx - 52) * SS, (hy - 62) * SS, (hx + 52) * SS, (hy + 54) * SS), fill=DARK)  # hood
        self.arc(hx, hy - 4, 52, 58, -100, 268, CHALK, 2.8)
        self.arc(hx, hy + 6, 30, 36, 0, 360, DIM, 1.8)                           # face in its shadow
        self.box(cx - 95, top + 150, cx + 95, top + 262, 10, fill=DARK)         # laptop, lid towards us
        self.stroke([(cx - 125, top + 272), (cx + 125, top + 272)], CHALK, 3.0)
        if sign:
            self.write((cx, top + 206), sign, 34, RED, "mm", sign_face)
        else:
            self.wifi(cx, top + 222, 1.3, RED)

    # ── Text ──

    def write(self, xy, text, size, colour=CHALK, anchor="la", face=TEXT):
        self.d.text((xy[0] * SS, xy[1] * SS), text, font=font(face, int(size * SS)), fill=colour, anchor=anchor)

    def note(self, xy, title, body, colour, width=380, centre=False):
        """A short note: a coloured line (\\n for two), then a sentence or two under it."""
        x, y = xy
        a = "ma" if centre else "la"
        for line in title.split("\n"):
            self.write((x, y), line, 24, colour, a, BOLD)
            y += 32
        for i, line in enumerate(wrap(body, font(TEXT, 19), width)):
            self.write((x, y + 4 + i * 27), line, 19, BODY, a)

    def save(self, path):
        self.img.resize((1600, 900), Image.LANCZOS).convert("RGB").save(path)
        return path


def wifi_flow(p):
    # 1. The list: two networks, same name. The open one is the copy.
    p.box(110, 190, 500, 360)
    p.write((132, 204), "Wi-Fi", 20, DIM)
    p.wifi(150, 272)
    p.write((180, 262), "Caf\u00e9 Wi-Fi", 26, anchor="lm")
    p.lock(460, 258)
    p.ring((436, 236, 484, 284), GREEN, 24)  # the real one has the lock: the "or..." below
    p.wifi(150, 332)
    p.write((180, 322), "Caf\u00e9 Wi-Fi", 26, anchor="lm")
    p.ring((122, 294, 488, 350), RED)
    p.note((110, 390), "Same name, but no lock",
           "Anyone can name a network after the caf\u00e9. The open one can be a copy someone nearby is running.", RED)

    # 2. The open one opens a sign-in page that wants your email password.
    p.arrow((505, 322), (690, 230), 50, RED)
    p.box(700, 150, 1030, 460)
    p.write((722, 164), "free-wifi-portal.net", 18, DIM)
    p.stroke([(712, 196), (1018, 196)], DIM, 1.6)
    p.write((865, 216), "Free Wi-Fi", 28, anchor="ma", face=BOLD)
    p.write((730, 274), "Email", 19, DIM)
    p.box(730, 300, 1000, 334, 8, CHALK, 2.0)
    p.write((730, 342), "Email password", 19, DIM)
    p.box(730, 382, 1000, 416, 8, CHALK, 2.0)
    p.write((746, 399), "\u2022 \u2022 \u2022 \u2022 \u2022 \u2022 \u2022", 22, anchor="lm")
    p.ring((720, 374, 1010, 424), RED)
    p.note((700, 490), "Wi-Fi never needs your email password",
           "This page isn't your email service. It belongs to whoever runs the network.", RED, 340)

    # 3. It goes straight to whoever runs the copy.
    a = p.arrow((1040, 399), (1240, 360), -60, RED)
    p.write(tuple(bezier(*a, 0.5) + (0, -30)), "\u2022 \u2022 \u2022 \u2022", 28, RED, anchor="mm")
    p.hacker(1370, 160)
    p.note((1370, 470), "Now they have your login",
           "They can read your mail and reset your other accounts with it.", RED, 290, centre=True)

    # Or: the one with the lock.
    p.write((150, 596), "or...", 34, GREEN, face=BOLD)
    p.arrow((240, 625), (430, 700), 30, GREEN)
    p.box(440, 640, 760, 770)
    p.lock(470, 668, 1.2, GREEN)
    p.write((496, 668), "Password for Caf\u00e9 Wi-Fi", 20, DIM, anchor="lm")
    p.box(462, 704, 738, 744, 8, CHALK, 2.0)
    p.write((478, 724), "\u2022 \u2022 \u2022 \u2022 \u2022 \u2022", 22, anchor="lm")
    p.ring((452, 694, 748, 754), GREEN)
    p.note((800, 650), "Ask the staff for the password",
           "The real network only needs that one. Your email login stays yours.", GREEN, 420)


def friend_flow(p):
    # A fork: everything starts with the one message (top), then splits. Reply and pay (red, left and down,
    # spreading to more friends) or call (green, right, where the real friend answers).
    p.box(520, 135, 1000, 334)
    p.person(560, 175)
    p.write((590, 165), "Your friend", 24, anchor="lm", face=BOLD)
    p.write((590, 189), "Active now", 17, DIM, anchor="lm")
    p.stroke([(532, 212), (988, 212)], DIM, 1.6)
    p.bubble(554, 232, 900, 302)
    p.write((574, 254), "Emergency! Can you send me", 20, anchor="lm")
    p.write((574, 282), "₱1,000 now? I'll pay you back.", 20, anchor="lm")
    p.ring((540, 222, 914, 322), RED)
    p.note((110, 180), "It looks just like your friend",
           "The name and photo are real, but the account may be hacked: someone else is typing.", RED, 400)

    # Red: reply in the chat and send the money.
    p.arrow((600, 342), (330, 418), 40, RED)
    p.write((520, 420), "reply & send", 20, RED, anchor="mm", face=BOLD)
    p.box(110, 428, 400, 596)
    p.write((255, 454), "Send money", 22, anchor="mm", face=BOLD)
    p.write((130, 492), "To", 17, DIM, anchor="lm")
    p.write((170, 492), "0917 ••• 2468", 19, anchor="lm")
    p.write((386, 492), "new", 15, RED, anchor="rm")
    p.ring((120, 474, 392, 510), RED)
    p.write((130, 530), "Amount", 17, DIM, anchor="lm")
    p.write((210, 530), "₱1,000", 20, anchor="lm", face=BOLD)
    p.box(185, 552, 325, 584, 10, CHALK, 2.0)
    p.write((255, 568), "Send", 17, anchor="mm", face=BOLD)
    p.note((110, 616), "The money goes to a stranger", "Not to your friend, and it's very hard to get back.", RED, 300)

    a = p.arrow((406, 520), (492, 612), -30, RED)
    p.write(tuple(bezier(*a, 0.5) + (24, -6)), "₱", 22, RED, anchor="mm", face=BOLD)
    p.hacker(600, 470, "₱")
    # ...and the same message goes out to the next friends.
    p.note((790, 430), "Then they ask the next friend", "The same account messages everyone until someone pays.", RED, 300)
    for y in (556, 636, 716):
        p.arrow((710, 600), (772, y), 0 if y == 636 else (-18 if y < 636 else 18), RED, 2.4)
        p.person(796, y, 14, DIM)
        p.bubble(820, y - 17, 1010, y + 15, DIM)
        p.write((834, y - 1), "Emergency! Can you…", 15, DIM, anchor="lm")

    # Green: call them on their number instead.
    p.arrow((940, 342), (1180, 402), -40, GREEN)
    p.write((1100, 344), "call them", 20, GREEN, anchor="mm", face=BOLD)
    p.box(1130, 410, 1490, 500)
    p.person(1172, 455, 22, GREEN)
    p.write((1206, 443), "Calling your friend...", 21, anchor="lm", face=BOLD)
    p.write((1206, 469), "Mobile", 17, DIM, anchor="lm")
    p.write((1462, 456), "", 20, GREEN, anchor="mm", face=ICONS)
    p.arrow((1310, 508), (1310, 552), 0, GREEN)
    p.bubble(1150, 562, 1490, 636, GREEN)
    p.write((1170, 586), "That wasn't me! Someone got", 19, anchor="lm")
    p.write((1170, 612), "into my account.", 19, anchor="lm")
    p.note((1130, 664), "Call your friend first",
           "Use their phone number, not the chat. A hacked account can't answer a call.", GREEN, 370)


def login_flow(p):
    # Two lanes: what they do with your stolen password (top), what reaches you (bottom).
    p.write((1510, 426), "THEM", 18, DIM, anchor="rs", face=BOLD)
    p.write((1510, 446), "YOU", 18, DIM, anchor="ra", face=BOLD)
    for x in range(110, 1510, 24):  # the dashed line between the two
        p.stroke([(x, 436), (x + 12, 436)], (58, 63, 70), 1.6, 0)
    p.hacker(250, 150, "••••")
    p.arrow((350, 330), (432, 286), -10, RED)
    p.box(440, 170, 730, 372)
    p.write((585, 196), "Sign in", 22, anchor="mm", face=BOLD)
    p.box(462, 226, 708, 260, 8, CHALK, 2.0)
    p.write((476, 243), "you@mail.com", 18, anchor="lm")
    p.box(462, 276, 708, 310, 8, CHALK, 2.0)
    p.write((476, 293), "• • • • • • • •", 20, anchor="lm")
    p.ring((452, 268, 718, 318), RED)
    p.write((585, 344), "your stolen password", 17, RED, anchor="mm")
    p.note((770, 170), "They already have your password",
           "Stolen from a fake page or a leak. Only one thing is left in their way: you.", RED, 340)

    # The request crosses down to your phone, again and again.
    p.arrow((600, 378), (600, 452), 0, RED)
    for k in (2, 1):
        p.box(470 + k * 10, 470 - k * 10, 810 + k * 10, 740 - k * 10, 14, DIM, 1.6)
    p.box(470, 470, 810, 740, fill=tuple(int(c) for c in BG) + (255,))
    p.write((640, 502), "Was this you?", 26, anchor="mm", face=BOLD)
    for i, (label, value) in enumerate((("Device", "Windows PC"), ("Location", "Another city"), ("Time", "Just now"))):
        y = 546 + i * 30
        p.write((494, y), label, 17, DIM, anchor="lm")
        p.write((600, y), value, 19, anchor="lm")
    p.ring((484, 528, 796, 594), RED)
    p.box(494, 628, 786, 664, 10, CHALK, 2.2)
    p.write((640, 646), "Yes, it's me", 20, anchor="mm", face=BOLD)
    p.box(494, 692, 786, 728, 10, CHALK, 2.2)
    p.write((640, 710), "No, it's not me", 20, anchor="mm", face=BOLD)
    p.note((110, 484), "You didn't start this",
           "A device you don't own, in a city you're not in. They may send it again and again, hoping you tap Yes.", RED, 320)

    # Yes: the request goes back up and lets them in.
    p.ring((486, 620, 794, 672), RED)
    p.arrow((800, 640), (1140, 300), 70, RED)
    p.box(1150, 150, 1500, 360, colour=RED)
    p.write((1182, 190), "", 22, RED, anchor="mm", face=ICONS)
    p.write((1206, 190), "They're in", 24, RED, anchor="lm", face=BOLD)
    for i, line in enumerate(("Password changed", "Recovery email changed", "You're signed out")):
        y = 246 + i * 36
        p.d.ellipse(((1184 - 3.5) * SS, (y - 3.5) * SS, (1184 + 3.5) * SS, (y + 3.5) * SS), fill=RED)
        p.write((1202, y), line, 20, anchor="lm")

    # No: blocked. Then a new password makes the stolen one useless.
    p.ring((486, 684, 794, 736), GREEN)
    p.arrow((800, 710), (950, 690), -20, GREEN)
    p.box(960, 620, 1220, 750, colour=GREEN)
    p.lock(992, 652, 1.2, GREEN)
    p.write((1016, 652), "Sign-in blocked", 21, anchor="lm", face=BOLD)
    p.write((980, 692), "Next: change your", 18, BODY, anchor="lm")
    p.write((980, 718), "password", 18, BODY, anchor="lm")
    p.note((1250, 612), "Deny it, then\nchange your password",
           "They stay out, and a new password makes the stolen one useless.", GREEN, 260)


def update_flow(p):
    # A staircase down to locked files: pop-up → downloaded file → warning → files locked. Each step has its own
    # green way out, and the real update sits apart, in the system's settings (top right).
    def check(x, y, text):
        """A green way out: a tick in a circle and two or three words."""
        p.arc(x, y, 13, 13, 0, 360, GREEN, 2.2, 0.2)
        p.stroke([(x - 6, y), (x - 1, y + 5), (x + 7, y - 5)], GREEN, 2.4, 0)
        p.write((x + 22, y), text, 19, GREEN, anchor="lm", face=BOLD)

    def file(x, y, colour=CHALK, s=1.0):
        """A page with a folded corner (x, y: top left)."""
        w, h, f = 26 * s, 34 * s, 8 * s
        p.stroke([(x, y), (x + w - f, y), (x + w, y + f), (x + w, y + h), (x, y + h), (x, y)], colour, 2.0, 0.2)
        p.stroke([(x + w - f, y), (x + w - f, y + f), (x + w, y + f)], colour, 1.6, 0)

    # 1. The pop-up.
    p.box(110, 150, 470, 296)
    p.write((136, 182), "", 20, RED, anchor="mm", face=ICONS)
    p.write((158, 182), "Your browser is out of date!", 20, anchor="lm", face=BOLD)
    p.write((136, 214), "Update now to keep browsing safely.", 17, DIM, anchor="lm")
    p.box(270, 242, 450, 280, 10, CHALK, 2.2)
    p.write((360, 261), "Update now", 18, anchor="mm", face=BOLD)
    p.ring((262, 234, 458, 288), RED)
    check(500, 178, "close the page")
    p.note((110, 318), "It says this to everyone",
           "A web page can't check your browser. It's bait, so you'll download its file.", RED, 340)

    # 2. The file it downloads.
    p.arrow((466, 262), (566, 334), -40, RED)
    p.box(500, 340, 840, 470)
    file(522, 360)
    p.write((562, 370), "browser_update.exe", 19, anchor="lm", face=BOLD)
    p.write((562, 394), "2.4 MB · done", 16, DIM, anchor="lm")
    p.ring((512, 352, 828, 410), RED)
    p.box(700, 422, 820, 456, 10, CHALK, 2.0)
    p.write((760, 439), "Open", 17, anchor="mm", face=BOLD)
    check(870, 370, "delete it")
    p.note((500, 490), "The “update” is a program",
           ".exe means it runs on your laptop and can do anything you can.", RED, 300)

    # 3. The warning before it runs.
    p.arrow((836, 446), (930, 524), -40, RED)
    p.box(850, 530, 1190, 706)
    p.write((1020, 558), "Allow this app to make changes?", 18, anchor="mm", face=BOLD)
    p.write((872, 596), "Program", 16, DIM, anchor="lm")
    p.write((960, 596), "browser_update.exe", 17, anchor="lm")
    p.write((872, 626), "Publisher", 16, DIM, anchor="lm")
    p.write((960, 626), "Unknown", 17, RED, anchor="lm", face=BOLD)
    p.ring((862, 610, 1178, 642), RED)
    p.box(872, 656, 1012, 692, 10, CHALK, 2.0)
    p.write((942, 674), "No", 17, anchor="mm", face=BOLD)
    p.box(1030, 656, 1170, 692, 10, CHALK, 2.0)
    p.write((1100, 674), "Yes", 17, anchor="mm", face=BOLD)
    check(1220, 546, "click No")
    p.note((500, 640), "No company stands behind it",
           "Real updates name their maker. “Unknown” means no one does.", RED, 320)

    # The bottom of the stairs: everything locked.
    p.arrow((1174, 692), (1262, 726), -20, RED)
    p.box(1270, 612, 1510, 780, colour=RED)
    p.write((1296, 640), "", 18, RED, anchor="mm", face=ICONS)
    p.write((1314, 640), "Files locked", 21, RED, anchor="lm", face=BOLD)
    for i in range(4):
        x = 1296 + i * 52
        file(x, 668, DIM)
        p.lock(x + 13, 690, 0.65, RED)
    p.write((1290, 742), "Pay to get them back.", 18, BODY, anchor="lm")

    # Apart from all that: where real updates come from.
    p.box(1000, 150, 1500, 316, colour=GREEN)
    p.write((1024, 178), "Settings  ›  Update", 17, DIM, anchor="lm")
    p.arc(1038, 222, 13, 13, 0, 360, GREEN, 2.2, 0.2)
    p.stroke([(1032, 222), (1037, 227), (1045, 217)], GREEN, 2.4, 0)
    p.write((1062, 222), "You're up to date", 22, anchor="lm", face=BOLD)
    p.write((1062, 250), "Last checked: today", 16, DIM, anchor="lm")
    p.box(1290, 262, 1480, 298, 10, CHALK, 2.0)
    p.write((1385, 280), "Check for updates", 16, anchor="mm", face=BOLD)
    p.note((1000, 336), "Updates come from your system",
           "Your laptop updates itself from its own settings. A web page can't even see which version you have.", GREEN, 480)

    p.note((110, 640), "Three chances to stop",
           "Close the page, delete the file, or click No. Any one of them keeps your laptop safe.", GREEN, 360)


def ransom_flow(p):
    # The ransom note (top left) leads into a loop (red, right): pay, they ask for more, the files stay locked,
    # pay again. The way out runs in a straight line underneath (green): don't pay, reset, restore, done.
    def head(tip, prev, colour, width=3.2):
        back = (prev - tip) / np.linalg.norm(prev - tip)
        for a in (-0.5, 0.5):
            c, s = math.cos(a), math.sin(a)
            p.stroke([tip, tip + np.array((back[0] * c - back[1] * s, back[0] * s + back[1] * c)) * 18], colour, width)

    def pill(cx, cy, text, colour, w=210):
        p.box(cx - w / 2, cy - 22, cx + w / 2, cy + 22, 22, colour, 2.4, fill=tuple(int(c) for c in BG) + (255,))
        p.write((cx, cy), text, 19, colour, anchor="mm", face=BOLD)

    def file(x, y, colour=CHALK, s=1.0):
        w, h, f = 26 * s, 34 * s, 8 * s
        p.stroke([(x, y), (x + w - f, y), (x + w, y + f), (x + w, y + h), (x, y + h), (x, y)], colour, 2.0, 0.2)
        p.stroke([(x + w - f, y), (x + w - f, y + f), (x + w, y + f)], colour, 1.6, 0)

    # The note.
    p.box(110, 140, 520, 430, colour=RED)
    p.write((140, 174), "", 20, RED, anchor="mm", face=ICONS)
    p.write((162, 174), "Your files are locked", 24, RED, anchor="lm", face=BOLD)
    for i, name in enumerate(("thesis_final.docx", "photos.zip", "notes.txt")):
        y = 222 + i * 36
        file(136, y - 14, DIM, 0.75)
        p.write((168, y), name, 18, anchor="lm")
        p.write((168 + p.d.textlength(name, font=font(TEXT, 18 * SS)) / SS, y), ".locked", 18, RED, anchor="lm")
    p.stroke([(130, 338), (500, 338)], DIM, 1.4)
    p.write((136, 370), "Pay ₱5,000 to unlock them.", 21, anchor="lm", face=BOLD)
    p.write((136, 402), "Time left: 23:59:12", 17, RED, anchor="lm")

    # The loop: in at "Pay" (left), round clockwise, back to "Pay".
    cx, cy, rx, ry = 1090, 300, 230, 135
    nodes = [(180, "Pay ₱5,000"), (300, "“Now ₱10,000”"), (420, "Still locked")]
    for k in range(3):
        a0, a1 = nodes[k][0] + 30, (nodes[(k + 1) % 3][0] if k < 2 else 540) - 30
        th = np.radians(np.linspace(a0, a1, 40))
        pts = np.stack([cx + rx * np.cos(th), cy + ry * np.sin(th)], 1)
        p.stroke(pts, RED, 3.0)
        head(pts[-1], pts[-4], RED, 3.0)
    for a, text in nodes:
        pill(cx + rx * math.cos(math.radians(a)), cy + ry * math.sin(math.radians(a)), text, RED)
    p.write((cx + 10, cy - 12), "and again,", 18, DIM, anchor="mm")
    p.write((cx + 10, cy + 12), "and again", 18, DIM, anchor="mm")
    p.arrow((528, 270), (740, 296), -20, RED)
    p.write((636, 262), "pay", 20, RED, anchor="mm", face=BOLD)
    p.note((1140, 470), "Paying doesn't unlock them",
           "It shows them you will pay. Many who pay are asked for more, and the files stay locked.", RED, 380)

    # The way out.
    p.arrow((300, 436), (240, 552), 30, GREEN)
    p.write((322, 500), "don't pay", 20, GREEN, anchor="lm", face=BOLD)
    p.box(110, 560, 380, 670, colour=GREEN)
    p.write((140, 594), "", 20, GREEN, anchor="mm", face=ICONS)
    p.write((162, 594), "Reset the laptop", 20, anchor="lm", face=BOLD)
    p.write((132, 630), "Wipes out what locked it", 17, DIM, anchor="lm")

    p.arrow((386, 615), (436, 615), 0, GREEN)
    p.box(446, 540, 776, 700, colour=GREEN)
    p.write((611, 570), "Restore from backup", 20, anchor="mm", face=BOLD)
    file(474, 598, CHALK, 0.8)
    p.write((506, 612), "Flash drive", 18, anchor="lm")
    p.write((474 + 12, 664), "", 20, CHALK, anchor="mm", face=ICONS)
    p.write((506, 664), "Cloud drive", 18, anchor="lm")
    for y in (612, 664):
        p.arc(744, y, 11, 11, 0, 360, GREEN, 2.0, 0.2)
        p.stroke([(739, y), (743, y + 4), (750, y - 4)], GREEN, 2.2, 0)

    p.arrow((782, 615), (832, 615), 0, GREEN)
    p.box(842, 560, 1102, 670, colour=GREEN)
    file(866, 592, GREEN, 1.1)
    p.write((910, 602), "thesis_final.docx", 18, anchor="lm", face=BOLD)
    p.write((910, 630), "Opens again", 17, GREEN, anchor="lm")

    p.note((842, 700), "Never pay. Restore from a backup.",
           "A copy kept somewhere else gets your work back, and the attackers get nothing.", GREEN, 560)


def reuse_flow(p):
    # Side by side, the same leak in two worlds: left, one password everywhere (the leak fans out and opens every
    # account); right, a different password for each (the leak stops at the shop).
    accounts = (("", "Email", ICONS), ("₱", "Bank", BOLD), ("", "Social", ICONS), ("", "School", ICONS))

    def shop(cx, password):
        p.box(cx - 180, 190, cx + 180, 296, colour=RED)
        p.write((cx - 150, 220), "", 20, CHALK, anchor="mm", face=ICONS)
        p.write((cx - 128, 220), "Online shop", 21, anchor="lm", face=BOLD)
        p.write((cx + 160, 220), "DATA LEAK", 15, RED, anchor="rm", face=BOLD)
        p.write((cx - 160, 264), "you@mail.com", 18, DIM, anchor="lm")
        p.write((cx + 160, 264), password, 19, RED, anchor="rm", face=BOLD)

    def tile(cx, glyph, label, face, opened):
        p.box(cx - 66, 510, cx + 66, 606)
        p.write((cx, 540), glyph, 24, CHALK, anchor="mm", face=face)
        p.write((cx, 578), label, 18, anchor="mm")
        if opened:
            p.write((cx + 48, 526), "", 16, RED, anchor="mm", face=ICONS)
        else:
            p.lock(cx + 48, 522, 0.8, GREEN)

    # Left: the same password everywhere.
    p.write((440, 168), "The same password everywhere", 20, RED, anchor="mm", face=BOLD)
    shop(440, "Sunny2004")
    p.arrow((440, 302), (440, 358), 0, RED)
    p.box(350, 366, 530, 406, 20, RED, 2.4)
    p.write((440, 386), "Sunny2004", 19, RED, anchor="mm", face=BOLD)
    xs = (190, 357, 523, 690)
    for x, (glyph, label, face) in zip(xs, accounts):
        p.arrow((440 + (x - 440) * 0.18, 412), (x, 502), 0, RED, 2.6)
        tile(x, glyph, label, face, True)
    p.note((124, 640), "One leak opens everything",
           "Attackers try a leaked password on other sites. If you used it there too, it works.", RED, 670)

    # The line between the two.
    for y in range(160, 790, 24):
        p.stroke([(810, y), (810, y + 12)], (58, 63, 70), 1.6, 0)

    # Right: a different password for every account.
    p.write((1180, 168), "A different password for each", 20, GREEN, anchor="mm", face=BOLD)
    shop(1180, "k7#Rain-Bus-Lamp")
    p.arrow((1180, 302), (1180, 358), 0, RED)
    p.box(1070, 366, 1290, 406, 20, RED, 2.4)
    p.write((1180, 386), "k7#Rain-Bus-Lamp", 19, RED, anchor="mm", face=BOLD)
    xs = (930, 1097, 1263, 1430)
    for x, (glyph, label, face) in zip(xs, accounts):
        start = np.array((1180 + (x - 1180) * 0.18, 412))
        end = start + (np.array((x, 502)) - start) * 0.55
        p.stroke([start, end], DIM, 2.0)
        cxx, cyy = end
        p.stroke([(cxx - 7, cyy - 7), (cxx + 7, cyy + 7)], DIM, 2.4, 0)
        p.stroke([(cxx - 7, cyy + 7), (cxx + 7, cyy - 7)], DIM, 2.4, 0)
        tile(x, glyph, label, face, False)
    p.note((864, 640), "One account, one password",
           "A leak stays at the site that leaked. A password manager remembers the rest for you.", GREEN, 620)


def seller_flow(p):
    # Spot the signs: two listings of the same calculator face to face. Numbered red flags point in at the cheap one
    # from the left, green ticks at the trusted one from the right; under each, where it ends.
    def badge(x, y, colour, text=None):
        p.arc(x, y, 11, 11, 0, 360, colour, 2.0, 0.2)
        if text:
            p.write((x, y), text, 13, colour, anchor="mm", face=BOLD)
        else:
            p.stroke([(x - 5, y), (x - 1, y + 4), (x + 5, y - 4)], colour, 2.0, 0)

    def listing(x0, price, old, seller, joined, stars, reviews, pay, bad):
        x1 = x0 + 330
        flag = RED if bad else CHALK
        p.box(x0, 160, x1, 528)
        p.box(x0 + 20, 180, x1 - 20, 296, 10, DIM, 1.6)
        p.write(((x0 + x1) / 2, 238), "", 52, CHALK, anchor="mm", face=ICONS)
        p.write((x0 + 20, 322), "Scientific calculator", 19, anchor="lm", face=BOLD)
        p.write((x0 + 20, 360), price, 28, flag, anchor="lm", face=BOLD)
        if old:
            w = p.d.textlength(price, font=font(BOLD, 28 * SS)) / SS
            p.write((x0 + 34 + w, 362), old, 17, DIM, anchor="lm")
            ow = p.d.textlength(old, font=font(TEXT, 17 * SS)) / SS
            p.stroke([(x0 + 32 + w, 362), (x0 + 36 + w + ow, 362)], DIM, 1.4, 0)
        p.person(x0 + 30, 404, 11, DIM)
        p.write((x0 + 50, 404), seller, 17, anchor="lm")
        p.write((x0 + 50, 432), joined, 16, flag, anchor="lm")
        p.write((x0 + 30, 462), stars, 14, flag if bad else (240, 200, 90), anchor="mm", face=ICONS)
        p.write((x0 + 50, 462), reviews, 16, flag, anchor="lm")
        p.write((x0 + 20, 498), pay, 16, flag, anchor="lm", face=BOLD)

    def callout(x, y, title, body, colour, to, align="l"):
        a = "la" if align == "l" else "ra"
        p.write((x, y), title, 20, colour, anchor=a, face=BOLD)
        p.write((x, y + 28), body, 16, BODY, anchor=a)
        edge = x + (p.d.textlength(title, font=font(BOLD, 20 * SS)) / SS + 10 if align == "l" else -p.d.textlength(title, font=font(BOLD, 20 * SS)) / SS - 10)
        p.stroke([(edge, y + 12), to], colour, 1.6, 0.3)

    # The cheap listing and its red flags.
    listing(400, "₱99", "₱1,200", "gadget_deals22", "Joined 3 days ago", "", "No reviews yet",
            "Pay by e-wallet first", True)
    rows = {"price": 360, "joined": 432, "reviews": 462, "pay": 498}
    flags = [("1", "Far too cheap", "Real ones cost about ₱1,200.", "price"),
             ("2", "A brand-new account", "Easy to make again after a scam.", "joined"),
             ("3", "No reviews", "No one has bought from them.", "reviews"),
             ("4", "Pay first, by e-wallet", "Once it's sent, it's gone.", "pay")]
    for i, (n, title, body, row) in enumerate(flags):
        badge(378, rows[row], RED, n)
        y = 176 + i * 92
        badge(122, y + 12, RED, n)
        p.write((142, y), title, 20, RED, anchor="la", face=BOLD)
        p.write((142, y + 28), body, 16, BODY, anchor="la")

    p.write((815, 344), "vs", 26, DIM, anchor="mm", face=BOLD)

    # The trusted listing and what makes it safe.
    listing(900, "₱1,150", None, "Official store", "Selling since 2019", "", "4.9 · 12,000 reviews",
            "Pay at checkout or on delivery", False)
    oks = [("A normal price", "Close to what it costs elsewhere.", "price"),
           ("Years of good reviews", "Thousands bought, and liked it.", "reviews"),
           ("Pay through checkout", "You can get a refund if it fails.", "pay")]
    for i, (title, body, row) in enumerate(oks):
        badge(1252, rows[row], GREEN)
        y = rows[row] - 14 + (0, -28, 6)[i]  # beside its tick, spread so the notes don't touch
        p.write((1276, y), title, 20, GREEN, anchor="la", face=BOLD)
        p.write((1276, y + 28), body, 16, BODY, anchor="la")

    # Where each one ends.
    p.arrow((565, 534), (565, 572), 0, RED)
    p.box(400, 580, 730, 690)
    p.box(470, 596, 714, 636, 14, CHALK, 2.0)
    p.write((484, 616), "Sent ₱99. When will it ship?", 16, anchor="lm")
    p.write((714, 656), "Seen", 15, DIM, anchor="rm")
    p.write((416, 674), "...no reply", 15, DIM, anchor="lm")
    p.note((400, 712), "Then the chat goes quiet", "The calculator never comes.", RED, 330)

    p.arrow((1065, 534), (1065, 572), 0, GREEN)
    p.box(900, 580, 1230, 690)
    p.write((950, 635), "", 40, GREEN, anchor="mm", face=ICONS)
    p.write((990, 620), "Delivered", 21, anchor="lm", face=BOLD)
    p.write((990, 648), "Tracked through checkout", 16, DIM, anchor="lm")
    p.note((900, 712), "It arrives", "And if it hadn't, checkout could refund you.", GREEN, 400)


def laptop_icon(p, cx, cy, colour=CHALK):
    p.box(cx - 20, cy - 16, cx + 20, cy + 8, 3, colour, 2.0)
    p.stroke([(cx - 27, cy + 13), (cx + 27, cy + 13)], colour, 2.2, 0)


def drive_icon(p, cx, cy, colour=CHALK):
    p.box(cx - 9, cy - 10, cx + 9, cy + 16, 3, colour, 2.0)
    p.box(cx - 5, cy - 18, cx + 5, cy - 10, 1, colour, 1.8)


def mark(p, cx, cy, ok):
    if ok:
        p.stroke([(cx - 14, cy), (cx - 4, cy + 10), (cx + 16, cy - 12)], GREEN, 3.4, 0.3)
    else:
        p.stroke([(cx - 12, cy - 12), (cx + 12, cy + 12)], RED, 3.4, 0.3)
        p.stroke([(cx - 12, cy + 12), (cx + 12, cy - 12)], RED, 3.4, 0.3)


def backup_flow(p):
    # A table: three accidents down the side, how many copies you keep across the top, and what survives.
    cols = (655, 1005, 1350)
    # Column heads.
    laptop_icon(p, cols[0], 180)
    laptop_icon(p, cols[1] - 22, 180)
    drive_icon(p, cols[1] + 26, 178)
    laptop_icon(p, cols[2] - 44, 180)
    drive_icon(p, cols[2] + 2, 178)
    p.write((cols[2] + 42, 180), "", 26, CHALK, anchor="mm", face=ICONS)
    for x, head, sub in zip(cols, ("One copy", "Two copies", "Two copies, two places"),
                            ("on the laptop", "laptop + flash drive", "+ a cloud drive")):
        p.write((x, 224), head, 20, anchor="mm", face=BOLD)
        p.write((x, 250), sub, 16, DIM, anchor="mm")
    # Rows.
    rows = (("Laptop breaks", "or gets stolen", (False, True, True)),
            ("Ransomware locks it", "and a drive left plugged in", (False, True, True)),
            ("Bag stolen", "laptop and flash drive together", (False, False, True)))
    for i, (what, more, results) in enumerate(rows):
        y = 330 + i * 100
        p.stroke([(110, y - 50), (1510, y - 50)], (44, 48, 54), 1.4, 0)
        p.write((120, y - 12), what, 21, anchor="lm", face=BOLD)
        p.write((120, y + 16), more, 16, DIM, anchor="lm")
        for x, ok in zip(cols, results):
            mark(p, x, y, ok)
    p.write((cols[1], 450), "if you unplugged it", 15, DIM, anchor="mm")
    p.stroke([(110, 580), (1510, 580)], (44, 48, 54), 1.4, 0)
    p.ring((cols[2] - 160, 140, cols[2] + 160, 572), GREEN, 16)

    p.note((110, 616), "One copy is one accident away",
           "A broken laptop, a locked screen or a lost bag, and the work is gone.", RED, 660)
    p.note((850, 616), "Two copies, two places",
           "One on a flash drive you unplug, one in the cloud. No single accident reaches both.", GREEN, 660)


def mfa_flow(p):
    # One lock, two keys: they bring the stolen password (left), only your phone has the code (right).
    p.hacker(230, 160, "", ICONS)
    p.note((110, 470), "They have your password",
           "From a leak or a fake page. With a password only, that's all they need.", RED, 330)

    # The account, with its two locks.
    p.box(560, 160, 1040, 560)
    p.write((600, 196), "", 20, CHALK, anchor="mm", face=ICONS)
    p.write((622, 196), "Your account", 22, anchor="lm", face=BOLD)
    p.write((592, 238), "1   Password", 17, DIM, anchor="lm")
    p.box(592, 268, 1008, 312, 10, CHALK, 2.0)
    p.write((608, 290), "• • • • • • • •", 20, anchor="lm")
    p.write((994, 290), "stolen", 15, RED, anchor="rm", face=BOLD)
    p.ring((582, 260, 1018, 320), RED)
    p.write((592, 352), "2   Code from your phone", 17, DIM, anchor="lm")
    p.box(592, 382, 1008, 426, 10, CHALK, 2.0)
    p.write((608, 404), "_  _  _    _  _  _", 20, DIM, anchor="lm")
    p.ring((582, 374, 1018, 434), GREEN)
    p.lock(722, 500, 1.3, GREEN)
    p.write((744, 502), "Still locked to them", 22, GREEN, anchor="lm", face=BOLD)

    # Their way in: the password fits, the code doesn't.
    p.arrow((350, 300), (574, 290), -20, RED)
    p.write((462, 266), "stolen password", 17, RED, anchor="mm", face=BOLD)
    p.stroke([(350, 360), (520, 404)], DIM, 2.0)
    p.stroke([(522, 394), (542, 414)], RED, 3.0, 0)
    p.stroke([(522, 414), (542, 394)], RED, 3.0, 0)
    p.write((440, 412), "no code", 16, DIM, anchor="mm")

    # Your phone: the only place the code is.
    p.box(1190, 190, 1340, 470, 22)
    p.box(1238, 204, 1292, 214, 4, DIM, 1.4)
    p.write((1265, 262), "Authenticator", 15, DIM, anchor="mm")
    p.write((1265, 312), "482 913", 30, GREEN, anchor="mm", face=BOLD)
    p.arc(1265, 372, 16, 16, -90, 200, GREEN, 2.4, 0.2)
    p.write((1265, 410), "new in 28s", 14, DIM, anchor="mm")
    p.arrow((1182, 380), (1026, 404), -20, GREEN)
    p.write((1110, 360), "only you", 17, GREEN, anchor="mm", face=BOLD)
    p.note((1180, 500), "Only your phone has the code",
           "It changes every 30 seconds, so a stolen one is soon useless.", GREEN, 330)

    p.note((560, 600), "Turn it on for school, email and bank",
           "Accounts with 2FA are over 99.9% less likely to be broken into.", GREEN, 580)


PAGES = {
    # threat id: (sample file, title, source, flow)
    "evil_twin": ("sample_details_wifi", "Fake café Wi-Fi", "FTC, “Are Public Wi-Fi Networks Safe?” (2023)", wifi_flow),
    "friend_scam": ("sample_details_friend", "A friend asks for money", "FTC, “How to Recover Your Hacked Email or Social Media Account”", friend_flow),
    "login_alert": ("sample_details_login", "A sign-in you didn't start",
                    "CISA, “Implementing Number Matching in MFA Applications” (2022)", login_flow),
    "fake_update": ("sample_details_update", "Fake browser update", "CISA, Secure Our World: “Update Software”", update_flow),
    "ransomware": ("sample_details_ransom", "Files locked for ransom", "CISA, #StopRansomware Guide (2023)", ransom_flow),
    "password_reuse": ("sample_details_reuse", "Reused password", "CISA, Secure Our World: “Use Strong Passwords”", reuse_flow),
    "online_selling_scam": ("sample_details_seller", "Too-good-to-be-true seller", "FTC, “Buying From an Online Marketplace”", seller_flow),
    "backup": ("sample_details_backup", "Back up your files", "CISA, #StopRansomware Guide (2023)", backup_flow),
    "mfa": ("sample_details_mfa", "Two-factor authentication", "CISA, Secure Our World: “Turn On MFA”; Microsoft Security Blog (2019)", mfa_flow),
}


def details_page(name, title, source, flow):
    """Whole-page mock-up, as the game shows it."""
    p = Sketch()
    p.write((80, 48), title, 46, face=BOLD)
    p.write((82, 112), "How the trick works", 24, DIM)
    flow(p)
    p.write((80, 860), "Source: " + source, 17, DIM, anchor="lm")
    p.write((1520, 860), "CLOSE", 22, CHALK, anchor="rm", face=BOLD)
    return p.save(os.path.join(OUT, f"{name}.png"))


FLOW = (90, 120, 1530, 790)   # the part of the page the flow fills (1x)
SPRITES = os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))), "Assets", "Sprites", "Rundown")


def export(threat, draw):
    """The flow alone, transparent, at 1.4x for a sharp picture on a 1920-wide screen."""
    p = Sketch(transparent=True)
    draw(p)
    img = p.img.crop(tuple(v * SS for v in FLOW))
    img = img.resize((int(img.width * 1.4 / SS), int(img.height * 1.4 / SS)), Image.LANCZOS)
    path = os.path.join(SPRITES, f"{threat}_details.png")
    img.save(path)
    return path


if __name__ == "__main__":
    for threat, (name, title, source, flow) in PAGES.items():
        print(export(threat, flow))
        print(details_page(name, title, source, flow))
