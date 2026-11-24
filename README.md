# Pi-IA

Assistente de IA **local** para o terminal. Você digita uma mensagem e o Pi conversa com um modelo rodando no seu computador via Ollama — sem serviços em nuvem, chave de API ou telemetria.

O Pi também cuida do servidor: se o Ollama não estiver aberto, ele inicia `ollama serve` em segundo plano e espera ficar pronto antes de aceitar mensagens.

## Estrutura

```text
Pi-IA/
├── Pi.sln                       solução .NET
├── Directory.Build.props        TFM, nullable e artifacts centralizados
├── Directory.Packages.props     versões centralizadas de pacotes
├── .env.example                 configuração (copie para .env)
├── docs/protocol-v1.md          contrato NDJSON entre TUI e Host
├── artifacts/debug/             logs locais (pi-host.log, pi-tui.log, ndjson-trace.log)
├── src/
│   ├── Pi.Contracts/            envelope, comandos, eventos e snapshot
│   ├── Pi.Core/                 abstrações, orquestrador, budget, tools e sandbox
│   ├── Pi.Infrastructure/       Ollama, ferramentas e persistência JSON
│   ├── Pi.Host/                 composition root, ponte NDJSON e supervisor da TUI
│   └── Pi.Tui/                  Bun + OpenTUI (protocol/state/ui/telemetry + tests)
└── tests/
    ├── Pi.Core.Tests/
    ├── Pi.Infrastructure.Tests/
    └── Pi.Bridge.Tests/
```

As dependências apontam sempre para a camada interna:

```text
Pi.Tui ──NDJSON──> Pi.Host ──> Pi.Infrastructure ──> Pi.Core ──> Pi.Contracts
```

`Pi.Core` não conhece HTTP, disco, host ou terminal. `Pi.Contracts` é o único vocabulário compartilhado. O contrato entre TUI e Host é exclusivamente o [protocolo v1](docs/protocol-v1.md).

## Como funciona

```text
dotnet run --project src/Pi.Host    → supervisor inicia bun run src/index.ts
src/Pi.Tui/index.ts ──spawn──► Pi.Host.dll --bridge stdio ──NDJSON──► src/Pi.Tui
```

- A TUI é dona do terminal e inicia o backend já compilado em `artifacts/bin/Pi.Host/debug/Pi.Host.dll`.
- No modo `--bridge stdio`, o `Pi.Host` usa `Pi.Core` + `Pi.Infrastructure` em processo.
- O `stdout` do C# fica reservado ao NDJSON; logs vão para `artifacts/debug/pi-host.log`.
- Comandos usam `prompt_submit`, `interrupt_turn`, `tool_decision`, `session_switch` e `session_clear`.
- Eventos usam `session_snapshot`, `stream_delta`, `tool_request`, `tool_result`, `context_metrics` e `error_event`.

## Requisitos

| Requisito | Observação |
|---|---|
| [.NET SDK 10.0](https://dotnet.microsoft.com/download) | Versão fixada em `global.json` |
| [Ollama](https://ollama.com/download) | Precisa estar instalado; o Pi não instala o Ollama |
| [Bun 1.4.5+](https://bun.sh/) | Necessário para a TUI |

## Primeira execução

```powershell
# 1. Instale o modelo padrão no Ollama
ollama pull qwen3.8-2b-local

# 2. (Opcional) copie a configuração e ajuste o que precisar
Copy-Item .env.example .env

# 3. Compile e inicie tudo
dotnet build Pi.sln --configuration Debug
dotnet run --project src/Pi.Host
```

`dotnet run --project src/Pi.Host` compila o backend via `ProjectReference`, inicia a TUI e encerra a árvore de processos no `Ctrl+C` com código `0`. Para validar sem abrir o terminal: `PI_SKIP_TUI=1 dotnet run --project src/Pi.Host`.

## Controles

| Tecla | Ação |
|---|---|
| `Ctrl+P` | Foca o campo de texto |
| `Ctrl+L` | Limpa o contexto da conversa |
| `Esc` ou `Ctrl+C` durante a geração | Cancela o turno atual |
| `Ctrl+C` com a TUI ociosa | Encerra TUI, backend e processos filhos |
| `Y` / `N` / `Esc` | Aprova ou nega uma ferramenta sensível |
| Mouse na área de mensagens | Rolagem; `Jump to latest ↓` reaparece no fim |

## Ferramentas e segurança

- `read_file`: leitura paginada com limite de bytes.
- `search_code`: busca com `.gitignore`, timeout e limite de resultados.
- `edit_file`: exige aprovação, troca atômica e backup em caso de falha.
- `execute_shell`: desativada por padrão; habilite com `PI_ALLOW_SHELL=1`.
- `WorkspaceSandboxPolicy`: bloqueia `..`, caminhos absolutos fora da raiz e comandos destrutivos.
- Snapshots de sessão são gravados em `.pi-ia/sessions/` com arquivo temporário + `Move(overwrite)`.
- Tool calling depende de modelo com suporte a functions: o padrão `qwen3.8-2b-local` responde em texto; para testar `read_file`, use `PI_MODEL=qwen3-6b-iq2-local`.

## Configuração

Variáveis de ambiente (ou `.env` na raiz) têm prioridade sobre `src/Pi.Host/appsettings.json`:

| Variável | Padrão | Descrição |
|---|---|---|
| `OLLAMA_BASE_URL` | `http://127.0.0.1:11434` | Endereço do Ollama |
| `PI_MODEL` | `qwen3.8-2b-local` | Modelo usado |
| `PI_LOG_LEVEL` | `Information` | Nível de log do host |
| `PI_CONTEXT_WINDOW_TOKENS` | `32768` | Janela de contexto estimada |
| `PI_SESSION_DIR` | `.pi-ia/sessions` | Diretório dos snapshots |
| `PI_ALLOW_SHELL` | `0` | Habilita `execute_shell` |
| `PI_DEBUG` | `0` | Logs de diagnóstico em `artifacts/debug/` |

## Debug

No VS Code, selecione **Pi — Debug (tudo)** e pressione `F5`. A task `debug-prep` compila a solução e instala as dependências da TUI. A configuração **Pi — Debug (C# + TUI)** adiciona o attach do Bun na porta 9229; para breakpoint no backend use **Pi.Host — Anexar ao backend**.

Logs ficam em `artifacts/debug/`:

- `pi-host.log` — decisões e erros do backend;
- `pi-tui.log` — eventos e diagnósticos da TUI;
- `ndjson-trace.log` — todas as linhas do protocolo.

## Verificação

```powershell
dotnet build Pi.sln
dotnet test Pi.sln

cd src/Pi.Tui
bun install --frozen-lockfile
bun run check:arch
bun run typecheck
bun run test
bun run build
```

## Documentação

- [Protocolo NDJSON v1](docs/protocol-v1.md)
- [Configuração de exemplo](.env.example)
