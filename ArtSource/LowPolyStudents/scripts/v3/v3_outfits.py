"""v3 outfits: garments cut from the weighted base body (exec'd after v3_body.py).

Each garment copies the body faces a selector picks after clean plane cuts, pushed out along the normals,
with an inward hem fold on every open edge. Garments inherit the body's skin weights, so cloth and skin
deform together and never separate.
"""

def garment(bmW, names, cuts, select, offset, mat, fold=0.006, top=False, fold_skip=None):
    """top=True: upper-body garment; thigh influence moves to the pelvis so hems don't flare with the knee."""
    b = bmW.copy()
    for co, no, region in cuts:
        if region is None: geom = b.verts[:] + b.edges[:] + b.faces[:]
        else:
            fs = [f for f in b.faces if region(f)]
            geom = list({e for f in fs for e in f.edges}) + fs + list({v for f in fs for v in f.verts})
        bmesh.ops.bisect_plane(b, geom=geom, plane_co=co, plane_no=no)
    b.normal_update()
    dl = b.verts.layers.deform.active
    faces = [f for f in b.faces if select(f)]; fs = set(faces)
    mb = MB(); vm = {}; mb.rims = []
    def nv(v):
        if v not in vm: vm[v] = mb.add(v.co + v.normal * offset(v.co, v.normal), vweights(v, dl, names))
        return vm[v]
    for f in faces:
        mb.face([nv(v) for v in f.verts], mat(f) if callable(mat) else mat)
    if fold:
        for f in faces:
            for lo in f.loops:
                e = lo.edge
                if sum(1 for g in e.link_faces if g in fs) != 1: continue
                a, c = lo.vert, lo.link_loop_next.vert
                if fold_skip and fold_skip(a.co): continue
                mb.rims.append((vm[a], vm[c], f.calc_center_median() + f.normal * offset(f.calc_center_median(), f.normal), f.normal.copy()))
                ai = mb.add(a.co - a.normal * fold, vweights(a, dl, names)); ci = mb.add(c.co - c.normal * fold, vweights(c, dl, names))
                mb.face([vm[c], vm[a], ai, ci], mat(f) if callable(mat) else mat)
    b.free()
    if top:
        for i, w in enumerate(mb.w):
            nw = {}
            for k, x in w.items():
                k = 'pelvis' if k.startswith('thigh') else k; nw[k] = nw.get(k, 0) + x
            mb.w[i] = nw
    return mb

def _seg_t(p, a, b): ab = b - a; return (p - a).dot(ab) / ab.length_squared
def seg_dist(p, a, b):
    t = max(0.0, min(1.0, _seg_t(p, a, b))); return (p - (a + (b - a) * t)).length

def arm_param(J):
    """face (or point) -> position along the arm chain (0 shoulder, 1 elbow, 2 wrist, 3 hand tip), None off the arm."""
    def arm_t(f):
        c = f if isinstance(f, V) else f.calc_center_median(); sd = 'L' if c.x > 0 else 'R'
        S, E = J['upper_arm.' + sd]; _, W = J['forearm.' + sd]; _, Ht = J['hand.' + sd]
        du, tu = seg_dist(c, S, E), _seg_t(c, S, E)
        if abs(c.x) < abs(S.x) - 0.01 and tu < 0.25: return None
        dfo, tfo = seg_dist(c, E, W), _seg_t(c, E, W)
        dh, th = seg_dist(c, W, Ht), _seg_t(c, W, Ht)
        dt = min(seg_dist(c, *J['chest']), seg_dist(c, *J['spine']))
        m = min(du, dfo, dh)
        if dh < 0.11 and dh <= min(du, dfo) + 1e-9: return 2 + max(0.0, min(1.0, th))
        if m > 0.08 or dt < m: return None
        if m == du: return max(0.0, min(1.0, tu))
        if m == dfo: return 1 + max(0.0, min(1.0, tfo))
        return 2 + max(0.0, min(1.0, th))
    return arm_t

def arm_plane(J, sd, s):
    S, E = J['upper_arm.' + sd]; _, W = J['forearm.' + sd]
    if s <= 1: return S + (E - S) * s, (E - S).normalized()
    return E + (W - E) * (s - 1), (W - E).normalized()

def leg_offset(J, rnd, base=0.011):
    """Straight trouser legs: fill the calf taper below the knee, stack a little at the hem."""
    def off(co, n):
        sd = 'L' if co.x > 0 else 'R'
        K, A = J['shin.' + sd]; H = J['thigh.' + sd][0]
        a = (K + (A - K) * _seg_t(co, K, A)) if co.z < K.z else (H + (K - H) * max(0.0, _seg_t(co, H, K)))
        r = V((co.x - a.x, co.y - a.y, 0)).length
        straight = max(0.0, 0.06 - r) * ss(0.52, 0.36, co.z)
        return base + straight + 0.004 * ss(0.2, 0.13, co.z) + rnd.uniform(-0.0015, 0.0015)
    return off

# ------------------------------------------------------------------ shoes (rigid on foot/toe): stacked footprint rings
SHOE_RINGS = {   # z, front reach, back reach, half width (from the ankle centre, metres)
    'boot': [(0.03, .185, .072, .056), (0.058, .188, .074, .058), (0.082, .165, .072, .056), (0.102, .12, .066, .051),
             (0.12, .078, .061, .047), (0.142, .06, .057, .045), (0.168, .056, .055, .044)],
    'school': [(0.026, .178, .068, .052), (0.05, .18, .07, .053), (0.068, .15, .068, .051), (0.084, .095, .062, .047),
               (0.098, .064, .058, .044)],
}
def _footprint(z, Lf, Lb, W, n=16, e=2.6):
    pts = []
    for k in range(n):
        a = 2 * math.pi * k / n; ca, sa = math.cos(a), math.sin(a)
        r = lambda c: math.copysign(abs(c) ** (2 / e), c)
        y = -(Lf if ca > 0 else Lb) * (abs(ca) ** (2 / (2.2 if ca > 0 else e))) * (1 if ca > 0 else -1)
        pts.append(V((W * r(sa), y, z)))
    return pts

def build_shoe(bmW, names, J, kind):
    up, sole_m = ('Boot', 'Boot_Sole') if kind == 'boot' else ('Shoe', 'Shoe_Sole')
    mb = MB(); spec = SHOE_RINGS[kind]
    for sd in ('L', 'R'):
        A, ball = J['foot.' + sd]; c = V((A.x, A.y - 0.01, 0))
        def w(p):
            k = ss(ball.y + 0.02, ball.y - 0.04, p.y)
            return {'foot.' + sd: 1 - k, 'toe.' + sd: k} if k > 0 else {'foot.' + sd: 1.0}
        rings = [[mb.add(c + p, w(c + p)) for p in _footprint(*r)] for r in spec]
        n = len(rings[0])
        for ri, (r0, r1) in enumerate(zip(rings, rings[1:])):
            for k in range(n):
                q = [r0[k], r0[(k + 1) % n], r1[(k + 1) % n], r1[k]]
                cen = sum((mb.v[i] for i in q), V()) / 4
                lace = kind == 'boot' and ri >= 2 and abs(cen.x - c.x) < 0.02 and cen.y < c.y
                mb.face(q, 'Lace' if lace else up)
        top = mb.add(c + V((0, 0, spec[-1][0])), {'foot.' + sd: 1.0})
        for k in range(n): mb.face([rings[-1][k], rings[-1][(k + 1) % n], top], sole_m)
        z0, Lf, Lb, W = spec[0]; h = z0
        lo_ = [mb.add(c + p, w(c + p)) for p in _footprint(0.0, Lf + 0.008, Lb + 0.008, W + 0.006)]
        hi_ = [mb.add(c + p, w(c + p)) for p in _footprint(h + 0.002, Lf + 0.008, Lb + 0.008, W + 0.006)]
        cb = mb.add(c, {'foot.' + sd: 1.0}); ct = mb.add(c + V((0, 0, h + 0.002)), {'foot.' + sd: 1.0})
        for k in range(n):
            l = (k + 1) % n
            mb.face([lo_[l], lo_[k], hi_[k], hi_[l]], sole_m); mb.face([lo_[k], lo_[l], cb], sole_m); mb.face([hi_[l], hi_[k], ct], sole_m)
        bot = mb.add(c + V((0, 0, z0 - 0.003)), {'foot.' + sd: 1.0})
        for k in range(n): mb.face([rings[0][(k + 1) % n], rings[0][k], bot], sole_m)
    return mb

# ------------------------------------------------------------------ prints (front projection windows)
PRINT_WIN = {'Tee_Print': (0.0, 1.13, 0.30), 'Vest_Print': (0.08, 1.225, 0.06), 'Shirt_Front': (0.0, 1.29, 0.28)}   # centre x, centre z, size

def cloth_uv(mat, co, n):
    if mat == 'Face': return face_uv(mat, co, n)
    x0, z0, sz = PRINT_WIN[mat]
    if n.y > -0.2: return (0.002, 0.002)
    return ((co.x - x0) / sz + 0.5, (co.z - z0) / sz + 0.5)

def paint_skull(res=512):
    x0, z0, sz = PRINT_WIN['Tee_Print']
    c = Canvas(-sz / 2, -sz / 2, sz, res, PAL['Tee'][0])
    k = 1.3; c.X /= k; c.Z /= k; c.px /= k             # skull authored at 0.15 m wide, printed 30% larger
    cream = '#DCCDB0'
    skull = np.minimum(c.ell(0, 0.022, 0.075, 0.066), c.fill([(-0.05, -0.02), (0.05, -0.02), (0.042, -0.072), (-0.042, -0.072)]))
    c.put(skull, cream)
    for sg in (1, -1):    # angry eye sockets
        c.put(c.fill([(sg * 0.012, 0.03), (sg * 0.054, 0.042), (sg * 0.05, 0.004), (sg * 0.018, 0.0)]), PAL['Tee'][0])
    c.put(c.fill([(0, 0.0), (0.011, -0.022), (-0.011, -0.022)]), PAL['Tee'][0])            # nose
    for k in range(-3, 4):    # teeth gaps
        c.put(c.seg(k * 0.0115, -0.04, k * 0.0115, -0.072, 0.0018, 0.0018), PAL['Tee'][0])
    c.put(c.seg(-0.05, -0.038, 0.05, -0.038, 0.0014, 0.0014), PAL['Tee'][0])
    # faded print: a little tee colour speckled through
    rng = np.random.default_rng(3); noise = rng.random(c.img.shape[:2]) < 0.06
    c.img[noise] = c.img[noise] * 0.7 + _hex(PAL['Tee'][0]) * 0.3
    return c

def paint_crest(res=512):
    """STI-style school badge, simplified for a ~4 cm patch: silver-edged navy shield, two cupped yellow hands
    holding a flame with 'STI' between them, silver laurels low on each side, a navy ribbon across the bottom."""
    x0, z0, sz = PRINT_WIN['Vest_Print']
    c = Canvas(-sz / 2, -sz / 2, sz, res, PAL['Vest'][0])
    sh = lambda k: [(-0.019 * k, 0.024 * k), (0.019 * k, 0.024 * k), (0.019 * k, -0.002 * k), (0.0, -0.024 * k), (-0.019 * k, -0.002 * k)]
    c.put(c.fill(sh(1.08)), '#BFC2C6')          # silver embroidered edge
    c.put(c.fill(sh(0.94)), '#1F2B58')          # navy field
    Y = '#E6D83A'; S = '#D9DBDE'
    c.put(c.poly([(-0.010, 0.014), (-0.0075, 0.004), (-0.004, -0.002), (0.004, -0.002), (0.0075, 0.004), (0.010, 0.014)], 0.0016, 0.0016), Y)
    for sg in (1, -1):    # fingertips flaring at the top of the cupped hands
        c.put(c.seg(sg * 0.010, 0.014, sg * 0.0125, 0.0175, 0.0012, 0.0008), Y)
        c.put(c.seg(sg * 0.010, 0.014, sg * 0.0085, 0.018, 0.0011, 0.0007), Y)
    c.put(c.fill([(0.0, 0.020), (0.0028, 0.0145), (0.0016, 0.010), (-0.0016, 0.010), (-0.0028, 0.0145)]), Y)   # flame
    c.put(c.seg(0.0, 0.010, 0.0, 0.006, 0.0011, 0.0011), Y)                                                   # wick / torch
    w = 0.00065; bz = 0.0035                    # 'STI' inside the cup
    c.put(np.minimum.reduce([c.poly([(-0.0028, bz + 0.0022), (-0.0047, bz + 0.0026), (-0.0047, bz + 0.0004), (-0.0028, bz),
                                     (-0.0028, bz - 0.0022), (-0.0047, bz - 0.0018)], w, w),
                             c.seg(-0.0018, bz + 0.0026, 0.0018, bz + 0.0026, w, w), c.seg(0.0, bz + 0.0026, 0.0, bz - 0.0022, w, w),
                             c.seg(0.0034, bz + 0.0026, 0.0034, bz - 0.0022, w, w)]), Y)
    for sg in (1, -1):    # laurel: a curved stem with leaves
        for k in range(5):
            a = 0.35 + k * 0.32
            lx, lz = sg * (0.0135 - 0.004 * math.cos(a) * 0.5), -0.007 + 0.0042 * k
            c.put(c.ell(lx, lz, 0.0010, 0.0019), S)
    c.put(c.fill([(-0.0235, -0.009), (0.0235, -0.009), (0.0205, -0.0165), (-0.0205, -0.0165)]), '#BFC2C6')   # ribbon edge
    c.put(c.fill([(-0.022, -0.010), (0.022, -0.010), (0.0193, -0.0155), (-0.0193, -0.0155)]), '#141B3A')
    for k in range(11):                         # suggestion of the ribbon lettering
        c.put(c.seg(-0.0155 + k * 0.0031, -0.0127, -0.0142 + k * 0.0031, -0.0127, 0.00045, 0.00045), S, 0.9)
    return c

# ------------------------------------------------------------------ outfits
def neck_ring_pts(head_bvh, z, r_extra, n=20, y0=0.033):
    pts = []
    for i in range(n):
        a = 2 * math.pi * i / n; d = V((math.sin(a), -math.cos(a), 0))
        hit, _, _, dist = head_bvh.ray_cast(V((0, y0, z)), d, 0.3)
        pts.append(V((0, y0, z)) + d * ((dist if hit is not None else 0.06) + r_extra))
    return pts

def hug_neck(mb, head_bvh, z0=1.33, z1=1.366, gap=0.0006):
    """The surface runs up the (thin) neck: from z1 up it lies on the neck, painted as skin, so the cloth edge
    is a pixel edge in the texture rather than a cut. Upper vertices take the neck's own weights."""
    ny = 0.033 + HEAD_Y * HEAD_H * 0.5
    for i, p in enumerate(mb.v):
        if p.z < z0 or (V((p.x, p.y - ny, 0)).length > 0.13 and p.z < 1.35): continue   # above 1.35 it is all neck
        d = V((p.x, p.y - ny, 0)).normalized()
        hit, _, _, dist = head_bvh.ray_cast(V((0, ny, p.z)), d, 0.3)
        if hit is None: continue
        mb.v[i] = p.lerp(V((0, ny, p.z)) + d * (dist + gap), ss(z0, z1, p.z))
        if p.z >= 1.37: mb.w[i] = head_weights(p.z)

NECK_TOP = 1.395
SKIN_EDGE = ('#7E4C38', '#C98563')       # dark-brown pixel line, then a shade, on the skin side of every fabric edge
def skin_at(d):
    """colour of skin at distance d (m) beyond a fabric edge"""
    return SKIN_EDGE[0] if d < 0.0045 else SKIN_EDGE[1] if d < 0.009 else PAL['Skin'][0]
neck_line = lambda p: 1.356 + 0.014 * ss(-0.03, 0.07, p.y)    # where the cloth stops on the neck (lower at the front)

def paint_shirt_front(res=512):
    """Shirt front inside the vest's V: collar points and the navy tie (sampled into the pixel texture)."""
    x0, z0, sz = PRINT_WIN['Shirt_Front']
    c = Canvas(-sz / 2, z0 - sz / 2, sz, res, PAL['Shirt'][0])
    line = '#A9AEBA'
    for sg in (1, -1):     # collar points: brighter white with a grey outline
        pts = [(sg * 0.024, 1.372), (sg * 0.011, 1.352), (sg * 0.044, 1.316), (sg * 0.056, 1.36)]
        c.put(c.fill(pts), '#FFFFFF')
        c.put(c.poly(pts + [pts[0]], 0.0012, 0.0012), line)
    navy, dark = '#262E4D', '#161B33'
    knot = [(-0.0115, 1.364), (0.0115, 1.364), (0.0085, 1.338), (-0.0085, 1.338)]
    blade = [(-0.0075, 1.340), (0.0075, 1.340), (0.0185, 1.212), (0.0, 1.192), (-0.0185, 1.212)]
    c.put(c.fill(blade), navy); c.put(c.fill(knot), navy)
    c.put(c.seg(-0.0115, 1.338, 0.0115, 1.338, 0.0011, 0.0011), dark)     # knot edge
    return c

def build_outfits(bmW, names, J, head_bvh):
    """Each outfit is ONE flush surface over the body (no layers, no collar) plus shoes; its colours come from
    colorfn(point, normal, material) baked into a pixel texture. Returns {outfit: (MB, colorfn)}."""
    arm_t = arm_param(J); rnd = random.Random(5)
    horiz = lambda z: (V((0, 0, z)), V((0, 0, 1)), None)
    fz = lambda f: f.calc_center_median().z
    is_arm = lambda f: arm_t(f) is not None
    def cut_arm(s):
        return [(arm_plane(J, sd, s)[0], arm_plane(J, sd, s)[1],
                 (lambda f, sd=sd: is_arm(f) and ((f.calc_center_median().x > 0) == (sd == 'L')))) for sd in ('L', 'R')]
    torso = lambda f: not is_arm(f) and abs(f.calc_center_median().x) < 0.3
    NECK = 1.366; TOP = NECK_TOP                         # the surface continues up the neck to TOP, painted as skin
    legs = leg_offset(J, rnd)
    def off(co, n):     # fabric thin and close to the skin; trousers keep a straight leg
        if arm_t(co) is not None: return 0.0015
        k = ss(0.92, 0.80, co.z); return 0.003 * (1 - k) + legs(co, n) * k
    def smooth_arm_weights(mb):
        """even upper-arm -> forearm blend across the elbow (and forearm -> hand at the wrist) instead of the
        base mesh's patchy heat weights, so the arm bends as one limb"""
        for i, p in enumerate(mb.v):
            t = arm_t(p)
            if t is None or t < 0.3: continue
            sd = 'L' if p.x > 0 else 'R'; k = ss(0.3, 0.55, t); wc = chain_weights(J, sd, p); w = {}
            for b, x in mb.w[i].items(): w[b] = w.get(b, 0) + x * (1 - k)
            for b, x in wc.items(): w[b] = w.get(b, 0) + x * k
            tot = sum(w.values()); mb.w[i] = {b: x / tot for b, x in w.items() if x / tot > 0.01}
    wrist = lambda co: (arm_t(co) or 0) > 1.9
    HEM = 0.009                                          # 2 px of darker fabric along every hem
    dark = lambda h: _hex(PAL[h][0]) * 0.74
    L1 = {sd: (J['upper_arm.' + sd][1] - J['upper_arm.' + sd][0]).length for sd in 'LR'}
    L2 = {sd: (J['forearm.' + sd][1] - J['forearm.' + sd][0]).length for sd in 'LR'}
    def arm_d(p, t, t_edge):    # metres along the arm past a sleeve edge at chain parameter t_edge
        sd = 'L' if p.x > 0 else 'R'
        return (t - t_edge) * (L1[sd] if t_edge < 1 else L2[sd])
    NX, NY, NC, ZN = 0.088, 0.07, 0.035, 1.315          # neckline oval: half-width, half-depth, centre y; above ZN
    oval = lambda c, g=0.0: (c.x / (NX + g)) ** 2 + ((c.y - NC) / (NY + g)) ** 2
    neckband = lambda c: c.z > ZN + 0.03 * ss(0.04, 0.09, c.y)      # the back sits close to the neck axis: start higher
    out = {}
    # ---------------- casual: olive tee (skull on the front), rust long sleeves / hem / crew neck, jeans
    skull = sampler(paint_skull(), lambda p: (p.x - PRINT_WIN['Tee_Print'][0], p.z - PRINT_WIN['Tee_Print'][1]))
    CUFF = 1.9
    def casual(p, n, mat):
        if mat != 'Cloth': return PAL[mat][0]
        if p.z > neck_line(p) and V((p.x, p.y - 0.035, 0)).length < 0.12: return skin_at(p.z - neck_line(p))
        t = arm_t(p)
        if t is not None and t > CUFF and not (abs(p.x) < 0.17 and p.z > 0.9): return skin_at(arm_d(p, t, CUFF))
        if t is not None and t >= 0.45 and abs(p.x) < 0.17 and p.z > 0.9: t = None   # torso side under the arm
        if t is not None and t < 2.05:
            if t < 0.45: return dark('Tee') if arm_d(p, t, 0.45) > -HEM else PAL['Tee'][0]       # tee sleeve hem
            return dark('Rust') if arm_d(p, t, CUFF) > -HEM else PAL['Rust'][0]                   # rust cuff
        if p.z < 0.856: return dark('Denim') if p.z < 0.105 + HEM else PAL['Denim'][0]           # jeans hem
        if p.z < 0.882: return dark('Rust') if p.z < 0.856 + HEM else PAL['Rust'][0]             # undershirt hem
        if p.z < 0.882 + HEM: return dark('Tee')                                                 # tee hem
        if neckband(p) and (oval(p, 0.012) < 1 or p.z > neck_line(p) - 0.016):                   # rust crew neck
            return dark('Rust') if p.z > neck_line(p) - HEM / 2 else PAL['Rust'][0]
        if n.y < -0.3 and abs(p.x) < 0.16:
            c = skull(p)
            if c is not None: return c
        return PAL['Tee'][0]
    g = garment(bmW, names, [horiz(0.105), horiz(TOP)] + cut_arm(2.0),
                lambda f: (0.105 < fz(f) < TOP and torso(f)) or (is_arm(f) and arm_t(f) < 2.0), off, 'Cloth', fold_skip=wrist)
    hug_neck(g, head_bvh); smooth_arm_weights(g); g.merge(build_shoe(bmW, names, J, 'boot'))
    out['Casual'] = (g, casual)
    # ---------------- student uniform: shirt, vest with two-band V trim and neckline trim, crest, tie, trousers
    sl = 0.5
    zV, zT, VW = 1.20, NECK + 0.004, 0.098
    TL, TN = 0.005, 0.01
    ZH, ZB = 0.84, 0.89
    ARM = [(1.42, 0.152), (1.33, 0.152), (1.27, 0.143), (1.21, 0.138), (1.16, 0.145), (1.13, 0.163), (1.105, 0.25)]
    def arm_x(z):
        if z >= ARM[0][0]: return ARM[0][1]
        for (z0, x0), (z1, x1) in zip(ARM, ARM[1:]):
            if z1 <= z <= z0: return lerp(x0, x1, (z0 - z) / (z0 - z1))
        return None
    Lv = math.hypot(zT - zV, VW)
    v_s = lambda c: (abs(c.x) * (zT - zV) - VW * (c.z - zV)) / Lv
    crest = sampler(paint_crest(), lambda p: (p.x - PRINT_WIN['Vest_Print'][0], p.z - PRINT_WIN['Vest_Print'][1]))
    front = sampler(paint_shirt_front(), lambda p: (p.x, p.z))
    def student(p, n, mat):
        if mat != 'Cloth': return PAL[mat][0]
        if p.z > neck_line(p) and V((p.x, p.y - 0.035, 0)).length < 0.12: return skin_at(p.z - neck_line(p))
        t = arm_t(p)
        if t is not None and not (abs(p.x) < 0.17 and p.z > 0.9 and t > sl):
            if t >= sl: return skin_at(arm_d(p, t, sl))                         # bare arm below the sleeve
            return dark('Shirt') if arm_d(p, t, sl) > -HEM else PAL['Shirt'][0]  # sleeve hem
        if p.z < ZH: return dark('Trousers') if p.z < 0.072 + HEM else PAL['Trousers'][0]
        if p.z < ZH + HEM: return dark('Vest_Band')                                  # vest hem
        xb = arm_x(p.z)
        if xb is not None and abs(p.x) > xb: return PAL['Shirt'][0]          # armhole: shirt shoulder
        fr = p.y < 0
        shirt = (front(p) if fr else None)
        shirt = PAL['Shirt'][0] if shirt is None else shirt
        if fr and v_s(p) < 0 and p.z > zV: return shirt                         # V opening (tie, collar points)
        if neckband(p) and oval(p) < 1:                                          # shirt around the neck
            return dark('Shirt') if p.z > neck_line(p) - HEM / 2 else shirt
        if fr:
            s_ = v_s(p)
            if s_ < TL and p.z > zV - 0.03: return PAL['Trim_Light'][0]
            if s_ < TL + TN and p.z > zV - 0.06: return PAL['Trim'][0]
        if neckband(p) and oval(p, TN) < 1: return PAL['Trim'][0]
        if p.z < ZB: return PAL['Vest_Band'][0]
        if fr and n.y < -0.3:
            c = crest(p)
            if c is not None: return c
        return PAL['Vest'][0]
    g = garment(bmW, names, [horiz(0.072), horiz(TOP)] + cut_arm(2.0),
                lambda f: (0.072 < fz(f) < TOP and torso(f)) or (is_arm(f) and arm_t(f) < 2.0), off, 'Cloth', fold_skip=wrist)
    hug_neck(g, head_bvh); smooth_arm_weights(g); g.merge(build_shoe(bmW, names, J, 'school'))
    out['Student'] = (g, student)
    return out
