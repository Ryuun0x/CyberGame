"""Low-poly student characters: geometry, skinning and rig.

Run inside Blender 5.2 (Text editor > Run, or exec(open(path).read())).
Coordinates: Z up, character faces -Y, +X is the character's LEFT.
Left-side parts are authored once and mirrored (MB.mirror) to the right.
"""
import bpy, bmesh, math, random
from mathutils import Vector as V, Quaternion
from mathutils.bvhtree import BVHTree
from mathutils.geometry import tessellate_polygon

# ------------------------------------------------------------------ helpers
def se(x, e): return math.copysign(abs(x) ** e, x)
def ss(e0, e1, x):
    t = max(0.0, min(1.0, (x - e0) / (e1 - e0))); return t * t * (3 - 2 * t)
def lerp(a, b, t): return a + (b - a) * t
def mx(p): return V((-p.x, p.y, p.z))
def swap(n): return n[:-2] + '.R' if n.endswith('.L') else (n[:-2] + '.L' if n.endswith('.R') else n)
def interp(pts, x):
    if x <= pts[0][0]: return pts[0][1]
    for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
        if x <= x1: return lerp(y0, y1, (x - x0) / (x1 - x0))
    return pts[-1][1]

PAL = {
    'Skin_Male': ('#DBA182', .62), 'Skin_Female': ('#E0A98C', .6),
    'Lips_Male': ('#B0705F', .55), 'Lips_Female': ('#BC6E64', .5),
    'Hair': ('#672E1D', .72), 'Hair_Tie': ('#2A2422', .6), 'Brow': ('#3C1E14', .8),
    'Eye_White': ('#E9E3DA', .4), 'Iris': ('#4A2E1E', .3), 'Pupil': ('#140C08', .2), 'Lash': ('#24160F', .7),
    'Skin_Shadow': ('#C38A6E', .65), 'Lips_Lower': ('#BF7E6B', .55),
    'Tee_Olive': ('#53584A', .9), 'Skull_Print': ('#D3C8AA', .85), 'Undershirt_Rust': ('#9A5343', .9),
    'Denim': ('#39455F', .88), 'Denim_Faded': ('#404C67', .9), 'Denim_Rip': ('#B8A08A', .9),
    'Boot_Leather': ('#5A3D2B', .6), 'Boot_Sole': ('#2A211B', .8), 'Boot_Lace': ('#33241A', .8),
    'Backpack': ('#2D3344', .8), 'Backpack_Strap': ('#232835', .8), 'Backpack_Hardware': ('#15171C', .45),
    'Shirt_White': ('#E6E6E8', .75), 'Vest_Gray': ('#A0A1A4', .92), 'Vest_Trim': ('#343946', .8),
    'Tie_Navy': ('#273050', .6), 'Crest_Navy': ('#233462', .6), 'Crest_Gold': ('#DDB33A', .5),
    'Crest_Green': ('#4D8844', .6), 'Lanyard_Blue': ('#2B6BB5', .7), 'Lanyard_Yellow': ('#E5C03A', .5),
    'ID_Card': ('#F0F0EA', .5), 'ID_Photo': ('#9BB5D0', .6), 'ID_Band': ('#E6C345', .6), 'ID_Bar': ('#B9BCC2', .6),
    'Trousers_Navy': ('#2B3350', .88), 'Skirt_Navy': ('#2D3555', .88), 'Cloth_Inner': ('#1A1F30', .95),
    'Shoe_Black': ('#1A1A1C', .35), 'Shoe_Sole': ('#0E0E0F', .7),
}

def srgb(h):
    h = h.lstrip('#'); c = [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    return tuple((x / 12.92) if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4 for x in c) + (1.0,)

def get_mat(name):
    m = bpy.data.materials.get(name)
    if m: return m
    col, rough = PAL[name]
    m = bpy.data.materials.new(name)
    try: m.use_nodes = True
    except Exception: pass
    b = m.node_tree.nodes.get('Principled BSDF')
    b.inputs['Base Color'].default_value = srgb(col)
    b.inputs['Roughness'].default_value = rough
    m.diffuse_color = srgb(col)
    m.use_backface_culling = True
    return m

# ------------------------------------------------------------------ mesh builder
class MB:
    def __init__(s):
        s.v, s.f, s.fm, s.w, s.mats = [], [], [], [], []
        s.mirror = False; s.wfn = None
    def mi(s, m):
        if m not in s.mats: s.mats.append(m)
        return s.mats.index(m)
    def add(s, p):
        p = V(p); w = s.wfn(p) if s.wfn else {}
        if s.mirror: p = mx(p); w = {swap(k): x for k, x in w.items()}
        s.v.append(p); s.w.append(w); return len(s.v) - 1
    def face(s, idx, m):
        idx = list(idx)
        if s.mirror: idx = idx[::-1]
        s.f.append(idx); s.fm.append(s.mi(m))
    def ring(s, pts): return [s.add(p) for p in pts]
    def bridge(s, a, b, m, skip=None):
        n = len(a)
        for i in range(n):
            if skip and skip(i): continue
            j = (i + 1) % n
            s.face([a[i], a[j], b[j], b[i]], m(i) if callable(m) else m)
    def cap(s, r, m, end=False, center=None):
        if center is None:
            s.face(r if end else r[::-1], m); return
        c = s.add(center); n = len(r)
        for i in range(n):
            j = (i + 1) % n
            s.face([r[i], r[j], c] if end else [r[j], r[i], c], m)
    def merge(s, o):
        off = len(s.v)
        s.v += o.v; s.w += o.w
        for f, m in zip(o.f, o.fm):
            s.f.append([i + off for i in f]); s.fm.append(s.mi(o.mats[m]))
    def bvh(s): return BVHTree.FromPolygons([tuple(p) for p in s.v], s.f)

def frame_for(d, ref=V((0, -1, 0))):
    d = d.normalized()
    if abs(d.dot(ref)) > 0.95: ref = V((0, 0, 1))
    u = (ref - d * ref.dot(d)).normalized()
    return u, d.cross(u)        # u x w == d  -> rings CCW around d

def ring_pts(c, d, ru, rw, n, ref=V((0, -1, 0)), phase=0.0, e=1.0, jit=None):
    u, w = frame_for(d, ref); out = []
    for i in range(n):
        t = 2 * math.pi * i / n + phase
        k = 1.0 + (jit(i) if jit else 0.0)
        out.append(c + u * (ru * se(math.cos(t), e) * k) + w * (rw * se(math.sin(t), e) * k))
    return out

def tube(mb, chain, specs, n, mat, ref=V((0, -1, 0)), cap0=True, cap1=True, e=1.0, jit=None):
    """chain(s)->point; specs: [(s, ru, rw[, mat_of_next_band])]; returns ring index lists."""
    rings = []
    for sp in specs:
        s = sp[0]; c = chain(s)
        d = (chain(s + 0.04) - chain(s - 0.04))
        rings.append(mb.ring(ring_pts(c, d, sp[1], sp[2], n, ref, e=e, jit=(lambda i, s=s: jit(i, s)) if jit else None)))
    for k in range(len(rings) - 1):
        m = specs[k][3] if len(specs[k]) > 3 else mat
        mb.bridge(rings[k], rings[k + 1], m)
    if cap0: mb.cap(rings[0], specs[0][3] if len(specs[0]) > 3 else mat, end=False, center=chain(specs[0][0]))
    if cap1: mb.cap(rings[-1], mat, end=True, center=chain(specs[-1][0]))
    return rings

def polyline(pts):
    """Arc-length chain over a point list, s in [0,1], clamped/extrapolated."""
    pts = [V(p) for p in pts]; L = [0.0]
    for a, b in zip(pts, pts[1:]): L.append(L[-1] + (b - a).length)
    tot = L[-1]
    def f(s):
        x = s * tot
        if x <= 0: return pts[0] + (pts[1] - pts[0]).normalized() * x
        if x >= tot: return pts[-1] + (pts[-1] - pts[-2]).normalized() * (x - tot)
        for i in range(len(pts) - 1):
            if x <= L[i + 1]: return pts[i].lerp(pts[i + 1], (x - L[i]) / max(L[i + 1] - L[i], 1e-9))
    return f

def resample(pts, n):
    f = polyline(pts); return [f(i / (n - 1)) for i in range(n)]

def torso_ring(z, w, df, db, n=16, cy=0.0, e=0.8, jit=None):
    out = []
    for i in range(n):
        t = 2 * math.pi * i / n; c, s_ = math.cos(t), math.sin(t)
        k = 1 + (jit(i) if jit else 0)
        out.append(V((w * se(c, e) * k, cy + (db if s_ > 0 else df) * se(s_, e) * k, z)))
    return out      # i=0 +X (left), 4 back, 8 right, 12 front

def ribbon(mb, pts, nrm, width, thick, mat, taper=None, caps=True):
    """Flat strap lying on a surface. pts/nrm lists; width may be a function of t."""
    rings = []; n = len(pts)
    for i, (p, nn) in enumerate(zip(pts, nrm)):
        t = (pts[min(i + 1, n - 1)] - pts[max(i - 1, 0)]).normalized()
        nn = (nn - t * nn.dot(t)).normalized(); sd = nn.cross(t)
        h = (width(i / (n - 1)) if callable(width) else width) / 2
        rings.append(mb.ring([p + sd * h, p + sd * h + nn * thick, p - sd * h + nn * thick, p - sd * h]))
    for a, b in zip(rings, rings[1:]): mb.bridge(a, b, mat)
    if caps: mb.cap(rings[0], mat); mb.cap(rings[-1], mat, end=True)
    return rings

def on_surface(bvh, pts, gap):
    out, nr = [], []
    for p in pts:
        loc, n, _, _ = bvh.find_nearest(p)
        out.append(loc + n * gap); nr.append(n)
    return out, nr

def decal(mb, polys, bvh, mat, gap=0.003, dirn=V((0, 1, 0)), maxe=0.02, ref_up=V((0, 0, 1))):
    """polys: [outline, hole, ...] 2D (a,b) in plane perpendicular to dirn; projected along dirn."""
    u = ref_up.cross(dirn).normalized() * -1      # for dirn=+Y: u=+X
    if dirn.y < 0: u = -u
    w = ref_up
    pts3 = [[V((0, 0, 0)) + u * a + w * b for a, b in poly] for poly in polys]
    flat = [p for poly in pts3 for p in poly]
    tris = tessellate_polygon(pts3)
    verts = list(flat); mid = {}
    def midp(i, j):
        k = (min(i, j), max(i, j))
        if k not in mid: verts.append((verts[i] + verts[j]) / 2); mid[k] = len(verts) - 1
        return mid[k]
    out = []
    stack = [tuple(t) for t in tris]
    while stack:
        a, b, c = stack.pop()
        if max((verts[a] - verts[b]).length, (verts[b] - verts[c]).length, (verts[c] - verts[a]).length) > maxe:
            ab, bc, ca = midp(a, b), midp(b, c), midp(c, a)
            stack += [(a, ab, ca), (ab, b, bc), (ca, bc, c), (ab, bc, ca)]
        else: out.append((a, b, c))
    proj = {}
    def P(i):
        if i not in proj:
            o = verts[i] - dirn * 1.0
            loc, n, _, _ = bvh.ray_cast(o, dirn)
            if loc is None: loc, n, _, _ = bvh.find_nearest(verts[i])
            proj[i] = (loc - dirn * gap, n)
        return proj[i]
    idx = {}
    for a, b, c in out:
        pa, pb, pc = P(a)[0], P(b)[0], P(c)[0]
        nf = (pb - pa).cross(pc - pa)
        order = (a, b, c) if nf.dot(dirn) < 0 else (a, c, b)
        for i in order:
            if i not in idx: idx[i] = mb.add(P(i)[0])
        mb.face([idx[i] for i in order], mat)

def raised_decal(mb, outline, bvh, mat, h, dirn=V((0, 1, 0)), sink=0.0015):
    """Outline (x,z) projected along dirn, extruded by h along the surface normal (brows, lashes, pockets)."""
    base, top = [], []
    for a, b in outline:
        o = V((a, 0, b)) - dirn * 1.0
        loc, n, _, _ = bvh.ray_cast(o, dirn)
        if loc is None: loc, n, _, _ = bvh.find_nearest(V((a, 0, b)))
        base.append(loc - n * sink); top.append(loc + n * h)
    c = sum(top, V()) / len(top); cb = sum(base, V()) / len(base)
    out = (c - cb).normalized()
    def put(ps, outward):
        nf = (ps[1] - ps[0]).cross(ps[2] - ps[0])
        if nf.dot(outward) < 0: ps = ps[::-1]
        mb.face([mb.add(p) for p in ps], mat)
    n = len(top)
    for i in range(n):
        j = (i + 1) % n
        put([top[i], top[j], c], out)
        mid = (top[i] + top[j] + base[i] + base[j]) / 4
        put([base[i], base[j], top[j], top[i]], (mid - cb) - out * (mid - cb).dot(out))

def prism(mb, outline, base_o, nrm, up, depth, mat, back_mat=None, center_dip=0.0):
    """Extrude a 2D outline (a,b) in plane(base_o, side=up x nrm, up) along nrm."""
    side = up.cross(nrm).normalized()
    bot = [base_o + side * a + up * b for a, b in outline]
    top = [p + nrm * depth for p in bot]
    rb, rt = mb.ring(bot), mb.ring(top)
    # outline assumed CCW when viewed from +nrm
    mb.bridge(rb, rt, mat)
    c = sum(top, V()) / len(top) + nrm * (-center_dip)
    mb.cap(rt, mat, end=True, center=c)
    mb.cap(rb, back_mat or mat, end=False)
    return rb, rt

def box(mb, c, ax, ay, az, hx, hy, hz, mat):
    """Oriented box: axes ax,ay,az (unit), half sizes."""
    cs = [c + ax * (sx * hx) + ay * (sy * hy) + az * (sz * hz) for sz in (-1, 1) for sy in (-1, 1) for sx in (-1, 1)]
    ids = [mb.add(p) for p in cs]
    F = [(0, 2, 3, 1), (4, 5, 7, 6), (0, 1, 5, 4), (2, 6, 7, 3), (0, 4, 6, 2), (1, 3, 7, 5)]
    if ax.cross(ay).dot(az) < 0: F = [f[::-1] for f in F]
    for f in F: mb.face([ids[i] for i in f], mat)

# ------------------------------------------------------------------ proportions / skeleton
def params(sex):
    if sex == 'M':
        return dict(sex='M', s=1.0, pelvis=(0.965, 1.05), spine_top=1.20, chest_top=1.42,
            neck=(V((0, 0.012, 1.462)), V((0, 0.008, 1.576))), head_top=1.78,
            head_c=V((0, -0.006, 1.631)), head_dims=(0.085, 0.104, 0.112, 0.128, 0.114),
            clav0=V((0.022, 0.0, 1.432)), shoulder=V((0.182, 0.006, 1.425)),
            arm_angle=30, upper=0.285, fore=0.255, hand=0.18, elbow_bend=8,
            hip=V((0.092, 0.0, 0.93)), knee=V((0.097, -0.012, 0.50)), ankle=V((0.102, 0.02, 0.085)),
            ball=V((0.106, -0.105, 0.022)), toe=V((0.108, -0.175, 0.02)))
    return dict(sex='F', s=0.92, pelvis=(0.905, 0.98), spine_top=1.115, chest_top=1.325,
        neck=(V((0, 0.012, 1.365)), V((0, 0.008, 1.478))), head_top=1.665,
        head_c=V((0, -0.006, 1.533)), head_dims=(0.079, 0.101, 0.109, 0.13, 0.124),
        clav0=V((0.02, 0.0, 1.338)), shoulder=V((0.158, 0.006, 1.332)),
        arm_angle=35, upper=0.262, fore=0.232, hand=0.165, elbow_bend=8,
        hip=V((0.088, 0.0, 0.868)), knee=V((0.091, -0.012, 0.468)), ankle=V((0.095, 0.018, 0.08)),
        ball=V((0.099, -0.098, 0.021)), toe=V((0.1, -0.162, 0.019)))

def joints(P):
    J = {}
    z0, z1 = P['pelvis']
    J['root'] = (V((0, 0, 0)), V((0, 0.25, 0)))
    J['pelvis'] = (V((0, 0, z0)), V((0, 0, z1)))
    J['spine'] = (V((0, 0, z1)), V((0, 0, P['spine_top'])))
    J['chest'] = (V((0, 0, P['spine_top'])), V((0, 0, P['chest_top'])))
    J['neck'] = P['neck']
    J['head'] = (P['neck'][1], V((0, P['neck'][1].y, P['head_top'])))
    a, b = math.radians(P['arm_angle']), math.radians(P['elbow_bend'])
    S = P['shoulder']; d1 = V((math.sin(a), 0, -math.cos(a)))
    d2 = V((math.sin(a) * math.cos(b), -math.sin(b), -math.cos(a) * math.cos(b)))
    E = S + d1 * P['upper']; Wr = E + d2 * P['fore']
    L = {'clavicle': (P['clav0'], S), 'upper_arm': (S, E), 'forearm': (E, Wr), 'hand': (Wr, Wr + d2 * P['hand']),
         'thigh': (P['hip'], P['knee']), 'shin': (P['knee'], P['ankle']), 'foot': (P['ankle'], P['ball']),
         'toe': (P['ball'], P['toe'])}
    for k, (h, t) in L.items():
        J[k + '.L'] = (h.copy(), t.copy()); J[k + '.R'] = (mx(h), mx(t))
    return J

def seg_dist(p, a, b):
    ab = b - a; t = max(0.0, min(1.0, (p - a).dot(ab) / ab.length_squared))
    return (p - (a + ab * t)).length

def wfun(J, cands, power=4):
    """cands: [(bone, radius, mask_fn|None)] -> envelope weights, max 4 influences."""
    def f(p):
        ws = {}
        for b, r, mask in cands:
            w = (r / max(seg_dist(p, *J[b]), 1e-3)) ** power
            if mask: w *= mask(p)
            if w > 1e-6: ws[b] = ws.get(b, 0) + w
        if not ws: return {cands[0][0]: 1.0}
        top = sorted(ws.items(), key=lambda kv: -kv[1])[:4]
        tot = sum(v for _, v in top); top = [(k, v / tot) for k, v in top if v / tot > 0.03]
        tot = sum(v for _, v in top); return {k: v / tot for k, v in top}
    return f

def fixed(b): return lambda p: {b: 1.0}

def arm_chain(J):
    S, E = J['upper_arm.L']; _, Wr = J['forearm.L']; _, Ht = J['hand.L']
    def f(s):
        if s <= 1: return S + (E - S) * s
        if s <= 2: return E + (Wr - E) * (s - 1)
        return Wr + (Ht - Wr) * (s - 2)
    return f

def leg_chain(J):
    H, K = J['thigh.L']; _, A = J['shin.L']
    return lambda s: H + (K - H) * s if s <= 1 else K + (A - K) * (s - 1)

# weight presets ------------------------------------------------------------
def W_torso(J, P):
    hz = J['thigh.L'][0].z; sz = P['shoulder'].z
    arm_m = lambda p: ss(sz - 0.08, sz - 0.03, p.z) * ss(sz + 0.03, sz - 0.005, p.z) * ss(0.11, 0.17, abs(p.x))
    L = [('pelvis', .14, None), ('spine', .14, None), ('chest', .16, None),
         ('neck', .045, lambda p: ss(sz + 0.02, sz + 0.06, p.z))]
    for sd, sg in (('L', 1), ('R', -1)):
        L += [('clavicle.' + sd, .05, lambda p, sg=sg: ss(sz - 0.04, sz + 0.02, p.z) * ss(0.0, 0.05, sg * p.x)),
              ('upper_arm.' + sd, .05, lambda p, sg=sg, m=arm_m: m(p) * (sg * p.x > 0)),
              ('thigh.' + sd, .085, lambda p, sg=sg: ss(hz + 0.07, hz - 0.03, p.z) * ss(-0.04, 0.04, sg * p.x))]
    return wfun(J, L)

def W_arm(J, P):
    S, E = J['upper_arm.L']; sz = S.z; d1 = (E - S).normalized(); ul = (E - S).length
    def up_m(p):   # sleeve-root vertices above the joint ride the clavicle, not the swinging arm
        t = (p - S).dot(d1) / ul
        return 1 - 0.75 * ss(-0.01, 0.035, p.z - S.z) * ss(0.28, 0.04, t)
    return wfun(J, [('chest', .06, lambda p: ss(0.17, 0.12, p.x)), ('clavicle.L', .06, lambda p: ss(sz - 0.03, sz + 0.02, p.z)),
                    ('upper_arm.L', .06, up_m), ('forearm.L', .055, None), ('hand.L', .04, None)])

def W_leg(J, P):
    hz = J['thigh.L'][0].z
    return wfun(J, [('pelvis', .13, None), ('thigh.L', .085, lambda p: ss(hz + 0.07, hz - 0.03, p.z)),
                    ('shin.L', .075, None), ('foot.L', .05, lambda p: ss(0.11, 0.07, p.z))])

def W_foot(J, P):
    return wfun(J, [('shin.L', .06, lambda p: ss(0.08, 0.14, p.z)), ('foot.L', .07, None), ('toe.L', .045, None)], power=6)

def W_neck(J, P):
    hz = J['head'][0].z
    return wfun(J, [('neck', .05, None), ('head', .05, lambda p: ss(hz - 0.03, hz + 0.01, p.z)),
                    ('chest', .06, lambda p: ss(J['neck'][0].z + 0.02, J['neck'][0].z - 0.03, p.z))])

def W_skirt(J, P, ztop, zhem):
    def f(p):
        h = ss(ztop - 0.06, zhem, p.z); infl = 0.95 * h ** 0.75
        side = ss(-0.07, 0.07, p.x)
        w = {'pelvis': 1 - infl, 'thigh.L': infl * side, 'thigh.R': infl * (1 - side)}
        return {k: v for k, v in w.items() if v > 0.02}
    return f
