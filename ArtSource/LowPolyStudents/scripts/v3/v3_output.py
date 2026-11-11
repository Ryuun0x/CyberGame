"""v3 stage, preview renders, contact sheets, GLB export + reimport check (exec'd after v3_model.py)."""

def setup_stage():
    sc = bpy.context.scene
    sc.render.engine = 'BLENDER_EEVEE'
    sc.view_settings.view_transform = 'Standard'; sc.view_settings.look = 'None'
    sc.render.film_transparent = False
    sc.render.fps = 30
    w = sc.world or bpy.data.worlds.new('World'); sc.world = w; w.use_nodes = True
    bg = w.node_tree.nodes['Background']; bg.inputs['Color'].default_value = srgb('#D7DADE'); bg.inputs['Strength'].default_value = 0.9
    for nm, rot, en, sz in (('Key', (50, 0, -35), 3.2, 4), ('Fill', (65, 0, 60), 1.1, 8), ('Rim', (55, 0, 165), 2.2, 3)):
        L = bpy.data.objects.get('Light_' + nm)
        if not L:
            L = bpy.data.objects.new('Light_' + nm, bpy.data.lights.new('Light_' + nm, 'SUN')); sc.collection.objects.link(L)
        L.data.energy = en; L.data.angle = math.radians(sz)
        L.rotation_euler = [math.radians(a) for a in rot]
    cam = bpy.data.objects.get('PreviewCam')
    if not cam:
        cam = bpy.data.objects.new('PreviewCam', bpy.data.cameras.new('PreviewCam')); sc.collection.objects.link(cam)
    sc.camera = cam
    fl = bpy.data.objects.get('Stage_Floor')
    if not fl:
        me = bpy.data.meshes.new('Stage_Floor'); me.from_pydata([(-3, -3, 0), (3, -3, 0), (3, 3, 0), (-3, 3, 0)], [], [(0, 1, 2, 3)])
        fl = bpy.data.objects.new('Stage_Floor', me); sc.collection.objects.link(fl)
        m = bpy.data.materials.new('Stage_Floor'); m.use_nodes = True
        m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = srgb('#CDD0D4')
        m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value = 1.0
        me.materials.append(m)
    return cam

def shoot(path, target, azim, elev=0.0, ortho=2.0, res=(800, 1000), persp=None):
    """Render one view. azim 0 = front (camera on -Y), 90 = character's left side (camera on +X)."""
    sc = bpy.context.scene; cam = sc.camera
    a, e = math.radians(azim), math.radians(elev)
    d = V((math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)))
    if persp:
        cam.data.type = 'PERSP'; cam.data.lens = persp[0]; cam.location = V(target) + d * persp[1]
    else:
        cam.data.type = 'ORTHO'; cam.data.ortho_scale = ortho; cam.location = V(target) + d * 10
    cam.data.clip_end = 100
    cam.rotation_euler = (-d).to_track_quat('-Z', 'Y').to_euler()
    sc.render.resolution_x, sc.render.resolution_y = res; sc.render.resolution_percentage = 100
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True)
    return path

def _load_px(path):
    im = bpy.data.images.load(path, check_existing=False)
    a = np.array(im.pixels[:], np.float32).reshape(im.size[1], im.size[0], 4)
    bpy.data.images.remove(im)
    return a

def sheet(paths, out, cols=None):
    """Stitch same-size renders into one image (row-major, top row first)."""
    ims = [_load_px(p) for p in paths]
    cols = cols or len(ims); rows = (len(ims) + cols - 1) // cols
    h, w = ims[0].shape[:2]
    S = np.ones((h * rows, w * cols, 4), np.float32)
    for i, a in enumerate(ims):
        r, c = divmod(i, cols)
        S[h * (rows - 1 - r):h * (rows - r), w * c:w * (c + 1)] = a
    im = bpy.data.images.new(os.path.basename(out), w * cols, h * rows)
    im.pixels.foreach_set(S.ravel()); im.filepath_raw = out; im.file_format = 'PNG'; im.save()
    bpy.data.images.remove(im)
    for p in paths:
        if os.path.basename(p).startswith('_'): os.remove(p)
    return out

def views(out, target, angles, ortho, res=(700, 900), elev=0.0, persp=None):
    d = os.path.dirname(out)
    ps = [shoot(os.path.join(d, f'_v{i}.png'), target, a, elev, ortho, res, persp) for i, a in enumerate(angles)]
    return sheet(ps, out)

def pose_sheet(info, out, shots, target=(0, 0, 0.9), ortho=2.1, res=(520, 760), angles=(30, 90), cols=None):
    """shots: [(outfit, clip, frame)] -> one row per shot angle, columns = shots."""
    rig = info['rig']; d = os.path.dirname(out); ps = []
    for ai, a in enumerate(angles):
        for si, (outfit, clip, f) in enumerate(shots):
            use_outfit(outfit); play(rig, clip); bpy.context.scene.frame_set(f)
            ps.append(shoot(os.path.join(d, f'_p{ai}_{si}.png'), target, a, 0, ortho, res))
    return sheet(ps, out, cols=cols or len(shots))

REN = os.path.join(ROOT, 'renders')

def final_stills(info):
    """Rest-pose turnaround (front, left, right, back, 3/4) and face close-ups per outfit."""
    rig = info['rig']; rig.data.pose_position = 'REST'; out = []
    os.makedirs(REN, exist_ok=True)
    for outfit in ('Casual', 'Student'):
        use_outfit(outfit)
        out.append(views(os.path.join(REN, f'{outfit}_turnaround.png'), (0, 0, 0.87), [0, 90, -90, 180, 35], 1.95, (620, 1000)))
        out.append(views(os.path.join(REN, f'{outfit}_face.png'), (0, 0.0, 1.55), [0, 90, -90, 35, 180], 0.46, (560, 640)))
    rig.data.pose_position = 'POSE'
    return out

def _pair(info, offsets):
    """Temporary copies of the character, one per outfit, sharing the rig's armature data and actions."""
    col = bpy.data.collections.new('_Preview'); bpy.context.scene.collection.children.link(col)
    rig = info['rig']; out = []
    for outfit, off in zip(('Casual', 'Student'), offsets):
        r2 = rig.copy(); r2.animation_data_clear(); r2.location = off; col.objects.link(r2)
        for c in ('Base', 'Outfit_' + outfit):
            for o in bpy.data.collections[c].objects:
                o2 = o.copy(); o2.parent = r2; o2.modifiers['Armature'].object = r2; col.objects.link(o2)
        out.append(r2)
    bpy.data.collections[NAME + '_Character'].hide_render = True
    return col, out

def _unpair(col):
    for o in list(col.objects): bpy.data.objects.remove(o)
    bpy.data.collections.remove(col); bpy.data.collections[NAME + '_Character'].hide_render = False

def render_clip(info, clip, path, loops=2, side=False, res=(960, 640)):
    sc = bpy.context.scene; T = CLIPS[clip]['T']
    col, rigs = _pair(info, [V((0, -0.6, 0)), V((0, 0.6, 0))] if side else [V((-0.55, 0, 0)), V((0.55, 0, 0))])
    act = bpy.data.actions[clip]
    mods = []
    for fc in iter_fcurves(act):
        mods.append((fc, fc.modifiers.new('CYCLES')))
    for r in rigs:
        r.animation_data_create(); r.animation_data.action = act
        try: r.animation_data.action_slot = act.slots[0]
        except Exception: pass
    if side: shoot_setup((0, 0, 0.88), 90, 0, ortho=2.9)
    else: shoot_setup((0, 0, 0.85), 30, 8, persp=(50, 5.4))
    sc.render.resolution_x, sc.render.resolution_y = res
    sc.frame_start, sc.frame_end = 0, T * loops - 1
    ims = sc.render.image_settings
    try: ims.media_type = 'VIDEO'
    except Exception: pass
    ims.file_format = 'FFMPEG'; sc.render.ffmpeg.format = 'MPEG4'; sc.render.ffmpeg.codec = 'H264'
    sc.render.ffmpeg.constant_rate_factor = 'HIGH'
    sc.render.filepath = path
    bpy.ops.render.render(animation=True)
    try: ims.media_type = 'IMAGE'
    except Exception: pass
    ims.file_format = 'PNG'
    for fc, m in mods: fc.modifiers.remove(m)
    _unpair(col)
    return path

def iter_fcurves(act):
    if hasattr(act, 'layers'):
        for ly in act.layers:
            for st in ly.strips:
                for cb in st.channelbags: yield from cb.fcurves
    else: yield from act.fcurves

def shoot_setup(target, azim, elev, ortho=None, persp=None):
    cam = bpy.context.scene.camera
    a, e = math.radians(azim), math.radians(elev)
    d = V((math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)))
    if persp: cam.data.type = 'PERSP'; cam.data.lens = persp[0]; cam.location = V(target) + d * persp[1]
    else: cam.data.type = 'ORTHO'; cam.data.ortho_scale = ortho; cam.location = V(target) + d * 10
    cam.rotation_euler = (-d).to_track_quat('-Z', 'Y').to_euler()

def filmstrip(info, clip, out, n=6):
    T = CLIPS[clip]['T']; fr = [round(i * T / n) for i in range(n)]
    shots = [(o, clip, f) for o in ('Casual', 'Student') for f in fr]
    return pose_sheet(info, out, shots, res=(360, 560), angles=(30,), cols=n)

# ------------------------------------------------------------------ export + verification
def export_glb(info, outfit, path):
    use_outfit(outfit)
    rig = info['rig']; vl = bpy.context.view_layer
    for o in vl.objects: o.select_set(False)
    objs = [rig] + list(bpy.data.collections['Base'].objects) + list(bpy.data.collections['Outfit_' + outfit].objects)
    for o in objs: o.hide_set(False); o.select_set(True)
    vl.objects.active = rig
    ad = rig.animation_data; keep = ad.action; ad.action = None
    for pb in rig.pose.bones: pb.rotation_quaternion = (1, 0, 0, 0); pb.location = (0, 0, 0)
    bpy.ops.export_scene.gltf(filepath=path, export_format='GLB', use_selection=True, export_apply=False,
                              export_animations=True, export_animation_mode='ACTIONS', export_skins=True,
                              export_force_sampling=True, export_yup=True, export_materials='EXPORT')
    ad.action = keep
    return path

def verify_glb(path):
    """Reimport into the scene, report what came back, then delete everything the import created."""
    before = {k: set(getattr(bpy.data, k).keys()) for k in ('objects', 'meshes', 'armatures', 'actions', 'materials', 'images')}
    bpy.ops.import_scene.gltf(filepath=path)
    new = {k: [getattr(bpy.data, k)[n] for n in getattr(bpy.data, k).keys() if n not in before[k]] for k in before}
    arms = [o for o in new['objects'] if o.type == 'ARMATURE']
    meshes = [o for o in new['objects'] if o.type == 'MESH' and o.users_collection and not o.name.startswith('Icosphere')]   # importer's bone-display shape
    rep = {'file': os.path.basename(path), 'size_kb': round(os.path.getsize(path) / 1024),
           'armatures': len(arms), 'bones': len(arms[0].data.bones) if arms else 0,
           'meshes': {o.name: dict(tris=sum(len(p.vertices) - 2 for p in o.data.polygons), skinned=any(m.type == 'ARMATURE' for m in o.modifiers),
                                   groups=len(o.vertex_groups), mats=[m.name for m in o.data.materials]) for o in meshes},
           'images': [i.name for i in new['images']], 'clips': {}}
    for a in new['actions']:
        fcs = list(iter_fcurves(a))
        moving = sum(1 for fc in fcs if len(fc.keyframe_points) > 1 and
                     max(k.co[1] for k in fc.keyframe_points) - min(k.co[1] for k in fc.keyframe_points) > 1e-4)
        loop = max((abs(fc.keyframe_points[0].co[1] - fc.keyframe_points[-1].co[1]) for fc in fcs if len(fc.keyframe_points) > 1), default=0)
        rep['clips'][a.name.split('.')[0]] = dict(frames=round(a.frame_range[1] - a.frame_range[0]), seconds=round((a.frame_range[1] - a.frame_range[0]) / 30, 3),
                                    animated_channels=moving, loop_seam_max_delta=round(loop, 5))
    for o in new['objects']: bpy.data.objects.remove(o)
    for k in ('meshes', 'armatures', 'actions', 'materials', 'images'):
        for x in new[k]:
            try: getattr(bpy.data, k).remove(x)
            except Exception: pass
    return rep

README_TXT = """Male student v3 - outfit switching
Collections under Male_Character:
  Base            body (head, neck, hands) + hair - always on
  Outfit_Casual   tee + rust undershirt, jeans, boots
  Outfit_Student  shirt, vest, tie, trousers, shoes + bare arms
Show exactly one Outfit_ collection (eye + camera icons), or run in the Python console:
  bpy.data.collections['Outfit_Casual'].hide_viewport = False; bpy.data.collections['Outfit_Student'].hide_viewport = True
Clips (actions on Male_Rig): Idle, Walk, Run - 30 fps, in place. Each has a muted NLA track;
assign one in the Action editor (or unmute its track) to play it.
Rebuild everything: scripts/v3/run_v3.py
"""

def finish_blend(path):
    stage = bpy.data.collections.get('Preview_Stage') or bpy.data.collections.new('Preview_Stage')
    if stage.name not in bpy.context.scene.collection.children: bpy.context.scene.collection.children.link(stage)
    for n in ('Light_Key', 'Light_Fill', 'Light_Rim', 'PreviewCam', 'Stage_Floor'):
        o = bpy.data.objects.get(n)
        if o and o.name not in stage.objects:
            for c in list(o.users_collection): c.objects.unlink(o)
            stage.objects.link(o)
    for c in list(bpy.data.collections):      # factory 'Collection', importer's 'glTF_not_exported'
        if c.name.startswith(('Collection', 'glTF_not_exported')) and not c.all_objects: bpy.data.collections.remove(c)
    t = bpy.data.texts.get('README') or bpy.data.texts.new('README'); t.clear(); t.write(README_TXT)
    bpy.ops.file.pack_all()
    bpy.ops.wm.save_as_mainfile(filepath=path)
    return path
