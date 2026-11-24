# Configuração

Arquivo principal: `Pi.App/Modulos/BodyTools/appsettings.json`. Valores podem ser sobrescritos por variáveis de ambiente, por exemplo `Ollama__Model=llama3.2`.

## Seções

### `Ollama`

| Chave | Padrão | Descrição |
|---|---|---|
| `BaseUrl` | `http://127.0.0.1:11434` | Endereço do servidor Ollama (IPv4 evita a resolução IPv6 de `localhost` no Windows) |
| `Model` | `qwen3.8-2b-local` | Modelo usado em `/api/generate` |
| `TimeoutSeconds` | `60` | Timeout da requisição HTTP |
| `AutoStart` | `true` | Inicia `ollama serve` automaticamente se o servidor local não responder |
| `StartupTimeoutSeconds` | `30` | Tempo máximo (1–300 s) esperando o servidor responder |
| `ExecutablePath` | `""` | Caminho explícito do executável; vazio = PATH + locais de instalação conhecidos |
| `StopOnExit` | `false` | Encerra o processo iniciado pelo Pi ao sair |

O auto-start só atua quando `BaseUrl` aponta para `localhost`, `127.0.0.1` ou `::1`. Com uma URL remota ele é ignorado. Se o executável não for encontrado ou o servidor não responder, o Pi **avisa e continua** — os comandos seguem disponíveis e a primeira mensagem recebe um erro explicativo. O Pi não instala o Ollama nem baixa modelos: use `Pi.App/scripts/setup-ollama.ps1` para isso.

### `Assistant`

| Chave | Padrão | Descrição |
|---|---|---|
| `AgentName` | `geral` | Rótulo do agente exibido no chat (informativo; agentes reais virão depois) |
| `MaxHistoryMessages` | `20` | Máximo de mensagens mantidas no histórico |
| `ContextWindowTokens` | `32768` | Limite usado para calcular a porcentagem exibida na TUI |
| `ExitKeywords` | `["sair", "exit"]` | Palavras que encerram a aplicação |
| `SystemPrompt` | ver arquivo | Prompt base do assistente |

O modelo em uso (`Ollama:Model`) e o agente (`Assistant:AgentName`) vêm no evento `ready` exibido pela TUI. O status muda conforme os eventos `thought`, `tool_call` e `tool_result` chegam.

### `Tools`

| Chave | Padrão | Descrição |
|---|---|---|
| `AllowedFolders` | `[]` | Pastas que a busca pode varrer. **Vazio por padrão** — configure explicitamente |
| `MaxResults` | `20` | Limite de arquivos retornados |
| `SearchTimeoutSeconds` | `10` | Timeout da busca |

Exemplo:

```json
"Tools": {
  "AllowedFolders": ["E:\\MeusProjetosRepositorio", "D:\\Gearbox"],
  "MaxResults": 20,
  "SearchTimeoutSeconds": 10
}
```

### `Logging`

`Console.LogToStandardErrorThreshold` está em `Trace` para que **todos os logs** vão para stderr. Como a TUI é a única interface, o `BodyTools` roda sempre com `--bridge stdio` e seu stdout fica reservado ao protocolo NDJSON lido pela TUI. Sem essa flag, o programa exibe o uso no stderr e encerra com código não-zero.

## Validação

As opções são validadas na inicialização (`ValidateOnStart`). Configurações inválidas impedem o host de subir, com mensagem indicando a seção afetada.
