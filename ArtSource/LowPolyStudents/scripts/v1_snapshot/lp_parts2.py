"""Hair, backpack, printed graphics, uniform accessories (exec'd after lp_parts.py)."""

# ================================================================== HAIR
def hair_pt(P, th, ph, lift):
    p = head_pt(P, th, ph); return P['head_c'] + p + p.normalized() * lift

def hairline_fn(sex):
    pts = ([(0, 32), (36, 27), (54, 16), (72, 10), (90, 9), (108, -4), (140, -26), (180, -34)] if sex == 'M' else
           [(0, 29), (30, 25), (50, 13), (70, 4), (90, 0), (110, -12), (140, -34), (180, -46)])
    return lambda th: interp(pts, abs(th))

def build_hair_cap(P, liftfn, rows=8, jit=0.004, seed=1):
    mb = MB(); mb.wfn = fixed('head'); rnd = random.Random(seed)
    hl = hairline_fn(P['sex']); n = HEAD_NLON
    ths = [-180 + 360 * i / n for i in range(n)]
    R = []
    for r in range(rows):
        pts = []
        for th in ths:
            ph = hl(th) if r == 0 else lerp(hl(th), 84, (r / (rows - 1)) ** 0.9)
            lift = -0.002 if r == 0 else liftfn(th, ph) * (0.6 if r == 1 else 1.0) + rnd.uniform(-jit, jit)
            pts.append(hair_pt(P, th, ph, lift))
        R.append(mb.ring(pts))
    for a, b in zip(R, R[1:]): mb.bridge(a, b, 'Hair')
    mb.cap(R[-1], 'Hair', end=True, center=hair_pt(P, 0, 90, liftfn(0, 90)))
    return mb

def lock(mb, P, ctrl, width, thick, n=6, mat='Hair', bury=0.35):
    """Faceted hair lock: triangular cross-section, ridge outward, tapering to a point."""
    pts = resample([hair_pt(P, *c) for c in ctrl], n)
    C = P['head_c']; rings = []
    for i, p in enumerate(pts[:-1]):
        t = (pts[i + 1] - pts[max(i - 1, 0)]).normalized()
        nr = (p - C).normalized(); nr = (nr - t * nr.dot(t)).normalized(); sd = nr.cross(t)
        f = i / (n - 1)
        wv = width * (1 - f ** 1.7) * (0.85 + 0.3 * math.sin(math.pi * min(1.0, f * 1.6)))
        tv = thick * (1 - 0.6 * f)
        rings.append(mb.ring([p + sd * (wv / 2) - nr * (thick * bury), p + nr * tv, p - sd * (wv / 2) - nr * (thick * bury)]))
    for a, b in zip(rings, rings[1:]): mb.bridge(a, b, mat)
    mb.cap(rings[0], mat)
    mb.cap(rings[-1], mat, end=True, center=pts[-1])

def build_hair_male(P):
    """Messy swept crop: volume on top, fringe falling to his right, tufts over the ears, layered back."""
    def lift(th, ph):
        g = math.exp(-(th / 60) ** 2 - ((ph - 52) / 26) ** 2)
        side = 0.007 * ss(40, 70, abs(th)) * ss(-10, 25, ph) * ss(160, 110, abs(th))
        return 0.009 + 0.008 * ss(15, 65, ph) + 0.009 * g + side * 0.3 + 0.002 * ss(110, 170, abs(th)) * ss(-20, 20, ph)
    mb = build_hair_cap(P, lift, jit=0.005, seed=3)
    L = []
    # front fringe: swept from the crown forward, tips falling over his right brow
    L += [([(12, 84, .03), (2, 66, .05), (-10, 48, .045), (-19, 33, .025), (-24, 21, .012)], .075, .032),
          ([(32, 80, .03), (22, 62, .05), (10, 45, .042), (1, 31, .022), (-3, 22, .011)], .068, .03),
          ([(-22, 82, .03), (-32, 64, .046), (-40, 48, .036), (-46, 34, .02), (-49, 25, .01)], .062, .028),
          ([(50, 74, .03), (44, 58, .044), (35, 44, .034), (28, 32, .016)], .06, .028),
          ([(4, 72, .045), (16, 64, .07), (30, 58, .066), (44, 56, .04), (54, 56, .016)], .062, .03),   # raised quiff
          ([(-6, 76, .045), (-14, 70, .066), (-24, 64, .06), (-34, 60, .03)], .055, .028)]
    # top layer
    L += [([(130, 82, .03), (70, 82, .05), (38, 74, .055), (24, 70, .03)], .075, .03),
          ([(-130, 82, .03), (-70, 82, .05), (-42, 74, .05), (-28, 70, .028)], .072, .03)]
    # sides: tufts pointing down/back over the top of the ears
    for sg in (1, -1):
        L += [([(sg * 58, 64, .035), (sg * 70, 46, .036), (sg * 78, 28, .027), (sg * 84, 13, .014)], .062, .028),
              ([(sg * 82, 68, .032), (sg * 96, 46, .033), (sg * 104, 26, .026), (sg * 108, 10, .012)], .06, .028),
              ([(sg * 104, 66, .03), (sg * 118, 42, .03), (sg * 126, 20, .022), (sg * 130, 4, .01)], .06, .026)]
    # back: two layers of tufts down to the nape
    for th in (138, 160, 180, -160, -138):
        d = 6 if th > 0 else -6
        L += [([(th, 66, .03), (th + d * 0.5, 42, .034), (th + d, 18, .028), (th + d * 1.5, -4, .017), (th + d * 1.6, -22, .008)], .07, .028)]
    for th in (148, 170, -170, -148):
        L += [([(th, 20, .02), (th, 0, .022), (th + (3 if th > 0 else -3), -20, .013), (th, -33, .005)], .055, .022)]
    # crown swirl
    L += [([(160, 84, .03), (178, 70, .036), (-165, 58, .03), (-150, 50, .016)], .06, .026),
          ([(170, 84, .028), (150, 72, .034), (130, 62, .028), (118, 56, .014)], .058, .026)]
    for ctrl, w, t in L:
        k = 0.6 if abs(ctrl[0][0]) < 55 else 0.42      # fringe/top keep volume, sides and back hug the head
        lock(mb, P, [(a, b, c * k) for a, b, c in ctrl], w * 0.85, t * 0.7, n=5)
    return mb

def build_hair_female(P, J):
    def lift(th, ph):
        part = 0.004 * math.exp(-((th + 12) / 7) ** 2) * ss(40, 60, ph)
        return 0.011 + 0.006 * ss(30, 70, ph) + 0.003 * ss(100, 160, abs(th)) - part
    mb = build_hair_cap(P, lift, jit=0.0025, seed=7)
    L = []
    for sg in (1, -1):
        L += [([(sg * 8, 66, .016), (sg * 28, 46, .018), (sg * 46, 22, .017), (sg * 56, -4, .014), (sg * 58, -30, .012), (sg * 56, -46, .006)], .036, .016),
              ([(sg * 30, 60, .016), (sg * 52, 38, .018), (sg * 66, 10, .016), (sg * 70, -20, .012), (sg * 70, -38, .006)], .04, .016),
              ([(sg * 4, 72, .016), (sg * 24, 58, .02), (sg * 48, 44, .018), (sg * 64, 34, .01)], .05, .014),
              ([(sg * 80, 60, .014), (sg * 110, 40, .014), (sg * 140, 10, .012), (sg * 165, -14, .006)], .05, .014)]
    for ctrl, w, t in L: lock(mb, P, ctrl, w, t)
    # ponytail
    pp = pony_points(P); J['ponytail.01'] = (pp[0], pp[2]); J['ponytail.02'] = (pp[2], pp[4])
    mb.wfn = wfun(J, [('head', .03, None), ('ponytail.01', .04, None), ('ponytail.02', .04, None)], power=6)
    pts = pony_points(P)
    ch = polyline(pts); rnd = random.Random(11)
    tube(mb, ch, [(0.0, .03, .03), (0.075, .027, .027), (0.2, .039, .037), (0.42, .037, .034), (0.7, .028, .026), (0.9, .015, .014)],
         6, 'Hair', ref=V((1, 0, 0)), cap0=True, cap1=True, jit=lambda i, s: rnd.uniform(-0.12, 0.12) * (s > 0.1))
    tube(mb, ch, [(0.045, .032, .032), (0.105, .032, .032)], 8, 'Hair_Tie', ref=V((1, 0, 0)))
    mb.wfn = fixed('head')
    return mb

def pony_points(P):
    b = hair_pt(P, 180, -24, 0.004)
    return [b, b + V((0, .038, -.022)), b + V((0, .058, -.075)), b + V((0, .052, -.14)), b + V((0, .036, -.2))]

# ================================================================== PRINTED / FLAT GRAPHICS
def skull_polys(k):
    # cranium, cheeks, then a jagged dripping lower edge (teeth/drips), slightly asymmetric
    right = [(.045, .1), (.075, .086), (.095, .062), (.103, .03), (.1, 0), (.09, -.026), (.076, -.04), (.071, -.056), (.069, -.078),
             (.06, -.08), (.056, -.112), (.048, -.082), (.04, -.097), (.033, -.082), (.025, -.128), (.017, -.084), (.008, -.102)]
    left = [(-.006, -.086), (-.014, -.118), (-.021, -.085), (-.029, -.1), (-.037, -.083), (-.046, -.122), (-.054, -.082),
            (-.06, -.096), (-.068, -.079), (-.071, -.056), (-.076, -.04), (-.09, -.026), (-.1, 0), (-.103, .03), (-.095, .062),
            (-.075, .086), (-.045, .1)]
    out = [(0, .105)] + right + left
    eyeL = [(.013, .022), (.072, .036), (.08, .002), (.052, -.028), (.02, -.02)]
    eyeR = [(-x, y) for x, y in reversed(eyeL)]
    nose = [(0, -.03), (.015, -.05), (.006, -.059), (0, -.051), (-.006, -.059), (-.015, -.05)]
    return [[(x * k, y * k) for x, y in p] for p in (out, eyeL, eyeR, nose)]

def shield(k, inset=1.0):
    o = [(-.018, .022), (.018, .022), (.018, -.002), (.012, -.016), (0, -.025), (-.012, -.016), (-.018, -.002)]
    return [(x * k * inset, y * k * inset) for x, y in o]

def offset2(poly, cx, cz): return [(x + cx, y + cz) for x, y in poly]

# ================================================================== BACKPACK
def back_y(bvh, z, xs=(0.0, 0.07, -0.07)):
    best = -1
    for x in xs:
        loc, n, _, _ = bvh.ray_cast(V((x, 1.0, z)), V((0, -1, 0)))
        if loc is not None: best = max(best, loc.y)
    return best

def build_backpack(P, J, torso_bvh):
    fem = P['sex'] == 'F'; s = P['s'] * (0.94 if fem else 1.0)
    mb = MB(); mb.wfn = fixed('chest')
    sz = P['shoulder'].z; zt = sz + 0.008; zb = sz - 0.40 * s; H = zt - zb
    prof = [(0.0, .132, .05), (0.04, .147, .062), (0.5, .152, .07), (0.8, .146, .068), (0.92, .127, .06), (0.98, .092, .045)]
    R = []; yfs = []
    for h, hw, hd in prof:
        z = zb + h * H
        yf = back_y(torso_bvh, min(z, sz - 0.02)) + 0.006
        yfs.append(yf)
        c = V((0, yf + hd * s, z))
        R.append(mb.ring([c + V((hw * s * se(math.cos(2 * math.pi * i / 12), 0.6), hd * s * se(math.sin(2 * math.pi * i / 12), 0.6), 0)) for i in range(12)]))
    for a, b in zip(R, R[1:]): mb.bridge(a, b, 'Backpack')
    mb.cap(R[0], 'Backpack', center=V((0, yfs[0] + prof[0][2] * s, zb - 0.004)))
    top = V((0, yfs[-1] + prof[-1][2] * s, zt + 0.006))
    mb.cap(R[-1], 'Backpack', end=True, center=top)
    # front pocket (outer face)
    pz = zb + 0.3 * H; yo = yfs[2] + 2 * prof[2][2] * s - 0.012
    ch = lambda t: V((0, yo + t, pz))
    tube(mb, ch, [(0.0, .088 * s, .11 * s), (0.032, .088 * s, .11 * s), (0.046, .08 * s, .102 * s)], 10, 'Backpack', ref=V((0, 0, 1)), cap0=False, cap1=True, e=0.55)
    yp = yo + 0.046
    X, Y, Z = V((1, 0, 0)), V((0, 1, 0)), V((0, 0, 1))
    if not fem:
        box(mb, V((0, yp - 0.006, pz + 0.062 * s)), X, Y, Z, 0.098 * s, 0.007, 0.0035, 'Backpack_Hardware')
        box(mb, V((0.06 * s, yp, pz + 0.05 * s)), X, Y, Z, 0.006, 0.003, 0.012, 'Backpack_Hardware')
        # side compression strap accents
        for sg in (1, -1):
            box(mb, V((sg * 0.152 * s, yfs[2] + prof[2][2] * s, zb + 0.55 * H)), X, Y, Z, 0.004, 0.03, 0.012, 'Backpack_Strap')
    for sg in (1, -1):   # side pockets
        sc = V((sg * 0.148 * s, yfs[1] + prof[1][2] * s + 0.004, zb + 0.16 * H))
        tube(mb, lambda t, sc=sc, sg=sg: sc + V((sg * t, 0, 0)), [(0.0, .05 * s, .055 * s), (0.022, .05 * s, .055 * s), (0.03, .044 * s, .05 * s)],
             8, 'Backpack', ref=V((0, 0, 1)), cap0=False, cap1=True, e=0.6)
        box(mb, sc + V((sg * 0.031, 0, 0.045 * s)), X, Y, Z, 0.003, 0.05 * s, 0.006, 'Backpack_Strap')
    else:
        box(mb, V((0, yp + 0.004, pz + 0.05 * s)), X, Y, Z, 0.106 * s, 0.009, 0.04 * s, 'Backpack')
        for sg in (1, -1):
            box(mb, V((sg * 0.05 * s, yp + 0.011, pz + 0.0)), X, Y, Z, 0.011 * s, 0.004, 0.06 * s, 'Backpack_Strap')
            box(mb, V((sg * 0.05 * s, yp + 0.014, pz - 0.022 * s)), X, Y, Z, 0.015 * s, 0.005, 0.01 * s, 'Backpack_Hardware')
    # top handle
    hy = yfs[-1] + prof[-1][2] * s
    hp = [V((-0.04 * s, hy, zt - 0.004)), V((-0.03 * s, hy, zt + 0.025)), V((0.03 * s, hy, zt + 0.025)), V((0.04 * s, hy, zt - 0.004))]
    hp = resample(hp, 7)
    ribbon(mb, hp, [V((0, 0, 1)) if abs(p.x) < 0.03 * s else V((math.copysign(1, p.x), 0, 0.3)).normalized() for p in hp], 0.022, 0.008, 'Backpack_Strap')
    # shoulder straps
    for mir in (False, True):
        mb.mirror = mir
        ctrl = [V((0.075 * s, yfs[-2] - 0.004, zt - 0.045)), V((0.098 * s, 0.07, sz + 0.05)), V((0.112 * s, 0.0, sz + 0.054)),
                V((0.124 * s, -0.085, sz + 0.006)), V((0.13 * s, -0.125, sz - 0.095)), V((0.148 * s, -0.115, sz - 0.2)),
                V((0.172 * s, -0.035, sz - 0.275)), V((0.158 * s, 0.07, sz - 0.33)), V((0.12 * s, yfs[1] - 0.004, zb + 0.05))]
        pts = resample(ctrl, 15)
        mid, nr = on_surface(torso_bvh, pts[1:-1], 0.003)
        pts = [pts[0]] + mid + [pts[-1]]
        nr = [V((0, -1, 0.4)).normalized()] + nr + [V((0, -1, 0))]
        ribbon(mb, pts, nr, 0.05 * s, 0.011, 'Backpack_Strap')
        # strap adjuster buckle
        i = 10; t = (pts[i + 1] - pts[i - 1]).normalized(); n_ = nr[i]; sd = n_.cross(t).normalized()
        box(mb, pts[i] + n_ * 0.012, sd, n_, t, 0.029 * s, 0.004, 0.01, 'Backpack_Hardware')
    mb.mirror = False
    return mb

# ================================================================== UNIFORM ACCESSORIES
def build_tie(P, J, shirt_bvh, z0, fy, zend):
    mb = MB(); mb.wfn = W_torso(J, P); s = P['s']
    X, Z = V((1, 0, 0)), V((0, 0, 1))
    n = V((0, -1, 0.25)).normalized(); up = n.cross(X).normalized()
    box(mb, V((0, fy - 0.006, z0 + 0.006)), X, n, up, 0.016 * s, 0.008, 0.014 * s, 'Tie_Navy')
    pts = resample([V((0, fy, z0 - 0.01)), V((0, fy - 0.03, zend))], 9)
    pts, nr = on_surface(shirt_bvh, pts, 0.004)
    ribbon(mb, pts, nr, lambda t: lerp(0.026, 0.046, t) * s, 0.006, 'Tie_Navy')
    return mb

def build_lanyard(P, J, bvh, z0, ry, clip_z, card_bvh_out):
    mb = MB(); mb.wfn = W_torso(J, P); s = P['s']
    for mir in (False, True):
        mb.mirror = mir
        ctrl = [V((0, ry + 0.01, z0 + 0.026)), V((0.055 * s, 0.055 * s, z0 + 0.02)), V((0.072 * s, 0.0, z0 + 0.004)),
                V((0.075 * s, -0.07 * s, z0 - 0.03)), V((0.058 * s, -0.11, z0 - 0.085)), V((0.028 * s, -0.13, z0 - 0.17)),
                V((0.006, -0.135, clip_z))]
        pts = resample(ctrl, 16)
        pts, nr = on_surface(bvh, pts, 0.002)
        ribbon(mb, pts, nr, 0.013 * s, 0.003, 'Lanyard_Blue')
    mb.mirror = False
    # clip at the junction
    loc, n, _, _ = bvh.find_nearest(V((0, -0.14, clip_z)))
    X = V((1, 0, 0)); up = n.cross(X).normalized()
    box(mb, loc + n * 0.007, X, n, up, 0.009 * s, 0.004, 0.012 * s, 'Lanyard_Yellow')
    return mb, loc, n

def build_id_card(P, J, clip_loc, clip_n, fem):
    mb = MB(); mb.wfn = fixed('id_card'); s = P['s']
    X = V((1, 0, 0)); n = V((clip_n.x * 0, clip_n.y, clip_n.z * 0.5)).normalized(); up = n.cross(X).normalized()
    cw, chh = 0.028 * s, 0.042 * s
    c = clip_loc + n * 0.008 - up * (0.012 * s + chh)
    box(mb, c, X, n, up, cw, 0.0018, chh, 'ID_Band' if fem else 'ID_Card')
    face = c + n * 0.0019
    def quad(cx, cz, hx, hz, mat, lift=0.0004):
        p = face + n * lift
        ids = [mb.add(p + X * (cx + a) + up * (cz + b)) for a, b in ((-hx, -hz), (hx, -hz), (hx, hz), (-hx, hz))]
        f = ids if X.cross(up).dot(n) > 0 else ids[::-1]
        mb.face(f, mat)
    if fem:
        quad(-0.011 * s, 0.014 * s, 0.011 * s, 0.013 * s, 'ID_Photo')
        quad(0.0, -0.012 * s, 0.022 * s, 0.003 * s, 'ID_Card'); quad(-0.006 * s, -0.024 * s, 0.016 * s, 0.003 * s, 'ID_Card')
    else:
        quad(0.0, 0.032 * s, 0.028 * s, 0.008 * s, 'ID_Band')
        quad(0.0, 0.004 * s, 0.012 * s, 0.014 * s, 'ID_Photo')
        quad(0.0, -0.022 * s, 0.02 * s, 0.0025 * s, 'ID_Bar'); quad(0.0, -0.031 * s, 0.014 * s, 0.0025 * s, 'ID_Bar')
    return mb, c + up * chh, c - up * chh
