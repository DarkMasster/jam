import * as THREE from 'three';

/* =========================================================
   palette — night office, near monochrome, red is the only hue
   ========================================================= */

export const COLORS = {
  void: 0x08080a,
  floor: 0x17171a,
  floorLit: 0x212126,
  wall: 0x212125,
  wallDark: 0x141416,
  metal: 0x35353b,
  silhouette: 0x040405,
  paper: 0xcfcabc,
  screenDead: 0x0e0e10,
  red: 0xd8241d,
  redDim: 0x6e1512,
  glass: 0x2c3a3d,
};

const lambert = (color, extra = {}) => new THREE.MeshLambertMaterial({ color, ...extra });

export const MAT = {
  black: lambert(COLORS.silhouette),
  wall: lambert(COLORS.wall),
  wallDark: lambert(COLORS.wallDark),
  floor: lambert(COLORS.floor),
  metal: lambert(0x45454e),
  panel: lambert(0x33333a),
  paper: lambert(COLORS.paper),
  screenDead: lambert(COLORS.screenDead),
  red: lambert(COLORS.red),
  // unlit, so bloom picks them up
  glow: new THREE.MeshBasicMaterial({ color: COLORS.red, side: THREE.DoubleSide }),
  glowDim: new THREE.MeshBasicMaterial({ color: COLORS.redDim, side: THREE.DoubleSide }),
  lamp: new THREE.MeshBasicMaterial({ color: 0xfff2d8 }),
  glass: new THREE.MeshLambertMaterial({
    color: COLORS.glass,
    transparent: true,
    opacity: 0.3,
    depthWrite: false,
  }),
  ghost: new THREE.MeshBasicMaterial({
    color: 0x9aa4b2,
    transparent: true,
    opacity: 0.16,
    depthWrite: false,
  }),
};

/* =========================================================
   prop specs
   radius -> round dynamic prop, half -> static AABB
   carry: light | medium | heavy (heavy cannot be picked up)
   ========================================================= */

export const PROP_SPECS = {
  // carryable / throwable
  mouse: { radius: 0.3, dynamic: true, mass: 0.2, carry: 'light', weapon: 'mice', breakSpeed: 99, heat: 0, score: 0, y: 0.82 },
  paperReam: { radius: 0.35, dynamic: true, mass: 0.3, carry: 'light', weapon: 'paper', breakSpeed: 99, heat: 0, score: 0, y: 0.84 },
  cigarettes: { radius: 0.3, dynamic: true, mass: 0.1, carry: 'light', pickup: 'cigs', breakSpeed: 99, heat: 0, score: 0, y: 0.84 },
  keyboard: { radius: 0.42, dynamic: true, mass: 0.25, carry: 'light', hp: 2, breakSpeed: 7, heat: 0.14, score: 1, y: 0.82 },
  stapler: { radius: 0.28, dynamic: true, mass: 0.2, carry: 'light', hp: 2, breakSpeed: 8, heat: 0.12, score: 1, y: 0.84 },
  extinguisher: { radius: 0.4, dynamic: true, mass: 0.35, carry: 'light', hp: 2, breakSpeed: 6, heat: 0.2, score: 1 },
  box: { radius: 0.5, dynamic: true, mass: 0.4, carry: 'medium', hp: 3, breakSpeed: 99, heat: 0, score: 0 },
  monitor: { radius: 0.5, dynamic: true, mass: 0.35, carry: 'medium', hp: 2, breakSpeed: 6, heat: 0.18, score: 1, y: 0.84 },
  chair: { radius: 0.62, dynamic: true, mass: 0.5, carry: 'medium', hp: 2, breakSpeed: 7, heat: 0.16, score: 1 },
  plant: { radius: 0.5, dynamic: true, mass: 0.4, carry: 'medium', hp: 1, breakSpeed: 5, heat: 0.12, score: 1 },
  cooler: { radius: 0.55, dynamic: true, mass: 0.7, carry: 'medium', hp: 2, breakSpeed: 8, heat: 0.16, score: 1 },
  // heavy / static
  desk: { half: [1.75, 0.85], dynamic: false, carry: 'heavy', hp: 3, breakSpeed: 9, heat: 0.22, score: 2 },
  whiteboard: { radius: 1.1, dynamic: true, mass: 0.9, carry: 'heavy', hp: 3, breakSpeed: 9, heat: 0.2, score: 2 },
  serverRack: { half: [0.7, 0.7], dynamic: false, carry: 'heavy', hp: 4, breakSpeed: 13, heat: 0.4, score: 4 },
  glassPanel: { half: [2.4, 0.14], dynamic: false, carry: 'heavy', hp: 1, breakSpeed: 8, heat: 0.3, score: 3 },
  turnstile: { half: [0.6, 0.5], dynamic: false, carry: 'heavy', hp: 3, breakSpeed: 11, heat: 0.34, score: 3 },
  printer: { half: [0.8, 0.6], dynamic: false, carry: 'heavy', hp: 3, breakSpeed: 10, heat: 0.26, score: 2 },
};

/* =========================================================
   helpers
   ========================================================= */

function mesh(geo, mat, x = 0, y = 0, z = 0) {
  const m = new THREE.Mesh(geo, mat);
  m.position.set(x, y, z);
  m.castShadow = true;
  return m;
}

const box = (w, h, d) => new THREE.BoxGeometry(w, h, d);

/* =========================================================
   office props
   ========================================================= */

function createDesk() {
  const g = new THREE.Group();
  g.add(mesh(box(3.5, 0.12, 1.7), MAT.panel, 0, 0.78, 0));
  g.add(mesh(box(0.14, 0.78, 1.5), MAT.metal, -1.6, 0.39, 0));
  g.add(mesh(box(0.14, 0.78, 1.5), MAT.metal, 1.6, 0.39, 0));
  g.add(mesh(box(1.0, 0.55, 0.7), MAT.wallDark, 1.05, 0.35, 0));
  return g;
}

function createMonitor() {
  const g = new THREE.Group();
  g.add(mesh(box(0.5, 0.06, 0.35), MAT.metal, 0, 0.03, 0));
  g.add(mesh(box(0.08, 0.34, 0.08), MAT.metal, 0, 0.2, 0));
  g.add(mesh(box(1.15, 0.7, 0.07), MAT.black, 0, 0.72, 0));
  g.add(mesh(box(1.03, 0.58, 0.02), MAT.screenDead, 0, 0.72, 0.05));
  g.add(mesh(new THREE.PlaneGeometry(0.05, 0.05), MAT.glow, 0.5, 0.42, 0.06));
  return g;
}

function createChair() {
  const g = new THREE.Group();
  g.add(mesh(box(0.62, 0.1, 0.62), MAT.black, 0, 0.46, 0));
  g.add(mesh(box(0.6, 0.62, 0.1), MAT.black, 0, 0.78, -0.28));
  g.add(mesh(new THREE.CylinderGeometry(0.06, 0.06, 0.46, 6), MAT.metal, 0, 0.23, 0));
  g.add(mesh(new THREE.CylinderGeometry(0.34, 0.34, 0.06, 8), MAT.metal, 0, 0.04, 0));
  return g;
}

function createCooler() {
  const g = new THREE.Group();
  g.add(mesh(box(0.5, 0.9, 0.5), MAT.panel, 0, 0.45, 0));
  g.add(mesh(new THREE.CylinderGeometry(0.28, 0.22, 0.6, 10), MAT.glass, 0, 1.2, 0));
  g.add(mesh(box(0.3, 0.08, 0.06), MAT.glow, 0, 0.62, 0.26));
  return g;
}

function createExtinguisher() {
  const g = new THREE.Group();
  g.add(mesh(new THREE.CylinderGeometry(0.16, 0.16, 0.7, 10), MAT.red, 0, 0.35, 0));
  g.add(mesh(new THREE.CylinderGeometry(0.05, 0.05, 0.2, 6), MAT.black, 0, 0.78, 0));
  return g;
}

function createPlant() {
  const g = new THREE.Group();
  g.add(mesh(new THREE.CylinderGeometry(0.22, 0.16, 0.4, 8), MAT.metal, 0, 0.2, 0));
  g.add(mesh(new THREE.ConeGeometry(0.42, 1.1, 6), MAT.black, 0, 0.95, 0));
  return g;
}

function createWhiteboard() {
  const g = new THREE.Group();
  g.add(mesh(box(2.2, 1.4, 0.07), MAT.panel, 0, 1.3, 0));
  g.add(mesh(box(2.2, 0.08, 0.09), MAT.metal, 0, 0.6, 0));
  g.add(mesh(box(0.08, 0.6, 0.08), MAT.metal, -0.9, 0.3, 0));
  g.add(mesh(box(0.08, 0.6, 0.08), MAT.metal, 0.9, 0.3, 0));
  g.add(mesh(box(0.9, 0.04, 0.02), MAT.glowDim, -0.4, 1.5, 0.05));
  return g;
}

function createServerRack() {
  const g = new THREE.Group();
  g.add(mesh(box(1.4, 2.4, 1.4), MAT.black, 0, 1.2, 0));
  for (let i = 0; i < 6; i++) {
    g.add(mesh(box(1.2, 0.12, 0.04), MAT.panel, 0, 0.45 + i * 0.34, 0.72));
    g.add(mesh(box(0.07, 0.07, 0.03), MAT.glow, 0.45, 0.45 + i * 0.34, 0.74));
  }
  return g;
}

function createGlassPanel() {
  const g = new THREE.Group();
  g.add(mesh(box(4.8, 2.8, 0.1), MAT.glass, 0, 1.4, 0));
  g.add(mesh(box(4.8, 0.1, 0.16), MAT.black, 0, 2.8, 0));
  g.add(mesh(box(0.11, 2.8, 0.16), MAT.black, -2.4, 1.4, 0));
  g.add(mesh(box(0.11, 2.8, 0.16), MAT.black, 2.4, 1.4, 0));
  g.add(mesh(box(4.8, 0.1, 0.18), MAT.glowDim, 0, 0.2, 0));
  return g;
}

function createTurnstile() {
  const g = new THREE.Group();
  g.add(mesh(box(0.5, 1.05, 0.9), MAT.panel, 0, 0.52, 0));
  g.add(mesh(box(0.3, 0.06, 0.3), MAT.glow, 0, 1.07, 0.2));
  for (let i = 0; i < 3; i++) {
    const arm = mesh(new THREE.CylinderGeometry(0.05, 0.05, 1.1, 6), MAT.metal, 0, 0.95, 0);
    arm.rotation.z = Math.PI / 2;
    arm.rotation.y = (i * Math.PI * 2) / 3;
    g.add(arm);
  }
  return g;
}

function createPrinter() {
  const g = new THREE.Group();
  g.add(mesh(box(1.5, 1.0, 1.1), MAT.panel, 0, 0.5, 0));
  g.add(mesh(box(1.2, 0.1, 0.9), MAT.metal, 0, 1.05, 0));
  g.add(mesh(box(0.35, 0.05, 0.03), MAT.glow, 0.4, 0.85, 0.56));
  return g;
}

function createBox() {
  const g = new THREE.Group();
  g.add(mesh(box(0.8, 0.6, 0.6), MAT.panel, 0, 0.3, 0));
  g.add(mesh(box(0.84, 0.1, 0.64), MAT.metal, 0, 0.62, 0));
  g.add(mesh(box(0.34, 0.16, 0.02), MAT.glow, 0, 0.36, 0.31));
  g.add(mesh(new THREE.ConeGeometry(0.14, 0.4, 7), MAT.black, 0.22, 0.78, 0));
  return g;
}

/* ---------------- weapons ---------------- */

function createMouse() {
  const g = new THREE.Group();
  const body = mesh(new THREE.SphereGeometry(0.16, 8, 6), MAT.panel, 0, 0.14, 0);
  body.scale.set(1, 0.72, 1.5);
  g.add(body);
  g.add(mesh(box(0.03, 0.02, 0.1), MAT.glow, 0, 0.24, 0.08));
  return g;
}

function createPaperReam() {
  const g = new THREE.Group();
  g.add(mesh(box(0.62, 0.34, 0.46), MAT.paper, 0, 0.17, 0));
  g.add(mesh(box(0.64, 0.06, 0.48), MAT.glowDim, 0, 0.2, 0));
  return g;
}

function createCigarettes() {
  const g = new THREE.Group();
  g.add(mesh(box(0.3, 0.4, 0.14), MAT.paper, 0, 0.2, 0));
  g.add(mesh(box(0.3, 0.12, 0.15), MAT.glow, 0, 0.34, 0));
  return g;
}

function createKeyboard() {
  const g = new THREE.Group();
  g.add(mesh(box(1.0, 0.08, 0.4), MAT.black, 0, 0.05, 0));
  for (let i = 0; i < 5; i++) {
    g.add(mesh(box(0.14, 0.03, 0.3), MAT.panel, -0.4 + i * 0.2, 0.1, 0));
  }
  return g;
}

function createStapler() {
  const g = new THREE.Group();
  g.add(mesh(box(0.5, 0.14, 0.16), MAT.black, 0, 0.08, 0));
  const top = mesh(box(0.46, 0.1, 0.14), MAT.red, 0.02, 0.19, 0);
  top.rotation.z = 0.12;
  g.add(top);
  return g;
}

/* ---------------- enemies ---------------- */

export function createRoomba() {
  const g = new THREE.Group();
  g.add(mesh(new THREE.CylinderGeometry(0.55, 0.6, 0.26, 16), MAT.black, 0, 0.14, 0));
  g.add(mesh(new THREE.CylinderGeometry(0.2, 0.2, 0.06, 12), MAT.panel, 0, 0.29, 0));
  const eye = mesh(new THREE.SphereGeometry(0.09, 8, 6), MAT.glow, 0, 0.3, 0.42);
  eye.name = 'eye';
  g.add(eye);
  const ring = mesh(new THREE.TorusGeometry(0.62, 0.035, 6, 20), MAT.glowDim, 0, 0.06, 0);
  ring.rotation.x = Math.PI / 2;
  ring.name = 'ring';
  g.add(ring);
  return g;
}

export function createDrone() {
  const g = new THREE.Group();
  g.add(mesh(new THREE.OctahedronGeometry(0.42), MAT.black, 0, 0, 0));
  g.add(mesh(new THREE.SphereGeometry(0.14, 8, 6), MAT.glow, 0, 0, 0.38));
  const ring = mesh(new THREE.TorusGeometry(0.66, 0.04, 6, 18), MAT.metal, 0, 0, 0);
  ring.rotation.x = Math.PI / 2;
  g.add(ring);
  return g;
}

export function createBossRack() {
  const g = new THREE.Group();
  g.add(mesh(box(2.6, 3.4, 1.8), MAT.black, 0, 1.7, 0));
  g.add(mesh(box(2.4, 0.14, 0.1), MAT.metal, 0, 3.35, 0.9));
  for (let i = 0; i < 8; i++) {
    const led = mesh(box(0.1, 0.1, 0.05), MAT.glow, -0.9 + (i % 4) * 0.6, 0.6 + Math.floor(i / 4) * 0.5, 0.92);
    led.name = 'led';
    g.add(led);
  }
  const lens = mesh(new THREE.CircleGeometry(0.34, 16), MAT.glow, 0, 2.6, 0.92);
  lens.name = 'lens';
  g.add(lens);

  // readable from the top-down camera: a beacon on the roof
  const beacon = mesh(new THREE.CircleGeometry(0.7, 16), MAT.glow, 0, 3.42, 0);
  beacon.rotation.x = -Math.PI / 2;
  beacon.name = 'beacon';
  g.add(beacon);
  const stripe = mesh(box(2.4, 0.04, 0.24), MAT.glowDim, 0, 3.42, -0.6);
  g.add(stripe);
  return g;
}

/* =========================================================
   registry
   ========================================================= */

const FACTORIES = {
  desk: createDesk,
  monitor: createMonitor,
  chair: createChair,
  cooler: createCooler,
  extinguisher: createExtinguisher,
  plant: createPlant,
  whiteboard: createWhiteboard,
  serverRack: createServerRack,
  glassPanel: createGlassPanel,
  turnstile: createTurnstile,
  printer: createPrinter,
  box: createBox,
  mouse: createMouse,
  paperReam: createPaperReam,
  cigarettes: createCigarettes,
  keyboard: createKeyboard,
  stapler: createStapler,
};

export function createProp(type) {
  const f = FACTORIES[type];
  if (!f) throw new Error(`unknown prop type: ${type}`);
  return f();
}

/* =========================================================
   broken state — one generic collapse, no real fragmentation
   ========================================================= */

export function makeBroken(intact) {
  const broken = intact.clone(true);
  broken.traverse((o) => {
    if (!o.isMesh) return;
    o.rotation.set(
      (Math.random() - 0.5) * 1.6,
      (Math.random() - 0.5) * 2.4,
      (Math.random() - 0.5) * 1.6
    );
    o.position.x += (Math.random() - 0.5) * 1.1;
    o.position.z += (Math.random() - 0.5) * 1.1;
    o.position.y *= 0.18;
  });
  broken.scale.set(1, 0.55, 1);
  broken.visible = false;
  return broken;
}

/* =========================================================
   player silhouette
   ========================================================= */

export function createPlayer() {
  const g = new THREE.Group();

  const marker = new THREE.Group();
  const ring = new THREE.Mesh(new THREE.RingGeometry(0.6, 0.72, 22), MAT.glow);
  ring.rotation.x = -Math.PI / 2;
  const nose = new THREE.Mesh(new THREE.CircleGeometry(0.2, 3), MAT.glow);
  nose.rotation.x = -Math.PI / 2;
  nose.rotation.z = -Math.PI / 2;
  nose.position.z = 0.92;
  marker.add(ring, nose);
  marker.position.y = 0.04;
  marker.name = 'marker';
  g.add(marker);

  const body = new THREE.Group();
  body.add(mesh(new THREE.CapsuleGeometry(0.36, 0.62, 4, 10), MAT.black, 0, 0.85, 0));
  body.add(mesh(new THREE.SphereGeometry(0.29, 12, 10), MAT.black, 0, 1.55, 0));
  const armL = mesh(box(0.2, 0.62, 0.2), MAT.black, -0.46, 0.85, 0);
  const armR = mesh(box(0.2, 0.62, 0.2), MAT.black, 0.46, 0.85, 0);
  armL.name = 'armL';
  armR.name = 'armR';
  body.add(armL, armR);
  body.name = 'body';
  g.add(body);

  return g;
}

export function createGhostBody() {
  const g = new THREE.Group();
  const m = MAT.ghost.clone();
  const a = new THREE.Mesh(new THREE.CapsuleGeometry(0.36, 0.62, 4, 8), m);
  a.position.y = 0.85;
  const b = new THREE.Mesh(new THREE.SphereGeometry(0.29, 8, 6), m);
  b.position.y = 1.55;
  g.add(a, b);
  g.userData.mat = m;
  return g;
}

/* =========================================================
   canvas text — signs and sticky notes
   ========================================================= */

export function makeTextTexture(text, opts = {}) {
  const { bg = '#cfcabc', fg = '#101012', w = 256, h = 256, font = 900, rotate = 0 } = opts;
  const c = document.createElement('canvas');
  c.width = w;
  c.height = h;
  const ctx = c.getContext('2d');
  ctx.fillStyle = bg;
  ctx.fillRect(0, 0, w, h);

  const words = text.split(' ');
  const lines = [];
  let line = '';
  const maxChars = Math.max(6, Math.floor(w / 26));
  for (const word of words) {
    if ((line + ' ' + word).trim().length > maxChars) {
      lines.push(line.trim());
      line = word;
    } else {
      line += ' ' + word;
    }
  }
  lines.push(line.trim());

  ctx.save();
  ctx.translate(w / 2, h / 2);
  ctx.rotate(rotate);
  ctx.fillStyle = fg;
  ctx.textAlign = 'center';
  ctx.textBaseline = 'middle';
  const longest = Math.max(...lines.map((l) => l.length));
  const size = Math.min(h / (lines.length + 0.45), (w * 0.88) / (longest * 0.6));
  ctx.font = `${font} ${size}px Helvetica, Arial, sans-serif`;
  lines.forEach((l, i) => ctx.fillText(l, 0, (i - (lines.length - 1) / 2) * size * 1.12));
  ctx.restore();

  const tex = new THREE.CanvasTexture(c);
  tex.anisotropy = 4;
  return tex;
}

/* glowing signage — unlit, catches bloom */
export function makeSign(text, width, height, opts = {}) {
  const tex = makeTextTexture(text, { w: 512, h: Math.round((512 * height) / width), ...opts });
  return new THREE.Mesh(
    new THREE.PlaneGeometry(width, height),
    new THREE.MeshBasicMaterial({ map: tex, transparent: true })
  );
}

/* paper — reacts to light like everything else in the room */
export function makePaperSign(text, width, height, opts = {}) {
  const tex = makeTextTexture(text, { w: 512, h: Math.round((512 * height) / width), ...opts });
  return new THREE.Mesh(
    new THREE.PlaneGeometry(width, height),
    new THREE.MeshLambertMaterial({ map: tex })
  );
}

/* =========================================================
   soft radial glow sprite — cheap light pooling on surfaces
   ========================================================= */

let glowTex = null;
export function glowSprite(size, color = 0xd8241d, opacity = 0.5) {
  if (!glowTex) {
    const c = document.createElement('canvas');
    c.width = c.height = 128;
    const ctx = c.getContext('2d');
    const grad = ctx.createRadialGradient(64, 64, 0, 64, 64, 64);
    grad.addColorStop(0, 'rgba(255,255,255,1)');
    grad.addColorStop(0.45, 'rgba(255,255,255,0.32)');
    grad.addColorStop(1, 'rgba(255,255,255,0)');
    ctx.fillStyle = grad;
    ctx.fillRect(0, 0, 128, 128);
    glowTex = new THREE.CanvasTexture(c);
  }
  return new THREE.Mesh(
    new THREE.PlaneGeometry(size, size),
    new THREE.MeshBasicMaterial({
      map: glowTex,
      color,
      transparent: true,
      opacity,
      depthWrite: false,
      blending: THREE.AdditiveBlending,
    })
  );
}
