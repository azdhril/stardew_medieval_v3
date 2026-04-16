#!/usr/bin/env node
// One-off: resize dungeon TMX maps so they're large enough to fill a 1920x1080
// viewport at zoom 2.3 without FitZoomToViewport bumping zoom. Anchors existing
// content at top-left; pads Ground CSV with the most-common tile; leaves all
// collision/trigger/door/spawn objects untouched (so player stays in original
// playable area — perimeter walls already inside the old bounds).
//
// Usage: node scripts/resize-dungeons.mjs

import { readFileSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';

const TARGET_W = 60;
const TARGET_H = 34;
const MAPS_DIR = 'assets/Maps';
const FILES = [
  'dungeon_r1.tmx', 'dungeon_r2.tmx', 'dungeon_r3.tmx', 'dungeon_r3a.tmx',
  'dungeon_r4.tmx', 'dungeon_r4a.tmx', 'dungeon_boss.tmx',
];

function mostCommon(arr) {
  const counts = new Map();
  for (const v of arr) counts.set(v, (counts.get(v) ?? 0) + 1);
  let best = arr[0], bestC = 0;
  for (const [v, c] of counts) if (c > bestC) { best = v; bestC = c; }
  return best;
}

function resize(file) {
  const path = join(MAPS_DIR, file);
  let tmx = readFileSync(path, 'utf8');

  // Map header: <map ... width="W" height="H" ...>
  const mapMatch = tmx.match(/<map\b[^>]*\bwidth="(\d+)"\s+height="(\d+)"/);
  if (!mapMatch) throw new Error(`${file}: no <map> header`);
  const oldW = +mapMatch[1], oldH = +mapMatch[2];
  if (oldW >= TARGET_W && oldH >= TARGET_H) {
    console.log(`${file}: already ${oldW}x${oldH}, skipping`);
    return;
  }

  // Ground <layer> with inline CSV.
  const layerRe = /(<layer\s+id="\d+"\s+name="Ground"\s+)width="(\d+)"\s+height="(\d+)"(>\s*<data encoding="csv">)([\s\S]*?)(<\/data>\s*<\/layer>)/;
  const m = tmx.match(layerRe);
  if (!m) throw new Error(`${file}: Ground layer CSV not found`);
  const [, lHead, lW, lH, lMid, csvBody, lTail] = m;
  if (+lW !== oldW || +lH !== oldH) {
    console.warn(`${file}: layer ${lW}x${lH} mismatches map ${oldW}x${oldH}`);
  }

  // Parse CSV into (oldH × oldW) grid of ints.
  const flat = csvBody.replace(/\s+/g, '').split(',').filter(s => s.length).map(Number);
  if (flat.length !== oldW * oldH)
    throw new Error(`${file}: CSV has ${flat.length} tiles, expected ${oldW*oldH}`);
  const grid = [];
  for (let y = 0; y < oldH; y++) grid.push(flat.slice(y*oldW, (y+1)*oldW));

  const fill = mostCommon(flat);

  // Build new grid: old content at (0,0), pad right with fill to TARGET_W,
  // append rows of TARGET_W fills until TARGET_H.
  const newGrid = [];
  for (let y = 0; y < TARGET_H; y++) {
    const row = new Array(TARGET_W).fill(fill);
    if (y < oldH) for (let x = 0; x < oldW; x++) row[x] = grid[y][x];
    newGrid.push(row);
  }

  // Serialize CSV the same way Tiled does: one row per line, trailing commas
  // on all rows except the last.
  const newCsv = '\n' + newGrid.map((row, i) => {
    const s = row.join(',');
    return i < TARGET_H - 1 ? s + ',' : s;
  }).join('\n') + '\n';

  // Rebuild TMX.
  tmx = tmx.replace(mapMatch[0],
    mapMatch[0].replace(`width="${oldW}"`, `width="${TARGET_W}"`)
               .replace(`height="${oldH}"`, `height="${TARGET_H}"`));
  tmx = tmx.replace(layerRe,
    `${lHead}width="${TARGET_W}" height="${TARGET_H}"${lMid}${newCsv}  ${lTail}`);

  writeFileSync(path, tmx, 'utf8');
  console.log(`${file}: ${oldW}x${oldH} -> ${TARGET_W}x${TARGET_H} (fill GID=${fill})`);
}

for (const f of FILES) {
  try { resize(f); }
  catch (e) { console.error(`${f}: ERROR ${e.message}`); }
}
