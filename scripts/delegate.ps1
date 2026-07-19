#!/usr/bin/env pwsh
# Délègue une tâche à OpenCode Go (modèle exécutant)
# Usage: .\scripts\delegate.ps1 T-01 "instruction complète"

[CmdletBinding()]
param(
    [Parameter(Mandatory, Position = 0)]
    [string]$Ticket,

    [Parameter(Mandatory, Position = 1)]
    [string]$Instruction,

    [string]$Model = $(if ($env:OPENCODE_MODEL) { $env:OPENCODE_MODEL } else { "opencode-go/kimi-k3" })
)

$ErrorActionPreference = 'Stop'

$logDir = ".delegate-logs"
if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir | Out-Null }
$log = Join-Path $logDir "$Ticket-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"

$prompt = @"
Tu es l'executant du projet Last Magicians. Lis AGENTS.md a la racine pour les regles anti-regression.

Ticket: $Ticket
Instruction: $Instruction

CONTRAINTES ABSOLUES:
- Ne touche JAMAIS aux fichiers .unity, .prefab, .meta
- Ne touche JAMAIS a ApiConfig.asset
- N'utilise JAMAIS le MCP Unity (port 6400), il est reserve a Claude Code
- Newtonsoft.Json obligatoire pour les DTOs, pas System.Text.Json
- Async: Awaitable (Unity 6), jamais Task
- Code complet et compilable, aucun placeholder

Quand tu as fini, resume en 5 lignes max les fichiers modifies.
"@

"=== DELEGATE $Ticket -> $Model ===" | Tee-Object -FilePath $log

opencode run --model $Model $prompt 2>&1 | Tee-Object -FilePath $log -Append

"=== FIN $Ticket (log: $log) ===" | Tee-Object -FilePath $log -Append