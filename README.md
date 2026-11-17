# Pi-IA

Assistente local de IA em .NET 10: entrada por **texto** e **voz**, transcrição com **Whisper.net**, respostas via **Ollama** e ferramentas extensíveis.

## Estrutura

```
Pi-IA/
├── src/
│   ├── Pi.Domain/          # entidades e regras da conversa (sem dependências externas)
│   ├── Pi.Application/     # contratos, casos de uso e orquestração
│   ├── Pi.Infrastructure/  # Ollama, Whisper, NAudio, canais e ferramentas
│   └── Pi.CLI/             # host, injeção de dependências e workers
├── tests/                  # testes unitários por camada
├── docs/                   # arquitetura, configuração e modelos
├── scripts/                # download de modelos e setup do Ollama
├── Directory.Build.props
├── Directory.Packages.props
└── Pi.slnx
```

## Pré-requisitos

- [.NET SDK 10.0](https://dotnet.microsoft.com/download) (fixado em `global.json`)
- [Ollama](https://ollama.com/download) em execução em `http://localhost:11434`
- Um modelo Whisper (`.bin`) em `VoiceModels/`

## Configuração rápida

```powershell
# 1. Modelo de voz (gera VoiceModels/ggml-base.bin)
.\scripts\download-models.ps1 -Model base

# 2. Modelo do Ollama
.\scripts\setup-ollama.ps1 -Model qwen2.5:3b

# 3. Ajuste pastas permitidas para a ferramenta de busca (seguro por padrão)
#    Edite Tools:AllowedFolders em src/Pi.CLI/appsettings.json
```

## Executar

```powershell
dotnet run --project src/Pi.CLI
```

Digite uma mensagem ou fale (a gravação dura `Audio:CaptureDurationMs` milissegundos). Use `sair` ou `Ctrl+C` para encerrar.

## Testes

```powershell
dotnet test Pi.slnx
```

## Documentação

- [Arquitetura](docs/ARCHITECTURE.md)
- [Configuração](docs/CONFIGURATION.md)
- [Modelos](docs/MODELS.md)
