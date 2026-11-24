# Protocolo NDJSON v1

Contrato exclusivo entre `src/Pi.Tui` e `src/Pi.Host --bridge stdio`.

- **Transporte:** uma mensagem JSON por linha (`\n`), UTF-8.
- **stdout do host:** somente NDJSON. Logs vão para `artifacts/debug/pi-host.log`.
- **stderr do host:** diagnósticos de emergência; a TUI não o desenha na tela.
- **Linhas corrompidas:** descartadas; o host responde com `error_event` (`invalid_message`).
- **Tamanho máximo por linha:** 1 MiB no host.
- **Ordem:** eventos de um mesmo `correlationId` chegam em ordem; turnos diferentes podem intercalar, mas o agente executa um por vez.

## Envelope

```json
{
  "v": 1,
  "id": "0e7f1f...",
  "correlationId": "b1a2c3...",
  "timestamp": "2026-11-24T12:34:56.789Z",
  "type": "stream_delta",
  "payload": { "text": "Olá", "final": false }
}
```

| Campo | Tipo | Descrição |
|---|---|---|
| `v` | `int` | Versão do protocolo. Atual: `1`. |
| `id` | `string` | Identificador único da mensagem. |
| `correlationId` | `string` | Identificador do turno/ação. Vazio para eventos de sessão. |
| `timestamp` | `string` | ISO-8601 em UTC. |
| `type` | `string` | Nome do comando ou evento. |
| `payload` | `object` | Objeto tipado conforme `type`. |

## Comandos (TUI → Host)

| `type` | Payload | Efeito |
|---|---|---|
| `prompt_submit` | `{ text, sessionId? }` | Inicia um turno. `correlationId` identifica a resposta. |
| `interrupt_turn` | `{ reason? }` | Cancela o turno ativo. Não encerra o host. |
| `tool_decision` | `{ toolRequestId, approved, reason? }` | Responde a um `tool_request` pendente. Sem resposta em 30 s = negação. |
| `session_switch` | `{ sessionId }` | Carrega/cria sessão e publica `session_snapshot`. |
| `session_clear` | `{ clearHistory? }` | Limpa o histórico e publica `session_snapshot`. |

## Eventos (Host → TUI)

| `type` | Payload | Quando |
|---|---|---|
| `session_snapshot` | `{ session: SessionSnapshot }` | Ao iniciar, trocar/limpar sessão e ao fim de cada turno. |
| `stream_delta` | `{ text, final? }` | Token a token; `final: true` fecha o turno. |
| `tool_request` | `{ toolRequestId, name, input, risk, requiresDecision }` | Antes de executar ferramenta. |
| `tool_result` | `{ toolRequestId, name, ok, text }` | Depois da execução ou da negação. |
| `context_metrics` | `{ used, limit, percent }` | Antes da chamada ao modelo e a cada chunk com contagem. |
| `error_event` | `{ code, message }` | Falha de parsing, comando, modelo, sessão ou ferramenta. |

### `SessionSnapshot`

```json
{
  "id": "default",
  "model": "qwen3.8-2b-local",
  "agent": "geral",
  "cwd": "C:/projetos/Pi-IA",
  "branch": "main",
  "messages": [{ "role": "user", "text": "oi", "timestamp": "..." }],
  "context": { "used": 10, "limit": 32768, "percent": 0.03 },
  "updatedAt": "2026-11-24T12:34:56.789Z"
}
```

## Códigos de erro

| Código | Significado |
|---|---|
| `invalid_message` | JSON inválido, envelope incompleto ou linha grande demais. |
| `unsupported_version` | `v` diferente de `1`. |
| `empty_prompt` | `text` vazio. |
| `unknown_command` | `type` não reconhecido. |
| `tool_denied` | Política de sandbox negou a ferramenta. |
| `tool_timeout` | Ferramenta excedeu o tempo limite. |
| `llm_failure` | Falha ao chamar o Ollama. |
| `session_failure` | Falha ao carregar/salvar snapshot. |
| `internal` | Erro não previsto. |

## Cancelamento e aprovação

1. `Esc` ou `Ctrl+C` durante a geração envia `interrupt_turn` com o `correlationId` ativo.
2. O host cancela o `CancellationToken` do turno, emite `stream_delta(final: true)` e publica o snapshot.
3. Deltas posteriores ao cancelamento são ignorados pela TUI; o `final` ainda fecha a bolha e marca `[cancelado]`.
4. Ferramentas `Write`/`Shell` emitem `tool_request` com `requiresDecision: true`. A TUI responde `Y`/`N`; timeout ou desconexão = negação.
5. `Ctrl+C` sem turno ativo encerra a TUI, o backend e a árvore de processos com saída `0`.
