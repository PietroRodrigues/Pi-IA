# Pi-IA

Assistente local de IA em .NET 10: entrada por **texto**, respostas via **Ollama** e ferramentas extensíveis.

## Estrutura

```
Pi-IA/
├── src/                    # raiz do código: um único projeto executável
│   ├── Abstracts/          # contratos: ILLMClient, IMessageBus, ITool
│   ├── Conversation/       # entidades da conversa
│   ├── Models/  Options/  Services/  UseCases/
│   ├── Ollama/  Messaging/  Tools/   # implementações concretas
│   ├── Workers/            # host: console e fila de mensagens
│   ├── DependencyInjection.cs
│   ├── Program.cs
│   ├── Pi.CLI.csproj
│   └── appsettings*.json
├── tests/Pi.Tests/         # projeto de testes, arquivos na raiz
├── docs/                   # arquitetura, configuração e backlog
├── scripts/                # setup do Ollama
├── artifacts/              # saída centralizada de build (bin/obj), ignorada pelo git
├── Directory.Build.props
├── Directory.Packages.props
└── Pi.slnx
```

A estrutura é **plana**: módulos são pastas por responsabilidade, sem camadas separadas em
projetos. Há apenas um `bin`, centralizado em `artifacts/`. As regras de isolamento entre
módulos são fiscalizadas por `tests/Pi.Tests/StructureTests.cs`.

## Pré-requisitos

- [.NET SDK 10.0](https://dotnet.microsoft.com/download) (fixado em `global.json`)
- [Ollama](https://ollama.com/download) em execução em `http://localhost:11434`

## Configuração rápida

```powershell
# 1. Modelo do Ollama
.\scripts\setup-ollama.ps1 -Model qwen2.5:3b

# 2. Ajuste pastas permitidas para a ferramenta de busca (seguro por padrão)
#    Edite Tools:AllowedFolders em src/appsettings.json
```

## Executar

```powershell
dotnet run --project src/Pi.CLI.csproj
```

Digite uma mensagem e pressione Enter. Use `sair` ou `Ctrl+C` para encerrar.

## Testes

```powershell
dotnet test Pi.slnx
```

## Documentação

- [Backlog](docs/BACKLOG.md)
- [Arquitetura](docs/ARCHITECTURE.md)
- [Configuração](docs/CONFIGURATION.md)
