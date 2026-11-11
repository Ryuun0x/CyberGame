"""PSX-style boy: long messy dark hair, oversized hoodie, pleated skirt, knee socks, chunky sneakers.

Original character in the style of PS1-era low-poly anime models (big head, slim limbs, one 256 px nearest-filtered
atlas, pixel-painted face). Built from scratch; nothing is taken from any reference asset.

Headless:  blender -b --factory-startup --python psx_boy.py      (env LP_OUT = output folder)
Reuses the LowPolyStudents toolkit (MB/tube/weights from lp_build, armature from lp_assemble, Idle/Walk/Run from lp_anim,
stage/renders from lp_render, GLB export from lp_final).
"""
import bpy, os, math, random
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
LP = os.path.join(os.path.dirname(HERE))                 # .../LowPolyStudents/scripts
OUT = os.environ.setdefault('LP_OUT', os.path.join(os.path.dirname(LP), 'psx_boy'))
os.environ.setdefault('LP_BLEND', 'PSX_Boy.blend')
for d in ('renders', 'glb', 'textures'): os.makedirs(os.path.join(OUT, d), exist_ok=True)
G = globals()
for f in ('lp_build.py', 'lp_assemble.py', 'lp_anim.py', 'lp_render.py', 'lp_final.py'):
    exec(compile(open(os.path.join(LP, f), encoding='utf8').read(), f, 'exec'), G)

NAME = 'PSX_Boy'
CHIN = 1.39; HEAD_H = 0.245                # slim, long-legged: ~6.6 heads tall
HK = HEAD_H / 0.27                         # head ring table is authored for a 0.27 m head
ATLAS = 256

# ------------------------------------------------------------------ proportions / skeleton (same keys as lp_build.params)
def boy_params():
    return dict(sex='M', s=0.9, pelvis=(0.92, 0.99), spine_top=1.12, chest_top=1.285,
                neck=(V((0, 0.012, 1.30)), V((0, 0.012, CHIN + 0.022))), head_top=CHIN + HEAD_H,
                head_c=V((0, 0.0, CHIN + 0.12)), clav0=V((0.02, 0.0, 1.29)), shoulder=V((0.14, 0.006, 1.285)),
                arm_angle=30, upper=0.265, fore=0.235, hand=0.15, elbow_bend=8,
                hip=V((0.074, 0.0, 0.895)), knee=V((0.078, -0.012, 0.49)), ankle=V((0.082, 0.016, 0.078)),
                ball=V((0.09, -0.098, 0.022)), toe=V((0.092, -0.16, 0.02)), skirt=True)

# ------------------------------------------------------------------ head
HEAD_RINGS = [  # z above chin, half-width, front depth, back depth, centre y  (front = -Y)
    (0.000, 0.024, 0.010, 0.020, -0.040),
    (0.030, 0.064, 0.050, 0.060, -0.012),
    (0.075, 0.096, 0.082, 0.092, 0.000),
    (0.125, 0.107, 0.090, 0.105, 0.004),
    (0.175, 0.110, 0.092, 0.114, 0.010),
    (0.220, 0.100, 0.080, 0.106, 0.016),
    (0.252, 0.066, 0.050, 0.074, 0.020),
]
HEAD_N = 10

def head_ring(z, w, df, db, cy, n=HEAD_N):
    z, w, df, db, cy = z * HK, w * HK, df * HK, db * HK, cy * HK
    pts = []
    for i in range(n):
        t = 2 * math.pi * i / n             # 0 = +X, CCW around +Z (toward +Y = back)
        c, s = math.cos(t), math.sin(t)
        x = w * se(c, 0.75); y = cy + (db if s > 0 else df) * se(s, 0.65 if s < 0 else 0.85)
        pts.append(V((x, y, CHIN + z)))
    return pts

def build_head(J, P):
    mb = MB(); mb.wfn = fixed('head')
    R = [mb.ring(head_ring(*r)) for r in HEAD_RINGS]
    for a, b in zip(R, R[1:]): mb.bridge(a, b, 'Head')
    mb.cap(R[0], 'Head', end=False, center=V((0, -0.044 * HK, CHIN - 0.004)))
    mb.cap(R[-1], 'Head', end=True, center=V((0, 0.022, CHIN + HEAD_H)))
    # ears: flat wedges at eye level, mostly under the hair
    for mir in (False, True):
        mb.mirror = mir
        e0 = V((0.100, 0.012, CHIN + 0.115)) * 1; e1 = V((0.118, 0.026, CHIN + 0.150)); e2 = V((0.104, 0.030, CHIN + 0.092))
        e0, e1, e2 = [V((e.x * HK, e.y * HK, CHIN + (e.z - CHIN) * HK)) for e in (e0, e1, e2)]
        a, b, c = mb.add(e0), mb.add(e1), mb.add(e2); mb.face([a, b, c], 'Skin'); mb.face([c, b, a], 'Skin')
    mb.mirror = False
    return mb

def build_neck(J, P):
    mb = MB(); mb.wfn = W_neck(J, P)
    ch = lambda t: V((0, 0.014, 1.28 + t))
    tube(mb, ch, [(0.0, 0.046, 0.046), (0.06, 0.035, 0.036), (0.13, 0.033, 0.035)], 6, 'Skin', ref=V((0, -1, 0)), cap0=False, cap1=False)
    return mb

# ------------------------------------------------------------------ hair (chunky polygon locks over a cap)
def head_surface(head_mb):
    bvh = head_mb.bvh(); C = V((0, 0.006, CHIN + 0.14))
    def pt(th, ph, lift):
        t, f = math.radians(th), math.radians(ph)
        d = V((math.cos(f) * math.sin(t), -math.cos(f) * math.cos(t), math.sin(f)))
        loc, n, _, _ = bvh.ray_cast(C, d)
        if loc is None: loc = C + d * 0.11
        return loc + (loc - C).normalized() * lift
    return pt, C

def lock(mb, pts, C, width, thick, mat='Hair'):
    """Faceted lock: triangle section, ridge outward, tapering to a point. pts = polyline from root to tip."""
    n = len(pts); rings = []
    for i, p in enumerate(pts[:-1]):
        t = (pts[i + 1] - pts[max(i - 1, 0)]).normalized()
        nr = (p - V((0, C.y, p.z))) if p.z < C.z - 0.05 else (p - C)        # below the head: radial from the body axis
        nr = (nr - t * nr.dot(t)).normalized(); sd = nr.cross(t)
        f = i / (n - 1); wv = width * (1 - f ** 1.4); tv = thick * (1 - 0.5 * f)
        rings.append(mb.ring([p + sd * (wv / 2) - nr * tv * 0.4, p + nr * tv, p - sd * (wv / 2) - nr * tv * 0.4]))
    for a, b in zip(rings, rings[1:]): mb.bridge(a, b, mat)
    mb.cap(rings[0], mat); mb.cap(rings[-1], mat, end=True, center=pts[-1])

def build_hair(head_mb):
    pt, C = head_surface(head_mb)
    mb = MB(); mb.wfn = fixed('head'); rnd = random.Random(7)
    # cap: rings from the hairline to the crown
    hl = lambda th: interp([(0, 40), (40, 34), (75, 14), (100, 4), (140, -18), (180, -26)], abs(th))
    n = 12; ths = [-180 + 360 * i / n for i in range(n)]; rows = []
    for r in range(4):
        rows.append(mb.ring([pt(th, lerp(hl(th), 86, (r / 3) ** 0.9), 0.012 + 0.006 * (r > 0) + rnd.uniform(0, 0.004)) for th in ths]))
    for a, b in zip(rows, rows[1:]): mb.bridge(a, b, 'Hair')
    mb.cap(rows[-1], 'Hair', end=True, center=pt(0, 90, 0.02))
    mb.cap(rows[0], 'Hair', end=False)       # closes the cap underside (hidden)
    def L(ctrl, w, t):
        pts = []
        for c in ctrl:
            pts.append(pt(*c) if isinstance(c, tuple) else V(c))
        lock(mb, resample(pts, 4), C, w, t)
    # bangs: uneven, falling over the brows to the eyes
    for th, tip_th, tip_z, w in ((-4, -1, 0.112, 0.06), (14, 18, 0.13, 0.055), (-20, -24, 0.108, 0.055), (32, 38, 0.122, 0.05),
                               (-38, -44, 0.116, 0.05), (52, 60, 0.108, 0.05), (-58, -66, 0.1, 0.05)):
        tip = pt(tip_th, 8, 0.012); tip.z = CHIN + tip_z * HK / 0.907
        L([(th, 70, 0.014), (th * 0.9 + tip_th * 0.1, 38, 0.016), tip], w, 0.016)
    for th, ph, ln in ((35, 78, 0.065), (-45, 76, 0.06), (75, 66, 0.055), (-85, 62, 0.06), (150, 72, 0.05)):
        b = pt(th, ph, 0.012); out = (b - C).normalized()
        side = V((math.copysign(1.0, math.sin(math.radians(th))), 0.4, 0.0))
        tip = b + (out * 0.55 + side * 0.9).normalized() * ln                         # fan out sideways/back, not up
        L([b, b.lerp(tip, 0.5), tip], 0.022, 0.01)
    # side locks framing the face, down past the jaw
    for sg in (1, -1):
        for th, tip_z, w in ((62, 0.02, 0.05), (82, -0.03, 0.055), (102, -0.06, 0.055)):
            base = pt(sg * th, 50, 0.018); mid = pt(sg * (th + 6), 5, 0.02)
            tip = V((sg * (0.10 + 0.012 * (th > 80)), mid.y + 0.012, CHIN + tip_z + rnd.uniform(-0.015, 0.015)))
            L([base, mid, tip], w, 0.02)
    # back: long messy locks to the upper back
    for k, th in enumerate((125, 145, 165, 180, -165, -145, -125)):
        base = pt(th, 55, 0.018); mid = pt(th, 0, 0.024)
        tip = V((mid.x * 1.15, mid.y + 0.04, 1.20 + rnd.uniform(-0.03, 0.03) - 0.02 * (abs(th) > 150)))
        L([base, mid, tip], 0.09, 0.022)
    return mb

# ------------------------------------------------------------------ hoodie, skirt, limbs, shoes
HOODIE = [(0.80, .15, .102, .102), (0.825, .164, .112, .112), (0.93, .168, .114, .108), (1.06, .162, .112, .102),
          (1.18, .168, .112, .1), (1.255, .158, .1, .094), (1.30, .13, .082, .08), (1.325, .09, .066, .066), (1.333, .062, .052, .052)]

def build_hoodie(J, P):
    mb = MB(); mb.wfn = W_torso(J, P)
    R = [mb.ring(torso_ring(z, w, df, db, 12)) for z, w, df, db in HOODIE]
    for a, b in zip(R, R[1:]): mb.bridge(a, b, 'Hoodie')
    # sleeves: wide, cuffs slouching over the hands ("sweater paws")
    mb.wfn = W_arm(J, P); ch = arm_chain(J)
    for mir in (False, True):
        mb.mirror = mir
        tube(mb, ch, [(0.02, .048, .046), (0.15, .06, .058), (0.6, .057, .056), (1.0, .053, .052), (1.5, .055, .054),
                      (1.85, .06, .058), (2.05, .052, .05)], 6, 'Sleeve', cap0=False, cap1=False)
    mb.mirror = False
    # folded hood: a fat roll around the back of the neck plus the hood body hanging down the back
    mb.wfn = wfun(J, [('chest', .1, None), ('neck', .04, None)])
    path = polyline([V((0.068, -0.032, 1.315)), V((0.098, 0.04, 1.335)), V((0.056, 0.096, 1.35)), V((0, 0.11, 1.355)),
                     V((-0.056, 0.096, 1.35)), V((-0.098, 0.04, 1.335)), V((-0.068, -0.032, 1.315))])
    rings = []
    for k in range(9):
        s = k / 8; c = path(s); d = (path(s + 0.03) - path(s - 0.03)).normalized()
        rings.append(mb.ring(ring_pts(c, d, 0.031, 0.04, 6, ref=V((0, 0, 1)))))
    for a, b in zip(rings, rings[1:]): mb.bridge(a, b, 'Hood')
    mb.cap(rings[0], 'Hood'); mb.cap(rings[-1], 'Hood', end=True)
    flap = [V((0.08, 0.108, 1.345)), V((-0.08, 0.108, 1.345)), V((-0.048, 0.138, 1.21)), V((0.0, 0.146, 1.17)), V((0.048, 0.138, 1.21))]
    top = [mb.add(p) for p in flap]; bot = [mb.add(p - V((0, 0.018, 0))) for p in flap]
    mb.face(top[::-1], 'Hood'); mb.face(bot, 'Hood')
    for i in range(5):
        j = (i + 1) % 5; mb.face([top[i], top[j], bot[j], bot[i]], 'Hood')
    return mb

SKIRT = [(0.63, .19, .13, .135), (0.73, .172, .118, .123), (0.83, .138, .094, .098), (0.93, .128, .086, .09)]   # tucked inside the hoodie above 0.80

def build_skirt(J, P):
    mb = MB(); mb.wfn = W_skirt(J, P, 0.93, 0.63)
    pleat = lambda z: (lambda i: 0.07 * ss(0.78, 0.67, z) * (1 if i % 2 else -0.4))
    R = [mb.ring(torso_ring(z, w, df, db, 16, jit=pleat(z))) for z, w, df, db in SKIRT]
    for a, b in zip(R, R[1:]): mb.bridge(a, b, 'Skirt')
    inner = mb.ring(torso_ring(0.64, .176, .12, .125, 16))         # inside of the hem, dark
    mb.bridge(inner, R[0], 'Skirt')
    return mb

def build_legs(J, P):
    mb = MB(); mb.wfn = W_leg(J, P); ch = leg_chain(J)
    for mir in (False, True):
        mb.mirror = mir
        tube(mb, ch, [(0.25, .058, .06), (0.6, .05, .052), (0.95, .039, .04), (1.05, .04, .041), (1.35, .043, .043),
                      (1.7, .032, .032), (1.95, .029, .029)], 6, 'Leg', cap0=True, cap1=False)
    mb.mirror = False
    return mb

def build_hands(J, P):
    mb = MB(); mb.wfn = wfun(J, [('hand.L', .05, None), ('forearm.L', .02, None)], power=6); ch = arm_chain(J)
    for mir in (False, True):
        mb.mirror = mir
        tube(mb, ch, [(1.97, .028, .022), (2.15, .04, .018), (2.55, .04, .016), (2.85, .03, .013)], 6, 'Hand', ref=V((0, -1, 0)), cap0=True, cap1=True)
        Wr, Ht = J['hand.L']; d = (Ht - Wr).normalized()
        b0 = Wr + d * 0.035 + V((0, -0.022, 0)); tdir = (d + V((0, -0.6, 0))).normalized()
        tube(mb, lambda t, b0=b0, tdir=tdir: b0 + tdir * t, [(0.0, .012, .01), (0.05, .01, .009)], 4, 'Hand', cap0=True, cap1=True)
    mb.mirror = False
    return mb

SHOE = [(0.07, .036, .09, .02), (0.045, .043, .1, .0), (0.0, .046, .095, .0), (-0.06, .049, .08, .0), (-0.12, .049, .066, .0),
        (-0.16, .044, .058, .0), (-0.185, .032, .05, .0)]    # dy from ankle, half width, top z, sole lift

def build_shoes(J, P):
    mb = MB(); mb.wfn = W_foot(J, P); A = J['foot.L'][0]
    for mir in (False, True):
        mb.mirror = mir; rings = []
        for dy, hw, zt, _ in SHOE:
            y = A.y + dy
            pts = [V((A.x + hw, y, 0.0)), V((A.x + hw * 1.02, y, zt * 0.45)), V((A.x + hw * 0.8, y, zt)),
                   V((A.x - hw * 0.8, y, zt)), V((A.x - hw * 1.02, y, zt * 0.45)), V((A.x - hw, y, 0.0))]
            rings.append(mb.ring(pts[::-1]))
        for a, b in zip(rings, rings[1:]): mb.bridge(a, b, 'Shoe')
        mb.cap(rings[0], 'Shoe'); mb.cap(rings[-1], 'Shoe', end=True)
    mb.mirror = False
    return mb

# ------------------------------------------------------------------ atlas regions + UVs
REG = {   # name: (x0, y0, w, h) in atlas pixels, origin bottom-left
    'Face': (0, 192, 64, 64), 'Skin': (64, 224, 32, 32), 'Hand': (96, 224, 32, 32), 'Hair': (128, 192, 64, 64),
    'Hood': (192, 192, 64, 64), 'Hoodie': (0, 96, 128, 96), 'Sleeve': (128, 96, 64, 96), 'Skirt': (0, 48, 128, 48),
    'Leg': (192, 96, 32, 96), 'Shoe': (128, 32, 64, 64), 'Head': None,
}

def _cyl(p, cx, cy, z0, z1):
    ang = math.atan2(p.x - cx, -(p.y - cy)); return 0.5 + ang / (2 * math.pi), (p.z - z0) / (z1 - z0)

def _chain_uv(p, pts):
    """(u around the limb, v along the chain 0..1) for a polyline chain."""
    best = None; acc = 0.0; tot = sum((b - a).length for a, b in zip(pts, pts[1:]))
    for a, b in zip(pts, pts[1:]):
        ab = b - a; L = ab.length; t = max(0.0, min(1.0, (p - a).dot(ab) / (L * L))); c = a + ab * t
        d = (p - c).length
        if best is None or d < best[0]: best = (d, (acc + t * L) / tot, c, ab.normalized())
        acc += L
    _, v, c, d = best; u_, w_ = frame_for(d)
    off = p - c; return 0.5 + math.atan2(off.dot(w_), off.dot(u_)) / (2 * math.pi), v

def face_uv(mat, p, n, J):
    """Returns (region, (u, v)) for a vertex position of a face with material `mat` and face normal n."""
    side = 'L' if p.x >= 0 else 'R'; q = p if side == 'L' else mx(p)
    if mat == 'Head':
        if n.y < -0.35 and p.z < CHIN + 0.25: return 'Face', ((p.x + 0.112) / 0.224, (p.z - CHIN) / HEAD_H)
        return 'Skin', _cyl(p, 0, 0.01, CHIN, CHIN + HEAD_H)
    if mat == 'Skin': return 'Skin', _cyl(p, 0, 0.01, 1.2, CHIN + HEAD_H)
    if mat == 'Hair': return 'Hair', _cyl(p, 0, 0.01, 1.15, CHIN + HEAD_H + 0.04)
    if mat == 'Hood': return 'Hood', _cyl(p, 0, 0.05, 1.16, 1.41)
    if mat == 'Hoodie': return 'Hoodie', _cyl(p, 0, 0, 0.80, 1.335)
    if mat == 'Skirt': return 'Skirt', _cyl(p, 0, 0, 0.63, 0.93)
    if mat == 'Sleeve':
        S, E = J['upper_arm.L']; _, W = J['forearm.L']; return 'Sleeve', _chain_uv(q, [S, E, W + (W - E).normalized() * 0.05])
    if mat == 'Hand':
        _, W = J['forearm.L']; _, H = J['hand.L']; return 'Hand', _chain_uv(q, [W, H + (H - W).normalized() * 0.03])
    if mat == 'Leg':
        H, K = J['thigh.L']; _, A = J['shin.L']; return 'Leg', _chain_uv(q, [H, K, A])
    if mat == 'Shoe':
        A = J['foot.L'][0]; return 'Shoe', ((A.y + 0.08 - q.y) / 0.29, q.z / 0.11)
    return 'Skin', (0.5, 0.5)

def make_obj(name, mb, coll, mat, J, smooth=True):
    me = bpy.data.meshes.new(name); me.from_pydata([tuple(p) for p in mb.v], [], mb.f); me.update()
    ob = bpy.data.objects.new(name, me); coll.objects.link(ob); me.materials.append(mat)
    uvl = me.uv_layers.new(name='UVMap')
    for fi, poly in enumerate(me.polygons):
        n = poly.normal; uvs = []; reg = None
        for li in poly.loop_indices:
            p = me.vertices[me.loops[li].vertex_index].co
            reg, uv = face_uv(mb.mats[mb.fm[fi]], p, n, J); uvs.append(list(uv))
        us = [u[0] for u in uvs]
        if max(us) - min(us) > 0.5:                               # seam: keep the face on one side of the wrap
            for u in uvs:
                if u[0] < 0.5: u[0] += 1.0
        x0, y0, w, h = REG[reg]
        for li, (u, v) in zip(poly.loop_indices, uvs):
            u = min(max(u, 0.0), 1.0); v = min(max(v, 0.0), 1.0)
            uvl.data[li].uv = ((x0 + 0.5 + u * (w - 1)) / ATLAS, (y0 + 0.5 + v * (h - 1)) / ATLAS)
        poly.use_smooth = smooth and mb.mats[mb.fm[fi]] not in ('Hair', 'Hood', 'Shoe')
    groups = {}
    for vi, w in enumerate(mb.w):
        for b, x in w.items():
            g = groups.get(b) or ob.vertex_groups.new(name=b); groups[b] = g; g.add([vi], x, 'REPLACE')
    return ob

# ------------------------------------------------------------------ texture painting (numpy, sRGB 0..1, row 0 = bottom)
def hexc(h): return np.array([int(h[i:i + 2], 16) / 255 for i in (1, 3, 5)], np.float32)
COL = dict(skin='#EFE7E9', skin_sh='#D6C9CF', blush='#E8C3CA', hair='#1E1B24', hair_hi='#3A3546', hair_sh='#121017',
           hood='#8C8794', hood_sh='#6E6976', hood_dk='#4D4955', string='#E8E4EA', skirt='#1C1C22', skirt_hi='#2E2E36',
           skirt_line='#8E8A96', sock='#16161A', sock_line='#E8E6EC', shoe='#1A1A1E', sole='#E4E2E6', lace='#C9C6CF',
           eye_dark='#1A1520', iris='#3B3346', white='#F4F2F6', lash='#0E0B12', bags='#C9A8B4', mouth='#9C6E72')

def paint_atlas(path):
    A = np.zeros((ATLAS, ATLAS, 3), np.float32); A[:] = hexc(COL['skin'])
    rnd = np.random.default_rng(3)
    def region(name):
        x0, y0, w, h = REG[name]; U, Vv = np.meshgrid((np.arange(w) + 0.5) / w, (np.arange(h) + 0.5) / h)
        return A[y0:y0 + h, x0:x0 + w], U, Vv
    def dither(img, amt):
        img += (rnd.random(img.shape[:2])[..., None] - 0.5) * amt
    # skin + hands
    for nm in ('Skin', 'Hand'):
        img, U, Vv = region(nm); img[:] = hexc(COL['skin']); dither(img, 0.02)
    # face
    img, U, Vv = region('Face'); img[:] = hexc(COL['skin']); paint_face(img)
    # hair: dark with a few lighter strand streaks and darker roots
    img, U, Vv = region('Hair'); img[:] = hexc(COL['hair'])
    for k in range(14):
        u0 = rnd.random(); wv = 0.012 + rnd.random() * 0.01
        m = (np.abs(((U - u0 + 0.5) % 1) - 0.5) < wv) & (Vv > rnd.random() * 0.5)
        img[m] = hexc(COL['hair_hi'])
    img[Vv > 0.9] = hexc(COL['hair_sh']); dither(img, 0.03)
    # hoodie body
    img, U, Vv = region('Hoodie'); img[:] = hexc(COL['hood'])
    img[:] = img * (0.86 + 0.14 * np.clip(Vv, 0, 1))[..., None]                       # darker toward the hem
    img[Vv < 0.06] = hexc(COL['hood_sh']); img[(Vv < 0.06) & ((np.arange(img.shape[1]) % 3) == 0)[None, :]] = hexc(COL['hood_dk'])
    front = np.abs(U - 0.5)
    pocket = (front < 0.13) & (Vv > 0.1) & (Vv < 0.36)
    img[pocket] = hexc(COL['hood_sh']) * 1.05
    edge = pocket & ((front > 0.115) | (Vv > 0.345))
    img[edge] = hexc(COL['hood_dk'])
    for su in (0.47, 0.53):                                                            # drawstrings
        img[(np.abs(U - su) < 0.006) & (Vv > 0.7) & (Vv < 0.95)] = hexc(COL['string'])
    for su in (0.25, 0.75):                                                            # side seams
        img[np.abs(U - su) < 0.006] = hexc(COL['hood_sh'])
    dither(img, 0.03)
    # sleeves: cuff rib at the wrist end, an elbow crease
    img, U, Vv = region('Sleeve'); img[:] = hexc(COL['hood'])
    img[(Vv > 0.88)] = hexc(COL['hood_sh']); img[(Vv > 0.88) & (((U * 64).astype(int) % 3) == 0)] = hexc(COL['hood_dk'])
    img[np.abs(Vv - 0.47) < 0.012] = hexc(COL['hood_sh']); dither(img, 0.03)
    # hood: inner lining darker on the inside half
    img, U, Vv = region('Hood'); img[:] = hexc(COL['hood']); img[Vv > 0.75] = hexc(COL['hood_sh']); dither(img, 0.03)
    # skirt: 16 pleats (alternating light/dark), thin pale line above the hem
    img, U, Vv = region('Skirt'); k = (U * 16).astype(int) % 2
    img[:] = np.where(k[..., None] == 1, hexc(COL['skirt_hi']), hexc(COL['skirt']))
    img[(Vv > 0.07) & (Vv < 0.1)] = hexc(COL['skirt_line']); dither(img, 0.02)
    # legs: skin to just below the knee, then black knee socks with two white stripes
    img, U, Vv = region('Leg'); img[:] = hexc(COL['skin'])
    sock = Vv > 0.53; img[sock] = hexc(COL['sock'])
    for v0 in (0.56, 0.6): img[np.abs(Vv - v0) < 0.012] = hexc(COL['sock_line'])
    dither(img, 0.02)
    # shoes: black upper, thick white sole, lace bars on top
    img, U, Vv = region('Shoe'); img[:] = hexc(COL['shoe']); img[Vv < 0.3] = hexc(COL['sole'])
    img[(Vv > 0.82) & (U > 0.25) & (U < 0.5) & (((Vv * 64).astype(int) % 3) == 0)] = hexc(COL['lace'])
    np.clip(A, 0, 1, out=A)
    im = bpy.data.images.new('PSX_Boy_Atlas', ATLAS, ATLAS, alpha=False)
    im.pixels.foreach_set(np.concatenate([A, np.ones((ATLAS, ATLAS, 1), np.float32)], 2).ravel())
    im.filepath_raw = path; im.file_format = 'PNG'; im.save(); im.pack()
    return im

def paint_face(img):
    """64x64 pixel face: heavy-lidded tired eyes with thick lashes and under-eye shadows, thin brows, small mouth."""
    h, w = img.shape[:2]
    def px(x, y, c):
        if 0 <= x < w and 0 <= y < h: img[y, x] = hexc(COL[c])
    def rect(x0, y0, x1, y1, c):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1): px(x, y, c)
    ey = int(round(0.47 * h))          # eye centre row (z ~ chin + 0.136)
    for sg in (1, -1):
        cx = 32 + sg * 12                # eye centre column
        xs = range(cx - 6, cx + 7) if sg > 0 else range(cx - 6, cx + 7)
        rect(cx - 5, ey - 3, cx + 5, ey + 2, 'white')                       # eye white
        rect(cx - 6, ey + 3, cx + 6, ey + 4, 'lash')                        # heavy upper lash line (half-lidded)
        rect(cx - 5, ey + 2, cx + 5, ey + 2, 'eye_dark')                    # lid shadow over the iris top
        px(cx + sg * 7, ey + 4, 'lash'); px(cx + sg * 8, ey + 5, 'lash')     # outer flick
        rect(cx - 4, ey - 4, cx + 4, ey - 4, 'skin_sh')                     # lower lash, faint
        rect(cx - 4, ey - 5, cx + 3, ey - 5, 'bags')                        # tired under-eye shadow
        rect(cx - 4, ey + 9, cx + 3, ey + 9, 'hair')                        # thin straight brow (mostly under bangs)
        px(cx + sg * 4, ey + 10, 'hair')
    px(32, int(0.31 * h), 'skin_sh'); px(33, int(0.31 * h) + 1, 'skin_sh')    # nose hint
    for sg in (1, -1):
        rect(32 + sg * 14 - 2, ey - 9, 32 + sg * 14 + 2, ey - 8, 'blush')    # faint blush

# ------------------------------------------------------------------ pupils + mouth: separate planes with their own small textures
def paint_small(name, size, fn):
    """RGBA texture painted by fn(img, alpha) on a size x size grid (row 0 = bottom)."""
    img = np.zeros((size, size, 3), np.float32); a = np.zeros((size, size), np.float32)
    fn(img, a)
    im = bpy.data.images.new(name, size, size, alpha=True)
    im.pixels.foreach_set(np.concatenate([img, a[..., None]], 2).ravel())
    im.filepath_raw = os.path.join(OUT, 'textures', name + '.png'); im.file_format = 'PNG'; im.save(); im.pack()
    return im

def _pupil(img, a):
    n = img.shape[0]; Y, X = np.mgrid[0:n, 0:n] + 0.5; c = n / 2
    r = np.hypot((X - c) / (n * 0.42), (Y - c) / (n * 0.48))
    iris = r < 1.0; a[iris] = 1.0
    img[iris] = hexc('#3E3650') * (0.75 + 0.35 * np.clip((Y[iris] - c) / n + 0.5, 0, 1))[:, None]   # darker at the top
    img[r < 0.55] = hexc('#120E18')                                                                  # pupil
    ring = (r > 0.86) & iris; img[ring] = hexc('#1B1624')
    for (hx, hy, rr) in ((0.36, 0.66, 0.11), (0.62, 0.34, 0.06)):                                   # highlights
        m = np.hypot(X / n - hx, Y / n - hy) < rr; img[m] = hexc('#F4F2F8'); a[m] = 1.0

def _mouth(img, a):
    n = img.shape[0]; Y, X = np.mgrid[0:n, 0:n] + 0.5
    line = (np.abs(Y - n * 0.5 - 0.06 * (X - n / 2) ** 2 / n) < n * 0.06) & (np.abs(X - n / 2) < n * 0.34)   # slight frown
    img[line] = hexc('#8A5F66'); a[line] = 1.0

def build_face_planes(head_mb):
    """Quads 1.5 mm in front of the face at the painted eye sockets and mouth; weighted to the head."""
    bvh = head_mb.bvh(); mb = MB(); mb.wfn = fixed('head'); uvs = {}
    def quad(cx, cz, hw, hh, mat):
        loc, n, _, _ = bvh.ray_cast(V((cx, -1.0, cz)), V((0, 1, 0)))
        y = (loc.y if loc else -0.09) - 0.0015
        ids = [mb.add(V((cx + dx, y, cz + dz))) for dx, dz in ((-hw, -hh), (hw, -hh), (hw, hh), (-hw, hh))]
        mb.face(ids, mat); uvs[len(mb.f) - 1] = [(0, 0), (1, 0), (1, 1), (0, 1)]
    ez = CHIN + 0.47 * HEAD_H; ex = 0.6875 * 0.224 - 0.112
    for sg in (1, -1): quad(sg * ex, ez + 0.001, 0.0125, 0.0125, 'Pupil')
    quad(0.0, CHIN + 0.205 * HEAD_H, 0.0135, 0.0065, 'Mouth')
    return mb, uvs

def make_face_obj(name, mb, uvs, coll, mats):
    me = bpy.data.meshes.new(name); me.from_pydata([tuple(p) for p in mb.v], [], mb.f); me.update()
    for m in mb.mats: me.materials.append(mats[m])
    me.polygons.foreach_set('material_index', mb.fm)
    uvl = me.uv_layers.new(name='UVMap')
    for fi, poly in enumerate(me.polygons):
        for li, uv in zip(poly.loop_indices, uvs[fi]): uvl.data[li].uv = uv
    ob = bpy.data.objects.new(name, me); coll.objects.link(ob)
    g = ob.vertex_groups.new(name='head'); g.add(list(range(len(mb.v))), 1.0, 'REPLACE')
    return ob

def tex_mat(name, img, alpha=False):
    m = bpy.data.materials.new(name); m.use_nodes = True
    nt = m.node_tree; b = nt.nodes['Principled BSDF']; tx = nt.nodes.new('ShaderNodeTexImage')
    tx.image = img; tx.interpolation = 'Linear'; nt.links.new(tx.outputs['Color'], b.inputs['Base Color'])
    b.inputs['Roughness'].default_value = 0.95
    try: b.inputs['Specular IOR Level'].default_value = 0.1
    except Exception: pass
    if alpha:
        nt.links.new(tx.outputs['Alpha'], b.inputs['Alpha'])
        for attr, val in (('blend_method', 'CLIP'), ('surface_render_method', 'DITHERED')):
            try: setattr(m, attr, val)
            except Exception: pass
    return m

# ------------------------------------------------------------------ build
def build():
    for o in [o for o in bpy.data.objects]: bpy.data.objects.remove(o)
    P = boy_params(); J = joints(P)
    root = bpy.data.collections.new('Characters'); bpy.context.scene.collection.children.link(root)
    coll = bpy.data.collections.new(NAME); root.children.link(coll)
    atlas = paint_atlas(os.path.join(OUT, 'textures', 'PSX_Boy_Atlas.png'))
    mat = tex_mat('PSX_Boy', atlas)                         # soft (linear) filtering, like the reference game
    fmats = {'Pupil': tex_mat('PSX_Boy_Pupil', paint_small('PSX_Boy_Pupil', 32, _pupil), True),
             'Mouth': tex_mat('PSX_Boy_Mouth', paint_small('PSX_Boy_Mouth', 32, _mouth), True)}
    head = build_head(J, P)
    face_mb, face_uvs = build_face_planes(head)
    parts = {'Body': MB(), 'Hair': build_hair(head), 'Clothes': MB()}
    for m in (head, build_neck(J, P), build_hands(J, P), build_legs(J, P)): parts['Body'].merge(m)
    for m in (build_hoodie(J, P), build_skirt(J, P), build_shoes(J, P)): parts['Clothes'].merge(m)
    arm = make_armature_data(NAME + '_Armature', J, P, {})
    rig = bpy.data.objects.new(NAME + '_Rig', arm); coll.objects.link(rig)
    tris = 0
    for pn, mb in parts.items():
        ob = make_obj(f'{NAME}_{pn}', mb, coll, mat, J); ob.parent = rig
        md = ob.modifiers.new('Armature', 'ARMATURE'); md.object = rig
        tris += sum(len(p.vertices) - 2 for p in ob.data.polygons)
    fo = make_face_obj(f'{NAME}_Face', face_mb, face_uvs, coll, fmats); fo.parent = rig
    md = fo.modifiers.new('Armature', 'ARMATURE'); md.object = rig
    tris += len(face_mb.f) * 2
    bpy.context.view_layer.update()
    info = {NAME: dict(P=P, J=J, rig=rig, tris=tris)}
    G['INFO'] = info
    return info

def main():
    info = build()
    setup_stage(); rep = animate_all(info)
    bpy.data.objects['Stage_Floor'].scale = (10, 10, 1)
    cam = bpy.data.objects['Preview_Cam']
    d = info[NAME]
    rest_pose(d['rig']); bpy.context.scene.frame_set(0)
    turnaround(NAME, 0, cam, os.path.join(OUT, 'renders', 'turnaround.png'), res=(600, 1000), ortho=1.9)
    face_sheet(NAME, os.path.join(OUT, 'renders', 'face.png'), res=(500, 540))
    restore_nla(d['rig'])
    for clip in ('Walk', 'Run'):
        G['pose_strip'](d, clip, os.path.join(OUT, 'renders', f'filmstrip_{clip.lower()}.png'))
    if os.environ.get('LP_VIDEOS', '0') != '0':
        for clip, loops, yaw in (('Idle', 1, 30), ('Walk', 3, 35), ('Run', 4, 35), ('Run', 4, 90)):
            tag = 'side' if yaw == 90 else '3q'
            render_video(info, clip, yaw, os.path.join(OUT, 'renders', f'preview_{clip.lower()}_{tag}.mp4'), loops, dist=6, lens=50)
    glb = verify_glb(export_glb(NAME, d))
    save_blend()
    import json
    json.dump(dict(tris=d['tris'], ik_miss=rep, glb=glb), open(os.path.join(OUT, 'report.json'), 'w'), indent=1, default=str)
    return d['tris']

def pose_strip(d, clip, out, n=6):
    rig = d['rig']; cam = bpy.data.objects['Preview_Cam']; play(rig, clip); T = CLIPS[clip]['T']; paths = []
    for k in range(n):
        bpy.context.scene.frame_set(int(round(k * T / n)))
        aim(cam, V((0, 0, 0.85)), 90 if k % 2 == 0 else 35, ortho=1.9)
        paths.append(still(os.path.join(OUT, 'renders', f'_ps{k}.png'), (300, 520)))
    stitch(paths, out)
    for p in paths: os.remove(p)
    restore_nla(rig)

if (__name__ == '__main__' or bpy.app.background) and os.environ.get('PSX_LIB') != '1':
    print('PSX_BOY_TRIS', main())
