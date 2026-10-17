/* Rhalgr Meter: motor de parse das linhas de log do ACT/IINACT (formato "Network log").
   Recebe linhas já separadas por "|" e monta as lutas: dano, cura, dano recebido, mortes e dano por habilidade.
   Funciona igual no navegador (window.RhalgrEngine) e no Node (module.exports), para dar para testar com um log real. */
(function (root) {
"use strict";

const JOBS = ["ADV","GLA","PGL","MRD","LNC","ARC","CNJ","THM","CRP","BSM","ARM","GSM","LTW","WVR","ALC","CUL","MIN","BTN","FSH",
  "PLD","MNK","WAR","DRG","BRD","WHM","BLM","ACN","SMN","SCH","ROG","NIN","MCH","DRK","AST","SAM","RDM","BLU","GNB","DNC","RPR","SGE","VPR","PCT"];
const TIMEOUT = 15000;          // sem nenhum dano por 15s, a próxima pancada abre uma luta nova
const KEEP = new Set(["01","02","03","21","22","24","25","26","260","261"]);

function hex(s) { const n = parseInt(s, 16); return isFinite(n) ? n : 0; }

/* O valor de dano/cura vem embaralhado: os 2 bytes de cima são o número; se o terceiro byte tem 0x40,
   o último byte é a parte alta de um valor acima de 65535. */
function amount(h) {
  if (!h || h.length <= 4) return 0;
  let d = parseInt(h.slice(0, -4), 16);
  if (h[h.length - 4] === "4") { const r = parseInt(h.slice(-2), 16); d = d - r + (r << 16); }
  return isFinite(d) ? d : 0;
}
function encodeAmount(v) {
  v = Math.max(0, Math.round(v));
  if (v < 0x10000) return (v * 0x10000).toString(16).toUpperCase();
  const r = v >>> 16, d = (v & 0xFFFF) + r;
  return d.toString(16).toUpperCase().padStart(4, "0") + "40" + r.toString(16).toUpperCase().padStart(2, "0");
}
// "2026-10-17T13:25:24.4350000-03:00": o Date.parse só aceita 3 casas nos milissegundos
function parseTime(s) {
  const m = /^(.*?\.\d{3})\d*(.*)$/.exec(s || "");
  return Date.parse(m ? m[1] + m[2] : s);
}
const isPlayerId = id => typeof id === "string" && id.length === 8 && id[0] === "1" && id[1] === "0";
const isNpcId = id => typeof id === "string" && id.length === 8 && id[0] === "4";
function mmss(sec) { sec = Math.floor(sec); return String(Math.floor(sec / 60)).padStart(2, "0") + ":" + String(sec % 60).padStart(2, "0"); }
const pct = (a, b) => b ? Math.round(a / b * 100) + "%" : "0%";

function create(opts) {
  const onEnd = (opts && opts.onEnd) || (() => {});
  const ents = new Map();          // id -> {name, job, owner}
  const statusNames = new Map();   // id do efeito -> nome (para rotular DoTs)
  let meId = "", zone = "", enc = null, lastActions = null;

  function ent(id) { let e = ents.get(id); if (!e) { e = { name: "", job: "", owner: "" }; ents.set(id, e); } return e; }
  // jogador dono da ação: o próprio jogador, ou o dono do pet/invocação
  function ownerOf(id) {
    if (isPlayerId(id)) return id;
    const e = ents.get(id);
    return e && isPlayerId(e.owner) ? e.owner : "";
  }
  function begin(t) {
    if (enc && t - enc.last > TIMEOUT) end();
    if (!enc) enc = { start: t, last: t, zone, players: new Map(), foes: new Map() };
    enc.last = t;
  }
  function player(id, name) {
    let p = enc.players.get(id);
    if (!p) {
      p = { id, name: name || "", dmg: 0, hits: 0, crits: 0, dhs: 0, heal: 0, over: 0, taken: 0, deaths: 0, maxhit: null, maxheal: null, acts: {} };
      enc.players.set(id, p);
    }
    return p;
  }
  function act(p, ab) { return p.acts[ab] || (p.acts[ab] = { n: 0, dmg: 0, heal: 0, hits: 0, crit: 0, dh: 0, max: 0 }); }

  function addDamage(so, tgt, tgtName, ab, v, crit, dh) {
    const p = player(so), a = act(p, ab);
    p.dmg += v; p.hits++; a.dmg += v; a.hits++;
    if (!p.first) p.first = enc.last;
    p.lastHit = enc.last;
    if (crit) { p.crits++; a.crit++; }
    if (dh) { p.dhs++; a.dh++; }
    if (v > a.max) a.max = v;
    if (!p.maxhit || v > p.maxhit.v) p.maxhit = { n: ab, v };
    const f = enc.foes.get(tgt) || { name: tgtName, dmg: 0 };
    f.dmg += v; enc.foes.set(tgt, f);
  }
  function addHeal(so, ab, v, cur, max) {
    const eff = max > 0 ? Math.min(v, Math.max(0, max - cur)) : v;
    const p = player(so), a = act(p, ab);
    p.heal += eff; p.over += v - eff; a.heal += eff;
    if (!p.maxheal || eff > p.maxheal.v) p.maxheal = { n: ab, v: eff };
  }

  // 21/22: ação. Os campos 8..23 são até 8 pares (flags, valor) de efeito.
  function ability(L, t) {
    const src = L[2], tgt = L[6], ab = L[5] || "?";
    if (isPlayerId(src) && L[3]) ent(src).name = L[3];
    if (isPlayerId(tgt) && L[7]) ent(tgt).name = L[7];
    const so = ownerOf(src), to = ownerOf(tgt);
    for (let i = 8; i < 24; i += 2) {
      const f = hex(L[i]); if (!f) continue;
      const k = f & 0xFF;
      if (k === 3 || k === 5 || k === 6) {          // dano (normal, bloqueado, aparado)
        const v = amount(L[i + 1]);
        if (so && isNpcId(tgt) && !to) { begin(t); addDamage(so, tgt, L[7], ab, v, f & 0x2000, f & 0x4000); }
        else if (to && isPlayerId(tgt) && !so) { begin(t); player(tgt).taken += v; }
      } else if (k === 4 && so && to && enc) {       // cura
        addHeal(so, ab, amount(L[i + 1]), +L[24] || 0, +L[25] || 0);
      }
    }
    // linhas 22 repetem a mesma ação para cada alvo; conta o uso só no primeiro
    if (so && enc && (L[45] === undefined || L[45] === "0")) act(player(so), ab).n++;
  }

  // 24: tique de DoT/HoT
  function tick(L, t) {
    const tgt = L[2], kind = L[4], v = hex(L[6]), src = L[17];
    const so = ownerOf(src), to = ownerOf(tgt);
    if (!enc || !v) return;
    const label = kind + (statusNames.has(L[5]) ? ": " + statusNames.get(L[5]) : "");
    if (kind === "DoT") {
      if (so && isNpcId(tgt) && !to) { begin(t); addDamage(so, tgt, L[3], label, v, false, false); }
      else if (to && isPlayerId(tgt) && !so) { begin(t); player(tgt).taken += v; }
    } else if (kind === "HoT" && so && to) {
      addHeal(so, label, v, +L[7] || 0, +L[8] || 0);
    }
  }

  function feed(L, t) {
    if (!L || !L.length) return;
    if (t === undefined) t = parseTime(L[1]);
    switch (L[0]) {
      case "21": case "22": ability(L, t); break;
      case "24": tick(L, t); break;
      case "25": if (enc && isPlayerId(L[2])) player(L[2]).deaths++; break;
      case "26": if (L[2] && L[3]) statusNames.set(L[2], L[3]); break;
      case "260": if (L[2] === "0" && enc) end(); break;     // ACT saiu de combate
      case "01": if (enc) end(); zone = L[3] || ""; break;
      case "02": meId = L[2]; ent(meId).name = L[3] || ""; break;
      case "03": {
        const e = ent(L[2]); e.name = L[3] || e.name; e.job = JOBS[hex(L[4])] || e.job;
        e.owner = isPlayerId(L[6]) ? L[6] : "";
        break;
      }
      case "261": {
        if (L[2] !== "Add" && L[2] !== "Change") break;
        const e = ent(L[3]);
        for (let i = 4; i + 1 < L.length; i += 2) {
          if (L[i] === "Name" && L[i + 1]) e.name = L[i + 1];
          else if (L[i] === "Job" && L[i + 1]) e.job = JOBS[+L[i + 1]] || e.job;
          else if (L[i] === "OwnerID" && L[i + 1]) e.owner = isPlayerId(L[i + 1]) ? L[i + 1] : "";
        }
        break;
      }
    }
  }

  function snapshot(active) {
    if (!enc) return null;
    if (active === undefined) active = true;
    const dur = Math.max(1, (enc.last - enc.start) / 1000);
    let totD = 0, totH = 0;
    enc.players.forEach(p => { totD += p.dmg; totH += p.heal; });
    const combatants = [...enc.players.values()].map(p => {
      const e = ents.get(p.id) || {};
      return {
        name: e.name || p.name || p.id, job: e.job || "",
        // dps: pela duração da luta (como o ENCDPS do ACT); dpsAct: pelo tempo entre o primeiro e o último golpe do jogador
        dps: p.dmg / dur, dpsAct: p.first ? p.dmg / Math.max(1, (p.lastHit - p.first) / 1000) : 0, dmg: p.dmg, dmgPct: pct(p.dmg, totD), crit: pct(p.crits, p.hits), dh: pct(p.dhs, p.hits), maxhit: p.maxhit,
        hps: p.heal / dur, heal: p.heal, healPct: pct(p.heal, totH), over: pct(p.over, p.heal + p.over), maxheal: p.maxheal,
        taken: p.taken, deaths: p.deaths, me: p.id === meId
      };
    });
    const actions = {};
    combatants.forEach((c, i) => { actions[c.name] = [...enc.players.values()][i].acts; });
    let top = null;
    enc.foes.forEach(f => { if (!top || f.dmg > top.dmg) top = f; });
    return { title: top ? top.name : (enc.zone || "Combate"), zone: enc.zone, time: mmss(dur), dur, dps: totD / dur, hps: totH / dur,
      active, when: enc.start, combatants, actions };
  }

  function end() {
    if (!enc) return;
    const snap = snapshot(false);
    lastActions = snap.actions;
    enc = null;
    if (snap.combatants.length) onEnd(snap);
  }

  return {
    feed,
    feedText(line) { const L = line.split("|"); if (KEEP.has(L[0])) feed(L); },
    snapshot, end,
    reset() { enc = null; lastActions = null; },
    recentActions() { return enc ? snapshot().actions : lastActions; },
    get active() { return !!enc; },
    get me() { return (ents.get(meId) || {}).name || ""; }
  };
}

const api = { create, parseTime, amount, encodeAmount, JOBS, KEEP };
if (typeof module === "object" && module.exports) module.exports = api; else root.RhalgrEngine = api;
})(typeof globalThis !== "undefined" ? globalThis : this);
