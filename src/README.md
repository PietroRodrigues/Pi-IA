# Código-fonte do Pi-IA

Projeto C# de um único assembly. A interface é a TUI em `tui/`: o `Pi.CLI`
roda **somente** com `--bridge stdio` e conversa com ela por NDJSON via
stdin/stdout. Sem essa flag, imprime o uso no stderr e encerra com código 1.

```text
src/
├── Core/          decisões (agente, conversa e contratos)
├── Tools/         ferramentas locais e roteamento
├── Connections/   provedores externos (Ollama hoje, MCP no futuro)
├── Bridge/        ponte NDJSON para a TUI
├── Options/       configurações tipadas
├── DependencyInjection.cs   composition root
└── Program.cs               ponto de entrada
```

## Core

Não conhece Ollama, interface nem MCP. Contém o agente (`AgentService`), o
histórico (`Conversation`), as mensagens (`ChatMessage`) e os contratos em
`Core/Contracts/`: `ILLMClient`, `IStreamingLLMClient`, `ITool` e
`LLMStreamChunk`. Eventos de progresso usam `IAgentEventSink`.

## Ferramentas (Tools)

Ações locais simples — buscar arquivos em pastas autorizadas e abrir
aplicativos. `ToolRouter` escolhe a ferramenta pelo prefixo da mensagem e
informa `tool_call`/`tool_result` quando recebe um sink.

## Conexões (Connections)

- `Ollama/` — cliente HTTP do modelo local (`OllamaClient`), parser de
  respostas, conversão de chunks e bootstrap do servidor (`ollama serve`).
- `Mcp/` — `IMcpConnection` é apenas o ponto de extensão para futuras conexões
  MCP, locais ou remotas. Uma implementação devolverá `ITool` usando os
  contratos do Core; hoje não há pacote, transporte nem implementação.

## Ponte (Bridge)

Pasta única, sem `Frontend/` intermediário. Lê `prompt`, `cancel` e `clear` do
stdin, resolve `AgentService` por pedido e escreve só NDJSON no stdout. Cada
pedido tem `requestId` e cancelamento próprios. Logs vão para stderr/arquivos.

## Regras rápidas

- `Core` não referencia `Bridge` nem `Connections`.
- `Bridge` referencia somente `Pi.Core.Chat` (`AgentService` e eventos).
- `ChatMessage` e `Conversation` usam apenas a BCL.
- Os tipos da antiga CLI textual (`ConsoleChatView`, `ChatSession`, etc.) e o
  namespace `Pi.Frontend` não devem reaparecer.
- `DependencyInjection.cs` é o único lugar que liga todas as camadas.

Detalhes em [docs/ARCHITECTURE.md](../docs/ARCHITECTURE.md).
