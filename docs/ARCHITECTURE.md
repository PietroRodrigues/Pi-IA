# Arquitetura

O projeto segue **Clean Architecture** adaptada a um monólito modular. A regra de dependência aponta para dentro: domínio não conhece ninguém; aplicação conhece o domínio; infraestrutura conhece domínio e aplicação; o host conhece todos.

```
Pi.Domain  ←  Pi.Application  ←  Pi.Infrastructure
                   ↑                    ↑
                 Pi.CLI (host / composition root)
```

## Camadas

### Pi.Domain
Contém os conceitos do negócio da conversa: `Conversation`, `ChatMessage`, `MessageRole` e `MessageSource`. Não referencia frameworks, bancos, HTTP ou dispositivos. O histórico é limitado pelo próprio agregado.

### Pi.Application
Define as **portas** (interfaces) que a infraestrutura implementa:

| Porta | Finalidade |
|---|---|
| `ILLMClient` | enviar prompt ao modelo de linguagem |
| `IAudioCapture` | capturar áudio do microfone |
| `ISpeechToText` | transcrever áudio em texto |
| `IMessageBus` | transportar mensagens entre workers |
| `ITool` | representar uma ferramenta invocável |

Também contém `AgentService` (montagem do prompt + histórico + ferramentas), `ToolRouter`, os casos de uso `ProcessMessageUseCase` e `CaptureVoiceUseCase`, e as classes de opções.

### Pi.Infrastructure
Implementações concretas: `OllamaClient` (HTTP), `NaudioAudioCapture` e `WhisperSpeechToText` (áudio), `ChannelMessageBus` (`System.Threading.Channels`), `SearchFilesTool` e `OpenAppTool`, além do registro de DI.

### Pi.CLI
Host genérico .NET. Responsabilidades:

- ler `appsettings.json` e configurar logging;
- registrar DI (`AddApplication`, `AddInfrastructure`);
- hospedar os `BackgroundService`:
  - `ConsoleInputWorker` — lê texto do console;
  - `VoiceInputWorker` — captura e transcreve voz;
  - `MessageProcessingWorker` — consome a fila e imprime a resposta.

O `Ctrl+C` chama `IHostApplicationLifetime.StopApplication()`, garantindo desligamento gracioso com `CancellationToken`.

## Fluxo de uma mensagem

```
Console / Microfone
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

## Decisões

- **DI e Options Pattern** eliminam `new` direto e permitem substituir implementações nos testes.
- **Ferramentas** são descobertas via `IEnumerable<ITool>`; adicionar uma ferramenta não exige alterar o roteador.
- **Histórico com papéis** (`User`/`Assistant`) substitui a concatenação de strings.
- **Bus limitado** evita crescimento sem controle da fila de mensagens.
- **Nenhuma dependência de infraestrutura no domínio** — regra verificável pelos project references.
