"""v3 skeleton, weighted base body, arms and hands (exec'd after v3_hair.py)."""

PARENTS = {'pelvis': 'root', 'spine': 'pelvis', 'chest': 'spine', 'neck': 'chest', 'head': 'neck'}
for _sd in ('L', 'R'):
    PARENTS.update({'clavicle.' + _sd: 'chest', 'upper_arm.' + _sd: 'clavicle.' + _sd, 'forearm.' + _sd: 'upper_arm.' + _sd,
                    'hand.' + _sd: 'forearm.' + _sd, 'thigh.' + _sd: 'pelvis', 'shin.' + _sd: 'thigh.' + _sd,
                    'foot.' + _sd: 'shin.' + _sd, 'toe.' + _sd: 'foot.' + _sd})
CONNECT = {'spine', 'chest', 'head'} | {b + '.' + sd for b in ('forearm', 'hand', 'shin', 'foot', 'toe') for sd in 'LR'}

def make_armature(name, J, coll):
    arm = bpy.data.armatures.new(name)
    ob = bpy.data.objects.new(name, arm); coll.objects.link(ob)
    vl = bpy.context.view_layer; vl.update()
    for o in vl.objects: o.select_set(False)
    vl.objects.active = ob; ob.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    for b in ['root'] + list(PARENTS):
        eb = arm.edit_bones.new(b); eb.head, eb.tail = J[b]
        eb.align_roll(V((0, 0, 1)) if b.startswith(('foot', 'toe', 'root')) else V((0, -1, 0)))
    for b, p in PARENTS.items():
        eb = arm.edit_bones[b]; eb.parent = arm.edit_bones[p]; eb.use_connect = b in CONNECT
    arm.edit_bones['root'].use_deform = False
    bpy.ops.object.mode_set(mode='OBJECT')
    arm.display_type = 'OCTAHEDRAL'
    return ob

# The base mesh has a strong sway-back: buttocks out, lumbar hollow 5-6 cm deep, upper back bulging.
# Back-half depth scale per height (teen scale), measured from its side profile -> a straighter back.
BACK_S = [(0.78, 1.0), (0.86, 0.86), (0.92, 0.92), (0.98, 1.03), (1.04, 1.13), (1.10, 1.28), (1.16, 1.03),
          (1.22, 0.87), (1.28, 0.91), (1.34, 0.96), (1.40, 1.0)]

def straighten_back(bm):
    for v in bm.verts:
        p = v.co
        if p.y <= 0 or not (BACK_S[0][0] < p.z < BACK_S[-1][0]): continue
        for (z0, s0), (z1, s1) in zip(BACK_S, BACK_S[1:]):
            if z0 <= p.z <= z1: s = lerp(s0, s1, (p.z - z0) / (z1 - z0)); break
        k = ss(0.19, 0.15, abs(p.x)) * ss(0.0, 0.03, p.y)    # torso only; arms and the sides of the hips keep shape
        p.y *= lerp(1.0, s, k)
    bm.normal_update()

# The base chest is a wedge (depth falls off linearly from the centre line). Re-profile the front of the
# torso as a boxy superellipse at each height: same centre depth and half-width, no prow.
CHEST_W = [(0.92, 0.14), (1.02, 0.14), (1.08, 0.135), (1.14, 0.145), (1.20, 0.155), (1.26, 0.16), (1.33, 0.16)]

def round_chest(bm):
    def W(z):
        for (z0, w0), (z1, w1) in zip(CHEST_W, CHEST_W[1:]):
            if z0 <= z <= z1: return lerp(w0, w1, (z - z0) / (z1 - z0))
        return None
    D = {}
    for v in bm.verts:     # centre-line depth per 1 cm height band
        if v.co.y < 0 and abs(v.co.x) < 0.012: k = round(v.co.z, 2); D[k] = min(D.get(k, 0.0), v.co.y)
    for v in bm.verts:
        p = v.co; w = W(p.z)
        if p.y >= 0 or w is None or abs(p.x) >= w: continue
        d = min((D.get(round(p.z + dz, 2), 0.0) for dz in (-0.01, 0.0, 0.01)), default=0.0)
        if d >= -0.02: continue
        tgt = d * 0.96 * (1 - (abs(p.x) / w) ** 2.6) ** (1 / 2.6)
        k = ss(0.92, 0.97, p.z) * ss(1.34, 1.29, p.z) * ss(w, w * 0.85, abs(p.x))
        p.y = lerp(p.y, min(p.y, tgt) if tgt < p.y else lerp(p.y, tgt, 0.5), k)
    bm.normal_update()

# Arm radius (m) along the chain s: 0 shoulder, 1 elbow, 2 wrist. The base mesh has a knob at the elbow
# (r 6.7 cm between 4.5 cm sections, centre 2 cm off the bone), which made the forearm read as a separate piece.
ARM_R = [(0.0, 0.056), (0.25, 0.05), (0.55, 0.045), (0.85, 0.041), (1.0, 0.04), (1.15, 0.041), (1.4, 0.038), (1.75, 0.032), (2.0, 0.027)]

def reshape_arms(bm, J):
    """Re-profile the base arms as one smooth taper centred on the bones (keeps each vertex's direction, so the
    low-poly facets stay). Shoulder blends in; torso side/armpit vertices are left alone."""
    def R(s):
        for (s0, r0), (s1, r1) in zip(ARM_R, ARM_R[1:]):
            if s0 <= s <= s1: return lerp(r0, r1, (s - s0) / (s1 - s0))
        return ARM_R[-1][1]
    def torso_w(z):
        if z < CHEST_W[0][0] or z > 1.36: return 0.0
        for (z0, w0), (z1, w1) in zip(CHEST_W, CHEST_W[1:]):
            if z0 <= z <= z1: return lerp(w0, w1, (z - z0) / (z1 - z0))
        return CHEST_W[-1][1]
    moved = 0
    for v in bm.verts:
        p = v.co; sd = 'L' if p.x > 0 else 'R'; sg = 1 if sd == 'L' else -1
        S, E = J['upper_arm.' + sd]; _, W = J['forearm.' + sd]
        du, Lu = (E - S).normalized(), (E - S).length; df, Lf = (W - E).normalized(), (W - E).length
        tu = (p - S).dot(du) / Lu; tf = (p - E).dot(df) / Lf
        if tu < 0.08 or tf > 1.02: continue
        if tf <= 0: c = S + du * (tu * Lu); s = tu
        else: c = E + df * (tf * Lf); s = 1 + tf
        off = p - c; d = du if tf <= 0 else df
        off = off - d * off.dot(d); r = off.length
        if r < 1e-5 or r > 0.1: continue
        if p.x * sg < torso_w(p.z) + 0.004: continue            # torso side / armpit
        k = ss(0.08, 0.32, tu) if tf <= 0 else 1.0
        tgt = R(s)
        v.co = c + off.normalized() * lerp(r, tgt, k)
        moved += 1
    bm.normal_update()
    return moved

def prepare_base():
    """Base body at teen scale with joints, subdivided once (flat) and heat-weighted to the skeleton."""
    bm = load_base(); J = landmarks(bm)
    bm.transform(Matrix.Scale(BODY_K, 4)); J = scale_J(J, BODY_K)
    straighten_back(bm); round_chest(bm); reshape_arms(bm, J)
    for sd in 'LR':   # hand bone along the forearm direction
        E, W = J['forearm.' + sd]; J['hand.' + sd] = (W.copy(), W + (W - E).normalized() * 0.075)
    yc = J['chest'][1].y
    J['neck'] = (V((0, yc, 1.325)), V((0, 0.03, CHIN_Z + 0.025)))
    J['head'] = (V((0, 0.03, CHIN_Z + 0.025)), V((0, 0.03, CHIN_Z + HEAD_H)))
    bmesh.ops.subdivide_edges(bm, edges=bm.edges[:], cuts=1, use_grid_fill=True)
    bm.normal_update()
    me = bpy.data.meshes.new('__base'); bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new('__base', me); bpy.context.scene.collection.objects.link(ob)
    rig = make_armature('__base_rig', J, bpy.context.scene.collection)
    vl = bpy.context.view_layer
    for o in vl.objects: o.select_set(False)
    ob.select_set(True); rig.select_set(True); vl.objects.active = rig
    bpy.ops.object.parent_set(type='ARMATURE_AUTO')
    names = [g.name for g in ob.vertex_groups]
    bmW = bmesh.new(); bmW.from_mesh(me)
    bpy.data.objects.remove(ob); bpy.data.meshes.remove(me)
    a = rig.data; bpy.data.objects.remove(rig); bpy.data.armatures.remove(a)
    bmW.normal_update()
    return bmW, names, J

def base_weights(bmW, bvh, names, p):
    """skin weights of the base surface at its point nearest p (inverse-distance blend of that face's corners)"""
    loc, _, fi, _ = bvh.find_nearest(p)
    bmW.faces.ensure_lookup_table(); dl = bmW.verts.layers.deform.active
    acc = {}
    for v in bmW.faces[fi].verts:
        k = 1.0 / ((v.co - loc).length + 1e-4)
        for b, x in vweights(v, dl, names).items(): acc[b] = acc.get(b, 0) + x * k
    w = sorted(acc.items(), key=lambda kv: -kv[1])[:4]; t = sum(x for _, x in w)
    return {b: x / t for b, x in w}

def vweights(v, dl, names, n=4):
    w = sorted(((names[g], x) for g, x in v[dl].items() if x > 0.01), key=lambda kv: -kv[1])[:n]
    t = sum(x for _, x in w) or 1.0
    return {k: x / t for k, x in w}

# ------------------------------------------------------------------ arms + hands (one loft per side)
ARM_N = 10

def arm_frame(d, sg):
    """(lat, fwd) unit vectors perpendicular to limb direction d; lat points away from the body."""
    fwd = (V((0, -1, 0)) - d * d.dot(V((0, -1, 0)))).normalized()
    lat = d.cross(fwd).normalized()
    if lat.x * sg < 0: lat = -lat
    return lat, fwd

def chain_weights(J, sd, p):
    S, E = J['upper_arm.' + sd]; _, W = J['forearm.' + sd]
    du = (p - S).dot((E - S).normalized()); L1 = (E - S).length
    df = (p - E).dot((W - E).normalized()); L2 = (W - E).length
    if du < L1 - 0.035: return {'upper_arm.' + sd: 1.0}
    if df < 0.035:
        k = ss(-0.035, 0.035, df); return {'upper_arm.' + sd: 1 - k, 'forearm.' + sd: k}
    if df < L2 - 0.02: return {'forearm.' + sd: 1.0}
    k = ss(L2 - 0.02, L2 + 0.015, df); return {'forearm.' + sd: 1 - k, 'hand.' + sd: k}

def build_arms(bmW, names, J):
    """Arms from shoulder to wrist ray-cast onto the base surface (2 mm inside), then palm, thumb, 4 fingers."""
    bvh = BVHTree.FromBMesh(bmW); mb = MB(); arm_faces = set()
    for sd, sg in (('L', 1), ('R', -1)):
        S, E = J['upper_arm.' + sd]; _, W = J['forearm.' + sd]
        rings = []
        stations = [(S, E, t) for t in (0.0, 0.2, 0.45, 0.7, 0.88)] + [(E, W, t) for t in (0.0, 0.12, 0.35, 0.6, 0.82, 1.0)]
        for a, b, t in stations:
            c = a + (b - a) * t; d = (b - a).normalized(); lat, fwd = arm_frame(d, sg)
            ring = []
            for i in range(ARM_N):
                th = 2 * math.pi * i / ARM_N
                rd = (lat * math.cos(th) + fwd * math.sin(th) * sg).normalized()
                hit, _, _, dist = bvh.ray_cast(c, rd, 0.15)
                r = (dist - 0.002) if hit is not None else 0.04
                if a == W or (a == E and t == 1.0): r = min(r, 0.03)
                p = c + rd * r
                ring.append(mb.add(p, chain_weights(J, sd, p)))
            rings.append(ring)
        hand(mb, J, sd, sg, rings)
        for r0, r1 in zip(rings, rings[1:]):
            for i in range(ARM_N):
                j = (i + 1) % ARM_N
                f = [r0[i], r0[j], r1[j], r1[i]]
                mb.face(f if sg > 0 else f[::-1], 'Skin'); arm_faces.add(len(mb.f) - 1)
        for r in rings:            # skin weights from the heat-weighted base, identical to the sleeves around it
            for i in r: mb.w[i] = base_weights(bmW, bvh, names, mb.v[i])
    arms = mb.subset(arm_faces); hands = mb.subset(set(range(len(mb.f))) - arm_faces)
    return arms, hands

def hand(mb, J, sd, sg, rings):
    """Palm loft continuing the arm rings; fingers extrude from the knuckle end, thumb from the front edge."""
    E, W = J['forearm.' + sd]; d = (W - E).normalized()
    d = (d + V((0, 0, -1)) * 0.25).normalized()         # relaxed wrist: hand hangs a little more vertically
    lat, fwd = arm_frame(d, sg); med = -lat
    w = {'hand.' + sd: 1.0}
    # palm rings: back-of-hand row (lateral) front->back, then palm row back->front; matches ARM_N = 10
    def palm_ring(s, hw, th, bulge=0.0):
        c = W + d * s
        ys = [-1, -0.5, 0, 0.5, 1]
        back = [c + fwd * (y * hw) + lat * (th * (1 - 0.35 * y * y)) for y in ys]
        palm = [c + fwd * (y * hw) - lat * (th * (1 - 0.35 * y * y) + bulge) for y in ys]
        pts = back + palm[::-1]
        # rotate list so index 0 sits at the front edge like the arm rings (fwd side) - arm ring 0 is lateral
        return pts
    prev = rings[-1]
    specs = [(0.012, 0.024, 0.014, 0.0), (0.035, 0.036, 0.0145, 0.003), (0.06, 0.04, 0.013, 0.002), (0.078, 0.039, 0.011, 0.0)]
    pr = []
    for s, hw, th, bl in specs:
        pts = palm_ring(s, hw, th, bl)
        pr.append([mb.add(p, w) for p in pts])
    # bridge wrist ring (arm, angular order) to first palm ring by nearest-angle matching
    c0 = W
    def ang(p):
        v = p - c0; return math.atan2(v.dot(fwd) * sg, v.dot(lat))
    order = sorted(range(ARM_N), key=lambda i: ang(mb.v[pr[0][i]]))
    arm_order = sorted(range(ARM_N), key=lambda i: ang(mb.v[prev[i]]))
    a_sorted = [prev[i] for i in arm_order]; p_sorted = [pr[0][i] for i in order]
    for i in range(ARM_N):
        j = (i + 1) % ARM_N
        f = [a_sorted[i], a_sorted[j], p_sorted[j], p_sorted[i]]
        mb.face(f if sg > 0 else f[::-1], 'Skin')
    for ri, (r0, r1) in enumerate(zip(pr, pr[1:])):
        for i in range(ARM_N):
            j = (i + 1) % ARM_N
            if ri == 1 and i == 9: continue          # opening for the thumb
            f = [r0[i], r0[j], r1[j], r1[i]]
            mb.face(f if sg < 0 else f[::-1], 'Skin')
    last = pr[-1]; back, palm = last[:5], last[5:][::-1]
    # fingers: index (front) .. pinky (back); each a 3-segment box tapering and curling toward the palm
    lens = [0.058, 0.064, 0.060, 0.048]
    for k in range(4):
        q = [back[k], back[k + 1], palm[k + 1], palm[k]]
        base = [mb.v[i].copy() for i in q]; cen = sum(base, V()) / 4
        seg_l = lens[k] / 3; dirn = d.copy(); pos = cen; ringv = q
        for sgi in range(3):
            dirn = (Quaternion(fwd * sg, math.radians(-14 - 6 * sgi)) @ dirn).normalized()
            pos = pos + dirn * seg_l
            sc_ = 0.82 - 0.06 * sgi
            nr = []
            for p0 in base:
                off = p0 - cen
                off = off - d * off.dot(d)
                nr.append(mb.add(pos + (Quaternion(fwd * sg, math.radians(-14 * (sgi + 1) - 3 * sgi * sgi)) @ off) * sc_, w))
            for i in range(4):
                j = (i + 1) % 4
                f = [ringv[i], ringv[j], nr[j], nr[i]]
                mb.face(f if sg < 0 else f[::-1], 'Skin')
            ringv = nr
        tip = mb.add(pos + dirn * 0.006, w)
        for i in range(4):
            f = [ringv[i], ringv[(i + 1) % 4], tip]
            mb.face(f if sg < 0 else f[::-1], 'Skin')
    # thumb from the front edge of the palm (between palm rings 0 and 2), angled forward/down and inward
    e = [pr[1][0], pr[1][9], pr[2][9], pr[2][0]]          # front-edge quad left open above
    base = [mb.v[i].copy() for i in e]; cen = sum(base, V()) / 4
    tdir = (d * 0.7 + fwd * 0.5 + med * 0.4).normalized()
    ringv = e; pos = cen
    for sgi, (L, sc_) in enumerate(((0.025, 0.8), (0.022, 0.72), (0.02, 0.62))):
        tdir = (tdir + med * 0.15).normalized()
        pos = pos + tdir * L
        nr = [mb.add(pos + (p0 - cen) * sc_ * (0.85 if sgi else 1.0), w) for p0 in base]
        for i in range(4):
            j = (i + 1) % 4
            f = [ringv[i], ringv[j], nr[j], nr[i]]
            mb.face(f if sg > 0 else f[::-1], 'Skin')
        ringv = nr
    tip = mb.add(pos + tdir * 0.006, w)
    for i in range(4):
        f = [ringv[i], ringv[(i + 1) % 4], tip]
        mb.face(f if sg > 0 else f[::-1], 'Skin')
