import * as THREE from 'three';
import {
  MAT,
  COLORS,
  PROP_SPECS,
  createProp,
  createPlayer,
  createGhostBody,
  makeBroken,
  makeSign,
} from './props.js';

/* =========================================================
   core setup
   ========================================================= */

const canvas = document.getElementById('scene');
const renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
renderer.setPixelRatio(Math.min(devicePixelRatio, 2));
renderer.shadowMap.enabled = true;
renderer.shadowMap.type = THREE.PCFSoftShadowMap;

const scene = new THREE.Scene();
scene.background = new THREE.Color(0xe9e9e6);
scene.fog = new THREE.Fog(0xe9e9e6, 60, 130);

const CAM = {
  menu: { pos: new THREE.Vector3(14.5, 9, 1.5), target: new THREE.Vector3(-1, 1.5, 6.6), frustum: 11 },
  game: { offset: new THREE.Vector3(0, 21, 21), frustum: 21 },
};

const camera = new THREE.OrthographicCamera(-1, 1, 1, -1, 0.1, 300);
let frustum = CAM.menu.frustum;
const camTarget = CAM.menu.target.clone();
camera.position.copy(CAM.menu.pos);
camera.lookAt(camTarget);

function resize() {
  const w = canvas.clientWidth || innerWidth;
  const h = canvas.clientHeight || innerHeight;
  if (canvas.width !== w * renderer.getPixelRatio() || canvas.height !== h * renderer.getPixelRatio()) {
    renderer.setSize(w, h, false);
  }
  applyFrustum();
}

function applyFrustum() {
  const aspect = (canvas.clientWidth || innerWidth) / (canvas.clientHeight || innerHeight);
  // widen the view a little on narrow windows so the level still reads
  const f = frustum * Math.min(1.3, Math.max(1, 1.45 / aspect));
  camera.left = (-f * aspect) / 2;
  camera.right = (f * aspect) / 2;
  camera.top = f / 2;
  camera.bottom = -f / 2;
  camera.updateProjectionMatrix();
}

scene.add(new THREE.HemisphereLight(0xffffff, 0x9a9a95, 1.05));
const sun = new THREE.DirectionalLight(0xffffff, 1.35);
sun.position.set(14, 30, 16);
sun.castShadow = true;
sun.shadow.mapSize.set(1024, 1024);
sun.shadow.camera.near = 1;
sun.shadow.camera.far = 110;
const SH = 34;
Object.assign(sun.shadow.camera, { left: -SH, right: SH, top: SH, bottom: -SH });
sun.shadow.camera.updateProjectionMatrix();
sun.shadow.bias = -0.0012;
scene.add(sun);
scene.add(sun.target);

/* =========================================================
   world
   ========================================================= */

const world = new THREE.Group();
scene.add(world);

const statics = []; // { x, z, hw, hd, prop? }
const props = []; // destructible entries
const drones = [];
const debris = [];
const afterimages = [];

const ROOM = { minX: -11, maxX: 11, startZ: 13, doorZ: -2 };
const HALL = { minX: -16, maxX: 16, endZ: -72 };

function addFloor() {
  const geo = new THREE.PlaneGeometry(40, 96);
  const floor = new THREE.Mesh(geo, MAT.white);
  floor.rotation.x = -Math.PI / 2;
  floor.position.set(0, 0, -28);
  floor.receiveShadow = true;
  world.add(floor);

  // carpet strip guiding to the exit
  const carpet = new THREE.Mesh(
    new THREE.PlaneGeometry(5, 92),
    new THREE.MeshLambertMaterial({ color: COLORS.light })
  );
  carpet.rotation.x = -Math.PI / 2;
  carpet.position.set(0, 0.01, -28);
  carpet.receiveShadow = true;
  world.add(carpet);
}

function addWall(cx, cz, hw, hd, height = 3, visible = true) {
  statics.push({ x: cx, z: cz, hw, hd });
  if (!visible) return;
  const m = new THREE.Mesh(
    new THREE.BoxGeometry(hw * 2, height, hd * 2),
    height < 1 ? MAT.mid : MAT.light
  );
  m.position.set(cx, height / 2, cz);
  m.castShadow = height > 1;
  m.receiveShadow = true;
  world.add(m);
}

function buildShell() {
  // start room
  addWall(ROOM.minX - 0.2, 5.5, 0.2, 7.7, 3.2); // sticker wall (left)
  addWall(ROOM.maxX + 0.2, 5.5, 0.2, 7.7, 0.3); // curb (right, near menu camera)
  addWall(0, ROOM.startZ + 0.2, 11.2, 0.2, 3.2); // back wall with EXIT door
  addWall(-7.25, ROOM.doorZ, 3.75, 0.25, 3.0);
  addWall(7.25, ROOM.doorZ, 3.75, 0.25, 3.0);

  // office hall
  addWall(HALL.minX - 0.2, -37, 0.2, 35.2, 3.2);
  addWall(HALL.maxX + 0.2, -37, 0.2, 35.2, 3.2);
  addWall(-12.5, ROOM.doorZ, 3.5, 0.25, 3.0);
  addWall(12.5, ROOM.doorZ, 3.5, 0.25, 3.0);
  addWall(-9.75, HALL.endZ - 0.2, 6.25, 0.2, 3.2);
  addWall(9.75, HALL.endZ - 0.2, 6.25, 0.2, 3.2);
}

/* ---------- narrative wall ---------- */

const WALL_TEXTS = [
  ['REPLACEABLE', true],
  ['AI CAN DO IT FASTER', false],
  ['TRAIN YOUR REPLACEMENT', false],
  ['LLM READY', true],
  ['COST CUTTING', false],
  ['YOUR ROLE IS EVOLVING', false],
  ['HEADCOUNT REDUCTION', true],
  ['DO MORE WITH LESS', false],
  ['THE FUTURE IS AUTOMATED', false],
  ['SYNERGY', false],
  ['Q3 OPTIMIZATION', false],
  ['THANK YOU FOR YOUR SERVICE', true],
];

function buildNarrativeWall() {
  const x = ROOM.minX + 0.02;
  let i = 0;
  for (let row = 0; row < 3; row++) {
    for (let col = 0; col < 4; col++) {
      const [text, red] = WALL_TEXTS[i++ % WALL_TEXTS.length];
      const sign = makeSign(text, 1.7, 0.88, {
        bg: red ? '#b3241d' : '#f4f3ef',
        fg: red ? '#f4f3ef' : '#121212',
      });
      sign.rotation.y = Math.PI / 2;
      sign.rotation.z = (Math.random() - 0.5) * 0.14;
      sign.position.set(x, 2.6 - row * 1.0, 11.4 - col * 2.2 + (Math.random() - 0.5) * 0.2);
      world.add(sign);
    }
  }

  const memo = makeSign('NOTICE OF ROLE ELIMINATION', 3.0, 1.4, {
    bg: '#f4f3ef',
    fg: '#b3241d',
  });
  memo.rotation.y = Math.PI / 2;
  memo.rotation.z = -0.05;
  memo.position.set(x, 1.5, 0.9);
  world.add(memo);
}

/* ---------- destructible props ---------- */

function addProp(type, x, z, rotY = 0) {
  const spec = PROP_SPECS[type];
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
    vx: 0,
    vz: 0,
    broken_: false,
    spin: 0,
  };
  props.push(entry);
  if (!spec.dynamic) {
    const rot = Math.abs(Math.sin(rotY)) > 0.5;
    entry.aabb = {
      x,
      z,
      hw: rot ? spec.half[1] : spec.half[0],
      hd: rot ? spec.half[0] : spec.half[1],
      prop: entry,
    };
    statics.push(entry.aabb);
  }
  return entry;
}

function buildOffice() {
  // desk pods
  const podZ = [-9, -21, -33, -45, -59];
  for (const z of podZ) {
    for (const side of [-1, 1]) {
      const x = side * (7 + Math.random() * 3);
      addProp('desk', x, z, Math.PI / 2);
      addProp('monitor', x + side * 0.2, z - 0.5, -side * (Math.PI / 2));
      addProp('monitor', x + side * 0.2, z + 0.7, -side * (Math.PI / 2));
      addProp('chair', x - side * 1.5, z - 0.4);
      addProp('chair', x - side * 1.5, z + 1.1);
    }
  }

  // glass partitions across the floor — the momentum gates
  for (const z of [-15, -27, -39, -51, -65]) {
    for (const x of [-13, -7.8, -2.6, 2.6, 7.8, 13]) addProp('glassPanel', x, z, 0);
  }

  // meeting room dressing
  addProp('whiteboard', -4.5, -36, 0.4);
  addProp('chair', -2, -30.5);
  addProp('chair', 2, -31.2);
  addProp('desk', 0, -30.8, 0);

  // server room
  for (let i = 0; i < 4; i++) {
    addProp('serverRack', -13.5 + i * 1.9, -55, 0);
    addProp('serverRack', 13.5 - i * 1.9, -55, 0);
  }

  // scatter
  const scatter = [
    ['cooler', -15, -12],
    ['cooler', 15, -42],
    ['cooler', -15, -62],
    ['plant', 14.6, -7],
    ['plant', -14.6, -24],
    ['plant', 14.6, -60],
    ['plant', -14.6, -48],
    ['extinguisher', -15.6, -35],
    ['extinguisher', 15.6, -22],
    ['extinguisher', 15.6, -68],
    ['whiteboard', 11, -25, 1.2],
    ['whiteboard', -11, -62, -0.8],
    ['chair', 0.5, -13],
    ['chair', -2.4, -24],
    ['chair', 1.8, -47],
    ['chair', -1.4, -57],
    ['monitor', 2.2, -19, 0.7],
    ['monitor', -2.6, -43, -0.9],
  ];
  for (const [type, x, z, r] of scatter) addProp(type, x, z, r || 0);

  // reception near the exit
  addProp('desk', -3.4, -68, 0);
  addProp('desk', 3.4, -68, 0);
  addProp('plant', -6, -70);
  addProp('plant', 6, -70);
}

/* ---------- exit ---------- */

let exitSign;
function buildExit() {
  const frame = new THREE.Mesh(new THREE.BoxGeometry(6.4, 3.2, 0.3), MAT.black);
  frame.position.set(0, 1.6, HALL.endZ - 0.1);
  world.add(frame);
  const light = new THREE.Mesh(new THREE.PlaneGeometry(5.6, 2.6), MAT.redGlow);
  light.position.set(0, 1.4, HALL.endZ + 0.1);
  world.add(light);

  exitSign = makeSign('EXIT', 3.4, 1.1, { bg: '#121212', fg: '#b3241d' });
  exitSign.position.set(0, 3.6, HALL.endZ + 0.2);
  world.add(exitSign);

  const arrow = makeSign('THIS WAY OUT', 5, 1.0, { bg: '#e9e9e6', fg: '#b3241d' });
  arrow.rotation.x = -Math.PI / 2;
  arrow.position.set(0, 0.03, HALL.endZ + 14);
  world.add(arrow);
}

/* ---------- menu room ---------- */

const menu = {};
const menuTargets = [];

const DESK_HOME = new THREE.Vector3(-2.2, 0, 6.0);

function buildMenuRoom() {
  const deskPivot = new THREE.Group();
  deskPivot.position.copy(DESK_HOME);
  world.add(deskPivot);
  menu.deskPivot = deskPivot;

  const desk = createProp('desk');
  desk.rotation.y = Math.PI / 2;
  desk.position.set(1.4, 0, 0);
  deskPivot.add(desk);

  // laptop = PLAY
  const laptop = new THREE.Group();
  const base = new THREE.Mesh(new THREE.BoxGeometry(0.9, 0.06, 0.62), MAT.black);
  base.position.y = 0.87;
  const lid = new THREE.Mesh(new THREE.BoxGeometry(0.9, 0.6, 0.05), MAT.black);
  lid.position.set(0, 1.17, -0.3);
  lid.rotation.x = -0.28;
  const glow = new THREE.Mesh(new THREE.PlaneGeometry(0.78, 0.48), MAT.redGlow);
  glow.position.set(0, 1.17, -0.35);
  glow.rotation.x = -0.28;
  laptop.add(base, lid, glow);
  laptop.position.set(1.6, 0, 0);
  laptop.rotation.y = -Math.PI / 2;
  deskPivot.add(laptop);
  registerMenuTarget(laptop, 'PLAY', 'play');

  const playLabel = makeSign('PLAY', 1.5, 0.5, { bg: '#b3241d', fg: '#f4f3ef' });
  playLabel.material.transparent = true;
  playLabel.position.set(1.58, 1.85, 0);
  playLabel.rotation.y = Math.PI / 2;
  deskPivot.add(playLabel);
  menu.playLabel = playLabel;

  // folder = SETTINGS / handbook
  const folder = new THREE.Group();
  const paper = new THREE.Mesh(new THREE.BoxGeometry(0.62, 0.06, 0.86), MAT.light);
  paper.position.y = 0.87;
  const tab = new THREE.Mesh(new THREE.BoxGeometry(0.5, 0.02, 0.5), MAT.red);
  tab.position.set(0, 0.91, 0);
  tab.rotation.y = 0.4;
  folder.add(paper, tab);
  folder.position.set(1.0, 0, 1.55);
  deskPivot.add(folder);
  registerMenuTarget(folder, 'HANDBOOK', 'handbook');

  deskPivot.traverse((o) => {
    if (o.isMesh) o.castShadow = true;
  });

  // EXIT door = QUIT
  const door = new THREE.Group();
  const slab = new THREE.Mesh(new THREE.BoxGeometry(1.9, 2.6, 0.16), MAT.black);
  slab.position.y = 1.3;
  door.add(slab);
  const sign = makeSign('EXIT', 1.6, 0.6, { bg: '#121212', fg: '#b3241d' });
  sign.position.set(0, 2.95, 0);
  door.add(sign);
  door.position.set(4.5, 0, ROOM.startZ - 0.1);
  door.rotation.y = Math.PI;
  world.add(door);
  registerMenuTarget(door, 'QUIT', 'quit');

  // personal belongings box
  const boxProp = addProp('box', 2.9, 8.4, 0.3);
  menu.box = boxProp;
  registerMenuTarget(boxProp.group, 'TAKE THE BOX', 'box');

  // chair the hero was sitting on
  menu.chair = addProp('chair', 1.6, 6.0, -Math.PI / 2);

  // start-room dressing
  addProp('plant', -9.4, 11.4);
  addProp('cooler', 8.6, 11.6);
  addProp('extinguisher', -10.2, -0.6);
  addProp('monitor', -0.9, 1.2, -Math.PI / 2);
  addProp('desk', -0.8, 1.2, Math.PI / 2);

  const wallSign = makeSign('FLOOR 7 — OPERATIONS', 3.6, 0.6, {
    bg: '#e9e9e6',
    fg: '#8f8f8a',
  });
  wallSign.position.set(-1.5, 2.7, ROOM.startZ - 0.05);
  wallSign.rotation.y = Math.PI;
  world.add(wallSign);
}

function registerMenuTarget(object, label, action) {
  object.traverse((o) => {
    if (o.isMesh) {
      o.userData.menuAction = action;
      o.userData.menuLabel = label;
    }
  });
  menuTargets.push({ object, label, action });
}

/* ---------- drones ---------- */

function buildDrones() {
  const spots = [
    [-6, -16, 9],
    [5, -27, 8],
    [-4, -46, 10],
    [6, -61, 9],
  ];
  for (const [x, z, range] of spots) {
    const g = new THREE.Group();
    const bodyMesh = new THREE.Mesh(new THREE.OctahedronGeometry(0.5), MAT.black);
    bodyMesh.castShadow = true;
    const eye = new THREE.Mesh(new THREE.SphereGeometry(0.16, 8, 6), MAT.redGlow);
    eye.position.z = 0.42;
    const ring = new THREE.Mesh(
      new THREE.TorusGeometry(0.72, 0.05, 6, 16),
      MAT.dark
    );
    ring.rotation.x = Math.PI / 2;
    g.add(bodyMesh, eye, ring);
    g.position.set(x, 1.7, z);
    world.add(g);
    drones.push({ group: g, x, z, homeX: x, homeZ: z, range, dir: 1, alive: true, t: Math.random() * 6 });
  }
}

/* =========================================================
   player
   ========================================================= */

const player = createPlayer();
player.traverse((o) => {
  if (o.isMesh) o.castShadow = true;
});
world.add(player);
const playerBody = player.getObjectByName('body');
const playerMarker = player.getObjectByName('marker');
playerMarker.visible = false;

const P = {
  x: 1.5,
  z: 6.0,
  vx: 0,
  vz: 0,
  angle: -Math.PI / 2,
  radius: 0.55,
  heat: 0,
  hasBox: false,
  invuln: 0,
};

const carriedBox = createProp('box');
carriedBox.scale.setScalar(0.75);
carriedBox.visible = false;
carriedBox.position.set(0, 0.75, 0.45);
player.add(carriedBox);

/* =========================================================
   game state
   ========================================================= */

const HUD = {
  title: document.getElementById('hud-title'),
  game: document.getElementById('hud-game'),
  fill: document.getElementById('momentum-fill'),
  timer: document.getElementById('hud-timer'),
  destroyed: document.getElementById('hud-destroyed'),
  prompt: document.getElementById('prompt'),
  flash: document.getElementById('flash'),
};

const GAME = {
  mode: 'menu', // menu | transition | playing | over
  time: 0,
  limit: 70,
  destroyed: 0,
  score: 0,
  transition: 0,
  shake: 0,
  path: [],
  lastPath: JSON.parse(localStorage.getItem('offboarding-run') || 'null'),
  pathTimer: 0,
};

/* ---------- reflection: ghost of the previous run ---------- */

const ghost = createGhostBody();
ghost.visible = false;
world.add(ghost);

/* =========================================================
   input
   ========================================================= */

const keys = new Set();
addEventListener('keydown', (e) => {
  const k = e.key.toLowerCase();
  keys.add(k);
  if (k === 'r' && GAME.mode !== 'menu') restart();
  if (k === 'escape') closeOverlays();
  if (k === ' ' && GAME.mode === 'playing') throwBox();
  if (GAME.mode === 'menu') {
    if (k === 'enter' || k === ' ') startGame();
    if (k === 'h') showOverlay('overlay-handbook');
  }
  if ([' ', 'arrowup', 'arrowdown', 'arrowleft', 'arrowright'].includes(k)) e.preventDefault();
});
addEventListener('keyup', (e) => keys.delete(e.key.toLowerCase()));

const pointer = new THREE.Vector2(-10, -10);
const raycaster = new THREE.Raycaster();
let hovered = null;

canvas.addEventListener('mousemove', (e) => {
  pointer.set((e.clientX / innerWidth) * 2 - 1, -(e.clientY / innerHeight) * 2 + 1);
});

canvas.addEventListener('click', () => {
  if (GAME.mode !== 'menu' || !hovered) return;
  if (hovered.action === 'play') startGame();
  if (hovered.action === 'handbook') showOverlay('overlay-handbook');
  if (hovered.action === 'quit') showOverlay('overlay-quit');
  if (hovered.action === 'box') toggleBox();
});

document.getElementById('handbook-close').onclick = closeOverlays;
document.getElementById('quit-close').onclick = closeOverlays;
document.getElementById('win-restart').onclick = () => {
  closeOverlays();
  restart();
};

function showOverlay(id) {
  document.getElementById(id).classList.remove('hidden');
}
function closeOverlays() {
  document.querySelectorAll('.overlay').forEach((o) => o.classList.add('hidden'));
}

function toggleBox() {
  P.hasBox = !P.hasBox;
  carriedBox.visible = P.hasBox;
  menu.box.group.visible = !P.hasBox;
}

/* =========================================================
   audio (tiny synth, no assets)
   ========================================================= */

let actx = null;
function blip(freq, dur, type = 'square', gain = 0.06) {
  if (!actx) return;
  const o = actx.createOscillator();
  const g = actx.createGain();
  o.type = type;
  o.frequency.setValueAtTime(freq, actx.currentTime);
  o.frequency.exponentialRampToValueAtTime(freq * 0.4, actx.currentTime + dur);
  g.gain.setValueAtTime(gain, actx.currentTime);
  g.gain.exponentialRampToValueAtTime(0.0001, actx.currentTime + dur);
  o.connect(g).connect(actx.destination);
  o.start();
  o.stop(actx.currentTime + dur);
}
function noise(dur = 0.18, gain = 0.09) {
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
addEventListener('pointerdown', () => {
  if (!actx) actx = new (window.AudioContext || window.webkitAudioContext)();
  if (actx.state === 'suspended') actx.resume();
}, { once: false });

/* =========================================================
   destruction
   ========================================================= */

function spawnDebris(x, z, count, tint = MAT.dark) {
  for (let i = 0; i < count; i++) {
    const s = 0.12 + Math.random() * 0.22;
    const m = new THREE.Mesh(new THREE.BoxGeometry(s, s, s), tint);
    m.position.set(x + (Math.random() - 0.5), 0.4 + Math.random() * 0.8, z + (Math.random() - 0.5));
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
  while (debris.length > 260) {
    const d = debris.shift();
    world.remove(d.mesh);
    d.mesh.geometry.dispose();
  }
}

function breakProp(entry, impactSpeed) {
  if (entry.broken_ || entry.type === 'box') return;
  entry.broken_ = true;
  entry.intact.visible = false;
  entry.broken.visible = true;
  entry.group.rotation.z = (Math.random() - 0.5) * 0.4;

  if (entry.aabb) {
    const i = statics.indexOf(entry.aabb);
    if (i >= 0) statics.splice(i, 1);
  }

  const glass = entry.type === 'glassPanel';
  spawnDebris(entry.x, entry.z, glass ? 14 : 7, glass ? MAT.glass : MAT.dark);

  P.heat = Math.min(1, P.heat + entry.spec.heat);
  GAME.destroyed++;
  GAME.score += entry.spec.score;
  GAME.shake = Math.min(0.7, GAME.shake + 0.18 + entry.spec.heat);

  noise(glass ? 0.3 : 0.16, glass ? 0.1 : 0.08);
  blip(glass ? 900 : 200 + impactSpeed * 6, 0.12, glass ? 'triangle' : 'square', 0.05);
}

/* =========================================================
   collision helpers
   ========================================================= */

function resolveCircleAabb(cx, cz, r, a) {
  const dx = cx - a.x;
  const dz = cz - a.z;
  const ox = a.hw + r - Math.abs(dx);
  const oz = a.hd + r - Math.abs(dz);
  if (ox <= 0 || oz <= 0) return null;
  if (ox < oz) return { nx: Math.sign(dx) || 1, nz: 0, depth: ox };
  return { nx: 0, nz: Math.sign(dz) || 1, depth: oz };
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
  blip(120, 0.35, 'sawtooth', 0.1);
  noise(0.4, 0.12);
}

function beginPlay() {
  GAME.mode = 'playing';
  playerMarker.visible = true;
  GAME.time = 0;
  GAME.path = [];
  HUD.game.classList.remove('hidden');
  HUD.prompt.classList.remove('hidden');
  HUD.prompt.textContent = P.hasBox ? 'SPACE — THROW THE BOX' : 'WASD — RUN. BREAK EVERYTHING.';
  setTimeout(() => HUD.prompt.classList.add('hidden'), 4000);
  if (GAME.lastPath && GAME.lastPath.length > 8) ghost.visible = true;
}

function endGame(won) {
  GAME.mode = 'over';
  localStorage.setItem('offboarding-run', JSON.stringify(GAME.path.slice(0, 18000)));
  const title = document.querySelector('#overlay-win h2');
  title.textContent = won ? "YOU'RE OUT" : 'SECURITY ESCORTED YOU';
  document.getElementById('win-stats').innerHTML = won
    ? `TIME <b>${GAME.time.toFixed(1)}s</b> · DESTROYED <b>${GAME.destroyed}</b> · SEVERANCE <b>${GAME.score * 120}$</b>`
    : `You ran out of time. DESTROYED <b>${GAME.destroyed}</b>.`;
  showOverlay('overlay-win');
  blip(won ? 520 : 90, 0.6, won ? 'square' : 'sawtooth', 0.08);
}

function restart() {
  closeOverlays();
  if (GAME.mode === 'playing') {
    localStorage.setItem('offboarding-run', JSON.stringify(GAME.path.slice(0, 18000)));
  }
  GAME.lastPath = JSON.parse(localStorage.getItem('offboarding-run') || 'null');

  for (const p of props) {
    if (p.broken_) {
      p.broken_ = false;
      p.intact.visible = true;
      p.broken.visible = false;
      p.group.rotation.z = 0;
      if (p.aabb && !statics.includes(p.aabb)) statics.push(p.aabb);
    }
    p.group.position.set(p.x, p.spec.y || 0, p.z);
    p.vx = p.vz = 0;
  }
  for (const d of debris) world.remove(d.mesh);
  debris.length = 0;
  for (const a of afterimages) world.remove(a.mesh);
  afterimages.length = 0;
  for (const d of drones) {
    d.alive = true;
    d.group.visible = true;
    d.x = d.homeX;
    d.z = d.homeZ;
  }

  P.x = 0;
  P.z = 1;
  P.vx = P.vz = 0;
  P.heat = 0.3;
  P.invuln = 0;
  P.hasBox = false;
  carriedBox.visible = false;
  menu.box.group.visible = true;
  playerBody.position.y = 0;
  playerBody.rotation.x = 0;
  GAME.destroyed = 0;
  GAME.score = 0;
  GAME.time = 0;
  GAME.path = [];
  menu.deskPivot.rotation.x = 0;
  menu.deskPivot.position.copy(DESK_HOME);
  ghost.visible = false;
  beginPlay();
}

/* =========================================================
   update
   ========================================================= */

const clock = new THREE.Clock();
const tmpTarget = new THREE.Vector3();

function updateMenu(dt) {
  raycaster.setFromCamera(pointer, camera);
  const hits = raycaster.intersectObjects(
    menuTargets.map((t) => t.object),
    true
  );
  const hit = hits.find((h) => h.object.userData.menuAction);
  const next = hit ? menuTargets.find((t) => t.action === hit.object.userData.menuAction) : null;

  if (next !== hovered) {
    hovered = next;
    canvas.classList.toggle('pointer', !!hovered);
    if (hovered) {
      HUD.prompt.textContent =
        hovered.action === 'box' ? (P.hasBox ? 'LEAVE THE BOX' : 'TAKE THE BOX') : hovered.label;
      HUD.prompt.classList.remove('hidden');
      blip(660, 0.05, 'square', 0.03);
    } else {
      HUD.prompt.classList.add('hidden');
    }
  }

  for (const t of menuTargets) {
    const target = hovered === t ? 1.08 : 1;
    t.object.scale.lerp(new THREE.Vector3(target, target, target), 1 - Math.pow(0.001, dt));
  }

  // hero breathing / tension
  const b = Math.sin(performance.now() * 0.004) * 0.02;
  playerBody.position.y = 0.28 + b;
  playerBody.rotation.z = Math.sin(performance.now() * 0.0021) * 0.02;
  player.position.set(P.x, 0, P.z);
  player.rotation.y = P.angle;

  menu.playLabel.material.opacity = 0.7 + Math.sin(performance.now() * 0.006) * 0.3;
  menu.playLabel.material.transparent = true;
}

function updateTransition(dt) {
  GAME.transition += dt;
  const t = Math.min(1, GAME.transition / 1.5);
  const e = t * t * (3 - 2 * t);

  // hero stands up
  playerBody.position.y = 0.28 * (1 - Math.min(1, t * 3));
  playerBody.rotation.x = -0.25 * Math.max(0, 1 - t * 3);

  // desk flip
  const f = Math.min(1, Math.max(0, (t - 0.12) / 0.5));
  menu.deskPivot.rotation.x = -f * 2.1;
  menu.deskPivot.position.y = Math.sin(f * Math.PI) * 1.2;
  menu.deskPivot.position.z = DESK_HOME.z - f * 2.4;

  if (GAME.transition > 0.2 && !menu.flipFx) {
    menu.flipFx = true;
    spawnDebris(DESK_HOME.x, DESK_HOME.z - 1.2, 10);
    GAME.shake = 0.7;
    noise(0.5, 0.14);
    blip(90, 0.5, 'sawtooth', 0.09);
    if (!menu.chair.broken_) breakProp(menu.chair, 12);
  }

  // camera swings from the menu framing to the gameplay framing
  const gamePos = new THREE.Vector3(P.x, 0, P.z).add(CAM.game.offset);
  camera.position.lerpVectors(CAM.menu.pos, gamePos, e);
  camTarget.lerpVectors(CAM.menu.target, new THREE.Vector3(P.x, 1, P.z), e);
  frustum = THREE.MathUtils.lerp(CAM.menu.frustum, CAM.game.frustum, e);
  applyFrustum();
  camera.lookAt(camTarget);

  P.angle = THREE.MathUtils.lerp(-Math.PI / 2, Math.PI, e);

  if (t >= 1) {
    P.heat = 0.45;
    beginPlay();
  }
}

function updatePlayer(dt) {
  let ix = 0;
  let iz = 0;
  if (keys.has('w') || keys.has('arrowup') || keys.has('ц')) iz -= 1;
  if (keys.has('s') || keys.has('arrowdown') || keys.has('ы')) iz += 1;
  if (keys.has('a') || keys.has('arrowleft') || keys.has('ф')) ix -= 1;
  if (keys.has('d') || keys.has('arrowright') || keys.has('в')) ix += 1;

  const len = Math.hypot(ix, iz);
  const maxSpeed = (9.5 + P.heat * 7) * (P.hasBox ? 0.9 : 1);
  const accel = 46;

  if (len > 0) {
    ix /= len;
    iz /= len;
    P.vx += ix * accel * dt;
    P.vz += iz * accel * dt;
  } else {
    const damp = Math.pow(0.02, dt);
    P.vx *= damp;
    P.vz *= damp;
  }

  const speed = Math.hypot(P.vx, P.vz);
  if (speed > maxSpeed) {
    P.vx = (P.vx / speed) * maxSpeed;
    P.vz = (P.vz / speed) * maxSpeed;
  }

  P.x += P.vx * dt;
  P.z += P.vz * dt;

  // collide with statics
  for (const a of statics) {
    const hit = resolveCircleAabb(P.x, P.z, P.radius, a);
    if (!hit) continue;
    const impact = Math.abs(hit.nx * P.vx + hit.nz * P.vz) || speed;
    if (a.prop && !a.prop.broken_ && speed * (1 + P.heat * 0.4) >= a.prop.spec.breakSpeed) {
      breakProp(a.prop, impact);
      P.vx *= 0.86;
      P.vz *= 0.86;
      continue;
    }
    P.x += hit.nx * hit.depth;
    P.z += hit.nz * hit.depth;
    if (hit.nx) P.vx *= -0.15;
    if (hit.nz) P.vz *= -0.15;
    if (speed > 9) GAME.shake = Math.min(0.5, GAME.shake + 0.12);
  }

  // collide with dynamic props
  for (const p of props) {
    if (p.spec.dynamic === false) continue;
    if (p.broken_) continue;
    if (p === menu.box && !menu.box.group.visible) continue;
    const dx = P.x - p.group.position.x;
    const dz = P.z - p.group.position.z;
    const dist = Math.hypot(dx, dz);
    const minDist = P.radius + p.spec.radius;
    if (dist > minDist || dist === 0) continue;

    if (p.type === 'box') {
      if (!P.hasBox) {
        toggleBox();
        blip(440, 0.1, 'square', 0.05);
      }
      continue;
    }

    if (speed * (1 + P.heat * 0.4) >= p.spec.breakSpeed) {
      breakProp(p, speed);
      P.vx *= 0.94;
      P.vz *= 0.94;
    } else {
      const nx = dx / dist;
      const nz = dz / dist;
      p.vx -= (nx * speed) / p.spec.mass * 0.35;
      p.vz -= (nz * speed) / p.spec.mass * 0.35;
      P.x += nx * (minDist - dist) * 0.6;
      P.z += nz * (minDist - dist) * 0.6;
      P.vx *= 0.9;
      P.vz *= 0.9;
    }
  }

  if (speed > 0.6) P.angle = Math.atan2(P.vx, P.vz);
  player.position.set(P.x, 0, P.z);
  player.rotation.y = P.angle;
  playerBody.rotation.x = -Math.min(0.22, speed * 0.012);

  // momentum decay
  P.heat = Math.max(0, P.heat - (0.16 + (speed < 4 ? 0.25 : 0)) * dt);
  if (speed > 12) P.heat = Math.min(1, P.heat + 0.05 * dt);
  P.invuln = Math.max(0, P.invuln - dt);

  // afterimages (reflection trail)
  if (P.heat > 0.25 && speed > 6) {
    GAME.pathTimer -= dt;
    if (GAME.pathTimer <= 0) {
      GAME.pathTimer = 0.07;
      spawnAfterimage();
    }
  }

  // record run for the next reflection
  GAME.path.push(+P.x.toFixed(2), +P.z.toFixed(2), +P.angle.toFixed(2));

  // exit
  if (P.z < HALL.endZ + 1.5 && Math.abs(P.x) < 3.2) endGame(true);
}

const afterimagePool = [];
function spawnAfterimage() {
  let mesh = afterimagePool.pop();
  if (!mesh) mesh = createGhostBody();
  mesh.position.set(P.x, 0, P.z);
  mesh.rotation.y = P.angle;
  mesh.visible = true;
  mesh.traverse((o) => {
    if (o.isMesh) o.material.opacity = 0.2;
  });
  world.add(mesh);
  afterimages.push({ mesh, life: 0.55 });
}

function updateAfterimages(dt) {
  for (let i = afterimages.length - 1; i >= 0; i--) {
    const a = afterimages[i];
    a.life -= dt;
    const k = Math.max(0, a.life / 0.55);
    a.mesh.traverse((o) => {
      if (o.isMesh) o.material.opacity = 0.2 * k;
    });
    if (a.life <= 0) {
      world.remove(a.mesh);
      afterimagePool.push(a.mesh);
      afterimages.splice(i, 1);
    }
  }
}

function updateGhost() {
  const path = GAME.lastPath;
  if (!path || !ghost.visible) return;
  const i = Math.min(Math.floor(GAME.time * 60) * 3, path.length - 3);
  if (i < 0) return;
  ghost.position.set(path[i], 0, path[i + 1]);
  ghost.rotation.y = path[i + 2];
}

function updateProps(dt) {
  for (const p of props) {
    if (p.broken_) continue;
    if (!p.spec.dynamic) continue;
    const speed = Math.hypot(p.vx, p.vz);
    if (speed < 0.05) {
      p.vx = p.vz = 0;
      continue;
    }
    p.group.position.x += p.vx * dt;
    p.group.position.z += p.vz * dt;
    const damp = Math.pow(speed > 12 ? 0.55 : 0.06, dt);
    p.vx *= damp;
    p.vz *= damp;
    p.group.rotation.y += speed * dt * 0.5;

    // fast flying props smash what they touch (chain destruction)
    if (speed > 9) {
      for (const other of props) {
        if (other === p || other.broken_) continue;
        const dx = other.group.position.x - p.group.position.x;
        const dz = other.group.position.z - p.group.position.z;
        const r = (other.spec.radius || Math.max(...other.spec.half)) + (p.spec.radius || 0.5);
        if (dx * dx + dz * dz < r * r && speed >= other.spec.breakSpeed * 0.8) {
          breakProp(other, speed);
          p.vx *= 0.7;
          p.vz *= 0.7;
        }
      }
      for (const d of drones) {
        if (!d.alive) continue;
        if (Math.hypot(d.x - p.group.position.x, d.z - p.group.position.z) < 1.2) killDrone(d);
      }
    }

    // keep inside bounds
    const bx = p.group.position.z > ROOM.doorZ ? ROOM.maxX : HALL.maxX;
    p.group.position.x = THREE.MathUtils.clamp(p.group.position.x, -bx + 0.6, bx - 0.6);
    p.group.position.z = THREE.MathUtils.clamp(p.group.position.z, HALL.endZ + 0.6, ROOM.startZ - 0.6);
  }
}

function throwBox() {
  if (!P.hasBox) return;
  P.hasBox = false;
  carriedBox.visible = false;
  const b = menu.box;
  b.group.visible = true;
  b.broken_ = false;
  const dirX = Math.sin(P.angle);
  const dirZ = Math.cos(P.angle);
  b.group.position.set(P.x + dirX * 1.2, 0, P.z + dirZ * 1.2);
  const speed = Math.hypot(P.vx, P.vz);
  b.vx = dirX * (18 + speed);
  b.vz = dirZ * (18 + speed);
  GAME.shake = Math.min(0.6, GAME.shake + 0.2);
  blip(300, 0.16, 'square', 0.06);
}

function killDrone(d) {
  d.alive = false;
  d.group.visible = false;
  spawnDebris(d.x, d.z, 9);
  P.heat = Math.min(1, P.heat + 0.3);
  GAME.destroyed++;
  GAME.score += 3;
  GAME.shake = Math.min(0.8, GAME.shake + 0.3);
  noise(0.25, 0.1);
  blip(1200, 0.2, 'triangle', 0.05);
}

function updateDrones(dt) {
  for (const d of drones) {
    if (!d.alive) continue;
    d.t += dt;
    const distToPlayer = Math.hypot(P.x - d.x, P.z - d.z);
    if (distToPlayer < 13) {
      const nx = (P.x - d.x) / distToPlayer;
      const nz = (P.z - d.z) / distToPlayer;
      d.x += nx * 7.5 * dt;
      d.z += nz * 7.5 * dt;
      d.group.rotation.y = Math.atan2(nx, nz);
    } else {
      d.x = d.homeX + Math.sin(d.t * 0.6) * d.range;
      d.group.rotation.y = Math.cos(d.t * 0.6) > 0 ? Math.PI / 2 : -Math.PI / 2;
    }
    d.group.position.set(d.x, 1.7 + Math.sin(d.t * 3) * 0.12, d.z);
    d.group.children[0].rotation.y += dt * 2;

    if (distToPlayer < 1.15) {
      const speed = Math.hypot(P.vx, P.vz);
      if (speed > 12 || P.heat > 0.6) {
        killDrone(d);
      } else if (P.invuln <= 0) {
        P.invuln = 1.1;
        P.heat *= 0.35;
        const nx = (P.x - d.x) / (distToPlayer || 1);
        const nz = (P.z - d.z) / (distToPlayer || 1);
        P.vx = nx * 14;
        P.vz = nz * 14;
        HUD.flash.classList.remove('hit');
        void HUD.flash.offsetWidth;
        HUD.flash.classList.add('hit');
        GAME.shake = 0.6;
        blip(80, 0.3, 'sawtooth', 0.08);
      }
    }
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

function updateCamera(dt) {
  const lookX = THREE.MathUtils.clamp(P.x + P.vx * 0.28, -7, 7);
  const lookZ = THREE.MathUtils.clamp(P.z + P.vz * 0.28, HALL.endZ + 6, ROOM.startZ - 3);
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

  const targetFrustum = CAM.game.frustum + P.heat * 4;
  if (Math.abs(frustum - targetFrustum) > 0.02) {
    frustum += (targetFrustum - frustum) * Math.min(1, dt * 3);
    applyFrustum();
  }
  camera.lookAt(camTarget);

  sun.position.set(P.x + 16, 34, P.z + 18);
  sun.target.position.set(P.x, 0, P.z);
}

function updateHUD() {
  HUD.fill.style.width = `${P.heat * 100}%`;
  const left = Math.max(0, GAME.limit - GAME.time);
  HUD.timer.textContent = `${String(Math.floor(left / 60)).padStart(2, '0')}:${String(
    Math.floor(left % 60)
  ).padStart(2, '0')}`;
  HUD.timer.style.color = left < 20 ? '#b3241d' : '#333';
  HUD.destroyed.textContent = `DESTROYED: ${GAME.destroyed}`;
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
    updateDrones(dt);
    updateDebris(dt);
    updateAfterimages(dt);
    updateGhost();
    updateCamera(dt);
    updateHUD();
    if (GAME.time >= GAME.limit) endGame(false);
  } else {
    updateDebris(dt);
    updateProps(dt);
    updateCamera(dt);
  }

  if (exitSign) exitSign.material.opacity = 0.55 + Math.abs(Math.sin(performance.now() * 0.003)) * 0.45;

  renderer.render(scene, camera);
}

/* =========================================================
   boot
   ========================================================= */

addFloor();
buildShell();
buildNarrativeWall();
buildMenuRoom();
buildOffice();
buildExit();
buildDrones();

exitSign.material.transparent = true;
player.position.set(P.x, 0, P.z);
player.rotation.y = P.angle;
playerBody.position.y = 0.28;
playerBody.rotation.x = -0.25;

resize();
tick();
