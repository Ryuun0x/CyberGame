"""Preview lighting, cameras and still/turnaround rendering."""
import numpy as np

import os
REN = (os.environ.get("LP_OUT") or r"C:\Users\pajar\Documents\GitHub\CyberGame\ArtSource\LowPolyStudents") + r"\renders"

def setup_stage():
    sc = bpy.context.scene
    sc.render.engine = 'BLENDER_EEVEE'
    sc.view_settings.view_transform = 'Standard'
    sc.view_settings.look = 'None'
    sc.render.film_transparent = True
    sc.render.fps = 30
    try: sc.eevee.taa_render_samples = 32
    except Exception: pass
    w = sc.world or bpy.data.worlds.new('World'); sc.world = w
    try: w.use_nodes = True
    except Exception: pass
    bg = w.node_tree.nodes.get('Background')
    bg.inputs[0].default_value = (0.62, 0.62, 0.63, 1); bg.inputs[1].default_value = 0.55
    pc = bpy.data.collections.get('Preview_Stage')
    if pc:
        for o in list(pc.objects): bpy.data.objects.remove(o)
    else:
        pc = bpy.data.collections.new('Preview_Stage'); sc.collection.children.link(pc)
    def light(name, kind, energy, loc, rot, size=1.0, color=(1, 1, 1)):
        ld = bpy.data.lights.get(name) or bpy.data.lights.new(name, kind)
        ld.energy = energy; ld.color = color
        if kind == 'AREA': ld.size = size
        if kind == 'SUN': ld.angle = math.radians(8)
        o = bpy.data.objects.new(name, ld); pc.objects.link(o)
        o.location = loc; o.rotation_euler = [math.radians(a) for a in rot]
        return o
    light('Key_Sun', 'SUN', 3.2, (0, 0, 5), (48, 0, -32), color=(1.0, 0.97, 0.92))
    light('Fill_Area', 'AREA', 260, (4.5, -4.0, 2.6), (65, 0, 48), size=4, color=(0.9, 0.94, 1.0))
    light('Rim_Area', 'AREA', 220, (-2.5, 4.5, 3.2), (-60, 0, 210), size=3)
    me = bpy.data.meshes.get('Stage_Floor') or bpy.data.meshes.new('Stage_Floor')
    if not me.vertices:
        me.from_pydata([(-12, -12, 0), (12, -12, 0), (12, 12, 0), (-12, 12, 0)], [], [(0, 1, 2, 3)])
    fl = bpy.data.objects.new('Stage_Floor', me); pc.objects.link(fl); fl.hide_render = True
    m = bpy.data.materials.get('Stage_Floor') or bpy.data.materials.new('Stage_Floor')
    try: m.use_nodes = True
    except Exception: pass
    m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (0.5, 0.5, 0.51, 1)
    m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value = 0.95
    if not me.materials: me.materials.append(m)
    cd = bpy.data.cameras.get('Preview_Cam') or bpy.data.cameras.new('Preview_Cam')
    cam = bpy.data.objects.new('Preview_Cam', cd); pc.objects.link(cam)
    sc.camera = cam
    return cam

def aim(cam, target, yaw, dist=6.0, elev=4.0, ortho=None, lens=85):
    a = math.radians(yaw); e = math.radians(elev)
    d = V((math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)))
    cam.location = target + d * dist
    cam.rotation_euler = (-d).to_track_quat('-Z', 'Y').to_euler()
    if ortho: cam.data.type = 'ORTHO'; cam.data.ortho_scale = ortho
    else: cam.data.type = 'PERSP'; cam.data.lens = lens
    cam.data.clip_end = 100

def solo(names):
    root = bpy.data.collections['Characters']
    for c in root.children: c.hide_render = c.name not in names

def still(path, res=(700, 1000)):
    sc = bpy.context.scene
    sc.render.resolution_x, sc.render.resolution_y = res; sc.render.resolution_percentage = 100
    sc.render.image_settings.media_type = 'IMAGE'; sc.render.image_settings.file_format = 'PNG'
    sc.render.image_settings.color_mode = 'RGBA'
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True)
    return path

def stitch(paths, out, axis=1):
    arrs = []
    for p in paths:
        im = bpy.data.images.load(p, check_existing=False)
        a = np.array(im.pixels[:], dtype=np.float32).reshape(im.size[1], im.size[0], 4)
        if a[..., 3].min() < 1.0:   # composite transparent film over a flat studio grey
            bgc = np.array([0.78, 0.78, 0.79], dtype=np.float32)
            a[..., :3] = a[..., :3] * a[..., 3:4] + bgc * (1 - a[..., 3:4]); a[..., 3] = 1.0
        arrs.append(a); bpy.data.images.remove(im)
    big = np.concatenate(arrs[::-1] if axis == 0 else arrs, axis=axis)
    h, w = big.shape[:2]
    im = bpy.data.images.new('__stitch', w, h, alpha=True)
    im.pixels.foreach_set(big.ravel())
    im.filepath_raw = out; im.file_format = 'PNG'; im.save(); bpy.data.images.remove(im)
    return out

VIEWS = [('front', 0), ('left', 90), ('back', 180), ('threequarter', 35)]

def turnaround(name, x, cam, out, res=(560, 1000), z=0.88, ortho=2.0, views=VIEWS, tag=''):
    solo([name]); paths = []
    for vn, yaw in views:
        aim(cam, V((x, 0, z)), yaw, ortho=ortho)
        paths.append(still(f"{REN}\\_tmp_{name}_{vn}{tag}.png", res))
    return stitch(paths, out)
