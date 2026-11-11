"""Male head built from reference front/side profiles (exec'd after lp_parts.py; overrides head_pt/build_head for 'M').

Local coords: origin at eye level on the mid-plane, -Y forward, +X = his left. Metres.
Each row: z, half-width, face-plane depth (front), back depth, centre-line depth (nose/lips/chin), centre feature width.
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
    (-0.012, 0.075, 0.089, 0.102, 0.105, 0.012),
    (-0.024, 0.074, 0.090, 0.098, 0.112, 0.013),
    (-0.034, 0.072, 0.090, 0.093, 0.117, 0.014),  # nose tip
    (-0.042, 0.069, 0.089, 0.087, 0.108, 0.018),  # nose base / wings
    (-0.050, 0.066, 0.087, 0.079, 0.1, 0.02),     # subnasal
    (-0.060, 0.064, 0.086, 0.069, 0.099, 0.022),  # upper lip
    (-0.068, 0.064, 0.085, 0.061, 0.095, 0.024),  # mouth line
    (-0.076, 0.063, 0.084, 0.053, 0.097, 0.022),  # lower lip
    (-0.087, 0.061, 0.082, 0.042, 0.091, 0.026),  # labiomental groove
    (-0.099, 0.056, 0.080, 0.030, 0.095, 0.03),   # chin
    (-0.109, 0.047, 0.075, 0.016, 0.091, 0.032),
    (-0.116, 0.032, 0.062, 0.004, 0.075, 0.034),
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
    e = lerp(0.82, 0.7, ss(-0.02, 0.04, z)) if s < 0 else 0.9     # rounder lower face, flatter brow plane
    return V((w * se(c, e), (df if s < 0 else db) * se(s, e), z))

def _male_features(p):
    """Centre-line profile (nose, lips, chin) and facial planes, applied after the base is relaxed."""
    x, y, z = p
    _, w, df, db, yc, sig = _row(z)
    if y >= 0: return p
    fm = ss(0.0, 0.5, -y / max(df, 1e-3))
    ax = abs(x)
    y -= (yc - df) * math.exp(-(x / sig) ** 2) * fm
    y += 0.0095 * _gauss(ax, z, 0.033, 0.002, 0.014, 0.011) * fm        # eye sockets
    y -= 0.0045 * _gauss(ax, z, 0.03, 0.02, 0.022, 0.008) * fm          # brow ridge
    y -= 0.0065 * _gauss(ax, z, 0.016, -0.04, 0.007, 0.008) * fm        # nose wings
    y += 0.004 * _gauss(ax, z, 0.026, -0.068, 0.006, 0.006) * fm        # mouth corners
    y += 0.003 * _gauss(ax, z, 0.052, -0.05, 0.016, 0.018) * fm         # under-cheekbone plane
    y -= 0.003 * _gauss(ax, z, 0.05, -0.012, 0.016, 0.014) * fm         # cheekbones forward
    x += math.copysign(0.004 * _gauss(ax, z, 0.055, -0.012, 0.016, 0.014), x)    # cheekbones out
    x -= math.copysign(0.004 * _gauss(ax, z, 0.07, 0.035, 0.02, 0.02), x) * fm   # temples
    x += math.copysign(0.004 * _gauss(ax, z, 0.06, -0.09, 0.01, 0.01), x)        # jaw angle
    return V((x, y, z))

_MHEAD = {}
def male_head_dense(n_lon=96, n_z=64):
    if 'mesh' in _MHEAD: return _MHEAD['mesh']
    verts, faces, rings = [], [], []
    zs = [lerp(-0.119, 0.12, k / (n_z - 1)) for k in range(n_z)]
    for k, z in enumerate(zs):       # staggered rings -> triangle lattice (no horizontal facet rows)
        r = []
        for i in range(n_lon):
            verts.append(_male_base(2 * math.pi * i / n_lon, z)); r.append(len(verts) - 1)   # column n/4*3 = centre line
        rings.append(r)
    for k in range(n_z - 1):
        a, b = rings[k], rings[k + 1]
        for i in range(n_lon):
            j = (i + 1) % n_lon
            if (i + k) % 2 == 0: faces += [[a[i], a[j], b[i]], [a[j], b[j], b[i]]]   # alternating diagonals
            else: faces += [[a[i], a[j], b[j]], [a[i], b[j], b[i]]]
    verts.append(V((0, 0.0, 0.1225))); top = len(verts) - 1
    verts.append(V((0, -0.025, -0.1215))); bot = len(verts) - 1
    for i in range(n_lon):
        j = (i + 1) % n_lon
        faces.append([rings[-1][i], rings[-1][j], top]); faces.append([rings[0][j], rings[0][i], bot])
    bm = bmesh.new()
    bv_ = [bm.verts.new(v) for v in verts]
    for fc in faces: bm.faces.new([bv_[i] for i in fc])
    for _ in range(3): bmesh.ops.smooth_vert(bm, verts=bm.verts, factor=0.5, use_axis_x=True, use_axis_y=True, use_axis_z=False)
    verts = [_male_features(v.co.copy()) for v in bm.verts]; bm.free()
    _MHEAD['mesh'] = (verts, faces)
    _MHEAD['bvh'] = BVHTree.FromPolygons([tuple(v) for v in verts], faces)
    return _MHEAD['mesh']

def decimated(verts, faces, tris):
    me = bpy.data.meshes.new('__dec'); me.from_pydata([tuple(v) for v in verts], [], faces)
    ob = bpy.data.objects.new('__dec', me); bpy.context.scene.collection.objects.link(ob)
    md = ob.modifiers.new('Dec', 'DECIMATE'); md.decimate_type = 'COLLAPSE'
    cur = sum(len(f) - 2 for f in faces); md.ratio = min(1.0, tris / cur)
    md.use_symmetry = True; md.symmetry_axis = 'X'; md.use_collapse_triangulate = True
    dg = bpy.context.evaluated_depsgraph_get(); eo = ob.evaluated_get(dg); m2 = eo.to_mesh()
    v = [x.co.copy() for x in m2.vertices]; f = [list(p.vertices) for p in m2.polygons]
    eo.to_mesh_clear(); bpy.data.objects.remove(ob); bpy.data.meshes.remove(me)
    return v, f

head_pt_generic = head_pt
def head_pt(P, th, ph):
    if P['sex'] != 'M': return head_pt_generic(P, th, ph)
    male_head_dense()
    t, f = math.radians(th), math.radians(ph)
    d = V((math.cos(f) * math.sin(t), -math.cos(f) * math.cos(t), math.sin(f)))
    o = V((0, 0.006, 0.0))
    loc, _, _, _ = _MHEAD['bvh'].ray_cast(o, d)
    return (loc if loc is not None else o + d * 0.09)

build_head_generic = build_head
def build_head(P, J, skin, lips):
    if P['sex'] != 'M': return build_head_generic(P, J, skin, lips)
    C = P['head_c']
    v, f = decimated(*male_head_dense(), tris=1700)
    mb = MB(); mb.wfn = fixed('head')
    ids = [mb.add(C + p) for p in v]
    for fc in f: mb.face([ids[i] for i in fc], skin)
    bv = mb.bvh()
    ez = C.z + 0.002; ex = 0.032; es = 1.02
    for mir in (False, True):
        mb.mirror = mir
        E = lambda a, b: (ex + a * es, ez + b * es)
        eye = [E(-0.0135, -0.0002), E(-0.0075, 0.0048), E(0.003, 0.0062), E(0.0135, 0.0016), E(0.0068, -0.0036), E(-0.005, -0.0042)]
        decal(mb, [eye], bv, 'Eye_White', gap=0.0009, maxe=0.006)
        circ = lambda r, cx, cz, n=8: [E(cx + r * math.cos(k * 2 * math.pi / n), cz + r * math.sin(k * 2 * math.pi / n)) for k in range(n)]
        decal(mb, [circ(0.0055, 0.0, 0.0006)], bv, 'Iris', gap=0.0016, maxe=0.008)
        decal(mb, [circ(0.0025, 0.0, 0.0006, 6)], bv, 'Pupil', gap=0.0022, maxe=0.01)
        decal(mb, [circ(0.001, 0.0018, 0.0025, 4)], bv, 'Eye_White', gap=0.0027, maxe=0.01)
        lash = [E(-0.0145, 0.0002), E(-0.0078, 0.0058), E(0.003, 0.0076), E(0.0152, 0.0022), E(0.0136, 0.001), E(0.003, 0.0058), E(-0.0075, 0.0042)]
        raised_decal(mb, lash, bv, 'Lash', 0.0011)
        lid = [E(-0.0128, 0.0082), E(-0.004, 0.0112), E(0.007, 0.0112), E(0.0145, 0.006), E(0.008, 0.0092), E(-0.004, 0.0094)]
        decal(mb, [lid], bv, 'Skin_Shadow', gap=0.0008, maxe=0.008)
        by = ez + 0.0205
        brow = [(ex - 0.021, by - 0.004), (ex - 0.0205, by + 0.0032), (ex - 0.002, by + 0.0068), (ex + 0.015, by + 0.0046),
                (ex + 0.0235, by - 0.0022), (ex + 0.013, by - 0.0003), (ex - 0.004, by - 0.0014)]
        raised_decal(mb, brow, bv, 'Brow', 0.0032)
        decal(mb, [[(0.0095, C.z - 0.0445), (0.0155, C.z - 0.042), (0.0175, C.z - 0.0455), (0.012, C.z - 0.047)]], bv, 'Skin_Shadow', gap=0.0008, maxe=0.01)  # nostril shade
    mb.mirror = False
    mz = C.z - 0.0685; mw = 0.022
    lipup = [(-mw, mz), (-0.012, mz + 0.0035), (-0.0045, mz + 0.0058), (0.0, mz + 0.0046), (0.0045, mz + 0.0058), (0.012, mz + 0.0035), (mw, mz), (0.0, mz - 0.0004)]
    liplo = [(-mw, mz), (0.0, mz - 0.0004), (mw, mz), (0.014, mz - 0.0055), (0.0, mz - 0.0075), (-0.014, mz - 0.0055)]
    decal(mb, [lipup], bv, lips, gap=0.0009, maxe=0.008)
    mw = mw * 0.9
    decal(mb, [liplo], bv, 'Lips_Lower', gap=0.0008, maxe=0.008)
    decal(mb, [[(-mw + 0.003, mz + 0.0005), (mw - 0.003, mz + 0.0005), (mw - 0.003, mz - 0.0006), (-mw + 0.003, mz - 0.0006)]], bv, 'Lash', gap=0.0015, maxe=0.01)
    # ears
    for mir in (False, True):
        mb.mirror = mir
        base = C + head_pt(P, 92, -8); base.x -= 0.007
        ol = [(0.02, 0.028), (0.003, 0.035), (-0.013, 0.026), (-0.016, 0.004), (-0.011, -0.022), (-0.002, -0.034), (0.008, -0.031), (0.014, -0.014), (0.019, 0.01)]
        nrm = V((1, 0.3, 0)).normalized(); up = V((0, 0.12, 1)).normalized()
        up = (up - nrm * up.dot(nrm)).normalized()
        prism(mb, ol, base + V((0, 0.014, 0)), nrm, up, 0.019, skin, center_dip=0.010)
    mb.mirror = False
    return mb
