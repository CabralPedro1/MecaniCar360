[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $BackupFile,
    [Parameter(Mandatory)][string] $DestinationDirectory
)
. "$PSScriptRoot/Backup.Common.ps1"
Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.IO.Compression
$destination = [IO.Path]::GetFullPath($DestinationDirectory).TrimEnd('\')
Assert-NoReparsePoint $BackupFile
Assert-NoReparsePoint $destination
if (Test-Path -LiteralPath $destination) { throw 'El destino debe ser un directorio NUEVO; nunca se sobrescriben evidencias existentes.' }
if (!(Test-Path -LiteralPath $BackupFile -PathType Leaf)) { throw 'ZIP inexistente.' }
$zip = [IO.Compression.ZipFile]::OpenRead([IO.Path]::GetFullPath($BackupFile))
try {
    $names = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    # Validate ALL entries before creating destination. Reject traversal, ADS and aliases.
    foreach ($entry in $zip.Entries) {
        $relative = $entry.FullName.Replace('/', '\')
        if ($relative -match '\A\\|:|[\x00-\x1f]|[<>"|?*]' -or $relative.EndsWith('\')) { throw 'Entrada ZIP no admitida.' }
        foreach ($part in $relative.Split('\')) {
            if (!$part -or $part -in @('.','..') -or $part -match '[. ]\z|\A(?:CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\.|\z)') { throw 'Ruta ZIP no segura.' }
        }
        $target = [IO.Path]::GetFullPath((Join-Path $destination $relative))
        if (!$target.StartsWith($destination + '\', [StringComparison]::OrdinalIgnoreCase) -or !$names.Add($target)) { throw 'Ruta ZIP duplicada o fuera del destino.' }
    }
    [void] [IO.Directory]::CreateDirectory($destination)
    foreach ($entry in $zip.Entries) {
        $target = Join-Path $destination $entry.FullName.Replace('/', '\')
        Assert-NoReparsePoint $target
        [void] [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($target))
        [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $target, $false)
    }
    [pscustomobject]@{ Directory=$destination; Files=$zip.Entries.Count; CompletedUtc=[DateTime]::UtcNow }
} finally { $zip.Dispose() }
# On extraction failure a partial NEW directory is retained for inspection; never used as live storage.
