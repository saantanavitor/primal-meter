// Grava o que o IINACT manda pelo WebSocket, para investigar números estranhos.
// Uso: node tools/capture.js [segundos] [saida.jsonl]   (padrão: 20s, capture.jsonl)
"use strict";
const fs = require("fs");
const secs = +(process.argv[2] || 20), out = process.argv[3] || "capture.jsonl";
const ws = new WebSocket(process.env.OVERLAY_WS || "ws://127.0.0.1:10501/ws");
const file = fs.createWriteStream(out);
let n = 0;
ws.onopen = () => {
  ws.send(JSON.stringify({ call: "subscribe", events: ["CombatData", "LogLine", "ChangePrimaryPlayer"] }));
  console.log(`conectado; gravando ${secs}s em ${out}`);
  setTimeout(() => { ws.close(); file.end(); console.log(`${n} mensagens`); }, secs * 1000);
};
ws.onmessage = ev => { n++; file.write(ev.data + "\n"); };
ws.onerror = () => { console.error("não conectou no IINACT"); process.exit(1); };
