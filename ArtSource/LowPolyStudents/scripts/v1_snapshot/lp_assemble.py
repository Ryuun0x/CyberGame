"""Variant assembly, Blender objects and armatures (exec'd after lp_parts2.py)."""

VARIANTS = [('M', 'Casual'), ('M', 'Uniform')]          # female variants paused (code kept)
SEXNAME = {'M': 'Male', 'F': 'Female'}
X_OFFSET = {('M', 'Casual'): -0.6, ('F', 'Casual'): -1.8, ('M', 'Uniform'): 0.6, ('F', 'Uniform'): 1.8}

TEE_M = [(0.925, .158, .1, .1), (0.915, .177, .111, .113), (0.935, .177, .111, .113), (0.935, .194, .122, .12), (0.965, .194, .122, .12),
         (1.06, .182, .114, .11), (1.18, .19, .12, .11), (1.30, .2, .127, .113), (1.38, .2, .122, .112), (1.425, .186, .108, .104),
         (1.465, .134, .086, .09), (1.496, .084, .075, .071), (1.504, .073, .066, .062), (1.496, .057, .055, .053)]
TEE_F = [(0.875, .15, .098, .098), (0.865, .168, .108, .108), (0.885, .168, .108, .108), (0.885, .182, .118, .116), (0.91, .18, .116, .114),
         (0.99, .152, .104, .1), (1.10, .152, .112, .098), (1.19, .162, .135, .1), (1.26, .168, .118, .1), (1.30, .17, .105, .098),
         (1.335, .156, .094, .091), (1.368, .108, .072, .076), (1.39, .068, .064, .062), (1.398, .06, .057, .054), (1.39, .048, .047, .046)]
TEE_MATS = ['Undershirt_Rust', 'Undershirt_Rust', 'Tee_Olive'] + ['Tee_Olive'] * 8 + ['Undershirt_Rust', 'Undershirt_Rust']
TEE_MATS_F = ['Undershirt_Rust', 'Undershirt_Rust', 'Tee_Olive'] + ['Tee_Olive'] * 9 + ['Undershirt_Rust', 'Undershirt_Rust']

SHIRT_M = [(0.95, .178, .114, .11), (1.06, .176, .11, .106), (1.18, .184, .116, .106), (1.30, .194, .124, .11), (1.38, .194, .118, .108),
           (1.425, .18, .104, .1), (1.465, .128, .082, .086), (1.494, .081, .072, .068), (1.488, .06, .057, .054)]
SHIRT_F = [(0.9, .168, .108, .106), (0.99, .148, .1, .096), (1.10, .148, .108, .094), (1.19, .158, .13, .096), (1.26, .164, .114, .096),
           (1.30, .166, .102, .094), (1.335, .152, .091, .088), (1.368, .104, .07, .073), (1.39, .068, .063, .06), (1.385, .052, .05, .048)]
VEST_M = [(0.905, .176, .112, .112), (0.915, .19, .122, .12), (0.945, .19, .122, .12), (1.06, .184, .116, .112), (1.18, .192, .122, .112),
          (1.30, .202, .13, .116), (1.38, .202, .124, .114), (1.425, .188, .11, .106), (1.465, .136, .088, .092), (1.496, .087, .076, .072)]
VEST_F = [(0.858, .17, .11, .11), (0.868, .18, .118, .116), (0.895, .18, .118, .116), (0.99, .155, .106, .102), (1.10, .155, .114, .1),
          (1.19, .165, .138, .102), (1.26, .172, .12, .102), (1.30, .174, .108, .1), (1.335, .16, .097, .094), (1.368, .112, .075, .078),
          (1.392, .072, .067, .064)]
HIP_M = [(0.82, .15, .095, .1), (0.86, .172, .108, .112), (0.93, .17, .108, .108), (1.0, .152, .096, .096)]
HIP_F = [(0.76, .145, .09, .1), (0.81, .165, .1, .108), (0.88, .162, .104, .106), (0.97, .14, .09, .09)]
SKIRT = [(0.53, .2, .13, .136), (0.65, .192, .124, .13), (0.77, .182, .118, .122), (0.86, .172, .11, .114), (0.92, .158, .104, .104), (0.98, .138, .092, .092)]

def front_fade(lo, hi, base, faded):
    return lambda s: (lambda i: faded if i in (0, 9) else base) if lo <= s < hi else base

def build_variant(sex, outfit):
    P = params(sex); J = joints(P); fem = sex == 'F'
    skin = 'Skin_Female' if fem else 'Skin_Male'
    body, outfitm, hair = MB(), MB(), MB()
    head = build_head(P, J, skin, 'Lips_Female' if fem else 'Lips_Male')
    body.merge(head); body.merge(build_neck(P, J, skin)); body.merge(build_hand(P, J, skin))
    hair.merge(build_hair_female(P, J) if fem else build_hair_male(P))
    if outfit == 'Casual':
        trnd = random.Random(5)
        tm, _ = build_torso(P, J, TEE_F if fem else TEE_M, TEE_MATS_F if fem else TEE_MATS, jit=lambda i: trnd.uniform(-0.018, 0.018))
        outfitm.merge(tm); outer = tm.bvh()
        outfitm.merge(build_arm_layer(P, J, [(-0.05, .058, .04), (0.06, .062, .056), (0.2, .062, .062), (0.42, .061, .062), (0.40, .052, .053)], 'Tee_Olive'))
        outfitm.merge(build_arm_layer(P, J, [(0.25, .054, .054), (0.6, .052, .051), (0.97, .046, .046), (1.1, .046, .044), (1.55, .041, .039),
                                             (1.9, .035, .033), (1.94, .038, .036), (1.97, .037, .035), (1.95, .027, .027)], 'Undershirt_Rust'))
        hrnd = random.Random(9)
        hm, _ = build_torso(P, J, HIP_F if fem else HIP_M, 'Denim', cap_bottom=True, jit=lambda i: hrnd.uniform(-0.03, 0.03)); outfitm.merge(hm)
        hb = hm.bvh(); outfitm.wfn = W_torso(J, P)
        for sg in (1, -1):   # back pockets
            px = sg * 0.075
            raised_decal(outfitm, [(px - 0.048, 0.912), (px + 0.048, 0.912), (px + 0.044, 0.858), (px, 0.842), (px - 0.044, 0.858)], hb, 'Denim_Faded', 0.003, dirn=V((0, -1, 0)))
        outfitm.wfn = None
        legs = ([(0.0, .082, .084), (0.45, .074, .075), (0.92, .058, .058), (1.08, .056, .056), (1.5, .052, .052), (1.92, .057, .057), (1.9, .048, .048)] if fem else
                [(0.0, .092, .094), (0.3, .086, .088), (0.62, .079, .08), (0.92, .069, .069), (1.0, .069, .068), (1.08, .067, .066), (1.35, .065, .064),
                 (1.62, .064, .064), (1.78, .068, .068), (1.9, .072, .072), (1.88, .056, .056)])
        frnd = random.Random(4)
        fade = lambda s: (lambda i: 'Denim_Faded' if (i in (0, 9) and frnd.random() < 0.5) else 'Denim') if 0.3 <= s < 1.4 else 'Denim'
        lm = build_leg_layer(P, J, legs, 'Denim', matfn=lambda s: front_fade(0.4, 1.1, 'Denim', 'Denim_Faded')(s) if fem else fade(s),
                             jit=None if fem else denim_jit(21))
        outfitm.merge(lm)
        if fem:
            lb = lm.bvh()
            decal(outfitm, [offset2([(-.016, .004), (-.006, .011), (.006, .007), (.017, .01), (.012, -.002), (.016, -.009), (.002, -.006), (-.012, -.01)], 0.096, 0.47)], lb, 'Denim_Rip', gap=0.002, maxe=0.01)
            decal(outfitm, [offset2([(-.011, .003), (-.002, .008), (.01, .005), (.007, -.003), (.011, -.007), (-.004, -.005)], -0.105, 0.66)], lb, 'Denim_Rip', gap=0.002, maxe=0.01)
            decal(outfitm, [offset2([(-.008, .002), (.0, .006), (.009, .003), (.004, -.004), (-.006, -.004)], -0.088, 0.43)], lb, 'Denim_Rip', gap=0.002, maxe=0.01)
        outfitm.merge(build_shoe(P, J, 'boot'))
        k = 0.84 if fem else 1.0; cz = 1.165 if fem else 1.245
        outfitm.wfn = W_torso(J, P)
        decal(outfitm, [offset2(p, 0, cz) for p in skull_polys(k)], outer, 'Skull_Print', gap=0.0035, maxe=0.034)
        outfitm.wfn = None
    else:
        sh, _ = build_torso(P, J, SHIRT_F if fem else SHIRT_M, 'Shirt_White'); outfitm.merge(sh); shirt_bvh = sh.bvh()
        if fem:
            outfitm.merge(build_arm_layer(P, J, [(-0.05, .054, .038), (0.06, .057, .052), (0.3, .057, .057), (0.92, .052, .052), (0.9, .043, .043)], 'Shirt_White'))
            body.merge(build_arm_layer(P, J, [(0.75, .045, .045), (1.0, .041, .04), (1.45, .037, .035), (1.93, .027, .023)], skin))
        else:
            outfitm.merge(build_arm_layer(P, J, [(-0.05, .058, .04), (0.06, .062, .056), (0.2, .062, .062), (0.5, .058, .059), (0.48, .048, .048)], 'Shirt_White'))
            body.merge(build_arm_layer(P, J, [(0.3, .05, .05), (0.62, .048, .047), (0.97, .043, .042), (1.12, .044, .042), (1.5, .039, .037), (1.93, .028, .024)], skin))
        vm, edge, back = build_vest(P, J, VEST_F if fem else VEST_M, 6 if fem else 5, 0.064 if fem else 0.074, 'Vest_Gray', 'Vest_Trim')
        outfitm.merge(vm); vb = vm.bvh()
        tp, tn = on_surface(vb, edge, 0.0015)
        tm_ = MB(); tm_.wfn = W_torso(J, P); ribbon(tm_, tp, tn, 0.011 * P['s'], 0.003, 'Vest_Trim'); outfitm.merge(tm_)
        tp, tn = on_surface(vb, resample(back, 12), 0.0015)
        tm_ = MB(); tm_.wfn = W_torso(J, P); ribbon(tm_, tp, tn, 0.011 * P['s'], 0.003, 'Vest_Trim'); outfitm.merge(tm_)
        z0 = 1.382 if fem else 1.483; rx, ry = (0.058, 0.055) if fem else (0.066, 0.062)
        col = build_collar(P, J, z0, rx, ry, 'Shirt_White'); outfitm.merge(col)
        zV = (VEST_F if fem else VEST_M)[6 if fem else 5][0]
        tie = build_tie(P, J, shirt_bvh, z0, -(ry + 0.004), zV - 0.035); outfitm.merge(tie)
        un = MB(); un.merge(vm); un.merge(col); un.merge(tie); ub = un.bvh()
        clip_z = zV - 0.035
        ly, cl, cn = build_lanyard(P, J, ub, z0, ry, clip_z, None); outfitm.merge(ly)
        card, ctop, cbot = build_id_card(P, J, cl, cn, fem); outfitm.merge(card)
        P['id_bone'] = (ctop + V((0, 0, 0.012)), cbot)
        outfitm.wfn = W_torso(J, P)
        cx, cz = (0.075, 1.255) if fem else (0.088, 1.35)
        decal(outfitm, [offset2(shield(P['s']), cx, cz)], vb, 'Crest_Navy', gap=0.002, maxe=0.03)
        decal(outfitm, [offset2(shield(P['s'], 0.72), cx, cz + 0.001)], vb, 'Crest_Gold', gap=0.0032, maxe=0.03)
        decal(outfitm, [offset2([(0, .009), (.006, -.002), (0, -.011), (-.006, -.002)], cx, cz)], vb, 'Crest_Green', gap=0.0044, maxe=0.03)
        outfitm.wfn = None
        if fem:
            sk = MB(); sk.wfn = W_skirt(J, P, 0.98, 0.53)
            ro = [sk.ring(torso_ring(z, w, df, db, 16, jit=(lambda i, z=z: 0.045 * ss(0.85, 0.6, z) * (i % 2)))) for z, w, df, db in SKIRT]
            ri = [sk.ring(torso_ring(z, w * 0.965, df * 0.965, db * 0.965, 16, jit=(lambda i, z=z: 0.045 * ss(0.85, 0.6, z) * (i % 2)))) for z, w, df, db in SKIRT]
            for a, b in zip(ro, ro[1:]): sk.bridge(a, b, 'Skirt_Navy')
            for a, b in zip(ri, ri[1:]): sk.bridge(b, a, 'Cloth_Inner')
            sk.bridge(ri[0], ro[0], 'Skirt_Navy')
            sk.cap(sk.ring(torso_ring(0.80, .17, .108, .114, 16)), 'Cloth_Inner', center=V((0, 0, 0.78)))
            outfitm.merge(sk)
            P['skirt'] = True
            body.merge(build_leg_layer(P, J, [(0.74, .055, .057), (0.85, .05, .052), (1.0, .046, .047), (1.3, .05, .05), (1.65, .037, .037), (1.97, .03, .03)], skin, n=8, cap0=True))
            body.merge(build_foot_skin(P, J, skin))
            outfitm.merge(build_shoe(P, J, 'pump'))
        else:
            hm, _ = build_torso(P, J, HIP_M, 'Trousers_Navy', cap_bottom=True); outfitm.merge(hm)
            outfitm.merge(build_leg_layer(P, J, [(0.0, .092, .094), (0.5, .084, .084), (1.0, .07, .07), (1.5, .068, .067), (1.94, .068, .068), (1.92, .05, .05)], 'Trousers_Navy'))
            outfitm.merge(build_shoe(P, J, 'school'))
        outer = un.bvh()
    if outfit == 'Casual': outer_bvh = outer
    else:
        o2 = MB(); o2.merge(vm); o2.merge(col); outer_bvh = o2.bvh()
    pack = build_backpack(P, J, outer_bvh)
    return P, J, {'Body': body, 'Hair': hair, 'Outfit': outfitm, 'Backpack': pack}

# ------------------------------------------------------------------ blender objects
def make_object(name, mb, coll):
    me = bpy.data.meshes.new(name)
    me.from_pydata([tuple(p) for p in mb.v], [], mb.f)
    for m in mb.mats: me.materials.append(get_mat(m))
    me.polygons.foreach_set('material_index', [mb.fm[i] for i in range(len(mb.f))])
    me.update()
    ob = bpy.data.objects.new(name, me); coll.objects.link(ob)
    groups = {}
    for vi, w in enumerate(mb.w):
        for b, x in w.items():
            g = groups.get(b) or ob.vertex_groups.new(name=b); groups[b] = g
            g.add([vi], x, 'REPLACE')
    bm = bmesh.new(); bm.from_mesh(me)
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0002)
    bmesh.ops.triangulate(bm, faces=bm.faces, quad_method='BEAUTY', ngon_method='BEAUTY')
    bm.to_mesh(me); bm.free()
    for p in me.polygons: p.use_smooth = False
    return ob

PARENTS = {'pelvis': 'root', 'spine': 'pelvis', 'chest': 'spine', 'neck': 'chest', 'head': 'neck'}
for sd in ('L', 'R'):
    PARENTS.update({'clavicle.' + sd: 'chest', 'upper_arm.' + sd: 'clavicle.' + sd, 'forearm.' + sd: 'upper_arm.' + sd,
                    'hand.' + sd: 'forearm.' + sd, 'thigh.' + sd: 'pelvis', 'shin.' + sd: 'thigh.' + sd,
                    'foot.' + sd: 'shin.' + sd, 'toe.' + sd: 'foot.' + sd})
CONNECT = {'spine', 'chest', 'head'} | {b + '.' + sd for b in ('forearm', 'hand', 'shin', 'foot', 'toe') for sd in 'LR'}

def make_armature_data(name, J, P, extras):
    arm = bpy.data.armatures.new(name)
    tmp = bpy.data.objects.new('__tmp_arm', arm); bpy.context.scene.collection.objects.link(tmp)
    vl = bpy.context.view_layer
    vl.update()
    for o in vl.objects:
        if o: o.select_set(False)
    vl.objects.active = tmp; tmp.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    bones = dict(J); bones.update(extras)
    par = dict(PARENTS); par.update({k: v[2] for k, v in extras.items()})
    for b in ['root'] + list(par.keys()):
        if b in arm.edit_bones: continue
        eb = arm.edit_bones.new(b); eb.head, eb.tail = bones[b][0], bones[b][1]
        eb.align_roll(V((0, 0, 1)) if b.startswith(('foot', 'toe', 'root')) else V((0, -1, 0)))
    for b, p in par.items():
        eb = arm.edit_bones[b]; eb.parent = arm.edit_bones[p]
        if b in CONNECT: eb.use_connect = True
    arm.edit_bones['root'].use_deform = False
    bpy.ops.object.mode_set(mode='OBJECT')
    bpy.data.objects.remove(tmp)
    arm.display_type = 'OCTAHEDRAL'
    return arm

def extras_for(P, outfit):
    ex = {}
    if P['sex'] == 'F':
        pp = pony_points(P)
        ex['ponytail.01'] = (pp[0], pp[2], 'head'); ex['ponytail.02'] = (pp[2], pp[4], 'ponytail.01')
    if 'id_bone' in P: ex['id_card'] = (P['id_bone'][0], P['id_bone'][1], 'chest')
    return ex

def clear_generated():
    for o in list(bpy.data.objects):
        if o.name.startswith('__tmp'): bpy.data.objects.remove(o)
    for c in list(bpy.data.collections):
        if c.name.startswith(('Male_', 'Female_', 'Characters', 'Preview')):
            for o in list(c.objects): bpy.data.objects.remove(o)
            bpy.data.collections.remove(c)
    for d in (bpy.data.meshes, bpy.data.armatures, bpy.data.actions):
        for x in list(d):
            if x.users == 0: d.remove(x)
    bpy.context.view_layer.update()

def build_all(only=None):
    clear_generated()
    root = bpy.data.collections.new('Characters'); bpy.context.scene.collection.children.link(root)
    out = {}
    for sex, outfit in VARIANTS:
        if only and (sex, outfit) not in only: continue
        nm = f'{SEXNAME[sex]}_{outfit}'
        P, J, parts = build_variant(sex, outfit)
        coll = bpy.data.collections.new(nm); root.children.link(coll)
        ex = extras_for(P, outfit)
        # outfits of one character share bone names; armature data per variant (uniform adds id_card)
        arm = make_armature_data(nm + '_Armature', J, P, ex)
        rig = bpy.data.objects.new(nm + '_Rig', arm); coll.objects.link(rig)
        rig.location.x = X_OFFSET[(sex, outfit)]
        tris = 0
        for pn, mb in parts.items():
            ob = make_object(f'{nm}_{pn}', mb, coll)
            ob.parent = rig
            md = ob.modifiers.new('Armature', 'ARMATURE'); md.object = rig
            tris += sum(len(p.vertices) - 2 for p in ob.data.polygons)
        out[nm] = dict(P=P, J=J, rig=rig, tris=tris)
    bpy.context.view_layer.update()
    return out
