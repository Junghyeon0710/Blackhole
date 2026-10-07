// 프로토타입(blackhole.html)의 물리·규칙 코드를 그대로 돌려 기준값을 만든다.
// Unity 의 PhysicsParityTests 가 같은 시나리오를 C# 이식본으로 돌려 이 값과 비교한다.
// 사용법: node Tools/Parity/parity.mjs
import { writeFileSync, mkdirSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const out = resolve(here, '..', '..', 'Assets', '_Game', 'Tests', 'EditMode', 'parity_expected.json');

// C# 이식본과 같은 계산식 (V8 의 Math.hypot 은 넘침 방지 계산이라 마지막 자리가 다를 수 있다)
Math.hypot = (x, y) => Math.sqrt(x * x + y * y);
// 표정 타이머용 난수. 물리에는 영향이 없다
let seed = 1;
Math.random = () => { seed = (seed * 16807) % 2147483647; return seed / 2147483647; };

/* ====================== 아래는 프로토타입 코드 (화면·소리·진동 호출만 뺐다) ====================== */
const TIERS = [11, 15, 20, 26, 32, 39, 47, 56, 66, 78, 92].map(r => ({ r }));
const MAX_T = TIERS.length - 1;
const value = t => (t + 1) * (t + 2) / 2;
const ARENA = 300;
const DANGER = 246;
const G = 1150;
const SUB = 8;
const DAMP = 0.9965;
const VMAX = 900;
const LAUNCH_V = 300;
const DANGER_LIMIT = 2.4;

let bodies = [];
let score = 0;
let current = 0, aimAngle = -Math.PI / 2;
let ready = true, cooldown = 0;
let state = 'play';
let combo = 0, comboTimer = 0, topTier = 0;
let bh = null, dangerLevel = 0, idCounter = 0;

function makeBody(t, x, y){
  return { id: idCounter++, t, x, y, px: x, py: y, r: TIERS[t].r, tr: TIERS[t].r, grow: 0,
           age: 0, alive: true, merging: false, dangerT: 0, happy: 0,
           blinkT: 1 + Math.random() * 4, blinking: 0 };
}
function addScore(p){ score += p; }

function step(h){
  const gx = bh ? bh.x : 0, gy = bh ? bh.y : 0, gStr = bh ? 2600 : G;
  for (const b of bodies){
    if (!b.alive) continue;
    const dx = gx - b.x, dy = gy - b.y, d = Math.hypot(dx, dy) || 1e-6;
    const gs = gStr * Math.min(1, d / 18);
    let vx = (b.x - b.px) * DAMP, vy = (b.y - b.py) * DAMP;
    const sp = Math.hypot(vx, vy), lim = VMAX * h;
    if (sp > lim){ vx *= lim / sp; vy *= lim / sp; }
    b.px = b.x; b.py = b.y;
    b.x += vx + dx / d * gs * h * h;
    b.y += vy + dy / d * gs * h * h;
    if (b.grow > 0){ b.r = Math.min(b.tr, b.r + b.grow * h); if (b.r >= b.tr) b.grow = 0; }
  }
  const merges = [];
  const n = bodies.length;
  for (let i = 0; i < n; i++){
    const a = bodies[i]; if (!a.alive) continue;
    for (let j = i + 1; j < n; j++){
      const b = bodies[j]; if (!b.alive) continue;
      const dx = b.x - a.x, dy = b.y - a.y, rr = a.r + b.r, d2 = dx * dx + dy * dy;
      if (d2 >= rr * rr) continue;
      if (a.t === b.t && !a.merging && !b.merging && !bh){
        a.merging = b.merging = true; merges.push([a, b]); continue;
      }
      const d = Math.sqrt(d2) || 1e-4, nx = dx / d, ny = dy / d, ov = (rr - d) * 0.8;
      const ma = a.r * a.r, mb = b.r * b.r, M = ma + mb;
      a.x -= nx * ov * mb / M; a.y -= ny * ov * mb / M;
      b.x += nx * ov * ma / M; b.y += ny * ov * ma / M;
    }
  }
  for (const b of bodies){
    if (!b.alive) continue;
    const d = Math.hypot(b.x, b.y);
    if (d + b.r > ARENA){ const k = (ARENA - b.r) / d; b.x *= k; b.y *= k; }
  }
  for (const [a, b] of merges) doMerge(a, b);
  if (merges.length) bodies = bodies.filter(b => b.alive);
}

function doMerge(a, b){
  a.alive = b.alive = false;
  const ma = a.r * a.r, mb = b.r * b.r, M = ma + mb;
  const x = (a.x * ma + b.x * mb) / M, y = (a.y * ma + b.y * mb) / M;
  const vx = ((a.x - a.px) * ma + (b.x - b.px) * mb) / M, vy = ((a.y - a.py) * ma + (b.y - b.py) * mb) / M;
  const nt = a.t + 1;
  combo = comboTimer > 0 ? combo + 1 : 1; comboTimer = 1.3;
  if (nt > MAX_T){ startBlackHole(x, y); return; }
  const nb = makeBody(nt, x, y);
  nb.r = Math.max(a.r, b.r); nb.grow = (nb.tr - nb.r) / 0.16;
  nb.px = x - vx * 0.5; nb.py = y - vy * 0.5; nb.age = 5; nb.happy = 0.7;
  bodies.push(nb);
  const pts = Math.round(value(nt) * (1 + (combo - 1) * 0.5));
  addScore(pts);
  if (nt > topTier) topTier = nt;
}

function startBlackHole(x, y){
  bh = { x, y, r: 12, t: 0, fade: 1, done: false };
  addScore(500);
}
function updateBlackHole(dt){
  if (!bh) return;
  bh.t += dt;
  if (!bh.done){
    bh.r = Math.min(60, 12 + bh.t * 40);
    for (const b of bodies){
      if (!b.alive) continue;
      const d = Math.hypot(b.x - bh.x, b.y - bh.y);
      if (d < bh.r + b.r * .3 || bh.t > 2.6){
        b.alive = false;
        const pts = value(b.t) * 2; addScore(pts);
      }
    }
    bodies = bodies.filter(b => b.alive);
    if (bh.t > 2.6){ bh.done = true; }
  } else {
    bh.fade -= dt * 1.4;
    if (bh.fade <= 0) bh = null;
  }
}

function launch(){
  if (state !== 'play' || !ready || bh) return false;
  const r = TIERS[current].r, L = ARENA - r - 1;
  const ux = Math.cos(aimAngle), uy = Math.sin(aimAngle);
  const b = makeBody(current, ux * L, uy * L);
  const h = 1 / 60 / SUB;
  b.px = b.x + ux * LAUNCH_V * h; b.py = b.y + uy * LAUNCH_V * h;
  bodies.push(b);
  combo = 0; comboTimer = 0;
  ready = false; cooldown = 0.45;
  return true;
}

function tick(dt){
  if (state === 'play'){
    for (let s = 0; s < SUB; s++) step(dt / SUB);
    updateBlackHole(dt);
    if (!ready){ cooldown -= dt; if (cooldown <= 0) ready = true; }
    if (comboTimer > 0) comboTimer -= dt;
    let worst = 0;
    for (const b of bodies){
      b.age += dt;
      if (b.happy > 0) b.happy -= dt;
      b.blinkT -= dt;
      if (b.blinkT < 0){ b.blinking = .13; b.blinkT = 2 + Math.random() * 4; }
      if (b.blinking > 0) b.blinking -= dt;
      const out = Math.hypot(b.x, b.y) + b.r > DANGER + 3;
      const speed = Math.hypot(b.x - b.px, b.y - b.py) * SUB * 60;
      if (out && b.age > 1.4 && speed < 140 && !bh) b.dangerT += dt; else b.dangerT = Math.max(0, b.dangerT - dt * 2);
      worst = Math.max(worst, b.dangerT);
    }
    dangerLevel = worst / DANGER_LIMIT;
    if (worst >= DANGER_LIMIT) state = 'over';
  }
}
/* ====================== 프로토타입 코드 끝 ====================== */

function reset(){
  bodies = []; score = 0; current = 0; aimAngle = -Math.PI / 2;
  ready = true; cooldown = 0; state = 'play';
  combo = 0; comboTimer = 0; topTier = 0; bh = null; dangerLevel = 0; idCounter = 0;
}

// 실수는 IEEE 비트(16진수)로 넘긴다. Unity JsonUtility 는 17자리 실수를 정확히 읽지 못해서 마지막 자리가 어긋난다.
const view = new DataView(new ArrayBuffer(8));
const bits = v => { view.setFloat64(0, v); return view.getBigUint64(0).toString(16).padStart(16, '0'); };

// 시나리오 좌표와 각도는 Unity 와 같은 "위쪽이 +y" 기준이다. 프로토타입은 화면 좌표(아래가 +y)라서 y 를 뒤집는다.
function run(sc){
  reset();
  const snapshots = [], launched = [], launchStates = [];
  for (let k = 0; k < sc.ticks; k++){
    for (const s of sc.spawns.filter(s => s.tick === k)) bodies.push(makeBody(s.tier, s.x, -s.y));
    for (const l of sc.launches.filter(l => l.tick === k)){
      current = l.tier; aimAngle = -l.angle;
      const ok = launch();
      launched.push(ok);
      // cos·sin 은 엔진마다 마지막 자리가 다를 수 있어서, 쏜 직후 상태를 그대로 넘겨 C# 쪽이 같은 값에서 출발하게 한다
      if (ok){ const b = bodies[bodies.length - 1]; launchStates.push({ x: bits(b.x), y: bits(-b.y), px: bits(b.px), py: bits(-b.py) }); }
    }
    tick(1 / 60);
    if (sc.checkpoints.includes(k + 1)){
      snapshots.push({
        tick: k + 1, score, combo, topTier, over: state === 'over', danger: bits(dangerLevel),
        blackHole: !!bh, bhX: bits(bh ? bh.x : 0), bhY: bits(bh ? -bh.y : 0), bhR: bits(bh ? bh.r : 0), bhFade: bits(bh ? bh.fade : 0),
        bodies: bodies.map(b => ({ tier: b.t, x: bits(b.x), y: bits(-b.y), r: bits(b.r) })),
      });
    }
  }
  return {
    name: sc.name, ticks: sc.ticks, checkpoints: sc.checkpoints, launched, launchStates, snapshots,
    spawns: sc.spawns.map(p => ({ tick: p.tick, tier: p.tier, x: bits(p.x), y: bits(p.y) })),
    launches: sc.launches.map(l => ({ tick: l.tick, tier: l.tier, angle: bits(l.angle) })),
  };
}

const every = (n, step) => Array.from({ length: Math.floor(n / step) }, (_, i) => (i + 1) * step);
const scenarios = [];

// 1) 발사 30번: 같은 단계를 연달아 쏴서 합체·콤보·연쇄가 나오게 한다
{
  const pattern = [0, 0, 1, 0, 0, 1, 2, 2, 3, 0, 1, 1, 2, 3, 0, 0, 1, 2, 3, 3, 0, 1, 0, 2, 1, 3, 0, 0, 2, 1];
  const launches = pattern.map((tier, i) => ({ tick: 5 + i * 30, tier, angle: (i * 2.399963229728653) % (2 * Math.PI) }));
  scenarios.push({ name: 'launches', ticks: 1200, spawns: [], launches, checkpoints: every(1200, 60) });
}

// 2) 태양 두 개 → 블랙홀, 주변 행성 흡수와 사라짐
{
  const spawns = [
    { tick: 0, tier: 10, x: -96, y: 4 }, { tick: 0, tier: 10, x: 96, y: -4 },
    { tick: 0, tier: 5, x: 0, y: 200 }, { tick: 0, tier: 3, x: 0, y: -210 }, { tick: 0, tier: 6, x: 190, y: 150 },
    { tick: 0, tier: 2, x: -200, y: -140 }, { tick: 0, tier: 7, x: -170, y: 160 },
  ];
  scenarios.push({ name: 'blackhole', ticks: 360, spawns, launches: [{ tick: 120, tier: 1, angle: 1 }], checkpoints: every(360, 15) });
}

// 3) 아무렇게나 쏘다가 판이 넘쳐 게임 오버가 나는 판. 게임 오버가 나는 시드를 찾아 쓴다
{
  const maxTicks = 4800;
  const make = s => {
    let r = s;
    const rnd = () => { r = (r * 48271) % 2147483647; return r / 2147483647; };
    const launches = [];
    for (let k = 3; k < maxTicks; k += 28)
      launches.push({ tick: k, tier: Math.floor(rnd() * 5), angle: rnd() * Math.PI * 2 });
    // 가운데를 큰 행성(서로 다른 단계라 합쳐지지 않음)으로 채워 둔다
    const spawns = [9, 8, 7, 6, 5].map((tier, i) => ({ tick: 0, tier, x: Math.cos(i * 1.3) * 40, y: Math.sin(i * 1.3) * 40 }));
    return { name: 'overflow', ticks: maxTicks, spawns, launches, checkpoints: [] };
  };
  let chosen = null;
  for (let s = 1; s < 400 && !chosen; s++){
    const sc = make(s);
    sc.checkpoints = [maxTicks];
    const r = run(sc);
    if (r.snapshots[0].over) chosen = sc;
  }
  if (!chosen) throw new Error('게임 오버가 나는 시드를 찾지 못했습니다');
  chosen.checkpoints = every(maxTicks, 120);
  scenarios.push(chosen);
}

const result = { scenarios: scenarios.map(run) };
mkdirSync(dirname(out), { recursive: true });
writeFileSync(out, JSON.stringify(result));
for (const s of result.scenarios){
  const last = s.snapshots[s.snapshots.length - 1];
  console.log(`${s.name}: 마지막 점수 ${last.score}, 행성 ${last.bodies.length}개, 게임오버 ${last.over}, 블랙홀 ${last.blackHole}`);
}
console.log('기준값 →', out);
