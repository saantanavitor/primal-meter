# Rhalgr Meter

Medidor de combate para Final Fantasy XIV, no estilo do ACT, com interface moderna e sem instalação complicada.
É um único `RhalgrMeter.exe`: você abre, ele fica por cima do jogo e se conecta sozinho ao IINACT.

**[⬇ Baixar a versão mais recente](https://github.com/saantanavitor/rhalgr-meter/releases/latest)**

- **DPS, cura e dano recebido** de toda a party, atualizados a cada segundo
- **Detalhes por jogador**: crítico, direct hit, maior golpe, overheal e **dano por habilidade**
- **Histórico** das últimas 20 lutas
- **Modo travado**: o mouse passa direto para o jogo
- **Reproduzir log**: abre um log do IINACT/ACT para rever lutas fora do jogo
- **Demonstração** com uma luta simulada

---

## Instalação

O Rhalgr Meter mostra os números, e quem lê o combate do jogo é o **IINACT**, um plugin do Dalamud. Por isso, você precisa dos dois.

### 1. Instale o XIVLauncher (se ainda não usa)

1. Baixe em [goatcorp.github.io](https://goatcorp.github.io/) e instale.
2. Abra o FFXIV sempre pelo XIVLauncher, para os plugins do Dalamud carregarem.

### 2. Instale o IINACT

1. Siga as instruções de [iinact.com](https://www.iinact.com/). Lá está o endereço do repositório de plugins que você adiciona no Dalamud.
2. Dentro do jogo, abra o instalador de plugins com `/xlplugins`, procure por **IINACT** e instale.
3. Abra o IINACT com `/iinact` e confira se o **servidor WebSocket** está ligado. O padrão é `ws://127.0.0.1:10501/ws`, e o Rhalgr Meter já vem configurado com esse endereço.

### 3. Baixe o Rhalgr Meter

1. Na página de [Releases](https://github.com/saantanavitor/rhalgr-meter/releases/latest), baixe o `RhalgrMeter.exe`.
2. Coloque-o onde preferir, por exemplo em `Documentos\RhalgrMeter`. Ele não tem instalador e não precisa do .NET instalado.
3. Na primeira vez que abrir, o Windows pode mostrar um aviso azul. Veja [o aviso do Windows](#o-aviso-do-windows-não-é-vírus) logo abaixo.

### 4. Ajuste o jogo

Em **Configurações do sistema → Configurações de tela**, coloque o jogo em **Janela sem bordas** (*Borderless Windowed*). No modo de tela cheia exclusiva, nenhum programa consegue aparecer por cima do jogo.

---

## Como usar

1. Abra o FFXIV pelo XIVLauncher, com o IINACT ativo.
2. Abra o `RhalgrMeter.exe`. O medidor aparece no canto direito da tela.
3. Confira o ponto ao lado do título: **verde, "Ao vivo"**, quer dizer que ele está conectado. **Vermelho, "Sem conexão"**, quer dizer que o IINACT não está rodando ou que o WebSocket está desligado. O medidor tenta de novo a cada 3 segundos.
4. Ajuste a posição: **arraste pelo topo** para mover e **puxe o canto inferior direito** para redimensionar. A posição e o tamanho ficam salvos.
5. **Trave antes de jogar** com **Ctrl+Shift+L**. Travado, o medidor ignora o mouse e os cliques vão para o jogo. Use o mesmo atalho para destravar.

| Atalho | O que faz |
| --- | --- |
| **Ctrl+Shift+L** | Travar / destravar (os cliques passam para o jogo) |
| **Ctrl+Shift+H** | Mostrar / ocultar o medidor |

**No medidor**

- **Abas DPS, Cura e Tanque**: cada uma ordena a party por um número diferente.
- **Clique num jogador** para ver os detalhes e o dano (ou a cura) de cada habilidade.
- **Seletor "Luta"**: abre as lutas anteriores. As últimas 20 ficam salvas.
- **Menu ⋯**: demonstração, reproduzir log, opacidade do fundo, endereço do IINACT, limpar histórico e sair.

**Ícone na bandeja** (perto do relógio do Windows): clique para mostrar ou ocultar o medidor. O botão direito abre as mesmas opções do menu.

**Reproduzir log**: no menu ⋯, escolha "Reproduzir log…" e abra um arquivo de `Documentos\IINACT`. Use ▶/❚❚ para pausar, 1×–64× para mudar a velocidade e ⏭ para processar tudo de uma vez.

---

## O aviso do Windows não é vírus

Na primeira vez que você abrir o `RhalgrMeter.exe`, o Windows pode mostrar:

> **O Windows protegeu o computador**
> O Microsoft Defender SmartScreen impediu que um aplicativo não reconhecido fosse iniciado.

Para abrir, clique em **Mais informações** e depois em **Executar assim mesmo**. O aviso não aparece de novo para esse arquivo.

### Por que o aviso aparece

O SmartScreen **não encontrou vírus nenhum**. Ele avisa sobre **todo programa que ainda não é conhecido**, e um programa só passa a ser conhecido de dois jeitos:

1. **Assinatura digital de código.** É um certificado pago, que custa centenas de dólares por ano e é emitido para empresas ou pessoas verificadas. O Rhalgr Meter é um projeto pessoal e gratuito e ainda não tem esse certificado.
2. **Reputação.** Depois que muitas pessoas baixam o mesmo arquivo sem problemas, o aviso some sozinho.

Muitos programas da comunidade de FFXIV, como plugins, ferramentas e o próprio ACT, mostram o mesmo aviso pelo mesmo motivo. O aviso diz que o Windows **não conhece** o programa, não que ele é perigoso.

### Por que você pode confiar

- **O código é aberto.** Tudo o que o programa faz está neste repositório: a janela em [`src/RhalgrMeter`](src/RhalgrMeter) e a interface e os cálculos em [`web`](web). Qualquer pessoa pode ler.
- **Você mesmo pode gerar o `.exe`** a partir do código, com o comando da seção [Desenvolvimento](#desenvolvimento). Assim não precisa confiar no arquivo que eu publiquei.
- **O programa faz muito pouco:**
  - não pede permissão de administrador;
  - não mexe nos arquivos do jogo e não injeta nada no processo do FFXIV;
  - só se conecta ao IINACT **no seu próprio computador** (`127.0.0.1`). Ele não manda dados para a internet; a única coisa que busca de fora são as fontes do Google Fonts;
  - só grava as próprias preferências em `%APPDATA%\RhalgrMeter` e o histórico de lutas em `%LOCALAPPDATA%\RhalgrMeter`.
- **Você pode conferir o arquivo.** Cada release informa o **SHA-256** do `.exe`. No PowerShell, rode o comando abaixo e compare com o código da release:

  ```powershell
  Get-FileHash .\RhalgrMeter.exe -Algorithm SHA256
  ```

  Se os códigos forem iguais, o arquivo é exatamente o que foi publicado aqui, sem nenhuma alteração no caminho.
- **Você pode pedir uma segunda opinião.** Envie o arquivo ao [VirusTotal](https://www.virustotal.com/), que o verifica com dezenas de antivírus de uma vez. Um ou outro antivírus menos conhecido às vezes acusa programas sem assinatura sem motivo real (um falso positivo).

> Regra geral, que vale para qualquer programa: baixe o Rhalgr Meter **só** pela página de [Releases](https://github.com/saantanavitor/rhalgr-meter/releases) deste repositório. Cópias enviadas por outras pessoas ou outros sites podem ter sido alteradas.

---

## Problemas comuns

| Problema | O que fazer |
| --- | --- |
| "Sem conexão" o tempo todo | Confira se o jogo foi aberto pelo XIVLauncher, se o IINACT está ativo (`/iinact`) e se o servidor WebSocket está ligado. Se você mudou a porta, ajuste o endereço no menu ⋯. |
| O medidor não aparece por cima do jogo | Coloque o jogo em **Janela sem bordas**. |
| Não consigo clicar no medidor | Ele está travado. Aperte **Ctrl+Shift+L**. |
| O medidor sumiu | Aperte **Ctrl+Shift+H** ou clique no ícone na bandeja. |
| "Precisa do WebView2 Runtime" | Isso só acontece no Windows 10 antigo. Instale o [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) e abra de novo. |

---

## Como funciona

```
FFXIV ──► IINACT (plugin do Dalamud) ──WebSocket──► RhalgrMeter.exe ──► interface (WebView2)
                         │                              │
                         └── Documentos\IINACT\*.log ───┘  (modo "Reproduzir log")
```

- **`src/RhalgrMeter`**: app WPF (.NET 10). Janela transparente e sempre por cima, clique atravessando quando travada, atalhos globais, ícone na bandeja e cliente WebSocket do IINACT.
- **`web/index.html`**: a interface. Vai embutida no `.exe` e também funciona sozinha no navegador ou no Browsingway (`index.html?OVERLAY_WS=ws://127.0.0.1:10501/ws`).
- **`web/engine.js`**: motor de parse das linhas de log do ACT (tipos 21/22 ações, 24 DoT/HoT, 25 mortes, 03/261 combatentes, 260 estado de combate). Ao vivo, os totais vêm do IINACT e o motor calcula o dano por habilidade. Na reprodução de log e na demonstração, o motor calcula tudo.

## Desenvolvimento

Requisitos: .NET SDK 10 e Node.js (só para os testes e para editar a interface).

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
