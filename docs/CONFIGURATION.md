# Configuração

Arquivo principal: `src/Pi.CLI/appsettings.json`. Valores podem ser sobrescritos por variáveis de ambiente, por exemplo `Ollama__Model=llama3.2`.

## Seções

### `Ollama`

| Chave | Padrão | Descrição |
|---|---|---|
| `BaseUrl` | `http://localhost:11434` | Endereço do servidor Ollama |
| `Model` | `qwen2.5:3b` | Modelo usado em `/api/generate` |
| `TimeoutSeconds` | `60` | Timeout da requisição HTTP |

### `Assistant`

| Chave | Padrão | Descrição |
|---|---|---|
| `MaxHistoryMessages` | `20` | Máximo de mensagens mantidas no histórico |
| `ExitKeywords` | `["sair", "exit"]` | Palavras que encerram a aplicação |
| `SystemPrompt` | ver arquivo | Prompt base do assistente |

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

`Console.LogToStandardErrorThreshold` está em `Trace` para que **todos os logs** vão para stderr, preservando o stdout para as respostas (`Pi: ...`).

## Validação

As opções são validadas na inicialização (`ValidateOnStart`). Configurações inválidas impedem o host de subir, com mensagem indicando a seção afetada.
