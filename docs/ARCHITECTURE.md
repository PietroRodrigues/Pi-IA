# Arquitetura

O Pi-IA é um **monólito de um único assembly** com quatro responsabilidades
claras em `src/`: `Core` decide, `Tools` executa ações locais, `Connections`
fala com o mundo externo (Ollama hoje, MCP no futuro) e `Bridge` é a ponte
NDJSON usada pela TUI. Não existe mais interface textual em C#.

```text
src/
├── Core/                           ← decisões, sem Ollama/UI/MCP
│   ├── Chat/
│   │   ├── AgentService.cs         (prompt, histórico, despacho e streaming)
│   │   ├── Conversation.cs         (histórico limitado)
│   │   ├── ChatMessage.cs          (mensagem + MessageRole)
│   │   └── IAgentEventSink.cs      (eventos; implementação nula como padrão)
│   └── Contracts/
│       ├── ILLMClient.cs           (porta do modelo)
│       ├── IStreamingLLMClient.cs  (extensão opcional de streaming)
│       ├── LLMStreamChunk.cs       (chunk genérico, sem Ollama)
│       └── ITool.cs
│
├── Tools/                          ← ferramentas locais e roteamento
│   ├── ToolRouter.cs
│   ├── ToolNames.cs
│   ├── SearchFilesTool.cs
│   └── OpenAppTool.cs
│
├── Connections/                    ← provedores e conexões externas
│   ├── Ollama/
│   │   ├── OllamaClient.cs         (POST /api/generate)
│   │   ├── OllamaResponseParser.cs
│   │   ├── OllamaBootstrapper.cs
│   │   └── OllamaProcessLauncher.cs
│   └── Mcp/
│       └── IMcpConnection.cs       (extensão futura, sem implementação)
│
├── Bridge/                         ← única interface: ponte NDJSON da TUI
│   ├── BridgeProtocol.cs           (DTOs de entrada e saída)
│   ├── JsonLinesWriter.cs          (escritor exclusivo do stdout)
│   ├── BridgeAgentEventSink.cs     (eventos do agente -> NDJSON)
│   ├── StdioBridgeWorker.cs        (loop de leitura e despacho)
│   └── BridgeRegistration.cs       (AddBridge)
│
├── Options/                        (configurações tipadas)
├── DependencyInjection.cs          (composition root)
└── Program.cs                      (exige --bridge stdio)
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
        └── Footer.tsx
```

## Regras de dependência

| De | Pode referenciar | Não pode referenciar |
|---|---|---|
| `Bridge/` | `Pi.Core.Chat` (`AgentService`, eventos), `Pi.Options`, `Microsoft.Extensions.*`, BCL | qualquer outro namespace do Core |
| `Core/` | BCL, `Microsoft.Extensions.*`, `Pi.Options`, `Pi.Tools` | `Pi.Bridge`, `Pi.Connections` |
| `Tools/` | `Pi.Core.Chat`, `Pi.Core.Contracts`, `Pi.Options` | `Pi.Bridge`, `Pi.Connections` |
| `Connections/` | `Pi.Core.Contracts`, `Pi.Options` | `Pi.Bridge`, `Pi.Core.Chat` |
| `ChatMessage.cs` e `Conversation.cs` | apenas BCL | qualquer `using` externo |
| `DependencyInjection.cs` / `Program.cs` | todas as camadas | — |

A dependência aponta em um único sentido:

```text
Bridge            ──► Core.Chat (AgentService, IAgentEventSink)
Core              ──► Tools (ITool, ToolRouter)
Connections       ──► Core.Contracts (ILLMClient, ITool)
Core              ──X──► Connections / Bridge
```

O `AgentService` e os eventos ficam em `Pi.Core.Chat` porque são usados tanto
pelo Core quanto pela ponte. A composition root é o único arquivo que conhece
todas as camadas.

## Guardrails

`tests/Pi.Tests/StructureTests.cs` verifica quatro regras curtas, sem reflection:

1. `Core` não referencia `Pi.Bridge` nem `Pi.Connections` (Ollama/MCP);
2. `Bridge` referencia somente `Pi.Core.Chat`;
3. os tipos da antiga CLI textual (`ConsoleChatView`, `ChatSession`,
   `ChatWorker`, `IChatView`, `NullChatView`, `ProcessMessage`) não reaparecem
   em `src/`;
4. `ChatMessage` e `Conversation` não têm `using` externo à BCL.

Os testes espelham a estrutura:

```text
tests/Pi.Tests/
├── Core/                (AgentService, Conversation, ChatMessage, ...)
├── Tools/               (ToolRouter)
├── Connections/Ollama/  (parser e bootstrapper)
├── Bridge/              (JsonLinesWriter)
└── StructureTests.cs
```

## Fluxo de uma mensagem

```text
prompt (NDJSON no stdin)
        │
        ▼
StdioBridgeWorker ──► AgentService
                      ├─ ToolRouter  (se casar)
                      └─ ILLMClient  (senão)
        │
        ▼
BridgeAgentEventSink ──► token / thought / tool_call /
                         tool_result / context_stats / done
```

Nada é desenhado no terminal pelo C#: a TUI recebe os eventos e monta a
transcrição. Cada prompt tem `requestId` e `CancellationToken` próprios, então
pedidos cancelados não disputam a saída.

## Bridge: ponte NDJSON

`StdioBridgeWorker` lê `prompt`, `cancel` e `clear` do stdin. `cancel`
interrompe a geração e `clear` cancela tudo, limpa `Conversation` e zera
`context_stats`.

`JsonLinesWriter` serializa um evento por linha com flush imediato e é o único
produtor de stdout. Todos os `ILogger` vão para stderr — inclusive os do host —
e o processo `ollama serve` tem stdout/stderr redirecionados para não herdar o
pipe da TUI.

`AgentService` usa `BuildPrompt()`, `ToolRouter` e `Conversation`; com
`AskStreamAsync()` transmite chunks via `IStreamingLLMClient` e publica em
`IAgentEventSink`. `NullAgentEventSink` permanece como padrão para chamadas
sem sink (por exemplo, o caminho direto de `ToolRouter`).

A TUI filtra eventos pelo `requestId` ativo: tokens, pensamentos e ferramentas
que chegam depois de um `cancel` ou `clear` são descartados, enquanto o `done`
ainda é aceito para fechar a bolha. Durante a geração o input exibe
`Gerando… Esc para cancelar`; `Esc` e `Ctrl+C` cancelam o pedido, e `Ctrl+C`
com a TUI ociosa encerra a aplicação.

A TUI inicia o DLL já compilado (`artifacts/bin/Pi.CLI/debug/Pi.CLI.dll`), não
`dotnet run`, para evitar que mensagens de build corrompam o parser. Ao sair,
envia `cancel`, fecha o stdin e mata o processo filho; não há servidor HTTP,
porta ou firewall envolvidos.

**Sem `--bridge stdio`:** `Program.cs` não cria o host; escreve o uso no stderr
e encerra com código 1. Assim `ValidateOnBuild` nunca é exercitado nesse
caminho e nenhum texto acidental polui o stdout.

## Core, Ferramentas e Conexões

- **Core/Chat/**: `AgentService` monta o prompt, consulta ferramentas, mantém o
  histórico e publica eventos no `IAgentEventSink`. `Conversation` limita o
  histórico e `ChatMessage` representa cada mensagem.
- **Core/Contracts/**: `ILLMClient` e `IStreamingLLMClient` são as portas;
  `LLMStreamChunk` é o pedaço genérico de streaming, sem dependência de Ollama.
- **Tools/**: despacho por prefixo (`procure`, `abra`) e pastas permitidas;
  `ToolRouter` emite `tool_call`/`tool_result` quando recebe um sink.
- **Connections/Ollama/**: `OllamaClient` fala com `/api/generate` e converte os
  chunks em `LLMStreamChunk`; `OllamaBootstrapper` sobe `ollama serve` quando
  necessário e espera o servidor responder. É registrado em
  `AddInfrastructure()` **antes** do hosted service da ponte, porque o host
  executa `IHostedService.StartAsync` em ordem.
- **Connections/Mcp/**: `IMcpConnection` é só o ponto de extensão para futuras
  conexões MCP; não há implementação nem dependência de pacote por enquanto.

`Program.cs` liga `Ctrl+C` a `IHostApplicationLifetime.StopApplication()`,
garantindo desligamento gracioso via `CancellationToken`.

## Build e saída

`Directory.Build.props` define `ArtifactsPath=artifacts`. Não existem pastas
`bin/` ou `obj/` dentro de `src/` ou `tests/`: toda a saída fica em
`artifacts/{bin,obj}/<projeto>/<config>/<framework>`, e `artifacts/` está no
`.gitignore`.

A solution `Pi.slnx` contém dois projetos: `Pi.CLI` e `Pi.Tests`.

## Decisões

- **Quatro responsabilidades:** `Core`, `Tools`, `Connections` e `Bridge`, fáceis de localizar ao abrir `src/`.
- **Poucos namespaces:** `Pi.Core`, `Pi.Core.Chat`, `Pi.Core.Contracts`,
  `Pi.Tools`, `Pi.Connections.Ollama`, `Pi.Connections.Mcp`, `Pi.Bridge`,
  `Pi.Options`.
- **Uma interface, um Core:** a TUI em TypeScript é a única interface; o C# só
  expõe o protocolo por `--bridge stdio`.
- **Guardrails curtos:** quatro regras, sem reflection.
- **stdout exclusivo do NDJSON:** logs e diagnósticos ficam no stderr/arquivos.
- **Eventos com `requestId`:** evita disputa pela saída entre pedidos.
