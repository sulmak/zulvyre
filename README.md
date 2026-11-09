# Zulvyre — Unity Edition

A real-time 3D open world — human village, dark forest, orc and vampire settlements, a sunken crypt — with animated
humanoid characters and a 1v1 duel battle, packaged as a runnable Unity 6 project and Windows player.

Powered by **Md. Atikur Rahman Mridha**.

Version history: see [CHANGELOG.md](CHANGELOG.md). Current release: 1.4.0 (2026-10-29).

## Editions in this repository

- **Unity edition** — this folder. Open it with Unity 6 (6000.6.2f1), or run the prebuilt `Build/Zulvyre/Zulvyre.exe`.
- **Web edition** — [`web/`](web): *Alderwick, a living village*, a canvas village you can play in a browser with
  `cd web && node server.js`. It is the earlier hand-drawn build that the 3D edition grew out of.

## Play now (Windows)

Run `Build/Zulvyre/Zulvyre.exe` — no install required.

- Sign in with a local account (or **Play as Guest**). The camp opens with a left-side menu: **Continue Adventure**,
  **Quick Duel**, **Change Adventurer** and **Log out**. Your hero is remembered per account, so there is no hero
  picker on every run.
- World: start in Alderwick, speak with Elder Maren, then follow the quest chain — the Greywood wolves, Durnmar the
  orc village, Nocturne the vampire village, the Sunken Crypt and The Undying Warden. Press **M** for the world map.
- Level 1 starts as *Wanderer*; the chain of 17 titles ends at *Undying Supreme Lord* on level 100.
- Duel: a 3D night arena fought by the same humanoid cast. First to yield wins; the clock runs 90 seconds and higher
  health wins on time.
- **Change Adventurer** opens the hero menu if you want Rowan, Ilsa or Corvin; its return link and every pause panel
  lead back to the camp.

## Controls

| Input | Action |
| --- | --- |
| WASD / Arrows | Move (camera-relative) |
| Shift (hold) | Run |
| Right mouse (hold) | Orbit the third-person camera |
| Mouse wheel | Zoom the camera (3.6–11 m) |
| E | Interact / talk / challenge / advance dialogue |
| Space | Light strike (3-hit combo) |
| F | Heavy strike (guard break) |
| Q | Hero art |
| M | World map |
| Shift (duel) | Dodge |
| Esc | Pause |

Shortcuts on the menu: **M** mute, **Alt+Enter** fullscreen. On the camp: **Enter** adventure, **Q** duel,
**C** adventurer, **Esc** log out.

## The cast

Three Maya-authored humanoid bodies — **man** (1.78 m), **woman** (1.66 m) and **child** (1.18 m) — each rigged to a
23-joint skeleton and animated with six clips: idle, walk, run, attack, hurt and die. The player, Elder Maren, the
ten villagers of Alderwick (Hanna, Bram, Sable, Orin, Pip, Tess, Mira, Tomas, Wren, Nell) and both duelists are drawn
from those bodies and tinted per character, so the village moves, works and reacts instead of standing still.

## Open in Unity

1. Install Unity **6000.6.2f1** (Unity 6) with Windows Build Support.
2. Open this folder with Unity Hub.
3. Rebuild the player with menu **Zulvyre > Build Windows Player**, or in batch:

```
"C:\Program Files\Unity\Hub\Editor\6000.6.2f1\Editor\Unity.exe" -batchmode ^
  -projectPath "<path>\ZulvyreUnity" ^
  -executeMethod Zulvyre.EditorTools.BuildGame.BuildWindows -quit ^
  -logFile "<path>\ZulvyreUnity\Logs\build.log"
```

The build lands in `Build/Zulvyre/Zulvyre.exe`.

## Regenerate the characters (Autodesk Maya)

The humanoids are procedural — no Maya session required:

```
"C:\Program Files\Autodesk\Maya2022\bin\mayapy.exe" Tools\maya\build_humans.py
```

Optionally pass filters to rebuild a subset, e.g. `build_humans.py man idle walk`. Each run models, rigs, skins and
animates the cast, then writes 18 FBX clips into `Assets/Resources/Art3D/Humans`. `Assets/Editor/HumanModelImport.cs`
forces the Legacy animation type on import.

## Project layout

- `Assets/Scripts/Core` — game bootstrap, theming, FX, audio, hero roster, capture tool.
- `Assets/Scripts/Menu` — login page, camp landing page (left-side menu) and the hero-select menu.
- `Assets/Scripts/World` — 3D open world, regions, quests, animated player, enemies, villagers, humanoid animator.
- `Assets/Scripts/Village` — legacy 2D village scene, dialogue and quests.
- `Assets/Scripts/Duel` — duel scene, fighter controller, rival AI.
- `Assets/Resources/Art3D` — Maya-exported world props and the 18 humanoid FBX clips.
- `Assets/StreamingAssets` — runtime-loaded background and character art.
- `Assets/Editor` — Windows build automation, humanoid import rule, gallery and probe diagnostics.
- `Tools/maya` — procedural character pipeline (`build_humans.py`, `probe_rig.py`).
- `Shots` — automated menu / duel / world / humanoid screenshots.

The game runs on IMGUI only (no UGUI package), using the legacy input manager.

## Automated screenshot tours

```
Zulvyre.exe -tour "<folder>" -finish        " menu, village, duel intro, fight, clash, result
Zulvyre.exe -world -tour "<folder>"         " nine world shots: Alderwick → Greywood → combat → quest
                                             "   → Durnmar → Nocturne → crypt → map → dungeon
powershell -File Tools\testHumanoids.ps1     " real keyboard input: idle, walk, run, strike, village
```

Both Unity tours capture and exit on their own.
