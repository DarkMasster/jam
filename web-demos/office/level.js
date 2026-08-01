import * as THREE from 'three';
import { MAT, COLORS, makeSign, makePaperSign, glowSprite } from './props.js';
import { world, addWall, addProp, addFloorGlow, ROOM, HALL } from './core.js';

/* =========================================================
   zones
   1  start office + menu          z  13 .. -2
   2  open space + roombas         z  -2 .. -26
   3  glass meeting room           z -26 .. -44
   4  server room + boss           z -44 .. -66
   5  reception, turnstiles, exit  z -66 .. -86
   ========================================================= */

export const ZONES = [
  { z: -2, name: 'OPEN SPACE' },
  { z: -26, name: 'MEETING ROOM' },
  { z: -44, name: 'SERVER ROOM' },
  { z: -66, name: 'RECEPTION' },
];

export const WALL_TEXTS = [
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
  ['WHY HUMAN?', true],
  ['EFFICIENCY FIRST', false],
];

export const spawns = { roombas: [], drones: [], boss: null };

/* ---------------- floor ---------------- */

function buildFloor() {
  const floor = new THREE.Mesh(new THREE.PlaneGeometry(40, 110), MAT.floor);
  floor.rotation.x = -Math.PI / 2;
  floor.position.set(0, 0, -36);
  floor.receiveShadow = true;
  world.add(floor);

  const carpet = new THREE.Mesh(new THREE.PlaneGeometry(5, 106), new THREE.MeshLambertMaterial({ color: 0x121215 }));
  carpet.rotation.x = -Math.PI / 2;
  carpet.position.set(0, 0.01, -36);
  carpet.receiveShadow = true;
  world.add(carpet);
}

/* ---------------- shell ---------------- */

function buildShell() {
  // start room
  addWall(ROOM.minX - 0.2, 5.5, 0.2, 7.7, 3.2);
  addWall(ROOM.maxX + 0.2, 5.5, 0.2, 7.7, 0.3);
  addWall(0, ROOM.startZ + 0.2, 11.2, 0.2, 3.2);
  // doorway is exactly one glass panel wide — the tutorial target
  addWall(-6.7, ROOM.doorZ, 4.3, 0.25, 3.0);
  addWall(6.7, ROOM.doorZ, 4.3, 0.25, 3.0);

  // hall
  addWall(HALL.minX - 0.2, -42, 0.2, 40.2, 3.2);
  addWall(HALL.maxX + 0.2, -42, 0.2, 40.2, 3.2);
  addWall(-12.5, ROOM.doorZ, 3.5, 0.25, 3.0);
  addWall(12.5, ROOM.doorZ, 3.5, 0.25, 3.0);
  addWall(-9.75, HALL.endZ - 0.2, 6.25, 0.2, 3.2);
  addWall(9.75, HALL.endZ - 0.2, 6.25, 0.2, 3.2);

  // ceiling strip lights: dim pools down the corridor
  for (let z = 4; z > HALL.endZ; z -= 11) {
    addFloorGlow(0, z, 16, 0x8ea0b8, 0.1);
  }
}

/* ---------------- zone 1: narrative wall ---------------- */

function buildNarrativeWall() {
  const x = ROOM.minX + 0.02;
  let i = 0;
  for (let row = 0; row < 3; row++) {
    for (let col = 0; col < 4; col++) {
      const [text, red] = WALL_TEXTS[i++ % WALL_TEXTS.length];
      const sign = makePaperSign(text, 1.7, 0.88, {
        bg: red ? '#8f1a15' : '#cfcabc',
        fg: red ? '#e8e2d4' : '#111113',
      });
      sign.rotation.y = Math.PI / 2;
      sign.rotation.z = (Math.random() - 0.5) * 0.14;
      sign.position.set(x, 2.6 - row * 1.0, 11.4 - col * 2.2 + (Math.random() - 0.5) * 0.2);
      world.add(sign);
    }
  }

  const memo = makePaperSign('NOTICE OF ROLE ELIMINATION', 3.0, 1.4, {
    bg: '#cfcabc',
    fg: '#b3241d',
  });
  memo.rotation.y = Math.PI / 2;
  memo.rotation.z = -0.05;
  memo.position.set(x, 1.5, 0.9);
  world.add(memo);
}

/* ---------------- zone 2: open space ---------------- */

function buildOpenSpace() {
  // glass door out of the start room — first thing the box goes through
  addProp('glassPanel', 0, ROOM.doorZ, 0);
  addFloorGlow(0, ROOM.doorZ, 8, COLORS.red, 0.16);

  const podZ = [-7, -14, -21];
  for (const z of podZ) {
    for (const side of [-1, 1]) {
      const x = side * (7.5 + Math.random() * 2);
      addProp('desk', x, z, Math.PI / 2);
      addProp('monitor', x + side * 0.2, z - 0.5, -side * (Math.PI / 2));
      addProp('monitor', x + side * 0.2, z + 0.7, -side * (Math.PI / 2));
      addProp('chair', x - side * 1.5, z - 0.4);
      addProp('chair', x - side * 1.5, z + 1.1);
      addProp('mouse', x - side * 0.4, z + 0.1);
      addProp('keyboard', x - side * 0.6, z - 0.8, Math.PI / 2);
    }
  }

  addProp('mouse', 1.4, -10);
  addProp('mouse', -1.8, -17);
  addProp('paperReam', 2.2, -12.5);
  addProp('cigarettes', -2.4, -6.5);
  addProp('printer', -14.4, -11, 0);
  addProp('cooler', 14.6, -9);
  addProp('plant', -14.6, -19);
  addProp('extinguisher', 15.5, -18);
  addProp('stapler', 2.6, -19.5);

  // first glass gate
  for (const x of [-13, -7.8, -2.6, 2.6, 7.8, 13]) addProp('glassPanel', x, -25, 0);

  spawns.roombas.push({ x: -5, z: -11, range: 7 }, { x: 5, z: -19, range: 7 });
  spawns.drones.push({ x: 6, z: -6, range: 8 });

  addFloorGlow(-14.4, -11, 5, COLORS.red, 0.22);
  addFloorGlow(0, -25, 12, COLORS.red, 0.14);
}

/* ---------------- zone 3: glass meeting room ---------------- */

function buildMeetingRoom() {
  // outer shell of the meeting room, open on the corridor side
  for (const x of [-9.6, -4.8, 4.8, 9.6]) addProp('glassPanel', x, -30, 0);
  for (const z of [-32.4, -37.2]) {
    addProp('glassPanel', -11.9, z, Math.PI / 2);
    addProp('glassPanel', 11.9, z, Math.PI / 2);
  }
  for (const x of [-9.6, -4.8, 0, 4.8, 9.6]) addProp('glassPanel', x, -40, 0);

  addProp('desk', 0, -34.5, 0);
  addProp('desk', 0, -36.5, 0);
  addProp('chair', -2.4, -34.2);
  addProp('chair', 2.4, -34.8);
  addProp('chair', -2.4, -36.8);
  addProp('chair', 2.4, -36.4);
  addProp('whiteboard', -6.5, -35.5, 1.4);
  addProp('paperReam', 0.8, -35.4);
  addProp('mouse', -0.9, -35.6);
  addProp('cigarettes', 6.4, -36.2);

  const sign = makePaperSign('MEETING ROOM 7B — REFLECTION', 4.2, 0.8, { bg: '#cfcabc', fg: '#111113' });
  sign.rotation.y = Math.PI / 2;
  sign.position.set(-11.85, 2.3, -34.8);
  world.add(sign);

  spawns.roombas.push({ x: -6, z: -33, range: 5 });
  spawns.drones.push({ x: 4, z: -38, range: 7 });

  addFloorGlow(0, -35, 14, 0x7fa0c0, 0.12);
}

/* ---------------- zone 4: server room ---------------- */

function buildServerRoom() {
  for (let i = 0; i < 5; i++) {
    addProp('serverRack', -14 + i * 2.0, -49, 0);
    addProp('serverRack', 14 - i * 2.0, -49, 0);
    addProp('serverRack', -14 + i * 2.0, -60, 0);
    addProp('serverRack', 14 - i * 2.0, -60, 0);
  }
  addProp('extinguisher', -15.4, -54);
  addProp('extinguisher', 15.4, -54);
  addProp('cigarettes', -3.2, -52);
  addProp('paperReam', 3.4, -58);

  for (const z of [-49, -60]) {
    addFloorGlow(-11, z, 8, COLORS.red, 0.2);
    addFloorGlow(11, z, 8, COLORS.red, 0.2);
  }

  const sign = makeSign('SERVER ROOM — AUTHORIZED SYSTEMS ONLY', 6.0, 0.7, { bg: '#0b0b0d', fg: '#d8241d' });
  sign.rotation.y = Math.PI / 2;
  sign.position.set(HALL.minX + 0.03, 2.6, -54);
  world.add(sign);

  spawns.boss = { x: 0, z: -55 };
  spawns.drones.push({ x: -6, z: -63, range: 6 });
}

/* ---------------- zone 5: reception and exit ---------------- */

function buildReception() {
  for (const x of [-13, -7.8, 7.8, 13]) addProp('glassPanel', x, -68, 0);

  addProp('desk', -3.6, -73, 0);
  addProp('desk', 3.6, -73, 0);
  addProp('monitor', -3.6, -73.4);
  addProp('monitor', 3.6, -73.4);
  addProp('plant', -7.5, -75);
  addProp('plant', 7.5, -75);
  addProp('cigarettes', 3.6, -72.4);

  // turnstiles guard the exit
  for (const x of [-4.2, -1.4, 1.4, 4.2]) addProp('turnstile', x, -79, 0);

  const logo = makeSign('OPTIMEX GLOBAL', 6.4, 1.1, { bg: '#0b0b0d', fg: '#8d8878' });
  logo.position.set(0, 3.0, -76.9);
  world.add(logo);

  const arrow = makeSign('THIS WAY OUT', 5, 1.0, { bg: '#0b0b0d', fg: '#d8241d' });
  arrow.rotation.x = -Math.PI / 2;
  arrow.position.set(0, 0.04, -71);
  world.add(arrow);

  spawns.drones.push({ x: 0, z: -76, range: 9 });
}

/* ---------------- exit ---------------- */

let exitSign = null;
export function getExitSign() {
  return exitSign;
}

function buildExit() {
  const frame = new THREE.Mesh(new THREE.BoxGeometry(6.4, 3.2, 0.3), MAT.black);
  frame.position.set(0, 1.6, HALL.endZ - 0.1);
  world.add(frame);

  const light = new THREE.Mesh(new THREE.PlaneGeometry(5.6, 2.6), MAT.glowDim);
  light.position.set(0, 1.4, HALL.endZ + 0.1);
  world.add(light);

  exitSign = makeSign('EXIT', 3.4, 1.1, { bg: '#0b0b0d', fg: '#ff3b30' });
  exitSign.material.transparent = true;
  exitSign.position.set(0, 3.5, HALL.endZ + 0.2);
  world.add(exitSign);

  const halo = glowSprite(9, COLORS.red, 0.28);
  halo.position.set(0, 1.6, HALL.endZ + 0.4);
  world.add(halo);

  addFloorGlow(0, HALL.endZ + 4, 12, COLORS.red, 0.22);
}

/* ---------------- entry point ---------------- */

export function buildLevel() {
  buildFloor();
  buildShell();
  buildNarrativeWall();
  buildOpenSpace();
  buildMeetingRoom();
  buildServerRoom();
  buildReception();
  buildExit();
}
