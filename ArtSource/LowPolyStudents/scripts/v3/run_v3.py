"""v3 male student: full rebuild -> .blend, GLBs, renders, previews, report.

Blender 5.2 (tested 5.2.3):
    blender.exe --python run_v3.py              (or open in the Text Editor and Run Script)
Outputs go to ../../v3 (override with the LP_V3 environment variable).
"""
import bpy, builtins, os, json
D = os.path.dirname(os.path.abspath(__file__))
G = {'__name__': 'v3'}
for f in ('v3_model.py', 'v3_head.py', 'v3_pixel.py', 'v3_hair.py', 'v3_body.py', 'v3_outfits.py', 'v3_rig.py', 'v3_output.py'):
    p = os.path.join(D, f); exec(compile(open(p, encoding='utf8').read(), p, 'exec'), G)
builtins.V3 = G
VIDEOS = os.environ.get('LP_VIDEOS', '1') != '0'

def main():
    for o in [o for o in bpy.data.objects if o.name in ('Cube', 'Light', 'Camera')]: bpy.data.objects.remove(o)
    bmW, names, J = G['prepare_base']()
    info = G['assemble'](bmW, names, J)
    G['setup_stage']()
    anim = G['animate'](info)
    R = G['REN']; os.makedirs(R, exist_ok=True)
    G['final_stills'](info)
    for clip in ('Idle', 'Walk', 'Run'):
        G['filmstrip'](info, clip, os.path.join(R, f'filmstrip_{clip.lower()}.png'))
    if VIDEOS:
        G['render_clip'](info, 'Idle', os.path.join(R, 'preview_idle_3q.mp4'), loops=1)
        for clip, loops in (('Walk', 3), ('Run', 4)):
            G['render_clip'](info, clip, os.path.join(R, f'preview_{clip.lower()}_3q.mp4'), loops=loops)
            G['render_clip'](info, clip, os.path.join(R, f'preview_{clip.lower()}_side.mp4'), loops=loops, side=True)
    G['use_outfit']('Casual'); G['play'](info['rig'], 'Idle')
    root = G['ROOT']; gl = os.path.join(root, 'glb'); os.makedirs(gl, exist_ok=True)
    report = {'triangles': info['tris'], 'clips': anim, 'glb': {}}
    for outfit in ('Casual', 'Student'):
        report['glb'][outfit] = G['verify_glb'](G['export_glb'](info, outfit, os.path.join(gl, f'Male_{outfit}.glb')))
    G['use_outfit']('Casual'); G['play'](info['rig'], 'Idle')
    G['finish_blend'](os.path.join(root, 'Male_Student_v3.blend'))
    with open(os.path.join(root, 'report.json'), 'w') as fh: json.dump(report, fh, indent=1)
    return report

REPORT = main()
