$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot
Push-Location $projectRoot
try {
    $sources = @(rg --files Assets -g '*.cs' -g '*.asmdef')
    foreach ($path in $sources) {
        $copy = Join-Path $PSScriptRoot ('UnityProject/' + $path)
        if ((Get-FileHash -LiteralPath $path).Hash -ne (Get-FileHash -LiteralPath $copy).Hash) { throw "Source differs: $path" }
    }
    $guids = @{}
    foreach ($path in @(rg --files Assets -g '*.meta')) {
        $match = [regex]::Match([IO.File]::ReadAllText((Join-Path $projectRoot $path)), '(?m)^guid: ([a-f0-9]+)')
        if ($match.Success) {
            $guid = $match.Groups[1].Value
            if ($guids.ContainsKey($guid)) { throw "Duplicate GUID in $path" }
            $guids[$guid] = $path
        }
    }
    $references = 0
    $knownGuids = New-Object 'System.Collections.Generic.HashSet[string]'
    foreach ($guid in $guids.Keys) { [void]$knownGuids.Add($guid) }
    foreach ($meta in Get-ChildItem (Join-Path $PSScriptRoot 'UnityProject/Library/PackageCache') -Recurse -Filter '*.meta' -File) {
        $match = [regex]::Match([IO.File]::ReadAllText($meta.FullName), '(?m)^guid: ([a-f0-9]{32})')
        if ($match.Success) { [void]$knownGuids.Add($match.Groups[1].Value) }
    }
    foreach ($file in Get-ChildItem Assets/EcoCity/Generated -Recurse -File) {
        $relative = $file.FullName.Substring($projectRoot.Length + 1)
        $copy = Join-Path $PSScriptRoot ('UnityProject/' + $relative)
        # The original prototype scene is deliberately preserved. Unity may add default
        # serialized fields while inspecting its validation copy; never copy those edits back.
        if ($relative -ne 'Assets\EcoCity\Generated\Submission\Prototype\EcoCityPrototype.unity' -and
            (Get-FileHash $file.FullName).Hash -ne (Get-FileHash $copy).Hash) { throw "Generated file differs: $relative" }
        foreach ($match in [regex]::Matches([IO.File]::ReadAllText($file.FullName), 'guid: ([a-f0-9]{32})')) {
            $guid = $match.Groups[1].Value
            if ($guid.StartsWith('0000000000000000')) { continue }
            if (!$knownGuids.Contains($guid)) { throw "Unresolved GUID in $relative : $guid" }
            $references++
        }
    }
    Write-Output "Verified $($sources.Count) source/asmdef copies, $($guids.Count) unique asset GUIDs, $references generated GUID references."
} finally { Pop-Location }
