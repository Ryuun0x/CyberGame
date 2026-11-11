"""v3 PS1-style pixel textures (exec'd after v3_head.py).

bake_object() gives a mesh one low-resolution texture with nearest-pixel filtering:
  1. every face gets UVs from an orthographic projection along its dominant axis (world units),
  2. Blender packs the resulting islands into 0-1 without overlap (no rotation, so pixels stay axis-aligned),
  3. each triangle is rasterised in UV space; every texel's 3D point is classified by colorfn(p, n, mat),
     so diagonal boundaries come out as pixel stairs, like hand-painted retro textures.
"""
TEXEL = 0.0045          # metres per texel on clothes (about 4.5 mm)

def _project_uvs(ob):
    me = ob.data
    uvl = me.uv_layers.get('UVMap') or me.uv_layers.new(name='UVMap')
    for poly in me.polygons:
        n = poly.normal; ax = max(range(3), key=lambda i: abs(n[i])); s = 1 if n[ax] > 0 else -1
        for li in poly.loop_indices:
            co = me.vertices[me.loops[li].vertex_index].co
            uvl.data[li].uv = ((co.y * s, co.z) if ax == 0 else (-co.x * s, co.z) if ax == 1 else (co.x, co.y * s))
    area = sum(p.area for p in me.polygons)
    return uvl, area

def _pack(ob, margin=0.008):
    vl = bpy.context.view_layer
    for o in vl.objects: o.select_set(False)
    ob.select_set(True); vl.objects.active = ob
    ts = bpy.context.scene.tool_settings; sync = ts.use_uv_select_sync; ts.use_uv_select_sync = True
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.pack_islands(rotate=False, margin=margin)
    bpy.ops.object.mode_set(mode='OBJECT')
    ts.use_uv_select_sync = sync

def bake_object(ob, colorfn, name, texel=TEXEL, res=None):
    """Replace ob's materials with one pixel texture whose texels are coloured by colorfn(point, normal, material)."""
    me = ob.data
    _, area = _project_uvs(ob)
    _pack(ob)
    uvl = me.uv_layers.active
    res = res or int(min(1024, max(64, math.ceil(math.sqrt(area / 0.62) / texel / 8) * 8)))
    img = np.zeros((res, res, 3), np.float32); filled = np.zeros((res, res), bool)
    names = [m.name for m in me.materials]
    cache = {}
    def rgb(c):
        if isinstance(c, str):
            if c not in cache: cache[c] = _hex(c)
            return cache[c]
        return c
    me.calc_loop_triangles()
    for lt in me.loop_triangles:
        uv = [uvl.data[li].uv * res for li in lt.loops]
        P = [me.vertices[vi].co for vi in lt.vertices]
        n = me.polygons[lt.polygon_index].normal; mat = names[me.polygons[lt.polygon_index].material_index]
        x0, x1 = int(max(0, math.floor(min(u.x for u in uv)))), int(min(res - 1, math.ceil(max(u.x for u in uv))))
        y0, y1 = int(max(0, math.floor(min(u.y for u in uv)))), int(min(res - 1, math.ceil(max(u.y for u in uv))))
        (ax, ay), (bx, by), (cx, cy) = [(u.x, u.y) for u in uv]
        den = (by - cy) * (ax - cx) + (cx - bx) * (ay - cy)
        if abs(den) < 1e-12: continue
        # barycentric -> distance (in texels) from each edge, so the coverage tolerance is a fixed 0.75 px
        hs = [abs(den) / max(1e-9, math.hypot(bx - cx, by - cy)), abs(den) / max(1e-9, math.hypot(cx - ax, cy - ay)),
              abs(den) / max(1e-9, math.hypot(ax - bx, ay - by))]
        for ty in range(y0, y1 + 1):
            for tx in range(x0, x1 + 1):
                px, py = tx + 0.5, ty + 0.5
                w0 = ((by - cy) * (px - cx) + (cx - bx) * (py - cy)) / den
                w1 = ((cy - ay) * (px - cx) + (ax - cx) * (py - cy)) / den
                w2 = 1 - w0 - w1
                dmin = min(w0 * hs[0], w1 * hs[1], w2 * hs[2])
                if dmin < -0.75: continue                  # up to 0.75 px past the edge so island borders are covered
                if filled[ty, tx] and dmin < 0: continue
                p = P[0] * w0 + P[1] * w1 + P[2] * w2
                img[ty, tx] = rgb(colorfn(p, n, mat)); filled[ty, tx] = True
    for _ in range(3):            # bleed colours into the packing margins so filtering never shows black seams
        grow = ~filled
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            src = np.roll(np.roll(filled, dy, 0), dx, 1) & grow
            img[src] = np.roll(np.roll(img, dy, 0), dx, 1)[src]; filled |= src; grow &= ~src
    path = os.path.join(TEX, name + '.png')
    im = bpy.data.images.get(name + '.png')
    if im: bpy.data.images.remove(im)
    im = bpy.data.images.new(name + '.png', res, res)
    im.pixels.foreach_set(np.concatenate([img, np.ones((res, res, 1), np.float32)], 2).ravel())
    im.filepath_raw = path; im.file_format = 'PNG'; im.save()
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name); m.use_nodes = True
    nt = m.node_tree; bsdf = nt.nodes['Principled BSDF']; bsdf.inputs['Roughness'].default_value = 0.9
    tn = next((x for x in nt.nodes if x.type == 'TEX_IMAGE'), None) or nt.nodes.new('ShaderNodeTexImage')
    tn.image = im; tn.interpolation = 'Closest'; tn.extension = 'EXTEND'
    nt.links.new(tn.outputs['Color'], bsdf.inputs['Base Color'])
    me.materials.clear(); me.materials.append(m)
    for p in me.polygons: p.material_index = 0
    return res

def sampler(canvas, to_local):
    """nearest-texel lookup into a painted Canvas at a 3D point (to_local maps the point to canvas x, z)."""
    def f(p):
        x, z = to_local(p)
        j = int((x - canvas.x0) / canvas.size * canvas.res); i = int((z - canvas.z0) / canvas.size * canvas.res)
        if 0 <= i < canvas.res and 0 <= j < canvas.res: return canvas.img[i, j]
        return None
    return f

def hair_color(p, n, mat):
    """flat hair colour with the odd darker strand: tall thin cells, about one in eleven a shade darker"""
    h = (int(math.floor(p.x / 0.006)) * 73856093) ^ (int(math.floor(p.y / 0.006)) * 19349663) ^ (int(math.floor(p.z / 0.04)) * 83492791)
    base = _hex(PAL[mat][0])
    return base * 0.82 if h % 11 == 0 else base
