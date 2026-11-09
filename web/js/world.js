// Alderwick — geometry, hand-built decoration, collision and pathfinding.
import {
  WORLD, RIVER, BUILDINGS, PATHS, LOCATIONS, TREES_HINT,
  GATHER_NODES, FISH_SPOTS, QUEST_OBJECTS, STALLS, ANIMAL_SITES, INTERACTABLES
} from './data.js';

export function mulberry32(seed) {
  let a = seed >>> 0;
  return function () {
    a |= 0; a = (a + 0x6D2B79F5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

const dist = (x1, y1, x2, y2) => Math.hypot(x2 - x1, y2 - y1);
const clamp = (v, a, b) => Math.max(a, Math.min(b, v));

function distToSegment(px, py, x1, y1, x2, y2) {
  const dx = x2 - x1, dy = y2 - y1;
  const len = dx * dx + dy * dy;
  let t = len ? ((px - x1) * dx + (py - y1) * dy) / len : 0;
  t = clamp(t, 0, 1);
  return Math.hypot(px - (x1 + dx * t), py - (y1 + dy * t));
}

class MinHeap {
  constructor() { this.items = []; }
  push(node, priority) {
    const items = this.items;
    items.push({ node, priority });
    let i = items.length - 1;
    while (i > 0) {
      const p = (i - 1) >> 1;
      if (items[p].priority <= items[i].priority) break;
      [items[p], items[i]] = [items[i], items[p]];
      i = p;
    }
  }
  pop() {
    const items = this.items;
    if (!items.length) return null;
    const top = items[0];
    const last = items.pop();
    if (items.length) {
      items[0] = last;
      let i = 0;
      for (;;) {
        const l = i * 2 + 1, r = l + 1;
        let smallest = i;
        if (l < items.length && items[l].priority < items[smallest].priority) smallest = l;
        if (r < items.length && items[r].priority < items[smallest].priority) smallest = r;
        if (smallest === i) break;
        [items[smallest], items[i]] = [items[i], items[smallest]];
        i = smallest;
      }
    }
    return top.node;
  }
  get size() { return this.items.length; }
}

export class World {
  constructor() {
    this.width = WORLD.width;
    this.height = WORLD.height;
    this.buildings = BUILDINGS;
    this.paths = PATHS;
    this.locations = LOCATIONS;
    this.stalls = STALLS;
    this.clearings = [
      { x: LOCATIONS.glade.x, y: LOCATIONS.glade.y, r: 128 },
      { x: LOCATIONS.stones.x, y: LOCATIONS.stones.y, r: 100 },
      { x: LOCATIONS.woodlot.x, y: LOCATIONS.woodlot.y, r: 76 },
      { x: 887, y: 500, r: 94 },
      { x: 1080, y: 524, r: 88 }
    ];
    this.trees = [];
    this.bushes = [];
    this.rocks = [];
    this.flowers = [];
    this.reeds = [];
    this.lilies = [];
    this.stumps = [];
    this.logs = [];
    this.lamps = [
      [1134, 1302], [1104, 1112], [1178, 920], [1180, 646], [1098, 816],
      [906, 1032], [1300, 1012], [1462, 1042], [1610, 892], [1698, 1024],
      [852, 978], [716, 946], [772, 1178], [1388, 1186], [1240, 604]
    ];
    this.benches = [
      { x: 1088, y: 1140, a: 0.15 }, { x: 1240, y: 1116, a: -0.2 },
      { x: 1188, y: 1188, a: 0.08 }, { x: 1336, y: 1012, a: Math.PI },
      { x: 1600, y: 858, a: -0.3 }, { x: 804, y: 1188, a: 0.5 },
      { x: 1072, y: 1036, a: 0.1 }
    ];
    this.solidProps = [];
    this.stoneCircle = [];
    this.crops = [];
    this.fences = [];
    this.hives = [{ x: 1176, y: 616 }, { x: 1202, y: 628 }, { x: 1190, y: 600 }];
    this.scarecrow = { x: 420, y: 1090 };
    this.wagon = { x: 1494, y: 1252 };
    this.well = { x: INTERACTABLES.find(i => i.id === 'well').x, y: INTERACTABLES.find(i => i.id === 'well').y };
    this.oak = { x: INTERACTABLES.find(i => i.id === 'oldOak').x, y: INTERACTABLES.find(i => i.id === 'oldOak').y };
    this.dockCrates = [{ x: 1600, y: 930 }, { x: 1636, y: 946 }, { x: 1606, y: 972 }, { x: 1572, y: 948 }];
    this.bridge = RIVER.bridge;
    this.dock = RIVER.dock;

    this.generate();
    this.buildSolids();
    this.buildNavGrid();
  }

  /* ---------------- river ---------------- */
  riverCenter(x) {
    return RIVER.centerY + RIVER.amp * Math.sin(x / RIVER.freq) + RIVER.amp2 * Math.sin(x / RIVER.freq2 + 2.1);
  }
  riverHalf(x) {
    return RIVER.halfWidth + 7 * Math.sin(x / 170 + 1.1);
  }
  riverEdges(x) {
    const c = this.riverCenter(x);
    const h = this.riverHalf(x);
    return { top: c - h, bottom: c + h, center: c };
  }
  inRect(x, y, r, pad = 0) {
    return x >= r.x - pad && x <= r.x + r.w + pad && y >= r.y - pad && y <= r.y + r.h + pad;
  }
  isWaterPoint(x, y) {
    if (this.inRect(x, y, this.bridge)) return false;
    if (this.inRect(x, y, this.dock)) return false;
    const e = this.riverEdges(x);
    return y >= e.top && y <= e.bottom;
  }

  /* ---------------- decoration ---------------- */
  generate() {
    const rng = mulberry32(0xA1D3F0);
    const nearObj = (x, y, list, pad) => list.some(o => dist(x, y, o.x, o.y) < pad);
    const nearPath = (x, y, pad) => this.paths.some(p => {
      for (let i = 1; i < p.points.length; i++) {
        const [x1, y1] = p.points[i - 1];
        const [x2, y2] = p.points[i];
        if (distToSegment(x, y, x1, y1, x2, y2) < p.width / 2 + pad) return true;
      }
      return false;
    });
    const nearBuilding = (x, y, pad) => this.buildings.some(b => x > b.x - pad && x < b.x + b.w + pad && y > b.y - pad && y < b.y + b.h + pad);
    const inClearing = (x, y) => this.clearings.some(c => dist(x, y, c.x, c.y) < c.r);
    const nearLocation = (x, y) => Object.values(this.locations).some(l => dist(x, y, l.x, l.y) < l.radius + 26);

    this.stoneCircle = [];
    const stoneAngles = [-0.55, 0.15, 0.8, 1.5, 2.2, 2.9, 3.6, 4.3];
    for (const a of stoneAngles) {
      const x = LOCATIONS.stones.x + Math.cos(a) * 58;
      const y = LOCATIONS.stones.y + Math.sin(a) * 44;
      this.stoneCircle.push({ x, y, h: 22 + rng() * 16, w: 12 + rng() * 8, tilt: (rng() - .5) * .18 });
      this.solidProps.push({ type: 'circle', x, y, r: 13 });
    }

    for (let attempt = 0; attempt < 3200 && this.trees.length < 238; attempt++) {
      const x = 36 + rng() * (this.width - 72);
      const y = 34 + rng() * (this.height - 70);
      const north = y < TREES_HINT.forestTop - 20;
      const southGrove = !north && (
        (x < 300 && y > 720 && y < 1000) ||
        (x > 2200 && y > 800 && y < 1320) ||
        (x > 1880 && y > 1050 && y < 1450) ||
        (y > 1440) ||
        (x > 880 && x < 1080 && y > 1430)
      );
      if (!north && !southGrove) continue;
      if (rng() < (north ? 0.24 : 0.5)) continue;
      const margin = 26;
      if (x < margin || y < margin || x > this.width - margin || y > this.height - margin) continue;
      if (dist(x, y, this.riverCenter(x), 0) < this.riverHalf(x) + 40) continue;
      if (y > this.riverCenter(x) - this.riverHalf(x) - 42 && y < this.riverCenter(x) + this.riverHalf(x) + 42) continue;
      if (nearBuilding(x, y, 32)) continue;
      if (nearPath(x, y, 24)) continue;
      if (nearLocation(x, y)) continue;
      if (inClearing(x, y)) continue;
      if (nearObj(x, y, GATHER_NODES, 34)) continue;
      if (nearObj(x, y, QUEST_OBJECTS, 32)) continue;
      if (nearObj(x, y, FISH_SPOTS, 40)) continue;
      if (nearObj(x, y, ANIMAL_SITES, 30)) continue;
      if (nearObj(x, y, this.stalls, 46)) continue;
      if (nearObj(x, y, this.trees, 32)) continue;
      if (nearObj(x, y, this.stoneCircle, 24)) continue;
      const roll = rng();
      const type = north
        ? (roll < 0.46 ? 'pine' : roll < 0.78 ? 'oak' : 'birch')
        : (roll < 0.55 ? 'oak' : roll < 0.8 ? 'birch' : 'pine');
      const scale = 0.78 + rng() * 0.62;
      this.trees.push({
        x, y, type, scale, sway: rng() * Math.PI * 2,
        tone: rng(), mossy: rng() < 0.35
      });
    }

    for (let i = 0; i < 150; i++) {
      const x = 60 + rng() * (this.width - 120);
      const y = 70 + rng() * (this.height - 160);
      if (this.isWaterPoint(x, y)) continue;
      const north = y < 640;
      if (!north && rng() < .55) continue;
      if (nearPath(x, y, 14) || nearBuilding(x, y, 16) || nearObj(x, y, this.trees, 20)) continue;
      if (north) this.bushes.push({ x, y, s: .55 + rng() * .6, tone: rng() });
    }

    for (let i = 0; i < 90; i++) {
      const x = 50 + rng() * (this.width - 100);
      const y = 50 + rng() * (this.height - 130);
      if (this.isWaterPoint(x, y)) continue;
      if (nearPath(x, y, 6) || nearBuilding(x, y, 12)) continue;
      if (y < 620 && rng() < .6) continue;
      this.flowers.push({ x, y, c: rng() < .5 ? '#e6d79a' : rng() < .5 ? '#d9a8ac' : '#bcc7e0', s: .6 + rng() * .7 });
    }

    for (let i = 0; i < 34; i++) {
      const x = 60 + rng() * (this.width - 120);
      const y = 70 + rng() * (this.height - 150);
      if (this.isWaterPoint(x, y) || nearPath(x, y, 10) || nearBuilding(x, y, 14) || inClearing(x, y)) continue;
      this.rocks.push({ x, y, s: .5 + rng() * 1.1, tone: rng() });
      if (rng() < .35 && !nearLocation(x, y)) this.solidProps.push({ type: 'circle', x, y, r: 9 * rng() + 5 });
    }

    for (let i = 0; i < 46; i++) {
      const x = 40 + rng() * (this.width - 80);
      const e = this.riverEdges(x);
      if (this.inRect(x, e.top - 12, this.bridge) || this.inRect(x, e.bottom + 12, this.bridge)) continue;
      if (this.inRect(x, e.top - 12, this.dock) || this.inRect(x, e.bottom + 12, this.dock)) continue;
      const north = rng() < .5;
      const y = north ? e.top - 5 - rng() * 9 : e.bottom + 5 + rng() * 9;
      this.reeds.push({ x, y, h: 10 + rng() * 12, lean: (rng() - .5) * .3 });
    }

    for (let i = 0; i < 34; i++) {
      const x = 100 + rng() * (this.width - 200);
      const e = this.riverEdges(x);
      if (this.inRect(x, e.center, this.bridge) || this.inRect(x, e.center, this.dock)) continue;
      this.lilies.push({ x, y: e.center + (rng() - .5) * 32, s: .6 + rng() * .7 });
    }

    for (let i = 0; i < 14; i++) {
      const x = 200 + rng() * (this.width - 400);
      const y = 90 + rng() * (this.height - 250);
      if (this.isWaterPoint(x, y) || nearPath(x, y, 12) || nearBuilding(x, y, 14)) continue;
      if (y > 620) continue;
      this.stumps.push({ x, y, s: .7 + rng() * .5 });
    }

    for (let i = 0; i < 9; i++) {
      const x = 260 + rng() * 700;
      const y = 160 + rng() * 380;
      if (nearPath(x, y, 20) || inClearing(x, y)) continue;
      this.logs.push({ x, y, a: rng() * Math.PI, s: .8 + rng() * .5 });
    }

    // crop rows in the Marle fields
    const cropTones = ['#a7a86a', '#8f9d58', '#c3b06b', '#7d9354'];
    for (let row = 0; row < 9; row++) {
      for (let col = 0; col < 8; col++) {
        const x = 292 + col * 27 + (rng() - .5) * 5;
        const y = 1068 + row * 30 + (rng() - .5) * 5;
        if (dist(x, y, 438, 1226) < 26) continue;
        this.crops.push({ x, y, tone: cropTones[(row + col) % cropTones.length], s: .75 + rng() * .4 });
      }
    }
    for (let col = 0; col < 5; col++) {
      for (let row = 0; row < 3; row++) {
        this.crops.push({ x: 604 + col * 22, y: 1322 + row * 24, tone: '#9aa765', s: .8 });
      }
    }

    // low rail fences around the sheep meadow and along field edges
    const addFence = (x1, y1, x2, y2, gapEvery = 0) => {
      const len = Math.hypot(x2 - x1, y2 - y1);
      const steps = Math.floor(len / 26);
      for (let i = 0; i <= steps; i++) {
        if (gapEvery && i % gapEvery === 0) continue;
        const t = i / steps;
        this.fences.push({ x: x1 + (x2 - x1) * t, y: y1 + (y2 - y1) * t, a: Math.atan2(y2 - y1, x2 - x1) });
      }
    };
    addFence(520, 1268, 712, 1268, 7);
    addFence(712, 1268, 712, 1406, 0);
    addFence(712, 1406, 520, 1406, 0);
    addFence(520, 1406, 520, 1268, 7);
    addFence(246, 1050, 512, 1050, 9);

    for (let i = 0; i < 20; i++) {
      const x = 380 + rng() * 500;
      const y = 740 + rng() * 120;
      if (this.isWaterPoint(x, y) || nearPath(x, y, 12)) continue;
      this.bushes.push({ x, y, s: .4 + rng() * .4, tone: rng() });
    }
    this.bushes.sort((a, b) => a.y - b.y);
  }

  buildSolids() {
    for (const b of this.buildings) {
      this.solidProps.push({ type: 'rect', x: b.x, y: b.y, w: b.w, h: b.h, label: 'building' });
    }
    for (const s of this.stalls) {
      this.solidProps.push({ type: 'rect', x: s.x - 34, y: s.y - 4, w: 68, h: 34, label: 'stall' });
    }
    this.solidProps.push({ type: 'rect', x: this.wagon.x - 54, y: this.wagon.y - 4, w: 108, h: 56, label: 'wagon' });
    this.solidProps.push({ type: 'circle', x: this.well.x, y: this.well.y, r: 19 });
    this.solidProps.push({ type: 'circle', x: this.oak.x, y: this.oak.y, r: 24 });
    for (const t of this.trees) {
      this.solidProps.push({ type: 'circle', x: t.x, y: t.y + 3, r: 10 * t.scale });
    }
  }

  /* ---------------- collision ---------------- */
  insideBuilding(x, y, pad = 0) {
    return this.buildings.some(b => x > b.x - pad && x < b.x + b.w + pad && y > b.y - pad && y < b.y + b.h + pad);
  }

  pointWalkable(x, y) {
    if (x < 28 || y < 28 || x > this.width - 28 || y > this.height - 28) return false;
    if (this.isWaterPoint(x, y)) return false;
    if (this.insideBuilding(x, y, 2)) return false;
    for (const s of this.solidProps) {
      if (s.type === 'rect') {
        if (x > s.x - 2 && x < s.x + s.w + 2 && y > s.y - 2 && y < s.y + s.h + 2) return false;
      } else if (dist(x, y, s.x, s.y) < s.r + 2) return false;
    }
    return true;
  }

  collides(x, y, r = 10) {
    if (x < 24 + r || y < 24 + r || x > this.width - 24 - r || y > this.height - 24 - r) return true;
    const samples = [[0, 0], [r, 0], [-r, 0], [0, r], [0, -r], [r * .7, r * .7], [-r * .7, -r * .7], [r * .7, -r * .7], [-r * .7, r * .7]];
    for (const [ox, oy] of samples) {
      if (this.isWaterPoint(x + ox, y + oy)) return true;
    }
    if (this.insideBuilding(x, y, r)) return true;
    for (const s of this.solidProps) {
      if (s.type === 'rect') {
        const cx = clamp(x, s.x, s.x + s.w);
        const cy = clamp(y, s.y, s.y + s.h);
        if (dist(x, y, cx, cy) < r) return true;
      } else if (dist(x, y, s.x, s.y) < s.r + r) return true;
    }
    return false;
  }

  moveCircle(x, y, dx, dy, r = 10) {
    let nx = x, ny = y;
    const steps = Math.ceil(Math.max(Math.abs(dx), Math.abs(dy)) / 6) || 1;
    for (let i = 0; i < steps; i++) {
      if (!this.collides(nx + dx / steps, ny, r)) nx += dx / steps;
      if (!this.collides(nx, ny + dy / steps, r)) ny += dy / steps;
    }
    return { x: nx, y: ny };
  }

  /* ---------------- navigation ---------------- */
  buildNavGrid() {
    this.cell = 32;
    this.cols = Math.ceil(this.width / this.cell);
    this.rows = Math.ceil(this.height / this.cell);
    this.nav = new Uint8Array(this.cols * this.rows);
    this.treeBuckets = new Map();
    for (const t of this.trees) {
      const key = `${Math.floor(t.x / 160)},${Math.floor(t.y / 160)}`;
      if (!this.treeBuckets.has(key)) this.treeBuckets.set(key, []);
      this.treeBuckets.get(key).push(t);
    }
    for (let cy = 0; cy < this.rows; cy++) {
      for (let cx = 0; cx < this.cols; cx++) {
        const x = cx * this.cell + this.cell / 2;
        const y = cy * this.cell + this.cell / 2;
        this.nav[cy * this.cols + cx] = this.pointWalkable(x, y) ? 1 : 0;
      }
    }
  }

  cellIndex(cx, cy) { return cy * this.cols + cx; }
  cellOpen(cx, cy) {
    if (cx < 0 || cy < 0 || cx >= this.cols || cy >= this.rows) return false;
    return this.nav[this.cellIndex(cx, cy)] === 1;
  }
  nearestCell(x, y) {
    const cx = clamp(Math.floor(x / this.cell), 0, this.cols - 1);
    const cy = clamp(Math.floor(y / this.cell), 0, this.rows - 1);
    if (this.cellOpen(cx, cy)) return { cx, cy };
    for (let r = 1; r < 9; r++) {
      for (let oy = -r; oy <= r; oy++) {
        for (let ox = -r; ox <= r; ox++) {
          if (Math.max(Math.abs(ox), Math.abs(oy)) !== r) continue;
          if (this.cellOpen(cx + ox, cy + oy)) return { cx: cx + ox, cy: cy + oy };
        }
      }
    }
    return { cx, cy };
  }

  lineClear(x1, y1, x2, y2, r = 8) {
    const d = Math.hypot(x2 - x1, y2 - y1);
    const steps = Math.max(1, Math.ceil(d / 14));
    for (let i = 1; i <= steps; i++) {
      const t = i / steps;
      if (this.collides(x1 + (x2 - x1) * t, y1 + (y2 - y1) * t, r)) return false;
    }
    return true;
  }

  pathfind(sx, sy, tx, ty) {
    const start = this.nearestCell(sx, sy);
    const end = this.nearestCell(tx, ty);
    if (start.cx === end.cx && start.cy === end.cy) {
      return this.lineClear(sx, sy, tx, ty, 7) ? [{ x: tx, y: ty }] : [];
    }
    const cols = this.cols, rows = this.rows;
    const total = cols * rows;
    const g = new Float32Array(total).fill(Infinity);
    const came = new Int32Array(total).fill(-1);
    const closed = new Uint8Array(total);
    const startIdx = this.cellIndex(start.cx, start.cy);
    const endIdx = this.cellIndex(end.cx, end.cy);
    g[startIdx] = 0;
    const heap = new MinHeap();
    const h = (cx, cy) => Math.hypot(cx - end.cx, cy - end.cy);
    heap.push(startIdx, h(start.cx, start.cy));
    const dirs = [
      [1, 0, 1], [-1, 0, 1], [0, 1, 1], [0, -1, 1],
      [1, 1, 1.414], [1, -1, 1.414], [-1, 1, 1.414], [-1, -1, 1.414]
    ];
    let guard = 0;
    while (heap.size && guard++ < 9000) {
      const idx = heap.pop();
      if (closed[idx]) continue;
      closed[idx] = 1;
      if (idx === endIdx) break;
      const cx = idx % cols;
      const cy = Math.floor(idx / cols);
      for (const [dx, dy, cost] of dirs) {
        const nx = cx + dx, ny = cy + dy;
        if (!this.cellOpen(nx, ny)) continue;
        if (dx && dy && (!this.cellOpen(cx + dx, cy) || !this.cellOpen(cx, cy + dy))) continue;
        const nIdx = this.cellIndex(nx, ny);
        if (closed[nIdx]) continue;
        const ng = g[idx] + cost;
        if (ng < g[nIdx]) {
          g[nIdx] = ng;
          came[nIdx] = idx;
          heap.push(nIdx, ng + h(nx, ny) * 1.05);
        }
      }
    }
    if (came[endIdx] === -1) return [];
    const raw = [];
    let cur = endIdx;
    while (cur !== startIdx && cur !== -1) {
      const cx = cur % cols;
      const cy = Math.floor(cur / cols);
      raw.push({ x: cx * this.cell + this.cell / 2, y: cy * this.cell + this.cell / 2 });
      cur = came[cur];
    }
    raw.reverse();
    raw.push({ x: tx, y: ty });
    raw.unshift({ x: sx, y: sy });
    // string-pull the grid path into natural walking lines
    const out = [];
    let i = 0;
    while (i < raw.length - 1) {
      let j = raw.length - 1;
      for (; j > i + 1; j--) {
        if (this.lineClear(raw[i].x, raw[i].y, raw[j].x, raw[j].y, 7)) break;
      }
      out.push(raw[j]);
      i = j;
    }
    return out;
  }

  findNearbyOpen(x, y, radius = 40, seed = 1) {
    const rng = mulberry32(seed * 977 + Math.floor(x) * 13 + Math.floor(y) * 7);
    for (let i = 0; i < 24; i++) {
      const a = rng() * Math.PI * 2;
      const d = i === 0 ? 0 : 6 + rng() * radius;
      const nx = x + Math.cos(a) * d;
      const ny = y + Math.sin(a) * d;
      if (!this.collides(nx, ny, 8)) return { x: nx, y: ny };
    }
    return { x, y };
  }

  /* ---------------- helpers ---------------- */
  location(name) { return this.locations[name]; }

  nearestLocationName(x, y) {
    let best = null, bestD = 178;
    for (const l of Object.values(this.locations)) {
      const d = dist(x, y, l.x, l.y);
      if (d < bestD) { bestD = d; best = l; }
    }
    if (best && bestD < (best.radius + 96)) return best.name;
    if (this.isWaterPoint(x, y) || Math.abs(y - this.riverCenter(x)) < this.riverHalf(x) + 80) return 'The River Fenn';
    if (y < 620) return 'The Elderwood';
    if (x < 760 && y > 980) return 'The Marle Fields';
    if (x > 1450 && y < 1050) return 'Fenn Dock';
    return 'Alderwick Green';
  }

  ambientPollen(x, y) {
    return dist(x, y, this.riverCenter(x), y) < 200;
  }
}
