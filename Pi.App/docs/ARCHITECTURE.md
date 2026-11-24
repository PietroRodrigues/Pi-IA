# Arquitetura

O Pi-IA é um sistema modular com um projeto pai (`Pi.App`), dois módulos de aplicação (`Modulos/`) e um módulo de testes (`tests/`). Cada módulo segue **MVC** na sua própria fronteira; o único contrato entre eles é o protocolo **NDJSON via stdin/stdout**.

```text
Pi-IA/                          configuração compartilhada (.NET SDK, pacotes, artifacts/)
└── Pi.App/                     projeto pai .NET — entry point e dono dos módulos
    ├── Pi.App.slnx             solution que orquestra Pi.App + BodyTools + BodyTools.Tests
    ├── Model/ View/ Controller/ MVC do pai (ciclo de vida)
    ├── Modulos/
    │   ├── BodyTools/          backend .NET — Model / Controller / View
    │   └── interfaceTui/       TUI Bun + OpenTUI — model / view / controller / infra
    ├── tests/BodyTools.Tests/  guardrails e testes do backend
    ├── docs/
    └── scripts/
```

## Projeto pai: Pi.App

O `Pi.App` é o entry point único e o único projeto .NET que "consome" os dois módulos. Ele não fala NDJSON e não substitui a TUI: o consumo do `BodyTools` é em tempo de build (`ProjectReference`) e o da `interfaceTui` é de processo (`bun run dev:debug` com stdio herdado).

```text
Pi.App/
├── Model/          ModuleManifest (descobre/valida os módulos), RuntimeState
├── View/           LauncherView (texto do launcher, sempre no stderr)
├── Controller/     RuntimeController (ciclo de vida e exit code)
├── Modulos/        BodyTools e interfaceTui
├── tests/          BodyTools.Tests (guardrails e testes do backend)
├── docs/  scripts/
├── Pi.App.slnx
└── Program.cs      composition root
```

1. `ModuleManifest.Discover()` sobe de `AppContext.BaseDirectory` até encontrar `global.json` + `Pi.App/Pi.App.slnx`; daí deriva o DLL do backend e a pasta da TUI em `Pi.App/Modulos/interfaceTui`.
2. `Validate()` falha cedo se faltar solução, DLL ou `package.json`.
3. Se `node_modules` não existir, roda `bun install --frozen-lockfile`.
4. Inicia `bun run dev:debug`. Com `PI_TUI_INSPECT` definido, repassa `--inspect=<porta>` para o Bun.
5. No Ctrl+C, cancela a espera e mata a árvore com `Kill(entireProcessTree: true)`; o exit code da TUI é propagado.
6. `PI_SKIP_TUI=1` valida os módulos sem abrir terminal — usado em smoke/CI.

A TUI continua dona do terminal e é ela que spawna `artifacts/bin/BodyTools/debug/BodyTools.dll` via pipe NDJSON. Se o pai virasse o broker do protocolo, seria preciso trocar stdin/stdout por socket — fora de escopo. Por isso um único F5 inicia os dois módulos, mas o attach de C# e o do Bun seguem sendo configurações separadas.

## Backend: BodyTools

```text
BodyTools/
├── Model/
│   ├── Chat/                   AgentService, Conversation, ChatMessage, IAgentEventSink
│   ├── Contracts/              ILLMClient, IStreamingLLMClient, ITool, LLMStreamChunk
│   ├── Tools/                  ToolRouter, ToolNames, SearchFilesTool, OpenAppTool
│   └── Connections/
│       ├── Ollama/             OllamaClient, parser, bootstrapper, process launcher
│       └── Mcp/                IMcpConnection (extensão futura)
├── Controller/
│   ├── BridgeController.cs     loop de stdin, requestId, cancel/clear, despacho
│   └── ControllerRegistration.cs
├── View/
│   ├── BridgeProtocol.cs       DTOs de entrada e saída
│   ├── BridgeView.cs           fachada tipada de serialização
│   ├── BridgeEventSink.cs      IAgentEventSink -> View
│   ├── JsonLinesWriter.cs      único produtor de stdout
│   └── ViewRegistration.cs
├── Options/
├── DependencyInjection.cs      AddModel / AddInfrastructure / AddView / AddController
└── Program.cs                  exige --bridge stdio
```

### Responsabilidades MVC

| Camada | Faz | Não faz |
|---|---|---|
| **Model** | decisões do agente, histórico, ferramentas, conexões, portas `ILLMClient`/`ITool` | conhecer Controller, View, stdin/stdout |
| **Controller** | ler NDJSON, validar `type`, criar `CancellationToken` por `requestId`, tratar `cancel`/`clear`, chamar Model | serializar eventos, decidir layout |
| **View** | construir os DTOs e escrever uma linha JSON por evento com flush | ler stdin, chamar ferramentas, conhecer Ollama |

### Regras de dependência

| De | Pode referenciar | Não pode referenciar |
|---|---|---|
| `Model/` | BCL, `Microsoft.Extensions.*`, `Pi.Options`, `Pi.Model.*` | `Pi.Controller`, `Pi.View`, `interfaceTui` |
| `Controller/` | `Pi.Model.*`, `Pi.View`, `Microsoft.Extensions.*` | `interfaceTui` |
| `View/` | DTOs do protocolo e `Pi.Model.Chat` (eventos) | `Pi.Controller`, `Pi.Model.Tools`, `Pi.Model.Connections` |
| `ChatMessage.cs` e `Conversation.cs` | apenas BCL | qualquer `using` externo |
| `DependencyInjection.cs` / `Program.cs` | todas as camadas | — |

`AgentService` fica em `Model/Chat` porque é a regra de negócio do chat; `BridgeEventSink` implementa `IAgentEventSink` para levar os eventos do Model até a View sem acoplar as camadas. A composition root é o único arquivo que conhece todas as camadas.

## Frontend: interfaceTui

```text
interfaceTui/
├── package.json                name: interfaceTui
├── src/
│   ├── main.tsx                renderer em alternate screen; só monta a View
│   ├── model/ChatModel.ts      sinais Solid, mensagens, status, requestId ativo
│   ├── controller/AppController.ts
│   │                           eventos NDJSON, teclado, submit, cancel/clear, shutdown
│   ├── view/
│   │   ├── App.tsx             monta a cena e delega ao Controller
│   │   ├── MessageList.tsx
│   │   ├── Composer.tsx
│   │   ├── Footer.tsx
│   │   └── theme.ts
│   └── infra/
│       ├── bridge/{client.ts, protocol.ts}   spawn do dotnet e parser de linhas
│       └── debug/monitor.ts                  diagnósticos em arquivo
├── tests/                      bun test: model, controller, bridge e monitor
└── scripts/                    build, debug, logs e check:arch
```

- **Model** não conhece a ponte nem a View.
- **View** recebe o Model e uma porta `ControllerViewPort` (`focusInput`, `close`); não importa nada de `infra/`.
- **Controller** concentra as decisões: traduz eventos em chamadas ao Model, interpreta teclado, obtém o monitor de debug, controla o ciclo de vida e encerra o processo filho.
- **Infra** são adaptadores: o `BridgeClient` inicia o DLL, separa linhas NDJSON e garante que o subprocesso morra junto com a TUI.

## Fluxo de uma mensagem

```text
View (TUI) ──submit──► Controller (TUI) ──prompt NDJSON──► Controller (BodyTools)
                                                              │
                                                              ▼
                                                        Model (AgentService)
                                                          ├─ ToolRouter
                                                          └─ ILLMClient (Ollama)
                                                              │
                                                              ▼
View (BodyTools) ──token/thought/tool_call/tool_result/stats/done──► Controller (TUI)
                                                              │
                                                              ▼
                                                        Model (TUI) ──► View (TUI)
```

Cada prompt tem `requestId` e `CancellationToken` próprios. A TUI descarta tokens/pensamentos/ferramentas de pedidos cancelados, mas aceita o `done` para fechar a bolha. Durante a geração o input mostra `Gerando… Esc para cancelar`; `Esc` e `Ctrl+C` cancelam, e `Ctrl+C` com a TUI ociosa encerra a aplicação.

## Protocolo NDJSON

`BridgeProtocol.cs` (BodyTools) e `infra/bridge/protocol.ts` (TUI) espelham o mesmo contrato:

- **TUI → BodyTools:** `prompt`, `cancel`, `clear`.
- **BodyTools → TUI:** `ready`, `context_stats`, `token`, `thought`, `tool_call`, `tool_result`, `done`, `error`.

Nomes em `camelCase` vêm de `JsonSerializerDefaults.Web`. `JsonLinesWriter` é o único produtor de stdout, com flush imediato e semáforo para não intercalar linhas de pedidos simultâneos. Todos os `ILogger` vão para stderr; o `ollama serve` tem stdout/stderr redirecionados.

`BridgeController` publica `ready` e `context_stats` na subida. `cancel` interrompe um pedido; `clear` cancela todos, limpa `Conversation` e zera as estatísticas. `Program.cs` liga `Ctrl+C` a `IHostApplicationLifetime.StopApplication()`.

## Guardrails

- `tests/BodyTools.Tests/StructureTests.cs`: módulos sob `Pi.App/Modulos/` e testes sob `Pi.App/tests/`; `Pi.App` com `Model`/`View`/`Controller` e sem chat/ponte duplicados; Model isolado de Controller/View; View limitada a `Pi.Model.Chat`; Controller como único leitor de stdin; entidades da conversa sem `using`; tipos antigos (`Pi.CLI`, `StdioBridgeWorker`, CLI textual) proibidos.
- `interfaceTui/tests/` (`bun run test`, usando `bun:test`): `ChatModel` (streaming, cancel/done, clear), `AppController` (eventos, teclado, submit, shutdown), `BridgeClient` (buffer NDJSON e mensagens de entrada) e `DebugMonitor` (raiz do repo e arquivos de log). Não dependem de dotnet, Ollama, rede ou renderer.
- `interfaceTui/scripts/check-architecture.ts` (`bun run check:arch`): pastas MVC presentes; `model/` não importa `infra`/`view`/`controller`; `view/` não importa `infra`; `controller/` não importa `@opentui`; cliente aponta para `BodyTools.dll`; pacote se chama `interfaceTui`; existe ao menos um `*.test.ts`.

## Build e saída

`Directory.Build.props` e `Directory.Packages.props` ficam na raiz do repositório para centralizar versão de SDK, pacotes e `ArtifactsPath`. Não existem `bin/` ou `obj/` dentro dos módulos: a saída vai para `artifacts/{bin,obj}/<projeto>/<config>/` (sem nível de framework, pois o SDK já unifica o target).

A TUI inicia `artifacts/bin/BodyTools/debug/BodyTools.dll` (já compilado) em vez de `dotnet run`. A solução `Pi.App/Pi.App.slnx` contém `Pi.App`, `BodyTools` e `BodyTools.Tests`; o entry point é `dotnet run --project Pi.App`, que produz `artifacts/bin/Pi.App/debug/Pi.App.dll`.

## Decisões

- **Projeto pai + dois módulos:** `Pi.App` orquestra o ciclo de vida; `BodyTools` e `interfaceTui` mantêm MVC próprio.
- **Pai não é broker:** a TUI continua dona do terminal e do pipe NDJSON com o backend; o pai só inicia, aguarda e encerra.
- **Contrato único:** NDJSON; nenhuma chamada direta entre C# e TypeScript.
- **Um assembly no backend:** as camadas MVC são pastas/namespaces dentro de `BodyTools`, com guardrails no lugar de projetos separados.
- **stdout exclusivo do NDJSON:** logs e diagnósticos ficam no stderr/arquivos.
- **Eventos com `requestId`:** evita disputa pela saída entre pedidos.
