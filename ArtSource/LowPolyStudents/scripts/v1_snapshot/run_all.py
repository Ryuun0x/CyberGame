"""One-shot rebuild of the male low-poly student: models, rig, Idle/Walk/Run, renders, GLBs, .blend.

Blender 5.2:  blender.exe --python run_all.py      (or open in the Text Editor and Run Script)
Outputs go to ../renders, ../glb and ../LowPoly_Student_Male.blend.
"""
import bpy, os
D = os.path.dirname(os.path.abspath(__file__)) + os.sep
G = {}
for f in ('lp_build.py', 'lp_parts.py', 'lp_head_m.py', 'lp_parts2.py', 'lp_assemble.py', 'lp_anim.py', 'lp_render.py', 'lp_final.py'):
    exec(compile(open(D + f, encoding='utf8').read(), f, 'exec'), G)
bpy.app.driver_namespace['LP'] = G

for o in [o for o in bpy.data.objects if o.name in ('Cube', 'Light', 'Camera')]:
    bpy.data.objects.remove(o)
info = G['build_all']()
G['INFO'] = info
G['setup_stage']()
G['animate_all'](info)
bpy.data.objects['Stage_Floor'].scale = (10, 10, 1)
G['final_stills'](info)
F = G['FIN']
G['render_video'](info, 'Idle', 35, F + r"\preview_idle_3q.mp4", loops=1)
G['render_video'](info, 'Walk', 35, F + r"\preview_walk_3q.mp4", loops=3)
G['render_video'](info, 'Walk', 90, F + r"\preview_walk_side.mp4", loops=3, dist=9)
G['render_video'](info, 'Run', 35, F + r"\preview_run_3q.mp4", loops=4)
G['render_video'](info, 'Run', 90, F + r"\preview_run_side.mp4", loops=4, dist=9)
for nm, d in info.items():
    print(G['verify_glb'](G['export_glb'](nm, d)))
G['save_blend']()
