# Backlog — Pi-IA

> Documento vivo. As etapas e os itens podem **crescer, diminuir ou ser reordenados** conforme o projeto evolui.
> Atualizado em: 2026-11-17

---

## 1. Visão

Transformar o Pi-IA de um assistente local de texto em uma **plataforma CLI para conectar modelos de IA modernos**, com:

- **Foco principal:** executar modelos **locais** via **Ollama** (e runtimes locais compatíveis).
- **Extensão:** consumir IAs em **nuvem** por meio de **APIs OpenAI-compatible**.
- **Operação:** comandos para escolher **modelos**, escolher **agentes** e **conectar provedores externos**.

O projeto atual já entrega a base: monólito de um assembly com estrutura plana (módulos por pasta/namespace), DI, Options, fila em memória, `ILLMClient` abstraído, cliente Ollama, ferramentas por prefixo, 18 testes e CI. O backlog abaixo parte dessa fundação.

### Interpretações assumidas

1. "Conexão baseada na OpenAI" = adotar o **formato de API OpenAI-compatible** (`/v1/chat/completions`, `base_url`, `api_key`, `model`) como padrão de integração. Isso permite conversar com OpenAI, Groq, Together, DeepSeek, vLLM, LM Studio, llama.cpp server etc. com um único cliente.
2. "Comandos" = comandos digitados no próprio CLI no formato `/comando` (confirmado), não argumentos de linha — mas os dois podem coexistir.
3. "Agentes" = perfis reutilizáveis que combinam **modelo + system prompt + ferramentas + parâmetros**.

---

## 2. Princípios

- **Local-first:** Ollama é o provedor padrão; nuvem é opcional e explícita.
- **Agnóstico de provedor:** a aplicação depende só de `ILLMClient` / portas; providers entram pela infraestrutura.
- **OpenAI-compatible como ponte:** um cliente genérico reaproveitado por vários serviços.
- **Config fora do código:** preferências do usuário em arquivo de perfil, nunca credenciais versionadas.
- **Incremental:** cada etapa entregável, testável e reversível.

---

## 3. Formato

- **Prioridade:** `P0` (blocker) · `P1` (alto) · `P2` (médio) · `P3` (baixo)
- **Status:** `[ ]` pendente · `[~]` em andamento · `[x]` concluído · `[-]` descartado
- **Dependência:** itens podem referenciar etapas anteriores.

### Mapa de etapas

| # | Etapa | Prioridade | Status | Depende de |
|---|---|---|---|---|
| 0 | Fundação (já entregue) | — | [x] | — |
| 1 | Sistema de comandos do CLI | P0 | [ ] | 0 |
| 2 | Registry de modelos + preferências | P0 | [ ] | 1 |
| 3 | Provedores locais (Ollama + compatíveis) | P0 | [ ] | 2 |
| 4 | Provedores de nuvem (OpenAI-compatible) | P1 | [ ] | 2, 3 |
| 5 | Agentes (perfis reutilizáveis) | P1 | [ ] | 2, 3 |
| 6 | Ferramentas + function calling | P1 | [ ] | 5 |
| 7 | Experiência de conversa | P2 | [ ] | 3 |
| 8 | Robustez e observabilidade | P2 | [ ] | 3, 4 |
| 9 | Distribuição e docs finais | P3 | [ ] | 8 |

---

## 4. Etapas detalhadas

### Etapa 0 — Fundação `[x]`

- [x] Estrutura plana em um único assembly (`src/`): módulos `Abstracts`, `Conversation`, `Models`, `Options`, `Services`, `UseCases`, `Ollama`, `Messaging`, `Tools`, `Workers`; saída centralizada em `artifacts/`
- [x] DI, Options Pattern com validação na subida, `IHttpClientFactory`
- [x] `IMessageBus` com `System.Threading.Channels`, workers com `CancellationToken`
- [x] `ILLMClient` + `OllamaClient` (texto, `/api/generate`)
- [x] `ITool`, `ToolRouter`, `SearchFilesTool`, `OpenAppTool` (placeholder)
- [x] Histórico em memória com limite
- [x] 15 testes unitários + 3 testes de estrutura, CI, documentação
- [x] Poda completa de voz/áudio

---

### Etapa 1 — Sistema de comandos do CLI `P0`

**Objetivo:** separar comandos de mensagens comuns, com descoberta e ajuda.

- [ ] Parser de comandos com sintaxe `/comando` (`:` fica como alternativa opcional)
- [ ] `/help` — lista comandos e atalhos
- [ ] `/exit` — encerrar (mantendo `sair` / `exit` / Ctrl+C)
- [ ] `/clear` — limpar histórico da conversa
- [ ] `/status` — provedor, modelo e agente ativos
- [ ] Tratamento de comando desconhecido com sugestão
- [ ] (Opcional) argumentos de linha: `pi --model <id> --agent <id>` aplicados na subida

**Critério de aceite:** digitar `/help` lista os comandos; comandos não são enviados ao LLM.

---

### Etapa 2 — Registry de modelos + preferências `P0`

**Objetivo:** catálogo único de modelos/provedores e persistência da escolha do usuário.

- [ ] `ModelRegistry` (id, provedor, nome, capacidades, parâmetros padrão)
- [ ] `/model list` — modelos conhecidos e disponíveis
- [ ] `/model use <id>` — trocar modelo em tempo de execução
- [ ] `/model info <id>` — detalhes do modelo
- [ ] Arquivo de preferências do usuário (ex.: `%USERPROFILE%\.pi-ia\config.json`)
- [ ] Carregar preferências na subida; salvar ao trocar modelo/agente/provedor
- [ ] (P2) `/config show` e `/config set <chave> <valor>`

**Critério de aceite:** trocar de modelo com `/model use` persiste após reiniciar o CLI.

---

### Etapa 3 — Provedores locais `P0`

**Objetivo:** consolidar Ollama e abrir espaço para outros runtimes locais.

- [ ] Refatorar `ILLMClient` → contrato de provider com `chat` + `list models`
- [ ] `OllamaClient`: usar `/api/chat` (papéis) em vez de `/api/generate`, mantendo compatibilidade
- [ ] `/models refresh` — consultar modelos instalados no Ollama
- [x] Detecção de Ollama offline com mensagem clara
- [x] Auto-start de `ollama serve` quando o servidor local não responde (config `Ollama:AutoStart`, `StartupTimeoutSeconds`, `ExecutablePath`, `StopOnExit`)
- [ ] Provider `OpenAICompatibleLocal` (LM Studio, llama.cpp server, vLLM local)
- [ ] Seleção de provider local na config (`Local__Provider = Ollama | OpenAICompatible`)

**Critério de aceite:** o CLI lista e alterna entre modelos Ollama sem recompilar.

---

### Etapa 4 — Provedores de nuvem (OpenAI-compatible) `P1`

**Objetivo:** conectar serviços externos com o mesmo contrato.

- [ ] Cliente `OpenAICompatibleClient` (`base_url`, `api_key`, `model`, headers extras)
- [ ] `/connect <nome> --url <base_url> --key <api_key> --model <modelo>`
- [ ] `/connect list` e `/connect remove <nome>`
- [ ] Armazenamento seguro de chaves (DPAPI no Windows / env var; **nunca** em `appsettings.json`)
- [ ] Prefixos conhecidos: OpenAI, Groq, Together, DeepSeek, OpenRouter (config por `base_url`)
- [ ] `/model use <provedor>:<modelo>` para alternar entre local e nuvem
- [ ] (P2) Fallback automático para outro provedor em caso de falha

**Critério de aceite:** cadastrar um provedor externo via `/connect` e conversar com ele; chave não aparece em logs nem no git.

---

### Etapa 5 — Agentes (perfis reutilizáveis) `P1`

**Objetivo:** pacotes configuráveis de comportamento.

- [ ] Entidade `Agent` (nome, system prompt, modelo, ferramentas, temperatura)
- [ ] `/agent list`, `/agent use <id>`, `/agent info <id>`
- [ ] `/agent create <id>` (interativo) e `/agent edit <id>`
- [ ] Agentes embutidos: `geral`, `dev`, `revisor`
- [ ] Carregar agentes customizados de `agents/*.json` no perfil do usuário
- [ ] `AgentService` passa a receber o agente ativo (substitui `AssistantOptions.SystemPrompt` fixo)

**Critério de aceite:** alternar entre dois agentes muda prompt, modelo e ferramentas disponíveis na mesma sessão.

---

### Etapa 6 — Ferramentas + function calling `P1`

**Objetivo:** evoluir do prefixo para um mecanismo real de ferramentas.

- [ ] Implementar `OpenAppTool` (abrir app por nome/caminho, com allowlist)
- [ ] Registro dinâmico de ferramentas por agente
- [ ] Definições de tools no formato OpenAI (`tools` / `function`)
- [ ] `ToolRouter` com duas estratégias: `Prefix` (atual) e `FunctionCalling` (modelo decide)
- [ ] Execução segura: confirmação para ações sensíveis, timeout, sandbox de pastas
- [ ] (P3) Ferramentas de leitura de arquivo, HTTP e execução de comandos (com aprovação)

**Critério de aceite:** um agente com function calling consegue chamar `SearchFilesTool` sem o usuário digitar `procure`.

---

### Etapa 7 — Experiência de conversa `P2`

- [ ] Streaming de tokens (SSE) com impressão incremental
- [ ] Histórico persistente entre sessões (SQLite ou JSON no perfil)
- [ ] `/history` e `/resume` (retomar conversa salva)
- [ ] Entrada multilinha e edição de linha (setas/histórico de comandos)
- [ ] Indicador de "pensando..." e tempo de resposta

---

### Etapa 8 — Robustez e observabilidade `P2`

- [ ] Retry com backoff para falhas transitórias de rede/429
- [ ] Tratamento uniforme de erros de API (401, 404, timeout, modelo inexistente)
- [ ] Testes de integração com servidor HTTP fake (WireMock/MockHttp) e Ollama real opcional
- [ ] Logs estruturados e níveis configuráveis por provedor
- [ ] Telemetria local opcional (contadores de uso por modelo), sem envio externo
- [ ] Matriz de compatibilidade: quais recursos cada provedor suporta

---

### Etapa 9 — Distribuição e documentação `P3`

- [ ] Publicar como `dotnet tool` (`dotnet tool install -g Pi.CLI`)
- [ ] Binários por plataforma (win-x64, linux-x64, osx-arm64)
- [ ] README com quickstart local e nuvem
- [ ] Docs de provedores, agentes, comandos e solução de problemas
- [ ] CHANGELOG e versionamento semântico

---

## 5. Decisões

### Tomadas

- **D1 — Sintaxe dos comandos:** `/comando` (confirmado em 2026-11-17). `:` permanece apenas como alternativa opcional.

### Em aberto

| # | Decisão | Opções | Sugestão |
|---|---|---|---|
| D2 | Onde guardar config do usuário | `%USERPROFILE%\.pi-ia\` · junto ao binário · portable | `%USERPROFILE%\.pi-ia\` |
| D3 | Credenciais de API | DPAPI · env var · arquivo criptografado | DPAPI + env var como fallback |
| D4 | Prioridade entre nuvem e agentes | Etapa 4 antes da 5 · 5 antes da 4 | Manter 4 → 5 (provedor antes de agente) |
| D5 | Streaming | Fazer cedo (P1) · deixar para P2 | P2, mas revisar se a latência incomodar |
| D6 | Modelo de dados do histórico | JSON · SQLite | SQLite na Etapa 7 |
| D7 | Nome do pacote/publicação | `Pi.CLI` · outro | Manter `Pi.CLI` |

---

## 6. Riscos

- **Credenciais:** risco alto se chave parar em log/config versionado → endereço na Etapa 4.
- **Fragmentação de API:** cada provider tem peculiaridades → camada de compatibilidade OpenAI + matriz na Etapa 8.
- **Escopo:** "conectar tudo" pode explodir → priorizar Ollama e 2-3 provedores nuvem primeiro.
- **Function calling:** nem todo modelo local suporta → manter estratégia `Prefix` como fallback.

---

## 7. Como evoluir este backlog

- Mover itens entre etapas, trocar prioridade ou marcar `[-]` quando descartado.
- Ao concluir uma etapa, registrar a data e o commit em "Etapas detalhadas".
- Novas ideias entram primeiro como item `P3` e sobem de prioridade quando validadas.
