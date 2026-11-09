"""Procedurally model, rig, skin and animate Zulvyre's human cast with Maya 2022.

Produces a man, a woman and a child, each exported as one FBX per animation clip
(idle, walk, run, attack, hurt, die) into Assets/Resources/Art3D/Humans.

Run:
    "C:\\Program Files\\Autodesk\\Maya2022\\bin\\mayapy.exe" build_humans.py
"""

import os
import math
import maya.standalone

maya.standalone.initialize(name="python")

import maya.cmds as cmds
import maya.mel as mel

OUT = r"E:\ai game\ZulvyreUnity\Assets\Resources\Art3D\Humans"
FPS = 24

CLIPS = ("idle", "walk", "run", "attack", "hurt", "die")

CHARS = {
    "man": {
        "height": 1.78, "build": 1.00, "head": 1.00,
        "skin": (0.70, 0.55, 0.44), "hair": (0.15, 0.10, 0.07), "hairLong": False,
        "cloth": (0.24, 0.30, 0.38), "accent": (0.42, 0.31, 0.18),
        "leather": (0.17, 0.12, 0.09), "dress": False, "bust": 0.0,
        "lips": (0.55, 0.36, 0.32), "eye": (0.92, 0.92, 0.90), "iris": (0.24, 0.34, 0.28),
    },
    "woman": {
        "height": 1.66, "build": 0.88, "head": 0.96,
        "skin": (0.76, 0.60, 0.50), "hair": (0.20, 0.12, 0.07), "hairLong": True,
        "cloth": (0.48, 0.19, 0.25), "accent": (0.72, 0.58, 0.30),
        "leather": (0.20, 0.14, 0.11), "dress": True, "bust": 0.038,
        "lips": (0.66, 0.34, 0.34), "eye": (0.93, 0.93, 0.91), "iris": (0.30, 0.24, 0.16),
    },
    "child": {
        "height": 1.18, "build": 0.72, "head": 1.22,
        "skin": (0.79, 0.63, 0.52), "hair": (0.30, 0.19, 0.10), "hairLong": False,
        "cloth": (0.68, 0.56, 0.24), "accent": (0.30, 0.42, 0.34),
        "leather": (0.24, 0.17, 0.12), "dress": False, "bust": 0.0,
        "lips": (0.68, 0.44, 0.40), "eye": (0.94, 0.94, 0.92), "iris": (0.28, 0.22, 0.16),
    },
}

MESHES = []
SHADERS = {}
# Bind-pose local translation of every joint, so animation can key offsets from
# rest instead of flattening the skeleton to the origin.
BIND = {}


# ------------------------------------------------------------------- shading

def shader(name, color, glossy=False):
    group = name + "SG"
    if group in SHADERS:
        return SHADERS[group]
    node = cmds.shadingNode("blinn" if glossy else "lambert", asShader=True, name=name)
    cmds.setAttr(node + ".color", color[0], color[1], color[2], type="double3")
    if glossy:
        cmds.setAttr(node + ".specularColor", 0.22, 0.22, 0.22, type="double3")
        cmds.setAttr(node + ".eccentricity", 0.45)
    sg = cmds.sets(renderable=True, noSurfaceShader=True, empty=True, name=group)
    cmds.connectAttr(node + ".outColor", sg + ".surfaceShader", force=True)
    SHADERS[group] = sg
    return sg


def assign(obj, sg):
    cmds.sets(obj, edit=True, forceElement=sg)
    MESHES.append(obj)
    return obj


# ------------------------------------------------------------------ geometry

def ball(pos, r, sg, scale=(1.0, 1.0, 1.0), seg=18, rings=14, name="ball"):
    o = cmds.polySphere(r=r, sx=seg, sy=rings, name=name)[0]
    if scale != (1.0, 1.0, 1.0):
        cmds.xform(o, s=scale, ws=True)
    cmds.xform(o, t=pos, ws=True)
    return assign(o, sg)


def block(pos, size, sg, rot=(0.0, 0.0, 0.0), soft=True, name="block"):
    o = cmds.polyCube(w=size[0], h=size[1], d=size[2], name=name)[0]
    if soft:
        cmds.polySoftEdge(o, a=180, ch=False)
    if rot != (0.0, 0.0, 0.0):
        cmds.xform(o, ro=rot, ws=True)
    cmds.xform(o, t=pos, ws=True)
    return assign(o, sg)


def tube(p0, p1, r, sg, seg=6, sides=14, name="tube"):
    dx, dy, dz = p1[0] - p0[0], p1[1] - p0[1], p1[2] - p0[2]
    length = math.sqrt(dx * dx + dy * dy + dz * dz)
    o = cmds.polyCylinder(r=r, h=length, sx=sides, sy=seg, sz=1, name=name)[0]
    rx = math.degrees(math.asin(max(-1.0, min(1.0, dz / length)))) if length > 1e-6 else 0.0
    rz = -math.degrees(math.asin(max(-1.0, min(1.0, dx / length)))) if length > 1e-6 else 0.0
    if rx or rz:
        cmds.xform(o, ro=(rx, 0.0, rz), ws=True)
    cmds.xform(o, t=tuple((p0[i] + p1[i]) * 0.5 for i in range(3)), ws=True)
    return assign(o, sg)


def skirt(pos, r_top, r_bottom, height, sg, rings=10, sides=24, name="skirt"):
    """One tapered, slightly belled cone. Stacked tubes left stair-step rings that
    read as a stack of buckets on the player model, so the flare is a single mesh."""
    o = cmds.polyCylinder(r=1.0, h=height, sx=sides, sy=rings, sz=1, name=name)[0]
    cmds.xform(o, t=(pos[0], pos[1] + height * 0.5, pos[2]), ws=True)
    for v in cmds.ls(o + ".vtx[*]", fl=True):
        p = cmds.pointPosition(v, world=True)
        t = max(0.0, min(1.0, (p[1] - pos[1]) / height))
        r = r_top + (r_bottom - r_top) * pow(1.0 - t, 1.25)
        cmds.xform(v, ws=True, t=(pos[0] + (p[0] - pos[0]) * r, p[1], pos[2] + (p[2] - pos[2]) * r))
    cmds.polySoftEdge(o, a=180, ch=False)
    return assign(o, sg)


# ---------------------------------------------------------------------- rig

def joints_for(p):
    H = p["height"]
    b = p["build"]
    hipY = 0.527 * H
    waistY = 0.615 * H
    chestY = 0.712 * H
    neckY = 0.838 * H
    headY = 0.918 * H
    shoulderY = 0.795 * H
    shoulderX = 0.126 * H * b
    elbowY = 0.615 * H
    elbowX = shoulderX + 0.022 * H
    handY = 0.438 * H
    handX = elbowX + 0.014 * H
    hipX = 0.056 * H * b
    kneeY = 0.283 * H
    ankleY = 0.062 * H
    toeZ = 0.115 * H

    table = {
        "root": ((0.0, 0.0, 0.0), None),
        "hips": ((0.0, hipY, 0.0), "root"),
        "spine": ((0.0, waistY, 0.0), "hips"),
        "chest": ((0.0, chestY, 0.0), "spine"),
        "neck": ((0.0, neckY, 0.0), "chest"),
        "head": ((0.0, headY, 0.0), "neck"),
        "headtop": ((0.0, headY + 0.10 * H * p["head"], 0.0), "head"),
    }
    for side, sign in (("l", -1.0), ("r", 1.0)):
        table["clavicle_" + side] = ((sign * shoulderX * 0.35, shoulderY + 0.012 * H, 0.0), "chest")
        table["upperarm_" + side] = ((sign * shoulderX, shoulderY, 0.0), "clavicle_" + side)
        table["forearm_" + side] = ((sign * elbowX, elbowY, 0.0), "upperarm_" + side)
        table["hand_" + side] = ((sign * handX, handY, 0.0), "forearm_" + side)
        table["thigh_" + side] = ((sign * hipX, hipY - 0.012 * H, 0.0), "hips")
        table["shin_" + side] = ((sign * hipX, kneeY, 0.0), "thigh_" + side)
        table["foot_" + side] = ((sign * hipX, ankleY, 0.0), "shin_" + side)
        table["toe_" + side] = ((sign * hipX, 0.012 * H, toeZ), "foot_" + side)
    return table, {"hipY": hipY, "waistY": waistY, "chestY": chestY, "neckY": neckY,
                   "headY": headY, "shoulderY": shoulderY, "shoulderX": shoulderX,
                   "elbowY": elbowY, "elbowX": elbowX, "handY": handY, "handX": handX,
                   "hipX": hipX, "kneeY": kneeY, "ankleY": ankleY, "toeZ": toeZ}


def build_rig(table):
    created = {}
    for name in ("root", "hips", "spine", "chest", "neck", "head", "headtop",
                 "clavicle_l", "upperarm_l", "forearm_l", "hand_l",
                 "clavicle_r", "upperarm_r", "forearm_r", "hand_r",
                 "thigh_l", "shin_l", "foot_l", "toe_l",
                 "thigh_r", "shin_r", "foot_r", "toe_r"):
        pos = table[name][0]
        cmds.select(clear=True)
        created[name] = cmds.joint(name=name, position=pos, radius=0.03)
    for name, (_, parent) in table.items():
        if parent is None:
            continue
        cmds.parent(created[name], created[parent])
        cmds.setAttr(created[name] + ".jointOrientX", 0)
        cmds.setAttr(created[name] + ".jointOrientY", 0)
        cmds.setAttr(created[name] + ".jointOrientZ", 0)
    BIND.clear()
    for name, joint in created.items():
        BIND[name] = tuple(cmds.getAttr(joint + ".translate")[0])
    return created


def body(kind, p, a):
    H = p["height"]
    b = p["build"]
    hd = p["head"]
    MESHES[:] = []
    SHADERS.clear()

    skin = shader("skin", p["skin"], glossy=True)
    hair = shader("hair", p["hair"])
    cloth = shader("cloth", p["cloth"])
    accent = shader("accent", p["accent"])
    leather = shader("leather", p["leather"])
    lips = shader("lips", p["lips"], glossy=True)
    eye = shader("eye", p["eye"], glossy=True)
    iris = shader("iris", p["iris"], glossy=True)

    hipY, waistY, chestY = a["hipY"], a["waistY"], a["chestY"]
    neckY, headY = a["neckY"], a["headY"]
    shoulderX, shoulderY = a["shoulderX"], a["shoulderY"]
    elbowX, elbowY, handX, handY = a["elbowX"], a["elbowY"], a["handX"], a["handY"]
    hipX, kneeY, ankleY, toeZ = a["hipX"], a["kneeY"], a["ankleY"], a["toeZ"]

    headR = 0.063 * H * hd

    # torso
    ball((0.0, hipY + 0.010 * H, 0.0), 0.092 * H * b, skin, scale=(1.06, 0.82, 0.78), name="pelvis")
    tube((0.0, hipY, -0.004 * H), (0.0, chestY, 0.004 * H), 0.082 * H * b, skin, seg=7, name="abdomen")
    ball((0.0, chestY + 0.012 * H, 0.0), 0.104 * H * b, skin, scale=(1.16, 0.96, 0.76), name="ribcage")
    ball((0.0, chestY + 0.030 * H, -0.014 * H), 0.096 * H * b, skin, scale=(1.06, 0.90, 0.62), name="back")
    for sign in (-1.0, 1.0):
        ball((sign * shoulderX * 0.94, shoulderY + 0.002 * H, 0.0), 0.042 * H * b, skin,
             scale=(1.00, 0.90, 0.94), name="deltoid")
    if p["bust"] > 0.0:
        for sign in (-1.0, 1.0):
            ball((sign * 0.040 * H, chestY + 0.012 * H, 0.058 * H), p["bust"] * H,
                 cloth if p["dress"] else skin, name="bust")
    tube((0.0, chestY + 0.045 * H, 0.0), (0.0, neckY + 0.010 * H, 0.004 * H), 0.029 * H, skin, seg=3, name="neckMesh")

    # head
    ball((0.0, headY, 0.004 * H), headR, skin, scale=(0.92, 1.10, 1.02), seg=22, rings=18, name="skull")
    ball((0.0, headY - 0.043 * H * hd, 0.016 * H), headR * 0.70, skin, scale=(0.84, 0.76, 0.94), name="jaw")
    ball((0.0, headY - 0.014 * H * hd, 0.048 * H), headR * 0.36, skin, scale=(0.68, 0.94, 1.02), name="chin")
    tube((0.0, headY - 0.004 * H, 0.070 * H), (0.0, headY - 0.020 * H, 0.086 * H), headR * 0.16, skin, seg=2, sides=8, name="nose")
    for sign in (-1.0, 1.0):
        ball((sign * headR * 0.90, headY - 0.004 * H, 0.0), headR * 0.23, skin, scale=(0.44, 0.92, 0.70), name="ear")
        ball((sign * headR * 0.40, headY + 0.010 * H, headR * 0.86), headR * 0.22, eye, scale=(1.0, 0.74, 0.56), name="eyeball")
        ball((sign * headR * 0.40, headY + 0.010 * H, headR * 0.92), headR * 0.075, iris, scale=(1.0, 1.0, 0.5), name="iris")
        block((sign * headR * 0.40, headY + 0.040 * H * hd, headR * 0.88),
              (headR * 0.52, headR * 0.10, headR * 0.14), hair, rot=(0.0, 0.0, sign * -6.0), name="brow")
    block((0.0, headY - 0.040 * H * hd, 0.060 * H), (headR * 0.58, headR * 0.11, headR * 0.14), lips, name="mouth")

    # hair
    ball((0.0, headY + 0.016 * H * hd, -0.008 * H), headR * 1.05, hair, scale=(1.01, 0.92, 1.05), name="hairCap")
    ball((0.0, headY + 0.042 * H * hd, 0.046 * H), headR * 0.74, hair, scale=(1.12, 0.40, 0.78), name="fringe")
    if p["hairLong"]:
        tube((0.0, headY + 0.010 * H, -headR * 0.55), (0.0, chestY - 0.030 * H, -headR * 0.80),
             headR * 0.72, hair, seg=6, sides=12, name="hairBack")
        ball((0.0, chestY - 0.040 * H, -headR * 0.80), headR * 0.60, hair, scale=(1.1, 0.8, 0.8), name="hairTip")
        for sign in (-1.0, 1.0):
            tube((sign * headR * 0.78, headY - 0.010 * H, 0.0), (sign * headR * 0.62, chestY + 0.020 * H, headR * 0.20),
                 headR * 0.30, hair, seg=4, sides=8, name="hairSide")
    else:
        ball((0.0, headY - 0.006 * H, -headR * 0.62), headR * 0.72, hair, scale=(1.02, 0.92, 0.72), name="hairBack")

    # arms
    for side, sign in (("l", -1.0), ("r", 1.0)):
        tube((sign * shoulderX, shoulderY, 0.0), (sign * elbowX, elbowY, 0.0),
             0.0335 * H * b, skin, seg=6, name="bicep_" + side)
        tube((sign * elbowX, elbowY, 0.0), (sign * handX, handY + 0.012 * H, 0.0),
             0.027 * H * b, skin, seg=6, name="armLow_" + side)
        ball((sign * handX, handY - 0.012 * H, 0.006 * H), 0.028 * H * b, skin,
             scale=(0.66, 1.10, 0.48), name="palm_" + side)
        ball((sign * (handX - 0.022 * H * b), handY + 0.002 * H, 0.012 * H), 0.011 * H * b, skin,
             scale=(0.70, 1.00, 0.70), name="thumb_" + side)
        # sleeve over the shoulder that ends above the elbow
        ball((sign * shoulderX * 0.98, shoulderY - 0.004 * H, 0.0), 0.040 * H * b, cloth,
             scale=(1.02, 0.72, 0.94), name="pauldron")
        tube((sign * shoulderX, shoulderY + 0.004 * H, 0.0),
             (sign * (shoulderX + (elbowX - shoulderX) * 0.55), shoulderY + (elbowY - shoulderY) * 0.55, 0.0),
             0.037 * H * b, cloth, seg=4, name="sleeve_" + side)

    # legs
    for side, sign in (("l", -1.0), ("r", 1.0)):
        tube((sign * hipX, hipY - 0.010 * H, 0.0), (sign * hipX, kneeY, 0.0),
             0.052 * H * b, skin, seg=6, name="legUp_" + side)
        tube((sign * hipX, kneeY, 0.0), (sign * hipX, ankleY + 0.010 * H, 0.0),
             0.038 * H * b, skin, seg=6, name="legLow_" + side)
        ball((sign * hipX, kneeY, 0.006 * H), 0.042 * H * b, skin, scale=(0.9, 0.9, 0.9), name="knee_" + side)
        block((sign * hipX, ankleY * 0.55, toeZ * 0.34),
              (0.078 * H * b, ankleY * 1.05, toeZ * 1.55), leather, name="bootFoot_" + side)
        tube((sign * hipX, ankleY + 0.10 * H, 0.0), (sign * hipX, ankleY + 0.012 * H, 0.0),
             0.046 * H * b, leather, seg=3, name="boot_" + side)
        ball((sign * hipX, ankleY + 0.095 * H, 0.0), 0.050 * H * b, leather,
             scale=(1.0, 0.42, 1.0), name="bootCuff_" + side)

    # garments
    tube((0.0, hipY - 0.075 * H, -0.004 * H), (0.0, chestY + 0.022 * H, 0.002 * H),
         0.100 * H * b, cloth, seg=8, name="tunic")
    ball((0.0, chestY + 0.014 * H, 0.0), 0.112 * H * b, cloth, scale=(1.18, 0.98, 0.80), name="chestCloth")
    if not p["dress"]:
        tube((0.0, hipY - 0.115 * H, -0.004 * H), (0.0, hipY - 0.070 * H, -0.004 * H),
             0.098 * H * b, cloth, seg=3, sides=18, name="tunicHem")
    tube((0.0, waistY - 0.018 * H, 0.0), (0.0, waistY + 0.014 * H, 0.0), 0.107 * H * b, accent, seg=3, name="belt")
    block((0.0, waistY - 0.002 * H, 0.104 * H * b), (0.030 * H, 0.020 * H, 0.014 * H), accent, name="buckle")
    if p["dress"]:
        skirt((0.0, ankleY + 0.02 * H, 0.0), 0.098 * H * b, 0.185 * H * b,
              hipY - ankleY, cloth, rings=12, sides=28)
        tube((0.0, chestY + 0.020 * H, 0.0), (0.0, neckY - 0.006 * H, 0.0), 0.070 * H * b, cloth, seg=3, name="bodice")
        block((0.0, neckY - 0.012 * H, 0.0), (0.10 * H, 0.012 * H, 0.10 * H), accent, name="collar")
    else:
        for side, sign in (("l", -1.0), ("r", 1.0)):
            tube((sign * hipX, hipY - 0.020 * H, 0.0), (sign * hipX, kneeY + 0.014 * H, 0.0),
                 0.062 * H * b, cloth, seg=5, name="trouser_" + side)
            if kind == "child":
                continue
            tube((sign * hipX, kneeY + 0.022 * H, 0.0), (sign * hipX, ankleY + 0.12 * H, 0.0),
                 0.050 * H * b, cloth, seg=5, name="legLowCloth_" + side)

    return [skin, hair, cloth, accent, leather, lips, eye, iris]


# ----------------------------------------------------------------- animation

def key(joint, frame, rx=0.0, ry=0.0, rz=0.0, tx=None, ty=None, tz=None):
    cmds.setAttr(joint + ".rotateX", rx)
    cmds.setAttr(joint + ".rotateY", ry)
    cmds.setAttr(joint + ".rotateZ", rz)
    base = BIND.get(joint, (0.0, 0.0, 0.0))
    attrs = ["rotateX", "rotateY", "rotateZ"]
    if tx is not None:
        cmds.setAttr(joint + ".translateX", base[0] + tx)
        attrs.append("translateX")
    if ty is not None:
        cmds.setAttr(joint + ".translateY", base[1] + ty)
        attrs.append("translateY")
    if tz is not None:
        cmds.setAttr(joint + ".translateZ", base[2] + tz)
        attrs.append("translateZ")
    cmds.setKeyframe(joint, t=frame, attribute=attrs)


def clear_keys(rig):
    """Drop animation but keep every joint parked at its bind-pose translation."""
    for joint in rig.values():
        cmds.cutKey(joint, clear=True, time=(1, 400), attribute=("rotateX", "rotateY", "rotateZ",
                                                                "translateX", "translateY", "translateZ"))
        for attr in ("rotateX", "rotateY", "rotateZ"):
            cmds.setAttr(joint + "." + attr, 0)
        base = BIND.get(joint, (0.0, 0.0, 0.0))
        cmds.setAttr(joint + ".translateX", base[0])
        cmds.setAttr(joint + ".translateY", base[1])
        cmds.setAttr(joint + ".translateZ", base[2])


def pose(rig, frame, values):
    for joint, angles in values.items():
        if joint in rig:
            key(rig[joint], frame, *angles[:3], **({"ty": angles[3]} if len(angles) > 3 else {}))


def anim_idle(rig, p):
    frames = (1, 61)
    f = (1, 16, 31, 46, 61)
    chest = (1.2, 2.4, 1.2, 0.3, 1.2)
    spine = (0.6, 1.3, 0.6, 0.1, 0.6)
    hipsY = (0.0, -0.007, 0.0, 0.005, 0.0)
    hipsYaw = (-1.6, 0.0, 1.6, 0.0, -1.6)
    headYaw = (-3.5, 1.5, 4.5, -0.5, -3.5)
    headPitch = (1.0, -1.5, 0.5, 2.5, 1.0)
    armPitch = (1.0, -1.5, 1.0, -1.0, 1.0)
    elbow = (-7.0, -9.5, -7.0, -5.0, -7.0)
    for i in range(5):
        pose(rig, f[i], {
            "hips": (0.0, hipsYaw[i], 0.0, hipsY[i]),
            "spine": (spine[i], 0.0, 0.0),
            "chest": (chest[i], -hipsYaw[i] * 0.5, 0.0),
            "neck": (-chest[i] * 0.4, 0.0, 0.0),
            "head": (headPitch[i], headYaw[i], 0.0),
            "upperarm_l": (-armPitch[i], 0.0, -2.5),
            "upperarm_r": (-armPitch[i], 0.0, 2.5),
            "forearm_l": (elbow[i], 0.0, 0.0),
            "forearm_r": (elbow[i], 0.0, 0.0),
            "thigh_l": (0.0, 0.0, -0.6),
            "thigh_r": (0.0, 0.0, 0.6),
        })
    return frames


def anim_walk(rig, p):
    frames = (1, 25)
    f = (1, 7, 13, 19, 25)
    thighL = (22.0, 0.0, -22.0, 0.0, 22.0)
    thighR = (-22.0, 0.0, 22.0, 0.0, -22.0)
    shinL = (6.0, 12.0, 8.0, 55.0, 6.0)
    shinR = (8.0, 55.0, 6.0, 12.0, 8.0)
    footL = (-8.0, 4.0, 12.0, 2.0, -8.0)
    footR = (12.0, 2.0, -8.0, 4.0, 12.0)
    bob = (0.0, -0.020, 0.0, -0.020, 0.0)
    twist = (-5.0, 0.0, 5.0, 0.0, -5.0)
    armL = (-19.0, 0.0, 19.0, 0.0, -19.0)
    armR = (19.0, 0.0, -19.0, 0.0, 19.0)
    elbowL = (-26.0, -36.0, -26.0, -16.0, -26.0)
    elbowR = (-26.0, -16.0, -26.0, -36.0, -26.0)
    for i in range(5):
        pose(rig, f[i], {
            "hips": (0.0, twist[i], 0.0, bob[i]),
            "spine": (2.0, -twist[i] * 0.4, 0.0),
            "chest": (4.0, -twist[i] * 0.8, 0.0),
            "neck": (-2.0, twist[i] * 0.3, 0.0),
            "head": (1.0, twist[i] * 0.2, 0.0),
            "upperarm_l": (armL[i], 0.0, -3.0),
            "upperarm_r": (armR[i], 0.0, 3.0),
            "forearm_l": (elbowL[i], 0.0, 0.0),
            "forearm_r": (elbowR[i], 0.0, 0.0),
            "thigh_l": (-thighL[i], 0.0, -1.0),
            "shin_l": (shinL[i], 0.0, 0.0),
            "foot_l": (footL[i], 0.0, 0.0),
            "thigh_r": (-thighR[i], 0.0, 1.0),
            "shin_r": (shinR[i], 0.0, 0.0),
            "foot_r": (footR[i], 0.0, 0.0),
        })
    return frames


def anim_run(rig, p):
    frames = (1, 19)
    f = (1, 6, 10, 15, 19)
    thighL = (38.0, 4.0, -34.0, -6.0, 38.0)
    thighR = (-34.0, -6.0, 38.0, 4.0, -34.0)
    shinL = (12.0, 18.0, 14.0, 95.0, 12.0)
    shinR = (14.0, 95.0, 12.0, 18.0, 14.0)
    bob = (0.0, -0.045, 0.0, -0.045, 0.0)
    twist = (-8.0, 0.0, 8.0, 0.0, -8.0)
    armL = (-30.0, -4.0, 32.0, 4.0, -30.0)
    armR = (32.0, 4.0, -30.0, -4.0, 32.0)
    elbowL = (-72.0, -58.0, -72.0, -46.0, -72.0)
    elbowR = (-72.0, -46.0, -72.0, -58.0, -72.0)
    for i in range(5):
        pose(rig, f[i], {
            "hips": (0.0, twist[i], 0.0, bob[i]),
            "spine": (5.0, -twist[i] * 0.4, 0.0),
            "chest": (9.0, -twist[i] * 0.8, 0.0),
            "neck": (-5.0, twist[i] * 0.3, 0.0),
            "head": (-6.0, twist[i] * 0.2, 0.0),
            "upperarm_l": (armL[i], 0.0, -6.0),
            "upperarm_r": (armR[i], 0.0, 6.0),
            "forearm_l": (elbowL[i], 0.0, 0.0),
            "forearm_r": (elbowR[i], 0.0, 0.0),
            "thigh_l": (-thighL[i], 0.0, -1.5),
            "shin_l": (shinL[i], 0.0, 0.0),
            "foot_l": (-6.0, 0.0, 0.0),
            "thigh_r": (-thighR[i], 0.0, 1.5),
            "shin_r": (shinR[i], 0.0, 0.0),
            "foot_r": (-6.0, 0.0, 0.0),
        })
    return frames


def anim_attack(rig, p):
    frames = (1, 21)
    f = (1, 6, 9, 13, 21)
    armR = (0.0, -35.0, -52.0, 55.0, 0.0)
    elbowR = (-20.0, -70.0, -85.0, -14.0, -20.0)
    abdR = (0.0, 22.0, 34.0, 8.0, 0.0)
    armL = (0.0, 10.0, 16.0, -26.0, 0.0)
    elbowL = (-25.0, -32.0, -36.0, -52.0, -25.0)
    twist = (0.0, -18.0, -26.0, 22.0, 0.0)
    lean = (2.0, -4.0, -6.0, 9.0, 2.0)
    thighL = (5.0, 8.0, 10.0, -14.0, 5.0)
    thighR = (-5.0, -8.0, -10.0, 14.0, -5.0)
    for i in range(5):
        pose(rig, f[i], {
            "hips": (0.0, twist[i] * 0.45, 0.0, -0.012 if i in (2, 3) else 0.0),
            "spine": (lean[i] * 0.5, twist[i] * 0.3, 0.0),
            "chest": (lean[i], twist[i], 0.0),
            "neck": (-lean[i] * 0.5, -twist[i] * 0.25, 0.0),
            "head": (-2.0, -twist[i] * 0.2, 0.0),
            "upperarm_l": (armL[i], 0.0, -8.0),
            "upperarm_r": (armR[i], 0.0, abdR[i]),
            "forearm_l": (elbowL[i], 0.0, 0.0),
            "forearm_r": (elbowR[i], 0.0, 0.0),
            "thigh_l": (-thighL[i], 0.0, -1.0),
            "shin_l": (abs(thighL[i]) * 0.4, 0.0, 0.0),
            "thigh_r": (-thighR[i], 0.0, 1.0),
            "shin_r": (abs(thighR[i]) * 0.4, 0.0, 0.0),
        })
    return frames


def anim_hurt(rig, p):
    frames = (1, 15)
    f = (1, 4, 8, 15)
    lean = (0.0, -16.0, -8.0, 0.0)
    head = (0.0, -18.0, -9.0, 0.0)
    drop = (0.0, -0.030, -0.015, 0.0)
    arm = (0.0, 26.0, 12.0, 0.0)
    abd = (0.0, 16.0, 8.0, 0.0)
    for i in range(4):
        pose(rig, f[i], {
            "hips": (0.0, 0.0, 0.0, drop[i]),
            "spine": (lean[i] * 0.5, 0.0, 0.0),
            "chest": (lean[i], 0.0, 0.0),
            "neck": (lean[i] * 0.3, 0.0, 0.0),
            "head": (head[i], 6.0 if i == 1 else 0.0, 0.0),
            "upperarm_l": (arm[i], 0.0, -8.0 - abd[i]),
            "upperarm_r": (arm[i], 0.0, 8.0 + abd[i]),
            "forearm_l": (-30.0 - arm[i], 0.0, 0.0),
            "forearm_r": (-30.0 - arm[i], 0.0, 0.0),
            "thigh_l": (-6.0 if i in (1, 2) else 0.0, 0.0, -1.0),
            "shin_l": (12.0 if i in (1, 2) else 0.0, 0.0, 0.0),
            "thigh_r": (-6.0 if i in (1, 2) else 0.0, 0.0, 1.0),
            "shin_r": (12.0 if i in (1, 2) else 0.0, 0.0, 0.0),
        })
    return frames


def anim_die(rig, p):
    frames = (1, 41)
    hipY = 0.527 * p["height"]
    drop = hipY - 0.155
    f = (1, 8, 16, 26, 34, 41)
    hipsPitch = (0.0, -12.0, -36.0, -66.0, -78.0, -80.0)
    hipsDrop = (0.0, -0.10, -0.34, -0.66, -drop, -drop)
    chest = (0.0, -8.0, -12.0, -6.0, 4.0, 6.0)
    head = (0.0, -10.0, -18.0, -12.0, -6.0, -4.0)
    thighL = (0.0, -16.0, -26.0, -12.0, -2.0, 0.0)
    shinL = (0.0, 26.0, 46.0, 22.0, 8.0, 6.0)
    thighR = (0.0, -10.0, -20.0, -16.0, -6.0, -4.0)
    shinR = (0.0, 18.0, 38.0, 28.0, 12.0, 10.0)
    armL = (0.0, 20.0, 34.0, 20.0, 6.0, 2.0)
    abdL = (0.0, -22.0, -38.0, -28.0, -12.0, -10.0)
    for i in range(6):
        pose(rig, f[i], {
            "hips": (hipsPitch[i], 0.0, 0.0, hipsDrop[i]),
            "spine": (chest[i] * 0.4, 0.0, 0.0),
            "chest": (chest[i], 0.0, 0.0),
            "neck": (head[i] * 0.3, 0.0, 0.0),
            "head": (head[i], 0.0, 0.0),
            "upperarm_l": (armL[i], 0.0, abdL[i]),
            "upperarm_r": (armL[i] * 0.8, 0.0, -abdL[i] * 0.9),
            "forearm_l": (-24.0 - armL[i] * 0.4, 0.0, 0.0),
            "forearm_r": (-20.0 - armL[i] * 0.3, 0.0, 0.0),
            "thigh_l": (thighL[i], 0.0, -2.0),
            "shin_l": (shinL[i], 0.0, 0.0),
            "foot_l": (10.0 if i > 1 else 0.0, 0.0, 0.0),
            "thigh_r": (thighR[i], 0.0, 2.0),
            "shin_r": (shinR[i], 0.0, 0.0),
            "foot_r": (10.0 if i > 1 else 0.0, 0.0, 0.0),
        })
    return frames


ANIMS = {"idle": anim_idle, "walk": anim_walk, "run": anim_run,
         "attack": anim_attack, "hurt": anim_hurt, "die": anim_die}


def export(kind, clip, objects, start, end):
    """Export the mesh list plus the armature root as one FBX clip.

    The objects are exported by selection rather than through a group node: a
    group makes the FBX writer fold the joint translations into a geometric
    pivot, which Unity then loses, and every bone rotates about the origin.
    """
    path = os.path.join(OUT, "%s_%s.fbx" % (kind, clip))
    cmds.playbackOptions(min=start, max=end, ast=start, aet=end)
    cmds.currentTime(start)
    cmds.select(clear=True)
    cmds.select(objects, r=True)
    mel.eval('FBXExportSkins -v true')
    mel.eval('FBXExportShapes -v false')
    mel.eval('FBXExportCameras -v false')
    mel.eval('FBXExportLights -v false')
    mel.eval('FBXExportTangents -v false')
    mel.eval('FBXExportSmoothMesh -v false')
    mel.eval('FBXExportInputConnections -v false')
    mel.eval('FBXExportApplyConstantKeyReducer -v false')
    mel.eval('FBXExportBakeComplexAnimation -v true')
    mel.eval('FBXExportBakeComplexStart -v %d' % start)
    mel.eval('FBXExportBakeComplexEnd -v %d' % end)
    mel.eval('FBXExportBakeComplexStep -v 1')
    mel.eval('FBXExportUpAxis "y"')
    mel.eval('FBXExport -f "%s" -s' % path.replace("\\", "/"))
    print("EXPORTED %s (%d-%d)" % (path, start, end))


def build_character(kind, clips):
    p = CHARS[kind]
    for clip in clips:
        cmds.file(new=True, force=True)
        cmds.currentUnit(linear="m", time="film")
        MESHES[:] = []
        SHADERS.clear()

        table, anchors = joints_for(p)
        rig = build_rig(table)
        body(kind, p, anchors)

        influences = list(rig.values())
        for mesh in MESHES:
            cmds.skinCluster(*(influences + [mesh]), toSelectedBones=True, maximumInfluences=3,
                             obeyMaxInfluences=True, dropoffRate=4.0, normalizeWeights=1)

        clear_keys(rig)
        start, end = ANIMS[clip](rig, p)
        export(kind, clip, list(MESHES) + [rig["root"]], start, end)
        print("BUILT %s %s" % (kind, clip))


def main():
    import sys
    argv = [a.lower() for a in sys.argv[1:]]
    kinds = [a for a in argv if a in CHARS] or ["man", "woman", "child"]
    clips = [a for a in argv if a in CLIPS] or list(CLIPS)
    if not os.path.isdir(OUT):
        os.makedirs(OUT)
    cmds.loadPlugin("fbxmaya", quiet=True)
    for kind in kinds:
        build_character(kind, clips)
    print("DONE")


if __name__ == "__main__":
    main()
