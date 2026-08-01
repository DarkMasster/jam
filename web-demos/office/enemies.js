import * as THREE from 'three';
import { MAT, COLORS, createRoomba, createDrone, createBossRack, glowSprite } from './props.js';
import {
  world,
  enemies,
  props,
  P,
  GAME,
  FX,
  HALL,
  blip,
  noise,
  spawnDebris,
  breakProp,
  damageProp,
  spawnProjectile,
  dist2,
} from './core.js';
import { spawns } from './level.js';

/* =========================================================
   spawning
   ========================================================= */

export function buildEnemies() {
  for (const s of spawns.roombas) {
    const group = createRoomba();
    group.position.set(s.x, 0, s.z);
    world.add(group);
    const halo = glowSprite(4, COLORS.red, 0);
    halo.rotation.x = -Math.PI / 2;
    halo.position.set(s.x, 0.03, s.z);
    world.add(halo);
    enemies.push({
      kind: 'roomba',
      group,
      halo,
      eye: group.getObjectByName('eye'),
      ring: group.getObjectByName('ring'),
      home: { x: s.x, z: s.z },
      x: s.x,
      z: s.z,
      range: s.range,
      dirX: 1,
      dirZ: 0,
      state: 'patrol', // patrol | telegraph | charge | recover
      t: Math.random() * 3,
      hp: 2,
      dead: false,
    });
  }

  for (const s of spawns.drones) {
    const group = createDrone();
    group.position.set(s.x, 1.7, s.z);
    world.add(group);
    enemies.push({
      kind: 'drone',
      group,
      home: { x: s.x, z: s.z },
      x: s.x,
      z: s.z,
      range: s.range,
      t: Math.random() * 6,
      hp: 1,
      dead: false,
    });
  }

  if (spawns.boss) {
    const group = createBossRack();
    group.position.set(spawns.boss.x, 0, spawns.boss.z);
    world.add(group);
    const halo = glowSprite(12, COLORS.red, 0.25);
    halo.rotation.x = -Math.PI / 2;
    halo.position.set(spawns.boss.x, 0.03, spawns.boss.z);
    world.add(halo);

    const beam = new THREE.Mesh(
      new THREE.PlaneGeometry(0.35, 26),
      new THREE.MeshBasicMaterial({
        color: COLORS.red,
        transparent: true,
        opacity: 0.12,
        depthWrite: false,
        side: THREE.DoubleSide,
      })
    );
    beam.rotation.x = -Math.PI / 2;
    beam.position.set(spawns.boss.x, 0.06, spawns.boss.z - 20);
    world.add(beam);

    enemies.push({
      kind: 'boss',
      group,
      halo,
      beam,
      lens: group.getObjectByName('lens'),
      beacon: group.getObjectByName('beacon'),
      x: spawns.boss.x,
      z: spawns.boss.z,
      t: 0,
      fireT: 2.4,
      hp: 6,
      maxHp: 6,
      dead: false,
      awake: false,
    });
  }
}

export function resetEnemies() {
  for (const e of enemies) {
    e.dead = false;
    e.group.visible = true;
    e.x = e.home ? e.home.x : e.x;
    e.z = e.home ? e.home.z : e.z;
    e.state = 'patrol';
    e.t = 0;
    if (e.kind === 'roomba') e.hp = 2;
    if (e.kind === 'drone') e.hp = 1;
    if (e.kind === 'boss') {
      e.hp = e.maxHp;
      e.awake = false;
      e.fireT = 2.4;
      if (e.beam) e.beam.visible = true;
    }
    if (e.halo) e.halo.material.opacity = e.kind === 'boss' ? 0.25 : 0;
    e.group.rotation.set(0, 0, 0);
  }
}

/* =========================================================
   damage
   ========================================================= */

export function killEnemy(e, silent = false) {
  if (e.dead) return;
  e.dead = true;
  if (e.kind === 'boss') {
    // becomes a sparking husk instead of disappearing
    e.group.rotation.z = 0.22;
    e.group.position.y = -0.15;
    if (e.beam) e.beam.visible = false;
    if (e.halo) e.halo.material.opacity = 0.12;
    spawnDebris(e.x, e.z, 24, MAT.metal, 1.4);
    GAME.score += 12;
    GAME.destroyed += 4;
    FX.shake(0.9);
    noise(0.7, 0.14);
    blip(70, 0.8, 'sawtooth', 0.08);
    P.heat = Math.min(1, P.heat + 0.55);
    FX.hint('SERVER RACK DOWN', 2.5);
    return;
  }
  e.group.visible = false;
  if (e.halo) e.halo.material.opacity = 0;
  spawnDebris(e.x, e.z, 9, MAT.metal, e.kind === 'drone' ? 1.6 : 0.4);
  GAME.destroyed++;
  GAME.score += 3;
  P.heat = Math.min(1, P.heat + 0.28);
  if (!silent) {
    FX.shake(0.3);
    noise(0.24, 0.09);
    blip(1100, 0.18, 'triangle', 0.04);
  }
}

export function damageEnemy(e, amount = 1) {
  if (e.dead) return false;
  if (e.kind === 'boss') e.awake = true;
  e.hp -= amount;
  blip(320, 0.08, 'square', 0.04);
  FX.shake(0.1);
  if (e.hp <= 0) {
    killEnemy(e);
    return true;
  }
  return false;
}

/** anything moving fast enough that touches an enemy hurts it */
export function hitEnemiesAt(x, z, radius, amount = 1, exclude = null) {
  let hits = 0;
  for (const e of enemies) {
    if (e.dead || e === exclude) continue;
    if (dist2(x, z, e.x, e.z) < radius * radius) {
      damageEnemy(e, amount);
      hits++;
    }
  }
  return hits;
}

/* =========================================================
   per-frame update
   ========================================================= */

function hurtPlayer(e, push = 13, heatMul = 0.45) {
  if (P.invuln > 0 || P.dashT > 0) return;
  P.invuln = 1.1;
  P.heat *= heatMul;
  const d = Math.hypot(P.x - e.x, P.z - e.z) || 1;
  P.vx = ((P.x - e.x) / d) * push;
  P.vz = ((P.z - e.z) / d) * push;
  if (P.carry) {
    // knocked out of your hands
    const c = P.carry;
    c.carried = false;
    c.thrown = true;
    c.vx = (Math.random() - 0.5) * 10;
    c.vz = (Math.random() - 0.5) * 10;
    c.vy = 4;
    P.carry = null;
    P.mice = 0;
    P.paper = 0;
    FX.hint('ITEM KNOCKED LOOSE', 1.6);
  }
  FX.hit();
  FX.shake(0.55);
  blip(80, 0.3, 'sawtooth', 0.07);
}

function nearestPropTo(x, z, radius) {
  let best = null;
  let bestD = radius * radius;
  for (const p of props) {
    if (p.dead || p.carried || p.spec.breakSpeed >= 99) continue;
    const d = dist2(x, z, p.group.position.x, p.group.position.z);
    if (d < bestD) {
      bestD = d;
      best = p;
    }
  }
  return best;
}

function updateRoomba(e, dt) {
  e.t -= dt;

  if (e.state === 'patrol') {
    const seen = dist2(P.x, P.z, e.x, e.z) < 100;
    e.x += e.dirX * 3.2 * dt;
    e.z += e.dirZ * 3.2 * dt;
    if (Math.abs(e.x - e.home.x) > e.range) e.dirX *= -1;
    if (Math.abs(e.z - e.home.z) > e.range) e.dirZ *= -1;
    if (e.t <= 0) {
      e.t = 1.4 + Math.random() * 1.6;
      const a = Math.random() * Math.PI * 2;
      e.dirX = Math.sin(a);
      e.dirZ = Math.cos(a);
    }
    if (seen) {
      e.state = 'telegraph';
      e.t = 0.75;
      blip(220, 0.25, 'square', 0.04);
    }
  } else if (e.state === 'telegraph') {
    // red flare before the ram
    const k = 1 - Math.max(0, e.t) / 0.75;
    e.halo.material.opacity = 0.15 + k * 0.55;
    e.halo.scale.setScalar(1 + k * 0.5);
    const d = Math.hypot(P.x - e.x, P.z - e.z) || 1;
    e.dirX = (P.x - e.x) / d;
    e.dirZ = (P.z - e.z) / d;
    e.group.rotation.y = Math.atan2(e.dirX, e.dirZ);
    if (e.t <= 0) {
      e.state = 'charge';
      e.t = 1.15;
      noise(0.15, 0.05);
    }
  } else if (e.state === 'charge') {
    e.halo.material.opacity = 0.5;
    e.x += e.dirX * 18 * dt;
    e.z += e.dirZ * 18 * dt;
    e.group.rotation.y = Math.atan2(e.dirX, e.dirZ);

    if (dist2(P.x, P.z, e.x, e.z) < 1.2) {
      hurtPlayer(e, 15);
      e.state = 'recover';
      e.t = 1.0;
    } else {
      // slamming into furniture wrecks it
      const hit = nearestPropTo(e.x, e.z, 1.2);
      if (hit) {
        damageProp(hit, 2, 12);
        e.state = 'recover';
        e.t = 1.1;
        FX.shake(0.25);
        noise(0.2, 0.07);
      }
    }
    if (e.t <= 0) {
      e.state = 'recover';
      e.t = 0.7;
    }
  } else {
    e.halo.material.opacity = Math.max(0, e.halo.material.opacity - dt);
    e.halo.scale.setScalar(1);
    if (e.t <= 0) {
      e.state = 'patrol';
      e.t = 1.2;
    }
  }

  e.x = THREE.MathUtils.clamp(e.x, HALL.minX + 1, HALL.maxX - 1);
  e.z = THREE.MathUtils.clamp(e.z, HALL.endZ + 1, 12);
  e.group.position.set(e.x, 0, e.z);
  e.group.children[0].rotation.y += dt * 3;
  e.halo.position.set(e.x, 0.03, e.z);

  // dashing through one destroys it
  if (P.dashT > 0 && dist2(P.x, P.z, e.x, e.z) < 1.6) killEnemy(e);
}

function updateDrone(e, dt) {
  e.t += dt;
  const d = Math.hypot(P.x - e.x, P.z - e.z);
  if (d < 13) {
    e.x += ((P.x - e.x) / d) * 7.5 * dt;
    e.z += ((P.z - e.z) / d) * 7.5 * dt;
    e.group.rotation.y = Math.atan2(P.x - e.x, P.z - e.z);
  } else {
    e.x = e.home.x + Math.sin(e.t * 0.6) * e.range;
    e.group.rotation.y += dt;
  }
  e.group.position.set(e.x, 1.7 + Math.sin(e.t * 3) * 0.12, e.z);

  if (d < 1.2) {
    const speed = Math.hypot(P.vx, P.vz);
    if (P.dashT > 0 || speed > 13 || P.heat > 0.7) killEnemy(e);
    else hurtPlayer(e, 12);
  }
}

function updateBoss(e, dt) {
  const dz = Math.abs(P.z - e.z);
  if (!e.awake && dz < 16) {
    e.awake = true;
    FX.hint('SERVER RACK ONLINE', 2.5);
    blip(60, 0.9, 'sawtooth', 0.07);
  }
  if (!e.awake) return;

  e.t += dt;
  const toPlayer = Math.atan2(P.x - e.x, P.z - e.z);
  e.group.rotation.y = THREE.MathUtils.lerp(e.group.rotation.y, toPlayer, 1 - Math.pow(0.02, dt));

  // scanning beam follows the aim
  e.beam.position.set(e.x + Math.sin(toPlayer) * 13, 0.06, e.z + Math.cos(toPlayer) * 13);
  e.beam.rotation.z = -toPlayer;
  e.beam.material.opacity = 0.1 + Math.abs(Math.sin(e.t * 3)) * 0.12;
  e.lens.scale.setScalar(1 + Math.sin(e.t * 6) * 0.15);
  if (e.beacon) e.beacon.scale.setScalar(0.75 + Math.abs(Math.sin(e.t * 4)) * 0.5);

  // volleys of data discs
  e.fireT -= dt;
  if (e.fireT <= 0) {
    e.fireT = 2.2;
    for (let i = -2; i <= 2; i++) {
      const a = toPlayer + i * 0.16;
      spawnProjectile({
        x: e.x + Math.sin(a) * 1.6,
        z: e.z + Math.cos(a) * 1.6,
        y: 1.4,
        vx: Math.sin(a) * 15,
        vz: Math.cos(a) * 15,
        kind: 'disc',
        hostile: true,
        life: 3,
      });
    }
    blip(500, 0.2, 'square', 0.04);
  }

  // close range EMP pulse
  if (dist2(P.x, P.z, e.x, e.z) < 16 && e.t % 2 < dt * 2) {
    hurtPlayer(e, 18, 0.35);
    FX.shake(0.5);
    noise(0.3, 0.1);
  }

  e.halo.material.opacity = 0.2 + Math.abs(Math.sin(e.t * 3)) * 0.2;
}

export function updateEnemies(dt) {
  for (const e of enemies) {
    if (e.dead) {
      if (e.kind === 'boss' && Math.random() < dt * 4) {
        spawnDebris(e.x + (Math.random() - 0.5) * 2, e.z + 1, 1, MAT.glow, 1.6);
      }
      continue;
    }
    if (e.kind === 'roomba') updateRoomba(e, dt);
    else if (e.kind === 'drone') updateDrone(e, dt);
    else if (e.kind === 'boss') updateBoss(e, dt);
  }
}
