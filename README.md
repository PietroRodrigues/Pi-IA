# Pi-IA

Assistente de IA **local-first** para o terminal, feito em .NET 10, com uma TUI opcional em TypeScript/Bun (OpenTUI). Você digita texto e o Pi conversa com um modelo rodando no seu computador via Ollama — sem serviços em nuvem, chaves de API ou telemetria.

O Pi também cuida do servidor: se o Ollama não estiver aberto, ele mesmo inicia `ollama serve` em segundo plano antes de aceitar mensagens.

## O que o projeto entrega hoje

- Conversa por texto com histórico em memória (limite padrão: 20 mensagens).
- Interface textual simples: boas-vindas com modelo/agente, prompt `você ›`, respostas em linhas comuns e nenhum código ANSI.
- Nenhum atalho próprio, nenhuma leitura de tecla e nenhuma rolagem customizada na CLI: o terminal cuida do scrollback e o sistema operacional cuida do layout do teclado.
- TUI opcional em `tui/` (OpenTUI + Solid), com painel lateral, streaming de tokens e eventos de ferramentas via NDJSON.
- Modelo local via Ollama (`/api/generate`), configurável — padrão `qwen3.8-2b-local`.
- Inicialização automática do servidor Ollama local, com espera e mensagens claras em caso de falha.
- Ferramentas extensíveis: busca em arquivos restrita a pastas autorizadas e abertura de aplicativos.
- Respostas no stdout e logs no stderr. A saída é sempre texto simples, sem códigos ANSI — fácil de usar em pipelines e arquivos.
- Funciona sem internet depois que o modelo está baixado.

**Fora de escopo:** voz, áudio, modelos embarcados e coleta de dados. O projeto é estritamente textual e local.

## Requisitos

| Requisito | Observação |
|---|---|
| [.NET SDK 10.0](https://dotnet.microsoft.com/download) | Versão fixada em `global.json` |
| [Ollama](https://ollama.com/download) | Precisa estar instalado; o Pi não o instala nem baixa modelos |
| [Bun 1.3.14+](https://bun.sh/) | Necessário apenas para a TUI em `tui/` |

## Primeira execução

```powershell
# 1. Baixe o modelo usado por padrão
.\scripts\setup-ollama.ps1 -Model qwen3.8-2b-local

# 2. (Opcional) libere pastas para a ferramenta de busca em src/appsettings.json
#    Tools:AllowedFolders — vazio por padrão, por segurança

# 3. Rode o assistente
dotnet run --project src/Pi.CLI.csproj
```

Digite uma mensagem e pressione Enter. Para sair: `sair`, `exit` ou `Ctrl+C`.

### Interface

O Pi não desenha tela própria: escreve linhas no stdout e lê o stdin. `Enter` envia a mensagem; `sair`, `exit`, `Ctrl+C` ou EOF (`Ctrl+D`) encerram. O scrollback e o histórico são os do seu terminal.

Como nenhuma tecla é interceptada, acentos, pontuação e teclas mortas dependem apenas do layout configurado no sistema operacional. A mesma saída serve para terminal, arquivo ou pipe.

### TUI (opcional)

A interface visual fica isolada em `tui/` e não substitui a CLI. Ela inicia o backend C# como subprocesso e conversa por **NDJSON via stdin/stdout**: o stdout do .NET fica exclusivo para JSON, enquanto logs vão para stderr.

```powershell
# 1. Compile o backend uma vez
dotnet build src/Pi.CLI.csproj

# 2. Instale e rode a TUI
cd tui
bun install
bun run dev
```

Atalhos: `Ctrl+P` foca o input, `Ctrl+L` limpa o contexto, `Esc` (ou `Ctrl+C` durante a geração) cancela o pedido atual, e `Ctrl+C` com a TUI ociosa encerra e mata o processo filho. Enquanto responde, o input mostra `Gerando… Esc para cancelar`. A rolagem do mouse funciona na área de mensagens; quando você sobe, aparece `Jump to latest ↓`.

### Debug / execução monitorada

O modo debug garante build atualizado, sobe a TUI com o backend C# em `Development` e grava tudo em arquivos — sem poluir o `stdout` NDJSON nem a renderização.

**Pelo VS Code:** selecione **Pi.TUI — Debug (Bun + C#)** no painel Run and Debug e pressione F5. A task `debug-prep` compila `Pi.slnx` e roda `bun install --frozen-lockfile` antes de iniciar. Se o adaptador Node+Bun não oferecer breakpoints no seu ambiente, use **Pi.TUI — Terminal monitorado (fallback)**. Para depurar o C# com breakpoints, inicie a TUI normalmente e depois use **Pi.CLI — Anexar ao processo**, escolhendo o `dotnet` filho.

**Pelo terminal:**

```powershell
cd tui
bun run dev:debug
```

O script compila `src/Pi.CLI.csproj` (saída em `artifacts/debug/dotnet-build.log`), liga `PI_DEBUG=1` e `DOTNET_ENVIRONMENT=Development`, e só então importa a TUI. Para não recompilar, use `PI_SKIP_BUILD=1 bun run dev:debug`.

**Onde acompanhar:** em outra janela, `cd tui; bun run logs:debug` mostra os quatro arquivos em `artifacts/debug/` e acompanha novidades em tempo real:

| Arquivo | Conteúdo |
|---|---|
| `tui.log` | exceções não tratadas, rejeições, spawn/saída do backend, eventos enviados |
| `pi-cli.stderr.log` | `stderr` cru do C# (logs do `appsettings.Development.json`, nível `Pi: Debug`) |
| `pi-cli.ndjson.log` | cada linha do protocolo NDJSON recebida do backend |
| `dotnet-build.log` | saída do `dotnet build` |

`Stop`/`Shift+F5`, `Ctrl+C` ou fechar o terminal enviam `cancel` ao pedido ativo, fecham o stdin e encerram o `dotnet` (com `SIGKILL` de segurança após 1 s). O `ollama serve` é externo e permanece rodando por configuração.

### Onde está cada coisa

Dentro de `src/` existem apenas dois troncos:

| Pasta | Conteúdo |
|---|---|
| `src/Frontend/` | interface textual mínima (`ConsoleChatView`) |
| `src/Backend/` | loop da conversa, agente, caso de uso, IA (Ollama), ferramentas, bootstrap e ponte NDJSON |

Fora de `src/`, a pasta `tui/` contém o projeto TypeScript/Bun da interface visual. O modo `--bridge stdio` do backend é aditivo: sem ele, o programa continua sendo exatamente a CLI textual.

`Backend` não conhece `Frontend`: conhece só o contrato `IChatView`, definido em `src/Backend/Chat/`. O `DependencyInjection.cs` é o único lugar que liga as duas partes. Detalhes em [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

Na subida, o Pi verifica o servidor em `http://127.0.0.1:11434`. Se ele não responder, inicia o Ollama e aguarda até ficar pronto. Ao encerrar, o servidor continua rodando por padrão (`Ollama:StopOnExit=false`), então a próxima execução já o encontra disponível.

## Configuração

As preferências ficam em `src/appsettings.json` e podem ser sobrescritas por variáveis de ambiente:

```powershell
$env:Ollama__Model = 'llama3.2'
dotnet run --project src/Pi.CLI.csproj
```

Os grupos são `Ollama` (modelo, endereço, auto-start), `Assistant` (prompt, agente, histórico, limite de contexto exibido na TUI), `Tools` (pastas permitidas e limites) e `Logging`. A lista completa está em [docs/CONFIGURATION.md](docs/CONFIGURATION.md).

## Verificação

```powershell
dotnet test Pi.slnx

# TUI
cd tui
bun run typecheck
bun run build
```

## Documentação

- [Backlog do projeto](docs/BACKLOG.md) — roadmap evolutivo
- [Configuração](docs/CONFIGURATION.md)
- [Arquitetura](docs/ARCHITECTURE.md)
