"""Male body from the provided low-poly base mesh (exec'd after lp_assemble.py; overrides build_variant for 'M').

Pipeline: load + scale the base FBX, narrow the stance, measure joints from cross-sections, heat-weight
the whole body to a temporary rig, then derive every garment from body faces (clean plane cuts,
offset along normals, folded hems). Garments copy the body's weights, so cloth and skin deform together.
Our profile-built head, hair, boots, backpack and uniform accessories are added on top.
"""
import os
from mathutils import Matrix
BASE_FBX = os.environ.get('LP_BASE_FBX') or r"C:\Users\pajar\Downloads\low-poly-male-base-mesh\source\lowpoly_male_base_mesh-TPOSE.fbx"
BASE_TOP = 1.75
HEAD_C = V((0, -0.006, 1.628))     # eye level of the base head after scaling (chin 1.508, crown 1.75)
USE_FACE_IMPORT = os.environ.get('LP_FACE', '1') != '0'   # transplant the face from FACE_FBX when it exists
HEAD_ALPHA = 0.55                  # how far the base head is pulled toward the reference-profile surface
_BASE = {}
_HEADSURF = {}

def refine_base_head(bm, C, alpha=HEAD_ALPHA):
    """Subdivide the base head once (shape-preserving) and pull it part-way toward the reference-profile
    surface for brow, sockets, nose, lips and jaw. Ears and the neck keep the base geometry."""
    male_head_mesh(); ref = _MHEAD['bvh']; o = V((0, 0.006, 0))
    def beyond(p):   # how far p sits outside the reference head surface, along the ray from the head centre
        d = (p - C) - o
        loc, _, _, _ = ref.ray_cast(o, d.normalized())
        return d.length - (loc - o).length if loc is not None else 0.0
    # the base ears are flat fins; remove them and close the openings (modelled ears are added later)
    ear = [f for f in bm.faces if 1.57 < f.calc_center_median().z < 1.69 and abs(f.calc_center_median().x) > 0.06
           and beyond(f.calc_center_median()) > 0.006]
    if ear:
        bmesh.ops.delete(bm, geom=ear, context='FACES')
        bnd = [e for e in bm.edges if len(e.link_faces) == 1 and e.verts[0].co.z > 1.5]
        bmesh.ops.holes_fill(bm, edges=bnd, sides=0)
        bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')
    edges = [e for e in bm.edges if all(v.co.z > 1.495 for v in e.verts)]
    bmesh.ops.subdivide_edges(bm, edges=edges, cuts=1, use_grid_fill=True)
    for v in bm.verts:
        if v.co.z < 1.495: continue
        d = (v.co - C) - o
        if d.length < 1e-6: continue
        loc, _, _, _ = ref.ray_cast(o, d.normalized())
        if loc is None: continue
        tgt = C + loc
        a = (alpha + 0.25 * ss(1.585, 1.54, v.co.z)) * ss(1.50, 1.535, v.co.z) * ss(0.03, 0.012, (tgt - v.co).length)   # firmer chin/jaw
        v.co = v.co.lerp(tgt, a)
    bm.normal_update()

head_pt_analytic = head_pt
def head_pt(P, th, ph):
    """Male hair/ear placement on the actual (refined base) head surface once it exists."""
    b = _HEADSURF.get('bvh')
    if P['sex'] != 'M' or b is None: return head_pt_analytic(P, th, ph)
    C = P['head_c']; t, f = math.radians(th), math.radians(ph)
    d = V((math.cos(f) * math.sin(t), -math.cos(f) * math.cos(t), math.sin(f)))
    o = C + V((0, 0.006, 0))
    loc, _, _, _ = b.ray_cast(o, d)
    ref = head_pt_analytic(P, th, ph)
    if loc is None: return ref
    got = loc - C
    # hair rides on whichever surface is outermost (transplanted skull vs reference head), so nothing pokes through
    return got if (got - V((0, 0.006, 0))).length >= (ref - V((0, 0.006, 0))).length else ref

def base_bmesh():
    if 'bm' not in _BASE:
        before = set(bpy.data.objects.keys())
        bpy.ops.import_scene.fbx(filepath=BASE_FBX)
        new = [o for o in bpy.data.objects if o.name not in before]
        mo = [o for o in new if o.type == 'MESH'][0]
        bm = bmesh.new(); bm.from_mesh(mo.data); bm.transform(mo.matrix_world)
        meshes = [o.data for o in new if o.type == 'MESH']
        for o in new: bpy.data.objects.remove(o)
        for m in meshes:
            if m.users == 0: bpy.data.meshes.remove(m)
        k = BASE_TOP / max(v.co.z for v in bm.verts)
        bm.transform(Matrix.Scale(k, 4))
        for v in bm.verts:   # narrower stance: ankles from ~26 cm to ~15 cm off centre, hips unchanged
            z = v.co.z
            if z < 0.885 and abs(v.co.x) > 1e-6:
                v.co.x -= math.copysign(0.105 * min(1.0, (0.885 - z) / (0.885 - 0.124)), v.co.x)
        bm.normal_update()
        refine_base_head(bm, HEAD_C)
        _BASE['bm'] = bm
    return _BASE['bm'].copy()

def sections(bm, z, no=V((0, 0, 1))):
    """Cross-section islands of bm at plane through (0,0,z): list of point lists."""
    b = bm.copy()
    res = bmesh.ops.bisect_plane(b, geom=b.verts[:] + b.edges[:] + b.faces[:], plane_co=(0, 0, z), plane_no=no)
    cut = [g for g in res['geom_cut'] if isinstance(g, bmesh.types.BMEdge)]
    verts = {v for e in cut for v in e.verts}; adj = {v: [] for v in verts}
    for e in cut:
        a, c = e.verts; adj[a].append(c); adj[c].append(a)
    seen, out = set(), []
    for v in verts:
        if v in seen: continue
        st, comp = [v], []
        while st:
            x = st.pop()
            if x in seen: continue
            seen.add(x); comp.append(x.co.copy()); st += adj[x]
        out.append(comp)
    b.free()
    return out

def _cent(pts): return sum(pts, V()) / len(pts)

def base_landmarks(bm):
    def islands(z): return [(_cent(p), p) for p in sections(bm, z)]
    def leg(z): return min((c for c, p in islands(z) if c.x > 0.02), key=lambda c: c.x)
    def torso_y(z): return max(islands(z), key=lambda cp: len(cp[1]) if abs(cp[0].x) < 0.05 else -1)[0].y
    def arm(z): return max((c for c, p in islands(z) if c.x > 0.2), key=lambda c: c.x)
    A = leg(0.10); K = leg(0.465); c70, c78 = leg(0.70), leg(0.78)
    H = c78 + (c78 - c70) * ((0.885 - 0.78) / (c78.z - c70.z))
    foot = [v.co for v in bm.verts if v.co.z < 0.06 and v.co.x > 0]
    toe = min(foot, key=lambda p: p.y)
    ball = V((A.x, toe.y + 0.07, 0.022)); toe_t = V((A.x, toe.y + 0.012, 0.02))
    # arm: upper-arm centroid line -> shoulder; narrowest section -> wrist; proportional elbow
    ups = [arm(z) for z in (1.12, 1.15, 1.18, 1.21, 1.24)]
    zs = [p.z for p in ups]; mz = sum(zs) / len(zs)
    def fit(attr):
        vals = [getattr(p, attr) for p in ups]; mv = sum(vals) / len(vals)
        sl = sum((z - mz) * (v - mv) for z, v in zip(zs, vals)) / sum((z - mz) ** 2 for z in zs)
        return lambda z: mv + sl * (z - mz)
    fx, fy = fit('x'), fit('y')
    S = V((fx(1.36), fy(1.36), 1.36))
    wz = min((0.93 + 0.005 * i for i in range(14)), key=lambda z: (lambda c, p: max(q.x for q in p) - min(q.x for q in p))(*max(((c, p) for c, p in islands(z) if c.x > 0.2), key=lambda cp: cp[0].x)))
    W = arm(wz)
    E = arm(S.z + 0.53 * (W.z - S.z))
    tip = max((v.co for v in bm.verts), key=lambda p: p.x)
    Ht = W + (tip - W) * 0.85
    yp, ys, yc, yn = torso_y(0.96), torso_y(1.08), torso_y(1.27), torso_y(1.53)
    J = {'root': (V((0, 0, 0)), V((0, 0.25, 0))),
         'pelvis': (V((0, yp, 0.92)), V((0, yp, 1.0))), 'spine': (V((0, yp, 1.0)), V((0, ys, 1.16))),
         'chest': (V((0, ys, 1.16)), V((0, yc, 1.38))), 'neck': (V((0, yc, 1.43)), V((0, yn, 1.575))),
         'head': (V((0, yn, 1.575)), V((0, yn, 1.78)))}
    L = {'clavicle': (V((0.025, S.y, S.z + 0.008)), S), 'upper_arm': (S, E), 'forearm': (E, W), 'hand': (W, Ht),
         'thigh': (H, K), 'shin': (K, A), 'foot': (A, ball), 'toe': (ball, toe_t)}
    for k_, (h, t) in L.items():
        J[k_ + '.L'] = (h.copy(), t.copy()); J[k_ + '.R'] = (mx(h), mx(t))
    return J

def heat_weights(bm, J, P):
    """Blender automatic (bone heat) weights for the full base body; returns bmesh with deform layer + group names."""
    me = bpy.data.meshes.new('__wt_body'); bm.to_mesh(me)
    ob = bpy.data.objects.new('__wt_body', me); bpy.context.scene.collection.objects.link(ob)
    arm = make_armature_data('__wt_arm', J, P, {})
    ao = bpy.data.objects.new('__wt_arm', arm); bpy.context.scene.collection.objects.link(ao)
    vl = bpy.context.view_layer; vl.update()
    for o in vl.objects:
        if o: o.select_set(False)
    ob.select_set(True); ao.select_set(True); vl.objects.active = ao
    bpy.ops.object.parent_set(type='ARMATURE_AUTO')
    names = [g.name for g in ob.vertex_groups]
    out = bmesh.new(); out.from_mesh(me)
    bpy.data.objects.remove(ob); bpy.data.objects.remove(ao); bpy.data.meshes.remove(me); bpy.data.armatures.remove(arm)
    return out, names

def _wdict(v, dl, names):
    w = sorted(((names[g], x) for g, x in v[dl].items() if x > 0.01), key=lambda kv: -kv[1])[:4]
    t = sum(x for _, x in w) or 1.0
    return {k: x / t for k, x in w}

def garment(bmW, names, cuts, select, offset, mat, fold=0.006):
    """Faces of the weighted body that pass `select(face)` after plane `cuts`, pushed out by offset(co, normal).
    Open boundaries get an inward hem fold so there is never a see-through gap to the skin."""
    b = bmW.copy()
    for co, no, region in cuts:
        geom = b.verts[:] + b.edges[:] + b.faces[:] if region is None else \
            list({e for f in b.faces if region(f) for e in f.edges}) + [f for f in b.faces if region(f)] + list({v for f in b.faces if region(f) for v in f.verts})
        bmesh.ops.bisect_plane(b, geom=geom, plane_co=co, plane_no=no)
    b.normal_update()
    dl = b.verts.layers.deform.active
    faces = [f for f in b.faces if select(f)]
    fs = set(faces)
    mb = MB(); vm = {}
    def nv(v):
        if v not in vm:
            mb.wfn = (lambda p, v=v: _wdict(v, dl, names))
            vm[v] = mb.add(v.co + v.normal * offset(v.co, v.normal))
        return vm[v]
    for f in faces:
        mb.face([nv(v) for v in f.verts], mat(f) if callable(mat) else mat)
    if fold:
        for f in faces:
            for lo in f.loops:
                e = lo.edge
                if sum(1 for g in e.link_faces if g in fs) != 1: continue
                a, c = lo.vert, lo.link_loop_next.vert
                mb.wfn = (lambda p, v=a: _wdict(v, dl, names)); ai = mb.add(a.co - a.normal * fold)
                mb.wfn = (lambda p, v=c: _wdict(v, dl, names)); ci = mb.add(c.co - c.normal * fold)
                m = mat(f) if callable(mat) else mat
                mb.face([vm[c], vm[a], ai, ci], m)
    mb.wfn = None
    b.free()
    return mb

def _seg_t(p, a, b): ab = b - a; return (p - a).dot(ab) / ab.length_squared

def regions(J):
    """Helpers that classify body faces by limb (both sides)."""
    def limb(f, bone):
        c = f.calc_center_median(); best = None
        for sd in ('L', 'R'):
            a, b = J[bone + '.' + sd]; t = _seg_t(c, a, b)
            d = seg_dist(c, a, b)
            if best is None or d < best[0]: best = (d, t, sd)
        return best
    def arm_t(f):
        """position along the arm chain: 0 shoulder, 1 elbow, 2 wrist, 3 hand tip; None if not on an arm."""
        c = f.calc_center_median()
        sd = 'L' if c.x > 0 else 'R'
        S, E = J['upper_arm.' + sd]; _, W = J['forearm.' + sd]; _, Ht = J['hand.' + sd]
        du, tu = seg_dist(c, S, E), _seg_t(c, S, E)
        if abs(c.x) < abs(S.x) - 0.01 and tu < 0.25: return None       # torso side / armpit
        dfo, tfo = seg_dist(c, E, W), _seg_t(c, E, W)
        dh, th = seg_dist(c, W, Ht), _seg_t(c, W, Ht)
        dt = min(seg_dist(c, *J['chest']), seg_dist(c, *J['spine']))
        m = min(du, dfo, dh)
        if dh < 0.12 and dh <= min(du, dfo) + 1e-9: return 2 + max(0.0, min(1.0, th))   # whole hand incl. fingertips
        if m > 0.085 or dt < m: return None
        if m == du: return max(0.0, min(1.0, tu))
        if m == dfo: return 1 + max(0.0, min(1.0, tfo))
        return 2 + max(0.0, min(1.0, th))
    return arm_t

def arm_plane(J, sd, s):
    """Plane perpendicular to the arm chain at chain parameter s (0 shoulder, 1 elbow, 2 wrist)."""
    S, E = J['upper_arm.' + sd]; _, W = J['forearm.' + sd]
    if s <= 1: return S + (E - S) * s, (E - S).normalized()
    return E + (W - E) * (s - 1), (W - E).normalized()

def build_variant_base(sex, outfit):
    P = params(sex); fem = False
    _HEADSURF.clear()
    bm0 = base_bmesh()
    P['head_c'] = HEAD_C.copy()
    J = base_landmarks(bm0)
    P['shoulder'] = J['upper_arm.L'][0].copy(); P['hip'] = J['thigh.L'][0].copy()
    P['neck'] = J['neck']; P['pack_sz'] = 1.425; P['arm_angle'] = math.degrees((J['upper_arm.L'][1] - J['upper_arm.L'][0]).angle(V((0, 0, -1))))
    bmW, names = heat_weights(bm0, J, P)
    arm_t = regions(J)
    skin = 'Skin_Male'
    body, outfitm, hair = MB(), MB(), MB()
    z_neck_cut = 1.598                       # base neck continues up inside our head
    sleeve_s = 0.45 if outfit == 'Casual' else 0.5
    def cut_arm(s):
        return [(arm_plane(J, sd, s)[0], arm_plane(J, sd, s)[1], (lambda f, sd=sd: (arm_t(f) is not None) and ((f.calc_center_median().x > 0) == (sd == 'L'))))
                for sd in ('L', 'R')]
    horiz = lambda z: (V((0, 0, z)), V((0, 0, 1)), None)
    fz = lambda f: f.calc_center_median().z
    is_arm = lambda f: arm_t(f) is not None
    # ---------------- visible skin: neck + hands (casual) / + arms below the sleeve (uniform)
    vis_from = 1.97 if outfit == 'Casual' else sleeve_s - 0.02
    tex_dir = os.path.join(os.environ.get('LP_OUT') or r"C:\Users\pajar\Documents\GitHub\CyberGame\ArtSource\LowPolyStudents", 'textures')
    os.makedirs(tex_dir, exist_ok=True)
    use_face = USE_FACE_IMPORT and os.path.exists(FACE_FBX)
    z_stub = 1.47
    if use_face:
        # transplanted face: imported head shell + painted eyes, neck tucked under the collar
        fb = load_face(V((0, HEAD_C.y - 0.082, HEAD_C.z + 0.002))).copy()
        close_and_trim(fb, 1.462)
        if 'skin_hex' not in _FACE:   # tint once; both outfits share the face texture and skin tone
            img, _FACE['skin_hex'] = tinted_face_texture(os.path.join(tex_dir, 'Face_Skin.png'), PAL['Skin_Male'][0])
            _FACE['scalp_px'] = repaint_scalp(img, fb, HEAD_C, PAL['Hair'][0])
        skin_hex = _FACE['skin_hex']; PAL['Skin_Male'] = (skin_hex, PAL['Skin_Male'][1])
        m_ = bpy.data.materials.get('Skin_Male')
        if m_:
            m_.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = srgb(skin_hex); m_.diffuse_color = srgb(skin_hex)
        fmb = MB(); wn = W_neck(J, P); uvl = fb.loops.layers.uv.active
        fmb.wfn = wn; vid = {}
        for v in fb.verts: vid[v] = fmb.add(v.co.copy())
        for f in fb.faces:
            fmb.face([vid[v] for v in f.verts], 'Face_Tex')
            if uvl: fmb.fuv[len(fmb.f) - 1] = [tuple(lo[uvl].uv) for lo in f.loops]
        fmb.wfn = None; fb.free()
        _HEADSURF['bvh'] = fmb.bvh()
        skin_sel = lambda f: (1.45 < fz(f) < z_stub and not is_arm(f)) or (is_arm(f) and arm_t(f) > vis_from)
        skin_mb = garment(bmW, names, [horiz(1.45), horiz(z_stub)] + cut_arm(vis_from), skin_sel, lambda co, n: 0.0, skin, fold=0)
        body.merge(skin_mb); body.merge(fmb)
    else:
        # base head + neck + hands (and lower arms for the uniform) are the visible skin
        skin_mb = garment(bmW, names, [horiz(1.45)] + cut_arm(vis_from),
                          lambda f: (fz(f) > 1.45 and not is_arm(f)) or (is_arm(f) and arm_t(f) > vis_from),
                          lambda co, n: 0.0, skin, fold=0)
        hb = skin_mb.bvh(); _HEADSURF['bvh'] = hb
        skin_mb.wfn = fixed('head')
        add_face_features(skin_mb, hb, P['head_c'], skin, 'Lips_Male')
        for mir in (False, True):
            skin_mb.mirror = mir; build_ear(skin_mb, P, skin)
        skin_mb.mirror = False; skin_mb.wfn = None
        body.merge(skin_mb)
    hair.merge(build_hair_male(P))
    trnd = random.Random(5)
    if outfit == 'Casual':
        # tee: torso from hem to neckline + sleeves to mid upper arm; looser toward the hem and sleeve ends
        tee = garment(bmW, names, [horiz(0.935), horiz(1.475)] + cut_arm(sleeve_s),
                      lambda f: (0.935 < fz(f) < 1.475 and not is_arm(f)) or (is_arm(f) and arm_t(f) < sleeve_s),
                      lambda co, n: 0.011 + 0.012 * ss(1.12, 0.94, co.z) + trnd.uniform(-0.002, 0.002), 'Tee_Olive')
        cz = 1.245; PRINT.update(x0=-0.2, z0=cz - 0.2, size=0.4)
        make_print_texture(os.path.join(tex_dir, 'Tee_Skull_Print.png'), cz).pack()
        mark_print(tee, PRINT)
        outfitm.merge(tee); outer = tee.bvh()
        # rust long-sleeve undershirt: sleeves to the wrist cuff, hem band under the tee, crew collar
        under = garment(bmW, names, [horiz(0.905), horiz(0.95), horiz(1.475), horiz(1.492)] + cut_arm(sleeve_s - 0.06) + cut_arm(1.97),
                        lambda f: (is_arm(f) and sleeve_s - 0.06 < arm_t(f) < 1.97) or (not is_arm(f) and (0.905 < fz(f) < 0.95 or 1.475 < fz(f) < 1.492)),
                        lambda co, n: 0.006 + 0.011 * (co.z < 1.0) + 0.003 * (1.4 < co.z < 1.5), 'Undershirt_Rust')
        outfitm.merge(under)
        jeans_mat, shoe = 'Denim', 'boot'
    else:
        shirt = garment(bmW, names, [horiz(0.95), horiz(1.48)] + cut_arm(sleeve_s),
                        lambda f: (0.95 < fz(f) < 1.48 and not is_arm(f)) or (is_arm(f) and arm_t(f) < sleeve_s),
                        lambda co, n: 0.009 + 0.006 * ss(0.3, 0.5, 0.0), 'Shirt_White')
        outfitm.merge(shirt); shirt_bvh = shirt.bvh()
        jeans_mat, shoe = 'Trousers_Navy', 'school'
    # ---------------- jeans / trousers: straight legs, stacked hem over the boot
    def leg_off(co, n):
        sd = 'L' if co.x > 0 else 'R'
        K, A = J['shin.' + sd]; H = J['thigh.' + sd][0]
        a = (K + (A - K) * _seg_t(co, K, A)) if co.z < K.z else (H + (K - H) * max(0.0, _seg_t(co, H, K)))
        r = V((co.x - a.x, co.y - a.y, 0)).length
        straight = max(0.0, 0.07 - r) * ss(0.55, 0.4, co.z)          # fill the calf taper below the knee
        return 0.011 + straight + 0.006 * ss(0.2, 0.13, co.z) + trnd.uniform(-0.0025, 0.0025)
    ztop = 0.925 if outfit == 'Casual' else 1.0
    pants = garment(bmW, names, [horiz(0.13), horiz(ztop)], lambda f: 0.13 < fz(f) < ztop and not is_arm(f) and abs(f.calc_center_median().x) < 0.35, leg_off, jeans_mat)
    outfitm.merge(pants)
    pb = pants.bvh()
    if outfit == 'Casual':
        outfitm.wfn = W_torso(J, P)
        for sg in (1, -1):   # back pockets
            px = sg * 0.075
            raised_decal(outfitm, [(px - 0.048, 0.912), (px + 0.048, 0.912), (px + 0.044, 0.858), (px, 0.842), (px - 0.044, 0.858)], pb, 'Denim_Faded', 0.003, dirn=V((0, -1, 0)))
        outfitm.wfn = None
    outfitm.merge(build_shoe(P, J, shoe))
    if outfit == 'Uniform':
        vest = garment(bmW, names, [horiz(0.905), horiz(1.485)] + cut_arm(0.0),
                       lambda f: 0.905 < fz(f) < 1.485 and not is_arm(f) and not _in_v(f.calc_center_median()) and not _armhole(f.calc_center_median(), J),
                       lambda co, n: 0.02 + 0.004 * ss(1.1, 0.93, co.z), 'Vest_Gray')
        outfitm.merge(vest); vb = vest.bvh()
        z0, rx, ry = 1.488, 0.072, 0.068
        col = build_collar(P, J, z0, rx, ry, 'Shirt_White')
        cw = wfun(J, [('chest', .12, None), ('neck', .03, None)])     # follow the shirt neckline, not the head
        col.w = [cw(p) for p in col.v]; outfitm.merge(col)
        zV = 1.31
        tie = build_tie(P, J, shirt_bvh, z0, -(ry + 0.004), zV - 0.035); outfitm.merge(tie)
        un = MB(); un.merge(vest); un.merge(col); un.merge(tie); ub = un.bvh()
        ly, cl, cn = build_lanyard(P, J, ub, z0, ry, zV - 0.04, None); outfitm.merge(ly)
        card, ctop, cbot = build_id_card(P, J, cl, cn, False); outfitm.merge(card)
        P['id_bone'] = (ctop + V((0, 0, 0.012)), cbot)
        outfitm.wfn = W_torso(J, P)
        cx, czc = 0.088, 1.33
        decal(outfitm, [offset2(shield(P['s']), cx, czc)], vb, 'Crest_Navy', gap=0.002, maxe=0.03)
        decal(outfitm, [offset2(shield(P['s'], 0.72), cx, czc + 0.001)], vb, 'Crest_Gold', gap=0.0032, maxe=0.03)
        decal(outfitm, [offset2([(0, .009), (.006, -.002), (0, -.011), (-.006, -.002)], cx, czc)], vb, 'Crest_Green', gap=0.0044, maxe=0.03)
        outfitm.wfn = None
        o2 = MB(); o2.merge(vest); o2.merge(col); outer = o2.bvh()
    pack = build_backpack(P, J, outer)
    bmW.free(); bm0.free()
    return P, J, {'Body': body, 'Hair': hair, 'Outfit': outfitm, 'Backpack': pack}

def _in_v(c, zV=1.31, zT=1.49, xn=0.075):
    """Front V-neck opening of the vest."""
    if c.y > -0.02 or c.z <= zV: return False
    return abs(c.x) < xn * ((c.z - zV) / (zT - zV)) ** 0.85
def _armhole(c, J):
    """Sleeveless armhole region of the vest around each shoulder joint."""
    for sd in ('L', 'R'):
        S = J['upper_arm.' + sd][0]
        if (V((c.x, c.y * 0.8, c.z)) - V((S.x, S.y * 0.8, S.z + 0.025))).length < 0.098 and abs(c.x) > abs(S.x) - 0.075: return True
    return False

build_variant_generic = build_variant
def build_variant(sex, outfit):
    if sex == 'M' and os.path.exists(BASE_FBX): return build_variant_base(sex, outfit)
    return build_variant_generic(sex, outfit)
