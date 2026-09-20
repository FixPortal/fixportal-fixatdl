<#
.SYNOPSIS
    Fails when library line coverage drops below a floor, and writes a short summary.

.DESCRIPTION
    Reads a Cobertura report and gates on the PRODUCTION assembly's line rate.

    Measuring the report's top-level `line-rate` would be wrong: it averages the test
    assembly in, and a test assembly covers itself almost completely. Measured on this
    repository at the time this script was written, the whole-report rate was 84.3%
    while the library alone was 73.3% -- an 11-point flattery that would let real
    library coverage rot a long way before any floor noticed.

    A missing package, a report with no packages, or an unparsable file all FAIL. The
    whole point of a floor is to distinguish "coverage dropped" from "coverage was
    never measured", and a checker that treats an absent number as a pass cannot.

.PARAMETER ReportPath
    Path to the Cobertura XML report.

.PARAMETER Package
    The package (assembly) name to gate on, as it appears in the report.

.PARAMETER MinimumLineRate
    The floor, as a percentage.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $ReportPath,
    [Parameter(Mandatory)][string] $Package,
    [Parameter(Mandatory)][double] $MinimumLineRate
)

# Stop applies to CMDLET failures, which should abort. It deliberately does NOT drive the
# guard clauses below: under Stop, `Write-Error` is itself script-terminating, so the
# `exit 1` after it never runs and the exit code comes from the unhandled error instead.
# The code was right by accident. Each guard now writes to the error stream with
# -ErrorAction Continue and exits explicitly, so the control flow says what it does.
$ErrorActionPreference = 'Stop'

function Fail([string] $Message) {
    Write-Error $Message -ErrorAction Continue
    exit 1
}

if (-not (Test-Path -LiteralPath $ReportPath -PathType Leaf)) {
    Fail "Coverage report not found at '$ReportPath'. The collect step did not produce one, so coverage was not measured -- this is a failure, not a pass."
}

try {
    $report = [xml](Get-Content -LiteralPath $ReportPath -Raw)
}
catch {
    Fail "Coverage report at '$ReportPath' is not parseable XML: $($_.Exception.Message)"
}

# @() is load-bearing: a single <package> element does not come back as an array, so
# `.Count` on the bare value would be $null and every count test below would misread.
# `Where-Object { $_ }` is load-bearing, not tidiness. On `<packages></packages>` the
# property access yields $null, and @($null) has Count 1 -- so the emptiness check below
# never fired, and an empty report fell through to "no package named X. Present: " with
# nothing listed. It still failed, but named the wrong cause.
$packages = @($report.coverage.packages.package | Where-Object { $_ })
if ($packages.Count -eq 0) {
    Fail "Coverage report at '$ReportPath' contains no packages. Nothing was measured."
}

$target = $packages | Where-Object { $_.name -eq $Package }
if (-not $target) {
    $names = ($packages | ForEach-Object { $_.name }) -join ', '
    Fail "Coverage report contains no package named '$Package'. Present: $names. A renamed assembly silently ends coverage enforcement, so this fails rather than passing over it."
}

# -eq on an array FILTERS rather than compares, so a duplicated package name would
# leave $target holding two elements and the cast below would throw an unhelpful error.
$target = @($target)
if ($target.Count -gt 1) {
    Fail "Coverage report contains $($target.Count) packages named '$Package'; cannot decide which to gate on."
}

# A rate must be parsed and range-checked BEFORE it is compared, because the comparison
# fails OPEN on a non-finite value: `[double]'NaN' -lt 70` is $false, so a NaN rate would
# sail past the floor and report a pass. A missing attribute casts to 0 and would fail
# safe, but "0% coverage" and "no line-rate attribute" are different facts and should not
# produce the same message.
function ConvertTo-Rate([object] $Element, [string] $Attribute) {
    $raw = $Element.$Attribute
    if ($null -eq $raw -or [string]::IsNullOrWhiteSpace([string]$raw)) {
        Fail "Package '$Package' has no '$Attribute' attribute. Coverage was not measured for it."
    }
    [double] $parsed = 0
    if (-not [double]::TryParse([string]$raw, [Globalization.NumberStyles]::Float,
            [Globalization.CultureInfo]::InvariantCulture, [ref] $parsed)) {
        Fail "Package '$Package' has a non-numeric '$Attribute' of '$raw'."
    }
    if (-not [double]::IsFinite($parsed)) {
        Fail "Package '$Package' has a non-finite '$Attribute' of '$raw'. A NaN compares false against any floor, so this would otherwise pass."
    }
    if ($parsed -lt 0 -or $parsed -gt 1) {
        Fail "Package '$Package' has an out-of-range '$Attribute' of '$raw'; a Cobertura rate is a fraction in [0,1]."
    }
    $parsed * 100
}

$lineRate = ConvertTo-Rate $target[0] 'line-rate'
$branchRate = ConvertTo-Rate $target[0] 'branch-rate'

$summary = @(
    "### Coverage floor",
    "",
    "| Assembly | Line | Branch | Floor |",
    "|---|--:|--:|--:|",
    ("| ``{0}`` | {1:N1}% | {2:N1}% | {3:N1}% |" -f $Package, $lineRate, $branchRate, $MinimumLineRate)
)

if ($lineRate -lt $MinimumLineRate) {
    $summary += ""
    $summary += ("**FAILED** - line coverage {0:N1}% is below the {1:N1}% floor." -f $lineRate, $MinimumLineRate)
    $summary
    Fail ("Line coverage for '{0}' is {1:N1}%, below the {2:N1}% floor." -f $Package, $lineRate, $MinimumLineRate)
}

$summary
exit 0
