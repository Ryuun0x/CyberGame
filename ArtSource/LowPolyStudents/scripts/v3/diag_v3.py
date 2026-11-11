"""Headless diagnostics for the v3 male: close-up renders of arms/elbows and head/neck, rest + Run.
blender -b <Male_Student_v3.blend> --python diag_v3.py   (env LP_DIAG_OUT = output folder, LP_DIAG_TAG = filename tag)
"""
import bpy, math, os
from mathutils import Vector as V
OUT = os.environ.get('LP_DIAG_OUT') or os.path.join(os.path.dirname(bpy.data.filepath), 'renders', 'diag')
TAG = os.environ.get('LP_DIAG_TAG', 'before')
os.makedirs(OUT, exist_ok=True)
sc = bpy.context.scene
sc.render.engine = 'BLENDER_EEVEE'; sc.render.film_transparent = False
sc.render.resolution_x, sc.render.resolution_y = 520, 520
cam = sc.camera or bpy.data.objects.new('DiagCam', bpy.data.cameras.new('DiagCam'))
if cam.name not in sc.objects: sc.collection.objects.link(cam)
sc.camera = cam; cam.data.type = 'ORTHO'
rig = next(o for o in sc.objects if o.type == 'ARMATURE')
for c in bpy.data.collections:          # show the casual outfit only
    if c.name.startswith('Outfit_'): c.hide_render = c.name != 'Outfit_Casual'
for o in sc.objects:
    if o.type == 'ARMATURE': o.hide_render = True

def aim(t, yaw, elev, scale):
    a, e = math.radians(yaw), math.radians(elev)
    d = V((math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)))
    cam.location = V(t) + d * 6; cam.rotation_euler = (-d).to_track_quat('-Z', 'Y').to_euler(); cam.data.ortho_scale = scale

def shot(name):
    sc.render.filepath = os.path.join(OUT, f'{TAG}_{name}.png'); bpy.ops.render.render(write_still=True)

def pose(clip, frame):
    ad = rig.animation_data
    if ad:
        for tr in ad.nla_tracks: tr.mute = True
        ad.action = bpy.data.actions.get(clip) if clip else None
        if ad.action:
            try: ad.action_slot = ad.action.slots[0]
            except Exception: pass
    if not clip:
        for pb in rig.pose.bones: pb.rotation_quaternion = (1, 0, 0, 0); pb.location = (0, 0, 0)
    sc.frame_set(frame)

pose(None, 0)
aim((0.2, 0, 1.15), 0, 0, 0.75); shot('armL_front')
aim((0.2, 0, 1.15), 60, 10, 0.75); shot('armL_3q')
aim((0.2, 0, 1.15), 120, 10, 0.75); shot('armL_back')
aim((0, 0, 1.45), 0, 0, 0.55); shot('neck_front')
aim((0, 0, 1.45), 90, 0, 0.55); shot('neck_side')
aim((0, 0, 0.85), 0, 0, 1.9); shot('full_front')
aim((0, 0, 0.85), 90, 0, 1.9); shot('full_side')
run = next((a.name for a in bpy.data.actions if 'Run' in a.name), None)
if run:
    pose(run, 5); aim((0, 0, 1.1), 70, 5, 1.0); shot('run5')
    pose(run, 16); aim((0, 0, 1.1), -70, 5, 1.0); shot('run16')
# numbers: arm / neck dimensions from the evaluated meshes
dg = bpy.context.evaluated_depsgraph_get()
pose(None, 0)
lines = []
for o in sc.objects:
    if o.type != 'MESH' or o.hide_render: continue
    me = o.evaluated_get(dg).to_mesh(); ws = [o.matrix_world @ v.co for v in me.vertices]
    if ws:
        lines.append(f"{o.name}: z {min(p.z for p in ws):.3f}..{max(p.z for p in ws):.3f} tris={sum(len(p.vertices)-2 for p in me.polygons)}")
    o.evaluated_get(dg).to_mesh_clear()
for b in rig.data.bones:
    if b.name in ('neck', 'head', 'clavicle.L', 'upper_arm.L', 'forearm.L', 'hand.L', 'chest'):
        h = rig.matrix_world @ b.head_local; t = rig.matrix_world @ b.tail_local
        lines.append(f"bone {b.name}: head {tuple(round(x,3) for x in h)} tail {tuple(round(x,3) for x in t)} len {(t-h).length:.3f}")
open(os.path.join(OUT, f'{TAG}_numbers.txt'), 'w').write('\n'.join(lines))
