# Draws Assets/Sprites/Hotbar/news_picture.png (phone News app header, 500x220): night city, warning sign,
# open lock, and the Ghostline hacker: the user's sample (hacker_sample.png) cut out of its white background,
# eyes turned glowing red. Run: python make_news_picture.py
import random
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter

W, H, S = 500, 220, 4  # drawn at 4x, scaled down for smooth edges
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets/Sprites/Hotbar/news_picture.png"

BG = (22, 28, 52)
FAR, NEAR = (30, 37, 66), (40, 50, 88)
LIT, UNLIT = (253, 212, 102), (52, 63, 104)
SAMPLE = Path(__file__).with_name("hacker_sample.png")
SAMPLE_BOX = (276, 196, 724, 792)  # the figure, above the sample's bottom bar
LAPTOP = (69, 402, 364, 596)       # the laptop inside that box (its edges stay solid)
EYES = (150, 160, 275, 190)        # area around both eyes inside that box
EYE_RED = (255, 40, 40)
SCALE = 0.38


def s(points):
    return [(x * S, y * S) for x, y in points]


def bezier(p0, c0, c1, p1, n=24):
    out = []
    for i in range(1, n + 1):
        t = i / n
        u = 1 - t
        out.append((u**3 * p0[0] + 3 * u * u * t * c0[0] + 3 * u * t * t * c1[0] + t**3 * p1[0],
                    u**3 * p0[1] + 3 * u * u * t * c0[1] + 3 * u * t * t * c1[1] + t**3 * p1[1]))
    return out


def path(start, *curves):
    pts = [start]
    for c0, c1, p1 in curves:
        pts += bezier(pts[-1], c0, c1, p1)
    return pts


def glow(size, draw_fn, radius):
    layer = Image.new("RGBA", size, (0, 0, 0, 0))
    draw_fn(ImageDraw.Draw(layer))
    return layer.filter(ImageFilter.GaussianBlur(radius))


def hacker():
    """The sample figure on a transparent background, red glowing eyes, scaled for the picture."""
    src = Image.open(SAMPLE).convert("RGB").crop(SAMPLE_BOX)
    w, h = src.size
    px = src.load()
    light = lambda c: min(c) >= 246
    # Background = white connected to the border (eye whites and the laptop glow are enclosed, so they stay).
    bg = set()
    todo = [(x, y) for x in range(w) for y in (0, h - 1)] + [(x, y) for y in range(h) for x in (0, w - 1)]
    while todo:
        x, y = todo.pop()
        if (x, y) in bg or not (0 <= x < w and 0 <= y < h) or not light(px[x, y]):
            continue
        bg.add((x, y))
        todo += [(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)]
    out = Image.new("RGBA", (w, h))
    o = out.load()
    eye_mask = Image.new("L", (w, h))
    e = eye_mask.load()
    for y in range(h):
        for x in range(w):
            if (x, y) in bg:
                continue
            r, g, b = px[x, y]
            a = 255
            in_laptop = LAPTOP[0] <= x <= LAPTOP[2] and LAPTOP[1] <= y <= LAPTOP[3]
            edge = any(n in bg for n in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)))
            if edge and not in_laptop:  # anti-aliased edge against white: un-mix the white
                a = max(1, min(255, round((255 - (r + g + b) / 3) / (255 - 45) * 255)))
                k = 255 / a
                r, g, b = (max(0, min(255, round(255 - (255 - c) * k))) for c in (r, g, b))
            if EYES[0] <= x <= EYES[2] and EYES[1] <= y <= EYES[3] and max(r, g, b) - min(r, g, b) < 30:
                t = max(0.0, min(1.0, ((r + g + b) / 3 - 60) / 180))  # how much of the eye white this pixel is
                if t > 0:
                    e[x, y] = round(t * 255)
                    hot = max(0.0, (t - 0.85) / 0.15) * 0.45  # slightly hot centre
                    r, g, b = (round(c * (1 - t) + (er + (255 - er) * hot) * t) for c, er in zip((r, g, b), EYE_RED))
            o[x, y] = (r, g, b, a)
    # Red glow around the eyes.
    glow = Image.new("RGBA", (w, h), EYE_RED + (0,))
    glow.putalpha(eye_mask.filter(ImageFilter.GaussianBlur(6)).point(lambda v: min(200, round(v * 1.6))))
    out.alpha_composite(glow)
    size = (round(w * SCALE), round(h * SCALE))
    return out.convert("RGBa").resize(size, Image.LANCZOS).convert("RGBA")


img = Image.new("RGBA", (W * S, H * S), BG + (255,))
d = ImageDraw.Draw(img)

# City: a far row and a near row of buildings with lit windows.
rnd = random.Random(7)
for color, rows in ((FAR, [(-4, 104, 40), (62, 128, 50), (116, 146, 44), (322, 132, 48), (376, 96, 44), (436, 116, 70)]),
                    (NEAR, [(-8, 138, 50), (38, 158, 48), (92, 176, 40), (346, 160, 46), (414, 136, 46), (458, 164, 50)])):
    for x, top, w in rows:
        d.rectangle(s([(x, top), (x + w, H)]), fill=color)
        for wy in range(top + 8, H - 4, 14):
            for wx in range(x + 7, x + w - 8, 12):
                on = rnd.random() < (0.45 if color == NEAR else 0.25)
                d.rectangle(s([(wx, wy), (wx + 5, wy + 6)]), fill=LIT if on else (UNLIT if color == NEAR else (40, 48, 82)))

# Warning sign (left) and open padlock (right).
tri = [(60, 64), (100, 134), (20, 134)]
d.polygon(s(tri), fill=(251, 193, 60))
d.line(s(tri + tri[:2]), fill=(251, 193, 60), width=10 * S // 2, joint="curve")
d.rounded_rectangle(s([(56, 86), (64, 116)]), radius=4 * S, fill=BG)
d.ellipse(s([(55.5, 120), (64.5, 129)]), fill=BG)
d.arc(s([(414, 22), (446, 54)]), 180, 360, fill=(255, 90, 90), width=8 * S)  # shackle, raised on the right = open
d.rectangle(s([(414, 37), (422, 62)]), fill=(255, 90, 90))
d.rectangle(s([(438, 37), (446, 46)]), fill=(255, 90, 90))
d.rounded_rectangle(s([(400, 60), (456, 106)]), radius=8 * S, fill=(255, 90, 90))
d.ellipse(s([(422, 72), (434, 84)]), fill=BG)
d.rectangle(s([(426, 80), (430, 94)]), fill=BG)

# Soft screen light behind the hacker.
img.alpha_composite(glow(img.size, lambda g: g.ellipse(s([(170, 110), (330, 230)]), fill=(70, 110, 170, 120)), 30 * S))

img = img.resize((W, H), Image.LANCZOS)
img.alpha_composite(hacker(), (W // 2 - 85, 10))
img.save(OUT)
print("wrote", OUT)
