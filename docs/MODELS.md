# Modelos locais

## Whisper (voz → texto)

Os modelos `.bin` **não são versionados**. Use o script:

```powershell
.\scripts\download-models.ps1 -Model base
```

| Modelo | Arquivo | Observação |
|---|---|---|
| tiny | `ggml-tiny.bin` | mais rápido, menos preciso |
| base | `ggml-base.bin` | padrão usado em `appsettings.json` |
| small | `ggml-small.bin` | mais preciso, mais lento |
| medium | `ggml-medium.bin` | maior consumo de memória |

Depois de baixar, ajuste `Whisper:ModelPath` se escolher outro modelo que não seja o `base`.

> O `Pi.CLI.csproj` copia automaticamente o arquivo `VoiceModels/ggml-base.bin` para o diretório de saída. Para copiar outro, defina a propriedade MSBuild:
> `dotnet run --project src/Pi.CLI -p:WhisperModelFile=ggml-small.bin`

## Ollama (texto → resposta)

```powershell
.\scripts\setup-ollama.ps1 -Model qwen2.5:3b
```

O servidor precisa estar ativo antes de iniciar o assistente. Para usar outro modelo:

```powershell
ollama pull llama3.2
```

e altere `Ollama:Model` no `appsettings.json`.
