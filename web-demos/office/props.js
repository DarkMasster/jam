import * as THREE from 'three';

export const COLORS = {
  black: 0x121212,
  dark: 0x3a3a38,
  mid: 0x8f8f8a,
  light: 0xd8d6cf,
  white: 0xf4f3ef,
  red: 0xb3241d,
  glass: 0xbfd0cf,
};

const lambert = (color, extra = {}) =>
  new THREE.MeshLambertMaterial({ color, ...extra });

export const MAT = {
  black: lambert(COLORS.black),
  dark: lambert(COLORS.dark),
  mid: lambert(COLORS.mid),
  light: lambert(COLORS.light),
  white: lambert(COLORS.white),
  red: lambert(COLORS.red),
  redGlow: new THREE.MeshBasicMaterial({ color: COLORS.red, side: THREE.DoubleSide }),
  screen: new THREE.MeshBasicMaterial({ color: 0xf4f3ef }),
  screenOff: lambert(0x2b2b29),
  glass: new THREE.MeshLambertMaterial({
    color: COLORS.glass,
    transparent: true,
    opacity: 0.38,
    depthWrite: false,
  }),
  ghost: new THREE.MeshBasicMaterial({
    color: COLORS.black,
    transparent: true,
    opacity: 0.18,
    depthWrite: false,
  }),
};

// radius -> dynamic props (pushed around), half -> static props (AABB)
export const PROP_SPECS = {
  chair: { radius: 0.62, dynamic: true, mass: 0.5, breakSpeed: 7, heat: 0.14, score: 1 },
  cooler: { radius: 0.55, dynamic: true, mass: 0.7, breakSpeed: 8, heat: 0.16, score: 1 },
  extinguisher: { radius: 0.4, dynamic: true, mass: 0.35, breakSpeed: 5, heat: 0.2, score: 1 },
  box: { radius: 0.5, dynamic: true, mass: 0.4, breakSpeed: 999, heat: 0, score: 0 },
  desk: { half: [1.75, 0.85], dynamic: false, breakSpeed: 9, heat: 0.22, score: 2 },
  monitor: { radius: 0.5, dynamic: true, mass: 0.3, breakSpeed: 6, heat: 0.18, score: 1, y: 0.84 },
  whiteboard: { radius: 1.1, dynamic: true, mass: 0.8, breakSpeed: 9, heat: 0.2, score: 2 },
  serverRack: { half: [0.7, 0.7], dynamic: false, breakSpeed: 13, heat: 0.4, score: 4 },
  glassPanel: { half: [2.4, 0.14], dynamic: false, breakSpeed: 8, heat: 0.3, score: 3 },
  plant: { radius: 0.5, dynamic: true, mass: 0.4, breakSpeed: 5, heat: 0.12, score: 1 },
};

function mesh(geo, mat, x = 0, y = 0, z = 0) {
  const m = new THREE.Mesh(geo, mat);
  m.position.set(x, y, z);
  m.castShadow = true;
  m.receiveShadow = false;
  return m;
}

const box = (w, h, d) => new THREE.BoxGeometry(w, h, d);

/* ---------------- individual props ---------------- */

function createDesk() {
  const g = new THREE.Group();
  g.add(mesh(box(3.5, 0.12, 1.7), MAT.white, 0, 0.78, 0));
  g.add(mesh(box(0.14, 0.78, 1.5), MAT.dark, -1.6, 0.39, 0));
  g.add(mesh(box(0.14, 0.78, 1.5), MAT.dark, 1.6, 0.39, 0));
  g.add(mesh(box(1.0, 0.55, 0.7), MAT.light, 1.05, 0.35, 0));
  return g;
}

function createMonitor() {
  const g = new THREE.Group();
  g.add(mesh(box(0.5, 0.06, 0.35), MAT.dark, 0, 0.03, 0));
  g.add(mesh(box(0.08, 0.34, 0.08), MAT.dark, 0, 0.2, 0));
  g.add(mesh(box(1.15, 0.7, 0.07), MAT.black, 0, 0.72, 0));
  g.add(mesh(box(1.03, 0.58, 0.02), MAT.screen, 0, 0.72, 0.05));
  return g;
}

function createChair() {
  const g = new THREE.Group();
  g.add(mesh(box(0.62, 0.1, 0.62), MAT.black, 0, 0.46, 0));
  g.add(mesh(box(0.6, 0.62, 0.1), MAT.black, 0, 0.78, -0.28));
  g.add(mesh(new THREE.CylinderGeometry(0.06, 0.06, 0.46, 6), MAT.mid, 0, 0.23, 0));
  g.add(mesh(new THREE.CylinderGeometry(0.34, 0.34, 0.06, 8), MAT.mid, 0, 0.04, 0));
  return g;
}

function createCooler() {
  const g = new THREE.Group();
  g.add(mesh(box(0.5, 0.9, 0.5), MAT.light, 0, 0.45, 0));
  g.add(mesh(new THREE.CylinderGeometry(0.28, 0.22, 0.6, 10), MAT.glass, 0, 1.2, 0));
  g.add(mesh(box(0.3, 0.1, 0.06), MAT.red, 0, 0.62, 0.26));
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
  g.add(mesh(new THREE.CylinderGeometry(0.22, 0.16, 0.4, 8), MAT.mid, 0, 0.2, 0));
  g.add(mesh(new THREE.ConeGeometry(0.42, 1.1, 6), MAT.black, 0, 0.95, 0));
  return g;
}

function createWhiteboard() {
  const g = new THREE.Group();
  g.add(mesh(box(2.2, 1.4, 0.07), MAT.white, 0, 1.3, 0));
  g.add(mesh(box(2.2, 0.08, 0.09), MAT.dark, 0, 0.6, 0));
  g.add(mesh(box(0.08, 0.6, 0.08), MAT.dark, -0.9, 0.3, 0));
  g.add(mesh(box(0.08, 0.6, 0.08), MAT.dark, 0.9, 0.3, 0));
  g.add(mesh(box(0.9, 0.05, 0.02), MAT.red, -0.4, 1.5, 0.05));
  g.add(mesh(box(1.3, 0.05, 0.02), MAT.mid, -0.1, 1.3, 0.05));
  return g;
}

function createServerRack() {
  const g = new THREE.Group();
  g.add(mesh(box(1.4, 2.4, 1.4), MAT.black, 0, 1.2, 0));
  for (let i = 0; i < 6; i++) {
    g.add(mesh(box(1.2, 0.12, 0.04), MAT.dark, 0, 0.45 + i * 0.34, 0.72));
    g.add(mesh(box(0.08, 0.08, 0.03), MAT.redGlow, 0.45, 0.45 + i * 0.34, 0.74));
  }
  return g;
}

function createGlassPanel() {
  const g = new THREE.Group();
  g.add(mesh(box(4.8, 2.8, 0.1), MAT.glass, 0, 1.4, 0));
  g.add(mesh(box(4.8, 0.1, 0.16), MAT.black, 0, 2.8, 0));
  g.add(mesh(box(0.11, 2.8, 0.16), MAT.black, -2.4, 1.4, 0));
  g.add(mesh(box(0.11, 2.8, 0.16), MAT.black, 2.4, 1.4, 0));
  g.add(mesh(box(4.8, 0.16, 0.2), MAT.red, 0, 0.22, 0));
  g.add(mesh(box(4.8, 0.05, 0.18), MAT.mid, 0, 1.5, 0));
  return g;
}

function createBox() {
  const g = new THREE.Group();
  g.add(mesh(box(0.8, 0.6, 0.6), MAT.light, 0, 0.3, 0));
  g.add(mesh(box(0.84, 0.1, 0.64), MAT.dark, 0, 0.62, 0));
  g.add(mesh(box(0.3, 0.22, 0.02), MAT.red, 0, 0.36, 0.31));
  g.add(mesh(new THREE.ConeGeometry(0.14, 0.4, 7), MAT.black, 0.22, 0.78, 0));
  return g;
}

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
  box: createBox,
};

export function createProp(type) {
  return FACTORIES[type]();
}

/* ---------------- generic broken state ---------------- */

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

/* ---------------- player silhouette ---------------- */

export function createPlayer() {
  const g = new THREE.Group();

  const marker = new THREE.Group();
  const ring = new THREE.Mesh(new THREE.RingGeometry(0.62, 0.74, 20), MAT.redGlow);
  ring.rotation.x = -Math.PI / 2;
  const nose = new THREE.Mesh(new THREE.CircleGeometry(0.22, 3), MAT.redGlow);
  nose.rotation.x = -Math.PI / 2;
  nose.rotation.z = -Math.PI / 2;
  nose.position.z = 0.95;
  marker.add(ring, nose);
  marker.position.y = 0.04;
  marker.name = 'marker';
  g.add(marker);

  const body = new THREE.Group();
  body.add(mesh(new THREE.CapsuleGeometry(0.36, 0.62, 4, 10), MAT.black, 0, 0.85, 0));
  body.add(mesh(new THREE.SphereGeometry(0.29, 12, 10), MAT.black, 0, 1.55, 0));
  body.add(mesh(box(0.2, 0.62, 0.2), MAT.black, -0.46, 0.85, 0));
  body.add(mesh(box(0.2, 0.62, 0.2), MAT.black, 0.46, 0.85, 0));
  body.name = 'body';
  g.add(body);
  return g;
}

export function createGhostBody() {
  const g = new THREE.Group();
  g.add(mesh(new THREE.CapsuleGeometry(0.36, 0.62, 4, 8), MAT.ghost, 0, 0.85, 0));
  g.add(mesh(new THREE.SphereGeometry(0.29, 8, 6), MAT.ghost, 0, 1.55, 0));
  g.traverse((o) => {
    if (o.isMesh) o.material = MAT.ghost.clone();
  });
  return g;
}

/* ---------------- text on canvas (signs, sticky notes) ---------------- */

export function makeTextTexture(text, opts = {}) {
  const {
    bg = '#f4f3ef',
    fg = '#121212',
    w = 256,
    h = 256,
    font = 900,
    rotate = 0,
  } = opts;
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
  lines.forEach((l, i) => {
    ctx.fillText(l, 0, (i - (lines.length - 1) / 2) * size * 1.12);
  });
  ctx.restore();

  const tex = new THREE.CanvasTexture(c);
  tex.anisotropy = 4;
  return tex;
}

export function makeSign(text, width, height, opts = {}) {
  const tex = makeTextTexture(text, {
    w: 512,
    h: Math.round((512 * height) / width),
    ...opts,
  });
  const m = new THREE.Mesh(
    new THREE.PlaneGeometry(width, height),
    new THREE.MeshBasicMaterial({ map: tex, transparent: true })
  );
  return m;
}
