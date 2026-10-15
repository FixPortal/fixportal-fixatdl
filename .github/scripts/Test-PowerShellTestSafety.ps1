#Requires -Version 7
<# Parse tracked tests/verifiers only. Copy beside Find-PesterSuites.ps1; executes no inspected source.
   This lint supplements sandbox execution and cannot prove runtime deletion-target ownership. #>
[CmdletBinding()]
param([string] $Repository = (Join-Path $PSScriptRoot '..' '..'))
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $Repository).Path
$gitRoot = & git -C $root rev-parse --show-toplevel
if ($LASTEXITCODE -ne 0 -or [IO.Path]::GetFullPath($gitRoot) -ne [IO.Path]::GetFullPath($root)) {
    throw 'Repository must be the Git checkout root'
}
. (Join-Path $PSScriptRoot 'Find-PesterSuites.ps1')
# NUL separators preserve spaces, Unicode and newlines; no filesystem walk or untracked worktree reads.
$listed = @(& git -C $root -c core.quotepath=false ls-files -z -- ':(icase)*.ps1')
if ($LASTEXITCODE -ne 0) { throw 'Git tracked-file discovery failed' }
$files = @(($listed -join "`n") -split "`0" | Where-Object { $_ -match '(?i)(^|/)(verify-[^/]*\.ps1|[^/]+\.Tests?\.ps1)$' })
if ($files.Count -eq 0) { throw 'Found no tracked PowerShell test or verifier files; refusing empty coverage' }
$failed = 0
foreach ($relative in $files) {
    $path = Join-Path $root $relative
    # Refuse file links and linked parent directories before reading any source.
    $cursor = $path
    while ($cursor -ne $root) {
        $entry = Get-Item -LiteralPath $cursor -Force -ErrorAction Stop
        if ($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Refusing linked source path: $relative" }
        $cursor = Split-Path -Parent $cursor
    }
    $flags = @(Get-PesterRisk -Source ([IO.File]::ReadAllText($path)) | Where-Object { $_ -in 'automatic-variable-assignment', 'parse-error' })
    if ($flags.Count) {
        Write-Host "FAIL: $relative : $($flags -join ', ')"
        $failed++
    }
}
if ($failed) { throw "$failed tracked PowerShell test/verifier file(s) failed safety lint" }
Write-Host "PASS: $($files.Count) tracked PowerShell test/verifier file(s); no automatic-variable assignment or parse error"
exit 0
