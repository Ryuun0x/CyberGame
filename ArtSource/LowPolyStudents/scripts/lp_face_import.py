"""Transplant a face (head skin mesh + its painted texture) from another model onto our character.

Steps: import the FBX, take the connected skin component around the head, estimate the head's own
frame (mirror-symmetry plane, nose-forward, chin-to-crown up), un-pose it into our axes, scale it to
our head, decimate to a low-poly budget, and hand back an MB with UVs for the face texture.
"""
import os
from mathutils import Matrix
from mathutils.kdtree import KDTree
FACE_FBX = os.environ.get('LP_FACE_FBX') or r"C:\Users\pajar\Downloads\po-uta\source\Po-uta.fbx"
FACE_TEX = os.environ.get('LP_FACE_TEX') or r"C:\Users\pajar\Downloads\po-uta\textures\porter_Base_color.png"
FACE_SRC = dict(obj='porter.001', skin_mat='porter', hair_mat='hair')
_FACE = {}

def _import_face_source():
    before = set(bpy.data.objects.keys())
    bpy.ops.import_scene.fbx(filepath=FACE_FBX)
    new = [o for o in bpy.data.objects if o.name not in before]
    def has(o, key): return o.type == 'MESH' and any(m and m.name.split('.')[0] == key for m in o.data.materials)
    src = max((o for o in new if has(o, FACE_SRC['skin_mat']) and has(o, FACE_SRC['hair_mat'])), key=lambda o: len(o.data.polygons))
    me = src.data
    skin = [i for i, m in enumerate(me.materials) if m and m.name.split('.')[0] == FACE_SRC['skin_mat']]
    hair = [i for i, m in enumerate(me.materials) if m and m.name.split('.')[0] == FACE_SRC['hair_mat']]
    bm = bmesh.new(); bm.from_mesh(me); bm.transform(src.matrix_world)
    meshes = {o.data for o in new if o.type == 'MESH'}; arms = {o.data for o in new if o.type == 'ARMATURE'}
    mats = {m for o in new if o.type == 'MESH' for m in o.data.materials if m}
    for o in new: bpy.data.objects.remove(o)
    for d, coll in ((meshes, bpy.data.meshes), (arms, bpy.data.armatures)):
        for x in d:
            if x.users == 0: coll.remove(x)
    for m in mats:
        if m.users == 0: bpy.data.materials.remove(m)
    return bm, skin, hair

def _components(faces):
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

def _fcent(comp):
    pts = [v.co for f in comp for v in f.verts]; return sum(pts, V()) / len(pts)

FACE_PITCH = 25.0  # nod correction for the source pose (degrees), tuned by eye

def _head_parts(bm, skin, hair):
    hp = [v.co.copy() for f in bm.faces if f.material_index in hair for v in f.verts]
    hc = sum(hp, V()) / len(hp)
    comps = _components([f for f in bm.faces if f.material_index in skin])
    main = min((c for c in comps if len(c) > 500), key=lambda c: (_fcent(c) - hc).length)
    small = [c for c in comps if len(c) < 60 and (_fcent(c) - hc).length < 2.7]
    # two eye clusters: split small parts by the farthest pair of centroids
    cs = [_fcent(c) for c in small]
    a, b = max(((i, j) for i in range(len(cs)) for j in range(i + 1, len(cs))), key=lambda ij: (cs[ij[0]] - cs[ij[1]]).length)
    A = [c for c, p in zip(small, cs) if (p - cs[a]).length < (p - cs[b]).length]
    B = [c for c, p in zip(small, cs) if (p - cs[a]).length >= (p - cs[b]).length]
    ea = sum((_fcent(c) * len(c) for c in A), V()) / sum(len(c) for c in A)
    eb = sum((_fcent(c) * len(c) for c in B), V()) / sum(len(c) for c in B)
    mc = (ea + eb) / 2
    neck = max((v.co for f in main for v in f.verts), key=lambda p: (p - mc).length)
    return main, small, hc, ea, eb, neck

def face_frame(hc, ea, eb, neck):
    """R maps source vectors into our axes (X = his left, -Y forward, Z up); eye midpoint is the origin."""
    mid = (ea + eb) / 2
    side = (eb - ea).normalized()
    f0 = mid - hc; fw = (f0 - side * f0.dot(side)).normalized()
    up = fw.cross(side)
    if up.dot(neck - mid) > 0: side = -side; up = fw.cross(side)     # the neck end of the skin is down
    # the hair centroid sits above/behind the eyes, so fw leans down: lift it so the gaze is level
    # optional nod correction: positive FACE_PITCH lifts the gaze (rotating about `side` by -angle raises fw toward up)
    q = Quaternion(side, -math.radians(FACE_PITCH)); fw = (q @ fw).normalized(); up = fw.cross(side).normalized()
    return mid, Matrix((side, -fw, up))

def load_face(target_c, target_h=0.242):
    """Head skin + eye cards of the source model in our space, eye line at target_c (eye level)."""
    if 'bm' in _FACE: return _FACE['bm']
    bm, skin, hair = _import_face_source()
    main, small, hc, ea, eb, neck = _head_parts(bm, skin, hair)
    mid, R = face_frame(hc, ea, eb, neck)
    comp = main + [f for c in small for f in c]
    vs = list({v for f in comp for v in f.verts})
    loc = {v: R @ (v.co - mid) for v in vs}
    mv = {v: loc[v] for v in {v for f in main for v in f.verts}}
    zs = [p.z for p in mv.values()]
    front = [p for p in mv.values() if p.y < 0 and abs(p.x) < 0.2 * (max(zs) - min(zs))]
    chin = min(p.z for p in front if p.z < 0)
    k = target_h / (max(zs) - chin)
    out = bmesh.new(); vm = {}; mset = set(main)
    uvl = bm.loops.layers.uv.active; ouv = out.loops.layers.uv.new('UVMap')
    for v in vs:
        vm[v] = out.verts.new(loc[v] * k + target_c)
    for f in comp:
        try: nf = out.faces.new([vm[v] for v in f.verts])
        except ValueError: continue
        nf.material_index = 0 if f in mset else 1
        for lo, nlo in zip(f.loops, nf.loops): nlo[ouv].uv = lo[uvl].uv if uvl else (0, 0)
    bm.free(); out.normal_update()
    _FACE.update(bm=out, k=k, eye_dist=(ea - eb).length * k, chin=chin * k)
    return out

FACE_PITCH = 25.0

def close_and_trim(bm, z_cut):
    """Cut the transplanted neck at z_cut (drop everything below) and cap every other opening (back of skull)."""
    geom = bm.verts[:] + bm.edges[:] + bm.faces[:]
    bmesh.ops.bisect_plane(bm, geom=geom, plane_co=(0, 0, z_cut), plane_no=(0, 0, 1), clear_inner=True)
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')
    bnd = [e for e in bm.edges if len(e.link_faces) == 1]
    loops = []; left = set(bnd)
    while left:
        e = left.pop(); loop = {e}; st = [e]
        while st:
            x = st.pop()
            for v in x.verts:
                for g in v.link_edges:
                    if g in left: left.remove(g); loop.add(g); st.append(g)
        loops.append(loop)
    for loop in loops:
        zmin = min(v.co.z for e in loop for v in e.verts)
        if zmin < z_cut + 0.004: continue                   # the neck opening stays open (it sits inside the collar)
        if len(loop) < 3: continue
        try: bmesh.ops.holes_fill(bm, edges=list(loop), sides=0)
        except Exception: pass
    bm.normal_update()

def tinted_face_texture(path_out, skin_hex, amount=0.5):
    """Warm the source's pale painted skin toward our skin tone; keep near-white (eye whites, glints) untouched.
    Returns (image, average tinted skin colour as sRGB hex)."""
    import numpy as np
    src = bpy.data.images.load(FACE_TEX, check_existing=True)
    w, h = src.size
    a = np.array(src.pixels[:], np.float32).reshape(h, w, 4)
    rgb = a[..., :3]
    bright = rgb.mean(axis=2)
    sat = rgb.max(axis=2) - rgb.min(axis=2)
    skinish = (bright > 0.55) & (bright < 0.985) & (sat < 0.25)
    target = np.array([int(skin_hex[i:i + 2], 16) / 255 for i in (1, 3, 5)], np.float32)
    avg = rgb[skinish].mean(axis=0) if skinish.any() else np.ones(3, np.float32)
    factor = 1 + (target / np.maximum(avg, 1e-3) - 1) * amount
    m = skinish[..., None].astype(np.float32)
    out = rgb * (1 - m) + np.clip(rgb * factor, 0, 1) * m
    a[..., :3] = out
    im = bpy.data.images.get('Face_Skin') or bpy.data.images.new('Face_Skin', w, h, alpha=True)
    if tuple(im.size) != (w, h): im.scale(w, h)
    im.pixels.foreach_set(a.ravel())
    if w > 1024: im.scale(1024, 1024)          # game-sized face texture
    im.filepath_raw = path_out; im.file_format = 'PNG'; im.save(); im.pack()
    newavg = np.clip(avg * factor, 0, 1)
    return im, '#%02X%02X%02X' % tuple(int(round(c * 255)) for c in newavg)

def repaint_scalp(img, fb, C, hair_hex):
    """Paint the source's blond/green hair strokes on scalp and nape regions with our hair colour.
    Mask = UV area of faces above the brow line or behind the ears; inside it only non-skin pixels change."""
    import numpy as np
    w, h = img.size
    mask = np.zeros((h, w), bool)
    uvl = fb.loops.layers.uv.active
    for f in fb.faces:
        c = f.calc_center_median()
        skull = c.z > C.z - 0.05                                  # never the neck
        if not skull or not (c.z > C.z + 0.045 or c.y > C.y + 0.03 or (abs(c.x) > 0.05 and c.z > C.z - 0.015 and c.y > C.y - 0.075)): continue
        uv = [lo[uvl].uv for lo in f.loops]
        for i in range(1, len(uv) - 1):
            tri = np.array([(uv[0].x * w, uv[0].y * h), (uv[i].x * w, uv[i].y * h), (uv[i + 1].x * w, uv[i + 1].y * h)])
            x0, y0 = np.floor(tri.min(0)).astype(int); x1, y1 = np.ceil(tri.max(0)).astype(int)
            x0, y0 = max(x0, 0), max(y0, 0); x1, y1 = min(x1, w - 1), min(y1, h - 1)
            if x1 < x0 or y1 < y0: continue
            X, Y = np.meshgrid(np.arange(x0, x1 + 1) + 0.5, np.arange(y0, y1 + 1) + 0.5)
            (ax, ay), (bx, by), (cx, cy) = tri
            d = (by - cy) * (ax - cx) + (cx - bx) * (ay - cy)
            if abs(d) < 1e-9: continue
            l1 = ((by - cy) * (X - cx) + (cx - bx) * (Y - cy)) / d
            l2 = ((cy - ay) * (X - cx) + (ax - cx) * (Y - cy)) / d
            inside = (l1 >= -0.02) & (l2 >= -0.02) & (1 - l1 - l2 >= -0.02)
            mask[y0:y1 + 1, x0:x1 + 1] |= inside
    a = np.array(img.pixels[:], np.float32).reshape(h, w, 4)
    rgb = a[..., :3]; mxc, mnc = rgb.max(2), rgb.min(2); sat = (mxc - mnc) / np.maximum(mxc, 1e-3)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    # inside the scalp mask everything that is not skin-coloured is old hair paint (blond, green, speckle)
    skin_ref = np.median(rgb[mask & (sat < 0.25) & (mxc > 0.6)], axis=0) if (mask & (sat < 0.25) & (mxc > 0.6)).any() else np.array([0.9, 0.8, 0.7], np.float32)
    hairpaint = (g > b * 1.12) & (sat > 0.16) & (np.linalg.norm(rgb - skin_ref, axis=2) > 0.06)   # blond / green strokes only
    sel = mask & hairpaint
    hair = np.array([int(hair_hex[i:i + 2], 16) / 255 for i in (1, 3, 5)], np.float32)
    shade = (mxc / max(float(np.percentile(mxc[sel], 90)) if sel.any() else 1.0, 1e-3)).clip(0.55, 1.15)[..., None]
    rgb[sel] = (hair * shade[sel]).clip(0, 1)
    a[..., :3] = rgb
    img.pixels.foreach_set(a.ravel()); img.save(); img.pack()
    return int(sel.sum())
