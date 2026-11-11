"""Dev helper for the live Blender session: (re)load all v3 modules into builtins.V3."""
import builtins, os
D = os.path.dirname(os.path.abspath(__file__))
G = {'__name__': 'v3'}
for f in ('v3_model.py', 'v3_head.py', 'v3_pixel.py', 'v3_hair.py', 'v3_body.py', 'v3_outfits.py', 'v3_rig.py', 'v3_output.py'):
    p = os.path.join(D, f)
    if os.path.exists(p): exec(compile(open(p, encoding='utf8').read(), p, 'exec'), G)
builtins.V3 = G
