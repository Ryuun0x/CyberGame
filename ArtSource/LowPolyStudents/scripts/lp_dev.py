# Dev: reload scripts, rebuild, render 4 turnarounds stacked. Exec inside Blender.
import bpy, traceback
from mathutils import Vector as V
D = r"C:\Users\pajar\Documents\GitHub\CyberGame\ArtSource\LowPolyStudents\scripts\\"
G = {}
for f in ('lp_build.py', 'lp_parts.py', 'lp_head_m.py', 'lp_parts2.py', 'lp_assemble.py', 'lp_face_import.py', 'lp_base.py', 'lp_anim.py', 'lp_render.py'):
    exec(compile(open(D + f, encoding='utf8').read(), f, 'exec'), G)
bpy.app.driver_namespace['LP'] = G
def dev_all(res=(300, 540), names=None):
    out = G['build_all']()
    cam = G['setup_stage']()
    rows = []
    for nm, info in out.items():
        if names and nm not in names: continue
        rows.append(G['turnaround'](nm, info['rig'].location.x, cam, G['REN'] + f"\_row_{nm}.png", res=res))
    G['stitch'](rows, G['REN'] + "\dev_all.png", axis=0)
    return {k: v['tris'] for k, v in out.items()}
G['dev_all'] = dev_all
def dev_anim():
    out = G['build_all']()
    G['setup_stage']()
    rep = G['animate_all'](out)
    G['INFO'] = out
    return rep
G['dev_anim'] = dev_anim
def pose_sheet(nm, clip, fracs, yaw, out, res=(260, 480), z=0.9, ortho=2.1):
    info = G['INFO'][nm]; rig = info['rig']; cam = bpy.data.objects['Preview_Cam']
    G['solo']([nm]); G['play'](rig, clip)
    T = G['CLIPS'][clip]['T']; paths = []
    for i, fr in enumerate(fracs):
        bpy.context.scene.frame_set(int(round(fr * T)))
        G['aim'](cam, V((rig.location.x, 0, z)), yaw, ortho=ortho)
        paths.append(G['still'](G['REN'] + f"\_tmp_pose_{i}.png", res))
    return G['stitch'](paths, out)
G['pose_sheet'] = pose_sheet
def face_sheet(nm, out, res=(360, 400)):
    info = G['INFO'][nm]; rig = info['rig']; cam = bpy.data.objects['Preview_Cam']
    ad = rig.animation_data
    for tr in ad.nla_tracks: tr.mute = True
    ad.action = None
    for pb in rig.pose.bones: pb.rotation_quaternion = (1, 0, 0, 0); pb.location = (0, 0, 0)
    bpy.context.scene.frame_set(0); G['solo']([nm]); paths = []
    for i, yaw in enumerate((0, 90, -90, 35, 180)):
        G['aim'](cam, V((rig.location.x, 0.0, info['P']['head_c'].z - 0.03)), yaw, ortho=0.4)
        paths.append(G['still'](G['REN'] + f"\_tmp_face_{i}.png", res))
    return G['stitch'](paths, out)
G['face_sheet'] = face_sheet
