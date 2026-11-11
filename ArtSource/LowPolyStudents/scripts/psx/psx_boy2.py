"""PSX_Boy2: slim long-legged boy in the :time / Koharu style (visual reference only; nothing copied).

Long dark hair in big jagged layered panels with one high ponytail and crown spikes; fitted striped tee tucked into
a short skirt with a studded belt; crew socks and sneakers. Painted textures with baked ambient occlusion.

Headless:  blender -b --factory-startup --python psx_boy2.py          (output: ../../psx_boy2, env LP_OUT overrides)
"""
import bpy, os, math, random
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
os.environ['LP_OUT'] = os.environ.get('LP_OUT_BOY2') or os.path.join(os.path.dirname(os.path.dirname(HERE)), 'psx_boy2')
os.environ['LP_BLEND'] = 'PSX_Boy2.blend'
os.environ['PSX_LIB'] = '1'
G = globals()
exec(compile(open(os.path.join(HERE, 'psx_boy.py'), encoding='utf8').read(), 'psx_boy.py', 'exec'), G)   # helpers + lp toolkit

NAME = 'PSX_Boy2'
CHIN = 1.40; HEAD_H = 0.235; HK = HEAD_H / 0.27
HC = V((0, 0.004, CHIN + 0.52 * HEAD_H))           # head centre for hair placement

def boy_params():
    return dict(sex='M', ponytail=True, skirt=True, s=0.85, pelvis=(0.93, 1.0), spine_top=1.13, chest_top=1.30,
                neck=(V((0, 0.012, 1.315)), V((0, 0.010, CHIN + 0.02))), head_top=CHIN + HEAD_H,
                head_c=V((0, 0.0, CHIN + 0.11)), clav0=V((0.02, 0.0, 1.305)), shoulder=V((0.124, 0.008, 1.298)),
                arm_angle=31, upper=0.265, fore=0.24, hand=0.14, elbow_bend=8,
                hip=V((0.066, 0.0, 0.90)), knee=V((0.068, -0.01, 0.49)), ankle=V((0.07, 0.014, 0.074)),
                ball=V((0.073, -0.088, 0.02)), toe=V((0.075, -0.145, 0.018)))

# ------------------------------------------------------------------ body (skin): neck, arms, hands, legs
def build_skin(J, P):
    mb = MB()
    mb.wfn = W_neck(J, P)
    tube(mb, lambda t: V((0, 0.012, 1.27 + t)), [(0.0, .036, .036), (0.06, .029, .03), (0.15, .027, .029)], 6, 'Skin', cap0=False, cap1=False)
    mb.wfn = W_arm(J, P); ch = arm_chain(J)
    for mir in (False, True):
        mb.mirror = mir
        tube(mb, ch, [(0.3, .032, .031), (0.7, .029, .028), (0.97, .025, .025), (1.1, .027, .026), (1.45, .026, .024),
                      (1.85, .02, .018), (1.98, .019, .017)], 6, 'Arm', cap0=False, cap1=False)
    mb.mirror = False
    mb.wfn = wfun(J, [('hand.L', .05, None), ('forearm.L', .02, None)], power=6)
    for mir in (False, True):
        mb.mirror = mir
        tube(mb, ch, [(1.96, .02, .017), (2.12, .03, .013), (2.5, .031, .012), (2.82, .022, .009)], 6, 'Hand', cap0=False, cap1=True)
        Wr, Ht = J['hand.L']; d = (Ht - Wr).normalized()
        b0 = Wr + d * 0.03 + V((0, -0.017, 0)); tdir = (d + V((0, -0.55, 0))).normalized()
        tube(mb, lambda t, b0=b0, tdir=tdir: b0 + tdir * t, [(0.0, .009, .008), (0.045, .008, .007)], 4, 'Hand', cap0=True, cap1=True)
    mb.mirror = False
    mb.wfn = W_leg(J, P); lc = leg_chain(J)
    for mir in (False, True):
        mb.mirror = mir
        tube(mb, lc, [(0.2, .052, .054), (0.6, .045, .046), (0.95, .034, .035), (1.05, .035, .036), (1.35, .037, .037),
                      (1.7, .027, .027), (1.97, .024, .024)], 6, 'Leg', cap0=True, cap1=False)
    mb.mirror = False
    return mb

# ------------------------------------------------------------------ clothes
TEE = [(0.965, .108, .068, .066), (1.02, .108, .069, .065), (1.10, .11, .073, .065), (1.18, .12, .079, .069),
       (1.25, .128, .08, .07), (1.292, .116, .07, .066), (1.312, .07, .05, .05), (1.32, .046, .036, .036)]
SKIRT2 = [(0.72, .168, .112, .118), (0.80, .156, .104, .11), (0.90, .136, .088, .094), (0.985, .116, .074, .074)]

def build_clothes(J, P):
    mb = MB(); mb.wfn = W_torso(J, P)
    R = [mb.ring(torso_ring(z, w, df, db, 12)) for z, w, df, db in TEE]
    for a, b in zip(R, R[1:]): mb.bridge(a, b, 'Tee')
    mb.wfn = W_arm(J, P); ch = arm_chain(J)
    for mir in (False, True):                                  # short fitted sleeves
        mb.mirror = mir
        tube(mb, ch, [(0.0, .036, .034), (0.12, .042, .04), (0.38, .039, .038), (0.4, .033, .032)], 6, 'TeeSleeve', cap0=False, cap1=False)
    mb.mirror = False
    # skirt: short, soft folds (not pleats), slight A-line
    mb.wfn = W_skirt(J, P, 0.985, 0.72)
    fold = lambda z: (lambda i: 0.035 * ss(0.92, 0.76, z) * math.sin(i * math.pi / 2))
    S = [mb.ring(torso_ring(z, w, df, db, 16, jit=fold(z))) for z, w, df, db in SKIRT2]
    for a, b in zip(S, S[1:]): mb.bridge(a, b, 'Skirt')
    inner = mb.ring(torso_ring(0.73, .158, .105, .111, 16)); mb.bridge(inner, S[0], 'Skirt')
    # belt: band over the skirt top, buckle at the front
    mb.wfn = W_torso(J, P)
    B0 = mb.ring(torso_ring(0.958, .122, .079, .079, 16)); B1 = mb.ring(torso_ring(0.99, .121, .078, .078, 16))
    mb.bridge(B0, B1, 'Belt')
    box(mb, V((0.0, -0.081, 0.974)), V((1, 0, 0)), V((0, 1, 0)), V((0, 0, 1)), 0.016, 0.004, 0.017, 'Buckle')
    # crew socks + low sneakers
    mb.wfn = W_leg(J, P); lc = leg_chain(J)
    for mir in (False, True):
        mb.mirror = mir
        tube(mb, lc, [(1.72, .03, .03), (1.97, .027, .027), (2.0, .027, .027)], 6, 'Sock', cap0=False, cap1=False)
    mb.mirror = False
    mb.wfn = W_foot(J, P); A = J['foot.L'][0]
    shoe = [(0.06, .03, .07), (0.04, .037, .078), (0.0, .04, .072), (-0.05, .042, .062), (-0.1, .042, .052), (-0.14, .037, .045), (-0.162, .027, .04)]
    for mir in (False, True):
        mb.mirror = mir; rings = []
        for dy, hw, zt in shoe:
            y = A.y + dy
            pts = [V((A.x + hw, y, 0.0)), V((A.x + hw * 1.02, y, zt * 0.4)), V((A.x + hw * 0.8, y, zt)),
                   V((A.x - hw * 0.8, y, zt)), V((A.x - hw * 1.02, y, zt * 0.4)), V((A.x - hw, y, 0.0))]
            rings.append(mb.ring(pts[::-1]))
        for a, b in zip(rings, rings[1:]): mb.bridge(a, b, 'Shoe')
        mb.cap(rings[0], 'Shoe'); mb.cap(rings[-1], 'Shoe', end=True)
    mb.mirror = False
    return mb

# ------------------------------------------------------------------ hair: big jagged layered panels + high ponytail
def head_surf(head_mb):
    bvh = head_mb.bvh()
    def pt(th, ph, lift=0.0):
        t, f = math.radians(th), math.radians(ph)
        d = V((math.cos(f) * math.sin(t), -math.cos(f) * math.cos(t), math.sin(f)))
        loc, n, _, _ = bvh.ray_cast(HC, d)
        if loc is None: loc = HC + d * 0.1
        return loc + (loc - HC).normalized() * lift
    def push(p, gap):
        """keep p at least `gap` outside the head"""
        loc, n, _, dist = bvh.find_nearest(p)
        if loc is None: return p
        side = (p - loc).dot(n)
        return p if side >= gap else loc + n * gap
    return pt, push

def jag_panel(mb, cols, rows, thick, mat='Hair', closed=False):
    """cols: list of column polylines (root -> tip), all with rows+1 points. Two skins (outer/inner offset by thick
    toward the column's own 'in' side) + side walls + jagged bottom. Adjacent columns of different length make the zigzag."""
    n = len(cols); outer, inner = [], []
    for ci, pts in enumerate(cols):
        o, i_ = [], []
        for k, p in enumerate(pts):
            nb = cols[(ci + 1) % n] if (closed or ci + 1 < n) else cols[ci - 1]
            pb = cols[(ci - 1) % n] if (closed or ci > 0) else cols[ci + 1]
            across = (nb[k] - pb[k]); along = (pts[min(k + 1, rows)] - pts[max(k - 1, 0)])
            nrm = along.cross(across)
            nrm = nrm.normalized() if nrm.length > 1e-9 else V((0, 0, 1))
            if (p - HC).dot(nrm) < 0 and p.z > CHIN - 0.05: nrm = -nrm          # outward on the head
            t = thick * (1 - 0.6 * k / rows)
            o.append(mb.add(p)); i_.append(mb.add(p - nrm * t))
        outer.append(o); inner.append(i_)
    rng = range(n) if closed else range(n - 1)
    for c in rng:
        d = (c + 1) % n
        for k in range(rows):
            mb.face([outer[c][k], outer[d][k], outer[d][k + 1], outer[c][k + 1]], mat)
            mb.face([inner[c][k + 1], inner[d][k + 1], inner[d][k], inner[c][k]], mat)
        mb.face([outer[c][rows], outer[d][rows], inner[d][rows], inner[c][rows]], mat)       # jagged bottom edge
        mb.face([inner[c][0], inner[d][0], outer[d][0], outer[c][0]], mat)                   # root edge
    if not closed:
        for c, flip in ((0, False), (n - 1, True)):
            for k in range(rows):
                f = [outer[c][k], outer[c][k + 1], inner[c][k + 1], inner[c][k]]
                mb.face(f[::-1] if flip else f, mat)

def build_hair(head_mb, J):
    pt, push = head_surf(head_mb)
    mb = MB(); mb.wfn = fixed('head'); rnd = random.Random(11); R = 4
    def col(root, tip, gap, sag=0.0, bend=None):
        pts = []
        for k in range(R + 1):
            t = k / R; p = root.lerp(tip, t)
            if bend is not None: p = p + bend * math.sin(math.pi * t) * 0.5
            p = p + V((0, 0, -sag * math.sin(math.pi * t)))
            pts.append(push(p, gap * (1 - 0.3 * t)) if p.z > CHIN - 0.06 else p)
        return pts
    # skull cap
    hl = lambda th: interp([(0, 44), (40, 38), (75, 20), (100, 8), (140, -6), (180, -12)], abs(th))
    n = 14; ths = [-180 + 360 * i / n for i in range(n)]; rows = []
    for r in range(4):
        rows.append(mb.ring([pt(th, lerp(hl(th), 86, (r / 3) ** 0.9), 0.008 + 0.014 * (r > 0) + 0.004 * (r > 1)) for th in ths]))
    for a, b in zip(rows, rows[1:]): mb.bridge(a, b, 'Hair')
    mb.cap(rows[-1], 'Hair', end=True, center=pt(0, 90, 0.03)); mb.cap(rows[0], 'Hair', end=False)
    # bangs: one wide panel across the forehead, jagged down to the eyes (longer strands between the eyes and at the sides)
    cols = []
    for i, th in enumerate(range(-70, 71, 10)):
        L = (1.0, 0.35, 0.8, 0.3, 0.95, 0.4, 0.7, 0.25, 1.0, 0.35, 0.85, 0.3, 0.9, 0.4, 1.0)[i % 15]
        root = pt(th * 0.9, 68, 0.024); tipz = CHIN + HEAD_H * (0.74 - 0.3 * L)
        tip = pt(th * 1.08, 10, 0.02); tip.z = tipz
        cols.append(col(root, tip, 0.016, bend=(root - HC).normalized() * 0.03))
    jag_panel(mb, cols, R, 0.02)
    # side curtains framing the face, to below the chin
    for sg in (1, -1):
        cols = []
        for i, th in enumerate((66, 76, 86, 96, 106)):
            L = (1.0, 0.45, 0.9, 0.5, 0.8)[i]
            root = pt(sg * th, 52, 0.022); tip = pt(sg * (th + 4), -10, 0.018); tip.z = CHIN + 0.03 - 0.09 * L
            cols.append(col(root, tip, 0.012))
        if sg < 0: cols = cols[::-1]
        jag_panel(mb, cols, R, 0.018)
    # nape panel: short layer covering the back of the head under the tail
    cols = []
    for i, th in enumerate(range(118, 243, 16)):
        L = (1.0, 0.5, 0.9, 0.45, 1.0, 0.5, 0.85, 0.45, 1.0)[i % 9]
        root = pt(th, 40, 0.022); tip = pt(th, -18 * L, 0.02); tip.z = CHIN + 0.04 - 0.08 * L
        cols.append(col(root, tip, 0.014))
    jag_panel(mb, cols, R, 0.018)
    # high ponytail: tie at the back of the crown, a closed ring of jagged columns falling to the waist
    tie = pt(180, 52, 0.03); ring_r = 0.03
    tail_path = polyline([tie, tie + V((0, 0.05, 0.025)), tie + V((0, 0.085, -0.07)), V((0, tie.y + 0.07, 1.16)), V((0, tie.y + 0.06, 0.95))])
    cols = []; TN = 10
    for i in range(TN):
        a = 2 * math.pi * i / TN; L = (1.0, 0.7, 0.95, 0.6, 1.0, 0.75, 0.9, 0.65, 0.95, 0.7)[i]
        pts = []
        for k in range(R + 1):
            s = 0.08 + 0.92 * (k / R) * L
            c = tail_path(s); d = (tail_path(s + 0.02) - tail_path(s - 0.02)).normalized()
            u_, w_ = frame_for(d, V((1, 0, 0)))
            r = ring_r * (1.0 + 1.5 * math.sin(math.pi * min(1.0, s * 1.15)) ** 0.7) * (1 - 0.65 * (k == R))
            pts.append(c + (u_ * math.cos(a) + w_ * math.sin(a)) * r)
        cols.append(pts)
    j0 = len(mb.v)
    jag_panel(mb, cols, R, 0.006, closed=True)
    J['ponytail.01'] = (tail_path(0.05), tail_path(0.45)); J['ponytail.02'] = (tail_path(0.45), tail_path(1.0))
    pw = wfun(J, [('head', .04, None), ('ponytail.01', .05, None), ('ponytail.02', .05, None)], power=6)
    for vi in range(j0, len(mb.v)): mb.w[vi] = pw(mb.v[vi])
    tube(mb, lambda t: tail_path(0.02 + t), [(0.0, .034, .034), (0.04, .034, .034)], 8, 'HairTie', ref=V((1, 0, 0)), cap0=False, cap1=False)
    # crown spikes: thin needles fanning sideways from the top, like the reference's silhouette
    for th, ph, ln in ((40, 74, 0.06), (-40, 72, 0.055), (90, 64, 0.05), (-90, 62, 0.055), (140, 70, 0.05), (-140, 70, 0.05)):
        b = pt(th, ph, 0.012); out = (b - HC).normalized()
        side = V((math.copysign(1.0, math.sin(math.radians(th))), 0.3 * math.copysign(1, -math.cos(math.radians(th))), 0.2))
        tip = b + (out * 0.4 + side).normalized() * ln
        lock(mb, [b, b.lerp(tip, 0.5), tip], HC, 0.016, 0.008)
    return mb

# ------------------------------------------------------------------ UVs / textures
REG.update({'Tee': (0, 128, 128, 64), 'TeeSleeve': (128, 128, 64, 32), 'Arm': (192, 128, 32, 64), 'Belt': (0, 112, 128, 16),
            'Buckle': (128, 112, 16, 16), 'Sock': (144, 112, 32, 16), 'HairTie': (176, 112, 16, 16), 'Skirt': (0, 48, 128, 64),
            'Leg': (224, 96, 32, 96)})

face_uv_base = face_uv
def face_uv(mat, p, n, J):
    side = 'L' if p.x >= 0 else 'R'; q = p if side == 'L' else mx(p)
    if mat == 'Tee': return 'Tee', _cyl(p, 0, 0, 0.965, 1.322)
    if mat == 'TeeSleeve':
        S, E = J['upper_arm.L']; return 'TeeSleeve', _chain_uv(q, [S - (E - S).normalized() * 0.01, S + (E - S) * 0.4])
    if mat == 'Arm':
        S, E = J['upper_arm.L']; _, W = J['forearm.L']; return 'Arm', _chain_uv(q, [S, E, W])
    if mat == 'Skirt': return 'Skirt', _cyl(p, 0, 0, 0.72, 0.99)
    if mat == 'Belt': return 'Belt', _cyl(p, 0, 0, 0.958, 0.99)
    if mat == 'Buckle': return 'Buckle', ((p.x + 0.016) / 0.032, (p.z - 0.957) / 0.034)
    if mat == 'Sock':
        H, K = J['thigh.L']; _, A = J['shin.L']; u, v = _chain_uv(q, [H, K, A]); return 'Sock', (u, (v - 0.86) / 0.14)
    if mat == 'HairTie': return 'HairTie', (0.5, 0.5)
    if mat == 'Hair': return 'Hair', _cyl(p, 0, 0.03, 0.92, CHIN + HEAD_H + 0.05)
    return face_uv_base(mat, p, n, J)

COL.update(dict(skin='#EEE6E8', skin_sh='#D5C7CD', hair='#2B2733', hair_hi='#4E475C', hair_sh='#17141C',
                tee_a='#2E4652', tee_b='#17191E', skirt='#25252B', skirt_sh='#18181D', belt='#121216', stud='#B9B7BF',
                sock='#E6E4EA', sock_sh='#BDBAC4', shoe='#1A1A1E', sole='#E2E0E6', lace='#C9C6CF'))

def paint_atlas2(path):
    A = np.zeros((ATLAS, ATLAS, 3), np.float32); A[:] = hexc(COL['skin']); rnd = np.random.default_rng(5)
    def region(name):
        x0, y0, w, h = REG[name]; U, Vv = np.meshgrid((np.arange(w) + 0.5) / w, (np.arange(h) + 0.5) / h)
        return A[y0:y0 + h, x0:x0 + w], U, Vv
    def noise(img, amt, scale=1):
        h, w = img.shape[:2]; n = rnd.random((h // scale + 1, w // scale + 1)).repeat(scale, 0).repeat(scale, 1)[:h, :w]
        img *= (1 + (n[..., None] - 0.5) * amt)
    for nm in ('Skin', 'Hand', 'Arm', 'Leg'):
        img, U, Vv = region(nm); img[:] = hexc(COL['skin']); noise(img, 0.04, 2)
    img, U, Vv = region('Arm'); img[np.abs(Vv - 0.5) < 0.04] *= 0.93                                    # elbow
    img, U, Vv = region('Leg'); img[np.abs(Vv - 0.5) < 0.035] = img[np.abs(Vv - 0.5) < 0.035] * 0.94 + np.array([0.03, 0.0, 0.0])   # knees
    img, U, Vv = region('Face'); img[:] = hexc(COL['skin']); paint_face(img)
    # hair: near-black, lighter streaks along the strands, darker roots
    img, U, Vv = region('Hair'); img[:] = hexc(COL['hair'])
    for k in range(26):
        u0 = rnd.random(); wv = 0.006 + rnd.random() * 0.01; v0 = rnd.random() * 0.6
        img[(np.abs(((U - u0 + 0.5) % 1) - 0.5) < wv) & (Vv > v0) & (Vv < v0 + 0.4)] = hexc(COL['hair_hi'])
    img *= (0.8 + 0.25 * Vv)[..., None]; noise(img, 0.12, 2)
    # tee: horizontal stripes, ribbed crew neck, hem, fold shading under the arms and at the waist
    img, U, Vv = region('Tee'); band = ((Vv * 11).astype(int) % 2) == 0
    img[:] = np.where(band[..., None], hexc(COL['tee_a']), hexc(COL['tee_b']))
    img[Vv > 0.93] = hexc(COL['tee_b']) * 1.3                                                           # collar rib
    side = np.minimum(np.abs(U - 0.25), np.abs(U - 0.75)); img *= (0.82 + 0.18 * np.clip(side / 0.12, 0, 1))[..., None]
    for k in range(5):                                                                                   # diagonal folds
        u0 = 0.32 + 0.09 * k; f = np.abs((U - u0) - 0.12 * (Vv - 0.4)) < 0.008
        img[f & (Vv > 0.15) & (Vv < 0.6)] *= 0.82
    noise(img, 0.1, 1)
    img, U, Vv = region('TeeSleeve'); band = ((Vv * 4).astype(int) % 2) == 0
    img[:] = np.where(band[..., None], hexc(COL['tee_a']), hexc(COL['tee_b'])); img[Vv > 0.88] *= 0.75; noise(img, 0.1, 1)
    # skirt: dark twill with soft folds and a stitched hem
    img, U, Vv = region('Skirt'); img[:] = hexc(COL['skirt'])
    img *= (0.85 + 0.15 * (0.5 + 0.5 * np.sin(U * 2 * math.pi * 4)))[..., None]                       # folds (match the 4 lobes)
    img[(Vv > 0.06) & (Vv < 0.08)] *= 0.6; img[Vv > 0.95] *= 0.7; noise(img, 0.14, 1)
    # belt with grommets, buckle
    img, U, Vv = region('Belt'); img[:] = hexc(COL['belt'])
    for k in range(24):
        cx = (k + 0.5) / 24; m = (np.hypot((U - cx) * 128, (Vv - 0.5) * 16) < 3.0)
        ring = m & (np.hypot((U - cx) * 128, (Vv - 0.5) * 16) > 1.6)
        if abs(cx - 0.5) > 0.03: img[ring] = hexc(COL['stud'])
    noise(img, 0.08, 1)
    img, U, Vv = region('Buckle'); img[:] = hexc(COL['stud']); img[(np.abs(U - 0.5) < 0.3) & (np.abs(Vv - 0.5) < 0.3)] = hexc(COL['belt'])
    img, U, Vv = region('Sock'); img[:] = hexc(COL['sock']); img[Vv > 0.8] = hexc(COL['sock_sh']); noise(img, 0.06, 1)
    img, U, Vv = region('HairTie'); img[:] = hexc('#3A2C44')
    img, U, Vv = region('Shoe'); img[:] = hexc(COL['shoe']); img[Vv < 0.28] = hexc(COL['sole'])
    img[(Vv > 0.8) & (U > 0.25) & (U < 0.5) & (((Vv * 64).astype(int) % 3) == 0)] = hexc(COL['lace']); noise(img, 0.06, 1)
    np.clip(A, 0, 1, out=A)
    im = bpy.data.images.new('PSX_Boy2_Atlas', ATLAS, ATLAS, alpha=False)
    im.pixels.foreach_set(np.concatenate([A, np.ones((ATLAS, ATLAS, 1), np.float32)], 2).ravel())
    im.filepath_raw = path; im.file_format = 'PNG'; im.save(); im.pack()
    return im

def bake_ao(objs, atlas, strength=0.55):
    """Cycles AO bake of all meshes into one image (rest pose), then darken the atlas by it."""
    sc = bpy.context.scene; prev = sc.render.engine
    sc.render.engine = 'CYCLES'; sc.cycles.samples = 64; sc.cycles.device = 'CPU'
    ao = bpy.data.images.new('__ao', ATLAS, ATLAS, alpha=False); ao.generated_color = (1, 1, 1, 1)
    mats = {m for o in objs for m in o.data.materials if m}
    nodes = {}
    for m in mats:
        n = m.node_tree.nodes.new('ShaderNodeTexImage'); n.image = ao; m.node_tree.nodes.active = n; nodes[m] = n
    vl = bpy.context.view_layer
    for o in vl.objects: o.select_set(False)
    for o in objs: o.select_set(True)
    vl.objects.active = objs[0]
    sc.render.bake.margin = 3; sc.render.bake.use_clear = True
    bpy.ops.object.bake(type='AO')
    a = np.array(ao.pixels[:], np.float32).reshape(ATLAS, ATLAS, 4)[..., 0]
    px = np.array(atlas.pixels[:], np.float32).reshape(ATLAS, ATLAS, 4)
    px[..., :3] *= (1 - strength + strength * a)[..., None]
    atlas.pixels.foreach_set(px.ravel()); atlas.save(); atlas.pack()
    for m, n in nodes.items(): m.node_tree.nodes.remove(n)
    bpy.data.images.remove(ao); sc.render.engine = prev
    return float(a.mean())

def build():
    for o in list(bpy.data.objects): bpy.data.objects.remove(o)
    P = boy_params(); J = joints(P)
    root = bpy.data.collections.new('Characters'); bpy.context.scene.collection.children.link(root)
    coll = bpy.data.collections.new(NAME); root.children.link(coll)
    atlas = paint_atlas2(os.path.join(OUT, 'textures', 'PSX_Boy2_Atlas.png'))
    mat = tex_mat('PSX_Boy2', atlas)
    fmats = {'Pupil': tex_mat('PSX_Boy2_Pupil', paint_small('PSX_Boy2_Pupil', 32, _pupil), True),
             'Mouth': tex_mat('PSX_Boy2_Mouth', paint_small('PSX_Boy2_Mouth', 32, _mouth), True)}
    head = build_head(J, P)
    face_mb, face_uvs = build_face_planes(head)
    hair = build_hair(head, J)                              # also adds ponytail joints to J
    body = MB(); body.merge(head); body.merge(build_skin(J, P))
    parts = {'Body': body, 'Hair': hair, 'Clothes': build_clothes(J, P)}
    ex = {'ponytail.01': J['ponytail.01'] + ('head',), 'ponytail.02': J['ponytail.02'] + ('ponytail.01',)}
    arm = make_armature_data(NAME + '_Armature', J, P, ex)
    rig = bpy.data.objects.new(NAME + '_Rig', arm); coll.objects.link(rig)
    objs = []; tris = 0
    for pn, mb in parts.items():
        ob = make_obj(f'{NAME}_{pn}', mb, coll, mat, J); ob.parent = rig
        md = ob.modifiers.new('Armature', 'ARMATURE'); md.object = rig
        objs.append(ob); tris += sum(len(p.vertices) - 2 for p in ob.data.polygons)
    fo = make_face_obj(f'{NAME}_Face', face_mb, face_uvs, coll, fmats); fo.parent = rig
    md = fo.modifiers.new('Armature', 'ARMATURE'); md.object = rig; tris += len(face_mb.f) * 2
    bpy.context.view_layer.update()
    ao_mean = bake_ao(objs, atlas)
    info = {NAME: dict(P=P, J=J, rig=rig, tris=tris, ao=ao_mean)}
    G['INFO'] = info
    return info

if __name__ == '__main__' or bpy.app.background:
    print('PSX_BOY2_TRIS', main())
