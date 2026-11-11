"""Vivi -> female uniform from the reference sheet (no backpack), plus Idle/Walk/Run on her Mixamo rig.

Exec order inside Blender: lp_build, lp_parts, lp_head_m, lp_parts2, lp_assemble, lp_anim (our helpers),
then lp_vivi (skirt), then this file; call build_vivi_uniform().

Vivi faces +Y and is authored at ~12.4 units tall. Accessories and animation are computed in our
"character frame" (metres, faces -Y, +X = her left) and mapped back onto her rig.
"""
import numpy as np
from mathutils import Matrix, Quaternion
from mathutils.kdtree import KDTree
from mathutils.bvhtree import BVHTree

VIVI_HEIGHT_M = 1.55
# plain, loose skirt (overrides for lp_vivi)
STRAIGHT, A_LINE, CLEAR = 1.0, 0.14, 0.10
SKIRT_PLAIN_MAT = 'Skirt_Plain_Navy'
VEST_GRAY = '#A3A4A7'; VEST_TRIM = '#2E3342'
SLEEVE_T = 0.88          # sleeve hem along the upper arm (0 shoulder, 1 elbow): "approximately elbow-length"
RESTRICT = dict(walk_stride=0.82, run_stride=0.68, run_lift=0.48)   # straight skirt limits stride and knee lift

MIX = {'pelvis': 'Hips', 'spine': 'Spine', 'chest': 'Spine1', 'neck': 'Neck', 'head': 'Head'}
for _sd, _mx in (('L', 'Left'), ('R', 'Right')):
    MIX.update({f'clavicle.{_sd}': f'{_mx}Shoulder', f'upper_arm.{_sd}': f'{_mx}Arm', f'forearm.{_sd}': f'{_mx}ForeArm',
                f'hand.{_sd}': f'{_mx}Hand', f'thigh.{_sd}': f'{_mx}UpLeg', f'shin.{_sd}': f'{_mx}Leg',
                f'foot.{_sd}': f'{_mx}Foot', f'toe.{_sd}': f'{_mx}ToeBase'})

class Frame:
    """world <-> character frame (metres, -Y forward)."""
    def __init__(s, arm):
        s.arm = arm
        hips = arm.matrix_world @ arm.data.bones['mixamorig:Hips'].head_local
        top = (arm.matrix_world @ arm.data.bones['mixamorig:HeadTop_End'].head_local).z     # crown
        s.O = V((hips.x, hips.y, 0.0)); s.u = VIVI_HEIGHT_M / (top * 1.0)
        s.R = Matrix.Rotation(math.pi, 3, 'Z')          # character -> world (she faces +Y)
        s.Ri = s.R.transposed()
    def c(s, pw): return (s.Ri @ (pw - s.O)) * s.u
    def w(s, pc): return s.R @ (pc / s.u) + s.O
    def bone(s, name, tail=False):
        b = s.arm.data.bones['mixamorig:' + name]
        return s.c(s.arm.matrix_world @ (b.tail_local if tail else b.head_local))

def vivi_joints(F):
    b = F.bone
    J = {'root': (V((0, 0, 0)), V((0, 0.25, 0))),
         'pelvis': (b('Hips'), b('Spine')), 'spine': (b('Spine'), b('Spine1')), 'chest': (b('Spine1'), b('Neck')),
         'neck': (b('Neck'), b('Head')), 'head': (b('Head'), b('HeadTop_End'))}
    for sd, mx in (('L', 'Left'), ('R', 'Right')):
        J[f'clavicle.{sd}'] = (b(f'{mx}Shoulder'), b(f'{mx}Arm'))
        J[f'upper_arm.{sd}'] = (b(f'{mx}Arm'), b(f'{mx}ForeArm'))
        J[f'forearm.{sd}'] = (b(f'{mx}ForeArm'), b(f'{mx}Hand'))
        J[f'hand.{sd}'] = (b(f'{mx}Hand'), b(f'{mx}HandMiddle1'))
        J[f'thigh.{sd}'] = (b(f'{mx}UpLeg'), b(f'{mx}Leg'))
        J[f'shin.{sd}'] = (b(f'{mx}Leg'), b(f'{mx}Foot'))
        J[f'foot.{sd}'] = (b(f'{mx}Foot'), b(f'{mx}ToeBase'))
        J[f'toe.{sd}'] = (b(f'{mx}ToeBase'), b(f'{mx}Toe_End'))
    return J

def vivi_params(J):
    P = params('F'); P['skirt'] = True; P.update(RESTRICT)
    P['shoulder'] = J['upper_arm.L'][0].copy(); P['hip'] = J['thigh.L'][0].copy(); P['neck'] = J['neck']
    P['arm_angle'] = math.degrees((J['upper_arm.L'][1] - J['upper_arm.L'][0]).angle(V((0, 0, -1))))
    return P

# ------------------------------------------------------------------ mesh helpers on her body
def islands(bm, faces):
    ss_ = set(faces); seen = set(); out = []
    for f in faces:
        if f in seen: continue
        st, comp = [f], []
        while st:
            x = st.pop()
            if x in seen: continue
            seen.add(x); comp.append(x)
            for e in x.edges:
                for g in e.link_faces:
                    if g in ss_ and g not in seen: st.append(g)
        out.append(comp)
    return out

def tex_of(mat):
    return next((n.image for n in mat.node_tree.nodes if n.type == 'TEX_IMAGE' and n.image), None) if mat and mat.node_tree else None

def face_color(px, uvl, f):
    h, w = px.shape[:2]; u = sum((lo[uvl].uv for lo in f.loops), V((0, 0))) / len(f.loops)
    return px[int(min(max(u.y, 0), 0.999) * h), int(min(max(u.x, 0), 0.999) * w), :3]

def raster_uv(faces, uvl, w, h):
    mask = np.zeros((h, w), bool)
    for f in faces:
        uv = [lo[uvl].uv for lo in f.loops]
        for i in range(1, len(uv) - 1):
            tri = np.array([(uv[0].x * w, uv[0].y * h), (uv[i].x * w, uv[i].y * h), (uv[i + 1].x * w, uv[i + 1].y * h)])
            x0, y0 = np.floor(tri.min(0)).astype(int); x1, y1 = np.ceil(tri.max(0)).astype(int)
            x0, y0, x1, y1 = max(x0, 0), max(y0, 0), min(x1, w - 1), min(y1, h - 1)
            if x1 < x0 or y1 < y0: continue
            X, Y = np.meshgrid(np.arange(x0, x1 + 1) + 0.5, np.arange(y0, y1 + 1) + 0.5)
            (ax, ay), (bx, by), (cx, cy) = tri
            d = (by - cy) * (ax - cx) + (cx - bx) * (ay - cy)
            if abs(d) < 1e-9: continue
            l1 = ((by - cy) * (X - cx) + (cx - bx) * (Y - cy)) / d; l2 = ((cy - ay) * (X - cx) + (ax - cx) * (Y - cy)) / d
            mask[y0:y1 + 1, x0:x1 + 1] |= (l1 >= -0.01) & (l2 >= -0.01) & (1 - l1 - l2 >= -0.01)
    return mask

def _hex(hx): return np.array([int(hx[i:i + 2], 16) / 255 for i in (1, 3, 5)], np.float32)

def dilate(m, r):
    out = m.copy()
    for dy in range(-r, r + 1):
        for dx in range(-r, r + 1):
            if dx * dx + dy * dy <= r * r: out |= np.roll(np.roll(m, dy, 0), dx, 1)
    return out

# ------------------------------------------------------------------ uniform edits
def recolor_vest(body):
    """Navy knit vest -> light gray (knit shading kept), dark trim painted along the V-neck and armholes."""
    me = body.data
    tops = [i for i, m in enumerate(me.materials) if m and 'Tops' in m.name]
    img = tex_of(me.materials[tops[0]]); w, h = img.size
    px = np.array(img.pixels[:], np.float32).reshape(h, w, 4)
    bm = bmesh.new(); bm.from_mesh(me); uvl = bm.loops.layers.uv.active
    isl = islands(bm, [f for f in bm.faces if f.material_index in tops])
    vest = max(isl, key=len)                                     # the knit vest shell
    mask = raster_uv(vest, uvl, w, h); bm.free()
    rgb = px[..., :3]; lum = rgb.mean(2)
    knit = mask & (lum < 0.45)
    ref = np.median(lum[knit])
    gray = _hex(VEST_GRAY)
    rgb[knit] = (gray[None, :] * (lum[knit] / ref).clip(0.55, 1.35)[:, None]).clip(0, 1)
    # trim: vest pixels next to the unused texture area inside the V and the armhole notches (not the hem, not the shoulder seams)
    rows = np.where(mask.any(1))[0]; vtop, vbot = rows.max(), rows.min()
    cols = np.where(mask.any(0))[0]; cl, cr = cols.min(), cols.max()
    inside = ~mask
    band = np.zeros_like(mask); band[vbot + int(0.18 * (vtop - vbot)): vtop - 6, cl + 16: cr - 16] = True   # V + armholes, not hem/seams
    trim = mask & dilate(inside & band, 11) & band
    rgb[trim] = _hex(VEST_TRIM)
    px[..., :3] = rgb
    img.pixels.foreach_set(px.ravel()); img.pack()
    return int(knit.sum()), int(trim.sum())

def remove_bow(body, F):
    """Delete the saturated-blue bow pieces at the front of the collar (the tie replaces them)."""
    me = body.data; M = body.matrix_world
    tops = {i for i, m in enumerate(me.materials) if m and 'Tops' in m.name}
    img = tex_of(me.materials[min(tops)]); w, h = img.size
    px = np.array(img.pixels[:], np.float32).reshape(h, w, 4)
    bm = bmesh.new(); bm.from_mesh(me); uvl = bm.loops.layers.uv.active
    neck = F.bone('Neck'); kill = []
    for comp in islands(bm, [f for f in bm.faces if f.material_index in tops]):
        if len(comp) > 150: continue
        cs = [F.c(M @ f.calc_center_median()) for f in comp]; c = sum(cs, V()) / len(cs)
        if not (c.y < -0.02 and abs(c.x) < 0.07 and neck.z - 0.12 < c.z < neck.z + 0.02): continue    # in front of the collar
        col = np.mean([face_color(px, uvl, f) for f in comp], axis=0)
        sat = col.max() - col.min()
        if sat > 0.18 or col.mean() < 0.12: kill += comp                                          # blue bow / its dark knot
    bmesh.ops.delete(bm, geom=kill, context='FACES_ONLY')
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')
    bm.to_mesh(me); bm.free(); me.update()
    return len(kill)

def shorten_sleeves(body, F, J):
    """Cut both shirt sleeves just above the elbow (clean plane cut), drop the cuffs, fold the hem inward."""
    me = body.data; M = body.matrix_world; Mi = M.inverted()
    tops = {i for i, m in enumerate(me.materials) if m and 'Tops' in m.name}
    bm = bmesh.new(); bm.from_mesh(me)
    for sd in ('L', 'R'):
        S, E = J[f'upper_arm.{sd}']
        cut_c = S + (E - S) * SLEEVE_T; nrm_c = (E - S).normalized()
        cw, nw = F.w(cut_c), (F.R @ nrm_c).normalized()
        co_l, no_l = Mi @ cw, (Mi.to_3x3() @ nw).normalized()
        sleeve = [f for f in bm.faces if f.material_index in tops and
                  (F.c(M @ f.calc_center_median()) - S).dot(nrm_c) > 0.02 and
                  ((F.c(M @ f.calc_center_median()).x > 0) == (sd == 'L'))]
        geom = list({e for f in sleeve for e in f.edges}) + sleeve + list({v for f in sleeve for v in f.verts})
        bmesh.ops.bisect_plane(bm, geom=geom, plane_co=co_l, plane_no=no_l)
        bm.faces.ensure_lookup_table()
        beyond = [f for f in bm.faces if f.material_index in tops and
                  ((F.c(M @ f.calc_center_median()).x > 0) == (sd == 'L')) and
                  (F.c(M @ f.calc_center_median()) - cut_c).dot(nrm_c) > 0.0]
        bmesh.ops.delete(bm, geom=beyond, context='FACES_ONLY')
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')
    # (no hem fold: the arm skin sits right under the open sleeve edge)
    bm.to_mesh(me); bm.free(); me.update()
    return 0

def plain_skirt_material(body, S_world_ids=None):
    me = body.data
    bot = {i for i, m in enumerate(me.materials) if m and 'Bottoms' in m.name}
    m = bpy.data.materials.get(SKIRT_PLAIN_MAT)
    if not m:
        m = bpy.data.materials.new(SKIRT_PLAIN_MAT)
        try: m.use_nodes = True
        except Exception: pass
        b = m.node_tree.nodes['Principled BSDF']; b.inputs['Base Color'].default_value = srgb(PAL['Skirt_Navy'][0]); b.inputs['Roughness'].default_value = 0.85
        m.diffuse_color = srgb(PAL['Skirt_Navy'][0])
    if SKIRT_PLAIN_MAT not in [x.name for x in me.materials if x]: me.materials.append(m)
    mi = [x.name if x else '' for x in me.materials].index(SKIRT_PLAIN_MAT)
    arm = body.find_armature(); knee = arm.matrix_world @ arm.data.bones['mixamorig:LeftLeg'].head_local
    M = body.matrix_world; n = 0
    for p in me.polygons:
        if p.material_index in bot and (M @ p.center).z > knee.z - 0.3:
            p.material_index = mi; n += 1
    return n

# ------------------------------------------------------------------ accessories (tie, crest, lanyard, ID)
def body_bvh_char(body, F, mats_key=('Tops', 'SKIN')):
    me = body.data; M = body.matrix_world
    keep = {i for i, m in enumerate(me.materials) if m and any(k in m.name for k in mats_key)}
    vs = [F.c(M @ v.co) for v in me.vertices]
    fs = [tuple(p.vertices) for p in me.polygons if p.material_index in keep]
    return BVHTree.FromPolygons([tuple(v) for v in vs], fs), vs, fs

def add_accessories(info, F, J, P):
    body, arm = info['body'], info['arm']
    me = body.data
    tops = {i for i, m in enumerate(me.materials) if m and 'Tops' in m.name}
    bv, vs, _ = body_bvh_char(body, F, ('Tops',))
    neck0 = J['neck'][0]
    # collar front: the highest shirt point right in front of the neck, where the collar points meet
    M = body.matrix_world
    front = [F.c(M @ me.vertices[v].co) for p in me.polygons if p.material_index in tops for v in p.vertices]
    front = [q for q in front if abs(q.x) < 0.03 and q.y < neck0.y - 0.015 and neck0.z - 0.08 < q.z < neck0.z + 0.08]
    top_pt = max(front, key=lambda q: q.z)
    z0 = top_pt.z - 0.012
    fy = top_pt.y                  # the collar is open at the very front, so take the collar point itself
    ry = -(fy - neck0.y)
    # V bottom: walking down the centre line the hit goes deeper (shirt inside the V) until the vest jumps forward
    zV = z0 - 0.12; prev = None
    for k in range(80):
        z = z0 - 0.015 - 0.004 * k
        h_ = bv.ray_cast(V((0, -1, z)), V((0, 1, 0)))
        if h_[0] is None: continue
        if prev is not None and h_[0].y < prev - 0.008: zV = z; break
        prev = h_[0].y
    mb = MB()
    tie = build_tie(P, J, bv, z0, fy - 0.003, zV - 0.03); mb.merge(tie)
    un = MB(); un.merge(tie); ubv = un.bvh()
    # lanyard rides on whichever is outer: tie or shirt/vest
    class Both:
        def find_nearest(self, p):
            a, b = bv.find_nearest(p), ubv.find_nearest(p)
            return a if (b[0] is None or (a[0] is not None and a[3] <= b[3])) else b
        def ray_cast(self, o, d, *a): return bv.ray_cast(o, d, *a)
    ly, cl, cn = build_lanyard(P, J, Both(), z0, ry, zV - 0.045, None); mb.merge(ly)
    card, ctop, cbot = build_id_card(P, J, cl, cn, True); mb.merge(card)
    mb.wfn = None
    cx, cz = 0.068, J['upper_arm.L'][0].z - 0.075
    decal(mb, [offset2(shield(P['s']), cx, cz)], bv, 'Crest_Navy', gap=0.0025, maxe=0.03)
    decal(mb, [offset2(shield(P['s'], 0.72), cx, cz + 0.001)], bv, 'Crest_Gold', gap=0.0037, maxe=0.03)
    decal(mb, [offset2([(0, .009), (.006, -.002), (0, -.011), (-.006, -.002)], cx, cz)], bv, 'Crest_Green', gap=0.0049, maxe=0.03)
    # Blender object in world space, parented to her rig, weights copied from the nearest body vertex
    M = body.matrix_world
    kd = KDTree(len(me.vertices))
    for v in me.vertices: kd.insert(M @ v.co, v.index)
    kd.balance()
    names = {g.index: g.name for g in body.vertex_groups}
    obj_me = bpy.data.meshes.new('Vivi_Uniform_Accessories')
    obj_me.from_pydata([tuple(F.w(p)) for p in mb.v], [], mb.f)
    for m in mb.mats: obj_me.materials.append(get_mat(m))
    obj_me.polygons.foreach_set('material_index', mb.fm)
    obj_me.update()
    ob = bpy.data.objects.new('Vivi_Uniform_Accessories', obj_me)
    info['coll'].objects.link(ob)
    ob.parent = arm; ob.matrix_parent_inverse = arm.matrix_world.inverted()
    md = ob.modifiers.new('Armature', 'ARMATURE'); md.object = arm
    groups = {}
    for i, p in enumerate(mb.v):
        _, vi, _ = kd.find(F.w(p))
        for g in me.vertices[vi].groups:
            n = names[g.group]
            gg = groups.get(n) or ob.vertex_groups.new(name=n); groups[n] = gg
            gg.add([i], g.weight, 'REPLACE')
    for p in obj_me.polygons: p.use_smooth = False
    info['objs'].append(ob)
    return dict(tie_z=(z0, zV), collar_front=fy, faces=len(mb.f))

# ------------------------------------------------------------------ animation on the Mixamo rig
def bake_vivi(info, F, J, P):
    arm = info['arm']
    rest_q = {b.name: b.matrix_local.to_quaternion() for b in arm.data.bones}
    Raw = arm.matrix_world.to_quaternion(); s_arm = arm.matrix_world.to_scale().x
    Rcw = F.R.to_quaternion()
    Rac = Raw.inverted() @ Rcw              # character-frame rotation -> armature-local
    ad = arm.animation_data_create()
    for tr in list(ad.nla_tracks): ad.nla_tracks.remove(tr)
    acts = []
    for clip in ('Idle', 'Walk', 'Run'):
        T = CLIPS[clip]['T']; extra = {}
        if clip != 'Idle':
            extra['gait'] = Gait(J, clip, skirt=True)
            g = extra['gait']
            if clip == 'Walk': g.Ds *= P['walk_stride']; g.y0 = -g.Ds / 2 - 0.012
            else: g.Ds *= P['run_stride']; g.y0 = -g.Ds / 2 - 0.012; g.run_lift = P['run_lift']
            ps = [f / T for f in range(T)]
            for p in ps: body_pose(J, P, clip, p, extra)
            raw = [extra['zraw'][p] for p in ps]; sm = raw[:]
            sm = [min(raw[i], (sm[i - 1] + 2 * sm[i] + sm[(i + 1) % T]) / 4) for i in range(T)]
            extra['zs'] = dict(zip(ps, sm)); extra['miss'] = []
        nm = f'Vivi_{clip}'
        for old in [a for a in bpy.data.actions if a.name == nm or a.name.startswith(nm + '.')]: bpy.data.actions.remove(old)
        act = bpy.data.actions.new(nm); act.use_fake_user = True
        ad.action = act
        prev = {}
        for f in range(T + 1):
            p = (f % T) / T
            R, off = body_pose(J, P, clip, p, extra)
            for ours, mx in MIX.items():
                pb = arm.pose.bones.get('mixamorig:' + mx)
                if pb is None: continue
                pb.rotation_mode = 'QUATERNION'
                qa = Rac @ R.get(ours, Quaternion()) @ Rac.inverted()
                r = rest_q[pb.name]; b = r.inverted() @ qa @ r
                if pb.name in prev and prev[pb.name].dot(b) < 0: b.negate()
                prev[pb.name] = b
                pb.rotation_quaternion = b
                pb.keyframe_insert('rotation_quaternion', frame=f, group=pb.name)
                if ours == 'pelvis':
                    off_w = F.R @ (off / F.u)
                    pb.location = r.inverted() @ (Raw.inverted() @ off_w) / s_arm
                    pb.keyframe_insert('location', frame=f, group=pb.name)
            for bn, qc in (FINGER_CURLS.get(clip, {}) if 'FINGER_CURLS' in globals() else {}).items():
                pb = arm.pose.bones.get(bn)
                if pb is None: continue
                pb.rotation_mode = 'QUATERNION'
                r = rest_q[bn]; b = r.inverted() @ (Rac @ qc @ Rac.inverted()) @ r
                pb.rotation_quaternion = b
                pb.keyframe_insert('rotation_quaternion', frame=f, group=bn)
        for fc in iter_fcurves(act):
            for kp in fc.keyframe_points: kp.interpolation = 'LINEAR'
        act.frame_range = (0, T); act.use_frame_range = True; act.use_cyclic = True
        acts.append((clip, act))
    ad.action = None
    for clip, act in acts:
        tr = ad.nla_tracks.new(); tr.name = clip
        st = tr.strips.new(clip, 0, act)
        try: st.action_slot = act.slots[0]
        except Exception: pass
        tr.mute = clip != 'Idle'
    for pb in arm.pose.bones:
        pb.rotation_quaternion = (1, 0, 0, 0); pb.location = (0, 0, 0)
    return [a.name for _, a in acts]

def build_vivi_uniform():
    G_ = globals()
    G_['STRAIGHT'], G_['A_LINE'], G_['CLEAR'] = STRAIGHT, A_LINE, CLEAR
    info = build_vivi()
    arm, body = info['arm'], info['body']
    F = Frame(arm); J = vivi_joints(F); P = vivi_params(J)
    rep = dict(skirt_hem=info['hem_after'])
    rep['skirt_faces_plain'] = plain_skirt_material(body)
    rep['bow_faces_removed'] = remove_bow(body, F)
    rep['sleeve_hem_edges'] = shorten_sleeves(body, F, J)
    rep['vest_px'], rep['trim_px'] = recolor_vest(body)
    rep['accessories'] = add_accessories(info, F, J, P)
    rep['actions'] = bake_vivi(info, F, J, P)
    rep['arm_angle'] = round(P['arm_angle'], 1)
    info.update(F=F, J=J, P=P)
    return info, rep

# ------------------------------------------------------------------ recolor-only variant (original model, pleats kept)
HAIR_DARK, HAIR_LIGHT = '#0E0E12', '#3C3D47'
BOW_NAVY = '#26304F'

def recolor_hair(objs):
    """Hair textures -> black, keeping the painted strand shading as brightness."""
    done = set()
    for o in objs:
        if o.type != 'MESH' or not o.name.startswith('Hair'): continue
        for m in o.data.materials:
            img = tex_of(m)
            if not img or img.name in done: continue
            w, h = img.size; px = np.array(img.pixels[:], np.float32).reshape(h, w, 4)
            lum = (px[..., :3] @ np.array([0.3, 0.59, 0.11], np.float32))
            lo, hi = np.percentile(lum[px[..., 3] > 0.5], [5, 98]) if (px[..., 3] > 0.5).any() else (0.0, 1.0)
            t = ((lum - lo) / max(hi - lo, 1e-3)).clip(0, 1)[..., None] ** 1.4
            px[..., :3] = _hex(HAIR_DARK) * (1 - t) + _hex(HAIR_LIGHT) * t
            img.pixels.foreach_set(px.ravel()); img.pack(); done.add(img.name)
    return len(done)

def recolor_bow(body, F):
    """Blue bow/collar ribbon pixels -> navy (same shading); geometry untouched."""
    me = body.data; M = body.matrix_world
    tops = {i for i, m in enumerate(me.materials) if m and 'Tops' in m.name}
    img = tex_of(me.materials[min(tops)]); w, h = img.size
    px = np.array(img.pixels[:], np.float32).reshape(h, w, 4)
    bm = bmesh.new(); bm.from_mesh(me); uvl = bm.loops.layers.uv.active
    neck = F.bone('Neck'); faces = []
    for comp in islands(bm, [f for f in bm.faces if f.material_index in tops]):
        if len(comp) > 150: continue
        cs = [F.c(M @ f.calc_center_median()) for f in comp]; c = sum(cs, V()) / len(cs)
        if abs(c.x) < 0.09 and neck.z - 0.14 < c.z < neck.z + 0.04: faces += comp      # collar ribbon pieces
    mask = raster_uv(faces, uvl, w, h); bm.free()
    rgb = px[..., :3]; sat = rgb.max(2) - rgb.min(2)
    blue = mask & (rgb[..., 2] > rgb[..., 0] + 0.08) & (sat > 0.12)
    lum = rgb.mean(2); ref = np.median(lum[blue]) if blue.any() else 0.5
    rgb[blue] = (_hex(BOW_NAVY)[None, :] * (lum[blue] / ref).clip(0.5, 1.6)[:, None]).clip(0, 1)
    px[..., :3] = rgb; img.pixels.foreach_set(px.ravel()); img.pack()
    return int(blue.sum())

def build_vivi_recolor():
    G_ = globals()
    G_['STRAIGHT'], G_['A_LINE'], G_['CLEAR'] = 0.0, 0.0, 0.06     # keep the original pleats: only lengthen
    info = build_vivi()
    arm, body = info['arm'], info['body']
    F = Frame(arm); J = vivi_joints(F); P = vivi_params(J)
    rep = dict(skirt_hem=info['hem_after'])
    rep['vest_px'], rep['trim_px'] = recolor_vest(body)
    rep['bow_px'] = recolor_bow(body, F)
    rep['hair_textures'] = recolor_hair(info['objs'])
    rep['actions'] = bake_vivi(info, F, J, P)
    info.update(F=F, J=J, P=P)
    return info, rep
