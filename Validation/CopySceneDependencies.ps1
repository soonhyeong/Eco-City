$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot
$destinationRoot = Join-Path $PSScriptRoot 'UnityProject'
$assetMap = @{}
Get-ChildItem (Join-Path $projectRoot 'Assets') -Recurse -Filter '*.meta' -File | ForEach-Object {
    $match = [regex]::Match([IO.File]::ReadAllText($_.FullName), '(?m)^guid: ([a-f0-9]{32})')
    if ($match.Success) { $assetMap[$match.Groups[1].Value] = $_.FullName.Substring(0, $_.FullName.Length - 5) }
}
$queue = New-Object 'System.Collections.Generic.Queue[string]'
$visited = New-Object 'System.Collections.Generic.HashSet[string]'
$queue.Enqueue((Join-Path $projectRoot 'Assets/EcoCity/Generated/Submission/Prototype/EcoCityPrototype.unity'))
$alpha = Join-Path $projectRoot 'Assets/EcoCity/Generated/Submission/Alpha/EcoCityAlpha.unity'
if (Test-Path -LiteralPath $alpha) { $queue.Enqueue($alpha) }
$submission = Join-Path $projectRoot 'Assets/1. Scene/EcoCitySubmission.unity'
if (Test-Path -LiteralPath $submission) { $queue.Enqueue($submission) }
# Include the newer title/character scenes, not only the original three scenes.
$scenesFolder = Join-Path $projectRoot 'Assets/Scenes'
if (Test-Path -LiteralPath $scenesFolder) {
    Get-ChildItem -LiteralPath $scenesFolder -Filter '*.unity' -File | ForEach-Object { $queue.Enqueue($_.FullName) }
}
$animation = Join-Path $projectRoot 'Assets/Player/Common/DEMO/Animator/StarterAssetsThirdPerson.controller'
if (Test-Path -LiteralPath $animation) { $queue.Enqueue($animation) }
$bytes = 0L
while ($queue.Count -gt 0) {
    $path = $queue.Dequeue()
    if (!$visited.Add($path) -or !(Test-Path -LiteralPath $path -PathType Leaf)) { continue }
    $relative = $path.Substring($projectRoot.Length + 1)
    $destination = Join-Path $destinationRoot $relative
    New-Item -ItemType Directory (Split-Path $destination) -Force | Out-Null
    Copy-Item -LiteralPath $path -Destination $destination -Force
    $bytes += (Get-Item -LiteralPath $path).Length
    $texts = @()
    if ([IO.Path]::GetExtension($path) -in @('.unity','.prefab','.asset','.mat','.controller','.overrideController','.anim','.asmdef')) {
        $texts += [IO.File]::ReadAllText($path)
    }
    if (Test-Path -LiteralPath ($path + '.meta')) {
        Copy-Item -LiteralPath ($path + '.meta') -Destination ($destination + '.meta') -Force
        $texts += [IO.File]::ReadAllText($path + '.meta')
    }
    foreach ($body in $texts) {
        foreach ($match in [regex]::Matches($body, 'guid: ([a-f0-9]{32})')) {
            $guid = $match.Groups[1].Value
            if ($assetMap.ContainsKey($guid)) { $queue.Enqueue($assetMap[$guid]) }
        }
    }
}
Write-Output "Copied scene dependency closure: $($visited.Count) assets, $bytes bytes."
# The verifier also compares unused generated assets and folder metadata. Preserve
# those files instead of letting the validation editor assign fresh folder GUIDs.
$generated = Join-Path $projectRoot 'Assets/EcoCity/Generated'
Copy-Item -LiteralPath $generated -Destination (Join-Path $destinationRoot 'Assets/EcoCity') -Recurse -Force
Copy-Item -LiteralPath ($generated + '.meta') -Destination (Join-Path $destinationRoot 'Assets/EcoCity/Generated.meta') -Force
