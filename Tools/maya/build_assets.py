"""Procedurally model and export Zulvyre's low-poly asset set with Maya 2022.

Run:
    "C:\\Program Files\\Autodesk\\Maya2022\\bin\\mayapy.exe" build_assets.py
"""

import os
import maya.standalone

maya.standalone.initialize(name="python")

import maya.cmds as cmds
import maya.mel as mel

OUT = r"E:\ai game\ZulvyreUnity\Assets\Resources\Art3D"

PALETTE = {
    "skin":     (0.72, 0.56, 0.43),
    "cloth":    (0.27, 0.36, 0.23),
    "accent":   (0.80, 0.64, 0.31),
    "leather":  (0.32, 0.22, 0.14),
    "metal":    (0.60, 0.63, 0.66),
    "wood":     (0.34, 0.24, 0.15),
    "roof":     (0.38, 0.28, 0.17),
    "stone":    (0.45, 0.46, 0.43),
    "darkstone":(0.27, 0.28, 0.30),
    "thatch":   (0.55, 0.44, 0.24),
    "plaster":  (0.72, 0.64, 0.50),
    "leaf":     (0.20, 0.34, 0.18),
    "darkleaf": (0.12, 0.24, 0.14),
    "orcskin":  (0.35, 0.43, 0.24),
    "bone":     (0.84, 0.81, 0.70),
    "blood":    (0.46, 0.12, 0.15),
    "vampcloth":(0.15, 0.13, 0.19),
    "fur":      (0.30, 0.25, 0.19),
    "hair":     (0.16, 0.13, 0.12),
    "ember":    (0.90, 0.48, 0.15),
    "ice":      (0.55, 0.70, 0.78),
}

parts = []


def ensure_material(name):
    group = name + "SG"
    if cmds.objExists(group):
        return group
    shader = cmds.shadingNode("lambert", asShader=True, name=name)
    rgb = PALETTE[name]
    cmds.setAttr(shader + ".color", rgb[0], rgb[1], rgb[2], type="double3")
    sg = cmds.sets(renderable=True, noSurfaceShader=True, empty=True, name=group)
    cmds.connectAttr(shader + ".outColor", sg + ".surfaceShader", force=True)
    return sg


def use(obj, material):
    cmds.sets(obj, edit=True, forceElement=ensure_material(material))
    parts.append(obj)
    return obj


def place(obj, x=0.0, z=0.0, base=None, center=None, rot=None):
    if rot:
        cmds.xform(obj, ro=rot, ws=True)
    bb = cmds.exactWorldBoundingBox(obj)
    if base is not None:
        y = base - bb[1]
    else:
        y = center - (bb[1] + bb[4]) * 0.5
    cmds.xform(obj, t=(x, y, z), ws=True)
    return obj


def box(size, material, x=0.0, z=0.0, base=None, center=None, rot=None, name="part"):
    o = cmds.polyCube(w=size[0], h=size[1], d=size[2], name=name)[0]
    use(o, material)
    return place(o, x, z, base, center, rot)


def sphere(radius, material, x=0.0, z=0.0, base=None, center=None, scale=None, name="part"):
    o = cmds.polySphere(r=radius, sx=10, sy=8, name=name)[0]
    if scale:
        cmds.xform(o, s=scale, ws=True)
    use(o, material)
    return place(o, x, z, base, center)


def cone(radius, height, material, x=0.0, z=0.0, base=None, center=None, sides=8, rot=None, name="part"):
    o = cmds.polyCone(r=radius, h=height, subdivisionsAxis=sides, subdivisionsHeight=1, name=name)[0]
    use(o, material)
    return place(o, x, z, base, center, rot)


def cylinder(radius, height, material, x=0.0, z=0.0, base=None, center=None, sides=10, rot=None, name="part"):
    o = cmds.polyCylinder(r=radius, h=height, subdivisionsAxis=sides, subdivisionsHeight=1, name=name)[0]
    use(o, material)
    return place(o, x, z, base, center, rot)


def collapse(name):
    global parts
    if not parts:
        return None
    merged = cmds.polyUnite(parts, ch=False, mergeUVSets=True, name=name)[0]
    cmds.delete(merged, constructionHistory=True)
    cmds.makeIdentity(merged, apply=True, t=True, r=True, s=True)
    parts = []
    return merged


def export(name, mesh):
    path = os.path.join(OUT, name + ".fbx")
    cmds.select(mesh, r=True)
    mel.eval('FBXExportSkins -v false')
    mel.eval('FBXExportShapes -v false')
    mel.eval('FBXExportCameras -v false')
    mel.eval('FBXExportLights -v false')
    mel.eval('FBXExportTangents -v false')
    mel.eval('FBXExportApplyConstantKeyReducer -v false')
    mel.eval('FBXExportUpAxis "y"')
    mel.eval('FBXExport -f "%s" -s' % path.replace("\\", "/"))
    print("EXPORTED %s" % path)


def new_scene():
    global parts
    cmds.file(new=True, force=True)
    parts = []
    cmds.currentUnit(linear="m")


# ---------------------------------------------------------------- characters

def build_hero():
    new_scene()
    for sx in (-1, 1):
        box((0.17, 0.18, 0.28), "leather", x=sx * 0.12, z=0.02, base=0.0, name="boot")
        box((0.16, 0.55, 0.18), "leather", x=sx * 0.12, base=0.17, name="leg")
    box((0.42, 0.16, 0.25), "leather", base=0.70)
    box((0.46, 0.52, 0.27), "cloth", base=0.85)
    box((0.48, 0.07, 0.29), "accent", base=0.86)
    for sx in (-1, 1):
        box((0.17, 0.2, 0.21), "cloth", x=sx * 0.3, base=1.24, name="shoulder")
        box((0.13, 0.44, 0.15), "cloth", x=sx * 0.31, base=0.82, name="arm")
        box((0.12, 0.12, 0.14), "skin", x=sx * 0.31, base=0.72, name="hand")
    sphere(0.14, "skin", center=1.58, name="head")
    sphere(0.165, "cloth", center=1.60, z=-0.02, scale=(1.0, 0.98, 1.06), name="hood")
    box((0.52, 0.86, 0.06), "cloth", z=-0.17, base=0.66, name="cloak")
    box((0.54, 0.06, 0.07), "accent", z=-0.17, base=1.42, name="cloak trim")
    box((0.04, 0.62, 0.015), "metal", x=0.4, base=0.2, name="blade")
    box((0.16, 0.05, 0.06), "accent", x=0.4, base=0.82, name="guard")
    box((0.05, 0.16, 0.05), "leather", x=0.4, base=0.80, name="grip")
    export("hero", collapse("hero"))


def build_orc():
    """Hunched, heavy-shouldered raider: broad fur mantle, tusked jaw, cleaver."""
    new_scene()
    for sx in (-1, 1):
        box((0.32, 0.16, 0.4), "leather", x=sx * 0.19, z=0.04, base=0.0, name="boot")
        box((0.27, 0.54, 0.29), "leather", x=sx * 0.19, base=0.16, name="shin")
        box((0.29, 0.46, 0.31), "orcskin", x=sx * 0.19, base=0.68, name="thigh")

    # hips, belt, barrel chest pitched forward
    box((0.74, 0.18, 0.44), "leather", base=1.02, name="belt")
    box((0.16, 0.16, 0.08), "metal", z=0.24, base=1.04, name="buckle")
    box((0.78, 0.52, 0.46), "orcskin", z=0.04, base=1.12, rot=(6, 0, 0), name="chest")
    box((0.82, 0.08, 0.5), "leather", z=0.03, base=1.20, rot=(6, 0, -18), name="chest strap")

    # fur mantle sitting behind the jaw so the face stays readable
    box((0.94, 0.16, 0.44), "fur", z=-0.12, base=1.62, name="mantle")
    for sx in (-1, 1):
        box((0.34, 0.26, 0.4), "fur", x=sx * 0.44, z=-0.1, base=1.58, name="mantle tuft")
        box((0.30, 0.26, 0.36), "metal", x=sx * 0.54, z=0.02, base=1.54, rot=(0, 0, sx * -10), name="pauldron")
        box((0.24, 0.48, 0.26), "orcskin", x=sx * 0.55, base=1.02, name="upper arm")
        box((0.26, 0.34, 0.28), "orcskin", x=sx * 0.58, base=0.66, name="forearm")
        box((0.20, 0.18, 0.22), "orcskin", x=sx * 0.58, base=0.48, name="hand")
        sphere(0.05, "metal", x=sx * 0.58, z=0.12, center=0.78, name="bracer stud")

    # neck, skull, heavy brow, jutting jaw
    box((0.24, 0.16, 0.24), "orcskin", z=0.02, base=1.68, name="neck")
    sphere(0.235, "orcskin", z=0.06, center=1.94, scale=(1.04, 1.0, 1.06), name="head")
    box((0.36, 0.08, 0.16), "orcskin", z=0.21, center=1.99, name="brow")
    box((0.34, 0.2, 0.26), "orcskin", z=0.15, center=1.82, name="jaw")
    box((0.1, 0.1, 0.08), "orcskin", z=0.29, center=1.87, name="nose")
    for sx in (-1, 1):
        cone(0.045, 0.2, "bone", x=sx * 0.11, z=0.24, base=1.76, rot=(0, 0, sx * -14), sides=5, name="tusk")
        sphere(0.04, "blood", x=sx * 0.1, z=0.28, center=1.98, name="eye")
        cone(0.075, 0.18, "orcskin", x=sx * 0.24, z=0.02, center=1.95, rot=(0, 0, sx * 74), sides=4, name="ear")
    box((0.1, 0.16, 0.24), "hair", z=-0.03, center=2.13, name="topknot")

    # cleaver
    cylinder(0.05, 1.02, "wood", x=0.7, base=0.4, sides=6, name="axe handle")
    box((0.4, 0.3, 0.07), "metal", x=0.85, base=1.12, name="axe head")
    box((0.18, 0.1, 0.05), "metal", x=0.7, base=1.26, name="axe back")
    export("orc", collapse("orc"))


def build_vampire():
    """Tall, narrow silhouette: high collar, long cloak, fangs and cravat."""
    new_scene()
    for sx in (-1, 1):
        box((0.15, 0.78, 0.17), "vampcloth", x=sx * 0.11, base=0.06, name="leg")
        box((0.12, 0.1, 0.24), "leather", x=sx * 0.11, z=0.04, base=0.0, name="shoe")

    # narrow waist, chest, belt
    box((0.5, 0.34, 0.3), "vampcloth", base=0.72, name="hips")
    box((0.44, 0.56, 0.28), "vampcloth", base=1.02, name="chest")
    box((0.46, 0.09, 0.3), "blood", base=0.98, name="sash")
    box((0.34, 0.08, 0.06), "blood", z=0.17, base=1.34, name="cravat")
    sphere(0.06, "blood", z=0.19, center=1.38, name="brooch")

    # shoulders and long coat tails
    for sx in (-1, 1):
        box((0.18, 0.2, 0.24), "vampcloth", x=sx * 0.26, base=1.46, name="shoulder")
        box((0.13, 0.56, 0.16), "vampcloth", x=sx * 0.3, base=0.94, name="arm")
        box((0.1, 0.11, 0.13), "skin", x=sx * 0.3, base=0.83, name="hand")
        box((0.16, 0.72, 0.06), "vampcloth", x=sx * 0.2, z=-0.06, base=0.3, rot=(0, 0, sx * 6), name="coat tail")
        box((0.15, 0.62, 0.05), "vampcloth", x=sx * 0.09, z=0.15, base=0.42, rot=(0, 0, sx * -3), name="coat front")
        cone(0.12, 0.26, "vampcloth", x=sx * 0.15, z=-0.05, base=1.48, rot=(0, 0, sx * 20), sides=5, name="collar")

    # head, swept hair, fangs
    sphere(0.135, "skin", z=0.02, center=1.67, scale=(0.94, 1.06, 1.0), name="head")
    box((0.24, 0.1, 0.22), "hair", z=-0.02, center=1.77, name="hair")
    box((0.1, 0.06, 0.16), "hair", z=-0.06, center=1.74, name="hair tail")
    for sx in (-1, 1):
        cone(0.014, 0.06, "bone", x=sx * 0.04, z=0.11, center=1.58, sides=4, name="fang")
        sphere(0.032, "blood", x=sx * 0.05, z=0.12, center=1.69, name="eye")

    # flowing cloak with trim
    box((0.62, 1.12, 0.05), "blood", z=-0.19, base=0.4, name="cape")
    box((0.5, 0.34, 0.06), "vampcloth", z=-0.21, base=1.24, name="cape yoke")
    box((0.64, 0.06, 0.06), "vampcloth", z=-0.19, base=1.5, name="cape trim")
    for sx in (-1, 1):
        box((0.08, 0.5, 0.04), "vampcloth", x=sx * 0.3, z=-0.19, base=0.4, rot=(0, 0, sx * 6), name="cape fold")
    export("vampire", collapse("vampire"))


def build_wolf():
    new_scene()
    # Tapered volumes instead of one long box so the silhouette reads as a wolf.
    cylinder(0.23, 0.58, "fur", z=0.0, center=0.68, sides=10, rot=(90, 0, 0), name="barrel")
    sphere(0.26, "fur", z=0.34, center=0.70, scale=(1.0, 1.12, 1.3), name="chest")
    sphere(0.25, "fur", z=-0.42, center=0.66, scale=(1.02, 1.08, 1.2), name="haunch")
    box((0.34, 0.07, 0.72), "hair", z=0.0, center=0.92, name="saddle")

    cylinder(0.14, 0.34, "fur", z=0.43, center=0.88, sides=8, rot=(35, 0, 0), name="neck")
    sphere(0.165, "fur", z=0.6, center=1.04, scale=(1.0, 0.95, 1.05), name="head")
    box((0.13, 0.11, 0.24), "fur", z=0.76, center=1.0, name="muzzle")
    box((0.065, 0.05, 0.06), "hair", z=0.89, center=1.0, name="nose")
    box((0.19, 0.055, 0.1), "hair", z=0.63, center=1.13, name="brow")

    for sx in (-1, 1):
        cone(0.08, 0.22, "hair", x=sx * 0.11, z=0.54, base=1.12, sides=4, name="ear")
        box((0.1, 0.4, 0.11), "fur", x=sx * 0.16, z=0.34, base=0.0, name="leg")
        box((0.1, 0.4, 0.11), "fur", x=sx * 0.16, z=-0.42, base=0.0, name="leg")
        box((0.12, 0.09, 0.2), "hair", x=sx * 0.16, z=0.4, base=0.0, name="paw")
        box((0.12, 0.09, 0.2), "hair", x=sx * 0.16, z=-0.36, base=0.0, name="paw")
        sphere(0.028, "ember", x=sx * 0.07, z=0.72, center=1.07, name="eye")

    cone(0.085, 0.52, "hair", z=-0.78, center=0.76, rot=(102, 0, 0), sides=6, name="tail")
    export("wolf", collapse("wolf"))


def build_skeleton():
    new_scene()
    for sx in (-1, 1):
        box((0.09, 0.62, 0.1), "bone", x=sx * 0.1, base=0.0, name="leg")
        box((0.1, 0.1, 0.2), "bone", x=sx * 0.1, z=0.04, base=0.0, name="foot")
    box((0.3, 0.12, 0.16), "bone", base=0.6)
    cylinder(0.045, 0.5, "bone", base=0.7, sides=6, name="spine")
    for i in range(3):
        box((0.36 - i * 0.04, 0.05, 0.2), "bone", center=1.0 + i * 0.12, name="rib")
    box((0.4, 0.12, 0.14), "bone", base=1.34)
    sphere(0.13, "bone", center=1.6, name="skull")
    box((0.14, 0.08, 0.1), "bone", z=0.09, center=1.52, name="jaw")
    sphere(0.03, "blood", x=-0.05, z=0.1, center=1.62, name="eye")
    sphere(0.03, "blood", x=0.05, z=0.1, center=1.62, name="eye")
    for sx in (-1, 1):
        box((0.07, 0.46, 0.08), "bone", x=sx * 0.26, base=0.88, name="arm")
    box((0.04, 0.55, 0.015), "metal", x=0.36, base=0.3, name="blade")
    box((0.14, 0.04, 0.05), "metal", x=0.36, base=0.82, name="guard")
    box((0.34, 0.42, 0.05), "wood", x=-0.34, base=0.9, name="shield")
    export("skeleton", collapse("skeleton"))


# ------------------------------------------------------------------ villages

def build_house():
    new_scene()
    box((4.6, 2.6, 4.0), "plaster", base=0.0, name="walls")
    for sx in (-1, 1):
        for sz in (-1, 1):
            box((0.24, 2.6, 0.24), "wood", x=sx * 2.2, z=sz * 1.9, base=0.0, name="post")
    box((4.8, 0.18, 0.22), "wood", z=2.0, base=2.4, name="beam")
    cone(3.9, 2.1, "thatch", base=2.6, sides=4, rot=(0, 45, 0), name="roof")
    box((0.9, 1.62, 0.14), "wood", z=2.02, base=0.0, name="door")
    box((0.55, 0.55, 0.1), "wood", x=1.4, z=2.02, center=1.55, name="window")
    box((0.55, 0.55, 0.1), "wood", x=-1.4, z=2.02, center=1.55, name="window")
    box((0.55, 1.5, 0.55), "stone", x=1.2, z=-0.8, base=2.6, name="chimney")
    export("house", collapse("house"))


def build_longhouse():
    new_scene()
    box((5.8, 2.2, 3.4), "wood", base=0.0, name="walls")
    cone(4.7, 1.9, "thatch", base=2.2, sides=4, rot=(0, 45, 0), name="roof")
    for i, z in enumerate((-0.6, 0.6)):
        cone(0.14, 0.7, "bone", z=z, base=4.0, sides=5, name="spike")
    box((0.9, 1.5, 0.12), "darkstone", z=1.72, base=0.0, name="door")
    box((1.6, 0.5, 0.1), "blood", z=1.73, base=1.9, name="banner")
    sphere(0.28, "bone", x=0.0, z=1.85, center=2.75, name="skull")
    for sx in (-1, 1):
        cone(0.06, 0.3, "bone", x=sx * 0.18, z=1.9, base=2.5, sides=5, name="tusk")
        box((0.2, 2.2, 0.2), "wood", x=sx * 2.4, z=0.0, base=0.0, name="post")
    export("longhouse", collapse("longhouse"))


def build_spire():
    new_scene()
    box((4.4, 3.2, 4.2), "darkstone", base=0.0, name="base")
    box((2.6, 5.0, 2.6), "darkstone", base=3.2, name="tower")
    cone(2.5, 3.6, "vampcloth", base=8.2, sides=4, rot=(0, 45, 0), name="roof")
    for sx in (-1, 1):
        for sz in (-1, 1):
            cone(0.35, 1.8, "vampcloth", x=sx * 1.9, z=sz * 1.9, base=3.2, sides=4, name="spire")
    for x in (-0.6, 0.6):
        box((0.34, 0.9, 0.1), "blood", x=x, z=1.32, center=6.4, name="window")
    box((1.0, 1.9, 0.14), "wood", z=2.12, base=0.0, name="door")
    export("spire", collapse("spire"))


def build_watchtower():
    new_scene()
    box((2.8, 4.2, 2.8), "stone", base=0.0, name="base")
    box((3.7, 0.3, 3.7), "wood", base=4.2, name="deck")
    for sx in (-1, 1):
        for sz in (-1, 1):
            box((0.18, 1.0, 0.18), "wood", x=sx * 1.7, z=sz * 1.7, base=4.5, name="rail")
    cone(2.8, 2.2, "roof", base=5.4, sides=4, rot=(0, 45, 0), name="roof")
    box((0.9, 1.6, 0.12), "wood", z=1.42, base=0.0, name="door")
    export("watchtower", collapse("watchtower"))


# ---------------------------------------------------------------- wilderness

def build_pine():
    new_scene()
    cylinder(0.2, 2.6, "wood", base=0.0, sides=7, name="trunk")
    cone(2.0, 3.6, "darkleaf", base=1.5, sides=8, name="tier")
    cone(1.55, 3.1, "darkleaf", base=3.6, sides=8, name="tier")
    cone(1.05, 2.7, "leaf", base=5.5, sides=8, name="tier")
    export("pine", collapse("pine"))


def build_oak():
    new_scene()
    cylinder(0.32, 3.2, "wood", base=0.0, sides=8, name="trunk")
    sphere(1.8, "leaf", center=4.1, scale=(1.1, 0.9, 1.0), name="canopy")
    sphere(1.35, "leaf", x=1.2, z=0.3, center=3.7, name="canopy")
    sphere(1.3, "darkleaf", x=-1.0, z=-0.4, center=3.8, name="canopy")
    sphere(1.1, "leaf", z=0.9, center=4.9, name="canopy")
    export("oak", collapse("oak"))


def build_rock():
    new_scene()
    sphere(1.0, "stone", scale=(1.6, 0.85, 1.35), base=0.0, name="rock")
    sphere(0.55, "darkstone", x=1.0, z=0.5, scale=(1.3, 0.8, 1.1), base=0.0, name="rock")
    export("rock", collapse("rock"))


def build_well():
    new_scene()
    cylinder(1.35, 0.85, "stone", base=0.0, sides=14, name="ring")
    cylinder(1.0, 0.9, "darkstone", base=0.02, sides=14, name="water")
    for sx in (-1, 1):
        box((0.16, 2.0, 0.16), "wood", x=sx * 1.05, base=0.85, name="post")
    cone(1.7, 1.0, "roof", base=2.85, sides=4, rot=(0, 45, 0), name="roof")
    cylinder(0.12, 1.1, "wood", base=2.2, sides=6, rot=(0, 0, 90), name="axle")
    box((0.4, 0.4, 0.4), "wood", base=1.2, name="bucket")
    export("well", collapse("well"))


def build_gate():
    new_scene()
    for sx in (-1, 1):
        box((0.85, 4.2, 0.9), "darkstone", x=sx * 1.95, base=0.0, name="pillar")
        box((1.15, 0.35, 1.15), "stone", x=sx * 1.95, base=4.2, name="cap")
        sphere(0.3, "bone", x=sx * 1.95, center=4.85, z=0.1, name="skull")
    box((4.9, 0.8, 1.0), "darkstone", base=4.55, name="lintel")
    box((6.0, 0.2, 1.2), "stone", z=0.9, base=0.0, name="step")
    box((5.4, 0.2, 1.2), "stone", z=1.4, base=0.0, name="step")
    cone(0.35, 0.9, "ember", z=0.0, base=4.8, sides=5, name="crest")
    export("gate", collapse("gate"))


def build_pillar():
    new_scene()
    box((1.4, 0.35, 1.4), "darkstone", base=0.0, name="base")
    cylinder(0.45, 3.2, "darkstone", base=0.35, sides=10, name="shaft")
    box((1.15, 0.3, 1.15), "stone", base=3.55, name="cap")
    cylinder(0.55, 0.25, "metal", base=3.85, sides=10, name="bowl")
    cone(0.42, 0.5, "ember", base=4.0, sides=7, name="flame")
    export("pillar", collapse("pillar"))


def build_lanternpost():
    new_scene()
    box((0.6, 0.3, 0.6), "stone", base=0.0, name="base")
    cylinder(0.11, 2.8, "wood", base=0.3, sides=6, name="pole")
    box((0.7, 0.12, 0.12), "wood", base=2.85, name="arm")
    box((0.34, 0.42, 0.34), "metal", x=0.3, base=2.55, name="lantern")
    box((0.24, 0.3, 0.24), "ember", x=0.3, center=2.76, name="glow")
    cone(0.26, 0.2, "metal", x=0.3, base=2.97, sides=4, name="cap")
    export("lanternpost", collapse("lanternpost"))


def build_fence():
    new_scene()
    for sx in (-1, 1):
        box((0.14, 1.25, 0.14), "wood", x=sx * 1.05, base=0.0, name="post")
    box((2.3, 0.1, 0.08), "wood", base=0.42, name="rail")
    box((2.3, 0.1, 0.08), "wood", base=0.86, name="rail")
    export("fence", collapse("fence"))


def build_crate():
    new_scene()
    box((0.85, 0.85, 0.85), "wood", base=0.0, name="crate")
    box((0.9, 0.1, 0.1), "leather", base=0.4, name="band")
    box((0.1, 0.9, 0.1), "leather", base=0.0, name="band")
    export("crate", collapse("crate"))


def main():
    if not os.path.isdir(OUT):
        os.makedirs(OUT)
    cmds.loadPlugin("fbxmaya", quiet=True)
    build_hero()
    build_orc()
    build_vampire()
    build_wolf()
    build_skeleton()
    build_house()
    build_longhouse()
    build_spire()
    build_watchtower()
    build_pine()
    build_oak()
    build_rock()
    build_well()
    build_gate()
    build_pillar()
    build_lanternpost()
    build_fence()
    build_crate()
    print("DONE")


main()
