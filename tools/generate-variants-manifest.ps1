<#
.SYNOPSIS
  Regenerates yuwan-variants.manifest from the lib/<version>/ folders present under the
  mod root.

.PARAMETER ModRoot
  The mod install folder (the one containing YuWanCard.json / YuWanCard.pck).
#>
param(
    [Parameter(Mandatory = $true)][string]$ModRoot
)

$ErrorActionPreference = 'Stop'
$libRoot = Join-Path $ModRoot 'lib'
$variantAssembly = 'YuWanCard.Content.dll'

$variants = @()

if (Test-Path $libRoot) {
    Get-ChildItem -Directory $libRoot | Sort-Object Name | ForEach-Object {
        $verDir = $_
        $markerPath = Join-Path $verDir.FullName 'compat-target.txt'
        $dllPath = Join-Path $verDir.FullName $variantAssembly

        if (-not (Test-Path $markerPath) -or -not (Test-Path $dllPath)) {
            Write-Warning "Skipping incomplete variant folder: $($verDir.FullName)"
            return
        }

        $target = (Get-Content $markerPath -Raw).Trim()
        # Get-FileHash is not available in some PowerShell environments; use .NET directly.
        $sha = [System.Security.Cryptography.SHA256]::Create()
        try {
            $stream = [System.IO.File]::OpenRead($dllPath)
            try {
                $hash = [System.BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-', '').ToLowerInvariant()
            } finally {
                $stream.Dispose()
            }
        } finally {
            $sha.Dispose()
        }

        $script:variants += @{
            compatTarget = $target
            directory    = "lib/$($verDir.Name)"
            assembly     = $variantAssembly
            sha256       = $hash
        }
    }
}

# Hand-build the JSON so a single variant stays an array (PowerShell ConvertTo-Json
# flattens single-element arrays, which would break the loader's deserialization).
$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('{')
[void]$sb.AppendLine('  "schema": 1,')
[void]$sb.AppendLine('  "variants": [')
for ($i = 0; $i -lt $variants.Count; $i++) {
    $v = $variants[$i]
    $comma = if ($i -lt $variants.Count - 1) { ',' } else { '' }
    [void]$sb.AppendLine('    {')
    [void]$sb.AppendLine('      "compatTarget": "' + $v.compatTarget + '",')
    [void]$sb.AppendLine('      "directory": "' + $v.directory + '",')
    [void]$sb.AppendLine('      "assembly": "' + $v.assembly + '",')
    [void]$sb.AppendLine('      "sha256": "' + $v.sha256 + '"')
    [void]$sb.AppendLine('    }' + $comma)
}
[void]$sb.AppendLine('  ]')
[void]$sb.AppendLine('}')

$manifestPath = Join-Path $ModRoot 'yuwan-variants.manifest'
[System.IO.File]::WriteAllText($manifestPath, $sb.ToString(), (New-Object System.Text.UTF8Encoding($false)))
Write-Host "Generated $manifestPath with $($variants.Count) variant(s)."
