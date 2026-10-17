# Primal Meter

🇧🇷 Português · [🇺🇸 English](README.en.md)

Medidor de combate para Final Fantasy XIV, no estilo do ACT, com interface moderna e **sem plugin nenhum**.
É um único `PrimalMeter.exe`: você abre, ele fica por cima do jogo e já começa a medir. Não precisa de XIVLauncher, Dalamud, IINACT nem do ACT.

**[⬇ Baixar a versão mais recente](https://github.com/saantanavitor/primal-meter/releases/latest)**

- **DPS, cura e dano recebido** de toda a party, atualizados a cada segundo
- **Detalhes por jogador**: crítico, direct hit, maior golpe, overheal e **dano por habilidade**
- **Histórico** das últimas 20 lutas
- **Modo travado**: o mouse passa direto para o jogo
- **Reproduzir log**: revê lutas antigas fora do jogo
- **Demonstração** com uma luta simulada
- Em **português ou inglês**
- Usa o **mesmo motor de parse do ACT**, então os números batem com os do ACT e do IINACT

---

## Instalação

1. Na página de [Releases](https://github.com/saantanavitor/primal-meter/releases/latest), baixe o `PrimalMeter.exe`.
2. Coloque-o onde preferir, por exemplo em `Documentos\PrimalMeter`. Ele não tem instalador e não precisa do .NET instalado.
3. Na primeira vez que abrir, o Windows pode mostrar um aviso azul. Veja [o aviso do Windows](#o-aviso-do-windows-não-é-vírus) mais abaixo.
4. No jogo, em **Configurações do sistema → Configurações de tela**, coloque o modo **Janela sem bordas** (*Borderless Windowed*). No modo de tela cheia exclusiva, nenhum programa consegue aparecer por cima do jogo.

Na primeira execução, o medidor baixa o motor de parse do ACT da página oficial do autor (cerca de 4 MB) e mostra "Preparando…" enquanto isso. Nas próximas vezes, ele só confere se saiu uma versão nova.

---

## Como usar

1. Abra o FFXIV do jeito que você já abre: Steam, launcher oficial ou XIVLauncher, tanto faz.
2. Abra o `PrimalMeter.exe`, antes ou depois do jogo. O medidor aparece no canto direito da tela.
3. Confira o ponto ao lado do título:
   - **amarelo, "Esperando o jogo…"**: o FFXIV ainda não está aberto;
   - **verde, "Ao vivo"**: está medindo.
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
- **Menu ⋯**:
  - demonstração e reprodução de log;
  - o cálculo do DPS (pela duração da luta, como no ACT, ou pelo tempo ativo de cada jogador);
  - opacidade do fundo;
  - fonte de dados (motor embutido ou IINACT);
  - idioma (português ou inglês);
  - limpar histórico e sair.

**Ícone na bandeja** (perto do relógio do Windows): clique para mostrar ou ocultar o medidor. O botão direito abre as mesmas opções do menu.

**Reproduzir log**: o medidor grava um log de cada dia em `Documentos\PrimalMeter`, no mesmo formato de log de rede do ACT e do IINACT. No menu ⋯, escolha "Reproduzir log…" e abra um desses arquivos. Use ▶/❚❚ para pausar, 1×–64× para mudar a velocidade e ⏭ para processar tudo de uma vez.

**Já usa o IINACT?** No menu ⋯, troque a fonte para "IINACT". O medidor passa a ler os dados dele, como nas versões anteriores (`ws://127.0.0.1:10501/ws`).

---

## O aviso do Windows não é vírus

Na primeira vez que você abrir o `PrimalMeter.exe`, o Windows pode mostrar:

> **O Windows protegeu o computador**
> O Microsoft Defender SmartScreen impediu que um aplicativo não reconhecido fosse iniciado.

Para abrir, clique em **Mais informações** e depois em **Executar assim mesmo**. O aviso não aparece de novo para esse arquivo.

### Por que o aviso aparece

O SmartScreen **não encontrou vírus nenhum**. Ele avisa sobre **todo programa que ainda não é conhecido**, e um programa só passa a ser conhecido de dois jeitos:

1. **Assinatura digital de código.** É um certificado pago, que custa centenas de dólares por ano e é emitido para empresas ou pessoas verificadas. O Primal Meter é um projeto pessoal e gratuito e ainda não tem esse certificado.
2. **Reputação.** Depois que muitas pessoas baixam o mesmo arquivo sem problemas, o aviso some sozinho.

Muitos programas da comunidade de FFXIV, como plugins, ferramentas e o próprio ACT, mostram o mesmo aviso pelo mesmo motivo. O aviso diz que o Windows **não conhece** o programa, não que ele é perigoso.

Alguns antivírus podem desconfiar do Primal Meter por um motivo mais específico: ele **injeta uma DLL no jogo** para ler o combate (veja abaixo). É a mesma técnica que o ACT usa, e por isso o ACT também costuma ser marcado em antivírus mais rígidos. Se o seu antivírus bloquear, adicione uma exceção para o `PrimalMeter.exe` e para a pasta `C:\ProgramData\PrimalMeter`.

### O que o programa faz no seu computador

Para não depender de plugins, o Primal Meter faz exatamente o que o ACT faz:

- **Baixa o motor de parse do ACT** (FFXIV_ACT_Plugin, do autor Ravahn) da [página oficial de releases](https://github.com/ravahn/FFXIV_ACT_Plugin/releases) e o guarda em `%LOCALAPPDATA%\PrimalMeter\engine`. A cada abertura, ele consulta essa página para ver se saiu uma versão nova.
- **Lê a memória do jogo** para saber nomes, jobs, party e zona. Ele só lê, nunca escreve.
- **Injeta no jogo o [Deucalion](https://github.com/ff14wed/deucalion)**, uma DLL pequena e de código aberto que vem junto com o motor do ACT. Ela repassa ao medidor os pacotes de combate que o jogo recebe. A cópia usada fica em `C:\ProgramData\PrimalMeter\deucalion`.
- **Ao fechar o medidor, desliga o Deucalion**, e o jogo fica como estava antes.

E o que ele **não** faz:

- não pede permissão de administrador;
- não altera arquivos do jogo;
- não envia dados seus nem do jogo para a internet. A comunicação com o Deucalion é local, por um *named pipe*, e os únicos acessos à internet são a consulta ao motor do ACT no GitHub e as fontes da interface (Google Fonts);
- não automatiza nada no jogo.

### Por que você pode confiar

- **O código é aberto.** Tudo o que o programa faz está neste repositório: a janela e o motor em [`src`](src), e a interface e os cálculos em [`web`](web). Qualquer pessoa pode ler.
- **Você mesmo pode gerar o `.exe`** a partir do código, com o comando da seção [Desenvolvimento](#desenvolvimento). Assim não precisa confiar no arquivo que eu publiquei.
- **Você pode conferir o arquivo.** Cada release informa o **SHA-256** do `.exe`. No PowerShell, rode o comando abaixo e compare com o código da release:

  ```powershell
  Get-FileHash .\PrimalMeter.exe -Algorithm SHA256
  ```

  Se os códigos forem iguais, o arquivo é exatamente o que foi publicado aqui, sem nenhuma alteração no caminho.
- **Você pode pedir uma segunda opinião.** Envie o arquivo ao [VirusTotal](https://www.virustotal.com/), que o verifica com dezenas de antivírus de uma vez. Por causa da injeção de DLL, é normal que um ou outro antivírus aponte algo genérico, como acontece com o ACT.

> Regra geral, que vale para qualquer programa: baixe o Primal Meter **só** pela página de [Releases](https://github.com/saantanavitor/primal-meter/releases) deste repositório. Cópias enviadas por outras pessoas ou outros sites podem ter sido alteradas.

---

## Problemas comuns

| Problema | O que fazer |
| --- | --- |
| Fica em "Esperando o jogo…" com o jogo aberto | Feche e abra o medidor de novo. Se o seu antivírus bloqueou o Deucalion, adicione uma exceção para `C:\ProgramData\PrimalMeter`. |
| "Erro no motor" | O erro aparece na mensagem amarela. O detalhe fica em `%APPDATA%\PrimalMeter\motor.log`. Logo depois de um patch grande do jogo, o motor do ACT pode levar algumas horas para ser atualizado pelo autor; reabra o medidor mais tarde. |
| "Preparando…" não termina | O primeiro uso precisa de internet para baixar o motor. Confira a conexão e reabra. |
| O medidor não aparece por cima do jogo | Coloque o jogo em **Janela sem bordas**. |
| Não consigo clicar no medidor | Ele está travado. Aperte **Ctrl+Shift+L**. |
| O medidor sumiu | Aperte **Ctrl+Shift+H** ou clique no ícone na bandeja. |
| "Precisa do WebView2 Runtime" | Isso só acontece no Windows 10 antigo. Instale o [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) e abra de novo. |

---

## Como funciona

```
FFXIV ◄── Deucalion (injetado) ──pipe──┐
  │                                    ▼
  └── memória (só leitura) ──► motor do ACT (FFXIV_ACT_Plugin) ──► NotACT ──► interface (WebView2)
                                                                    │
                                              Documentos\PrimalMeter\Network_*.log
```

- **`src/PrimalMeter`**: app WPF (.NET 10). Janela transparente e sempre por cima, clique atravessando quando travada, atalhos globais e ícone na bandeja.
  - `Engine/`: hospeda o motor do ACT fora do jogo e desliga o Deucalion ao fechar.
  - `IinactClient.cs`: cliente WebSocket do IINACT, para quem escolher essa fonte.
- **`src/NotACT`**: camada que imita o Advanced Combat Tracker para o motor. Veio do [IINACT](https://github.com/marzent/IINACT).
- **`src/EngineSetup`**: baixa o motor da release oficial, extrai as DLLs embutidas e aplica as correções para o .NET moderno. O motor **nunca é redistribuído**: cada pessoa o baixa na própria máquina.
- **`web/index.html`**: a interface. Vai embutida no `.exe` e também funciona sozinha no navegador ou no Browsingway (`index.html?OVERLAY_WS=ws://127.0.0.1:10501/ws`).
- **`web/engine.js`**: transforma as linhas de log do ACT em lutas, totais e dano por habilidade (tipos 21/22 ações, 24 DoT/HoT, 25 mortes, 03/261 combatentes, 260 estado de combate).

## Desenvolvimento

Requisitos: .NET SDK 10 e Node.js (só para os testes e para editar a interface).

Antes de compilar pela primeira vez, baixe o motor do ACT para `external_dependencies/` (ele é usado só para compilar e nunca entra no repositório):

```bash
powershell -ExecutionPolicy Bypass -File tools/fetch-engine.ps1
```

```bash
dotnet run --project src/PrimalMeter
```

A versão de desenvolvimento usa configurações próprias (`%APPDATA%\PrimalMeter-dev`) e pode rodar ao lado do medidor instalado.

```bash
node tests/replay.js "%USERPROFILE%\Documents\PrimalMeter\Network_xxxxx.log"
```

```bash
node tools/serve.js
```

O último serve a interface em http://localhost:5173/ com a demonstração rodando, para mexer no visual sem abrir o app.

Para gerar o executável de distribuição (`dist/PrimalMeter.exe`, cerca de 67 MB, não precisa do .NET instalado):

```bash
powershell -ExecutionPolicy Bypass -File tools/publish.ps1
```

## Limitações conhecidas

- Com o motor embutido, os totais são calculados pelo `engine.js` a partir das linhas do motor do ACT. Nos testes, o resultado bateu com o do IINACT. Uma diferença conhecida: **escudos não contam como cura**, e no ACT contam.
- Logo depois de um patch grande do jogo, o medidor só volta a funcionar quando o autor do motor do ACT publicar a atualização, o que normalmente leva algumas horas.
- Clicar no medidor destravado tira o foco do jogo. Trave antes de jogar.

## Nome antigo

Até a versão 0.2.0, o projeto se chamava **Rhalgr Meter**. Na primeira vez que o Primal Meter abre, ele copia as configurações, o histórico de lutas e o motor já baixado do Rhalgr Meter. Depois disso, o `RhalgrMeter.exe` antigo pode ser apagado. Os logs gravados antes da troca continuam em `Documentos\RhalgrMeter` e podem ser abertos em "Reproduzir log…".

## Créditos e licença

O Primal Meter é software livre, distribuído sob a [GNU GPL v3](LICENSE).

- **[FFXIV_ACT_Plugin](https://github.com/ravahn/FFXIV_ACT_Plugin)**, de Ravahn: o motor de parse. Ele é baixado da release oficial na máquina de quem usa e não faz parte deste repositório.
- **[IINACT](https://github.com/marzent/IINACT)**, de marzent (GPL-3): a camada NotACT e a forma de preparar o motor para rodar fora do ACT.
- **[Deucalion](https://github.com/ff14wed/deucalion)**, de ff14wed: a captura dos pacotes de rede do jogo.
- **[Machina](https://github.com/ravahn/machina)**, de Ravahn: a leitura dos pacotes, usada pelo motor.

## Aviso

Ferramentas de terceiros violam os termos de serviço da Square Enix. Use para você e não comente números de outras pessoas no jogo.
