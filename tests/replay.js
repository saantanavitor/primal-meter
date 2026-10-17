// Roda um log do IINACT/ACT pelo motor e imprime cada luta encontrada.
// Uso: node tests/replay.js "C:\Users\...\Documents\IINACT\Network_xxx.log"
"use strict";
const fs = require("fs");
const path = require("path");
const RE = require(path.join(__dirname, "..", "web", "engine.js"));

const file = process.argv[2];
if (!file) { console.error("Informe o caminho do log."); process.exit(1); }

let ok = true;
const fmt = n => Math.round(n).toLocaleString("pt-BR");
const engine = RE.create({
  onEnd(e) {
    if (e.dur < 5) return;
    console.log(`\n== ${e.title} · ${e.zone} · ${e.time} · ${fmt(e.dps)} DPS da party`);
    for (const c of e.combatants.sort((a, b) => b.dps - a.dps)) {
      const acts = Object.entries(e.actions[c.name] || {}).sort((a, b) => b[1].dmg - a[1].dmg).slice(0, 3)
        .map(([n, a]) => `${n} ${fmt(a.dmg)}`).join(", ");
      console.log(`  ${(c.job || "?").padEnd(3)} ${c.name.padEnd(24)} ${fmt(c.dps).padStart(8)} DPS  ${fmt(c.hps).padStart(7)} HPS  crít ${c.crit.padStart(4)}  DH ${c.dh.padStart(4)}  mortes ${c.deaths}  | ${acts}`);
      if (!isFinite(c.dps) || c.dps < 0) ok = false;
    }
  }
});

// valores grandes sobrevivem ida e volta pelo embaralhamento
for (const v of [0, 1, 1403, 65535, 65536, 123456, 2000000]) {
  if (RE.amount(RE.encodeAmount(v)) !== v) { console.error("encodeAmount falhou para", v); ok = false; }
}

for (const line of fs.readFileSync(file, "utf8").split(/\r?\n/)) engine.feedText(line);
engine.end();
process.exit(ok ? 0 : 1);
