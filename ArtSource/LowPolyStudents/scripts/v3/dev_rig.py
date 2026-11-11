"""Dev: full assembly with rig + clips in the live session."""
import bpy, builtins, os
exec(compile(open(os.path.join(os.path.dirname(__file__), 'dev.py')).read(), 'dev.py', 'exec'), {'__file__': __file__})
G = builtins.V3
for o in list(bpy.data.objects):
    if o.name.startswith('Dev_'): bpy.data.objects.remove(o)
if globals().get('REBASE') or not hasattr(builtins, 'V3_BASE'):
    builtins.V3_BASE = G['prepare_base']()
bmW, names, J = builtins.V3_BASE
INFO = G['assemble'](bmW, names, J)
G['setup_stage']()
REP = G['animate'](INFO)
builtins.V3_INFO = INFO
