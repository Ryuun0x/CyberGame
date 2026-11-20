"""The rundown's map page: the part of the city between home and the café, in the rundown's dark style, with the
walk from home to the café traced along the streets. Pins and labels are added by the game (DebriefScreen), so
they can show each threat's outcome; their positions come from the same crop as here (MAP_MIN / MAP_MAX).

Input: Assets/Resources/CityMapArtwork.png and CityMapLayout.json (the game's city map).
Output: Assets/Sprites/Rundown/map.png, and samples/sample_map.png (with sample pins, for review).
"""
import json
import os
from collections import deque

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

from make_samples import BG, GREEN, RED, ROOT, font

ART = os.path.join(ROOT, "Assets", "Resources", "CityMapArtwork.png")
LAYOUT = os.path.join(ROOT, "Assets", "Resources", "CityMapLayout.json")
SPRITE = os.path.join(ROOT, "Assets", "Sprites", "Rundown", "map.png")
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "samples")

# The crop in world units (x, z): keep in step with DebriefScreen.MapMin / MapMax.
MAP_MIN, MAP_MAX = (-180.0, 35.0), (15.0, 112.0)
OUT_W = 1800                     # sprite width; height follows the crop
HOME, CAFE = (-160.574, 61.988), (-2.57, 81.11)


def load():
    art = np.asarray(Image.open(ART).convert("RGB")).astype(np.int16)
    with open(LAYOUT, encoding="utf-8") as f:
        lay = json.load(f)
    return art, (lay["min"]["x"], lay["min"]["y"]), (lay["max"]["x"], lay["max"]["y"])


def to_pixel(x, z, mn, mx, w, h):
    return (x - mn[0]) / (mx[0] - mn[0]) * w, (1 - (z - mn[1]) / (mx[1] - mn[1])) * h


def route(art, mn, mx, step=4):
    """Shortest walk over road pixels from home to the café (breadth-first on a 1/step grid), in art pixels."""
    a = art[::step, ::step]
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    grey = (np.abs(r - g) < 7) & (np.abs(g - b) < 7)
    road = grey & (((r > 84) & (r < 110)) | (r > 215))  # asphalt and the white crosswalk stripes
    for _ in range(2):  # close the gaps between stripes and lane marks
        grown = road.copy()
        grown[1:] |= road[:-1]
        grown[:-1] |= road[1:]
        grown[:, 1:] |= road[:, :-1]
        grown[:, :-1] |= road[:, 1:]
        road = grown
    h, w = road.shape

    def nearest(x, z):
        u, v = to_pixel(x, z, mn, mx, w, h)
        ys, xs = np.nonzero(road)
        i = np.argmin((xs - u) ** 2 + (ys - v) ** 2)
        return ys[i], xs[i]

    start, goal = nearest(*HOME), nearest(*CAFE)
    prev = {start: None}
    q = deque([start])
    while q:
        y, x = q.popleft()
        if (y, x) == goal:
            break
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            n = (y + dy, x + dx)
            if 0 <= n[0] < h and 0 <= n[1] < w and road[n] and n not in prev:
                prev[n] = (y, x)
                q.append(n)
    if goal not in prev:
        raise RuntimeError("no road from home to the café in the map artwork")
    path, n = [], goal
    while n is not None:
        path.append((n[1] * step + step / 2, n[0] * step + step / 2))
        n = prev[n]
    return simplify(path[::-1], 6 * step)


def simplify(pts, tol):
    """Ramer-Douglas-Peucker: keep only the corners of the walk."""
    if len(pts) < 3:
        return pts
    a, b = np.array(pts[0]), np.array(pts[-1])
    d = [abs((b - a)[0] * (np.array(p) - a)[1] - (b - a)[1] * (np.array(p) - a)[0]) / (np.linalg.norm(b - a) + 1e-9) for p in pts[1:-1]]
    i = int(np.argmax(d)) + 1
    if d[i - 1] < tol:
        return [pts[0], pts[-1]]
    return simplify(pts[:i + 1], tol)[:-1] + simplify(pts[i:], tol)


def restyle(crop):
    """The city in the rundown's colours: grass becomes the page, roads a little lighter, sidewalks and buildings
    in between, so the streets read without any bright colour."""
    c = crop.astype(np.float32)
    r, g, b = c[..., 0], c[..., 1], c[..., 2]
    lum = (0.299 * r + 0.587 * g + 0.114 * b) / 255
    sat = (c.max(-1) - c.min(-1)) / 255
    grass = (g > r + 25) & (g > b + 40)
    road = (sat < 0.03) & (lum > 0.33) & (lum < 0.43)
    bg = np.array(BG, np.float32)
    out = bg + (np.array((40, 43, 50), np.float32) - bg) * np.clip(lum * 1.15, 0, 1)[..., None]   # buildings
    out[road] = (56, 61, 70)
    out[grass] = bg + 4
    walk = (sat < 0.05) & (lum > 0.52)                                                              # sidewalks
    out[walk & ~grass] = (47, 51, 59)
    img = Image.fromarray(np.clip(out, 0, 255).astype(np.uint8))
    return img.filter(ImageFilter.GaussianBlur(0.6))


def build():
    art, mn, mx = load()
    H, W = art.shape[:2]
    x0, y1 = to_pixel(MAP_MIN[0], MAP_MIN[1], mn, mx, W, H)
    x1, y0 = to_pixel(MAP_MAX[0], MAP_MAX[1], mn, mx, W, H)
    box = tuple(int(v) for v in (x0, y0, x1, y1))
    crop = art[box[1]:box[3], box[0]:box[2]]
    scale = OUT_W / crop.shape[1]
    img = restyle(crop).resize((OUT_W, int(crop.shape[0] * scale)), Image.LANCZOS).convert("RGBA")

    # The walk, as a dashed chalk line.
    SS = 2
    ink = Image.new("RGBA", (img.width * SS, img.height * SS), (0, 0, 0, 0))
    d = ImageDraw.Draw(ink)
    pts = [((px - box[0]) * scale * SS, (py - box[1]) * scale * SS) for px, py in route(art, mn, mx)]
    dash, gap, carry = 16 * SS, 12 * SS, 0.0
    for (ax, ay), (bx, by) in zip(pts, pts[1:]):
        seg = np.hypot(bx - ax, by - ay)
        t = -carry
        while t < seg:
            s, e = max(t, 0), min(t + dash, seg)
            if e > s:
                d.line([(ax + (bx - ax) * s / seg, ay + (by - ay) * s / seg), (ax + (bx - ax) * e / seg, ay + (by - ay) * e / seg)],
                       fill=(226, 226, 220, 210), width=4 * SS)
            t += dash + gap
        carry = t - seg
    img.alpha_composite(ink.resize(img.size, Image.LANCZOS))
    # Fade the edges into the page instead of a hard rectangle.
    fade = 70
    ys, xs = np.mgrid[0:img.height, 0:img.width]
    edge = np.minimum(np.minimum(xs, img.width - 1 - xs), np.minimum(ys, img.height - 1 - ys))
    img.putalpha(Image.fromarray((np.clip(edge / fade, 0, 1) ** 1.5 * 255).astype(np.uint8)))
    img.save(SPRITE)
    return img


def sample(img):
    """The map with pins as the game will draw them, for review."""
    page = Image.new("RGB", (1600, 900), tuple(int(c) for c in BG))
    d = ImageDraw.Draw(page)
    d.text((80, 60), "Where it happened", font=font("C:/Windows/Fonts/segoeui.ttf", 34), fill=(232, 232, 232))
    m = img.convert("RGB").resize((1440, int(img.height * 1440 / img.width)), Image.LANCZOS)
    page.paste(m, (80, 130))

    def at(x, z):
        return 80 + (x - MAP_MIN[0]) / (MAP_MAX[0] - MAP_MIN[0]) * m.width, 130 + (1 - (z - MAP_MIN[1]) / (MAP_MAX[1] - MAP_MIN[1])) * m.height

    f, fb = font("C:/Windows/Fonts/segoeui.ttf", 18), font("C:/Windows/Fonts/seguisb.ttf", 20)
    for (x, z), name, items in ((HOME, "Home", [("1", "Back up your files", GREEN), ("4", "Two-factor authentication", RED)]),
                               (CAFE, "Caf\u00e9", [("2", "Fake caf\u00e9 Wi-Fi", RED), ("3", "Fake browser update", (227, 179, 65))])):
        px, py = at(x, z)
        d.ellipse((px - 10, py - 10, px + 10, py + 10), fill=(232, 232, 232))
        bx, by = px + 22, py - 30
        d.rounded_rectangle((bx, by, bx + 300, by + 40 + 30 * len(items)), 12, fill=(24, 27, 32), outline=(70, 75, 84), width=2)
        d.text((bx + 16, by + 10), name, font=fb, fill=(232, 232, 232))
        for i, (n, t, c) in enumerate(items):
            d.ellipse((bx + 16, by + 46 + i * 30, bx + 34, by + 64 + i * 30), outline=c, width=2)
            d.text((bx + 25, by + 55 + i * 30), n, font=font("C:/Windows/Fonts/seguisb.ttf", 12), fill=c, anchor="mm")
            d.text((bx + 44, by + 55 + i * 30), t, font=f, fill=c, anchor="lm")
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, "sample_map.png")
    page.save(path)
    return path


if __name__ == "__main__":
    print(sample(build()))
