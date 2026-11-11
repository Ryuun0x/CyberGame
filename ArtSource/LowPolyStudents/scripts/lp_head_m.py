"""Male head built from reference front/side profiles (exec'd after lp_parts.py; overrides head_pt/build_head for 'M').

Local coords: origin at eye level on the mid-plane, -Y forward, +X = his left. Metres.
Each row: z, half-width, face-plane depth (front), back depth, centre-line depth (nose/lips/chin), centre feature width.
The shape is a smooth analytic surface; the render mesh samples it on a feature-aligned grid
(rows at brow, eyes, nose tip/base, mouth, lips, chin; denser columns across the face) so the
facets are deliberate planes rather than decimation noise.
"""
MALE_PROFILE = [
    (0.121, 0.012, 0.014, 0.018, 0.014, 0.03),
    (0.116, 0.034, 0.040, 0.050, 0.040, 0.03),
    (0.108, 0.053, 0.063, 0.076, 0.063, 0.03),
    (0.096, 0.065, 0.078, 0.091, 0.078, 0.03),
    (0.078, 0.073, 0.088, 0.101, 0.088, 0.03),
    (0.058, 0.077, 0.093, 0.106, 0.093, 0.03),
    (0.040, 0.078, 0.095, 0.108, 0.096, 0.03),
    (0.024, 0.078, 0.094, 0.108, 0.099, 0.026),   # brow ridge
    (0.012, 0.077, 0.090, 0.107, 0.095, 0.016),   # nasion
    (0.000, 0.076, 0.088, 0.105, 0.098, 0.011),   # bridge between the eyes
    (-0.012, 0.076, 0.089, 0.102, 0.105, 0.012),
    (-0.024, 0.076, 0.090, 0.098, 0.112, 0.013),
    (-0.034, 0.075, 0.090, 0.093, 0.117, 0.014),  # nose tip
    (-0.042, 0.071, 0.090, 0.087, 0.108, 0.018),  # nose base / wings
    (-0.050, 0.069, 0.088, 0.079, 0.100, 0.026),  # subnasal
    (-0.060, 0.066, 0.087, 0.069, 0.099, 0.03),   # upper lip
    (-0.068, 0.064, 0.086, 0.061, 0.095, 0.032),  # mouth line
    (-0.076, 0.062, 0.085, 0.053, 0.097, 0.03),   # lower lip
    (-0.087, 0.059, 0.083, 0.042, 0.091, 0.034),  # labiomental groove
    (-0.099, 0.052, 0.080, 0.030, 0.095, 0.04),   # chin
    (-0.109, 0.042, 0.075, 0.016, 0.091, 0.042),
    (-0.116, 0.028, 0.062, 0.004, 0.075, 0.034),
    (-0.120, 0.012, 0.038, -0.006, 0.045, 0.04),
]

def _row(z):
    R = MALE_PROFILE
    if z >= R[0][0]: return R[0]
    if z <= R[-1][0]: return R[-1]
    for i in range(len(R) - 1):
        if R[i + 1][0] <= z <= R[i][0]: break
    p1, p2 = R[i], R[i + 1]; p0 = R[max(i - 1, 0)]; p3 = R[min(i + 2, len(R) - 1)]
    t = (R[i][0] - z) / (R[i][0] - R[i + 1][0]); t2, t3 = t * t, t * t * t
    return tuple(0.5 * (2 * b + (-a + c) * t + (2 * a - 5 * b + 4 * c - d) * t2 + (-a + 3 * b - 3 * c + d) * t3)
                 for a, b, c, d in zip(p0, p1, p2, p3))

def _gauss(x, z, cx, cz, sx, sz): return math.exp(-((x - cx) / sx) ** 2 - ((z - cz) / sz) ** 2)

def _male_base(t, z):
    _, w, df, db, yc, sig = _row(z)
    c, s = math.cos(t), math.sin(t)
    e = lerp(0.84, 0.72, ss(-0.02, 0.04, z)) if s < 0 else 0.9     # full rounded cheeks, flatter brow plane
    return V((w * se(c, e), (df if s < 0 else db) * se(s, e), z))

def _male_features(p):
    """Centre-line profile (nose, lips, chin) and broad facial planes."""
    x, y, z = p
    _, w, df, db, yc, sig = _row(z)
    if y >= 0: return p
    fm = ss(0.0, 0.5, -y / max(df, 1e-3))
    ax = abs(x)
    y -= (yc - df) * math.exp(-(x / sig) ** 2) * fm
    y += 0.011 * _gauss(ax, z, 0.033, 0.003, 0.016, 0.012) * fm         # eye sockets
    y -= 0.005 * _gauss(ax, z, 0.03, 0.022, 0.022, 0.008) * fm          # brow ridge
    y -= 0.006 * _gauss(ax, z, 0.016, -0.04, 0.007, 0.008) * fm         # nose wings
    y += 0.003 * _gauss(ax, z, 0.026, -0.068, 0.006, 0.006) * fm        # mouth corners
    y -= 0.003 * _gauss(ax, z, 0.048, -0.02, 0.018, 0.02) * fm          # cheekbone/cheek fullness
    x += math.copysign(0.003 * _gauss(ax, z, 0.06, -0.02, 0.016, 0.02), x)      # cheekbones out
    x -= math.copysign(0.003 * _gauss(ax, z, 0.07, 0.035, 0.02, 0.02), x) * fm  # temples
    return V((x, y, z))

def male_surface(t, z): return _male_features(_male_base(t, z))

# feature-aligned sampling grid
HEAD_ROWS = [-0.119, -0.113, -0.104, -0.094, -0.083, -0.074, -0.068, -0.061, -0.053, -0.045, -0.038, -0.03, -0.021,
             -0.011, -0.002, 0.008, 0.019, 0.03, 0.045, 0.062, 0.08, 0.097, 0.11, 0.118]
_FRONT_OFF = [0, 6, 12, 19, 27, 36, 46, 57, 69, 81, 90]
_BACK_OFF = [103, 118, 134, 150, 165, 180]
def head_columns():
    offs = sorted(set([-o for o in _FRONT_OFF + _BACK_OFF] + _FRONT_OFF + _BACK_OFF))
    offs = [o for o in offs if -180 < o <= 180]
    # t measured CCW from +X; front centre is t = 270 deg
    return sorted(math.radians((270 + o) % 360) for o in offs)

_MHEAD = {}
def male_head_mesh():
    if 'mesh' in _MHEAD: return _MHEAD['mesh']
    cols = head_columns(); verts, faces, rings = [], [], []
    for k, z in enumerate(HEAD_ROWS):
        r = []
        for t in cols: verts.append(male_surface(t, z)); r.append(len(verts) - 1)
        rings.append(r)
    n = len(cols)
    for k in range(len(rings) - 1):
        a, b = rings[k], rings[k + 1]
        for i in range(n):
            j = (i + 1) % n
            # split each quad along its shorter diagonal: keeps planes clean, no slivers
            d1 = (verts[a[i]] - verts[b[j]]).length; d2 = (verts[a[j]] - verts[b[i]]).length
            if d1 < d2: faces += [[a[i], a[j], b[j]], [a[i], b[j], b[i]]]
            else: faces += [[a[i], a[j], b[i]], [a[j], b[j], b[i]]]
    verts.append(V((0, 0.0, 0.1225))); top = len(verts) - 1
    verts.append(V((0, -0.022, -0.1215))); bot = len(verts) - 1
    for i in range(n):
        j = (i + 1) % n
        faces.append([rings[-1][i], rings[-1][j], top]); faces.append([rings[0][j], rings[0][i], bot])
    _MHEAD['mesh'] = (verts, faces)
    # smooth reference surface for hair/ear placement (dense, analytic)
    dv, df_ = [], []; dr = []
    for z in [lerp(-0.119, 0.12, i / 47) for i in range(48)]:
        r = []
        for i in range(72): dv.append(male_surface(2 * math.pi * i / 72, z)); r.append(len(dv) - 1)
        dr.append(r)
    for a, b in zip(dr, dr[1:]):
        for i in range(72):
            j = (i + 1) % 72; df_.append([a[i], a[j], b[j], b[i]])
    dv.append(V((0, 0, 0.1225))); dv.append(V((0, -0.022, -0.1215)))
    for i in range(72):
        j = (i + 1) % 72; df_.append([dr[-1][i], dr[-1][j], len(dv) - 2]); df_.append([dr[0][j], dr[0][i], len(dv) - 1])
    _MHEAD['bvh'] = BVHTree.FromPolygons([tuple(v) for v in dv], df_)
    return _MHEAD['mesh']

head_pt_generic = head_pt
def head_pt(P, th, ph):
    if P['sex'] != 'M': return head_pt_generic(P, th, ph)
    male_head_mesh()
    t, f = math.radians(th), math.radians(ph)
    d = V((math.cos(f) * math.sin(t), -math.cos(f) * math.cos(t), math.sin(f)))
    o = V((0, 0.006, 0.0))
    loc, _, _, _ = _MHEAD['bvh'].ray_cast(o, d)
    return (loc if loc is not None else o + d * 0.09)

def build_ear(mb, P, skin):
    """Ear: rim (helix) wall, top face sloping into a sunken concha. Authored on the left side."""
    C = P['head_c']
    base = C + head_pt(P, 92, -8); base.x -= 0.006
    nrm = V((1, 0.16, 0)).normalized(); up = V((0, 0.12, 1)).normalized(); up = (up - nrm * up.dot(nrm)).normalized()
    side = up.cross(nrm).normalized()            # ~ +Y (towards the back of the head)
    o = base + V((0, 0.014, 0))
    ol = [(0.02, 0.028), (0.006, 0.035), (-0.01, 0.03), (-0.016, 0.012), (-0.014, -0.008), (-0.008, -0.026),
          (0.0, -0.035), (0.009, -0.031), (0.014, -0.016), (0.019, 0.006)]
    inner = [(a * 0.55 - 0.002, b * 0.6 + 0.002) for a, b in ol]
    P2 = lambda a, b, h: o + side * a + up * b + nrm * h
    r0 = mb.ring([P2(a, b, 0.0) for a, b in ol])
    r1 = mb.ring([P2(a, b * 0.92, 0.013 + 0.003 * (b > 0)) for a, b in ol])
    r2 = mb.ring([P2(a, b * 0.92, 0.008) for a, b in inner])
    mb.bridge(r0, r1, skin); mb.bridge(r1, r2, skin)
    mb.cap(r2, skin, end=True, center=P2(-0.002, 0.0, 0.006))
    mb.cap(r0, skin, end=False)

build_head_generic = build_head
def build_head(P, J, skin, lips):
    if P['sex'] != 'M': return build_head_generic(P, J, skin, lips)
    C = P['head_c']
    v, f = male_head_mesh()
    mb = MB(); mb.wfn = fixed('head')
    ids = [mb.add(C + p) for p in v]
    for fc in f: mb.face([ids[i] for i in fc], skin)
    bv = mb.bvh()
    add_face_features(mb, bv, C, skin, lips)
    for mir in (False, True):
        mb.mirror = mir; build_ear(mb, P, skin)
    mb.mirror = False
    return mb

def add_face_features(mb, bv, C, skin, lips):
    """Eyes (white, iris, pupil, glint), raised upper lid + lash line, crease/lower-lid shading, raised brows,
    nostril shading and lips, projected onto any head surface `bv` (front-facing, eye level at C.z)."""
    ez = C.z + 0.002; ex = 0.032; es = 1.12
    for mir in (False, True):
        mb.mirror = mir
        E = lambda a, b: (ex + a * es, ez + b * es)
        eye = [E(-0.0135, -0.0002), E(-0.0075, 0.0046), E(0.003, 0.006), E(0.0135, 0.0016), E(0.0068, -0.0034), E(-0.005, -0.004)]
        decal(mb, [eye], bv, 'Eye_White', gap=0.0009, maxe=0.012)
        circ = lambda r, cx, cz, n=8: [E(cx + r * math.cos(k * 2 * math.pi / n), cz + r * math.sin(k * 2 * math.pi / n)) for k in range(n)]
        decal(mb, [circ(0.0054, 0.0, 0.0006)], bv, 'Iris', gap=0.0016, maxe=0.02)
        decal(mb, [circ(0.0025, 0.0, 0.0006, 6)], bv, 'Pupil', gap=0.0022, maxe=0.02)
        decal(mb, [circ(0.001, 0.0018, 0.0025, 4)], bv, 'Eye_White', gap=0.0027, maxe=0.02)
        # upper eyelid: a raised skin fold over the eye, with the lash line on its lower edge
        lid = [E(-0.0155, 0.0006), E(-0.008, 0.0062), E(0.003, 0.0078), E(0.0158, 0.0026), E(0.0166, 0.0062),
               E(0.004, 0.0122), E(-0.008, 0.011), E(-0.016, 0.0048)]
        raised_decal(mb, lid, bv, skin, 0.0018)
        lash = [E(-0.0148, 0.0002), E(-0.0078, 0.0054), E(0.003, 0.0072), E(0.0152, 0.002), E(0.0136, 0.0008), E(0.003, 0.0052), E(-0.0075, 0.0038)]
        raised_decal(mb, lash, bv, 'Lash', 0.0016)
        decal(mb, [[E(-0.0145, 0.0122), E(0.004, 0.0138), E(0.0165, 0.0076), E(0.004, 0.0124)]], bv, 'Skin_Shadow', gap=0.0026, maxe=0.02)
        decal(mb, [[E(-0.0115, -0.0045), E(0.0045, -0.0052), E(0.0125, -0.0018), E(0.0045, -0.0062)]], bv, 'Skin_Shadow', gap=0.0008, maxe=0.02)
        by = ez + 0.0205
        brow = [(ex - 0.021, by - 0.004), (ex - 0.0205, by + 0.0032), (ex - 0.002, by + 0.0066), (ex + 0.015, by + 0.0044),
                (ex + 0.0235, by - 0.0022), (ex + 0.013, by - 0.0003), (ex - 0.004, by - 0.0014)]
        raised_decal(mb, brow, bv, 'Brow', 0.003)
        decal(mb, [[(0.0095, C.z - 0.0445), (0.0155, C.z - 0.042), (0.0175, C.z - 0.0455), (0.012, C.z - 0.047)]], bv, 'Skin_Shadow', gap=0.0008, maxe=0.02)
    mb.mirror = False
    mz = C.z - 0.0685; mw = 0.02
    lipup = [(-mw, mz), (-0.012, mz + 0.0033), (-0.0045, mz + 0.0056), (0.0, mz + 0.0045), (0.0045, mz + 0.0056), (0.012, mz + 0.0033), (mw, mz), (0.0, mz - 0.0004)]
    liplo = [(-mw * 0.92, mz - 0.0003), (0.0, mz - 0.0006), (mw * 0.92, mz - 0.0003), (0.013, mz - 0.0052), (0.0, mz - 0.0072), (-0.013, mz - 0.0052)]
    decal(mb, [lipup], bv, lips, gap=0.0009, maxe=0.02)
    decal(mb, [liplo], bv, 'Lips_Lower', gap=0.0008, maxe=0.02)
    decal(mb, [[(-mw + 0.003, mz + 0.0005), (mw - 0.003, mz + 0.0005), (mw - 0.003, mz - 0.0006), (-mw + 0.003, mz - 0.0006)]], bv, 'Lash', gap=0.0015, maxe=0.03)
