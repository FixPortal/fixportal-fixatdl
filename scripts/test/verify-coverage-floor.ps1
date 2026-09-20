<#
.SYNOPSIS
    Exercises assert-coverage-floor.ps1, including every path where it must FAIL.

.DESCRIPTION
    The floor's whole job is to tell "coverage dropped" from "coverage was never
    measured". A checker that reads an absent or malformed number as a pass cannot do
    that, so the failure paths matter more here than the happy one and are all covered.

    The NaN case is why this file exists: `[double]'NaN' -lt 70` evaluates to $false in
    PowerShell, so before the guard was added a report carrying a NaN rate sailed past
    the floor and printed a pass.
#>
$ErrorActionPreference = 'Stop'

$script = Join-Path (Split-Path $PSScriptRoot -Parent) 'assert-coverage-floor.ps1'
$root = Join-Path ([IO.Path]::GetTempPath()) ('coverage-floor-' + [guid]::NewGuid().ToString('N'))

function New-Report([string] $Body) {
    $path = Join-Path $root ([guid]::NewGuid().ToString('N') + '.xml')
    Set-Content -LiteralPath $path -Value $Body -Encoding utf8
    $path
}

function Invoke-Floor([string] $ReportPath, [string] $Package = 'Lib', [double] $Floor = 70) {
    $output = & pwsh -NoProfile -File $script -ReportPath $ReportPath -Package $Package -MinimumLineRate $Floor 2>&1 | Out-String
    [pscustomobject]@{ Code = $LASTEXITCODE; Output = $output }
}

function Assert-Fails($result, [string] $expected, [string] $case) {
    if ($result.Code -eq 0) { throw "$case must FAIL, but exited 0:`n$($result.Output)" }
    if ($result.Output -notmatch $expected) { throw "$case failed for the wrong reason (wanted /$expected/):`n$($result.Output)" }
}

try {
    New-Item -ItemType Directory -Path $root | Out-Null

    $healthy = New-Report @'
<coverage line-rate="0.84" branch-rate="0.66">
  <packages>
    <package name="Lib" line-rate="0.733" branch-rate="0.647" />
    <package name="Lib.Tests" line-rate="0.997" branch-rate="0.9" />
  </packages>
</coverage>
'@
    $pass = Invoke-Floor $healthy
    if ($pass.Code -ne 0) { throw "a report above the floor must pass:`n$($pass.Output)" }
    if ($pass.Output -notmatch '73\.3%') { throw "the summary must report the LIBRARY rate:`n$($pass.Output)" }
    # The whole-report rate is 84%, the library 73.3%. Gating the former would let the
    # library rot ~11 points before failing, because the test assembly covers itself.
    if ($pass.Output -match '84\.0%') { throw "the summary must not report the flattered whole-report rate:`n$($pass.Output)" }

    Assert-Fails (Invoke-Floor $healthy 'Lib' 99) 'below the' 'a rate under the floor'
    Assert-Fails (Invoke-Floor $healthy 'Absent') 'no package named' 'a package that is not in the report'
    Assert-Fails (Invoke-Floor (Join-Path $root 'nope.xml')) 'not found' 'a missing report'

    Assert-Fails (Invoke-Floor (New-Report '<coverage><packages></packages></coverage>')) 'no packages' 'a report with no packages'
    Assert-Fails (Invoke-Floor (New-Report '<coverage><packages><package name="Lib"')) 'not parseable' 'unparsable XML'

    # THE FAIL-OPEN CASE. NaN compares false against any floor, so this passed before the
    # guard existed -- the checker printed a clean pass over a report it could not read.
    Assert-Fails (Invoke-Floor (New-Report '<coverage><packages><package name="Lib" line-rate="NaN" branch-rate="0.5" /></packages></coverage>')) 'non-finite' 'a NaN line rate'
    Assert-Fails (Invoke-Floor (New-Report '<coverage><packages><package name="Lib" line-rate="Infinity" branch-rate="0.5" /></packages></coverage>')) 'non-finite' 'an infinite line rate'
    Assert-Fails (Invoke-Floor (New-Report '<coverage><packages><package name="Lib" branch-rate="0.5" /></packages></coverage>')) "no 'line-rate'" 'a missing line-rate attribute'
    Assert-Fails (Invoke-Floor (New-Report '<coverage><packages><package name="Lib" line-rate="" branch-rate="0.5" /></packages></coverage>')) "no 'line-rate'" 'an empty line-rate'
    Assert-Fails (Invoke-Floor (New-Report '<coverage><packages><package name="Lib" line-rate="high" branch-rate="0.5" /></packages></coverage>')) 'non-numeric' 'a non-numeric line-rate'
    Assert-Fails (Invoke-Floor (New-Report '<coverage><packages><package name="Lib" line-rate="7.33" branch-rate="0.5" /></packages></coverage>')) 'out-of-range' 'a rate above 1 (a percentage written as a fraction)'
    Assert-Fails (Invoke-Floor (New-Report '<coverage><packages><package name="Lib" line-rate="-0.1" branch-rate="0.5" /></packages></coverage>')) 'out-of-range' 'a negative rate'

    # Two packages of the same name: -eq on an array filters rather than compares, so
    # this used to reach the cast with two elements and throw something unhelpful.
    Assert-Fails (Invoke-Floor (New-Report '<coverage><packages><package name="Lib" line-rate="0.9" branch-rate="0.5" /><package name="Lib" line-rate="0.1" branch-rate="0.5" /></packages></coverage>')) 'cannot decide' 'a duplicated package name'

    # A single <package> element does not come back as an array; without @() the count
    # test would misread and the report would look empty.
    $single = New-Report '<coverage><packages><package name="Lib" line-rate="0.75" branch-rate="0.5" /></packages></coverage>'
    $one = Invoke-Floor $single
    if ($one.Code -ne 0) { throw "a report with exactly one package must pass:`n$($one.Output)" }

    'assert-coverage-floor.ps1 OK - library-not-total, floor breach, missing/duplicate package, and every unreadable-rate path fails closed'
}
finally {
    if ($root.StartsWith([IO.Path]::GetTempPath(), [StringComparison]::OrdinalIgnoreCase)) {
        Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
    }
}

# GitHub's pwsh wrapper exits with $LASTEXITCODE when it exists; do not leak the
# final child process's code after every verifier assertion has passed.
exit 0
