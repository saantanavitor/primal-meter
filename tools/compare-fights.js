// Compara os totais finais de cada luta entre dois arquivos .jsonl ("data\tCombatData" por linha), por exemplo o
// lutas.jsonl do Primal Meter e uma gravação do IINACT. Uso: node tools/compare-fights.js primal.jsonl iinact.jsonl
"use strict";
const fs = require("fs");

function num(v) {
  if (v == null) return 0;
  let s = String(v).replace(/[^\d.,-]/g, "");
  const dot = s.lastIndexOf("."), comma = s.lastIndexOf(",");
  if (comma > dot) s = s.replace(/\./g, "").replace(",", "."); else s = s.replace(/,/g, "");
  const n = parseFloat(s); return isFinite(n) ? n : 0;
}
function load(file) {
  return fs.readFileSync(file, "utf8").split(/\r?\n/).filter(Boolean).map(l => {
    const d = JSON.parse(l.slice(l.indexOf("\t") + 1));
    const e = d.Encounter || {};
    const people = {};
    for (const [name, c] of Object.entries(d.Combatant || {})) {
      people[name] = { job: c.Job, dps: num(c.encdps), dmg: num(c.damage), hps: num(c.enchps), heal: num(c.healed), taken: num(c.damagetaken), deaths: num(c.deaths) };
    }
    return { title: e.title, dur: num(e.DURATION), time: e.duration, dmg: Object.values(people).reduce((a, p) => a + p.dmg, 0), people };
  }).filter(f => f.dur >= 3);
}

const [a, b] = [load(process.argv[2]), load(process.argv[3])];
const pct = (x, y) => y ? ((x - y) / y * 100) : (x ? 100 : 0);
let matched = 0, exact = 0;
for (const fa of a) {
  // mesma luta: mesmo título e duração parecida, escolhendo a de dano total mais próximo
  const cands = b.filter(fb => fb.title === fa.title && Math.abs(fb.dur - fa.dur) <= 3);
  if (!cands.length) { console.log(`\n✗ ${fa.title} ${fa.time}: sem par no segundo arquivo`); continue; }
  const fb = cands.sort((x, y) => Math.abs(x.dmg - fa.dmg) - Math.abs(y.dmg - fa.dmg))[0];
  matched++;
  let worst = 0;
  const rows = [];
  for (const name of new Set([...Object.keys(fa.people), ...Object.keys(fb.people)])) {
    const pa = fa.people[name] || {}, pb = fb.people[name] || {};
    const d = { dps: pct(pa.dps || 0, pb.dps || 0), hps: pct(pa.hps || 0, pb.hps || 0), taken: pct(pa.taken || 0, pb.taken || 0) };
    worst = Math.max(worst, ...Object.values(d).map(Math.abs), (pa.deaths || 0) !== (pb.deaths || 0) ? 100 : 0);
    rows.push(`   ${(pa.job || pb.job || "").padEnd(4)}${name.padEnd(24)} DPS ${Math.round(pa.dps || 0)} × ${Math.round(pb.dps || 0)}   HPS ${Math.round(pa.hps || 0)} × ${Math.round(pb.hps || 0)}   recebido ${Math.round(pa.taken || 0)} × ${Math.round(pb.taken || 0)}   mortes ${pa.deaths || 0} × ${pb.deaths || 0}`);
  }
  if (worst < 0.5) exact++;
  console.log(`\n${worst < 0.5 ? "✓" : "≠"} ${fa.title} ${fa.time} × ${fb.time}  (maior diferença: ${worst.toFixed(2)}%)`);
  rows.forEach(r => console.log(r));
}
console.log(`\n${matched} de ${a.length} lutas com par; ${exact} iguais (diferença abaixo de 0,5%).`);
