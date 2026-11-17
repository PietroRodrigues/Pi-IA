# Arquitetura

O Pi-IA é um **monólito de um único assembly** com estrutura plana: os módulos são pastas por responsabilidade dentro de `src/`, sem camadas separadas em projetos. Isso gera apenas um `bin`, centralizado em `artifacts/`.

```
src/
├── Abstracts/      (Pi.Abstracts.*)     ← contratos (interfaces)
├── Conversation/   (Pi.Conversation)    ← entidades da conversa
├── Options/        (Pi.Options)
├── Services/       (Pi.Services)        ← lógica da conversa
├── UseCases/       (Pi.UseCases)
├── Ollama/         (Pi.Ollama)          ← cliente HTTP + auto-start do servidor
├── UI/             (Pi.UI)              ← interface do terminal (Spectre.Console)
├── Tools/          (Pi.Tools)           ← roteador + ferramentas
├── Workers/        (Pi.Workers)         ← adaptadores do host
├── DependencyInjection.cs (Pi)          ← composition root
└── Program.cs                           ← ponto de entrada
```

## Módulos

### `Abstracts/`
As portas do sistema:

| Interface | Finalidade |
|---|---|
| `ILLMClient` | enviar prompt ao modelo de linguagem (e expor `ModelName`) |
| `ITool` | representar uma ferramenta invocável |

### `Conversation/`
Entidades da conversa: `Conversation` (histórico limitado pelo próprio agregado), `ChatMessage` e `MessageRole`. Não referencia frameworks, HTTP, dispositivos nem outros módulos.

### `Services/` e `UseCases/`
`AgentService` monta o prompt com o histórico, consulta as ferramentas e chama `ILLMClient`. `ToolRouter` escolhe a ferramenta pelo prefixo. `ProcessMessageUseCase` recebe o texto digitado e chama o agente.

`ChatSession` contém a **lógica da conversa**: mostra boas-vindas, lê a entrada, reconhece `sair`/EOF, aciona o indicador de pensamento, chama o caso de uso e exibe a resposta ou o erro. Não conhece Spectre.Console nem escreve em `Console.*` — depende apenas de `IChatView`.

### `UI/`
Responsabilidade exclusiva de apresentação:

| Tipo | Finalidade |
|---|---|
| `IChatView` | contrato da tela; a lógica o consome sem saber como ele desenha |
| `SpectreChatView` | painéis, cores, prompt estilizado e spinner com Spectre.Console |
| `ChatTheme` | cores centralizadas para manter a identidade visual consistente |

Quando a saída está redirecionada, o Spectre desliga cores e animação: a interface vira texto simples com bordas, legível em pipes, arquivos e CI.

### `Ollama/` e `Tools/`
Implementações concretas: `OllamaClient` (`POST /api/generate`), `OllamaResponseParser` (JSON/NDJSON), `SearchFilesTool`, `OpenAppTool` e `ToolNames`.

Além disso, `Ollama/` contém o **auto-start do servidor local**:

- `IOllamaProcessLauncher` / `OllamaProcessLauncher` localizam o executável (PATH, `%LOCALAPPDATA%\Programs\Ollama`, `/Applications/Ollama.app`, `/usr/local/bin`) e disparam `ollama serve` com `CreateNoWindow`.
- `OllamaServerBootstrapper` sonda `GET /api/tags`, decide se precisa iniciar o processo e espera até a porta responder. Falhas são logadas sem derrubar o host; o processo iniciado é encerrado apenas se `StopOnExit=true`.
- O bootstrapper é registrado em `AddInfrastructure()` **antes** do worker do chat, porque o host executa `IHostedService.StartAsync` em ordem. Assim o console só começa a ler depois da verificação.

### `Options/` e `DependencyInjection.cs`
As classes de opções são validadas na subida (`ValidateOnStart`). O `DependencyInjection.cs` registra tudo em um único lugar, com `AddApplication()` (apresentação, lógica e caso de uso) e `AddInfrastructure()` (HTTP, auto-start e ferramentas).

### `Workers/` e `Program.cs`
`ChatSessionWorker` é um `BackgroundService` fino: só delega `ExecuteAsync` para `ChatSession.RunAsync`. `Ctrl+C` chama `IHostApplicationLifetime.StopApplication()`, garantindo desligamento gracioso com `CancellationToken`.

## Fluxo de uma mensagem

```
IChatView.ReadInputAsync
        │
        ▼
ChatSession ──► IChatView.BeginThinking (spinner)
        │
        ▼
ProcessMessageUseCase ──► AgentService
                           ├─ ToolRouter (se casar)
                           └─ ILLMClient (senão)
        │
        ▼
IChatView.ShowReply / ShowError
```

O loop é **sequencial** — nunca há duas partes desenhando no terminal ao mesmo tempo. Isso evita a corrida de shutdown em que `sair` cancelava uma resposta em andamento.

Logs usam `ILogger<T>` e são enviados para **stderr**; a interface do chat usa **stdout**.

## Guardrails

Como não há projetos separados para impor limites, `tests/Pi.Tests/StructureTests.cs` verifica:

1. `Abstracts/` não referencia implementações concretas (`Pi.Ollama`, `Pi.Tools`, `Pi.UI`, `Pi.Workers`) nem `Microsoft.Extensions`;
2. `Conversation/` permanece isolado dos demais módulos;
3. a API pública dos tipos de `Pi.Abstracts` não expõe tipos concretos (reflection).

Os testes de `ChatSession` usam um `IChatView` falso (lógica sem terminal), e os de `SpectreChatView` renderizam em `StringWriter` (apresentação sem IA).

## Build e saída

`Directory.Build.props` define `ArtifactsPath=artifacts`. Consequentemente:

- não existem pastas `bin/` ou `obj/` dentro de `src/` ou `tests/`;
- toda a saída fica em `artifacts/{bin,obj}/<projeto>/<config>/<framework>`;
- `artifacts/` está no `.gitignore`.

A solution `Pi.slnx` contém apenas dois projetos: `Pi.CLI` (aplicação) e `Pi.Tests` (testes).

## Decisões

- **Estrutura plana:** um assembly, módulos por pasta/namespace — navegação curta e um único `bin`.
- **Guardrail por testes:** substitui a fiscalização que os project references davam antes.
- **Apresentação × lógica:** `UI/` só desenha, `Services/ChatSession` só decide. O contrato `IChatView` mantém os dois isolados e testáveis.
- **DI e Options Pattern** eliminam `new` direto e permitem substituir implementações nos testes.
- **Ferramentas** são descobertas via `IEnumerable<ITool>`; adicionar uma ferramenta não exige alterar o roteador.
- **Histórico com papéis** (`User`/`Assistant`) substitui a concatenação de strings.
- **Loop sequencial** substitui o barramento: menos indirection e nenhuma disputa pela tela.
