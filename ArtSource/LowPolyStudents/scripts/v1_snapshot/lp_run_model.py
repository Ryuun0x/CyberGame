# Dev helper: rebuild models + stage (exec inside Blender)
import bpy, traceback
D = r"C:\Users\pajar\Documents\GitHub\CyberGame\ArtSource\LowPolyStudents\scripts\\"
G = {}
for f in ('lp_build.py', 'lp_parts.py', 'lp_parts2.py', 'lp_assemble.py', 'lp_render.py'):
    exec(compile(open(D + f, encoding='utf8').read(), f, 'exec'), G)
