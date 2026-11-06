$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot
$utf8 = New-Object System.Text.UTF8Encoding($false)
foreach ($relative in @('Assets/Player/Common/Scrips/G2CharacterGuildBook.cs','Assets/Player/Common/Scrips/ShaderSwapper.cs')) {
    $path = Join-Path $projectRoot $relative
    $body = [IO.File]::ReadAllText($path)
    if (!$body.StartsWith('#if UNITY_EDITOR')) { [IO.File]::WriteAllText($path, "#if UNITY_EDITOR`n" + $body + "`n#endif`n", $utf8) }
}
$copyRoot = Join-Path $PSScriptRoot 'UnityProject'
New-Item -ItemType Directory (Join-Path $copyRoot 'Assets') -Force | Out-Null
Copy-Item (Join-Path $projectRoot 'Packages') -Destination $copyRoot -Recurse -Force
Copy-Item (Join-Path $projectRoot 'ProjectSettings') -Destination $copyRoot -Recurse -Force
if (Test-Path (Join-Path $projectRoot 'Submission')) { Copy-Item (Join-Path $projectRoot 'Submission') -Destination $copyRoot -Recurse -Force }
Push-Location $projectRoot
try {
    $paths = rg --files Assets -g '*.cs' -g '*.asmdef' -g '*.cs.meta' -g '*.asmdef.meta'
    foreach ($relative in $paths) {
        $destination = Join-Path $copyRoot $relative
        New-Item -ItemType Directory (Split-Path $destination) -Force | Out-Null
        Copy-Item -LiteralPath $relative -Destination $destination -Force
    }
} finally { Pop-Location }
Write-Output "Prepared $copyRoot"
