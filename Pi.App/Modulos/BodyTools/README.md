# BodyTools

Backend .NET do Pi-IA. É um único assembly, organizado em MVC, que roda **somente** com `--bridge stdio` e conversa com a `interfaceTui` por NDJSON via stdin/stdout. Sem essa flag, imprime o uso no stderr e encerra com código 1.

```text
BodyTools/
├── Model/                  decisões e infraestrutura do domínio
│   ├── Chat/               agente, conversa, mensagens e eventos
│   ├── Contracts/          portas do modelo e das ferramentas
│   ├── Tools/              ferramentas locais e roteamento
│   └── Connections/        Ollama (hoje) e MCP (ponto de extensão)
├── Controller/             leitura do stdin, requestId, cancel/clear e despacho
├── View/                   DTOs NDJSON, escritor e eventos tipados
├── Options/                configurações tipadas
├── DependencyInjection.cs  composition root
└── Program.cs              ponto de entrada
```

## MVC

- **Model** — `AgentService` monta o prompt, consulta ferramentas, mantém o histórico e publica eventos em `IAgentEventSink`. `Conversation` limita o histórico e `ChatMessage` representa cada mensagem. `Connections/Ollama` implementa a porta `ILLMClient`; `Tools` executa ações locais.
- **Controller** — `BridgeController` lê `prompt`, `cancel` e `clear`, cria um `CancellationToken` por pedido e chama o Model. É o único leitor de stdin.
- **View** — `BridgeView` serializa os eventos em linhas NDJSON com flush imediato; `BridgeEventSink` adapta os eventos do Model para a View. É o único produtor de stdout.

Namespaces: `Pi.Model.Chat`, `Pi.Model.Contracts`, `Pi.Model.Tools`, `Pi.Model.Connections.*`, `Pi.Controller`, `Pi.View` e `Pi.Options`.

## Regras rápidas

- `Model` não referencia `Controller` nem `View`.
- `View` referencia somente `Pi.Model.Chat` (contrato de eventos).
- Somente `Controller` lê `Console.In`.
- `ChatMessage` e `Conversation` usam apenas a BCL.
- Tipos da antiga CLI textual e do antigo `Pi.CLI` não devem reaparecer.
- `DependencyInjection.cs` é o único lugar que liga todas as camadas.

Os guardrails vivem em `../tests/BodyTools.Tests/StructureTests.cs`. Detalhes em [../docs/ARCHITECTURE.md](../docs/ARCHITECTURE.md).
