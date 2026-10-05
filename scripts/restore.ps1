# Only a NEW database under a DIFFERENT name. Never REPLACE, DROP or ALTER existing databases.
[CmdletBinding()]
param(
    [Parameter(Mandatory)][Security.SecureString] $ConnectionString,
    [Parameter(Mandatory)][string] $ServerBackupFile,
    [Parameter(Mandatory)][string] $DestinationDatabase,
    [string] $ServerDataDirectory,
    [string] $ServerLogDirectory
)
. "$PSScriptRoot/Backup.Common.ps1"
Assert-DatabaseName $DestinationDatabase
$session = Open-BackupConnection $ConnectionString
try {
    $sql = $session.Connection
    if ($DestinationDatabase -eq $session.Database -or $DestinationDatabase -eq 'MecaniCar360') { throw 'Destino protegido: no se restaura sobre la base activa.' }
    $existing = Invoke-BackupSql $sql 'SELECT DB_ID(@name) AS Id' @{ '@name' = $DestinationDatabase }
    if ($existing.Rows[0].Id -isnot [DBNull]) { throw 'La base destino ya existe. Se requiere un nombre nuevo.' }
    if ((Get-ServerFile $sql $ServerBackupFile)[0] -ne 1) { throw 'Backup inexistente o inaccesible para SQL Server.' }
    $parameters = @{ '@path' = $ServerBackupFile }
    $header = Invoke-BackupSql $sql 'RESTORE HEADERONLY FROM DISK=@path' $parameters
    if ($header.Rows.Count -ne 1 -or $header.Rows[0].BackupType -ne 1 -or !$header.Rows[0].HasBackupChecksums) { throw 'Se requiere un archivo con un unico backup FULL con CHECKSUM.' }
    if ($DestinationDatabase -eq $header.Rows[0].DatabaseName) { throw 'El destino debe ser distinto de la base fuente del backup.' }
    $null = Invoke-BackupSql $sql 'RESTORE VERIFYONLY FROM DISK=@path WITH CHECKSUM, STOP_ON_ERROR' $parameters
    $defaults = Invoke-BackupSql $sql "SELECT CONVERT(nvarchar(4000), SERVERPROPERTY('InstanceDefaultDataPath')) AS DataPath, CONVERT(nvarchar(4000), SERVERPROPERTY('InstanceDefaultLogPath')) AS LogPath"
    if (!$ServerDataDirectory) { $ServerDataDirectory = [string] $defaults.Rows[0].DataPath }
    if (!$ServerLogDirectory) { $ServerLogDirectory = [string] $defaults.Rows[0].LogPath }
    Confirm-ServerDirectory $sql $ServerDataDirectory
    Confirm-ServerDirectory $sql $ServerLogDirectory
    $files = Invoke-BackupSql $sql 'RESTORE FILELISTONLY FROM DISK=@path' $parameters
    $moves = @(); $suffix = [Guid]::NewGuid().ToString('N')
    foreach ($file in $files.Rows) {
        if ($file.Type -notin @('D','L')) { throw 'Este script admite archivos SQL de datos/log, no FILESTREAM u otros contenedores.' }
        $dir = $ServerDataDirectory; $extension = '.mdf'
        if ($file.Type -eq 'L') { $dir = $ServerLogDirectory; $extension = '.ldf' }
        elseif ($file.FileId -ne 1) { $extension = '.ndf' }
        $physical = $dir.TrimEnd('\') + '\' + $DestinationDatabase + '_' + $suffix + '_' + $file.FileId + $extension
        if ((Get-ServerFile $sql $physical)[0] -eq 1) { throw 'Colision de archivo destino.' }
        $moves += 'MOVE ' + (ConvertTo-SqlLiteral $file.LogicalName) + ' TO ' + (ConvertTo-SqlLiteral $physical)
    }
    if (!$moves.Count) { throw 'El backup no contiene archivos.' }
    # Recheck immediately before RESTORE; absent REPLACE is also a server-side safeguard.
    $restore = "IF DB_ID(@name) IS NOT NULL THROW 51000, 'Destino existente', 1; RESTORE DATABASE [$DestinationDatabase] FROM DISK=@path WITH RECOVERY, CHECKSUM, STOP_ON_ERROR, " + ($moves -join ', ')
    $parameters['@name'] = $DestinationDatabase
    $null = Invoke-BackupSql $sql $restore $parameters
    $state = Invoke-BackupSql $sql 'SELECT state_desc FROM sys.databases WHERE name=@name' @{ '@name' = $DestinationDatabase }
    if ($state.Rows[0].state_desc -ne 'ONLINE') { throw 'Restore finalizado pero base no ONLINE; requiere revision tecnica.' }
    [pscustomobject]@{ Database=$DestinationDatabase; State='ONLINE'; ServerFile=$ServerBackupFile; CompletedUtc=[DateTime]::UtcNow }
} finally { $session.Connection.Dispose() }
