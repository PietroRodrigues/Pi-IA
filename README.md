# Pi-IA

Assistente de IA **local** para o terminal. Você digita uma mensagem e o Pi conversa com um modelo rodando no seu computador via Ollama — sem serviços em nuvem, chave de API ou telemetria.

O Pi também cuida do servidor: se o Ollama não estiver aberto, ele inicia `ollama serve` em segundo plano e espera ficar pronto antes de aceitar mensagens.

## Módulos

O repositório mantém a configuração compartilhada na raiz (`global.json`, `Directory.Build.props`, `Directory.Packages.props`, `artifacts/`) e o código em `Pi.App/`:

```text
Pi-IA/                            configuração compartilhada (.NET SDK, pacotes, artifacts/)
├── Pi.App/                       ← projeto pai: entry point e dono dos módulos
│   ├── Pi.App.slnx               ← solution que orquestra os projetos .NET
│   ├── Model/ View/ Controller/  ← MVC do pai (ciclo de vida)
│   ├── Modulos/
│   │   ├── BodyTools/            ← backend .NET (Model / Controller / View)
│   │   └── interfaceTui/         ← TUI Bun + OpenTUI (model / view / controller / infra + tests)
│   ├── tests/BodyTools.Tests/    ← testes xUnit do backend
│   ├── docs/                     ← arquitetura, configuração e backlog
│   └── scripts/                  ← instalação do modelo no Ollama
└── artifacts/                    ← build e logs de debug (gerado, não versionado)
```

Cada módulo é independente: o `BodyTools` não conhece a TUI, e a `interfaceTui` não conhece namespaces C#. O único contrato entre eles é o **NDJSON via stdin/stdout**. O `Pi.App` é o projeto pai: ele só descobre os módulos, garante o build e inicia a TUI.

## Como funciona

```
Pi.App (.NET) → interfaceTui (Bun) → BodyTools (.NET, --bridge stdio) → Ollama → interfaceTui
                │                     │
                └─ terminal           └─ NDJSON + ferramentas
```

1. A **View** da TUI envia o texto digitado ao **Controller** da TUI.
2. O Controller publica `prompt` em NDJSON; o **Controller** do BodyTools interpreta e chama o **Model** (agente, conversa, ferramentas).
3. O Ollama gera a resposta em streaming.
4. A **View** do BodyTools projeta `token`, `thought`, `tool_call`, `tool_result` e `context_stats` em NDJSON.
5. O Controller da TUI aplica os eventos ao seu **Model**, e a View renderiza a transcrição.

O `stdout` do C# fica reservado ao NDJSON; logs e diagnósticos vão para o `stderr` ou arquivos. Por isso o `BodyTools` exige `--bridge stdio`: sem a flag, ele exibe o uso no `stderr` e encerra com código não-zero.

## Requisitos

| Requisito | Observação |
|---|---|
| [.NET SDK 10.0](https://dotnet.microsoft.com/download) | Versão fixada em `global.json` |
| [Ollama](https://ollama.com/download) | Precisa estar instalado; o Pi não instala o Ollama nem baixa modelos |
| [Bun 1.3.14+](https://bun.sh/) | Necessário para a `interfaceTui` — a única interface |

## Primeira execução

```powershell
# 1. Baixe o modelo usado por padrão
.\Pi.App\scripts\setup-ollama.ps1 -Model qwen3.8-2b-local

# 2. (Opcional) libere pastas para a ferramenta de busca em Pi.App/Modulos/BodyTools/appsettings.json
#    Tools:AllowedFolders — vazio por padrão, por segurança

# 3. Compile os módulos e inicie tudo pelo projeto pai
dotnet run --project Pi.App
```

Digite uma mensagem e pressione `Enter`. Para sair: `Ctrl+C` com a aplicação ociosa.

A TUI inicia o DLL já compilado em `artifacts/bin/BodyTools/debug/BodyTools.dll`, não `dotnet run`, para não corromper o parser com mensagens de build. O `dotnet run --project Pi.App` compila o `BodyTools` via `ProjectReference` antes de abrir a TUI; para validar sem terminal, use `PI_SKIP_TUI=1`.

## Controles

| Tecla | Ação |
|---|---|
| `Ctrl+P` | Foca o campo de texto |
| `Ctrl+L` | Limpa o contexto da conversa |
| `Esc` ou `Ctrl+C` durante a geração | Cancela o pedido atual |
| `Ctrl+C` com a TUI ociosa | Encerra e mata o processo filho |
| Mouse na área de mensagens | Rolagem; `Jump to latest ↓` reaparece no fim |

Enquanto responde, o campo mostra `Gerando… Esc para cancelar`.

## Estrutura MVC

| Módulo | Papel |
|---|---|
| `Pi.App/Model/` | Descoberta e validação dos módulos, estado do ciclo de vida |
| `Pi.App/View/` | Mensagens do launcher no stderr do desenvolvedor |
| `Pi.App/Controller/` | Build → install → spawn da TUI → shutdown → exit code |
| `Pi.App/Modulos/BodyTools/Model/` | Decisões: agente, conversa, contratos, ferramentas e conexões |
| `Pi.App/Modulos/BodyTools/Controller/` | Entrada NDJSON, `requestId`, cancelamento e despacho ao Model |
| `Pi.App/Modulos/BodyTools/View/` | DTOs do protocolo, escritor NDJSON e eventos tipados |
| `Pi.App/Modulos/interfaceTui/src/model/` | Estado reativo e regras do pedido ativo |
| `Pi.App/Modulos/interfaceTui/src/controller/` | Eventos, teclado, submit, cancel/clear e shutdown |
| `Pi.App/Modulos/interfaceTui/src/view/` | Componentes OpenTUI + Solid |
| `Pi.App/Modulos/interfaceTui/src/infra/` | Cliente da ponte, protocolo e monitor de debug |

`Model` não conhece `Controller`/`View`; `View` só conhece os eventos do Model; na TUI, `view/` nunca importa o cliente da ponte. Guardrails em `tests/BodyTools.Tests/StructureTests.cs` e `bun run check:arch` protegem essas regras.

## Configuração

As preferências ficam em `Pi.App/Modulos/BodyTools/appsettings.json` e podem ser sobrescritas por variáveis de ambiente:

```powershell
$env:Ollama__Model = 'llama3.2'
cd Pi.App/Modulos/interfaceTui
bun run dev
```

Os grupos são `Ollama`, `Assistant`, `Tools` e `Logging`. Detalhes em [Pi.App/docs/CONFIGURATION.md](Pi.App/docs/CONFIGURATION.md).

## Debug / execução monitorada

No VS Code, selecione **Pi.App — Debug (tudo)** e pressione `F5`: a task `debug-prep` compila os três projetos e instala as dependências, e o `Pi.App` inicia a TUI. A config **Pi.App — Debug (C# + TUI)** adiciona o attach do Bun na porta 9229 (se o attach começar antes da TUI, rode **TUI — Attach (9229)** manualmente). Para breakpoint no backend, use **BodyTools — Anexar ao processo**. Equivalente no terminal:

```powershell
cd Pi.App/Modulos/interfaceTui
bun run dev:debug
```

Para não recompilar: `PI_SKIP_BUILD=1 bun run dev:debug`. Acompanhe os logs com `bun run logs:debug` na mesma pasta; os arquivos ficam em `artifacts/debug/`.

## Verificação

```powershell
dotnet build Pi.App/Pi.App.slnx
dotnet test Pi.App/Pi.App.slnx

cd Pi.App/Modulos/interfaceTui
bun run check:arch
bun run typecheck
bun run test
bun run build
```

## Documentação

- [Arquitetura](Pi.App/docs/ARCHITECTURE.md)
- [Configuração](Pi.App/docs/CONFIGURATION.md)
- [Backlog do projeto](Pi.App/docs/BACKLOG.md)
