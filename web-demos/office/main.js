import * as THREE from 'three';
import { COLORS, MAT, createProp, createPlayer, createGhostBody, makeSign, glowSprite } from './props.js';
import {
  scene,
  world,
  statics,
  props,
  enemies,
  projectiles,
  debris,
  P,
  GAME,
  FX,
  ROOM,
  HALL,
  initAudio,
  blip,
  noise,
  addProp,
  spawnDebris,
  damageProp,
  breakProp,
  resetProps,
  spawnProjectile,
  removeProjectile,
  resolveCircleAabb,
  dist2,
  damageMul,
  addFloorGlow,
} from './core.js';
import { buildLevel, getExitSign, ZONES } from './level.js';
import { buildEnemies, resetEnemies, updateEnemies, damageEnemy } from './enemies.js';

/* =========================================================
   renderer, camera, light
   ========================================================= */

const canvas = document.getElementById('scene');
const renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
renderer.setPixelRatio(Math.min(devicePixelRatio, 2));
renderer.shadowMap.enabled = true;
renderer.shadowMap.type = THREE.PCFSoftShadowMap;

scene.background = new THREE.Color(COLORS.void);
scene.fog = new THREE.Fog(COLORS.void, 34, 80);

const CAM = {
  menu: { pos: new THREE.Vector3(14.5, 9, 1.5), target: new THREE.Vector3(-1, 1.5, 6.6), frustum: 11 },
  game: { offset: new THREE.Vector3(0, 21, 21), frustum: 22 },
};

const camera = new THREE.OrthographicCamera(-1, 1, 1, -1, 0.1, 400);
let frustum = CAM.menu.frustum;
const camTarget = CAM.menu.target.clone();
camera.position.copy(CAM.menu.pos);
camera.lookAt(camTarget);

let composer = null;

function applyFrustum() {
  const aspect = (canvas.clientWidth || innerWidth) / (canvas.clientHeight || innerHeight);
  const f = frustum * Math.min(1.3, Math.max(1, 1.45 / aspect));
  camera.left = (-f * aspect) / 2;
  camera.right = (f * aspect) / 2;
  camera.top = f / 2;
  camera.bottom = -f / 2;
  camera.updateProjectionMatrix();
}

function resize() {
  const w = canvas.clientWidth || innerWidth;
  const h = canvas.clientHeight || innerHeight;
  const pr = renderer.getPixelRatio();
  if (canvas.width !== Math.floor(w * pr) || canvas.height !== Math.floor(h * pr)) {
    renderer.setSize(w, h, false);
    if (composer) composer.setSize(w, h);
  }
  applyFrustum();
}

// night office: barely any ambient, everything reads as pools of light
const ambient = new THREE.HemisphereLight(0x5c6a84, 0x0a0a0c, 0.45);
scene.add(ambient);

const key = new THREE.DirectionalLight(0x9fb0c8, 0.5);
key.position.set(14, 30, 16);
key.castShadow = true;
key.shadow.mapSize.set(1024, 1024);
key.shadow.camera.near = 1;
key.shadow.camera.far = 110;
const SH = 30;
Object.assign(key.shadow.camera, { left: -SH, right: SH, top: SH, bottom: -SH });
key.shadow.camera.updateProjectionMatrix();
key.shadow.bias = -0.0012;
scene.add(key, key.target);

// ceiling light travelling with the hero — keeps the silhouette readable
const followLight = new THREE.SpotLight(0xcfe0ff, 110, 36, 0.75, 0.75, 1.5);
followLight.position.set(0, 13, 0);
scene.add(followLight, followLight.target);

// momentum bleeds red light into the room
const heatLight = new THREE.PointLight(COLORS.red, 0, 18, 2);
heatLight.position.set(0, 2.4, 0);
scene.add(heatLight);

/* optional bloom — the look leans on the red glow, but never block on it */
try {
  const [{ EffectComposer }, { RenderPass }, { UnrealBloomPass }, { OutputPass }] = await Promise.all([
    import('three/addons/postprocessing/EffectComposer.js'),
    import('three/addons/postprocessing/RenderPass.js'),
    import('three/addons/postprocessing/UnrealBloomPass.js'),
    import('three/addons/postprocessing/OutputPass.js'),
  ]);
  composer = new EffectComposer(renderer);
  composer.addPass(new RenderPass(scene, camera));
  composer.addPass(new UnrealBloomPass(new THREE.Vector2(innerWidth, innerHeight), 0.7, 0.5, 0.4));
  composer.addPass(new OutputPass());
} catch (err) {
  console.warn('[offboarding] bloom unavailable, plain render', err);
}

/* =========================================================
   player rig
   ========================================================= */

const player = createPlayer();
player.traverse((o) => {
  if (o.isMesh) o.castShadow = true;
});
world.add(player);
const playerBody = player.getObjectByName('body');
const playerMarker = player.getObjectByName('marker');
const armL = player.getObjectByName('armL');
const armR = player.getObjectByName('armR');
playerMarker.visible = false;

// held mice orbit the hero, cables are plain lines
const miceRig = new THREE.Group();
world.add(miceRig);
const miceVisuals = [];
for (let i = 0; i < 4; i++) {
  const m = createProp('mouse');
  m.visible = false;
  miceRig.add(m);
  const line = new THREE.Line(
    new THREE.BufferGeometry().setFromPoints([new THREE.Vector3(), new THREE.Vector3()]),
    new THREE.LineBasicMaterial({ color: 0x7a7a84 })
  );
  line.visible = false;
  miceRig.add(line);
  miceVisuals.push({ mesh: m, line });
}

// cigarette smoke
const puffs = [];
for (let i = 0; i < 12; i++) {
  const s = glowSprite(1.6, 0xb9b9c2, 0);
  s.visible = false;
  world.add(s);
  puffs.push({ mesh: s, life: 0 });
}

// reflection echo — replays what you did a couple of seconds ago
const echo = createGhostBody();
echo.visible = false;
world.add(echo);
const ECHO_DELAY = 2.2;
const echoTape = [];
let echoTimer = 0;
let pendingAttack = false;

/* =========================================================
   HUD
   ========================================================= */

const HUD = {
  title: document.getElementById('hud-title'),
  game: document.getElementById('hud-game'),
  fill: document.getElementById('momentum-fill'),
  timer: document.getElementById('hud-timer'),
  destroyed: document.getElementById('hud-destroyed'),
  hold: document.getElementById('hud-hold'),
  cigs: document.getElementById('hud-cigs'),
  dash: document.getElementById('hud-dash'),
  prompt: document.getElementById('prompt'),
  zone: document.getElementById('zone'),
  smoke: document.getElementById('smoke-overlay'),
};

function showOverlay(id) {
  document.getElementById(id).classList.remove('hidden');
}
function closeOverlays() {
  document.querySelectorAll('.overlay').forEach((o) => o.classList.add('hidden'));
}

/* =========================================================
   menu room
   ========================================================= */

const menu = {};
const menuTargets = [];
const DESK_HOME = new THREE.Vector3(-2.2, 0, 6.0);

function registerMenuTarget(object, label, action) {
  object.traverse((o) => {
    if (o.isMesh) {
      o.userData.menuAction = action;
      o.userData.menuLabel = label;
    }
  });
  menuTargets.push({ object, label, action });
}

function buildMenuRoom() {
  const deskPivot = new THREE.Group();
  deskPivot.position.copy(DESK_HOME);
  world.add(deskPivot);
  menu.deskPivot = deskPivot;

  const desk = createProp('desk');
  desk.rotation.y = Math.PI / 2;
  desk.position.set(1.4, 0, 0);
  deskPivot.add(desk);

  // desk lamp — the only warm light in the game
  const lamp = new THREE.Group();
  lamp.add(new THREE.Mesh(new THREE.CylinderGeometry(0.22, 0.26, 0.06, 10), MAT.black));
  const arm = new THREE.Mesh(new THREE.CylinderGeometry(0.04, 0.04, 1.1, 6), MAT.black);
  arm.position.set(0, 0.55, 0);
  arm.rotation.z = 0.3;
  const head = new THREE.Mesh(new THREE.ConeGeometry(0.28, 0.4, 12, 1, true), MAT.black);
  head.position.set(-0.34, 1.05, 0);
  head.rotation.z = -2.3;
  const bulb = new THREE.Mesh(new THREE.CircleGeometry(0.2, 12), MAT.lamp);
  bulb.position.set(-0.42, 0.96, 0);
  bulb.rotation.set(-0.9, -1.2, 0);
  lamp.add(arm, head, bulb);
  lamp.position.set(1.5, 0.84, -1.2);
  deskPivot.add(lamp);

  menu.lampLight = new THREE.PointLight(0xffe0b0, 30, 12, 2);
  menu.lampLight.position.set(0.6, 1.9, 5.0);
  scene.add(menu.lampLight);
  menu.lampPool = addFloorGlow(-0.6, 5.2, 8, 0xffd9a0, 0.22);

  // laptop = PLAY
  const laptop = new THREE.Group();
  const base = new THREE.Mesh(new THREE.BoxGeometry(0.9, 0.06, 0.62), MAT.black);
  base.position.y = 0.87;
  const lid = new THREE.Mesh(new THREE.BoxGeometry(0.9, 0.6, 0.05), MAT.black);
  lid.position.set(0, 1.17, -0.3);
  lid.rotation.x = -0.28;
  const glow = new THREE.Mesh(new THREE.PlaneGeometry(0.78, 0.48), MAT.glow);
  glow.position.set(0, 1.17, -0.35);
  glow.rotation.x = -0.28;
  laptop.add(base, lid, glow);
  laptop.position.set(1.6, 0, 0);
  laptop.rotation.y = -Math.PI / 2;
  deskPivot.add(laptop);
  registerMenuTarget(laptop, 'PLAY', 'play');

  const screenHalo = glowSprite(3.6, COLORS.red, 0.45);
  screenHalo.position.set(1.32, 1.2, 0);
  screenHalo.rotation.y = Math.PI / 2;
  deskPivot.add(screenHalo);

  const playLabel = makeSign('PLAY', 1.5, 0.5, { bg: '#160707', fg: '#ff3b30' });
  playLabel.material.transparent = true;
  playLabel.position.set(1.58, 1.85, 0);
  playLabel.rotation.y = Math.PI / 2;
  deskPivot.add(playLabel);
  menu.playLabel = playLabel;

  // clipboard = SETTINGS
  const folder = new THREE.Group();
  const paper = new THREE.Mesh(new THREE.BoxGeometry(0.62, 0.06, 0.86), MAT.paper);
  paper.position.y = 0.87;
  const clip = new THREE.Mesh(new THREE.BoxGeometry(0.4, 0.04, 0.12), MAT.metal);
  clip.position.set(0, 0.91, -0.34);
  folder.add(paper, clip);
  folder.position.set(1.0, 0, 1.55);
  deskPivot.add(folder);
  registerMenuTarget(folder, 'SETTINGS', 'handbook');

  deskPivot.traverse((o) => {
    if (o.isMesh) o.castShadow = true;
  });

  // EXIT door = QUIT
  const door = new THREE.Group();
  const slab = new THREE.Mesh(new THREE.BoxGeometry(1.9, 2.6, 0.16), MAT.black);
  slab.position.y = 1.3;
  door.add(slab);
  const sign = makeSign('EXIT', 1.6, 0.6, { bg: '#160707', fg: '#ff3b30' });
  sign.position.set(0, 2.95, 0.02);
  door.add(sign);
  const halo = glowSprite(3, COLORS.red, 0.35);
  halo.position.set(0, 2.95, 0.12);
  door.add(halo);
  door.position.set(4.5, 0, ROOM.startZ - 0.1);
  door.rotation.y = Math.PI;
  world.add(door);
  registerMenuTarget(door, 'QUIT', 'quit');

  // personal belongings box — the tutorial weapon
  menu.box = addProp('box', 2.9, 8.4, 0.3);
  registerMenuTarget(menu.box.group, 'YOUR THINGS', 'box');

  menu.chair = addProp('chair', 1.6, 6.0, -Math.PI / 2);

  addProp('plant', -9.4, 11.4);
  addProp('cooler', 8.6, 11.6);
  addProp('cigarettes', 1.2, 8.6);
  addProp('mouse', -0.7, 5.2);

  const floorSign = makeSign('FLOOR 7 — OPERATIONS', 3.6, 0.6, { bg: '#0b0b0d', fg: '#55524a' });
  floorSign.position.set(-1.5, 2.7, ROOM.startZ - 0.05);
  floorSign.rotation.y = Math.PI;
  world.add(floorSign);
}

/* =========================================================
   input — one contextual grab button, dash, attack, smoke
   ========================================================= */

const keys = new Set();
let grabHeldSince = 0;

const up = () => keys.has('w') || keys.has('arrowup') || keys.has('ц');
const down = () => keys.has('s') || keys.has('arrowdown') || keys.has('ы');
const left = () => keys.has('a') || keys.has('arrowleft') || keys.has('ф');
const right = () => keys.has('d') || keys.has('arrowright') || keys.has('в');

function pressGrab() {
  if (GAME.mode !== 'playing') return;
  grabHeldSince = performance.now();
  if (P.carry || P.mice > 0) throwHeld(false);
  else grabNearest();
}

function releaseGrab() {
  if (GAME.mode !== 'playing') return;
  if ((P.carry || P.mice > 0) && performance.now() - grabHeldSince > 320) throwHeld(true);
}

addEventListener('keydown', (e) => {
  const k = e.key.toLowerCase();
  if (!e.repeat) {
    if (k === 'escape') closeOverlays();
    if (k === 'r' && GAME.mode !== 'menu') restart();
    if (GAME.mode === 'menu') {
      if (k === 'enter' || k === ' ') startGame();
      if (k === 'h') showOverlay('overlay-handbook');
    } else if (GAME.mode === 'playing') {
      if (k === 'e') pressGrab();
      if (k === 'f') attack();
      if (k === ' ' || k === 'shift') dash();
      if (k === 'q') smoke();
    }
  }
  keys.add(k);
  if ([' ', 'arrowup', 'arrowdown', 'arrowleft', 'arrowright'].includes(k)) e.preventDefault();
});

addEventListener('keyup', (e) => {
  const k = e.key.toLowerCase();
  keys.delete(k);
  if (k === 'e') releaseGrab();
});

const pointer = new THREE.Vector2(-10, -10);
const raycaster = new THREE.Raycaster();
let hovered = null;

canvas.addEventListener('mousemove', (e) => {
  pointer.set((e.clientX / innerWidth) * 2 - 1, -(e.clientY / innerHeight) * 2 + 1);
});
canvas.addEventListener('contextmenu', (e) => e.preventDefault());

canvas.addEventListener('mousedown', (e) => {
  initAudio();
  if (GAME.mode === 'menu') {
    if (e.button === 0 && hovered) {
      if (hovered.action === 'play') startGame();
      if (hovered.action === 'handbook') showOverlay('overlay-handbook');
      if (hovered.action === 'quit') showOverlay('overlay-quit');
      if (hovered.action === 'box') FX.hint('the box comes with you', 2);
    }
    return;
  }
  if (GAME.mode !== 'playing') return;
  if (e.button === 0) pressGrab();
  if (e.button === 2) attack();
});

canvas.addEventListener('mouseup', (e) => {
  if (e.button === 0) releaseGrab();
});

document.getElementById('handbook-close').onclick = closeOverlays;
document.getElementById('quit-close').onclick = closeOverlays;
document.getElementById('win-restart').onclick = () => {
  closeOverlays();
  restart();
};

/* =========================================================
   grab / carry / throw
   ========================================================= */

const forwardX = () => Math.sin(P.angle);
const forwardZ = () => Math.cos(P.angle);

function handPosition() {
  return { x: P.x + forwardX() * 0.8, z: P.z + forwardZ() * 0.8 };
}

function grabNearest() {
  const hand = handPosition();
  let best = null;
  let bestD = 2.6 * 2.6;
  for (const p of props) {
    if (p.dead || p.carried || p.spec.carry === 'heavy') continue;
    const d = dist2(hand.x, hand.z, p.group.position.x, p.group.position.z);
    if (d < bestD) {
      bestD = d;
      best = p;
    }
  }

  if (!best) {
    shoveHeavy();
    return;
  }

  if (best.spec.pickup === 'cigs') {
    takeCigs(best);
    return;
  }

  if (best.type === 'mouse') {
    if (P.carry || P.mice >= 4) return;
    P.mice++;
    best.dead = true;
    best.group.visible = false;
    blip(500 + P.mice * 90, 0.08, 'square', 0.035);
    FX.hint(P.mice === 4 ? 'MOUSE BALL — E TO HURL IT' : `${P.mice}× MOUSE — F TO SWING`, 2);
    return;
  }

  if (P.mice > 0) return;
  best.carried = true;
  best.thrown = false;
  best.vx = best.vz = best.vy = 0;
  P.carry = best;
  if (best.type === 'paperReam') {
    P.paper = 12;
    FX.hint('PAPER REAM — F FIRES A VOLLEY', 2.4);
  }
  blip(300, 0.07, 'square', 0.035);
}

function takeCigs(entry) {
  P.cigs += 3;
  entry.dead = true;
  entry.group.visible = false;
  blip(760, 0.1, 'square', 0.04);
  FX.hint('CIGARETTES +3 — Q TO SMOKE', 2.2);
}

function shoveHeavy() {
  const hand = handPosition();
  for (const p of props) {
    if (p.dead || p.spec.carry !== 'heavy') continue;
    if (dist2(hand.x, hand.z, p.group.position.x, p.group.position.z) > 6.5) continue;
    damageProp(p, 2 * damageMul(), 10);
    FX.shake(0.2);
    noise(0.14, 0.06);
    return;
  }
}

function throwHeld(charged) {
  const fx = forwardX();
  const fz = forwardZ();
  const power = (charged ? 30 : 23) * damageMul() * (1 + P.heat * 0.35);

  if (P.mice > 0) {
    spawnProjectile({
      x: P.x + fx * 1.1,
      z: P.z + fz * 1.1,
      y: 1.1,
      vx: fx * power,
      vz: fz * power,
      kind: 'ball',
      power: P.mice,
      life: 2.6,
    });
    blip(200, 0.2, 'sawtooth', 0.05);
    P.mice = 0;
    FX.shake(0.2);
    return;
  }

  const c = P.carry;
  if (!c) return;
  c.carried = false;
  c.thrown = true;
  c.vx = fx * power;
  c.vz = fz * power;
  c.vy = 3.2;
  P.carry = null;
  P.paper = 0;
  FX.shake(0.18);
  blip(240, 0.14, 'square', 0.045);
}

/* =========================================================
   attacks
   ========================================================= */

function meleeSweep(radius, amount) {
  const cx = P.x + forwardX() * radius * 0.5;
  const cz = P.z + forwardZ() * radius * 0.5;
  let hits = 0;
  for (const p of props) {
    if (p.dead || p.carried) continue;
    if (dist2(cx, cz, p.group.position.x, p.group.position.z) < radius * radius) {
      if (damageProp(p, amount, 10)) hits++;
    }
  }
  for (const e of enemies) {
    if (e.dead) continue;
    if (dist2(cx, cz, e.x, e.z) < radius * radius) {
      damageEnemy(e, amount);
      hits++;
    }
  }
  if (hits) P.heat = Math.min(1, P.heat + 0.05);
  return hits;
}

function firePaperVolley() {
  const shots = Math.min(5, P.paper);
  for (let i = 0; i < shots; i++) {
    const a = P.angle + (i - (shots - 1) / 2) * 0.17;
    spawnProjectile({
      x: P.x + Math.sin(a) * 0.9,
      z: P.z + Math.cos(a) * 0.9,
      y: 1.1,
      vx: Math.sin(a) * 26,
      vz: Math.cos(a) * 26,
      kind: 'paper',
      power: 1,
      life: 1.6,
    });
  }
  P.paper -= shots;
  noise(0.1, 0.05);
  blip(880, 0.08, 'triangle', 0.03);
  if (P.paper <= 0) FX.hint('REAM EMPTY — E TO THROW IT', 2);
}

function attack() {
  if (P.attackT > 0) return;
  P.attackT = 0.28;
  pendingAttack = true;

  const mul = damageMul();

  if (P.mice > 0) {
    const radius = [0, 2.0, 2.5, 3.0, 3.4][P.mice];
    meleeSweep(radius, (P.mice >= 3 ? 2 : 1) * mul);
    blip(140 + P.mice * 40, 0.12, 'sawtooth', 0.045);
    FX.shake(0.1 + P.mice * 0.04);
    return;
  }

  if (P.carry && P.carry.type === 'paperReam' && P.paper > 0) {
    firePaperVolley();
    return;
  }

  if (P.carry) {
    meleeSweep(2.4, 2 * mul);
    if (Math.random() < 0.35) damageProp(P.carry, 1, 8);
    if (P.carry && P.carry.dead) P.carry = null;
    blip(190, 0.1, 'square', 0.045);
    FX.shake(0.14);
    return;
  }

  meleeSweep(1.7, 1 * mul);
  blip(150, 0.08, 'square', 0.035);
}

function dash() {
  if (P.dashCd > 0) return;
  P.dashCd = 0.85;
  P.dashT = 0.18;
  let dx = 0;
  let dz = 0;
  if (up()) dz -= 1;
  if (down()) dz += 1;
  if (left()) dx -= 1;
  if (right()) dx += 1;
  const len = Math.hypot(dx, dz);
  if (len > 0) {
    dx /= len;
    dz /= len;
  } else {
    dx = forwardX();
    dz = forwardZ();
  }
  P.vx = dx * 27;
  P.vz = dz * 27;
  P.heat = Math.min(1, P.heat + 0.04);
  noise(0.12, 0.05);
  blip(420, 0.12, 'triangle', 0.035);
}

function smoke() {
  if (P.cigs <= 0) {
    FX.hint('NO CIGARETTES', 1.2);
    return;
  }
  P.cigs--;
  P.smokeT = 6;
  P.crashT = 0;
  P.heat = Math.min(1, P.heat + 0.35);
  HUD.smoke.classList.add('on');
  FX.hint('SMOKE — DAMAGE UP, MOMENTUM HOLDS', 2.4);
  blip(120, 0.5, 'sine', 0.05);
}

function spawnPuff(x, z) {
  const p = puffs.find((q) => q.life <= 0);
  if (!p) return;
  p.life = 1.4;
  p.mesh.visible = true;
  p.mesh.position.set(x, 1.5, z);
  p.mesh.scale.setScalar(1);
}

/* =========================================================
   reflection echo
   ========================================================= */

function updateEcho(dt) {
  echoTimer += dt;
  if (echoTimer >= 0.05) {
    echoTimer = 0;
    echoTape.push({ t: GAME.time, x: P.x, z: P.z, a: P.angle, atk: pendingAttack, played: false });
    pendingAttack = false;
    while (echoTape.length && GAME.time - echoTape[0].t > ECHO_DELAY + 1) echoTape.shift();
  }

  const active = P.heat > 0.25 && echoTape.length > 6;
  echo.visible = active;
  if (!active) return;

  const targetT = GAME.time - ECHO_DELAY;
  let frame = null;
  for (let i = echoTape.length - 1; i >= 0; i--) {
    if (echoTape[i].t <= targetT) {
      frame = echoTape[i];
      break;
    }
  }
  if (!frame) return;
  if (!frame.played) {
    frame.played = true;
    if (frame.atk) echoAttack(frame);
  }
  echo.position.set(frame.x, 0, frame.z);
  echo.rotation.y = frame.a;
  echo.userData.mat.opacity = 0.1 + P.heat * 0.16;
}

function echoAttack(frame) {
  const cx = frame.x + Math.sin(frame.a) * 1.1;
  const cz = frame.z + Math.cos(frame.a) * 1.1;
  let hits = 0;
  // the double only finishes off what you already weakened
  for (const p of props) {
    if (p.dead || p.carried || !p.weak) continue;
    if (dist2(cx, cz, p.group.position.x, p.group.position.z) < 5.3) {
      breakProp(p, 10);
      hits++;
    }
  }
  for (const e of enemies) {
    if (e.dead) continue;
    if (dist2(cx, cz, e.x, e.z) < 4.4) {
      damageEnemy(e, 1);
      hits++;
    }
  }
  if (hits) {
    P.heat = Math.min(1, P.heat + 0.08);
    blip(660, 0.14, 'sine', 0.035);
  }
}

/* =========================================================
   flow
   ========================================================= */

function startGame() {
  if (GAME.mode !== 'menu') return;
  GAME.mode = 'transition';
  GAME.transition = 0;
  menu.playLabel.visible = false;
  hovered = null;
  canvas.classList.remove('pointer');
  HUD.prompt.classList.add('hidden');
  HUD.title.style.opacity = '0';
  initAudio();
  blip(110, 0.4, 'sawtooth', 0.08);
  noise(0.45, 0.11);
}

function litForPlay() {
  // the menu keeps the reference's darkness; the run needs to be readable
  ambient.intensity = 1.0;
  key.intensity = 0.9;
  followLight.intensity = 190;
  scene.fog.near = 40;
  scene.fog.far = 96;
}

function beginPlay() {
  GAME.mode = 'playing';
  GAME.time = 0;
  litForPlay();
  playerMarker.visible = true;
  playerBody.position.y = 0;
  playerBody.rotation.x = 0;
  HUD.game.classList.remove('hidden');
  echoTape.length = 0;

  // the box is already in your hands: first lesson is throwing it
  const b = menu.box;
  if (b && !b.dead) {
    b.carried = true;
    P.carry = b;
  }
  FX.hint('E THROWS THE BOX — SMASH THE GLASS DOOR', 4);
}

function endGame(won) {
  GAME.mode = 'over';
  document.querySelector('#overlay-win h2').textContent = won ? "YOU'RE OUT" : 'SECURITY ESCORTED YOU';
  document.getElementById('win-stats').innerHTML = won
    ? `TIME <b>${GAME.time.toFixed(1)}s</b> · DESTROYED <b>${GAME.destroyed}</b> · SEVERANCE <b>${GAME.score * 120}$</b>`
    : `Time ran out. DESTROYED <b>${GAME.destroyed}</b> · SEVERANCE <b>${GAME.score * 120}$</b>`;
  showOverlay('overlay-win');
  blip(won ? 520 : 90, 0.6, won ? 'square' : 'sawtooth', 0.07);
}

function restart() {
  closeOverlays();
  resetProps();
  resetEnemies();
  for (const p of puffs) {
    p.life = 0;
    p.mesh.visible = false;
  }
  HUD.smoke.classList.remove('on');

  P.x = 0;
  P.z = 1;
  P.vx = P.vz = 0;
  P.angle = Math.PI;
  P.heat = 0.35;
  P.invuln = 0;
  P.dashT = P.dashCd = 0;
  P.carry = null;
  P.mice = 0;
  P.paper = 0;
  P.cigs = 1;
  P.smokeT = P.crashT = 0;
  P.attackT = 0;

  GAME.destroyed = 0;
  GAME.score = 0;
  GAME.time = 0;
  GAME.shake = 0;
  menu.deskPivot.rotation.x = 0;
  menu.deskPivot.position.copy(DESK_HOME);
  menu.flipFx = true;
  echoTape.length = 0;
  echo.visible = false;
  currentZone = '';

  GAME.mode = 'playing';
  litForPlay();
  playerMarker.visible = true;
  playerBody.position.y = 0;
  playerBody.rotation.x = 0;
  HUD.game.classList.remove('hidden');
  FX.hint('AGAIN', 1.5);
}

/* =========================================================
   updates
   ========================================================= */

const clock = new THREE.Clock();
const tmpTarget = new THREE.Vector3();
const tmpScale = new THREE.Vector3();

function updateMenu(dt) {
  raycaster.setFromCamera(pointer, camera);
  const hits = raycaster.intersectObjects(menuTargets.map((t) => t.object), true);
  const hit = hits.find((h) => h.object.userData.menuAction);
  const next = hit ? menuTargets.find((t) => t.action === hit.object.userData.menuAction) : null;

  if (next !== hovered) {
    hovered = next;
    canvas.classList.toggle('pointer', !!hovered);
    if (hovered) {
      HUD.prompt.textContent = hovered.label;
      HUD.prompt.classList.remove('hidden');
      blip(660, 0.05, 'square', 0.02);
    } else {
      HUD.prompt.classList.add('hidden');
    }
  }

  for (const t of menuTargets) {
    const s = hovered === t ? 1.08 : 1;
    t.object.scale.lerp(tmpScale.set(s, s, s), 1 - Math.pow(0.001, dt));
  }

  playerBody.position.y = 0.28 + Math.sin(performance.now() * 0.004) * 0.02;
  playerBody.rotation.z = Math.sin(performance.now() * 0.0021) * 0.02;
  player.position.set(P.x, 0, P.z);
  player.rotation.y = P.angle;
  menu.playLabel.material.opacity = 0.65 + Math.sin(performance.now() * 0.006) * 0.35;
}

function updateTransition(dt) {
  GAME.transition += dt;
  const t = Math.min(1, GAME.transition / 1.5);
  const e = t * t * (3 - 2 * t);

  playerBody.position.y = 0.28 * (1 - Math.min(1, t * 3));
  playerBody.rotation.x = -0.25 * Math.max(0, 1 - t * 3);

  const f = Math.min(1, Math.max(0, (t - 0.12) / 0.5));
  menu.deskPivot.rotation.x = -f * 2.1;
  menu.deskPivot.position.y = Math.sin(f * Math.PI) * 1.2;
  menu.deskPivot.position.z = DESK_HOME.z - f * 2.4;

  if (GAME.transition > 0.2 && !menu.flipFx) {
    menu.flipFx = true;
    spawnDebris(DESK_HOME.x, DESK_HOME.z - 1.2, 10);
    FX.shake(0.8);
    noise(0.5, 0.13);
    blip(90, 0.5, 'sawtooth', 0.08);
    if (!menu.chair.dead) breakProp(menu.chair, 12);
    menu.lampLight.intensity = 8;
    menu.lampPool.material.opacity = 0.06;
  }

  const gamePos = new THREE.Vector3(P.x, 0, P.z).add(CAM.game.offset);
  camera.position.lerpVectors(CAM.menu.pos, gamePos, e);
  camTarget.lerpVectors(CAM.menu.target, new THREE.Vector3(P.x, 1, P.z), e);
  frustum = THREE.MathUtils.lerp(CAM.menu.frustum, CAM.game.frustum, e);
  applyFrustum();
  camera.lookAt(camTarget);

  P.angle = THREE.MathUtils.lerp(-Math.PI / 2, Math.PI, e);

  if (t >= 1) {
    P.heat = 0.5;
    P.cigs = 1;
    beginPlay();
  }
}

function updatePlayer(dt) {
  P.attackT = Math.max(0, P.attackT - dt);
  P.dashT = Math.max(0, P.dashT - dt);
  P.dashCd = Math.max(0, P.dashCd - dt);
  P.invuln = Math.max(0, P.invuln - dt);

  if (P.smokeT > 0) {
    P.smokeT -= dt;
    if (Math.random() < dt * 8) spawnPuff(P.x, P.z);
    if (P.smokeT <= 0) {
      P.crashT = 2.5;
      HUD.smoke.classList.remove('on');
      FX.hint('CRASH — MOMENTUM DROPS FAST', 2);
    }
  } else if (P.crashT > 0) {
    P.crashT -= dt;
  }

  let ix = 0;
  let iz = 0;
  if (up()) iz -= 1;
  if (down()) iz += 1;
  if (left()) ix -= 1;
  if (right()) ix += 1;

  const carryDrag = P.carry ? (P.carry.spec.carry === 'medium' ? 0.86 : 0.96) : 1;
  const maxSpeed = (9.5 + P.heat * 7) * carryDrag;
  const len = Math.hypot(ix, iz);

  if (P.dashT <= 0) {
    if (len > 0) {
      ix /= len;
      iz /= len;
      P.vx += ix * 46 * dt;
      P.vz += iz * 46 * dt;
    } else {
      const damp = Math.pow(0.02, dt);
      P.vx *= damp;
      P.vz *= damp;
    }
    const sp = Math.hypot(P.vx, P.vz);
    if (sp > maxSpeed) {
      P.vx = (P.vx / sp) * maxSpeed;
      P.vz = (P.vz / sp) * maxSpeed;
    }
  }

  P.x += P.vx * dt;
  P.z += P.vz * dt;
  const speed = Math.hypot(P.vx, P.vz);

  // walls and heavy props
  for (const a of statics) {
    const hit = resolveCircleAabb(P.x, P.z, P.radius, a);
    if (!hit) continue;
    const target = a.prop;
    if (target && !target.dead) {
      const ram = speed * (1 + P.heat * 0.4) + (P.dashT > 0 ? 20 : 0);
      if (ram >= target.spec.breakSpeed) {
        damageProp(target, (P.dashT > 0 ? 3 : 2) * damageMul(), ram);
        P.vx *= 0.9;
        P.vz *= 0.9;
        if (target.dead) continue;
      }
    }
    P.x += hit.nx * hit.depth;
    P.z += hit.nz * hit.depth;
    if (hit.nx) P.vx *= -0.15;
    if (hit.nz) P.vz *= -0.15;
    if (speed > 9) FX.shake(0.1);
  }

  // loose props
  for (const p of props) {
    if (p.dead || p.carried || !p.spec.dynamic) continue;
    const dx = P.x - p.group.position.x;
    const dz = P.z - p.group.position.z;
    const d = Math.hypot(dx, dz);
    const minD = P.radius + p.spec.radius;
    if (d > minD || d === 0) continue;

    if (p.spec.pickup === 'cigs') {
      takeCigs(p);
      continue;
    }

    const ram = speed * (1 + P.heat * 0.4) + (P.dashT > 0 ? 20 : 0);
    if (ram >= p.spec.breakSpeed) {
      damageProp(p, (P.dashT > 0 ? 3 : 2) * damageMul(), ram);
      P.vx *= 0.95;
      P.vz *= 0.95;
    } else {
      const nx = dx / d;
      const nz = dz / d;
      p.vx -= ((nx * speed) / p.spec.mass) * 0.3;
      p.vz -= ((nz * speed) / p.spec.mass) * 0.3;
      P.x += nx * (minD - d) * 0.6;
      P.z += nz * (minD - d) * 0.6;
      P.vx *= 0.92;
      P.vz *= 0.92;
    }
  }

  if (speed > 0.6) P.angle = Math.atan2(P.vx, P.vz);
  player.position.set(P.x, 0, P.z);
  player.rotation.y = P.angle;
  playerBody.rotation.x = -Math.min(0.24, speed * 0.012);

  const swing = P.attackT > 0 ? Math.sin((1 - P.attackT / 0.28) * Math.PI) : 0;
  armR.rotation.x = -swing * 2.2;
  armL.rotation.x = swing * 0.6;

  // momentum bleed
  let decay = 0.16;
  if (P.smokeT > 0) decay = 0.05;
  else if (P.crashT > 0) decay = 0.34;
  if (speed < 4) decay += 0.22;
  P.heat = Math.max(0, P.heat - decay * dt);
  if (speed > 12) P.heat = Math.min(1, P.heat + 0.05 * dt);

  // carried item rides in front of the hero
  if (P.carry) {
    if (P.carry.dead) P.carry = null;
    else {
      const hand = handPosition();
      P.carry.group.position.set(hand.x, 1.0, hand.z);
      P.carry.group.rotation.y = P.angle;
    }
  }

  // mice orbit and drag their cables along
  for (let i = 0; i < 4; i++) {
    const v = miceVisuals[i];
    const on = i < P.mice;
    v.mesh.visible = on;
    v.line.visible = on;
    if (!on) continue;
    const a = performance.now() * 0.006 + (i * Math.PI * 2) / Math.max(1, P.mice);
    const r = 1.0 + P.mice * 0.12;
    const mx = P.x + Math.sin(a) * r;
    const mz = P.z + Math.cos(a) * r;
    v.mesh.position.set(mx, 0.9, mz);
    v.mesh.rotation.y = a;
    v.line.geometry.setFromPoints([
      new THREE.Vector3(P.x, 1.15, P.z),
      new THREE.Vector3(mx, 1.0, mz),
    ]);
  }

  if (P.z < HALL.endZ + 1.5 && Math.abs(P.x) < 3.2) endGame(true);
}

function updateProps(dt) {
  for (const p of props) {
    if (p.dead || p.carried || !p.spec.dynamic) continue;
    const rest = p.spec.y || 0;
    const speed = Math.hypot(p.vx, p.vz);
    const airborne = p.group.position.y > rest + 0.01 || p.vy !== 0;
    if (speed < 0.05 && !airborne) {
      p.vx = p.vz = 0;
      continue;
    }

    p.group.position.x += p.vx * dt;
    p.group.position.z += p.vz * dt;

    if (airborne) {
      p.vy -= 26 * dt;
      p.group.position.y += p.vy * dt;
      if (p.group.position.y <= 0) {
        p.group.position.y = 0;
        p.vy = 0;
        p.vx *= 0.5;
        p.vz *= 0.5;
        if (p.thrown && speed > 10) damageProp(p, 1, speed);
        p.thrown = false;
      }
      p.group.rotation.x += dt * 6;
    } else {
      p.group.rotation.y += speed * dt * 0.5;
    }

    const damp = Math.pow(speed > 12 ? 0.6 : 0.06, dt);
    p.vx *= damp;
    p.vz *= damp;

    // fast props wreck what they touch
    if (speed > 8) {
      const px = p.group.position.x;
      const pz = p.group.position.z;
      for (const other of props) {
        if (other === p || other.dead || other.carried) continue;
        const r = (other.spec.radius || Math.max(...other.spec.half)) + (p.spec.radius || 0.5);
        if (dist2(px, pz, other.group.position.x, other.group.position.z) < r * r) {
          if (damageProp(other, 2, speed)) {
            p.vx *= 0.7;
            p.vz *= 0.7;
          }
        }
      }
      for (const e of enemies) {
        if (e.dead) continue;
        if (dist2(px, pz, e.x, e.z) < 1.6) {
          damageEnemy(e, 2);
          p.vx *= 0.5;
          p.vz *= 0.5;
        }
      }
    }

    const bx = p.group.position.z > ROOM.doorZ ? ROOM.maxX : HALL.maxX;
    p.group.position.x = THREE.MathUtils.clamp(p.group.position.x, -bx + 0.6, bx - 0.6);
    p.group.position.z = THREE.MathUtils.clamp(p.group.position.z, HALL.endZ + 0.6, ROOM.startZ - 0.6);
  }
}

function updateProjectiles(dt) {
  for (let i = projectiles.length - 1; i >= 0; i--) {
    const pr = projectiles[i];
    pr.life -= dt;
    pr.x += pr.vx * dt;
    pr.z += pr.vz * dt;
    pr.mesh.position.set(pr.x, pr.y, pr.z);
    pr.mesh.rotation.z += pr.spin * dt;
    if (pr.kind === 'ball') pr.mesh.rotation.x += 12 * dt;

    let done = pr.life <= 0;

    if (pr.hostile) {
      if (dist2(pr.x, pr.z, P.x, P.z) < 0.9 && P.invuln <= 0 && P.dashT <= 0) {
        P.invuln = 0.8;
        P.heat *= 0.6;
        FX.hit();
        FX.shake(0.3);
        blip(90, 0.2, 'sawtooth', 0.05);
        done = true;
      }
    } else {
      for (const e of enemies) {
        if (e.dead) continue;
        if (dist2(pr.x, pr.z, e.x, e.z) < 1.3) {
          damageEnemy(e, pr.kind === 'ball' ? pr.power : 1);
          done = true;
          break;
        }
      }
      if (!done) {
        for (const p of props) {
          if (p.dead || p.carried) continue;
          const r = (p.spec.radius || Math.max(...p.spec.half)) + 0.3;
          if (dist2(pr.x, pr.z, p.group.position.x, p.group.position.z) < r * r) {
            damageProp(p, pr.kind === 'ball' ? pr.power * 2 : 1, 12);
            if (pr.kind !== 'ball') done = true;
            break;
          }
        }
      }
    }

    if (Math.abs(pr.x) > 18 || pr.z < HALL.endZ - 2 || pr.z > ROOM.startZ + 2) done = true;
    if (done) removeProjectile(i);
  }
}

function updateDebris(dt) {
  for (let i = debris.length - 1; i >= 0; i--) {
    const d = debris[i];
    d.life -= dt;
    d.vy -= 26 * dt;
    d.mesh.position.x += d.vx * dt;
    d.mesh.position.y += d.vy * dt;
    d.mesh.position.z += d.vz * dt;
    d.mesh.rotation.x += d.rx * dt;
    d.mesh.rotation.y += d.ry * dt;
    if (d.mesh.position.y < 0.08) {
      d.mesh.position.y = 0.08;
      d.vy *= -0.35;
      d.vx *= 0.6;
      d.vz *= 0.6;
      d.rx *= 0.4;
      d.ry *= 0.4;
    }
    if (d.life <= 0) {
      world.remove(d.mesh);
      d.mesh.geometry.dispose();
      debris.splice(i, 1);
    }
  }
}

function updatePuffs(dt) {
  for (const p of puffs) {
    if (p.life <= 0) continue;
    p.life -= dt;
    p.mesh.position.y += dt * 0.8;
    p.mesh.scale.setScalar(1 + (1.4 - p.life));
    p.mesh.material.opacity = Math.max(0, p.life / 1.4) * 0.22;
    p.mesh.quaternion.copy(camera.quaternion);
    if (p.life <= 0) p.mesh.visible = false;
  }
}

let currentZone = '';
function updateZoneBanner() {
  let name = 'YOUR DESK';
  for (const z of ZONES) if (P.z <= z.z) name = z.name;
  if (name !== currentZone) {
    currentZone = name;
    HUD.zone.textContent = name;
    HUD.zone.classList.remove('show');
    void HUD.zone.offsetWidth;
    HUD.zone.classList.add('show');
  }
}

function updateCamera(dt) {
  const lookX = THREE.MathUtils.clamp(P.x + P.vx * 0.26, -4.5, 4.5);
  const lookZ = THREE.MathUtils.clamp(P.z + P.vz * 0.26, HALL.endZ + 6, ROOM.startZ - 3);
  tmpTarget.set(lookX, 1, lookZ);
  camTarget.lerp(tmpTarget, 1 - Math.pow(0.0015, dt));

  const desired = new THREE.Vector3(camTarget.x, 0, camTarget.z).add(CAM.game.offset);
  camera.position.lerp(desired, 1 - Math.pow(0.0015, dt));

  if (GAME.shake > 0) {
    GAME.shake = Math.max(0, GAME.shake - dt * 2.2);
    camera.position.x += (Math.random() - 0.5) * GAME.shake;
    camera.position.y += (Math.random() - 0.5) * GAME.shake;
    camera.position.z += (Math.random() - 0.5) * GAME.shake;
  }

  frustum += (CAM.game.frustum + P.heat * 3 - frustum) * Math.min(1, dt * 3);
  applyFrustum();
  camera.lookAt(camTarget);

  key.position.set(P.x + 16, 32, P.z + 18);
  key.target.position.set(P.x, 0, P.z);
  followLight.position.set(P.x, 13, P.z + 1);
  followLight.target.position.set(P.x, 0, P.z);
  heatLight.position.set(P.x, 2.2, P.z);
  heatLight.intensity = P.heat * 26 + (P.smokeT > 0 ? 12 : 0);
}

function updateHUD(dt) {
  HUD.fill.style.width = `${P.heat * 100}%`;
  HUD.fill.classList.toggle('hot', P.heat > 0.75);

  const left_ = Math.max(0, GAME.limit - GAME.time);
  HUD.timer.textContent = `${String(Math.floor(left_ / 60)).padStart(2, '0')}:${String(
    Math.floor(left_ % 60)
  ).padStart(2, '0')}`;
  HUD.timer.classList.toggle('warn', left_ < 20);
  HUD.destroyed.textContent = `DESTROYED ${GAME.destroyed}`;
  HUD.cigs.textContent = `CIGS ${P.cigs}`;

  let hold = '—';
  if (P.mice > 0) hold = `${P.mice}× MOUSE`;
  else if (P.carry) hold = P.carry.type === 'paperReam' ? `PAPER ${P.paper}` : P.carry.type.toUpperCase();
  HUD.hold.textContent = hold;
  HUD.dash.style.opacity = P.dashCd > 0 ? 0.25 : 1;

  if (GAME.hintT > 0) {
    GAME.hintT -= dt;
    HUD.prompt.textContent = GAME.hint;
    HUD.prompt.classList.remove('hidden');
  } else {
    HUD.prompt.classList.add('hidden');
  }
}

/* =========================================================
   loop
   ========================================================= */

function tick() {
  requestAnimationFrame(tick);
  const dt = Math.min(0.05, clock.getDelta());
  resize();

  if (GAME.mode === 'menu') {
    updateMenu(dt);
    camera.position.copy(CAM.menu.pos);
    camera.position.x += Math.sin(performance.now() * 0.0004) * 0.4;
    camera.lookAt(camTarget);
  } else if (GAME.mode === 'transition') {
    updateTransition(dt);
    updateDebris(dt);
  } else if (GAME.mode === 'playing') {
    GAME.time += dt;
    updatePlayer(dt);
    updateProps(dt);
    updateEnemies(dt);
    updateProjectiles(dt);
    updateDebris(dt);
    updatePuffs(dt);
    updateEcho(dt);
    updateZoneBanner();
    updateCamera(dt);
    updateHUD(dt);
    if (GAME.time >= GAME.limit) endGame(false);
  } else {
    updateDebris(dt);
    updateProps(dt);
    updateCamera(dt);
  }

  const exitSign = getExitSign();
  if (exitSign) exitSign.material.opacity = 0.55 + Math.abs(Math.sin(performance.now() * 0.003)) * 0.45;

  if (composer) composer.render();
  else renderer.render(scene, camera);
}

/* =========================================================
   boot
   ========================================================= */

buildLevel();
buildMenuRoom();
buildEnemies();

player.position.set(P.x, 0, P.z);
player.rotation.y = P.angle;
playerBody.position.y = 0.28;
playerBody.rotation.x = -0.25;

addEventListener('resize', resize);
resize();
tick();
