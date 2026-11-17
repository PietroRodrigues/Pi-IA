# Arquitetura

O Pi-IA é um **monólito de um único assembly** com estrutura plana: os módulos são pastas por responsabilidade dentro de `src/`, sem camadas separadas em projetos. Isso gera apenas um `bin`, centralizado em `artifacts/`.

```
src/
├── Abstracts/      (Pi.Abstracts.*)     ← contratos (interfaces)
├── Conversation/   (Pi.Conversation)    ← entidades da conversa
├── Models/         (Pi.Models)
├── Options/        (Pi.Options)
├── Services/       (Pi.Services)        ← orquestração
├── UseCases/       (Pi.UseCases)
├── Ollama/         (Pi.Ollama)          ← cliente HTTP do Ollama
├── Messaging/      (Pi.Messaging)       ← barramento em memória
├── Tools/          (Pi.Tools)           ← roteador + ferramentas
├── Workers/        (Pi.Workers)         ← host CLI
├── DependencyInjection.cs (Pi)          ← composition root
└── Program.cs                           ← ponto de entrada
```

## Módulos

### `Abstracts/`
As portas do sistema:

| Interface | Finalidade |
|---|---|
| `ILLMClient` | enviar prompt ao modelo de linguagem |
| `IMessageBus` | transportar mensagens entre workers |
| `ITool` | representar uma ferramenta invocável |

### `Conversation/`
Entidades da conversa: `Conversation` (histórico limitado pelo próprio agregado), `ChatMessage` e `MessageRole`. Não referencia frameworks, HTTP, dispositivos nem outros módulos.

### `Services/` e `UseCases/`
`AgentService` monta o prompt com o histórico, consulta as ferramentas e chama `ILLMClient`. `ToolRouter` escolhe a ferramenta pelo prefixo. `ProcessMessageUseCase` é o caso de uso invocado pelo worker.

### `Ollama/`, `Messaging/` e `Tools/`
Implementações concretas: `OllamaClient` (`POST /api/generate`), `OllamaResponseParser` (JSON/NDJSON), `ChannelMessageBus` (`System.Threading.Channels`), `SearchFilesTool`, `OpenAppTool` e `ToolNames`.

### `Models/`, `Options/` e `DependencyInjection.cs`
`AssistantMessage` é o DTO da fila. As classes de opções são validadas na subida (`ValidateOnStart`). O `DependencyInjection.cs` registra tudo em um único lugar, com `AddApplication()` e `AddInfrastructure()`.

### `Workers/` e `Program.cs`
Host genérico .NET:

- `ConsoleInputWorker` lê `Console.In`, trata `sair`/`exit` e publica na fila;
- `MessageProcessingWorker` consome a fila e imprime a resposta;
- `Ctrl+C` chama `IHostApplicationLifetime.StopApplication()`, garantindo desligamento gracioso com `CancellationToken`.

## Fluxo de uma mensagem

```
Console
   │
   ▼
IMessageBus (canal limitado)
   │
   ▼
MessageProcessingWorker → ProcessMessageUseCase → AgentService
                                                   ├─ ToolRouter (se casar)
                                                   └─ ILLMClient (senão)
   │
   ▼
Console.Out ("Pi: ...")
```

Logs usam `ILogger<T>` e são enviados para **stderr**; a resposta do assistente usa **stdout**.

## Guardrails

Como não há projetos separados para impor limites, `tests/Pi.Tests/StructureTests.cs` verifica:

1. `Abstracts/` não referencia implementações concretas (`Pi.Ollama`, `Pi.Messaging`, `Pi.Tools`, `Pi.Workers`) nem `Microsoft.Extensions`;
2. `Conversation/` permanece isolado dos demais módulos;
3. a API pública dos tipos de `Pi.Abstracts` não expõe tipos concretos (reflection).

## Build e saída

`Directory.Build.props` define `ArtifactsPath=artifacts`. Consequentemente:

- não existem pastas `bin/` ou `obj/` dentro de `src/` ou `tests/`;
- toda a saída fica em `artifacts/{bin,obj}/<projeto>/<config>/<framework>`;
- `artifacts/` está no `.gitignore`.

A solution `Pi.slnx` contém apenas dois projetos: `Pi.CLI` (aplicação) e `Pi.Tests` (testes).

## Decisões

- **Estrutura plana:** um assembly, módulos por pasta/namespace — navegação curta e um único `bin`.
- **Guardrail por testes:** substitui a fiscalização que os project references davam antes.
- **DI e Options Pattern** eliminam `new` direto e permitem substituir implementações nos testes.
- **Ferramentas** são descobertas via `IEnumerable<ITool>`; adicionar uma ferramenta não exige alterar o roteador.
- **Histórico com papéis** (`User`/`Assistant`) substitui a concatenação de strings.
- **Bus limitado** evita crescimento sem controle da fila de mensagens.
