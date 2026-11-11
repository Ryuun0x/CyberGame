"""Vivi (VRoid-style student girl) -> female student matching the uniform sheet: knee-length straight skirt.

Only the skirt changes. Everything else (face, hair, shirt, vest, legs, rig) is the original model.
Run inside Blender: exec this file, then build_vivi(). The source .blend is only read, never modified.
"""
import bpy, bmesh, math, os
from mathutils import Vector as V

VIVI_BLEND = os.environ.get('LP_VIVI') or r"C:\Users\pajar\Downloads\vivi\source\Vivi.blend"
OUT_DIR = r"C:\Users\pajar\Documents\GitHub\CyberGame\ArtSource\LowPolyStudents\female_vivi"
SKIRT_MAT = 'Bottoms'          # material name fragment shared by skirt + shoes; skirt = the part above the knees
HEM_BELOW_KNEE = -0.07         # hem height relative to the knee joint (model units; ~ -1 cm): "ends around the knees"
STRAIGHT = 0.85                # how much of the pleat/flare is pulled onto the smooth hip outline
A_LINE = 0.08                  # extra hem width relative to the hips

def ss(e0, e1, x):
    t = max(0.0, min(1.0, (x - e0) / (e1 - e0))); return t * t * (3 - 2 * t)

def load_vivi(coll_name='Vivi_Student'):
    coll = bpy.data.collections.get(coll_name)
    if coll:
        for o in list(coll.objects): bpy.data.objects.remove(o)
    else:
        coll = bpy.data.collections.new(coll_name); bpy.context.scene.collection.children.link(coll)
    with bpy.data.libraries.load(VIVI_BLEND, link=False) as (src, dst):
        dst.objects = list(src.objects)
    objs = [o for o in dst.objects if o]
    for o in objs: coll.objects.link(o)
    arm = next(o for o in objs if o.type == 'ARMATURE')
    body = next(o for o in objs if o.type == 'MESH' and o.name.startswith('Body'))
    return coll, arm, body, objs

def rest(arm):
    ad = arm.animation_data
    if ad: ad.action = None
    for pb in arm.pose.bones:
        pb.rotation_quaternion = (1, 0, 0, 0); pb.rotation_euler = (0, 0, 0); pb.location = (0, 0, 0); pb.scale = (1, 1, 1)
    bpy.context.view_layer.update()

def lengthen_skirt(arm, body):
    me = body.data; M = body.matrix_world; Mi = M.inverted()
    bot = {i for i, m in enumerate(me.materials) if m and SKIRT_MAT in m.name}
    knee = arm.matrix_world @ arm.data.bones['mixamorig:LeftLeg'].head_local
    cand = {v for p in me.polygons if p.material_index in bot for v in p.vertices}
    W = {i: M @ me.vertices[i].co for i in cand}
    S = [i for i in cand if W[i].z > knee.z + 0.4]                     # the skirt, not the shoes
    top = max(W[i].z for i in S); hem0 = min(W[i].z for i in S); hem1 = knee.z + HEM_BELOW_KNEE
    cx = sum(W[i].x for i in S) / len(S); cy = sum(W[i].y for i in S) / len(S)
    t_of = {i: (top - W[i].z) / (top - hem0) for i in S}
    # smooth outer outline at the waist and the hips, per angle bin
    NB = 24
    def envelope(t0, t1):
        env = [0.0] * NB
        for i in S:
            if t0 <= t_of[i] <= t1:
                d = W[i] - V((cx, cy, 0)); a = math.atan2(d.y, d.x); b = int((a + math.pi) / (2 * math.pi) * NB) % NB
                env[b] = max(env[b], math.hypot(d.x, d.y))
        env = [max(env[k - 1], env[k], env[(k + 1) % NB]) for k in range(NB)]      # outer envelope, no empty bins
        for _ in range(2):
            env = [max(env[k], (env[k - 1] + 2 * env[k] + env[(k + 1) % NB]) / 4) for k in range(NB)]   # smooth, never shrink
        return env
    e_waist, e_hip = envelope(0.0, 0.12), envelope(0.28, 0.45)
    def R(a, t):
        f = (a + math.pi) / (2 * math.pi) * NB - 0.5; k0 = int(math.floor(f)) % NB; k1 = (k0 + 1) % NB; u = f - math.floor(f)
        w_ = e_waist[k0] * (1 - u) + e_waist[k1] * u; h_ = e_hip[k0] * (1 - u) + e_hip[k1] * u
        if t < 0.4: return w_ + (h_ - w_) * ss(0.0, 0.4, t)
        return h_ * (1 + A_LINE * (t - 0.4) / 0.6)
    for i in S:
        t = t_of[i]; p = W[i]
        d = V((p.x - cx, p.y - cy)); a = math.atan2(d.y, d.x); r = d.length
        tgt = R(a, t)
        if t < 0.45: tgt = max(tgt, r)        # at the hips only push out (pleats open), never into the body
        rn = r + (tgt - r) * STRAIGHT * ss(0.22, 0.5, t)     # untouched under the vest hem
        # waistband and hips stay where they are (under the vest); only the part below the hip line is stretched
        T0 = 0.35; z0 = top - T0 * (top - hem0)
        z = p.z if t <= T0 else z0 - (t - T0) / (1 - T0) * (z0 - hem1)
        me.vertices[i].co = Mi @ V((cx + math.cos(a) * rn, cy + math.sin(a) * rn, z))
    me.update()
    push_out(body, set(S), bot, {i: t_of[i] for i in S})
    return S, t_of, (cx, cy), (top, hem0, hem1)

CLEAR = 0.06    # minimum gap between skirt and body (model units, ~7 mm)

def push_out(body, S, bot, t_of=None):
    """Skirt vertices that ended up inside (or too close to) the body are moved out along the body normal."""
    from mathutils.bvhtree import BVHTree
    me = body.data; M = body.matrix_world; Mi = M.inverted()
    skin = {i for i, m in enumerate(me.materials) if m and 'SKIN' in m.name}      # body only, not the vest over the waistband
    keep = [p for p in me.polygons if p.material_index in skin]
    verts = [M @ v.co for v in me.vertices]
    bvh = BVHTree.FromPolygons([tuple(v) for v in verts], [tuple(p.vertices) for p in keep])
    moved = 0
    for _ in range(2):
        for i in S:
            p = M @ me.vertices[i].co
            loc, n, _, d = bvh.find_nearest(p)
            if loc is None: continue
            side = (p - loc).dot(n)
            if t_of is not None and t_of[i] < 0.25: continue        # under the vest hem: leave the waistband where it was
            gap = CLEAR if t_of is None else 0.02 + (CLEAR - 0.02) * ss(0.25, 0.45, t_of[i])
            if side < gap:
                me.vertices[i].co = Mi @ (loc + n * gap); moved += 1
    me.update()
    return moved

def reweight_skirt(body, S, t_of, center):
    """Upper skirt copies the weights of the skin right under it (moves with hips/thighs, no poke-through);
    toward the hem it blends into an even hips->thigh split by side so the longer hem follows the legs smoothly."""
    from mathutils.kdtree import KDTree
    me = body.data; M = body.matrix_world
    arm = body.find_armature()
    names = {g.index: g.name for g in body.vertex_groups}
    groups = {g.name: g for g in body.vertex_groups}
    skin_m = {i for i, m in enumerate(me.materials) if m and 'SKIN' in m.name}
    skin_v = sorted({v for p in me.polygons if p.material_index in skin_m for v in p.vertices})
    kd = KDTree(len(skin_v))
    for k, i in enumerate(skin_v): kd.insert(M @ me.vertices[i].co, k)
    kd.balance()
    lhip = arm.matrix_world @ arm.data.bones['mixamorig:LeftUpLeg'].head_local
    rhip = arm.matrix_world @ arm.data.bones['mixamorig:RightUpLeg'].head_local
    axis = (lhip - rhip); axis.z = 0; axis.normalize(); mid = (lhip + rhip) / 2
    for i in S:
        t = t_of[i]; p = M @ me.vertices[i].co
        # weights of the nearest few skin vertices (distance weighted)
        wb = {}; near = kd.find_n(p, 4); tot = 0.0
        for _, k, d in near:
            f = 1.0 / max(d, 1e-4); tot += f
            for g in me.vertices[skin_v[k]].groups: wb[names[g.group]] = wb.get(names[g.group], 0) + g.weight * f
        wb = {k: v / tot for k, v in wb.items()}
        infl = 0.9 * ss(0.25, 1.0, t) ** 0.9; side = ss(-0.3, 0.3, (p - mid).dot(axis))
        wsch = {'mixamorig:Hips': 1 - infl, 'mixamorig:LeftUpLeg': infl * side, 'mixamorig:RightUpLeg': infl * (1 - side)}
        k_ = ss(0.35, 0.8, t)
        w = {n: wb.get(n, 0) * (1 - k_) + wsch.get(n, 0) * k_ for n in set(wb) | set(wsch)}
        w = dict(sorted(((n, x) for n, x in w.items() if x > 0.02 and n in groups), key=lambda kv: -kv[1])[:4])
        tsum = sum(w.values()) or 1.0
        for g in list(me.vertices[i].groups): body.vertex_groups[g.group].remove([i])
        for n, x in w.items(): groups[n].add([i], x / tsum, 'REPLACE')

HIDE_DOWN_TO = 1.0   # hidden-body removal stops this far above the new hem (model units, ~12 cm): lifted thighs stay solid

def remove_hidden_body(body, S, hem1):
    """Delete skin faces that sit inside the skirt between the waist and mid-thigh (never visible at rest,
    and the main source of poke-through when the legs split)."""
    from mathutils.bvhtree import BVHTree
    me = body.data; M = body.matrix_world
    bot = {i for i, m in enumerate(me.materials) if m and SKIRT_MAT in m.name}
    skin = {i for i, m in enumerate(me.materials) if m and 'SKIN' in m.name}
    verts = [M @ v.co for v in me.vertices]
    sk_faces = [tuple(p.vertices) for p in me.polygons if p.material_index in bot and all(v in S for v in p.vertices)]
    bvh = BVHTree.FromPolygons([tuple(v) for v in verts], sk_faces)
    sx = sum(verts[i].x for i in S) / len(S); sy = sum(verts[i].y for i in S) / len(S)
    zt = max(verts[i].z for i in S)
    kill = []
    for p in me.polygons:
        if p.material_index not in skin: continue
        c = sum((verts[v] for v in p.vertices), V()) / len(p.vertices)
        if not (hem1 + HIDE_DOWN_TO < c.z < zt - 0.15): continue
        d = V((c.x - sx, c.y - sy, 0))
        if d.length < 1e-6: continue
        hit = bvh.ray_cast(c, d.normalized(), 2.5)
        if hit[0] is not None: kill.append(p.index)
    bm = bmesh.new(); bm.from_mesh(me); bm.faces.ensure_lookup_table()
    bmesh.ops.delete(bm, geom=[bm.faces[i] for i in kill], context='FACES_ONLY')
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')
    bm.to_mesh(me); bm.free(); me.update()
    return len(kill)

def build_vivi():
    coll, arm, body, objs = load_vivi()
    rest(arm)
    S, t_of, c, (top, hem0, hem1) = lengthen_skirt(arm, body)
    reweight_skirt(body, S, t_of, c)
    removed = remove_hidden_body(body, set(S), hem1)
    return dict(coll=coll, arm=arm, body=body, objs=objs, skirt_verts=len(S), top=top, hem_before=hem0, hem_after=hem1, hidden_faces_removed=removed)

def save_vivi(info, name='Vivi_Student_Uniform.blend'):
    os.makedirs(OUT_DIR, exist_ok=True)
    path = os.path.join(OUT_DIR, name)
    bpy.data.libraries.write(path, set(info['objs']) | {info['coll']}, fake_user=True, compress=True)
    return path
