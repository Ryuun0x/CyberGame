"""v3 male student: shared body, head, hair, hands and both outfits.

Run through run_v3.py (exec'd into one namespace). Coordinates: metres, +Z up, character faces -Y.
"""
import bpy, bmesh, math, os, random
import numpy as np
from mathutils import Vector as V, Matrix, Quaternion
from mathutils.bvhtree import BVHTree

ROOT = os.environ.get('LP_V3') or r"C:\Users\pajar\Documents\GitHub\CyberGame\ArtSource\LowPolyStudents\v3"
TEX = os.path.join(ROOT, 'textures')
SCRIPTS = os.environ.get('LP_V3_SCRIPTS') or os.path.join(os.path.dirname(ROOT), 'scripts', 'v3')
BASE_FBX = os.environ.get('LP_BASE_FBX') or r"C:\Users\pajar\Downloads\low-poly-male-base-mesh\source\lowpoly_male_base_mesh-TPOSE.fbx"
BODY_K = 1.62 / 1.75        # base body is measured at 1.75 m (v2 numbers), then shrunk to a teen
HEAD_H = 0.285              # chin -> crown: big retro-anime/PS1 head (~5.9 heads tall, Neto-like)
HK = HEAD_H / 0.272         # face paint, ears and hair sizes are authored at 0.272 and scale with the head
CHIN_Z = 1.392              # a touch lower: shorter visible neck

def lerp(a, b, t): return a + (b - a) * t
def ss(a, b, x):
    t = max(0.0, min(1.0, (x - a) / (b - a))); return t * t * (3 - 2 * t)
def mx(v): return V((-v.x, v.y, v.z))

# ------------------------------------------------------------------ palette / materials
PAL = {   # name: (hex, roughness)
    'Skin': ('#F2B48C', 0.75), 'Face': ('#F2B48C', 0.75), 'Hair': ('#6C3324', 0.85), 'Hair_Dark': ('#55271B', 0.85),
    'Hair_Light': ('#7E3E2B', 0.85), 'Ear_Inner': ('#DE9C78', 0.8),
    'Tee': ('#4A4A3D', 0.95), 'Tee_Print': ('#4A4A3D', 0.95), 'Rust': ('#9A4434', 0.95), 'Denim': ('#3B4766', 0.9),
    'Denim_Dark': ('#2F3953', 0.9), 'Boot': ('#5C3B2A', 0.7), 'Boot_Sole': ('#2E2620', 0.8), 'Lace': ('#3A2A20', 0.8),
    'Shirt': ('#F2F2EE', 0.85), 'Vest': ('#A9A9A6', 0.95), 'Vest_Print': ('#A9A9A6', 0.95), 'Trim': ('#2C3557', 0.9),
    'Trim_Light': ('#CFCFCB', 0.9), 'Vest_Band': ('#A0A09D', 0.95),
    'Shirt_Front': ('#F2F2EE', 0.85), 'Cloth': ('#808080', 0.9),
    'Tie': ('#262E4D', 0.8), 'Trousers': ('#2E3653', 0.9), 'Shoe': ('#1E1E21', 0.45), 'Shoe_Sole': ('#111113', 0.8),
}
TEXMAT = {'Face': 'Face_Skin.png'}
SMOOTH = {'Skin', 'Face', 'Ear_Inner'}

def srgb(h):
    c = [int(h[i:i + 2], 16) / 255 for i in (1, 3, 5)]
    return [x / 12.92 if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4 for x in c] + [1.0]

def get_mat(name):
    """Create or refresh a matte material (palette colour, optional image texture)."""
    m = bpy.data.materials.get(name)
    if not m: m = bpy.data.materials.new(name); m.use_nodes = True
    nt = m.node_tree; bsdf = nt.nodes['Principled BSDF']
    hx, rough = PAL[name]
    bsdf.inputs['Base Color'].default_value = srgb(hx); bsdf.inputs['Roughness'].default_value = rough
    m.diffuse_color = srgb(hx)
    if name in TEXMAT:
        img = bpy.data.images.get(TEXMAT[name]) or bpy.data.images.load(os.path.join(TEX, TEXMAT[name]))
        tn = next((n for n in nt.nodes if n.type == 'TEX_IMAGE'), None) or nt.nodes.new('ShaderNodeTexImage')
        tn.image = img; tn.interpolation = 'Closest'; tn.extension = 'EXTEND'      # pixel look
        nt.links.new(tn.outputs['Color'], bsdf.inputs['Base Color'])
    return m

# ------------------------------------------------------------------ mesh builder
class MB:
    """Minimal mesh accumulator: verts, faces with material names, optional per-face UVs, per-vertex weights."""
    def __init__(s): s.v, s.f, s.fm, s.w, s.uv = [], [], [], [], {}
    def add(s, p, w=None): s.v.append(V(p)); s.w.append(w or {}); return len(s.v) - 1
    def face(s, idx, mat, uv=None):
        s.f.append(list(idx)); s.fm.append(mat)
        if uv: s.uv[len(s.f) - 1] = uv
    def merge(s, o):
        n = len(s.v); s.v += [p.copy() for p in o.v]; s.w += [dict(w) for w in o.w]
        for i, (f, m) in enumerate(zip(o.f, o.fm)):
            s.f.append([x + n for x in f]); s.fm.append(m)
            if i in o.uv: s.uv[len(s.f) - 1] = o.uv[i]
    def bvh(s): return BVHTree.FromPolygons([tuple(p) for p in s.v], s.f)
    def subset(s, keep):
        """new MB with only the faces whose index is in keep (vertices remapped)"""
        o = MB(); vm = {}
        for i, (f, m) in enumerate(zip(s.f, s.fm)):
            if i not in keep: continue
            for x in f:
                if x not in vm: vm[x] = o.add(s.v[x], dict(s.w[x]))
            o.face([vm[x] for x in f], m)
        return o

def to_object(name, mb, coll, uvfn=None, sharp=70):
    """MB -> Blender mesh object. uvfn(mat, co, normal) -> (u,v) assigns UVs for textured materials."""
    me = bpy.data.meshes.new(name)
    me.from_pydata([tuple(p) for p in mb.v], [], mb.f)
    mats = sorted(set(mb.fm), key=mb.fm.index)
    for m in mats: me.materials.append(get_mat(m))
    me.polygons.foreach_set('material_index', [mats.index(m) for m in mb.fm])
    me.update()
    ob = bpy.data.objects.new(name, me); coll.objects.link(ob)
    groups = {}
    for vi, w in enumerate(mb.w):
        for b, x in w.items():
            if x <= 0: continue
            g = groups.get(b) or ob.vertex_groups.new(name=b); groups[b] = g
            g.add([vi], x, 'REPLACE')
    bm = bmesh.new(); bm.from_mesh(me)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    bmesh.ops.recalc_face_normals(bm, faces=[f for f in bm.faces])
    for isl in islands(bm):   # recalc can pick inward for open shells (garments): orient each island outward
        c = sum((f.calc_center_median() for f in isl), V()) / len(isl)
        if sum((f.calc_center_median() - c).dot(f.normal) * f.calc_area() for f in isl) < 0:
            bmesh.ops.reverse_faces(bm, faces=isl)
    uvl = bm.loops.layers.uv.new('UVMap')
    names = [m.name for m in me.materials]
    for f in bm.faces:
        mn = names[f.material_index]
        for lo in f.loops:
            fi = f.index
            lo[uvl].uv = uvfn(mn, lo.vert.co, f.normal) if (uvfn and mn in TEXMAT) else (0.5, 0.5)
    for f in bm.faces: f.smooth = names[f.material_index] in SMOOTH
    for e in bm.edges:
        lf = e.link_faces
        e.smooth = len(lf) == 2 and all(f.smooth for f in lf) and e.calc_face_angle(math.pi) < math.radians(sharp)
    bm.to_mesh(me); bm.free()
    return ob

def islands(bm):
    seen, out = set(), []
    for f in bm.faces:
        if f in seen: continue
        st, comp = [f], []
        seen.add(f)
        while st:
            x = st.pop(); comp.append(x)
            for e in x.edges:
                for g in e.link_faces:
                    if g not in seen: seen.add(g); st.append(g)
        out.append(comp)
    return out

def tris(ob):return sum(len(p.vertices) - 2 for p in ob.data.polygons)

# ------------------------------------------------------------------ base body (hidden; source of garments + weights)
def load_base():
    """Base FBX as bmesh at 1.75 m with the stance narrowed (v2 measurements stay valid at this scale)."""
    before = set(bpy.data.objects.keys())
    bpy.ops.import_scene.fbx(filepath=BASE_FBX)
    new = [o for o in bpy.data.objects if o.name not in before]
    mo = [o for o in new if o.type == 'MESH'][0]
    bm = bmesh.new(); bm.from_mesh(mo.data); bm.transform(mo.matrix_world)
    for o in new:
        d = o.data; bpy.data.objects.remove(o)
        if d and d.users == 0: bpy.data.meshes.remove(d)
    bm.transform(Matrix.Scale(1.75 / max(v.co.z for v in bm.verts), 4))
    for v in bm.verts:   # ankles from ~26 cm to ~17 cm off centre, hips unchanged
        z = v.co.z
        if z < 0.885 and abs(v.co.x) > 1e-6:
            v.co.x -= math.copysign(0.09 * min(1.0, (0.885 - z) / (0.885 - 0.124)), v.co.x)
    bm.normal_update()
    return bm

def sections(bm, z, no=V((0, 0, 1))):
    """Cross-section islands of bm at the plane through (0,0,z) -> list of point lists."""
    b = bm.copy()
    res = bmesh.ops.bisect_plane(b, geom=b.verts[:] + b.edges[:] + b.faces[:], plane_co=(0, 0, z), plane_no=no)
    cut = [g for g in res['geom_cut'] if isinstance(g, bmesh.types.BMEdge)]
    adj = {}
    for e in cut:
        a, c = e.verts; adj.setdefault(a, []).append(c); adj.setdefault(c, []).append(a)
    seen, out = set(), []
    for v in adj:
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

def landmarks(bm):
    """Joint positions measured from cross-sections of the 1.75 m base (same method as v2)."""
    def islands(z): return [(_cent(p), p) for p in sections(bm, z)]
    def leg(z): return min((c for c, p in islands(z) if c.x > 0.02), key=lambda c: c.x)
    def torso_y(z): return max(islands(z), key=lambda cp: len(cp[1]) if abs(cp[0].x) < 0.05 else -1)[0].y
    def arm(z): return max((c for c, p in islands(z) if c.x > 0.2), key=lambda c: c.x)
    A = leg(0.10); K = leg(0.465); c70, c78 = leg(0.70), leg(0.78)
    H = c78 + (c78 - c70) * ((0.885 - 0.78) / (c78.z - c70.z))
    foot = [v.co for v in bm.verts if v.co.z < 0.06 and v.co.x > 0]
    toe = min(foot, key=lambda p: p.y)
    ball = V((A.x, toe.y + 0.07, 0.022)); toe_t = V((A.x, toe.y + 0.012, 0.02))
    ups = [arm(z) for z in (1.12, 1.15, 1.18, 1.21, 1.24)]
    zs = [p.z for p in ups]; mz = sum(zs) / len(zs)
    def fit(attr):
        vals = [getattr(p, attr) for p in ups]; mv = sum(vals) / len(vals)
        sl = sum((z - mz) * (v - mv) for z, v in zip(zs, vals)) / sum((z - mz) ** 2 for z in zs)
        return lambda z: mv + sl * (z - mz)
    fx, fy = fit('x'), fit('y')
    S = V((fx(1.36), fy(1.36), 1.36))
    def width(z):
        c, p = max(((c, p) for c, p in islands(z) if c.x > 0.2), key=lambda cp: cp[0].x)
        return max(q.x for q in p) - min(q.x for q in p)
    wz = min((0.93 + 0.005 * i for i in range(14)), key=width)
    W = arm(wz)
    E = arm(S.z + 0.53 * (W.z - S.z))
    yp, ys, yc = torso_y(0.96), torso_y(1.08), torso_y(1.27)
    J = {'root': (V((0, 0, 0)), V((0, 0.25, 0))),
         'pelvis': (V((0, yp, 0.92)), V((0, yp, 1.0))), 'spine': (V((0, yp, 1.0)), V((0, ys, 1.16))),
         'chest': (V((0, ys, 1.16)), V((0, yc, 1.38))), 'neck': (V((0, yc, 1.43)), V((0, yc + 0.01, 1.53)))}
    L = {'clavicle': (V((0.025, S.y, S.z + 0.008)), S), 'upper_arm': (S, E), 'forearm': (E, W),
         'thigh': (H, K), 'shin': (K, A), 'foot': (A, ball), 'toe': (ball, toe_t)}
    for k_, (h, t) in L.items():
        J[k_ + '.L'] = (h.copy(), t.copy()); J[k_ + '.R'] = (mx(h), mx(t))
    return J

def scale_J(J, k):
    return {b: (h * k, t * k) for b, (h, t) in J.items()}
