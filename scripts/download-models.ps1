<#
.SYNOPSIS
    Baixa o modelo Whisper usado pelo Pi.CLI.
.EXAMPLE
    .\scripts\download-models.ps1 -Model base
#>
param(
    [ValidateSet('tiny', 'base', 'small', 'medium')]
    [string]$Model = 'base',

    [string]$TargetFolder = (Join-Path $PSScriptRoot '..\VoiceModels')
)

$ErrorActionPreference = 'Stop'
$fileName = "ggml-$Model.bin"
$destination = Join-Path $TargetFolder $fileName
$url = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/$fileName"

New-Item -ItemType Directory -Path $TargetFolder -Force | Out-Null

if (Test-Path $destination) {
    Write-Host "Modelo já existe: $destination"
    return
}

Write-Host "Baixando $fileName ..."
Invoke-WebRequest -Uri $url -OutFile $destination -UseBasicParsing
Write-Host "Modelo salvo em: $destination"
Write-Host "Ajuste Whisper:ModelPath no appsettings.json se necessário."
