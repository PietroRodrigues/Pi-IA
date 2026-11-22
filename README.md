# Pi-IA

Assistente de IA **local** para o terminal. Você digita uma mensagem e o Pi conversa com um modelo rodando no seu computador via Ollama — sem serviços em nuvem, chave de API ou telemetria.

O Pi também cuida do servidor: se o Ollama não estiver aberto, ele inicia `ollama serve` em segundo plano e espera ficar pronto antes de aceitar mensagens.

## O que é

O Pi é uma conversa por texto com memória da sessão (padrão: últimas 20 mensagens). Além de responder, ele pode usar ferramentas locais simples: buscar arquivos em pastas autorizadas e abrir aplicativos.

A interface ativa é a **TUI em TypeScript/Bun**, em `tui/`: coluna única, respostas em streaming e eventos de ferramentas aparecendo na conversa. O backend em C#/.NET roda como processo filho e só se comunica por NDJSON.

Tudo funciona sem internet depois que o modelo foi baixado. Estão fora de escopo: voz, áudio, modelos embarcados e coleta de dados.

## Como funciona

```
Você → TUI (Bun) → Pi.CLI (.NET, --bridge stdio) → Ollama → Pi → TUI
                          │                 ▲
                          └── ferramentas ──┘
```

1. Você envia uma mensagem no campo de texto da TUI.
2. O Pi monta o prompt com o histórico da conversa e informa ao modelo quais ferramentas estão disponíveis.
3. O Ollama gera a resposta aos poucos, em streaming.
4. O Pi publica eventos `token`, `thought`, `tool_call`, `tool_result` e `context_stats`; a TUI monta a transcrição a partir deles.
5. Se o modelo pedir, o Pi executa uma ferramenta — sempre dentro das pastas permitidas — e entrega o resultado de volta ao modelo.
6. Na subida, o Pi verifica `http://127.0.0.1:11434`. Se não responder, inicia o Ollama e aguarda. Ao encerrar, o servidor continua rodando por padrão (`Ollama:StopOnExit=false`).

A TUI conversa com o backend por **NDJSON via stdin/stdout**. O `stdout` do C# fica reservado a essas mensagens JSON; logs e diagnósticos vão para o `stderr` ou para arquivos. Por isso o `Pi.CLI` exige `--bridge stdio`: sem essa flag, ele exibe o uso no `stderr` e encerra com código não-zero.

## Requisitos

| Requisito | Observação |
|---|---|
| [.NET SDK 10.0](https://dotnet.microsoft.com/download) | Versão fixada em `global.json` |
| [Ollama](https://ollama.com/download) | Precisa estar instalado; o Pi não instala o Ollama nem baixa modelos |
| [Bun 1.3.14+](https://bun.sh/) | Necessário para a TUI — a única interface |

## Primeira execução

```powershell
# 1. Baixe o modelo usado por padrão
.\scripts\setup-ollama.ps1 -Model qwen3.8-2b-local

# 2. (Opcional) libere pastas para a ferramenta de busca em src/appsettings.json
#    Tools:AllowedFolders — vazio por padrão, por segurança

# 3. Compile o backend e inicie a TUI
dotnet build Pi.slnx
cd tui
bun install
bun run dev
```

Digite uma mensagem e pressione `Enter`. Para sair da TUI: `Ctrl+C` com a aplicação ociosa.

A TUI usa bolhas de mensagem em coluna única e rodapé com atalhos, e inicia o DLL já compilado em `artifacts/bin/Pi.CLI/debug/Pi.CLI.dll`. Se precisar recompilar sem sair da pasta, rode `dotnet build Pi.slnx` na raiz.

## Controles

| Tecla | Ação |
|---|---|
| `Ctrl+P` | Foca o campo de texto |
| `Ctrl+L` | Limpa o contexto da conversa |
| `Esc` ou `Ctrl+C` durante a geração | Cancela o pedido atual |
| `Ctrl+C` com a TUI ociosa | Encerra e mata o processo filho |
| Mouse na área de mensagens | Rolagem; `Jump to latest ↓` reaparece no fim |

Enquanto responde, o campo mostra `Gerando… Esc para cancelar`.

## Estrutura do projeto

| Pasta | Para que serve |
|---|---|
| `src/Core/` | Decisões: agente, conversa, prompt e contratos — sem Ollama, ferramentas ou UI |
| `src/Tools/` | Ferramentas locais (buscar arquivos, abrir app) e roteamento |
| `src/Connections/Ollama/` | Cliente do modelo local e inicialização do servidor |
| `src/Connections/Mcp/` | Ponto de extensão futuro para conexões MCP |
| `src/Bridge/` | A ponte NDJSON usada pela TUI |
| `tui/` | A interface visual em TypeScript/Bun (OpenTUI + Solid) |
| `artifacts/` | Build do .NET e logs de debug — gerado automaticamente, não versionado |
| `docs/` | Arquitetura, configuração e backlog |
| `scripts/` | Scripts de apoio, como a instalação do modelo no Ollama |

`Core` não conhece `Connections` nem `Bridge`: conhece só os contratos. O `src/DependencyInjection.cs` é o único ponto que liga todas as partes, e `src/Program.cs` só aceita `--bridge stdio`. Um resumo está em [src/README.md](src/README.md); detalhes em [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Configuração

As preferências ficam em `src/appsettings.json` e podem ser sobrescritas por variáveis de ambiente:

```powershell
$env:Ollama__Model = 'llama3.2'
cd tui
bun run dev
```

Os grupos são `Ollama` (modelo, endereço e auto-start), `Assistant` (prompt, agente e histórico), `Tools` (pastas permitidas e limites) e `Logging`. A lista completa está em [docs/CONFIGURATION.md](docs/CONFIGURATION.md).

## Debug / execução monitorada

O modo debug compila o backend, sobe a TUI com o C# em `Development` e grava tudo em arquivos — sem misturar diagnósticos com o NDJSON ou com a renderização.

**No VS Code:** selecione **Pi.TUI — Debug (Bun + C#)** e pressione `F5`. A task `debug-prep` roda `dotnet build Pi.slnx` e `bun install --frozen-lockfile` antes de iniciar. Se o adaptador Node+Bun não der breakpoints, use **Pi.TUI — Terminal monitorado (fallback)**. Para depurar o C#, inicie a TUI e depois use **Pi.CLI — Anexar ao processo**.

**No terminal:**

```powershell
cd tui
bun run dev:debug
```

Para não recompilar: `PI_SKIP_BUILD=1 bun run dev:debug`.

**Acompanhando os logs:** em outra janela, `cd tui; bun run logs:debug` mostra os arquivos em `artifacts/debug/` e segue novidades em tempo real.

| Arquivo | Conteúdo |
|---|---|
| `tui.log` | exceções da TUI, spawn/saída do backend e eventos enviados |
| `pi-cli.stderr.log` | logs do C# (nível `Pi: Debug`) |
| `pi-cli.ndjson.log` | cada mensagem NDJSON recebida do backend |
| `dotnet-build.log` | saída do `dotnet build` |

`Stop`/`Shift+F5`, `Ctrl+C` ou fechar o terminal encerram a TUI e o `dotnet` filho sem deixar órfãos. O `ollama serve` permanece rodando por configuração.

## Verificação

```powershell
dotnet test Pi.slnx

cd tui
bun run typecheck
bun run build
```

## Documentação

- [Arquitetura](docs/ARCHITECTURE.md)
- [Configuração](docs/CONFIGURATION.md)
- [Backlog do projeto](docs/BACKLOG.md)
