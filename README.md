# Pi-IA

Assistente de IA **local-first** para o terminal, feito em .NET 10. Você digita texto e o Pi conversa com um modelo rodando no seu computador via Ollama — sem serviços em nuvem, chaves de API ou telemetria.

O Pi também cuida do servidor: se o Ollama não estiver aberto, ele mesmo inicia `ollama serve` em segundo plano antes de aceitar mensagens.

## O que o projeto entrega hoje

- Conversa por texto com histórico em memória (limite padrão: 20 mensagens).
- Modelo local via Ollama (`/api/generate`), configurável — padrão `qwen2.5:3b`.
- Inicialização automática do servidor Ollama local, com espera e mensagens claras em caso de falha.
- Ferramentas extensíveis: busca em arquivos restrita a pastas autorizadas e abertura de aplicativos.
- Respostas no stdout e logs no stderr — fácil de usar em pipelines e redirecionamentos.
- Funciona sem internet depois que o modelo está baixado.

**Fora de escopo:** voz, áudio, modelos embarcados e coleta de dados. O projeto é estritamente textual e local.

## Requisitos

| Requisito | Observação |
|---|---|
| [.NET SDK 10.0](https://dotnet.microsoft.com/download) | Versão fixada em `global.json` |
| [Ollama](https://ollama.com/download) | Precisa estar instalado; o Pi não o instala nem baixa modelos |

## Primeira execução

```powershell
# 1. Baixe o modelo usado por padrão
.\scripts\setup-ollama.ps1 -Model qwen2.5:3b

# 2. (Opcional) libere pastas para a ferramenta de busca em src/appsettings.json
#    Tools:AllowedFolders — vazio por padrão, por segurança

# 3. Rode o assistente
dotnet run --project src/Pi.CLI.csproj
```

Digite uma mensagem e pressione Enter. Para sair: `sair`, `exit` ou `Ctrl+C`.

Na subida, o Pi verifica o servidor em `http://127.0.0.1:11434`. Se ele não responder, inicia o Ollama e aguarda até ficar pronto. Ao encerrar, o servidor continua rodando por padrão (`Ollama:StopOnExit=false`), então a próxima execução já o encontra disponível.

## Configuração

As preferências ficam em `src/appsettings.json` e podem ser sobrescritas por variáveis de ambiente:

```powershell
$env:Ollama__Model = 'llama3.2'
dotnet run --project src/Pi.CLI.csproj
```

Os grupos são `Ollama` (modelo, endereço, auto-start), `Assistant` (prompt, histórico, palavras de saída), `Tools` (pastas permitidas e limites) e `Logging`. A lista completa está em [docs/CONFIGURATION.md](docs/CONFIGURATION.md).

## Verificação

```powershell
dotnet test Pi.slnx
```

## Documentação

- [Backlog do projeto](docs/BACKLOG.md) — roadmap evolutivo
- [Configuração](docs/CONFIGURATION.md)
- [Arquitetura](docs/ARCHITECTURE.md)
