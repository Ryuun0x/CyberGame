"""Comparison renders against the reference sheet: neutral light, rest pose, no overlays (render engine only)."""

def neutral_lighting():
    for n, e in (('Key_Sun', 2.4), ('Fill_Area', 330), ('Rim_Area', 140)):
        o = bpy.data.objects.get(n)
        if o: o.data.energy = e

def compare_set(nm, outdir, tag, pose=None):
    """Body: front/side(left)/back/three-quarter orthographic. Head: front + side close-ups."""
    d = INFO[nm]; rig = d['rig']; cam = bpy.data.objects['Preview_Cam']
    neutral_lighting(); bpy.context.scene.render.film_transparent = True
    rest_pose(rig); bpy.context.scene.frame_set(0); solo([nm])
    x = rig.location.x; hz = d['P']['head_c'].z
    body = []
    for vn, yaw in (('front', 0), ('side', 90), ('back', 180), ('threequarter', 35)):
        aim(cam, V((x, 0, 0.9)), yaw, ortho=2.0)
        body.append(still(f"{outdir}\\_{tag}_{vn}.png", (600, 1000)))
    head = []
    for vn, yaw in (('head_front', 0), ('head_side', 90), ('head_3q', 35)):
        aim(cam, V((x, 0.0, hz + 0.01)), yaw, ortho=0.34)
        head.append(still(f"{outdir}\\_{tag}_{vn}.png", (700, 700)))
    b = stitch(body, f"{outdir}\\{nm}_{tag}_body.png")
    h = stitch(head, f"{outdir}\\{nm}_{tag}_head.png")
    for f in body + head: os.remove(f)
    restore_nla(rig)
    return b, h
