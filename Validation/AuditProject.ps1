$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
$guidMap = @{}
$duplicates = @()
$orphan = @()
foreach ($meta in Get-ChildItem (Join-Path $root 'Assets') -Recurse -File -Filter '*.meta') {
    $match = [regex]::Match([IO.File]::ReadAllText($meta.FullName), '(?m)^guid: ([a-f0-9]{32})')
    if ($match.Success) {
        $guid = $match.Groups[1].Value
        if ($guidMap.ContainsKey($guid)) { $duplicates += $meta.FullName }
        $guidMap[$guid] = $meta.FullName.Substring(0, $meta.FullName.Length - 5)
    }
    if (!(Test-Path -LiteralPath $meta.FullName.Substring(0, $meta.FullName.Length - 5))) { $orphan += $meta.FullName }
}
$known = New-Object 'System.Collections.Generic.HashSet[string]'
foreach ($guid in $guidMap.Keys) { [void]$known.Add($guid) }
foreach ($cache in @('Library/PackageCache','Validation/UnityProject/Library/PackageCache')) {
    $folder = Join-Path $root $cache
    if (!(Test-Path -LiteralPath $folder)) { continue }
    foreach ($meta in Get-ChildItem $folder -Recurse -File -Filter '*.meta') {
        $match = [regex]::Match([IO.File]::ReadAllText($meta.FullName), '(?m)^guid: ([a-f0-9]{32})')
        if ($match.Success) { [void]$known.Add($match.Groups[1].Value) }
    }
}
$broken = @{}
$references = 0
$texts = Get-ChildItem (Join-Path $root 'Assets/EcoCity'),(Join-Path $root 'Assets/Scenes'),(Join-Path $root 'Assets/1. Scene') -Recurse -File |
    Where-Object { $_.Extension -in '.unity','.prefab','.asset','.mat','.controller','.overrideController','.anim' }
foreach ($file in $texts) {
    foreach ($match in [regex]::Matches([IO.File]::ReadAllText($file.FullName), 'guid: ([a-f0-9]{32})')) {
        $guid = $match.Groups[1].Value
        if ($guid.StartsWith('0000000000000000')) { continue }
        $references++
        if (!$known.Contains($guid)) { $broken[$guid] = $file.FullName.Substring($root.Length + 1) }
    }
}
Write-Output "ASSET GUIDS: $($guidMap.Count); duplicate GUIDs: $($duplicates.Count); orphan meta: $($orphan.Count)"
Write-Output "CONTENT GUID REFERENCES: $references; unresolved unique GUIDs: $($broken.Count)"
foreach ($guid in $broken.Keys) { Write-Output "UNRESOLVED: $guid in $($broken[$guid])" }
foreach ($file in $duplicates) { Write-Output "DUPLICATE: $file" }
foreach ($file in $orphan) { Write-Output "ORPHAN META: $file" }
foreach ($builder in @('PrototypeSceneBuilder.cs','AlphaSceneBuilder.cs','SubmissionSceneBuilder.cs','SubmissionTitleBuilder.cs','SubmissionCharacterBuilder.cs')) {
    $body = [IO.File]::ReadAllText((Join-Path $root ('Assets/EcoCity/Editor/' + $builder)))
    $match = [regex]::Match($body, '(?:ScenePath|existing) = "([^"]+\.unity)"')
    if ($match.Success) { Write-Output "SCENE PATH: $builder => $($match.Groups[1].Value); exists=$(Test-Path -LiteralPath (Join-Path $root $match.Groups[1].Value))" }
}
foreach ($scene in @('Prototype/EcoCityPrototype.unity','Alpha/EcoCityAlpha.unity')) {
    $file = Join-Path $root ('Assets/EcoCity/Generated/Submission/' + $scene)
    Write-Output "PRESERVED SCENE SHA256: $scene $((Get-FileHash -LiteralPath $file).Hash)"
}
foreach ($file in @('Validation/GenerateKidSounds.py','Validation/GenerateKidVoice.ps1')) {
    Write-Output "DOCUMENTED GENERATOR: $file; exists=$(Test-Path -LiteralPath (Join-Path $root $file))"
}
