# Pi-IA

Assistente de IA **local** para o terminal. Você digita uma mensagem e o Pi conversa com um modelo rodando no seu computador via Ollama — sem serviços em nuvem, chave de API ou telemetria.

O Pi também cuida do servidor: se o Ollama não estiver aberto, ele inicia `ollama serve` em segundo plano e espera ficar pronto antes de aceitar mensagens.

## O que é

O Pi é uma conversa por texto com memória da sessão (padrão: últimas 20 mensagens). Além de responder, ele pode usar ferramentas locais simples: buscar arquivos em pastas autorizadas e abrir aplicativos.

Existem duas formas de usar o mesmo backend em C#/.NET:

- **CLI padrão** — texto simples, sem tela própria. É o modo normal do programa.
- **TUI opcional** — interface visual em TypeScript/Bun, com painel lateral, respostas em streaming e eventos de ferramentas aparecendo na conversa.

Tudo funciona sem internet depois que o modelo foi baixado. Estão fora de escopo: voz, áudio, modelos embarcados e coleta de dados.

## Como funciona

```
Você → Pi (.NET) → Ollama (modelo local) → Pi → Você
          │                 ▲
          └── ferramentas ──┘
```

1. Você envia uma mensagem com `Enter` (na CLI) ou no campo de texto (na TUI).
2. O Pi monta o prompt com o histórico da conversa e informa ao modelo quais ferramentas estão disponíveis.
3. O Ollama gera a resposta aos poucos, em streaming.
4. O Pi devolve o resultado: linhas de texto na CLI; eventos `token`, `thought`, `tool_call` e `tool_result` na TUI.
5. Se o modelo pedir, o Pi executa uma ferramenta — sempre dentro das pastas permitidas — e entrega o resultado de volta ao modelo.
6. Na subida, o Pi verifica `http://127.0.0.1:11434`. Se não responder, inicia o Ollama e aguarda. Ao encerrar, o servidor continua rodando por padrão (`Ollama:StopOnExit=false`).

A TUI roda em `tui/` e conversa com o backend por **NDJSON via stdin/stdout**. O `stdout` do C# fica reservado a essas mensagens JSON; logs e diagnósticos vão para o `stderr` ou para arquivos. A CLI textual permanece exatamente como era — o modo bridge é opt-in (`--bridge stdio`).

## Requisitos

| Requisito | Observação |
|---|---|
| [.NET SDK 10.0](https://dotnet.microsoft.com/download) | Versão fixada em `global.json` |
| [Ollama](https://ollama.com/download) | Precisa estar instalado; o Pi não instala o Ollama nem baixa modelos |
| [Bun 1.3.14+](https://bun.sh/) | Necessário apenas para a TUI |

## Primeira execução

```powershell
# 1. Baixe o modelo usado por padrão
.\scripts\setup-ollama.ps1 -Model qwen3.8-2b-local

# 2. (Opcional) libere pastas para a ferramenta de busca em src/appsettings.json
#    Tools:AllowedFolders — vazio por padrão, por segurança

# 3. Rode o assistente
dotnet run --project src/Pi.CLI.csproj
```

Digite uma mensagem e pressione `Enter`. Para sair: `sair`, `exit` ou `Ctrl+C`.

## Escolha sua interface

| | CLI (padrão) | TUI (opcional) |
|---|---|---|
| Como iniciar | `dotnet run --project src/Pi.CLI.csproj` | `cd tui; bun run dev` |
| Tela | Linhas no terminal, sem códigos ANSI | Painel lateral, bolhas de mensagem e rodapé |
| Resposta | Texto simples no stdout | Streaming de tokens via NDJSON |
| Controles | Somente o terminal (scrollback e teclado do SO) | Atalhos próprios e rolagem com o mouse |
| Preparação | Nenhuma | Compilar o backend e rodar `bun install` uma vez |

Para usar a TUI:

```powershell
dotnet build src/Pi.CLI.csproj
cd tui
bun install
bun run dev
```

## Controles

**CLI:** `Enter` envia; `sair`, `exit`, `Ctrl+C` ou `Ctrl+D` encerram. Nenhuma tecla é interceptada — acentos e layout do teclado dependem só do sistema operacional.

**TUI:**

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
| `src/Backend/` | O "cérebro": conversa, agente, prompt, Ollama, ferramentas e inicialização |
| `src/Frontend/` | A saída: interface textual e a ponte NDJSON usada pela TUI |
| `tui/` | A interface visual em TypeScript/Bun (OpenTUI + Solid) |
| `artifacts/` | Build do .NET e logs de debug — gerado automaticamente, não versionado |
| `docs/` | Arquitetura, configuração e backlog |
| `scripts/` | Scripts de apoio, como a instalação do modelo no Ollama |

`Backend` não conhece `Frontend`: conhece apenas o contrato `IChatView`. O `src/DependencyInjection.cs` é o único ponto que liga as duas partes. Detalhes em [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Configuração

As preferências ficam em `src/appsettings.json` e podem ser sobrescritas por variáveis de ambiente:

```powershell
$env:Ollama__Model = 'llama3.2'
dotnet run --project src/Pi.CLI.csproj
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
