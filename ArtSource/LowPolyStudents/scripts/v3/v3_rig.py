"""v3 assembly: one rig, shared body + hair, two outfit collections, Idle/Walk/Run (exec'd after v3_outfits.py).

Animation: v3_anim.py (procedural clips, analytic leg IK, baked quaternion keys), adapted from ../lp_anim.py.
"""
NAME = 'Male'
INFO_J = [None]       # joints of the current build, for helpers called after assembly

def clear_character():
    root = bpy.data.collections.get(NAME + '_Character')
    if root:
        for c in [root] + list(root.children_recursive):
            for o in list(c.objects): bpy.data.objects.remove(o)
        for c in list(root.children_recursive): bpy.data.collections.remove(c)
        bpy.data.collections.remove(root)
    for d in (bpy.data.meshes, bpy.data.armatures, bpy.data.actions, bpy.data.materials, bpy.data.images):
        for x in list(d):
            if x.users == 0: d.remove(x)

def assemble(bmW, names, J):
    """Build every mesh, link it to the rig, organise collections. Returns info dict."""
    clear_character()
    sc = bpy.context.scene
    T = TEX
    paint_face(os.path.join(T, 'Face_Skin.png'))
    root = bpy.data.collections.new(NAME + '_Character'); sc.collection.children.link(root)
    cols = {}
    for k in ('Base', 'Outfit_Casual', 'Outfit_Student'):
        cols[k] = bpy.data.collections.new(k); root.children.link(cols[k])
    rig = make_armature(NAME + '_Rig', J, root); INFO_J[0] = J
    base_bvh = BVHTree.FromBMesh(bmW)
    head = build_head(base_bvh)
    for i, p in enumerate(head.v):        # lower neck shares the base weights with the garment necklines
        if p.z < 1.37: head.w[i] = base_weights(bmW, base_bvh, names, p)
    _, hands = build_arms(bmW, names, J)      # bare arms are painted on the outfit surface
    outs = build_outfits(bmW, names, J, head.bvh())
    ny = 0.033 + HEAD_Y * HEAD_H * 0.5     # the outfit surfaces cover the neck up to NECK_TOP: the head keeps only
    near = lambda p: V((p.x, p.y - ny, 0)).length < 0.12    # a short collar of neck under them, tucked a little in
    head = head.subset({i for i, f in enumerate(head.f) if not all(head.v[k].z < NECK_TOP - 0.004 and near(head.v[k]) for k in f)})
    for i, p in enumerate(head.v):
        if p.z < NECK_TOP - 0.002 and near(p):
            d = V((p.x, p.y - ny, 0)); head.v[i] = p - d.normalized() * min(0.004, d.length * 0.1)
    body = MB(); body.merge(head); body.merge(hands)
    parts = [('Body', body, 'Base', face_uv, None), ('Hair', build_hair(), 'Base', None, hair_color),
             ('Casual_Clothes', outs['Casual'][0], 'Outfit_Casual', None, outs['Casual'][1]),
             ('Student_Clothes', outs['Student'][0], 'Outfit_Student', None, outs['Student'][1])]
    info = {'rig': rig, 'J': J, 'tris': {}}
    for nm, mb, col, uvfn, colorfn in parts:
        ob = to_object(NAME + '_' + nm, mb, cols[col], uvfn=uvfn)
        if colorfn: info.setdefault('tex', {})[nm] = bake_object(ob, colorfn, 'Pixel_' + nm, texel=0.006 if nm == 'Hair' else TEXEL)
        if nm.endswith('Clothes'): smooth_neck(ob)
        ob.parent = rig
        md = ob.modifiers.new('Armature', 'ARMATURE'); md.object = rig
        info['tris'][ob.name] = tris(ob)
    return info

def smooth_neck(ob, ny=None):
    """the outfit surface's neck part is painted skin: shade it smooth like the head so the two blend"""
    ny = 0.033 + HEAD_Y * HEAD_H * 0.5 if ny is None else ny
    bm = bmesh.new(); bm.from_mesh(ob.data)
    arm_t = arm_param(INFO_J[0])
    def pick(f):
        c = f.calc_center_median(); t = arm_t(c)
        if t is not None and t > 0.25 and not (abs(c.x) < 0.17 and c.z > 0.9): return True    # arms: one smooth limb
        return c.z > 1.35 and V((c.x, c.y - ny, 0)).length < 0.1
    sel = {f for f in bm.faces if pick(f)}
    for f in sel: f.smooth = True
    for e in bm.edges:
        if all(f in sel for f in e.link_faces) and len(e.link_faces) == 2: e.smooth = True
    bm.to_mesh(ob.data); bm.free()

def use_outfit(outfit):
    """Show one outfit ('Casual' or 'Student'); the other collection is hidden in viewport and render."""
    for k in ('Casual', 'Student'):
        lc = bpy.context.view_layer.layer_collection.children[NAME + '_Character'].children['Outfit_' + k]
        lc.exclude = False
        c = bpy.data.collections['Outfit_' + k]; c.hide_render = c.hide_viewport = k != outfit

def animate(info):
    A = dict(globals())                    # own namespace: v3_anim also defines play()
    p = os.path.join(SCRIPTS, 'v3_anim.py')
    exec(compile(open(p, encoding='utf8').read(), 'v3_anim.py', 'exec'), A)
    bake_clip, CLIPS = A['bake_clip'], A['CLIPS']; globals()['CLIPS'] = CLIPS
    rig, J = info['rig'], info['J']
    S, E = J['upper_arm.L']
    P = {'sex': 'M', 'arm_angle': math.degrees((E - S).angle(V((0, 0, -1))))}
    rep = {}; acts = []
    for clip in ('Idle', 'Walk', 'Run'):
        act, miss = bake_clip(rig, J, P, clip)
        old = bpy.data.actions.get(clip)
        if old and old != act: bpy.data.actions.remove(old)
        act.name = clip; acts.append(act)
        rep[clip] = dict(frames=CLIPS[clip]['T'], seconds=round(CLIPS[clip]['T'] / 30, 3), ik_miss_m=round(miss, 4))
    ad = rig.animation_data
    for tr in list(ad.nla_tracks): ad.nla_tracks.remove(tr)
    for act in acts:      # one muted NLA track per clip keeps every action referenced and easy to audition
        tr = ad.nla_tracks.new(); tr.name = act.name
        st = tr.strips.new(act.name, 0, act)
        tr.mute = True
    play(rig, 'Idle')
    return rep

def play(rig, clip):
    ad = rig.animation_data; act = bpy.data.actions[clip]
    ad.action = act
    try: ad.action_slot = act.slots[0]
    except Exception: pass
    bpy.context.scene.frame_start = 0; bpy.context.scene.frame_end = CLIPS[clip]['T'] - 1
