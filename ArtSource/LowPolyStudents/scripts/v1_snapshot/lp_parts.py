"""Body parts, hair, clothing and accessories (exec'd after lp_build.py)."""

# ================================================================== HEAD
HEAD_LATS = [-82, -68, -55, -42, -30, -15, 0, 12, 24, 38, 52, 67, 81]
HEAD_NLON = 24
HEAD_TILT = Quaternion((1, 0, 0), math.radians(8))   # face slightly down: neutral gaze

def head_pt(P, th, ph):
    W, DF, DB, UP, DN = P['head_dims']; fem = P['sex'] == 'F'
    t, f = math.radians(th), math.radians(ph)
    hx = se(math.sin(t), 0.8) * se(math.cos(f), 0.85)
    hy = -se(math.cos(t), 0.8) * se(math.cos(f), 0.85)
    hz = se(math.sin(f), 0.9)
    x = hx * W; y = hy * (DF if hy < 0 else DB); z = hz * (UP if hz > 0 else DN)
    if hz < 0:
        k = -hz
        x *= 1 - (0.36 if fem else 0.2) * k ** 1.8
        if y > 0: y *= 1 - 0.62 * k ** 1.1
        else: y *= 1 - 0.10 * k ** 2
        if not fem:   # flatter under-jaw at front/sides; skull base rises toward the neck at the back
            z = -DN * se(k, 0.75) * lerp(1.0, 0.55, ss(100, 160, abs(th)))
    if y > 0: y *= 1 + 0.06 * max(0.0, 1 - abs(hz - 0.3) * 2.2)
    if y < 0 and hz > 0.35: y *= 1 - 0.12 * (hz - 0.35)
    def g(t0, p0, st, sp): return math.exp(-((th - t0) / st) ** 2 - ((ph - p0) / sp) ** 2)
    dx = dy = dz = 0.0
    for sg in (1, -1):
        dy += 0.007 * g(sg * 22, 1, 9, 7)                        # eye sockets
        dy -= (0.0035 if fem else 0.008) * g(sg * 24, 13, 15, 5)  # brow ridge
        dx += sg * 0.005 * g(sg * 50, -10, 12, 12); dy -= 0.003 * g(sg * 50, -10, 12, 12)  # cheekbones
        dy += 0.003 * g(sg * 42, -32, 10, 9)                      # cheek hollow
        dy -= 0.006 * g(sg * 15, -15, 6, 6)                       # nostril wings
        if not fem:
            dx += sg * 0.01 * g(sg * 72, -52, 14, 12); dz -= 0.012 * g(sg * 80, -60, 14, 12); dy += 0.006 * g(sg * 82, -60, 14, 12)   # jaw angle
            dy -= 0.004 * g(sg * 15, -64, 8, 8)                  # square chin corners
    dy -= (0.019 if fem else 0.024) * g(0, -14, 7, 9)            # nose tip
    dy -= (0.008 if fem else 0.010) * g(0, 1, 7, 9)              # nose bridge
    dy -= (0.006 if fem else 0.004) * g(0, -30, 18, 6)           # lips
    dy -= (0.010 if fem else 0.013) * g(0, -64, 22, 10); dz -= 0.005 * g(0, -66, 22, 10)  # chin
    return HEAD_TILT @ V((x + dx, y + dy, z + dz))

def build_head(P, J, skin, lips):
    mb = MB(); mb.wfn = fixed('head'); C = P['head_c']
    rings = []
    for ph in HEAD_LATS:
        rings.append(mb.ring([C + head_pt(P, -180 + 360 * i / HEAD_NLON, ph) for i in range(HEAD_NLON)]))
    # th sweeps back -> right -> front -> left: CCW around +Z
    for a, b in zip(rings, rings[1:]): mb.bridge(a, b, skin)
    mb.cap(rings[0], skin, end=False, center=C + head_pt(P, 0, -90) + V((0, 0.012, 0)))
    mb.cap(rings[-1], skin, end=True, center=C + head_pt(P, 0, 90))
    bv = mb.bvh(); fem = P['sex'] == 'F'
    # ---- face features (left side authored, mirrored)
    hs = 1.1
    ez = C.z + 0.003 * hs; ex = 0.034 * hs; es = 1.08 * hs
    for mir in (False, True):
        mb.mirror = mir
        E = lambda a, b: (ex + a * es, ez + b * es)
        eye = [E(-0.0135, 0.0), E(-0.007, 0.0052), E(0.004, 0.0062), E(0.0138, 0.0018), E(0.0065, -0.0038), E(-0.006, -0.0040)]
        decal(mb, [eye], bv, 'Eye_White', gap=0.0010, maxe=0.008)
        circ = lambda r, cx, cz, n=6: [E(cx + r * math.cos(k * 2 * math.pi / n), cz + r * math.sin(k * 2 * math.pi / n)) for k in range(n)]
        decal(mb, [circ(0.0058, 0.0005, 0.0006)], bv, 'Iris', gap=0.0018, maxe=0.01)
        decal(mb, [circ(0.0026, 0.0005, 0.0006)], bv, 'Pupil', gap=0.0024, maxe=0.01)
        decal(mb, [circ(0.0011, 0.0024, 0.0026, 4)], bv, 'Eye_White', gap=0.0029, maxe=0.01)
        lash = [E(-0.0148, 0.0003), E(-0.007, 0.0066), E(0.004, 0.0078), E(0.0156, 0.0026), E(0.0138, 0.0012), E(0.004, 0.0058), E(-0.007, 0.0046)]
        raised_decal(mb, lash, bv, 'Lash', 0.0012)
        lid = [E(-0.013, 0.0095), E(-0.004, 0.0125), E(0.008, 0.0118), E(0.0145, 0.0062), E(0.008, 0.0098), E(-0.004, 0.0104)]
        decal(mb, [lid], bv, 'Skin_Shadow', gap=0.0009, maxe=0.01)
        by = ez + 0.02 * hs
        brow = [(ex - 0.021, by - 0.0045), (ex - 0.021, by + 0.003), (ex - 0.002, by + 0.0072), (ex + 0.016, by + 0.0045),
                (ex + 0.025, by - 0.003), (ex + 0.013, by - 0.0005), (ex - 0.004, by - 0.0015)]
        raised_decal(mb, brow, bv, 'Brow', 0.0035)
    mb.mirror = False
    mz = C.z - 0.062 * hs; mw = 0.019 * hs
    lipup = [(-mw, mz), (-0.007, mz + 0.0042), (0.0, mz + 0.0032), (0.007, mz + 0.0042), (mw, mz), (0.0, mz - 0.0005)]
    liplo = [(-mw, mz), (0.0, mz - 0.0005), (mw, mz), (0.009, mz - 0.0058), (-0.009, mz - 0.0058)]
    decal(mb, [lipup], bv, lips, gap=0.0012, maxe=0.01)
    decal(mb, [liplo], bv, 'Skin_Shadow', gap=0.0011, maxe=0.01)
    decal(mb, [[(-mw + 0.002, mz + 0.0006), (mw - 0.002, mz + 0.0006), (mw - 0.002, mz - 0.0007), (-mw + 0.002, mz - 0.0007)]], bv, 'Lash', gap=0.0019, maxe=0.01)
    # ---- ears: rim with a sunken concha
    for mir in (False, True):
        mb.mirror = mir
        base = C + head_pt(P, 90, -5); base.x -= 0.006
        ol = [(0.019, 0.027), (0.002, 0.034), (-0.014, 0.024), (-0.017, 0.0), (-0.009, -0.028), (0.005, -0.032), (0.014, -0.014), (0.018, 0.008)]
        nrm = V((1, 0.28, 0)).normalized(); up = V((0, 0, 1))
        prism(mb, ol, base + V((0, 0.012, 0)), nrm, up, 0.018, skin, center_dip=0.009)
    mb.mirror = False
    return mb

def build_neck(P, J, skin):
    mb = MB(); mb.wfn = W_neck(J, P)
    n0, n1 = J['neck']; s = P['s']
    ch = polyline([n0 + V((0, 0, -0.045)), n1 + V((0, -0.005, 0.035))])
    tube(mb, ch, [(0.0, 0.07 * s, 0.078 * s), (0.3, 0.056 * s, 0.058 * s), (0.75, 0.053 * s, 0.054 * s), (1.0, 0.05 * s, 0.052 * s)],
         8, skin, ref=V((0, -1, 0)))
    return mb

# ================================================================== HANDS / ARMS
def build_hand(P, J, skin):
    mb = MB(); mb.wfn = wfun(J, [('hand.L', .05, None), ('forearm.L', .03, None)], power=6)
    Wr, Ht = J['hand.L']; s = P['s'] * (1.03 if P['sex'] == 'F' else 1.2)
    A = (Ht - Wr).normalized(); U = V((0, -1, 0)); U = (U - A * U.dot(A)).normalized(); N = U.cross(A)
    def L(u, n, a): return Wr + U * (u * s) + N * (n * s) + A * (a * s)
    for mir in (False, True):
        mb.mirror = mir
        rings = []
        for a, hw, ht, uo in [(-0.025, 0.024, 0.017, 0.0), (0.0, 0.027, 0.017, 0.0), (0.04, 0.04, 0.016, 0.002), (0.088, 0.042, 0.013, 0.0)]:
            c = L(uo, 0.0, a)
            pts = [c + N * (ht * s * math.cos(2 * math.pi * k / 6 + math.pi / 6)) + U * (hw * s * math.sin(2 * math.pi * k / 6 + math.pi / 6)) for k in range(6)]
            rings.append(mb.ring(pts))
        for r0, r1 in zip(rings, rings[1:]): mb.bridge(r0, r1, skin)
        mb.cap(rings[0], skin); mb.cap(rings[-1], skin, end=True, center=L(0, 0, 0.092))
        fingers = [(0.03, (0.046, 0.038), 0.0098), (0.0105, (0.051, 0.043), 0.0102), (-0.0095, (0.048, 0.04), 0.0098), (-0.029, (0.038, 0.032), 0.0088)]
        for u0, (l1, l2), r in fingers:
            p0 = L(u0, -0.001, 0.08)
            d1 = (A * math.cos(math.radians(14)) - N * math.sin(math.radians(14))).normalized()
            d2 = (A * math.cos(math.radians(38)) - N * math.sin(math.radians(38))).normalized()
            p1 = p0 + d1 * (l1 * s); p2 = p1 + d2 * (l2 * s)
            fr = []
            for c, d, rr in [(p0, d1, r), (p1, (d1 + d2).normalized(), r * 0.92), (p2, d2, r * 0.72)]:
                fr.append(mb.ring([c + N * (rr * s * math.cos(2 * math.pi * k / 4)) + d.cross(N).normalized() * (rr * s * math.sin(2 * math.pi * k / 4)) for k in range(4)]))
            # ring basis (N, d x N): N x (d x N) = d  -> CCW around d
            mb.bridge(fr[0], fr[1], skin); mb.bridge(fr[1], fr[2], skin)
            mb.cap(fr[2], skin, end=True, center=p2 + d2 * (0.006 * s))
        tb = L(0.03, -0.006, 0.015)
        td = (A * 0.6 + U * 0.66 - N * 0.42).normalized(); td2 = (A * 0.85 + U * 0.35 - N * 0.4).normalized()
        t1 = tb + td * (0.038 * s); t2 = t1 + td2 * (0.032 * s)
        tr = []
        for c, d, rr in [(tb, td, 0.012), (t1, (td + td2).normalized(), 0.0105), (t2, td2, 0.0078)]:
            tr.append(mb.ring([c + N * (rr * s * math.cos(2 * math.pi * k / 4)) + d.cross(N).normalized() * (rr * s * math.sin(2 * math.pi * k / 4)) for k in range(4)]))
        mb.bridge(tr[0], tr[1], skin); mb.bridge(tr[1], tr[2], skin)
        mb.cap(tr[0], skin); mb.cap(tr[2], skin, end=True, center=t2 + td2 * (0.006 * s))
    mb.mirror = False
    return mb

def build_arm_layer(P, J, specs, mat, n=8, cap0=False, cap1=False):
    """specs along arm chain s: 0 shoulder, 1 elbow, 2 wrist. Both sides."""
    mb = MB(); mb.wfn = W_arm(J, P); ch = arm_chain(J); k = P['s']
    for mir in (False, True):
        mb.mirror = mir
        tube(mb, ch, [(sp[0], sp[1] * k, sp[2] * k) + tuple(sp[3:]) for sp in specs], n, mat, cap0=cap0, cap1=cap1)
    mb.mirror = False
    return mb

def build_leg_layer(P, J, specs, mat, n=10, cap0=False, cap1=False, matfn=None, jit=None):
    mb = MB(); mb.wfn = W_leg(J, P); ch = leg_chain(J); k = P['s']
    for mir in (False, True):
        mb.mirror = mir
        sp2 = []
        for sp in specs:
            sp2.append((sp[0], sp[1] * k, sp[2] * k) + ((matfn(sp[0]),) if matfn else tuple(sp[3:])))
        tube(mb, ch, sp2, n, mat, cap0=cap0, cap1=cap1, jit=jit)
    mb.mirror = False
    return mb

def denim_jit(seed):
    rnd = random.Random(seed); cache = {}
    def f(i, s):
        k = (i, round(s, 3))
        if k not in cache:
            fold = 0.07 * ss(1.6, 1.85, s) * (1 if i % 2 else -0.6)          # stacked hem
            knee = 0.04 * math.exp(-((s - 1.0) / 0.12) ** 2) * (i % 3 == 0)    # knee creases
            cache[k] = rnd.uniform(-0.035, 0.035) + fold + knee
        return cache[k]
    return f

# ================================================================== TORSO GARMENTS
def build_torso(P, J, rings, mats, n=16, cap_bottom=False, wfn=None, jit=None):
    """rings: [(z,w,df,db)], mats[i] for band between ring i and i+1."""
    mb = MB(); mb.wfn = wfn or W_torso(J, P)
    R = [mb.ring(torso_ring(z, w, df, db, n, jit=jit)) for (z, w, df, db) in rings]
    for i in range(len(R) - 1): mb.bridge(R[i], R[i + 1], mats[i] if isinstance(mats, list) else mats)
    if cap_bottom: mb.cap(R[0], mats[0] if isinstance(mats, list) else mats, center=V((0, 0, rings[0][0])))
    return mb, R

def build_vest(P, J, rings, v_first, xn, mat, trim, n=16):
    """Torso loft with a diagonal V-neck cut: rings[v_first:] are V bands k=0..K."""
    mb = MB(); mb.wfn = W_torso(J, P)
    F = 3 * n // 4
    pts = [torso_ring(z, w, df, db, n) for (z, w, df, db) in rings]
    zV, zT = rings[v_first][0], rings[-1][0]
    edge = []
    for k, ri in enumerate(range(v_first, len(rings))):
        z, w, df, db = rings[ri]
        xv = xn * ((z - zV) / (zT - zV)) ** 0.85
        for sg in (1, -1):
            c = min(1.0, (xv / w) ** (1 / 0.8)); sn = -math.sqrt(max(0.0, 1 - c * c))
            pts[ri][(F + sg * k) % n] = V((sg * xv, df * se(sn, 0.8) - 0.002 * (k == 0), z))
        edge.append(ri)
    R = [mb.ring(p) for p in pts]
    for i in range(len(R) - 1):
        if i < v_first:
            mb.bridge(R[i], R[i + 1], mat); continue
        k = i - v_first
        a, b = R[i], R[i + 1]
        for c in range(n):
            j = (c + 1) % n
            off = (c - F + n // 2) % n - n // 2          # column offset of face start from front
            # face spans columns off..off+1
            if -k <= off and off + 1 <= k: continue                    # fully inside the V
            if off == k:   # right of V (toward +X / left side of wearer) boundary face
                mb.face([a[c], a[j], b[j]], mat); continue
            if off + 1 == -k:
                mb.face([a[c], a[j], b[c]], mat); continue
            mb.face([a[c], a[j], b[j], b[c]], mat)
    # trim path: left edge top->bottom->right edge top, then around the back of the neck
    K = len(rings) - 1 - v_first
    left = [pts[v_first + k][(F + k) % n] for k in range(K, -1, -1)]
    right = [pts[v_first + k][(F - k) % n] for k in range(1, K + 1)]
    back = [pts[-1][(F + K + i) % n] for i in range(0, n - 2 * K + 1)]
    return mb, left + right, back

def build_collar(P, J, z0, rx, ry, mat, n=12, h=0.032):
    mb = MB(); mb.wfn = W_neck(J, P)
    def r(z, k): return [V((rx * k * math.cos(2 * math.pi * i / n), 0.004 + ry * k * math.sin(2 * math.pi * i / n), z)) for i in range(n)]
    a = mb.ring(r(z0, 1.0)); b = mb.ring(r(z0 + h, 0.93)); c = mb.ring(r(z0 + h, 0.8)); d = mb.ring(r(z0 - 0.004, 0.82))
    mb.bridge(a, b, mat); mb.bridge(b, c, mat); mb.bridge(c, d, mat); mb.bridge(d, a, mat)
    # collar points
    fy = -(ry + 0.004)
    for mir in (False, True):
        mb.mirror = mir
        ol = [(0.004 * P['s'], 0.0), (0.026 * P['s'], -0.045 * P['s']), (0.05 * P['s'], 0.004 * P['s'])]   # CCW, side=+X
        nrm = V((0.0, -1, 0.35)).normalized(); up = V((0, 0.35, 1)).normalized()
        prism(mb, ol, V((0.0, fy, z0 + 0.012)), nrm, up, 0.005, mat)
    mb.mirror = False
    return mb

# ================================================================== FOOTWEAR
def build_shoe(P, J, kind):
    """kind: 'boot' | 'school' | 'pump'. Left foot authored, mirrored."""
    mb = MB(); mb.wfn = W_foot(J, P)
    A = J['foot.L'][0]; s = P['s']
    if kind == 'boot':
        up, sole, mat, smat = 'Boot_Leather', 0.024, 'Boot_Leather', 'Boot_Sole'
        up, sole, mat, smat = 'Boot_Leather', 0.03, 'Boot_Leather', 'Boot_Sole'
        prof = [(0.066, 0.037, 0.09), (0.05, 0.047, 0.106), (0.0, 0.051, 0.11), (-0.05, 0.054, 0.1), (-0.1, 0.056, 0.08),
                (-0.145, 0.055, 0.068), (-0.176, 0.048, 0.06), (-0.198, 0.034, 0.05)]
        shaft = [(0.04, 0.057, 0.064), (0.1, 0.055, 0.061), (0.15, 0.06, 0.066)]
    elif kind == 'school':
        mat, smat, sole = 'Shoe_Black', 'Shoe_Sole', 0.02
        prof = [(0.06, 0.033, 0.072), (0.045, 0.041, 0.078), (0.0, 0.044, 0.08), (-0.06, 0.047, 0.07), (-0.12, 0.048, 0.055), (-0.165, 0.044, 0.046), (-0.19, 0.033, 0.04)]
        shaft = [(0.035, 0.049, 0.056), (0.088, 0.047, 0.053)]
    else:
        mat, smat, sole = 'Shoe_Black', 'Shoe_Sole', 0.012
        prof = [(0.058, 0.03, 0.062), (0.042, 0.037, 0.062), (0.005, 0.04, 0.052), (-0.05, 0.043, 0.05), (-0.115, 0.045, 0.043), (-0.155, 0.04, 0.037), (-0.178, 0.028, 0.032)]
        shaft = None
    for mir in (False, True):
        mb.mirror = mir
        rings = []
        for dy, hw, zt in prof:
            y = A.y + dy * s
            zb = sole if kind != 'pump' else (0.034 if dy > 0.02 else lerp(0.034, 0.012, ss(0.02, -0.09, dy)))
            c = V((A.x, y, (zt * s + zb) / 2)); hh = (zt * s - zb) / 2
            pts = []
            for k in range(8):
                t = 2 * math.pi * (k + 0.5) / 8
                pts.append(c + V((hw * s * se(math.cos(t), 0.55), 0, hh * se(math.sin(t), 0.55))))
            rings.append(mb.ring(pts))
        for r0, r1 in zip(rings, rings[1:]): mb.bridge(r0, r1, mat)
        mb.cap(rings[0], mat); mb.cap(rings[-1], mat, end=True)
        # sole slab
        srings = []
        for i, (dy, hw, zt) in enumerate(prof):
            y = A.y + (dy + (0.006 if i == 0 else (-0.006 if i == len(prof) - 1 else 0))) * s
            z0 = 0.0
            z1 = sole + 0.003 if kind != 'pump' else (0.04 if dy > 0.02 else lerp(0.04, 0.016, ss(0.02, -0.09, dy)))
            zb = 0.0 if kind != 'pump' else (0.0 if dy > 0.02 else lerp(0.028, 0.0, ss(0.0, -0.09, dy)))
            hw2 = hw * s + 0.004
            srings.append(mb.ring([V((A.x + hw2, y, zb)), V((A.x + hw2, y, z1)), V((A.x - hw2, y, z1)), V((A.x - hw2, y, zb))]))
        # rings ordered along -Y; ring order CCW around -Y: (+x,b)->(+x,t)->(-x,t)->(-x,b)
        for r0, r1 in zip(srings, srings[1:]): mb.bridge(r0, r1, smat)
        mb.cap(srings[0], smat); mb.cap(srings[-1], smat, end=True)
        if shaft:
            sc = V((A.x, A.y + 0.012, 0))
            ch = lambda t, sc=sc: sc + V((0, 0, t))
            sp = [(z, rx * s, ry * s) for z, rx, ry in shaft] + [(shaft[-1][0] - 0.01, shaft[-1][1] * s * 0.78, shaft[-1][2] * s * 0.78)]
            tube(mb, ch, sp, 8, mat, ref=V((0, -1, 0)), cap0=True, cap1=False)
        if kind == 'boot':   # criss-cross lace bars over the instep, padded collar band
            for i, dy in enumerate((-0.01, -0.04, -0.07)):
                y = A.y + dy * s; zt = interp([(p[0], p[2]) for p in prof[::-1]], dy) * s
                for sg in (1, -1):
                    rot = Quaternion((0, 0, 1), math.radians(sg * 22))
                    box(mb, V((A.x, y, zt + 0.0025)), rot @ V((1, 0, 0)), rot @ V((0, 1, 0)), V((0, 0, 1)), 0.019 * s, 0.0035, 0.0025, 'Boot_Lace')
            sc = V((A.x, A.y + 0.012, 0))
            tube(mb, lambda t, sc=sc: sc + V((0, 0, t)), [(0.128, 0.063 * s, 0.069 * s), (0.148, 0.063 * s, 0.069 * s)], 8, 'Boot_Sole', ref=V((0, -1, 0)), cap0=False, cap1=False)
    mb.mirror = False
    return mb

def build_foot_skin(P, J, skin):
    """Visible instep for pumps: small loft from the ankle into the shoe."""
    mb = MB(); mb.wfn = W_foot(J, P); A = J['foot.L'][0]; s = P['s']
    for mir in (False, True):
        mb.mirror = mir
        rings = []
        for dy, hw, zb, zt in [(0.03, 0.026, 0.03, 0.09), (0.0, 0.03, 0.03, 0.08), (-0.05, 0.034, 0.02, 0.052), (-0.1, 0.035, 0.015, 0.04)]:
            y = A.y + dy * s
            rings.append(mb.ring([V((A.x + hw * s, y, zb)), V((A.x + hw * s, y, zt)), V((A.x - hw * s, y, zt)), V((A.x - hw * s, y, zb))]))
        for r0, r1 in zip(rings, rings[1:]): mb.bridge(r0, r1, skin)
        mb.cap(rings[0], skin); mb.cap(rings[-1], skin, end=True)
    mb.mirror = False
    return mb
