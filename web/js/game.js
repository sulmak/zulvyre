// Alderwick — the living simulation and adventure systems.
import {
  WORLD, LOCATIONS, NPCS, QUESTS, STALLS, GATHER_NODES, FISH_SPOTS, QUEST_OBJECTS,
  INTERACTABLES, ANIMAL_SITES, WOLF_SITES, SPECIES, ITEMS, SELLERS, HEROES, FACTIONS, PLUS
} from './data.js';
import { mulberry32 } from './world.js';

const TAU = Math.PI * 2;
const clamp = (v, a, b) => Math.max(a, Math.min(b, v));
const hashStr = (s) => {
  let h = 2166136261;
  for (let i = 0; i < s.length; i++) { h ^= s.charCodeAt(i); h = Math.imul(h, 16777619); }
  return h >>> 0;
};

export function formatTime(minutes) {
  const m = ((Math.floor(minutes) % 1440) + 1440) % 1440;
  const h = Math.floor(m / 60);
  const mm = m % 60;
  return `${String(h).padStart(2, '0')}:${String(mm).padStart(2, '0')}`;
}

const HOME_ANCHORS = {
  maren: { x: 1595, y: 1042 },
  ospren: { x: 1708, y: 1160 },
  liora: { x: 1553, y: 1230 },
  hesta: { x: 826, y: 930 },
  bran: { x: 584, y: 1268 },
  tam: { x: 598, y: 1258 },
  nell: { x: 1216, y: 952 },
  ivor: { x: 900, y: 534 },
  ysolde: { x: 1082, y: 558 },
  kestrel: { x: 524, y: 936 },
  dagna: { x: 1464, y: 952 },
  osric: { x: 1494, y: 1322 },
  wren: { x: 1386, y: 1230 }
};

const GATHER_LABELS = { herb: 'Wild Herb', mushroom: 'Mooncap', firewood: 'Firewood Bundle' };
const QUEST_TARGET_NAMES = {
  lostNet: 'The lost gillnet',
  track1: 'Wolf tracks at the stones', track2: 'Wolf tracks at the fallen stone', track3: 'Wolf tracks at the thorn tree',
  page1: 'The ledger page', page2: 'The ledger page', page3: 'The ledger page'
};

export class Game {
  constructor(world) {
    this.world = world;
    this.ui = null;
    this.started = false;
    this.preview = true;
    this.previewTime = 0;
    this.speed = 1;
    this.uiPaused = false;
    this.minutes = WORLD.startMinutes;
    this.day = 1;
    this.effects = [];
    this.keys = {};
    this.movementPath = [];
    this.target = null;
    this.hudAccumulator = 0;
    this.promptAccumulator = 0;
    this.realTime = 0;
    this.trackedQuest = null;
    this.questToastShown = new Set();
    this.collectedNodes = new Set();
    this.wolves = [];
    this.wolvesSpawned = false;
    this.wolfWarned = false;
    this.stats = { gathered: 0, fish: 0, wolves: 0, quests: 0, distance: 0 };
    this.regard = { riverfolk: 0, hearthfolk: 0, wardens: 0, emberwrights: 0 };
    this.hero = null;
    this.player = null;
    this.initQuestState();
    this.npcs = [];
    this.animals = [];
    this.createNpcs();
    this.createAnimals();
    this.bindInput();
  }

  bindUI(ui) { this.ui = ui; }

  initQuestState() {
    this.quests = {};
    for (const q of Object.values(QUESTS)) {
      this.quests[q.id] = {
        state: q.auto ? 'active' : 'available',
        tasks: q.tasks.map(() => false),
        accepted: !!q.auto
      };
    }
  }

  /* ================= character ================= */
  start(heroId) {
    if (this.started) return;
    this.hero = HEROES.find(h => h.id === heroId) || HEROES[0];
    const look = { ...this.hero.look };
    const inventory = {};
    for (const [id, qty] of Object.entries(this.hero.startingItems || {})) {
      if (qty > 0) inventory[id] = qty;
    }
    this.player = {
      x: WORLD.startX,
      y: WORLD.startY,
      facing: -Math.PI / 2,
      hp: this.hero.maxHp,
      stamina: this.hero.maxStamina,
      level: 1,
      xp: 0,
      xpNeed: PLUS.LEVEL_XP(1),
      gold: PLUS.START_GOLD,
      inventory,
      heroId: this.hero.id,
      look,
      moving: false,
      step: 0,
      sprinting: false,
      attackTimer: 0,
      attackCooldown: 0,
      hitFlash: 0,
      invuln: 0,
      down: false,
      downTimer: 0
    };
    this.cooldowns = { ability: 0 };
    this.keenTimer = 0;
    this.wardTimer = 0;
    this.started = true;
    this.preview = false;
    this.speed = 1;
    this.movementPath = [];
    this.collectedNodes.clear();
    this.minutes = WORLD.startMinutes;
    this.regrowNodes();
    this.createNpcs();
    this.createAnimals();
    this.wolves = [];
    this.wolvesSpawned = false;
    this.syncTimeEvents(true);
    this.updateInteraction();
    this.ui?.refreshAll();
    this.ui?.toast('You pass the south gate. Alderwick smells of bread and river mud.', 'good');
  }

  get maxStamina() {
    let base = this.hero ? this.hero.maxStamina : 100;
    if (this.player?.inventory.wardenCharm) base += 15;
    return base;
  }

  get speedMultiplier() {
    if (!this.player) return 1;
    let m = 1;
    if (this.player.heroId === 'rowan') m *= 1.08;
    if (this.player.inventory.boots) m *= 1.12;
    return m;
  }

  addItem(id, qty = 1) {
    if (!ITEMS[id]) return;
    this.player.inventory[id] = (this.player.inventory[id] || 0) + qty;
  }
  removeItem(id, qty = 1) {
    if (!this.player.inventory[id]) return false;
    this.player.inventory[id] -= qty;
    if (this.player.inventory[id] <= 0) delete this.player.inventory[id];
    return true;
  }
  hasItem(id, qty = 1) { return (this.player?.inventory[id] || 0) >= qty; }

  addGold(n) {
    if (!this.player) return;
    this.player.gold = Math.max(0, this.player.gold + n);
    this.ui?.refreshHud();
  }

  addXp(n) {
    if (!this.player) return;
    this.player.xp += n;
    while (this.player.xp >= this.player.xpNeed) {
      this.player.xp -= this.player.xpNeed;
      this.player.level += 1;
      this.player.xpNeed = PLUS.LEVEL_XP(this.player.level);
      this.player.hp = Math.min(this.hero.maxHp + (this.player.level - 1) * 8, this.player.hp + 22);
      this.player.stamina = this.maxStamina;
      this.effects.push({ type: 'level', x: this.player.x, y: this.player.y, life: 2.2, max: 2.2 });
      this.ui?.toast(`You have grown into your road. Level ${this.player.level}.`, 'good');
    }
  }
  get playerMaxHp() { return this.hero ? this.hero.maxHp + (this.player.level - 1) * 8 : 100; }

  /* ================= time ================= */
  phase() {
    const h = this.minutes / 60;
    if (h < 5.5) return 'Night';
    if (h < 7.2) return 'Dawn';
    if (h < 12) return 'Morning';
    if (h < 17) return 'Afternoon';
    if (h < 20.6) return 'Evening';
    return 'Night';
  }
  phaseWord() {
    const p = this.phase();
    return p === 'Dawn' ? 'Dawn' : p;
  }
  isNight() { return this.minutes >= 20.9 * 60 || this.minutes < 5.2 * 60; }
  isWolfTime() { return this.minutes >= 21 * 60 || this.minutes < 5 * 60; }

  clockInfo() {
    return {
      time: formatTime(this.minutes),
      day: this.day,
      phase: this.phase(),
      minutes: this.minutes,
      paused: this.speed === 0
    };
  }

  setSpeed(speed) {
    this.speed = speed;
    this.ui?.refreshHud();
  }

  setUiPaused(paused) {
    this.uiPaused = paused;
    if (paused) this.ui?.setPrompt(null);
  }

  updateClock(dt) {
    if (this.speed === 0) return;
    const before = Math.floor(this.minutes);
    this.minutes += dt * WORLD.minutesPerSecond * this.speed;
    while (this.minutes >= 1440) {
      this.minutes -= 1440;
      this.day += 1;
      this.wolvesSpawned = false;
      this.ui?.onNewDay?.(this.day);
    }
    if (Math.floor(this.minutes) !== before) this.syncTimeEvents();
  }

  syncTimeEvents(force = false) {
    const hour = Math.floor(this.minutes / 60);
    if (this._lastHour !== hour || force) {
      this._lastHour = hour;
      if (hour === 6) {
        this.regrowNodes();
        this.ui?.toast('Dawn light reaches the Fenn. The hedgerows are full again.', 'good');
      }
      if (hour === 19 && !this.uiPaused) {
        this.ui?.toast('Lamps are lit along the Green. The village settles in for the evening.');
      }
    }
    if (this.isWolfTime()) {
      if (!this.wolvesSpawned) {
        this.spawnWolves();
        this.wolvesSpawned = true;
      }
    } else if (this.wolves.length) {
      this.wolves = [];
    }
  }

  regrowNodes() {
    this.collectedNodes.clear();
    this._regrowDay = this.day;
  }

  /* ================= schedules ================= */
  resolveBlock(def, minutes) {
    const m = ((Math.floor(minutes) % 1440) + 1440) % 1440;
    let found = -1;
    for (let i = 0; i < def.schedule.length; i++) {
      const b = def.schedule[i];
      if (b.from < b.to ? (m >= b.from && m < b.to) : (m >= b.from || m < b.to)) {
        if (b.from < b.to || m >= b.from) { found = i; break; }
      }
    }
    if (found === -1) {
      // midnight-crossing block fallback
      for (let i = 0; i < def.schedule.length; i++) {
        const b = def.schedule[i];
        if (b.to < b.from && m < b.to) { found = i; break; }
      }
    }
    if (found === -1) found = def.schedule.length - 1;
    return { block: def.schedule[found], index: found };
  }

  nextBlock(def, minutes) {
    const { index } = this.resolveBlock(def, minutes);
    return def.schedule[(index + 1) % def.schedule.length];
  }

  placeName(def, block) {
    if (block.place === 'home') return def.homeName;
    return LOCATIONS[block.place]?.name || 'Alderwick';
  }

  npcView(def) {
    const runtime = this.npcs.find(n => n.id === def.id);
    const { block, index } = this.resolveBlock(def, this.minutes);
    const next = this.nextBlock(def, this.minutes);
    const hidden = runtime?.hidden;
    const traveling = runtime?.traveling;
    let statusLine;
    if (hidden) statusLine = block.state === 'sleep' ? `Asleep at ${def.homeName}` : `At ${def.homeName}`;
    else if (traveling) statusLine = `Walking to ${this.placeName(def, block)}`;
    else statusLine = block.activity;
    return {
      id: def.id,
      def,
      hidden,
      traveling,
      state: block.state,
      activity: block.activity,
      placeName: this.placeName(def, block),
      statusLine,
      currentIndex: index,
      next: {
        time: formatTime(next.from),
        activity: next.activity,
        placeName: this.placeName(def, next)
      },
      schedule: def.schedule.map((b, i) => ({
        from: formatTime(b.from),
        to: formatTime(b.to === 1440 ? 0 : b.to),
        activity: b.activity,
        place: b.from === 1440 ? '' : this.placeName(def, b),
        state: b.state,
        current: i === index
      }))
    };
  }

  /* ================= NPC simulation ================= */
  createNpcs() {
    this.npcs = NPCS.map(def => {
      const { block } = this.resolveBlock(def, this.minutes);
      let x, y, hidden = false;
      if (block.place === 'home') {
        const home = HOME_ANCHORS[def.id] || { x: LOCATIONS.green.x, y: LOCATIONS.green.y };
        const open = this.world.findNearbyOpen(home.x, home.y, 18, hashStr(def.id) % 999);
        x = open.x; y = open.y; hidden = true;
      } else {
        const spot = this.npcSpot(block.place, def.id, block.from);
        x = spot.x; y = spot.y;
      }
      return {
        id: def.id,
        def,
        x, y,
        facing: Math.PI / 2,
        hidden,
        path: [],
        target: null,
        anchor: { x, y },
        activity: block.activity,
        state: block.state,
        place: block.place,
        blockKey: `${block.from}:${block.place}`,
        traveling: false,
        destName: '',
        homeTarget: false,
        idleTimer: 2 + Math.random() * 6,
        walkPhase: Math.random() * TAU,
        walking: false
      };
    });
  }

  npcSpot(place, id, seedOffset = 0) {
    const loc = LOCATIONS[place] || LOCATIONS.green;
    const rng = mulberry32(hashStr(`${id}:${place}:${seedOffset}`));
    const angle = rng() * TAU;
    const r = loc.radius * (0.25 + rng() * 0.7);
    const spot = this.world.findNearbyOpen(loc.x + Math.cos(angle) * r, loc.y + Math.sin(angle) * r, 22, hashStr(id + place) % 999);
    return spot;
  }

  changeNpcBlock(runtime, block) {
    runtime.blockKey = `${block.from}:${block.place}`;
    runtime.activity = block.activity;
    runtime.state = block.state;
    runtime.place = block.place;
    runtime.path = [];
    runtime.walking = false;
    if (block.place === 'home') {
      runtime.homeTarget = true;
      runtime.traveling = true;
      runtime.destName = runtime.def.homeName;
      const home = HOME_ANCHORS[runtime.def.id];
      if (home) this.walkNpcTo(runtime, home.x, home.y);
    } else {
      runtime.homeTarget = false;
      if (runtime.hidden) {
        const home = HOME_ANCHORS[runtime.def.id];
        if (home) {
          const open = this.world.findNearbyOpen(home.x, home.y, 18, hashStr(runtime.id) % 999);
          runtime.x = open.x; runtime.y = open.y;
        }
        runtime.hidden = false;
      }
      const spot = this.npcSpot(block.place, runtime.def.id, block.from);
      runtime.anchor = { ...spot };
      runtime.traveling = true;
      runtime.destName = LOCATIONS[block.place]?.name || 'Alderwick';
      this.walkNpcTo(runtime, spot.x, spot.y);
    }
  }

  walkNpcTo(npc, x, y) {
    const path = this.world.pathfind(npc.x, npc.y, x, y);
    if (path.length) {
      npc.path = path;
      npc.target = { x, y };
    } else {
      npc.path = [];
      npc.target = { x, y };
      const d = Math.hypot(npc.x - x, npc.y - y);
      if (d < 24) { npc.traveling = false; if (npc.homeTarget) npc.hidden = true; }
    }
  }

  updateNpcs(dt) {
    for (const npc of this.npcs) {
      const { block } = this.resolveBlock(npc.def, this.minutes);
      const key = `${block.from}:${block.place}`;
      if (key !== npc.blockKey) this.changeNpcBlock(npc, block);
      if (npc.hidden) continue;
      if (npc.path.length) {
        const speed = npc.traveling ? 88 : 64;
        let remaining = speed * dt;
        while (remaining > 0 && npc.path.length) {
          const node = npc.path[0];
          const dx = node.x - npc.x, dy = node.y - npc.y;
          const d = Math.hypot(dx, dy);
          if (d < 2) { npc.path.shift(); continue; }
          const step = Math.min(remaining, d);
          npc.x += (dx / d) * step;
          npc.y += (dy / d) * step;
          npc.facing = Math.atan2(dy, dx);
          npc.walking = true;
          npc.walkPhase += dt * 10;
          remaining -= step;
          if (d <= step + 0.01) npc.path.shift();
        }
        if (!npc.path.length) {
          npc.walking = false;
          if (npc.homeTarget) { npc.hidden = true; npc.traveling = false; }
          else { npc.traveling = false; npc.anchor = { x: npc.x, y: npc.y }; }
        }
      } else {
        npc.walking = false;
        npc.idleTimer -= dt;
        if (npc.idleTimer <= 0) {
          npc.idleTimer = 5 + Math.random() * 8;
          const chance = npc.state === 'work' ? 0.35 : npc.state === 'social' || npc.state === 'leisure' ? 0.7 : 0.4;
          if (Math.random() < chance) {
            const loc = LOCATIONS[npc.place];
            if (loc) {
              const r = Math.max(12, loc.radius * 0.5);
              const a = Math.random() * TAU;
              const tx = clamp(npc.anchor.x + Math.cos(a) * r, 40, this.world.width - 40);
              const ty = clamp(npc.anchor.y + Math.sin(a) * r, 40, this.world.height - 40);
              if (!this.world.collides(tx, ty, 8) && this.world.lineClear(npc.x, npc.y, tx, ty, 7)) {
                npc.path = [{ x: tx, y: ty }];
              }
            }
          }
        }
      }
    }
  }

  resnapNpcs() {
    for (const npc of this.npcs) {
      const { block } = this.resolveBlock(npc.def, this.minutes);
      npc.blockKey = `${block.from}:${block.place}`;
      npc.activity = block.activity;
      npc.state = block.state;
      npc.place = block.place;
      npc.path = [];
      npc.traveling = false;
      npc.homeTarget = false;
      npc.walking = false;
      if (block.place === 'home') {
        const home = HOME_ANCHORS[npc.def.id];
        const open = home ? this.world.findNearbyOpen(home.x, home.y, 18, hashStr(npc.id) % 999) : { x: npc.x, y: npc.y };
        npc.x = open.x; npc.y = open.y;
        npc.hidden = true;
      } else {
        const spot = this.npcSpot(block.place, npc.def.id, block.from);
        npc.x = spot.x; npc.y = spot.y;
        npc.hidden = false;
        npc.anchor = { ...spot };
      }
    }
  }

  /* ================= animals ================= */
  createAnimals() {
    this.animals = ANIMAL_SITES.map(site => {
      const sp = SPECIES[site.species];
      const rng = mulberry32(hashStr(site.id));
      return {
        ...site,
        sp,
        x: site.x,
        y: site.y,
        homeX: site.x,
        homeY: site.y,
        facing: rng() * TAU,
        state: 'idle',
        timer: 1 + rng() * 4,
        target: null,
        phase: rng() * TAU,
        speed: sp.speed,
        fleeing: 0
      };
    });
  }

  updateAnimals(dt) {
    for (const a of this.animals) {
      a.phase += dt;
      if (a.sp.behavior === 'fly') {
        a.x = a.homeX + Math.sin(a.phase * 0.35 + a.facing) * a.roam;
        a.y = a.homeY + Math.cos(a.phase * 0.27 + a.facing) * a.roam * 0.4;
        continue;
      }
      if (!this.player) continue;
      const pd = Math.hypot(this.player.x - a.x, this.player.y - a.y);
      if (a.sp.flee > 0 && pd < a.sp.flee * 0.55 && this.started) {
        a.fleeing = 1.25;
        const ang = Math.atan2(a.y - this.player.y, a.x - this.player.x);
        a.target = {
          x: a.x + Math.cos(ang) * 90,
          y: a.y + Math.sin(ang) * 90
        };
        a.state = 'flee';
      }
      if (a.fleeing > 0) {
        a.fleeing -= dt;
        if (a.target) this.moveAnimal(a, dt, a.speed * 3.2);
        if (a.fleeing <= 0) { a.state = 'idle'; a.timer = 1.5; a.target = null; }
        continue;
      }
      a.timer -= dt;
      if (a.state === 'walk' && a.target) {
        const arrived = this.moveAnimal(a, dt, a.speed);
        if (arrived) { a.state = 'idle'; a.timer = 2 + Math.random() * 5; a.target = null; }
      } else if (a.timer <= 0) {
        a.timer = 2 + Math.random() * 5;
        if (Math.random() < (a.sp.behavior === 'still' ? 0.15 : 0.6)) {
          const ang = Math.random() * TAU;
          const r = Math.random() * a.roam;
          const tx = a.homeX + Math.cos(ang) * r;
          const ty = a.homeY + Math.sin(ang) * r;
          if (a.sp.behavior === 'swim' || a.sp.behavior === 'still') {
            const e = this.world.riverEdges(tx);
            if (ty > e.top + 8 && ty < e.bottom - 8) {
              a.target = { x: tx, y: clamp(ty, e.top + 12, e.bottom - 12) };
              a.state = 'walk';
            }
          } else if (!this.world.collides(tx, ty, 6)) {
            a.target = { x: tx, y: ty };
            a.state = 'walk';
          }
        }
      }
    }
  }

  moveAnimal(a, dt, speed) {
    if (!a.target) return true;
    const dx = a.target.x - a.x, dy = a.target.y - a.y;
    const d = Math.hypot(dx, dy);
    if (d < 2) return true;
    const step = Math.min(d, speed * dt);
    const nx = a.x + (dx / d) * step;
    const ny = a.y + (dy / d) * step;
    if (a.sp.behavior === 'swim' || a.sp.behavior === 'still') {
      const e = this.world.riverEdges(nx);
      a.x = nx;
      a.y = clamp(ny, e.top + 8, e.bottom - 8);
    } else {
      if (!this.world.collides(nx, ny, 5)) { a.x = nx; a.y = ny; }
      else return true;
    }
    a.facing = Math.atan2(dy, dx);
    return step >= d;
  }

  /* ================= wolves ================= */
  spawnWolves() {
    this.wolves = WOLF_SITES.map((s, i) => ({
      id: `wolf${i}-${this.day}`,
      x: s.x, y: s.y,
      homeX: s.x, homeY: s.y,
      hp: 65, maxHp: 65,
      facing: Math.random() * TAU,
      biteTimer: 0,
      wanderTimer: 2 + Math.random() * 3,
      state: 'wander',
      target: null,
      hitFlash: 0
    }));
    if (!this.wolfWarned) {
      this.wolfWarned = true;
      this.ui?.toast('A long howl rolls down from the Elderwood. Wolves are abroad tonight.', 'warn');
    }
  }

  updateWolves(dt) {
    if (!this.started) return;
    if (!this.isWolfTime()) { if (this.wolves.length) this.wolves = []; return; }
    if (!this.wolvesSpawned) { this.spawnWolves(); this.wolvesSpawned = true; }
    for (const w of this.wolves) {
      w.biteTimer = Math.max(0, w.biteTimer - dt);
      w.hitFlash = Math.max(0, w.hitFlash - dt * 3);
      const d = Math.hypot(this.player.x - w.x, this.player.y - w.y);
      const warded = this.wardTimer > 0;
      if (warded && d < 280) {
        const ang = Math.atan2(w.y - this.player.y, w.x - this.player.x);
        w.target = { x: w.x + Math.cos(ang) * 120, y: w.y + Math.sin(ang) * 120 };
        w.state = 'flee';
      } else if (d < 290 && !this.player.down) {
        w.state = 'chase';
        w.target = { x: this.player.x, y: this.player.y };
      } else if (w.state === 'chase') {
        w.state = 'wander';
        w.target = null;
      }
      if (w.state === 'chase') {
        if (d > 34) {
          const ang = Math.atan2(this.player.y - w.y, this.player.x - w.x);
          const step = 92 * dt;
          const nx = w.x + Math.cos(ang) * step;
          const ny = w.y + Math.sin(ang) * step;
          if (!this.world.collides(nx, ny, 11)) { w.x = nx; w.y = ny; }
          w.facing = ang;
        } else if (w.biteTimer <= 0) {
          w.biteTimer = 1.35;
          this.damagePlayer(9, w);
        }
      } else if (w.state === 'flee' && w.target) {
        const dx = w.target.x - w.x, dy = w.target.y - w.y;
        const dist = Math.hypot(dx, dy) || 1;
        const step = 110 * dt;
        const nx = w.x + (dx / dist) * step;
        const ny = w.y + (dy / dist) * step;
        if (!this.world.collides(nx, ny, 11)) { w.x = nx; w.y = ny; }
        w.facing = Math.atan2(dy, dx);
      } else {
        w.wanderTimer -= dt;
        if (w.wanderTimer <= 0 || !w.target) {
          w.wanderTimer = 3 + Math.random() * 4;
          const a = Math.random() * TAU;
          w.target = { x: clamp(w.homeX + Math.cos(a) * 110, 60, this.world.width - 60), y: clamp(w.homeY + Math.sin(a) * 110, 60, this.world.height - 60) };
        }
        const dx = w.target.x - w.x, dy = w.target.y - w.y;
        const dist = Math.hypot(dx, dy);
        if (dist > 4) {
          const step = 38 * dt;
          const nx = w.x + (dx / dist) * step;
          const ny = w.y + (dy / dist) * step;
          if (!this.world.collides(nx, ny, 11)) { w.x = nx; w.y = ny; }
          w.facing = Math.atan2(dy, dx);
        }
      }
    }
    this.wolves = this.wolves.filter(w => w.hp > 0);
  }

  damagePlayer(amount, source) {
    if (this.player.down || this.player.invuln > 0) return;
    let dmg = amount;
    if (this.hero.id === 'ilsa') dmg *= 0.8;
    this.player.hp = Math.max(0, this.player.hp - dmg);
    this.player.invuln = 0.55;
    this.player.hitFlash = 0.45;
    this.effects.push({ type: 'text', x: this.player.x, y: this.player.y - 28, text: `-${Math.round(dmg)}`, color: '#e08a75', life: 1.2, max: 1.2 });
    this.effects.push({ type: 'hit', x: this.player.x, y: this.player.y, life: .5, max: .5 });
    this.shake = 8;
    if (this.player.hp <= 0) this.knockOut();
    this.ui?.refreshHud();
  }

  knockOut() {
    this.player.down = true;
    this.player.downTimer = 2.2;
    this.ui?.fadeScreen(true);
    this.ui?.toast('The dark closes in. You wake later at the Bent Hook.', 'bad');
  }

  updateDown(dt) {
    this.player.downTimer -= dt;
    this.player.hitFlash = Math.max(0, this.player.hitFlash - dt);
    if (this.player.downTimer <= 0) {
      this.player.down = false;
      const target = 6 * 60 + 30;
      if (this.minutes > target) this.day += 1;
      this.minutes = target;
      this._lastHour = 6;
      this.player.x = 1200; this.player.y = 970;
      this.player.hp = Math.round(this.playerMaxHp * 0.6);
      this.player.stamina = this.maxStamina;
      this.wolves = [];
      this.wolvesSpawned = false;
      this.regrowNodes();
      this.resnapNpcs();
      this.ui?.fadeScreen(false);
      this.ui?.toast('Nell left bread and a candle. “The road is not finished with you yet.”', 'good');
      this.ui?.refreshAll();
    }
  }

  /* ================= player ================= */
  updatePlayer(dt) {
    const p = this.player;
    p.attackCooldown = Math.max(0, p.attackCooldown - dt);
    p.attackTimer = Math.max(0, p.attackTimer - dt);
    p.hitFlash = Math.max(0, p.hitFlash - dt);
    p.invuln = Math.max(0, p.invuln - dt);
    this.cooldowns.ability = Math.max(0, this.cooldowns.ability - dt);
    this.keenTimer = Math.max(0, this.keenTimer - dt);
    this.wardTimer = Math.max(0, this.wardTimer - dt);

    let dx = 0, dy = 0;
    if (this.keys.w || this.keys.arrowup) dy -= 1;
    if (this.keys.s || this.keys.arrowdown) dy += 1;
    if (this.keys.a || this.keys.arrowleft) dx -= 1;
    if (this.keys.d || this.keys.arrowright) dx += 1;
    if (dx || dy) {
      this.movementPath = [];
      const len = Math.hypot(dx, dy);
      dx /= len; dy /= len;
      p.facing = Math.atan2(dy, dx);
      p.moving = true;
    } else if (this.movementPath.length) {
      const node = this.movementPath[0];
      const ddx = node.x - p.x, ddy = node.y - p.y;
      const d = Math.hypot(ddx, ddy);
      if (d < 5) {
        this.movementPath.shift();
        p.moving = false;
      } else {
        dx = ddx / d; dy = ddy / d;
        p.facing = Math.atan2(dy, dx);
        p.moving = true;
      }
    } else {
      p.moving = false;
    }

    const canSprint = (this.keys.shift) && p.stamina > 1 && p.moving;
    p.sprinting = canSprint;
    const base = this.hero.speed * this.speedMultiplier * (canSprint ? 1.55 : 1);
    if (p.moving) {
      const before = { x: p.x, y: p.y };
      const moved = this.world.moveCircle(p.x, p.y, dx * base * dt, dy * base * dt, 10);
      p.x = moved.x; p.y = moved.y;
      p.step += dt * (canSprint ? 13 : 9);
      const travelled = Math.hypot(p.x - before.x, p.y - before.y);
      this.stats.distance += travelled;
      if (travelled < .2 && this.movementPath.length) this.movementPath = [];
      if (canSprint) p.stamina = Math.max(0, p.stamina - 17 * dt);
    }
    if (!canSprint) p.stamina = Math.min(this.maxStamina, p.stamina + 13 * dt);
    if (this.speed === 0) p.moving = false;
  }

  setMoveTarget(x, y) {
    if (!this.started || this.uiPaused || this.player.down) return;
    if (this.world.collides(x, y, 10)) {
      const open = this.world.findNearbyOpen(x, y, 46, 7);
      x = open.x; y = open.y;
    }
    this.movementPath = this.world.pathfind(this.player.x, this.player.y, x, y);
    this.effects.push({ type: 'click', x, y, life: .8, max: .8 });
  }

  /* ================= display views ================= */
  get questListView() {
    return Object.values(QUESTS).map(q => this.questView(q.id)).filter(Boolean);
  }

  questView(id) {
    const def = QUESTS[id];
    if (!def) return null;
    const state = this.quests[id];
    const giver = def.giver ? NPCS.find(n => n.id === def.giver) : null;
    const tasks = def.tasks.map((task, i) => ({ ...task, done: this.taskDone(id, i) }));
    const ready = this.questReady(id);
    return {
      def, state: state.state, tasks, ready, giver, tracked: this.trackedQuest === id,
      giverName: giver?.name || null,
      statusLabel: state.state === 'done' ? 'Completed' : state.state === 'available' ? 'Rumour' : ready ? 'Ready to hand in' : 'In progress'
    };
  }

  taskDone(qid, index) {
    const def = QUESTS[qid];
    const state = this.quests[qid];
    const task = def.tasks[index];
    if (!task || !state) return false;
    if (state.state === 'done') return true;
    if (task.type === 'return') return false;
    if (task.type === 'have') return this.hasItem(task.item, task.qty);
    if (task.type === 'auto') return state.state === 'done';
    return !!state.tasks[index];
  }

  questReady(qid) {
    const def = QUESTS[qid];
    const state = this.quests[qid];
    if (!def || !state || state.state !== 'active') return false;
    return def.tasks.every((t, i) => t.type === 'return' || this.taskDone(qid, i));
  }

  getDialogue(npcId) {
    const def = NPCS.find(n => n.id === npcId);
    if (!def) return null;
    const view = this.npcView(def);
    let quest = null;
    if (def.quest && QUESTS[def.quest]) {
      const qv = this.questView(def.quest);
      if (qv.state !== 'done') quest = qv;
    }
    return {
      npc: def,
      view,
      quest,
      seller: this.sellerAvailable(def) ? this.sellerView(def) : null,
      faction: FACTIONS[def.faction],
      phaseWord: this.phase()
    };
  }

  sellerAvailable(def) {
    const seller = SELLERS[def.id];
    if (!seller) return false;
    const { block } = this.resolveBlock(def, this.minutes);
    return seller.shopPlaces.includes(block.place);
  }

  sellerView(def) {
    const seller = SELLERS[def.id];
    if (!seller) return null;
    const stock = seller.stock
      .filter(id => !(ITEMS[id].unique && this.hasItem(id)))
      .map(id => ({ id, ...ITEMS[id], price: ITEMS[id].buy }));
    const buys = seller.buys
      .filter(id => this.hasItem(id))
      .map(id => ({ id, ...ITEMS[id], price: ITEMS[id].sell, count: this.player.inventory[id] }));
    return {
      id: def.id,
      title: seller.title,
      line: seller.line,
      stock,
      buys,
      gold: this.player.gold
    };
  }

  buyItem(sellerId, itemId) {
    const def = NPCS.find(n => n.id === sellerId);
    const seller = SELLERS[sellerId];
    if (!def || !seller || !this.sellerAvailable(def)) return { ok: false, message: 'The seller is not at their stall.' };
    if (!seller.stock.includes(itemId)) return { ok: false, message: 'They do not sell that.' };
    const item = ITEMS[itemId];
    if (!item?.buy) return { ok: false, message: 'Not for sale.' };
    if (item.unique && this.hasItem(itemId)) return { ok: false, message: 'You already carry one.' };
    if (this.player.gold < item.buy) return { ok: false, message: 'Not enough gold.' };
    this.player.gold -= item.buy;
    this.addItem(itemId, 1);
    this.ui?.refreshHud();
    this.ui?.refreshActivePanel();
    return { ok: true, message: `Bought ${item.name} for ${item.buy} gold.` };
  }

  sellItem(sellerId, itemId) {
    const seller = SELLERS[sellerId];
    const def = NPCS.find(n => n.id === sellerId);
    if (!seller || !def || !this.sellerAvailable(def)) return { ok: false, message: 'The seller is not at their stall.' };
    if (!seller.buys.includes(itemId) || !this.hasItem(itemId)) return { ok: false, message: 'They do not want that.' };
    const item = ITEMS[itemId];
    this.removeItem(itemId, 1);
    this.player.gold += item.sell;
    this.ui?.refreshHud();
    this.ui?.refreshActivePanel();
    return { ok: true, message: `Sold ${item.name} for ${item.sell} gold.` };
  }

  useItem(itemId) {
    const item = ITEMS[itemId];
    if (!item || !this.hasItem(itemId)) return { ok: false, message: 'You do not have that.' };
    if (!item.use) return { ok: false, message: `${item.name} is not something you can use that way.` };
    const hpFull = this.player.hp >= this.playerMaxHp;
    const staminaFull = this.player.stamina >= this.maxStamina;
    if (item.use.hp >= 999 && hpFull && staminaFull) return { ok: false, message: 'You are already whole.' };
    if ((!item.use.hp || item.use.hp < 999) && hpFull) return { ok: false, message: 'You are already hale.' };
    this.removeItem(itemId, 1);
    if (item.use.hp) this.player.hp = Math.min(this.playerMaxHp, this.player.hp + (item.use.hp >= 999 ? this.playerMaxHp : item.use.hp));
    if (item.use.stamina) this.player.stamina = Math.min(this.maxStamina, this.player.stamina + (item.use.stamina >= 999 ? this.maxStamina : item.use.stamina));
    this.effects.push({ type: 'heal', x: this.player.x, y: this.player.y, life: 1.2, max: 1.2 });
    this.ui?.refreshHud();
    this.ui?.refreshActivePanel();
    return { ok: true, message: `Used ${item.name}.` };
  }

  /* ================= quests ================= */
  getWorldMarkers() {
    const markers = [];
    for (const q of Object.values(QUESTS)) {
      if (!q.giver) continue;
      const state = this.quests[q.id];
      if (state.state === 'available' || (state.state === 'active' && this.questReady(q.id))) {
        const npc = this.npcs.find(n => n.id === q.giver);
        if (npc && !npc.hidden) markers.push({ x: npc.x, y: npc.y - 58, type: 'giver', questId: q.id });
      }
    }
    if (this.trackedQuest) {
      const q = QUESTS[this.trackedQuest];
      const state = this.quests[this.trackedQuest];
      if (q && state.state === 'active') {
        q.tasks.forEach((t, i) => {
          if (t.type === 'visit' && !this.taskDone(this.trackedQuest, i)) {
            const obj = QUEST_OBJECTS.find(o => o.id === t.target);
            if (obj) markers.push({ x: obj.x, y: obj.y - 30, type: 'target', questId: q.id });
          }
        });
      }
    }
    return markers;
  }

  trackerInfo() {
    if (!this.trackedQuest) return null;
    const def = QUESTS[this.trackedQuest];
    const state = this.quests[this.trackedQuest];
    if (!def || state.state !== 'active') return null;
    let label = null, x = null, y = null;
    const giver = def.giver ? this.npcs.find(n => n.id === def.giver) : null;
    const visitTask = def.tasks.find((t, i) => t.type === 'visit' && !this.taskDone(this.trackedQuest, i));
    if (visitTask) {
      const obj = QUEST_OBJECTS.find(o => o.id === visitTask.target);
      if (obj) { label = QUEST_TARGET_NAMES[obj.id] || obj.label; x = obj.x; y = obj.y; }
    }
    if (label === null && this.questReady(this.trackedQuest)) {
      if (giver) {
        label = `Return to ${giver.name}`;
        const npc = this.npcs.find(n => n.id === giver.id);
        if (npc) { x = npc.x; y = npc.y; }
      }
    }
    if (label === null) {
      const have = def.tasks.find(t => t.type === 'have');
      if (have) label = `${have.label.replace('Gather ', 'Gather ')} (${this.player.inventory[have.item] || 0}/${have.qty})`;
      if (giver) {
        const npc = this.npcs.find(n => n.id === giver.id);
        if (npc) { x = npc.x; y = npc.y; }
        label = label || `Return to ${giver.name}`;
      }
    }
    if (!label) return null;
    let dist = 0, direction = '';
    if (x !== null) {
      const dx = x - this.player.x, dy = y - this.player.y;
      dist = Math.round(Math.hypot(dx, dy) / 3);
      const angle = Math.atan2(dy, dx) * 180 / Math.PI;
      const dirs = ['E', 'SE', 'S', 'SW', 'W', 'NW', 'N', 'NE'];
      direction = dirs[Math.round(((angle + 360) % 360) / 45) % 8];
    }
    return { title: def.title, label, distance: dist, direction, ready: this.questReady(this.trackedQuest) };
  }

  acceptQuest(qid) {
    const def = QUESTS[qid];
    const state = this.quests[qid];
    if (!def || !state || state.state !== 'available') return false;
    state.state = 'active';
    state.tasks = def.tasks.map(() => false);
    state.accepted = true;
    this.trackedQuest = qid;
    this.ui?.toast(`New quest: ${def.title}.`, 'good');
    this.ui?.refreshAll();
    return true;
  }

  turnInQuest(npcId) {
    const def = Object.values(QUESTS).find(q => q.giver === npcId);
    if (!def) return null;
    const state = this.quests[def.id];
    if (state.state !== 'active' || !this.questReady(def.id)) return null;
    this.finishQuest(def);
    return def;
  }

  finishQuest(def) {
    const state = this.quests[def.id];
    state.state = 'done';
    state.tasks = state.tasks.map(() => true);
    for (const task of def.tasks) {
      if (task.type === 'have' && task.consume) this.removeItem(task.item, task.qty);
    }
    const reward = def.reward || {};
    if (reward.gold) this.addGold(reward.gold);
    if (reward.xp) this.addXp(reward.xp);
    for (const it of reward.items || []) this.addItem(it.id, it.qty);
    if (def.faction) {
      this.regard[def.faction] = (this.regard[def.faction] || 0) + 8;
    }
    this.stats.quests += 1;
    if (this.trackedQuest === def.id) this.trackedQuest = null;
    for (const task of def.tasks) {
      if (task.type === 'visit') {
        const obj = QUEST_OBJECTS.find(o => o.id === task.target);
        if (obj) this.effects.push({ type: 'sparkle', x: obj.x, y: obj.y, life: 1, max: 1 });
      }
    }
    const rewardBits = [];
    if (reward.gold) rewardBits.push(`${reward.gold} gold`);
    if (reward.xp) rewardBits.push(`${reward.xp} xp`);
    for (const it of reward.items || []) rewardBits.push(`${ITEMS[it.id].name}${it.qty > 1 ? ` ×${it.qty}` : ''}`);
    this.ui?.toast(`Quest complete: ${def.title}${rewardBits.length ? ` — ${rewardBits.join(', ')}` : ''}.`, 'good');
    this.ui?.refreshAll();
  }

  completeWelcome(npcId) {
    if (this.quests.welcome.state !== 'active') return;
    this.quests.welcome.state = 'done';
    this.quests.welcome.tasks = [true];
    this.addGold(QUESTS.welcome.reward.gold);
    this.addXp(QUESTS.welcome.reward.xp);
    this.ui?.toast('The Road to Alderwick — you have met a neighbour. 10 gold earned.', 'good');
    this.ui?.refreshAll();
  }

  trackQuest(qid) {
    if (this.quests[qid]?.state !== 'active') return;
    this.trackedQuest = qid;
    this.ui?.refreshAll();
  }
  untrackQuest() {
    this.trackedQuest = null;
    this.ui?.refreshAll();
  }

  regardInfo(factionId) {
    const score = this.regard[factionId] || 0;
    const label = score >= 40 ? 'Kin' : score >= 24 ? 'Trusted' : score >= 8 ? 'Known face' : 'Stranger';
    return { score, label };
  }

  /* ================= interaction ================= */
  updateInteraction() {
    if (!this.started || this.uiPaused || this.player?.down) {
      this.target = null;
      this.ui?.setPrompt(null);
      return;
    }
    const p = this.player;
    const groups = { 0: [], 1: [], 2: [], 3: [] };
    for (const npc of this.npcs) {
      if (npc.hidden) continue;
      const d = Math.hypot(npc.x - p.x, npc.y - p.y);
      if (d < 84) groups[1].push({ kind: 'npc', npc, x: npc.x, y: npc.y, d, label: `Speak with ${npc.def.name}` });
    }
    for (const obj of QUEST_OBJECTS) {
      const activeTask = this.activeVisitTask(obj.id);
      if (!activeTask) continue;
      const d = Math.hypot(obj.x - p.x, obj.y - p.y);
      if (d < 66) groups[0].push({ kind: 'quest', object: obj, qid: activeTask.qid, taskIndex: activeTask.index, x: obj.x, y: obj.y, d, label: obj.label });
    }
    for (const node of GATHER_NODES) {
      if (this.collectedNodes.has(node.id)) continue;
      const d = Math.hypot(node.x - p.x, node.y - p.y);
      if (d < 62) groups[1].push({ kind: 'gather', node, x: node.x, y: node.y, d, label: node.label });
    }
    for (const spot of FISH_SPOTS) {
      const d = Math.hypot(spot.x - p.x, spot.y - p.y);
      if (d < 68) groups[1].push({ kind: 'fish', spot, x: spot.x, y: spot.y, d, label: `Fish at ${spot.name.toLowerCase()}` });
    }
    for (const stall of STALLS) {
      const d = Math.hypot(stall.x - p.x, stall.y - p.y);
      if (d < 88) {
        const seller = NPCS.find(n => n.id === stall.seller);
        const open = seller && this.sellerAvailable(seller);
        groups[2].push({ kind: 'stall', stall, x: stall.x, y: stall.y, d, label: open ? `Browse ${stall.name}` : `${stall.name} — shuttered` });
      }
    }
    for (const obj of INTERACTABLES) {
      const radius = obj.id === 'oldOak' ? 82 : 72;
      const d = Math.hypot(obj.x - p.x, obj.y - p.y);
      if (d < radius) {
        const labels = {
          sign: 'Read the gate sign', tavernDoor: 'Rest at the Bent Hook',
          well: 'Draw from the well', oldOak: 'Rest beneath the old oak'
        };
        groups[2].push({ kind: 'static', object: obj, x: obj.x, y: obj.y, d, label: labels[obj.id] || obj.type });
      }
    }
    for (const a of this.animals) {
      if (a.sp.behavior === 'fly' || a.species === 'wolf') continue;
      const d = Math.hypot(a.x - p.x, a.y - p.y);
      if (d < 64) groups[3].push({ kind: 'animal', animal: a, x: a.x, y: a.y, d, label: `Look at the ${a.sp.label.toLowerCase()}` });
    }
    let chosen = null;
    for (const key of [0, 1, 2, 3]) {
      if (groups[key].length) {
        groups[key].sort((a, b) => a.d - b.d);
        chosen = groups[key][0];
        break;
      }
    }
    this.target = chosen;
    this.ui?.setPrompt(chosen ? chosen.label : null);
  }

  activeVisitTask(objectId) {
    for (const [qid, q] of Object.entries(this.quests)) {
      if (q.state !== 'active') continue;
      const def = QUESTS[qid];
      for (let i = 0; i < def.tasks.length; i++) {
        const t = def.tasks[i];
        if (t.type === 'visit' && t.target === objectId && !this.taskDone(qid, i)) return { qid, index: i };
      }
    }
    return null;
  }

  interact() {
    if (!this.started || this.uiPaused || this.player.down) return;
    const target = this.target;
    if (!target) return;
    if (target.kind === 'npc') {
      this.completeWelcome();
      this.ui?.openDialogue(target.npc.id);
    } else if (target.kind === 'quest') {
      this.collectQuestObject(target);
    } else if (target.kind === 'gather') {
      this.gatherNode(target.node);
    } else if (target.kind === 'fish') {
      this.ui?.openFishing(target.spot.id);
    } else if (target.kind === 'stall') {
      const seller = NPCS.find(n => n.id === target.stall.seller);
      if (!seller) return;
      if (this.sellerAvailable(seller)) this.ui?.openShop(seller.id);
      else {
        const view = this.npcView(seller);
        this.ui?.toast(`${target.stall.name} is shuttered. ${seller.name} is ${view.statusLine.toLowerCase()}.`, 'warn');
      }
    } else if (target.kind === 'static') {
      this.useStatic(target.object);
    } else if (target.kind === 'animal') {
      this.inspectAnimal(target.animal);
    }
    this.updateInteraction();
  }

  collectQuestObject(target) {
    const { qid, index, object } = target;
    this.quests[qid].tasks[index] = true;
    const q = QUESTS[qid];
    const flavours = {
      net: ['You haul the gillnet from the reeds. It is heavy with weed, still sound.', 'The net comes free with a wet slurp. Two trout are tangled in it, both furious.'],
      tracks: ['These prints are too deep and too even for a deer. Wolf sign, and large.', 'The bent grass tells a story: something circled the stones twice, then left north.', 'A tuft of grey fur on the thorn. This was no dog.'],
      page: ['A rain-wrinkled ledger page covered in Osric’s tidy copperplate.', 'The page lists debts, favours and at least one very bad poem.', 'Another page, pinned under a bench leg. The wind has not read it yet.']
    };
    const lines = flavours[object.type] || ['You have what you came for.'];
    const line = lines[index % lines.length];
    this.effects.push({ type: 'sparkle', x: object.x, y: object.y, life: 1.1, max: 1.1 });
    this.ui?.toast(line, 'good');
    if (q && this.questReady(qid)) {
      const giver = NPCS.find(n => n.id === q.giver);
      if (giver) this.ui?.toast(`You have what ${giver.name} needs. Return to them when you are ready.`, 'good');
    }
    this.ui?.refreshAll();
  }

  gatherNode(node) {
    this.collectedNodes.add(node.id);
    let qty = 1;
    if ((node.type === 'herb' || node.type === 'mushroom') && this.hero.id === 'rowan' && Math.random() < .35) qty += 1;
    if (this.hasItem('charm') && Math.random() < .25) qty += 1;
    if (this.keenTimer > 0) qty += 1;
    const itemId = node.type;
    this.addItem(itemId, qty);
    this.stats.gathered += qty;
    this.addXp(PLUS.GATHER_XP);
    this.effects.push({ type: 'sparkle', x: node.x, y: node.y, life: 1, max: 1 });
    this.effects.push({ type: 'text', x: node.x, y: node.y - 22, text: `+${qty} ${GATHER_LABELS[itemId]}`, color: '#e2cc92', life: 1.4, max: 1.4 });
    this.ui?.toast(`Gathered ${qty} × ${GATHER_LABELS[itemId]}.`, 'good');
    this.ui?.refreshHud();
  }

  useStatic(obj) {
    if (obj.id === 'sign') this.ui?.openSign();
    else if (obj.id === 'tavernDoor') this.ui?.openRest();
    else if (obj.id === 'well') {
      if (this.player.stamina >= this.maxStamina - 1 && this.player.hp >= this.playerMaxHp) {
        this.ui?.toast('The water is cold and sweet. You need nothing more just now.');
        return;
      }
      this.player.stamina = this.maxStamina;
      this.player.hp = Math.min(this.playerMaxHp, this.player.hp + 6);
      this.effects.push({ type: 'heal', x: this.player.x, y: this.player.y, life: 1, max: 1 });
      this.ui?.toast('The well water tastes of iron and rain. You feel steadier.', 'good');
      this.ui?.refreshHud();
    } else if (obj.id === 'oldOak') {
      this.player.stamina = this.maxStamina;
      this.minutes = (this.minutes + 10) % 1440;
      this.ui?.toast('You rest in the shade of the old oak. Ten quiet minutes pass.', 'good');
      this.ui?.refreshHud();
    }
  }

  inspectAnimal(animal) {
    const notes = {
      cat: 'Pudding regards you with the mild contempt of a creature who has never paid rent.',
      deer: 'A red deer, grazing with one ear turned toward the gate. It has seen you and decided you are not worth leaving.',
      rabbit: 'A hare, all elbows and nerves. It bolts in a crooked line for the hedge.',
      fox: 'A red fox slipping along the treeline. It is not afraid of you; it is simply busy.',
      duck: 'Mallards on the slow water. One of them is almost certainly called “the fat one”.',
      heron: 'The grey heron stands as still as a fence post, waiting for the river to make a mistake.',
      otter: 'An otter rolls in the shallows, entirely aware of its own charm.',
      sheep: 'A meadow ewe. It looks through you with the serene certainty that you are not grass.',
      bird: 'A river swift, stitching the air above the water.'
    };
    this.ui?.toast(notes[animal.species] || `A ${animal.sp.label.toLowerCase()}.`, '');
  }

  openRest() { this.ui?.openRest(); }

  restUntilMorning() {
    const target = 6 * 60 + 30;
    let dayAdvanced = false;
    if (this.minutes > target) { this.day += 1; dayAdvanced = true; }
    this.minutes = target;
    this._lastHour = 6;
    this.player.hp = this.playerMaxHp;
    this.player.stamina = this.maxStamina;
    this.player.x = 1200;
    this.player.y = 970;
    this.wolves = [];
    this.wolvesSpawned = false;
    this.wolfWarned = false;
    this.regrowNodes();
    this.resnapNpcs();
    this.ui?.toast(`You sleep above the taproom. Morning comes to Alderwick${dayAdvanced ? `, day ${this.day}` : ''}.`, 'good');
    this.ui?.refreshAll();
  }

  /* ================= fishing ================= */
  fishingCatch(spotId, success, usedBait) {
    const spot = FISH_SPOTS.find(s => s.id === spotId);
    if (!spot) return { success: false, message: 'The river is empty.' };
    if (usedBait && this.hasItem('bait')) this.removeItem('bait', 1);
    if (!success) {
      this.ui?.refreshHud();
      return { success: false, count: 0, message: 'The line comes back empty. The trout are laughing at you.' };
    }
    let count = 1;
    if (spot.bonus >= 0.08 && Math.random() < .5) count++;
    if (this.hasItem('charm') && Math.random() < .25) count++;
    this.addItem('fish', count);
    this.stats.fish += count;
    this.addXp(PLUS.FISH_XP);
    this.effects.push({ type: 'splash', x: spot.x, y: spot.y, life: 1, max: 1 });
    this.ui?.refreshHud();
    return { success: true, count, message: `You land ${count} Fenn trout. Cold, silver and fat.` };
  }

  /* ================= combat & ability ================= */
  attack() {
    if (!this.started || this.uiPaused || this.player.down) return;
    if (this.player.attackCooldown > 0) return;
    if (this.player.stamina < 6) {
      this.ui?.toast('You are too winded to swing.', 'warn');
      return;
    }
    this.player.stamina -= 6;
    this.player.attackCooldown = 0.5;
    this.player.attackTimer = 0.26;
    const p = this.player;
    const dirX = Math.cos(p.facing), dirY = Math.sin(p.facing);
    let hit = null, hitDist = Infinity;
    for (const w of this.wolves) {
      const dx = w.x - p.x, dy = w.y - p.y;
      const d = Math.hypot(dx, dy);
      if (d < 74) {
        const dot = (dx / (d || 1)) * dirX + (dy / (d || 1)) * dirY;
        if (dot > -0.35 && d < hitDist) { hit = w; hitDist = d; }
      }
    }
    this.effects.push({ type: 'slash', x: p.x + dirX * 24, y: p.y + dirY * 24, angle: p.facing, life: .28, max: .28 });
    if (hit) {
      let dmg = 22 + this.hero.stats.might * 5;
      if (this.hasItem('oil')) dmg *= 1.25;
      hit.hp -= dmg;
      hit.hitFlash = 1;
      this.effects.push({ type: 'text', x: hit.x, y: hit.y - 28, text: `-${Math.round(dmg)}`, color: '#eac37a', life: 1.1, max: 1.1 });
      this.shake = 4;
      if (hit.hp <= 0) {
        this.wolves = this.wolves.filter(w => w !== hit);
        this.stats.wolves += 1;
        this.addItem('pelt', 1);
        this.addXp(PLUS.WOLF_XP);
        this.effects.push({ type: 'sparkle', x: hit.x, y: hit.y, life: 1.3, max: 1.3 });
        this.ui?.toast('The wolf breaks and flees. You take a grey pelt and 22 experience.', 'good');
      }
      this.ui?.refreshHud();
    }
  }

  ability() {
    if (!this.started || this.uiPaused || this.player.down) return;
    if (this.cooldowns.ability > 0) {
      this.ui?.toast(`${this.hero.ability.name} is not yet ready.`, 'warn');
      return;
    }
    const a = this.hero.ability;
    this.cooldowns.ability = a.cooldown;
    if (a.type === 'keen') {
      this.keenTimer = 18;
      this.ui?.toast('Keen Eye: the undergrowth seems to open up around you. Gatherables and wildlife glow.', 'good');
    } else if (a.type === 'secondwind') {
      this.player.hp = Math.min(this.playerMaxHp, this.player.hp + this.playerMaxHp * 0.4);
      this.player.stamina = this.maxStamina;
      this.effects.push({ type: 'heal', x: this.player.x, y: this.player.y, life: 1.4, max: 1.4 });
      this.ui?.toast('Second Wind: you plant your feet, breathe, and rise.', 'good');
    } else if (a.type === 'emberward') {
      this.wardTimer = 22;
      this.effects.push({ type: 'ward', x: this.player.x, y: this.player.y, life: 1.2, max: 1.2 });
      this.ui?.toast('Ember Ward: a ring of warm light settles around you. Wolves will keep their distance.', 'good');
    }
    this.ui?.refreshHud();
  }

  /* ================= frame update ================= */
  update(dt) {
    dt = Math.min(dt, 0.05);
    this.realTime += dt;
    if (!this.started) { this.updatePreview(dt); return; }
    if (this.uiPaused) return;
    if (this.player.down) { this.updateDown(dt); return; }
    if (this.speed > 0) {
      this.updateClock(dt);
      this.updateNpcs(dt);
      this.updateAnimals(dt);
      this.updateWolves(dt);
      this.syncTimeEvents();
    }
    this.updatePlayer(dt);
    this.updateEffects(dt);
    this.updateInteraction();
    this.hudAccumulator += dt;
    if (this.hudAccumulator > 0.16) {
      this.hudAccumulator = 0;
      this.ui?.refreshHud();
    }
    if (this.shake) this.shake = Math.max(0, this.shake - dt * 22);
  }

  updatePreview(dt) {
    this.previewTime += dt;
    this.updateAnimals(dt);
    for (const npc of this.npcs) {
      npc.walkPhase += dt * (npc.path.length ? 9 : 1.4);
    }
  }

  updateEffects(dt) {
    for (const e of this.effects) e.life -= dt;
    this.effects = this.effects.filter(e => e.life > 0);
    if (this.player?.hitFlash > 0) this.player.hitFlash = Math.max(0, this.player.hitFlash - dt);
  }

  /* ================= input ================= */
  bindInput() {
    window.addEventListener('keydown', (e) => {
      if (this.ui?.captureKey(e)) return;
      if (!this.started) return;
      const k = e.key.toLowerCase();
      const movement = ['w', 'a', 's', 'd', 'arrowup', 'arrowdown', 'arrowleft', 'arrowright', 'shift'];
      if (movement.includes(k)) {
        this.keys[k] = true;
        if (k.startsWith('arrow')) e.preventDefault();
        return;
      }
      if (k === 'e') { e.preventDefault(); this.interact(); }
      else if (k === 'f' || k === ' ') { e.preventDefault(); this.attack(); }
      else if (k === 'q') { e.preventDefault(); this.ability(); }
    });
    window.addEventListener('keyup', (e) => {
      this.keys[e.key.toLowerCase()] = false;
    });
    window.addEventListener('blur', () => { this.keys = {}; });
  }
}
