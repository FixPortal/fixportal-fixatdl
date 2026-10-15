#Requires -Version 7
<#
.SYNOPSIS
    Finds Pester suites under a repository and flags destructive shapes. Parses only; executes nothing.

.DESCRIPTION
    Discovery for the run-tests skill. A Pester suite found here is REPORTED, never run in the working profile: see "Isolated lane and
    trusted lane" in SKILL.md. The flags lower the chance of a mistake going unnoticed; they are not isolation and say nothing about
    a suite that builds its commands dynamically (that is itself flagged as 'dynamic-code').

    Dot-source for the functions, or run with -Repository to list suites.

    Flags: recursive-delete, automatic-variable-assignment, environment-write, junction-or-symlink, dynamic-code (all destructive),
    and profile-reference (informational).
#>
param([string] $Repository, [switch] $IncludeVerifyScripts)

# Read-only or meaning-bearing automatic variables. Benign, routinely assigned ones ($null discard, $global:LASTEXITCODE in mocks,
# $Args, $Input, $Error, $Matches, $Event) are deliberately not listed: flagging them buries the real signal.
$script:AutomaticVariables = @('Home', 'PSHome', 'Profile', 'PID', 'Host', 'PWD', 'ExecutionContext', 'MyInvocation', 'PSCmdlet',
    'PSCommandPath', 'PSScriptRoot', 'ShellId', 'PSVersionTable', 'true', 'false')
$script:ProfileEnvNames = @('USERPROFILE', 'HOME', 'HOMEPATH', 'HOMEDRIVE', 'APPDATA', 'LOCALAPPDATA', 'TEMP', 'TMP', 'ProgramData', 'PATH', 'PSModulePath')
$script:DeleteCommands = @('Remove-Item', 'ri', 'rm', 'rmdir', 'rd', 'del', 'erase')
$script:SkippedDirectories = @('node_modules', '.git', 'bin', 'obj', 'dist', '.next', 'coverage', 'TestResults', 'StrykerOutput')

function Get-VariableNameFromAst($VariableAst) { return [regex]::Replace($VariableAst.VariablePath.UserPath, '^[A-Za-z]+:', '') }

function Get-AssignedVariableAsts($Target) {
    if ($Target -is [System.Management.Automation.Language.VariableExpressionAst]) { return @($Target) }
    if ($Target -is [System.Management.Automation.Language.ConvertExpressionAst]) { return @(Get-AssignedVariableAsts $Target.Child) }
    if ($Target -is [System.Management.Automation.Language.ArrayLiteralAst]) { return @($Target.Elements | ForEach-Object { Get-AssignedVariableAsts $_ }) }
    return @()
}

function Get-PesterRisk {
    <# Returns the flags for a PowerShell source string. Pure: no file access, no execution. #>
    param([Parameter(Mandatory)][AllowEmptyString()][string] $Source)
    $flags = [System.Collections.Generic.HashSet[string]]::new()
    $tokens = $null; $errors = $null
    $tree = [System.Management.Automation.Language.Parser]::ParseInput($Source, [ref]$tokens, [ref]$errors)
    if ($errors -and $errors.Count) { [void]$flags.Add('parse-error'); return @($flags) }

    foreach ($ast in $tree.FindAll({ $args[0] -is [System.Management.Automation.Language.AssignmentStatementAst] }, $true)) {
        foreach ($var in (Get-AssignedVariableAsts $ast.Left)) {
            if ($var.VariablePath.IsDriveQualified -and $var.VariablePath.DriveName -eq 'env') { [void]$flags.Add($(if ((Get-VariableNameFromAst $var) -in $script:ProfileEnvNames) { 'environment-write' } else { 'environment-write-other' })); continue }
            if ((Get-VariableNameFromAst $var) -in $script:AutomaticVariables) { [void]$flags.Add('automatic-variable-assignment') }
        }
    }
    foreach ($ast in $tree.FindAll({ $args[0] -is [System.Management.Automation.Language.VariableExpressionAst] }, $true)) {
        $name = Get-VariableNameFromAst $ast
        if ($ast.VariablePath.IsDriveQualified -and $ast.VariablePath.DriveName -eq 'env') { if ($name -in $script:ProfileEnvNames) { [void]$flags.Add('profile-reference') } }
        elseif ($name -in 'Home', 'Profile') { [void]$flags.Add('profile-reference') }
    }
    foreach ($ast in $tree.FindAll({ $args[0] -is [System.Management.Automation.Language.CommandAst] }, $true)) {
        $name = $ast.GetCommandName()
        if (-not $name) {
            # `& $cmd` / `. $path`: the command is not a literal, so nothing about it can be judged.
            if ($ast.InvocationOperator -in 'Ampersand', 'Dot') { [void]$flags.Add('dynamic-code') }
            continue
        }
        # `Microsoft.PowerShell.Management\Remove-Item` and `C:\Windows\System32\cmd.exe` are the same commands under another spelling.
        $name = [regex]::Replace($name, '^.*\\|\.exe$', '', 'IgnoreCase')
        if ($name -in $script:DeleteCommands) {
            $recursive = @($ast.CommandElements | Where-Object { $_ -is [System.Management.Automation.Language.CommandParameterAst] -and $_.ParameterName -match '^(r|re|rec|recu|recur|recurs|recurse)$' }).Count -gt 0
            if ($recursive) { [void]$flags.Add('recursive-delete') }
            # `Remove-Item @splat` can hide -Recurse inside the hashtable, so it cannot be judged either.
            $splatted = @($ast.CommandElements | Where-Object { $_ -is [System.Management.Automation.Language.VariableExpressionAst] -and $_.Splatted }).Count -gt 0
            if ($splatted) { [void]$flags.Add('dynamic-code') }
        }
        # Native deletes bypass the cmdlet check: `rmdir /s`, `del /s`, and robocopy's destination-emptying /MIR and /PURGE.
        if ($name -eq 'cmd' -and $ast.Extent.Text -match '(?i)\b(rmdir|rd|del|erase)\b[^|&]*\s/s\b') { [void]$flags.Add('recursive-delete') }
        if ($name -eq 'robocopy' -and $ast.Extent.Text -match '(?i)\s/(mir|purge)\b') { [void]$flags.Add('recursive-delete') }
        if ($name -in 'Set-Variable', 'sv', 'New-Variable', 'nv') {
            # The variable name is `-Name <x>` / `-Name:<x>` or the first positional argument.
            $elements = @($ast.CommandElements | Select-Object -Skip 1)
            $named = $elements | Where-Object { $_ -is [System.Management.Automation.Language.CommandParameterAst] -and $_.ParameterName -match '^n(a|am|ame)?$' } | Select-Object -First 1
            $target = if ($named -and $named.Argument) { $named.Argument } elseif ($named) { $elements[[array]::IndexOf($elements, $named) + 1] } elseif ($elements.Count -and $elements[0] -isnot [System.Management.Automation.Language.CommandParameterAst]) { $elements[0] }
            if ($target -is [System.Management.Automation.Language.StringConstantExpressionAst] -and $target.Value -in $script:AutomaticVariables) { [void]$flags.Add('automatic-variable-assignment') }
        }
        if ($name -in 'Invoke-Expression', 'iex', 'Invoke-Command', 'icm') { [void]$flags.Add('dynamic-code') }
        if ($name -in 'pwsh', 'powershell') {
            $viaCommand = @($ast.CommandElements | Where-Object { $_ -is [System.Management.Automation.Language.CommandParameterAst] -and $_.ParameterName -match '^(c|co|com|comm|comma|comman|command)$' }).Count -gt 0
            if ($viaCommand) { [void]$flags.Add('dynamic-code') }
        }
        if ($name -in 'Set-Item', 'si', 'Set-Content' -and ($ast.Extent.Text -match '(?i)\benv:')) { [void]$flags.Add('environment-write') }
        if ($name -in 'New-Item', 'ni', 'mklink') {
            $text = $ast.Extent.Text
            if ($name -eq 'mklink' -or $text -match '(?i)-ItemType\s+[''"]?(Junction|SymbolicLink|HardLink)\b|-Type\s+[''"]?(Junction|SymbolicLink|HardLink)\b') { [void]$flags.Add('junction-or-symlink') }
        }
    }
    foreach ($ast in $tree.FindAll({ $args[0] -is [System.Management.Automation.Language.InvokeMemberExpressionAst] }, $true)) {
        $member = $ast.Member.Extent.Text
        if ($member -in 'Delete', 'DeleteDirectory') {
            $callArguments = if ($ast.Arguments) { @($ast.Arguments) } else { @() }
            # Directory.Delete(path, recursive) is static; DirectoryInfo.Delete(recursive) is an instance call, so the flag moves to argument 0.
            $flagIndex = if ($ast.Static) { 1 } else { 0 }
            $isNonRecursive = $callArguments.Count -le $flagIndex -or ($callArguments[$flagIndex] -is [System.Management.Automation.Language.VariableExpressionAst] -and $callArguments[$flagIndex].VariablePath.UserPath -eq 'false')
            if (-not $isNonRecursive) { [void]$flags.Add('recursive-delete') }
        }
        if ($member -eq 'SetEnvironmentVariable') { [void]$flags.Add('environment-write') }
        if ($member -eq 'Create' -and $ast.Static -and $ast.Expression.Extent.Text -match '(?i)^\[(System\.Management\.Automation\.)?ScriptBlock\]$') { [void]$flags.Add('dynamic-code') }
        if ($member -in 'CreateSymbolicLink', 'CreateHardLink', 'CreateSymbolicLinkA', 'CreateSymbolicLinkW') { [void]$flags.Add('junction-or-symlink') }
    }
    return @($flags | Sort-Object)
}

function Find-PesterSuites {
    <# Pruned walk, never entering a reparse point; returns Path, RelativePath, Kind, Flags and Destructive per file. Reads files, runs nothing.
       Kind is 'Pester' for *.Test(s).ps1. With -IncludeVerifyScripts it also returns verify-*.ps1 as 'VerifyScript': an audit view of the skills corpus
       contract scripts, which run in CI and are not Pester suites, so run-tests must not treat them as ones. #>
    param([Parameter(Mandatory)][string] $Repository, [switch] $IncludeVerifyScripts)
    $root = [IO.Path]::GetFullPath($Repository).TrimEnd('\', '/')
    if (-not [IO.Directory]::Exists($root)) { throw "repository path not found: $Repository" }
    $stack = [System.Collections.Generic.Stack[string]]::new(); $stack.Push($root)
    while ($stack.Count) {
        $dir = $stack.Pop()
        try { $files = @([IO.Directory]::EnumerateFiles($dir)); $subs = @([IO.Directory]::EnumerateDirectories($dir)) }
        catch {
            # A directory that cannot be read hides suites from the report; say so rather than abort the whole walk.
            Write-Warning "Find-PesterSuites could not read '$dir' ($($_.Exception.GetType().Name)); suites beneath it are NOT reported."
            continue
        }
        foreach ($file in $files) {
            $kind = if ($file -match '\.Tests?\.ps1$') { 'Pester' } elseif ($IncludeVerifyScripts -and $file -match '[\\/]verify-[^\\/]*\.ps1$') { 'VerifyScript' } else { continue }
            $flags = Get-PesterRisk -Source ([IO.File]::ReadAllText($file))
            $destructive = @($flags | Where-Object { $_ -in 'recursive-delete', 'automatic-variable-assignment', 'environment-write', 'junction-or-symlink', 'dynamic-code', 'parse-error' }).Count -gt 0
            [pscustomobject]@{ Path = $file; RelativePath = $file.Substring($root.Length + 1); Kind = $kind; Flags = $flags; Destructive = $destructive }
        }
        foreach ($sub in $subs) {
            if ($script:SkippedDirectories -contains [IO.Path]::GetFileName($sub)) { continue }
            if ([IO.File]::GetAttributes($sub) -band [IO.FileAttributes]::ReparsePoint) { continue }
            $stack.Push($sub)
        }
    }
}

if ($Repository) { Find-PesterSuites -Repository $Repository -IncludeVerifyScripts:$IncludeVerifyScripts }
