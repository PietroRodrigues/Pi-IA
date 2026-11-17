<#
.SYNOPSIS
    Instala o modelo padrão do Ollama usado pelo Pi.CLI.
.EXAMPLE
    .\scripts\setup-ollama.ps1 -Model qwen2.5:3b
#>
param(
    [string]$Model = 'qwen2.5:3b'
)

$ErrorActionPreference = 'Stop'

if (-not (Get-Command ollama -ErrorAction SilentlyContinue)) {
    throw 'Ollama não encontrado. Instale em https://ollama.com/download antes de continuar.'
}

Write-Host "Baixando modelo $Model ..."
ollama pull $Model

if ($LASTEXITCODE -ne 0) {
    throw "Falha ao baixar o modelo $Model."
}

Write-Host "Modelo $Model pronto. Confira Ollama:Model no appsettings.json."
