# Pi-IA

Assistente local de IA em .NET 10: entrada por **texto**, respostas via **Ollama** e ferramentas extensíveis.

## Estrutura

```
Pi-IA/
├── src/
│   ├── Pi.Domain/          # entidades e regras da conversa (sem dependências externas)
│   ├── Pi.Application/     # contratos, casos de uso e orquestração
│   ├── Pi.Infrastructure/  # Ollama, canais e ferramentas
│   └── Pi.CLI/             # host, injeção de dependências e workers
├── tests/                  # testes unitários por camada
├── docs/                   # arquitetura e configuração
├── scripts/                # setup do Ollama
├── Directory.Build.props
├── Directory.Packages.props
└── Pi.slnx
```

## Pré-requisitos

- [.NET SDK 10.0](https://dotnet.microsoft.com/download) (fixado em `global.json`)
- [Ollama](https://ollama.com/download) em execução em `http://localhost:11434`

## Configuração rápida

```powershell
# 1. Modelo do Ollama
.\scripts\setup-ollama.ps1 -Model qwen2.5:3b

# 2. Ajuste pastas permitidas para a ferramenta de busca (seguro por padrão)
#    Edite Tools:AllowedFolders em src/Pi.CLI/appsettings.json
```

## Executar

```powershell
dotnet run --project src/Pi.CLI
```

Digite uma mensagem e pressione Enter. Use `sair` ou `Ctrl+C` para encerrar.

## Testes

```powershell
dotnet test Pi.slnx
```

## Documentação

- [Arquitetura](docs/ARCHITECTURE.md)
- [Configuração](docs/CONFIGURATION.md)
