[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $SourceDirectory,
    [Parameter(Mandatory)][string] $BackupDirectory,
    [Parameter(Mandatory)][ValidatePattern('\A[A-Za-z0-9_-]{1,80}\z')][string] $SetId
)
. "$PSScriptRoot/Backup.Common.ps1"
Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.IO.Compression
$source = [IO.Path]::GetFullPath($SourceDirectory).TrimEnd('\')
$destination = [IO.Path]::GetFullPath($BackupDirectory).TrimEnd('\')
Assert-NoReparsePoint $source
Assert-NoReparsePoint $destination
if (!(Test-Path -LiteralPath $source -PathType Container)) { throw 'Directorio de evidencias inexistente. No se declara un backup vacio por error.' }
if ($destination -eq $source -or $destination.StartsWith($source + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'El backup no puede estar dentro del origen.' }
$files = New-Object 'System.Collections.Generic.List[string]'
$pending = New-Object 'System.Collections.Generic.Stack[string]'
$pending.Push($source)
while ($pending.Count) {
    $directory = $pending.Pop()
    foreach ($item in Get-ChildItem -LiteralPath $directory -Force) {
        if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'No se respaldan enlaces/junctions.' }
        if ($item.PSIsContainer) { $pending.Push($item.FullName) } else { $files.Add($item.FullName) }
    }
}
[void] [IO.Directory]::CreateDirectory($destination)
$path = Join-Path $destination ('evidencias_' + $SetId + '.zip')
$stream = $null; $zip = $null; $created = $false; $complete = $false
try {
    $stream = [IO.File]::Open($path, [IO.FileMode]::CreateNew, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    $created = $true
    $zip = New-Object IO.Compression.ZipArchive($stream, [IO.Compression.ZipArchiveMode]::Create, $true)
    foreach ($file in $files) {
        Assert-NoReparsePoint $file
        $relative = $file.Substring($source.Length + 1).Replace('\','/')
        [void] [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $file, $relative, [IO.Compression.CompressionLevel]::Optimal)
    }
    $zip.Dispose(); $zip = $null
    $stream.Dispose(); $stream = $null
    $complete = $true
} finally {
    if ($zip) { $zip.Dispose() }
    if ($stream) { $stream.Dispose() }
    if ($created -and !$complete) { Remove-Item -LiteralPath $path -Force }
}
[pscustomobject]@{ SetId=$SetId; File=$path; Files=$files.Count; Bytes=(Get-Item -LiteralPath $path).Length; Sha256=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash }
