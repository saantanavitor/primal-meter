# Rhalgr Meter

Medidor de combate para Final Fantasy XIV, no estilo do ACT, com interface moderna e sem instalação complicada.
É um único `RhalgrMeter.exe`: abre, fica por cima do jogo e conecta sozinho no IINACT.

- **DPS, cura e tanque** de toda a party, atualizando a cada segundo
- **Detalhes por jogador**: crítico, direct hit, maior golpe, overheal e **dano por habilidade**
- **Histórico** das últimas 20 lutas
- **Modo travado**: o mouse passa direto para o jogo (Ctrl+Shift+L)
- **Reproduzir log**: abre um log do IINACT/ACT e revê as lutas sem entrar no jogo
- **Demonstração** com uma luta simulada

## Como usar

1. Instale o **IINACT** pelo Dalamud (XIVLauncher) e deixe o servidor WebSocket ligado (padrão `ws://127.0.0.1:10501/ws`).
2. Abra o `RhalgrMeter.exe`. O medidor aparece no canto da tela e o ponto verde indica "Ao vivo".
3. Arraste pelo topo para mover e pelo canto inferior direito para redimensionar.
4. Antes de jogar, trave com **Ctrl+Shift+L** para os cliques irem para o jogo. Destrave com o mesmo atalho.

O jogo precisa estar em **Janela sem bordas** (Configurações do sistema → Tela). Em tela cheia exclusiva nenhum overlay aparece por cima.

| Atalho | O que faz |
| --- | --- |
| Ctrl+Shift+L | Travar / destravar (cliques passam para o jogo) |
| Ctrl+Shift+H | Mostrar / ocultar |

O ícone na bandeja do Windows tem as mesmas opções, além de Demonstração, Reproduzir log e Sair.
As preferências ficam em `%APPDATA%\RhalgrMeter`.

## Como funciona

```
FFXIV ──► IINACT (plugin do Dalamud) ──WebSocket──► RhalgrMeter.exe ──► interface (WebView2)
                         │                              │
                         └── Documentos\IINACT\*.log ───┘  (modo "Reproduzir log")
```

- **`src/RhalgrMeter`**: app WPF (.NET 10). Janela transparente e sempre por cima, clique atravessando quando travada, atalhos globais, ícone na bandeja e cliente WebSocket do IINACT.
- **`web/index.html`**: a interface. Vai embutida no .exe e também funciona sozinha no navegador ou no Browsingway (`index.html?OVERLAY_WS=ws://127.0.0.1:10501/ws`).
- **`web/engine.js`**: motor de parse das linhas de log do ACT (tipos 21/22 ações, 24 DoT/HoT, 25 mortes, 03/261 combatentes, 260 estado de combate). Ao vivo, os totais vêm do IINACT e o motor calcula o dano por habilidade. Na reprodução de log e na demonstração, o motor calcula tudo.

## Desenvolvimento

Requisitos: .NET SDK 10 e Node.js (só para testes e para editar a interface).

```bash
dotnet run --project src/RhalgrMeter
```

```bash
node tests/replay.js "%USERPROFILE%\Documents\IINACT\Network_xxxxx.log"
```

```bash
node tools/serve.js
```

O último serve a interface em http://localhost:5173/ com a demonstração rodando, para mexer no visual sem abrir o app.

Para gerar o executável de distribuição (`dist/RhalgrMeter.exe`, cerca de 67 MB, não precisa do .NET instalado):

```bash
powershell -ExecutionPolicy Bypass -File tools/publish.ps1
```

## Limitações conhecidas

- Depende do IINACT (ou do ACT com OverlayPlugin e WebSocket ligado) para os dados ao vivo.
- Na reprodução de log, DoTs sem origem informada no log não são atribuídos, e escudos não contam como cura. Ao vivo, esses números vêm do IINACT e não têm esse problema.
- Clicar no medidor destravado tira o foco do jogo. Trave antes de jogar.

## Aviso

Ferramentas de terceiros violam os termos de serviço da Square Enix. Use para você e não comente números de outras pessoas no jogo.
