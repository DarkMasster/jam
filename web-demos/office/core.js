import * as THREE from 'three';
import { MAT, PROP_SPECS, createProp, makeBroken, glowSprite } from './props.js';

/* =========================================================
   shared scene + mutable game state
   every other module talks to the game through this file
   ========================================================= */

export const scene = new THREE.Scene();
export const world = new THREE.Group();
scene.add(world);

export const statics = []; // { x, z, hw, hd, prop? }
export const props = []; // destructible / carryable entries
export const enemies = []; // roombas, drones, boss
export const projectiles = []; // paper, discs, thrown mice ball
export const debris = [];

export const ROOM = { minX: -11, maxX: 11, startZ: 13, doorZ: -2 };
export const HALL = { minX: -16, maxX: 16, endZ: -86 };

export const P = {
  x: 1.5,
  z: 6.0,
  vx: 0,
  vz: 0,
  angle: -Math.PI / 2,
  radius: 0.55,
  heat: 0,
  invuln: 0,
  // dash
  dashT: 0,
  dashCd: 0,
  // inventory
  carry: null, // prop entry in hand
  mice: 0,
  paper: 0,
  cigs: 0,
  // smoke buff
  smokeT: 0,
  crashT: 0,
  attackT: 0,
};

export const GAME = {
  mode: 'menu', // menu | transition | playing | over
  time: 0,
  limit: 90,
  destroyed: 0,
  score: 0,
  shake: 0,
  hint: '',
  hintT: 0,
};

export const FX = {
  shake(v) {
    GAME.shake = Math.min(0.9, GAME.shake + v);
  },
  hit() {
    const el = document.getElementById('flash');
    if (!el) return;
    el.classList.remove('hit');
    void el.offsetWidth;
    el.classList.add('hit');
  },
  hint(text, seconds = 3) {
    GAME.hint = text;
    GAME.hintT = seconds;
  },
};

export const damageMul = () => (P.smokeT > 0 ? 1.45 : 1);

/* =========================================================
   audio — tiny synth, no assets
   ========================================================= */

let actx = null;
export function initAudio() {
  if (!actx) actx = new (window.AudioContext || window.webkitAudioContext)();
  if (actx.state === 'suspended') actx.resume();
}

export function blip(freq, dur, type = 'square', gain = 0.05) {
  if (!actx) return;
  const o = actx.createOscillator();
  const g = actx.createGain();
  o.type = type;
  o.frequency.setValueAtTime(freq, actx.currentTime);
  o.frequency.exponentialRampToValueAtTime(Math.max(20, freq * 0.4), actx.currentTime + dur);
  g.gain.setValueAtTime(gain, actx.currentTime);
  g.gain.exponentialRampToValueAtTime(0.0001, actx.currentTime + dur);
  o.connect(g).connect(actx.destination);
  o.start();
  o.stop(actx.currentTime + dur);
}

export function noise(dur = 0.18, gain = 0.08) {
  if (!actx) return;
  const len = Math.floor(actx.sampleRate * dur);
  const buf = actx.createBuffer(1, len, actx.sampleRate);
  const d = buf.getChannelData(0);
  for (let i = 0; i < len; i++) d[i] = (Math.random() * 2 - 1) * (1 - i / len);
  const src = actx.createBufferSource();
  src.buffer = buf;
  const g = actx.createGain();
  g.gain.value = gain;
  src.connect(g).connect(actx.destination);
  src.start();
}

/* =========================================================
   geometry helpers
   ========================================================= */

export function resolveCircleAabb(cx, cz, r, a) {
  const dx = cx - a.x;
  const dz = cz - a.z;
  const ox = a.hw + r - Math.abs(dx);
  const oz = a.hd + r - Math.abs(dz);
  if (ox <= 0 || oz <= 0) return null;
  if (ox < oz) return { nx: Math.sign(dx) || 1, nz: 0, depth: ox };
  return { nx: 0, nz: Math.sign(dz) || 1, depth: oz };
}

export const dist2 = (ax, az, bx, bz) => (ax - bx) ** 2 + (az - bz) ** 2;

/* =========================================================
   walls
   ========================================================= */

export function addWall(cx, cz, hw, hd, height = 3.2, visible = true) {
  statics.push({ x: cx, z: cz, hw, hd });
  if (!visible) return null;
  const m = new THREE.Mesh(
    new THREE.BoxGeometry(hw * 2, height, hd * 2),
    height < 1 ? MAT.metal : MAT.wall
  );
  m.position.set(cx, height / 2, cz);
  m.castShadow = height > 1;
  m.receiveShadow = true;
  world.add(m);
  return m;
}

/* =========================================================
   props
   ========================================================= */

export function addProp(type, x, z, rotY = 0) {
  const spec = PROP_SPECS[type];
  if (!spec) throw new Error(`unknown prop: ${type}`);
  const intact = createProp(type);
  const broken = makeBroken(intact);

  const group = new THREE.Group();
  group.position.set(x, spec.y || 0, z);
  group.rotation.y = rotY;
  group.add(intact, broken);
  group.traverse((o) => {
    if (o.isMesh) o.castShadow = true;
  });
  world.add(group);

  const entry = {
    type,
    spec,
    group,
    intact,
    broken,
    x,
    z,
    rotY,
    vx: 0,
    vz: 0,
    vy: 0,
    y: spec.y || 0,
    hp: spec.hp || 1,
    weak: false,
    dead: false,
    carried: false,
    thrown: false,
  };
  props.push(entry);

  if (!spec.dynamic) {
    const rotated = Math.abs(Math.sin(rotY)) > 0.5;
    entry.aabb = {
      x,
      z,
      hw: rotated ? spec.half[1] : spec.half[0],
      hd: rotated ? spec.half[0] : spec.half[1],
      prop: entry,
    };
    statics.push(entry.aabb);
  }
  return entry;
}

export function spawnDebris(x, z, count, mat = MAT.metal, y = 0.5) {
  for (let i = 0; i < count; i++) {
    const s = 0.1 + Math.random() * 0.2;
    const m = new THREE.Mesh(new THREE.BoxGeometry(s, s, s), mat);
    m.position.set(x + (Math.random() - 0.5), y + Math.random() * 0.7, z + (Math.random() - 0.5));
    m.castShadow = true;
    world.add(m);
    debris.push({
      mesh: m,
      vx: (Math.random() - 0.5) * 9,
      vy: 3 + Math.random() * 5,
      vz: (Math.random() - 0.5) * 9,
      rx: (Math.random() - 0.5) * 12,
      ry: (Math.random() - 0.5) * 12,
      life: 6,
    });
  }
  while (debris.length > 300) {
    const d = debris.shift();
    world.remove(d.mesh);
    d.mesh.geometry.dispose();
  }
}

function detachStatic(entry) {
  if (!entry.aabb) return;
  const i = statics.indexOf(entry.aabb);
  if (i >= 0) statics.splice(i, 1);
}

/**
 * Deal damage to a prop. Anything that survives becomes "weak" — the
 * reflection echo can finish weakened props off.
 * Returns true when the prop broke.
 */
export function damageProp(entry, amount = 1, impact = 8) {
  if (!entry || entry.dead || entry.spec.breakSpeed >= 99) return false;
  entry.hp -= amount;
  if (entry.hp > 0) {
    if (!entry.weak) {
      entry.weak = true;
      entry.group.rotation.z += (Math.random() - 0.5) * 0.14;
      noise(0.07, 0.04);
    }
    return false;
  }
  breakProp(entry, impact);
  return true;
}

export function breakProp(entry, impact = 8) {
  if (!entry || entry.dead) return;
  entry.dead = true;
  entry.carried = false;
  entry.intact.visible = false;
  entry.broken.visible = true;
  entry.group.rotation.z = (Math.random() - 0.5) * 0.4;
  entry.group.position.y = 0;
  detachStatic(entry);

  const glass = entry.type === 'glassPanel';
  spawnDebris(entry.group.position.x, entry.group.position.z, glass ? 14 : 7, glass ? MAT.glass : MAT.metal);

  P.heat = Math.min(1, P.heat + entry.spec.heat);
  GAME.destroyed++;
  GAME.score += entry.spec.score;
  FX.shake(0.14 + entry.spec.heat);

  noise(glass ? 0.3 : 0.16, glass ? 0.09 : 0.07);
  blip(glass ? 900 : 180 + impact * 6, 0.12, glass ? 'triangle' : 'square', 0.04);
}

export function resetProps() {
  for (const p of props) {
    p.dead = false;
    p.weak = false;
    p.carried = false;
    p.thrown = false;
    p.hp = p.spec.hp || 1;
    p.intact.visible = true;
    p.broken.visible = false;
    p.group.rotation.set(0, p.rotY, 0);
    p.group.position.set(p.x, p.spec.y || 0, p.z);
    p.y = p.spec.y || 0;
    p.vx = p.vz = p.vy = 0;
    if (p.aabb && !statics.includes(p.aabb)) statics.push(p.aabb);
  }
  for (const d of debris) world.remove(d.mesh);
  debris.length = 0;
  for (const pr of projectiles) world.remove(pr.mesh);
  projectiles.length = 0;
}

/* =========================================================
   projectiles — paper sheets, boss discs, thrown mice ball
   ========================================================= */

export function spawnProjectile({ x, z, y = 1.0, vx, vz, kind = 'paper', hostile = false, power = 1, life = 2.4 }) {
  let mesh;
  if (kind === 'paper') {
    mesh = new THREE.Mesh(new THREE.PlaneGeometry(0.38, 0.5), MAT.paper);
    mesh.rotation.x = -Math.PI / 2.6;
  } else if (kind === 'disc') {
    mesh = new THREE.Mesh(new THREE.CylinderGeometry(0.22, 0.22, 0.06, 12), MAT.glow);
    mesh.rotation.x = Math.PI / 2;
  } else {
    mesh = new THREE.Mesh(new THREE.IcosahedronGeometry(0.42), MAT.metal);
  }
  mesh.position.set(x, y, z);
  mesh.castShadow = true;
  world.add(mesh);
  const pr = { mesh, x, z, y, vx, vz, kind, hostile, power, life, spin: Math.random() * 10 };
  projectiles.push(pr);
  return pr;
}

export function removeProjectile(i) {
  const pr = projectiles[i];
  world.remove(pr.mesh);
  projectiles.splice(i, 1);
}

/* =========================================================
   floor light pools — cheap fake of lamps bleeding onto the floor
   ========================================================= */

export function addFloorGlow(x, z, size, color, opacity = 0.4) {
  const g = glowSprite(size, color, opacity);
  g.rotation.x = -Math.PI / 2;
  g.position.set(x, 0.02, z);
  world.add(g);
  return g;
}
