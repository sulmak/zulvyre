# Changelog

All notable changes to **Zulvyre — Unity Edition** are documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versioning follows [Semantic Versioning](https://semver.org/).

## [1.4.0] — 2026-10-29

The entry flow becomes an account-driven camp, the quick duel moves into the same 3D world as the campaign, and the
over-world camera drops to a close third-person view over a denser village and forest.

### Added

- **Local account ledger** — `AccountStore` keeps named accounts in `accounts.json` next to the save folder with salted
  SHA-256 hashes. The login page offers **Log in** / **Create account** tabs, remembers the last name, and also allows
  a guest visit.
- **Camp landing page with a left-side menu** — after signing in the game opens a sidebar shell: ZULVYRE brand,
  identity card (name, signed-in/guest state, chosen hero), **Continue Adventure**, **Quick Duel**,
  **Change Adventurer** and **Log out**, with the hero dossier and ledger on the right.
- **Per-account hero memory** — the chosen adventurer is stored on the account (`HeroSet` / `Hero`), restored on login
  and again whenever a hero card is selected, so the game no longer asks for a hero on every run.
- **3D night arena for Quick Duel** — the duel is now fought by the same animated Maya humanoids on the stone circle
  with pillars, lanterns, fireflies and a perspective camera; attacks, hurts and deaths play through `HumanAnimator`.

### Changed

- **Menu is optional** — the main menu is reached through **Change Adventurer** (or its return link) instead of
  standing between login and play; `Enter`/`Space` on the camp starts the open world immediately.
- **Third-person camera** — the world camera now sits 6.6 m behind and 15° above the hero (FOV 50) instead of the old
  high follow view; hold the right mouse button to orbit and use the wheel to zoom (3.6–11 m), with sphere-cast
  occlusion pulling the camera in front of trees and houses.
- **Denser Alderwick** — an outer ring of houses, a longhouse, a second watchtower and a south gate frame the green;
  extra lanterns, fences, benches, barrels, a cart and planters dress the square, and the village now has ten
  wandering villagers (Hanna, Bram, Sable, Orin, Pip, Tess, Mira, Tomas, Wren and Nell).
- **Denser Greywood** — 250 trees, 150 saplings/shrubs and 36 boulders with a clear road corridor, so the forest
  reads as woodland instead of a lawn.
- **Exits lead back to camp** — the world pause/death panels and the duel result/pause panels now read
  **Return to Camp** and open the landing page instead of dropping the player into hero selection.

### Fixed

- **Mouse clicks on the leave prompt** — the three hero cards are one big invisible button each, and two of them sit
  under the **Leave Zulvyre?** dialog, so the card swallowed the press and **Stay** / **Quit to Desktop** ignored the
  mouse. The cards, the row of buttons and the return link stop taking clicks while the prompt is open.

## [1.2.0] — 2026-10-27

The project is renamed **Zulvyre** (previously Wyrmrise). Same world, same cast, same credit — new name everywhere.

### Changed

- **Product name** — menu title, quit prompt and world-map header now read ZULVYRE; `PlayerSettings.productName`,
  the Windows executable and the build folder become `Zulvyre.exe` in `Build/Zulvyre`. Company credit
  "Md. Atikur Rahman Mridha" is unchanged.
- **Code identifiers** — the root namespace is `Zulvyre` (was `Wyrmrise`), the bootstrap is `ZulvyreGame.cs`, editor
  menus live under **Zulvyre >**, the batch entry point is `Zulvyre.EditorTools.BuildGame.BuildWindows`, the
  PlayerPrefs hero key is `zulvyre.hero`, and the project folder is `ZulvyreUnity`.
- **Tooling and docs** — packaging, tour and probe scripts, the Maya builders and both markdown files point at the
  Zulvyre paths and output `Zulvyre-Unity.zip`. Version label moved to v1.2.
- Historical 1.1.0 and 1.0.0 entries were relabelled to the new name rather than rewritten; the rename itself is
  recorded only here.

## [1.1.0] — 2026-10-27

The village becomes a world: a full 3D open map with four regions, a quest chain that runs from level 1 to the
undying endgame, and Maya-authored humanoid characters that walk, run, strike, reel and die on a real skeleton.
Powered by Md. Atikur Rahman Mridha.

### Added

- **3D open world** — replaces the painted village backdrop: ground, roads, pond, hills and a dusk sky explored with
  a third-person follow camera (mouse-free orbit, WASD movement, Shift run, Alt+Enter fullscreen).
- **Four regions plus a dungeon** — Alderwick (human village, elder, six villagers, well, gate, fences), the Greywood
  forest (pines, oaks, rocks, wolves), Durnmar the orc village (longhouses, watchtower, war fire, Warchief Grok),
  Nocturne the vampire village (spires, crypt gate) and the Sunken Crypt with a full dungeon interior — pillars,
  rubble, the Warden seal and the exit portal.
- **Quest chain** — Ash on the Wind → The Durnmar Threat → Nocturne → the Sunken Crypt → The Undying Warden, with an
  on-screen tracker, objective text, region banners, toasts and a world map (M) that unlocks as stages clear.
- **Level and title progression** — XP per kill, 17 titles from *Wanderer* at level 1 to *Undying Supreme Lord* at
  level 100, with health, stamina and XP bars plus a level-up readout in the HUD.
- **Maya humanoid pipeline** — `Tools/maya/build_humans.py` procedurally models, rigs, skins and animates a man,
  a woman and a child in Autodesk Maya 2022 (mayapy, no GUI needed) and exports 18 FBX clips
  (`{man,woman,child}` × `idle, walk, run, attack, hurt, die`) straight into `Assets/Resources/Art3D/Humans`.
- **Animated cast in the world** — the player, Elder Maren and all six villagers are now skinned humanoids
  (Hanna, Bram, Sable, Orin, Pip and Tess draw from the woman / man / child bodies). Each hero picks a body:
  Ilsa walks as the woman, Rowan and Corvin as the man.
- **Runtime animation state machine** — `HumanAnimator` cross-fades between idle, walk and run from measured planar
  speed, plays attack and hurt as one-shots at scaled rates, and holds the death pose with `ClampForever`.
- **Legacy import rule** — `Assets/Editor/HumanModelImport.cs` forces every `Art3D/Humans` FBX to the Legacy
  animation type, and `WorldModels.SpawnHuman` stitches the six sibling clips onto one skeleton at spawn time and
  grounds the figure on its own bounds.
- **Editor diagnostics** — `Zulvyre > Render Human Gallery` and `Zulvyre > Probe Humanoid` render posed PNGs of
  every character and dump the imported hierarchy, bind poses and clip table without launching the player.
- **Real-input test harness** — `Tools/testHumanoids.ps1` launches the built player, drives the keyboard through
  idle, walk, run and strike, and captures five screen shots.

### Changed

- The world HUD now shows the quest panel, region title and level line instead of the single-village tracker.
- `WorldModels` gained a humanoid path with per-body tinting (`cloth`, `accent`, `hair`, `hood`), so one rig serves
  every villager, elder and hero in their own colours.
- Villagers were raised from four to six and given named bodies, so Alderwick reads as a living settlement.
- The dungeon gained a warden seal, extra pillars and scattered rubble for depth past the entrance.

### Fixed

- Humanoids sank about 0.9 m through the ground: the Maya export folded the hips offset into the model root, so the
  figure now hangs off a grounded pivot that `SpawnHuman` measures and corrects at spawn.
- Animated characters tore apart into a spike when any clip played. `clear_keys` in the Maya script zeroed every
  joint's translate, which flattened the skeleton to the origin while the skin bind poses still expected real joint
  heights. Joints are now parked at their bind-pose translation and animation keys are authored as offsets from rest.
- The exported FBX root carried a phantom `(0, 0.906, 0.026)` offset from grouping skinned meshes; the exporter now
  selects the meshes and the armature root directly, so the pivot sits at the feet.
- Bare skin showed through the waist and shoulders, and the woman's bust rendered in skin tone over her dress. The
  torso, tunic, pauldrons and trousers were re-proportioned to close every gap.
- Batch gallery renders came out blank because `-nographics` was passed to Unity; the editor tools now render with a
  live graphics device.

### Known notes

- The 18 humanoid FBXs (~34 MB) ship inside `Resources`, so the Windows build and the distribution zip are larger.
- The world window opens at 1600×900; on a 900 px-tall display the bottom strip sits under the taskbar — use
  **Alt+Enter** for fullscreen.
- IMGUI-only rendering (no UGUI package) and the legacy input manager remain intentional.

## [1.0.0] — 2026-10-27

First playable Unity release: a real-time village adventure with a 1v1 duel.
Powered by Md. Atikur Rahman Mridha.

### Added

- **Hero select menu** — Rowan the Wayfarer, Ilsa the Shieldbearer, and Corvin the Emberkith, each with portrait art, role, lore, blade/guard/spirit pips, and art blurb. Begin the Adventure, Quick Duel, and Quit.
- **Village of Alderwick** — camera-follow exploration over a painted 1792×1024 backdrop, run/walk movement, Isla and Vesper as visible NPCs, proximity interaction prompt, toast messages, quest tracker, pause menu, and footer controls.
- **Dialogue system** — portrait panel, per-NPC name/title, typewriter reveal, E to reveal/advance, multi-line conversations, and quest flag updates for Elder Maren and the wayfarer.
- **The Lantern Duel** — 90-second 1v1 arena with intro countdown, FIGHT banner, mirrored HP/stamina HUD with damage lag, combo counter, ability panel with cooldown, duel timer, and victory / defeat / draw result screen with damage dealt, best combo, time remaining, rematch, return to village, and main menu.
- **Fighter kit** — light 3-hit combo, heavy guard-breaking strike, dodge with invulnerability frames, stamina/breath economy, knockback, hit flash, and shield, burn, plus speed-buff status effects.
- **Three hero arts** — Wayfarer's Grace (heal + swiftness), Bulwark Oath (damage halved, then shield bash), Emberlash (homing cinder bolt with lingering burn).
- **Vesper AI** — approach/keep-spacing/strafe movement, reactive dodges against attacks and heavy wind-ups, melee bursts, low-stamina retreats, and Duskstep casts.
- **Presentation** — painted duel arena with lantern glows, honour ring and rune sigil, fireflies, procedural sparks, rings, slash arcs, afterimages, damage numbers, screen shake, hitstop, vignette, and dark themed IMGUI.
- **Audio** — procedural hit, heavy, shield, cast, victory, defeat, and click sounds with mute toggle.
- **Art pipeline** — backgrounds and character sheets loaded at runtime from `StreamingAssets`, with grayscale-to-tint palette support.
- **Windows build automation** — `Zulvyre > Build Windows Player` editor menu and a batch-mode entry point producing a 1600×900 windowed x64 player with product metadata.
- **Screenshot tour** — `-tour <dir> [-finish]` CLI mode that walks menu → village → duel, auto-pilots a fight, optionally forces a victory, captures six screenshots, and exits.
- **README** with controls, hero summary, project layout, and rebuild instructions.

### Changed

- Duel camera set to orthographic 4.8 at `(0, -0.3)` with a 5.0 → 4.8 intro zoom, so both fighters and the full honour ring stay in frame.
- Slash VFX rebuilt at a smaller base scale (1.5 × 1.16 world units) with softer alpha so strikes read as arcs instead of a full-screen flash.
- Ability panel compacted to 88 px with the cooldown bar inside the panel and the duplicate controls line removed.
- Village interaction prompt moved from mid-screen to the bottom centre so it no longer covers the player and Vesper at the dock.
- Menu ability blurbs shortened so all three cards fit on one line.
- UI text uses the shared `ImGuiTheme` styles, including public `StyleFor` for runtime-styled labels.

### Fixed

- Oversized translucent slash blob covering the duel arena.
- Duel sigil and honour ring washing out against the warm ground — now backed by a dark shadow underlay with brighter, pulsing rune alpha.
- "DUSKSTEP" being announced twice when Vesper cast his art.
- Result screen reporting inflated damage from the automated test kill; stats now reflect real combat only.
- Automated result screenshot occasionally missing when the rival was inside dodge invulnerability frames; test hook now bypasses i-frames.
- Projectile list modification during iteration when a bolt's impact killed a fighter.
- Menu card blurb clipping and ability panel text clipping.
- Compile blockers: inaccessible `ImGuiTheme.StyleFor` and the obsolete `FindFirstObjectByType` call.

### Known notes

- The window opens at 1600×900; on a 900 px-tall display the bottom strip sits under the taskbar — use **Alt+Enter** for fullscreen.
- IMGUI-only rendering (no UGUI package) and the legacy input manager are intentional; rebuilding in the editor needs Unity 6000.6.2f1 with Windows Build Support.
