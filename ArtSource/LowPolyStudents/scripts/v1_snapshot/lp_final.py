"""Final deliverables: turnaround sheets, preview videos, GLB export + reimport check, .blend save."""
import os
ROOT = os.environ.get("LP_OUT") or r"C:\Users\pajar\Documents\GitHub\CyberGame\ArtSource\LowPolyStudents"
FIN = ROOT + r"\renders"
GLB = ROOT + r"\glb"

def rest_pose(rig):
    ad = rig.animation_data
    if ad:
        ad.action = None
        for tr in ad.nla_tracks: tr.mute = True
    for pb in rig.pose.bones: pb.rotation_quaternion = (1, 0, 0, 0); pb.location = (0, 0, 0)

def restore_nla(rig):
    ad = rig.animation_data; ad.action = None
    for tr in ad.nla_tracks: tr.mute = tr.name != 'Idle'

def final_stills(info):
    cam = bpy.data.objects['Preview_Cam']; out = []
    for nm, d in info.items():
        rig = d['rig']; rest_pose(rig); bpy.context.scene.frame_set(0)
        out.append(turnaround(nm, rig.location.x, cam, f"{FIN}\\{nm}_turnaround.png", res=(700, 1200), ortho=2.0))
        face_sheet(nm, f"{FIN}\\{nm}_face.png", res=(600, 660))
        restore_nla(rig)
    for f in os.listdir(FIN):
        if f.startswith('_'): os.remove(os.path.join(FIN, f))
    return out

def loop_fcurves(act):
    for fc in iter_fcurves(act):
        if not any(m.type == 'CYCLES' for m in fc.modifiers): fc.modifiers.new('CYCLES')

def render_video(info, clip, yaw, path, loops, res=(1280, 720), ortho=None, z=0.95, dist=7.5, lens=60, spacing=None):
    sc = bpy.context.scene; cam = bpy.data.objects['Preview_Cam']
    for nm, d in info.items():
        play(d['rig'], clip); loop_fcurves(d['rig'].animation_data.action)
    solo(list(info.keys()))
    fl = bpy.data.objects['Stage_Floor']; fl.hide_render = False
    sc.render.film_transparent = False
    xs = [d['rig'].location.x for d in info.values()]
    aim(cam, V((sum(xs) / len(xs), 0, z)), yaw, dist=dist, elev=7, ortho=ortho, lens=lens)
    T = CLIPS[clip]['T']
    sc.frame_start, sc.frame_end = 0, T * loops - 1
    s = sc.render; s.resolution_x, s.resolution_y = res; s.resolution_percentage = 100
    s.image_settings.media_type = 'VIDEO'; s.image_settings.file_format = 'FFMPEG'
    s.ffmpeg.format = 'MPEG4'; s.ffmpeg.codec = 'H264'; s.ffmpeg.constant_rate_factor = 'HIGH'
    s.ffmpeg.ffmpeg_preset = 'GOOD'; s.ffmpeg.audio_codec = 'NONE'
    s.filepath = path; s.use_file_extension = False
    bpy.ops.render.render(animation=True)
    s.image_settings.media_type = 'IMAGE'; s.image_settings.file_format = 'PNG'
    sc.render.film_transparent = True; fl.hide_render = True
    for d in info.values(): restore_nla(d['rig'])
    return path

def export_glb(nm, d):
    rig = d['rig']; coll = bpy.data.collections[nm]
    vl = bpy.context.view_layer
    for o in vl.objects: o.select_set(False)
    keep = rig.location.copy(); rig.location = (0, 0, 0)
    ad = rig.animation_data; ad.action = None
    for tr in ad.nla_tracks: tr.mute = False
    for o in coll.objects: o.select_set(True)
    vl.objects.active = rig
    path = f"{GLB}\\{nm}.glb"
    bpy.ops.export_scene.gltf(filepath=path, export_format='GLB', use_selection=True, export_yup=True, export_apply=False,
                              export_animations=True, export_animation_mode='NLA_TRACKS', export_force_sampling=True,
                              export_frame_step=1, export_skins=True, export_def_bones=False, export_leaf_bone=False,
                              export_anim_slide_to_zero=True, export_optimize_animation_size=False, export_image_format='NONE')
    rig.location = keep; restore_nla(rig)
    return path

def verify_glb(path):
    """Import into a scratch scene and report meshes, materials, skeleton and clips."""
    main = bpy.context.window.scene
    tmp = bpy.data.scenes.new('__verify'); bpy.context.window.scene = tmp
    before_act = set(bpy.data.actions.keys())
    try:
        bpy.ops.import_scene.gltf(filepath=path)
        objs = list(tmp.objects)
        arms = [o for o in objs if o.type == 'ARMATURE']; meshes = [o for o in objs if o.type == 'MESH']
        new_acts = [a for a in bpy.data.actions if a.name not in before_act]
        clips = {}
        for a in new_acts:
            fr = a.frame_range
            clips[a.name] = dict(frames=round(fr[1] - fr[0], 2), seconds=round((fr[1] - fr[0]) / 30, 3),
                                 channels=len(list(iter_fcurves(a))))
        rep = dict(file=os.path.basename(path), size_kb=round(os.path.getsize(path) / 1024, 1),
                   armatures=[(a.name, len(a.data.bones)) for a in arms],
                   meshes=[(m.name, sum(len(p.vertices) - 2 for p in m.data.polygons), len(m.data.materials),
                            any(md.type == 'ARMATURE' for md in m.modifiers), len(m.vertex_groups)) for m in meshes],
                   materials=sorted({ms.name for m in meshes for ms in m.data.materials if ms}),
                   clips=clips)
        # pose check: evaluate each clip mid-way and confirm bones actually move
        if arms:
            a0 = arms[0]; moved = {}
            for a in new_acts:
                a0.animation_data_create(); a0.animation_data.action = a
                try: a0.animation_data.action_slot = a.slots[0]
                except Exception: pass
                tmp.frame_set(int(a.frame_range[1] // 2)); bpy.context.view_layer.update()
                moved[a.name] = round(max((pb.matrix.translation - pb.bone.matrix_local.translation).length for pb in a0.pose.bones), 3)
            rep['max_bone_offset_mid_clip_m'] = moved
    finally:
        bpy.context.window.scene = main
        for o in list(tmp.objects): bpy.data.objects.remove(o)
        bpy.data.scenes.remove(tmp)
        for a in [a for a in bpy.data.actions if a.name not in before_act]: bpy.data.actions.remove(a)
        for coll in (bpy.data.meshes, bpy.data.armatures, bpy.data.materials, bpy.data.images):
            for x in list(coll):
                if x.users == 0: coll.remove(x)
    return rep

def purge_unused():
    for a in list(bpy.data.actions):
        if a.name.startswith('Female_') or (a.users == 0 and not a.use_fake_user): bpy.data.actions.remove(a)
    for coll in (bpy.data.meshes, bpy.data.armatures, bpy.data.materials, bpy.data.cameras, bpy.data.lights, bpy.data.images):
        for x in list(coll):
            if x.users == 0: coll.remove(x)

def save_blend():
    purge_unused()
    path = ROOT + r"\LowPoly_Student_Male.blend"
    bpy.ops.wm.save_as_mainfile(filepath=path, check_existing=False)
    return path

def face_sheet(nm, out, res=(360, 400)):
    """Head close-ups: front, left, right, three-quarter, back (rest pose)."""
    d = INFO[nm]; rig = d['rig']; cam = bpy.data.objects['Preview_Cam']
    rest_pose(rig); bpy.context.scene.frame_set(0); solo([nm]); paths = []
    for i, yaw in enumerate((0, 90, -90, 35, 180)):
        aim(cam, V((rig.location.x, 0.0, d['P']['head_c'].z - 0.03)), yaw, ortho=0.4)
        paths.append(still(f"{REN}\_tmp_face_{i}.png", res))
    return stitch(paths, out)
