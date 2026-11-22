# Arquitetura

O Pi-IA é um **monólito de um único assembly** com uma regra simples de
organização: dentro de `src/`, tudo que fala com o usuário fica em `Frontend/`,
tudo que decide fica em `Backend/`.

```text
src/
├── Frontend/                       ← duas formas de falar com o usuário
│   ├── ConsoleChatView.cs          (CLI: implementa IChatView com WriteLine/ReadLine)
│   └── Bridge/                     (TUI: ponte NDJSON via stdin/stdout)
│       ├── BridgeProtocol.cs       (DTOs de entrada e saída)
│       ├── JsonLinesWriter.cs      (escritor exclusivo do stdout)
│       ├── BridgeAgentEventSink.cs (eventos do agente -> NDJSON)
│       ├── StdioBridgeWorker.cs    (loop de leitura e despacho)
│       └── BridgeRegistration.cs   (AddBridge)
│
├── Backend/                        ← regras, IA e ferramentas
│   ├── Chat/
│   │   ├── IChatView.cs            (contrato da CLI — ponte com o Frontend)
│   │   ├── IAgentEventSink.cs      (eventos para a ponte; implementação nula na CLI)
│   │   ├── ChatSession.cs          (loop da CLI)
│   │   ├── ChatWorker.cs           (adaptador do host)
│   │   ├── AgentService.cs         (prompt, histórico, despacho e streaming)
│   │   ├── ProcessMessage.cs       (caso de uso de uma mensagem)
│   │   ├── Conversation.cs         (histórico limitado)
│   │   └── ChatMessage.cs          (mensagem + MessageRole)
│   ├── AI/
│   │   ├── ILLMClient.cs           (porta do modelo)
│   │   ├── IStreamingLLMClient.cs  (extensão opcional de streaming)
│   │   ├── OllamaStreamChunk.cs    (chunk + contagens de contexto)
│   │   ├── OllamaClient.cs         (POST /api/generate)
│   │   └── OllamaResponseParser.cs
│   ├── Tools/
│   │   ├── ITool.cs
│   │   ├── ToolRouter.cs
│   │   ├── ToolNames.cs
│   │   ├── SearchFilesTool.cs
│   │   └── OpenAppTool.cs
│   └── Bootstrap/
│       ├── OllamaProcessLauncher.cs
│       └── OllamaBootstrapper.cs
│
├── Options/                        (configurações tipadas)
├── DependencyInjection.cs          (composition root)
└── Program.cs
```

```text
tui/                                ← projeto Bun + OpenTUI (isolado do .csproj)
├── bunfig.toml                     (preload do Solid)
├── package.json
├── tsconfig.json
└── src/
    ├── main.tsx                    (renderer em alternate screen)
    ├── state.ts                    (sinais Solid do chat)
    ├── theme.ts                    (#0A0A0A / #141414 / #1E1E1E / #F59E0B)
    ├── bridge/
    │   ├── protocol.ts             (tipos NDJSON espelhando os DTOs C#)
    │   └── client.ts               (spawn do dotnet + parser de linhas)
    └── components/
        ├── App.tsx
        ├── MessageList.tsx
        ├── Composer.tsx
        ├── Sidebar.tsx
        └── Footer.tsx
```

## Regras de dependência

| De | Pode referenciar | Não pode referenciar |
|---|---|---|
| `Frontend/` | `Pi.Backend.Chat.IChatView` e BCL | qualquer outra implementação do Backend |
| `Backend/` | BCL, `Microsoft.Extensions.*`, `Pi.Options` | `Pi.Frontend` |
| `Backend/Chat/ChatMessage.cs` e `Conversation.cs` | apenas BCL | qualquer `using` externo |
| `DependencyInjection.cs` / `Program.cs` | Frontend e Backend | — |

A dependência aponta em um único sentido:

```text
Frontend ──► Backend.Chat.IChatView
Backend  ──X──► Frontend
```

`IChatView` fica no Backend porque é o ChatSession quem consome o contrato.
As implementações ficam no Frontend. A composition root é o único arquivo que
conhece os dois lados.

## Guardrails

`tests/Pi.Tests/StructureTests.cs` fiscaliza apenas três regras:

1. `Backend` não referencia `Pi.Frontend`;
2. `Frontend` referencia somente `Pi.Backend.Chat` (na prática, `IChatView`);
3. `ChatMessage` e `Conversation` não têm `using` externo à BCL.

Os testes espelham a estrutura:

```text
tests/Pi.Tests/
├── Backend/     (ChatSession, AgentService, ToolRouter, Conversation, Ollama)
├── Frontend/    (ConsoleChatView)
└── StructureTests.cs
```

## Fluxo de uma mensagem

```text
IChatView.ReadInputAsync  (Frontend lê uma linha do stdin)
        │
        ▼
ChatSession ──► IChatView.BeginThinking (status "pensando")
        │
        ▼
ProcessMessage ──► AgentService
                   ├─ ToolRouter  (se casar)
                   └─ ILLMClient  (senão)
        │
        ▼
IChatView.ShowReply / ShowError
```

O loop é **sequencial**: nunca duas partes desenham no terminal ao mesmo tempo.
Logs usam `ILogger<T>` e vão para **stderr**; a interface usa **stdout**.

## Frontend

Existem dois frontends, escolhidos em `Program.cs`:

| Modo | Entrada | Saída | Eventos |
|---|---|---|---|
| padrão | `ConsoleChatView` (`IChatView`) | stdout textual | nenhum |
| `--bridge stdio` | `StdioBridgeWorker` | stdout **somente NDJSON** | `ready`, `token`, `thought`, `tool_call`, `tool_result`, `context_stats`, `done`, `error` |

### CLI textual

`ConsoleChatView.cs` é a única implementação de `IChatView`: linhas no stdout,
leitura do stdin. Não existe alternate screen, ANSI, spinner, editor de linha
nem leitura de mouse. O teclado é tratado apenas pelo terminal e pelo sistema
operacional, então layout ABNT2, acentos e pontuação seguem o padrão
configurado pelo usuário. Terminal, arquivo e pipe recebem a mesma saída.

### Ponte NDJSON / TUI

`StdioBridgeWorker` lê `prompt`, `cancel` e `clear` do stdin. Cada prompt ganha
um `requestId` e um `CancellationToken` próprio; `cancel` interrompe a geração e
`clear` cancela tudo, limpa `Conversation` e zera `context_stats`.

`JsonLinesWriter` serializa um evento por linha com flush imediato e é o único
produtor de stdout no modo bridge. Todos os `ILogger` continuam indo para
stderr — inclusive os do host — e o processo `ollama serve` tem stdout/stderr
redirecionados para não herdar o pipe da TUI.

O caminho do prompt é o mesmo da CLI: `AgentService` usa `BuildPrompt()`,
`ToolRouter` e `Conversation`. A única diferença é `AskStreamAsync()`, que
transmite chunks via `IStreamingLLMClient` e publica eventos em
`IAgentEventSink`. A CLI usa `NullAgentEventSink`, então seu comportamento não
mudou.

A TUI filtra eventos pelo `requestId` ativo: tokens, pensamentos e ferramentas
que chegam depois de um `cancel` ou `clear` são descartados, enquanto o `done`
ainda é aceito para fechar a bolha. Durante a geração o input exibe
`Gerando… Esc para cancelar`; `Esc` e `Ctrl+C` cancelam o pedido, e `Ctrl+C`
com a TUI ociosa encerra a aplicação.

A TUI inicia o DLL já compilado (`artifacts/bin/Pi.CLI/debug/Pi.CLI.dll`), não
`dotnet run`, para evitar que mensagens de build corrompam o parser. Ao sair,
envia `cancel`, fecha o stdin e mata o processo filho; não há servidor HTTP,
porta ou firewall envolvidos.

## Backend

- **Chat/**: `ChatSession` é o loop da conversa — mostra boas-vindas, lê a
  entrada, reconhece `sair`/EOF, aciona o indicador de pensamento, chama o caso
  de uso e exibe resposta ou erro. Não conhece detalhes do terminal nem
  escreve em `Console.*`.
- **AI/**: `ILLMClient` é a porta; `OllamaClient` fala com `/api/generate` e
  implementa `IStreamingLLMClient` para envio de chunks ao vivo.
- **Tools/**: despacho por prefixo (`procure`, `abra`) e pastas permitidas;
  `ToolRouter` emite `tool_call`/`tool_result` quando recebe um sink.
- **Bootstrap/**: sobe `ollama serve` quando necessário e espera o servidor
  responder. É registrado em `AddInfrastructure()` **antes** do `ChatWorker`,
  porque o host executa `IHostedService.StartAsync` em ordem.

`Program.cs` liga `Ctrl+C` a `IHostApplicationLifetime.StopApplication()`,
garantindo desligamento gracioso via `CancellationToken`.

## Build e saída

`Directory.Build.props` define `ArtifactsPath=artifacts`. Não existem pastas
`bin/` ou `obj/` dentro de `src/` ou `tests/`: toda a saída fica em
`artifacts/{bin,obj}/<projeto>/<config>/<framework>`, e `artifacts/` está no
`.gitignore`.

A solution `Pi.slnx` contém dois projetos: `Pi.CLI` e `Pi.Tests`.

## Decisões

- **Dois troncos apenas:** `Frontend` e `Backend`, fáceis de localizar ao abrir `src/`.
- **Poucos namespaces:** `Pi.Frontend`, `Pi.Backend.Chat`, `Pi.Backend.AI`,
  `Pi.Backend.Tools`, `Pi.Backend.Bootstrap`, `Pi.Options`.
- **Dois frontends, um backend:** CLI e TUI compartilham prompt, ferramentas e histórico.
- **Guardrail mínimo:** três regras curtas, sem reflection.
- **Saída da CLI sempre textual:** terminal, pipes e arquivos recebem o mesmo texto sem ANSI; no modo bridge, stdout é NDJSON puro.
- **Loop sequencial na CLI; eventos com `requestId` na TUI:** evita disputa pela saída.
