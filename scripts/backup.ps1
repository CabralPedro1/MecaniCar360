# FULL SQL Server. Paths refer to the SQL Server host, not the script host.
[CmdletBinding()]
param(
    [Parameter(Mandatory)][Security.SecureString] $ConnectionString,
    [string] $ServerBackupDirectory,
    [ValidatePattern('\A[A-Za-z0-9_-]{1,80}\z')][string] $SetId = ([DateTime]::UtcNow.ToString('yyyyMMdd_HHmmss_fff') + '_' + [Guid]::NewGuid().ToString('N')),
    [switch] $CreateLocalDirectory
)
. "$PSScriptRoot/Backup.Common.ps1"
$session = Open-BackupConnection $ConnectionString
try {
    $sql = $session.Connection
    if (!$ServerBackupDirectory) {
        $settings = Invoke-BackupSql $sql "SELECT CONVERT(nvarchar(4000), SERVERPROPERTY('InstanceDefaultBackupPath')) AS Path"
        $ServerBackupDirectory = [string] $settings.Rows[0].Path
    }
    Confirm-ServerDirectory $sql $ServerBackupDirectory -CreateLocalDirectory:$CreateLocalDirectory
    $path = $ServerBackupDirectory.TrimEnd('\') + '\' + $session.Database + '_' + $SetId + '.bak'
    if ((Get-ServerFile $sql $path)[0] -eq 1) { throw 'El archivo de backup ya existe. Use otro SetId; no se sobrescribe.' }
    $database = $session.Database
    $null = Invoke-BackupSql $sql "BACKUP DATABASE [$database] TO DISK=@path WITH COPY_ONLY, CHECKSUM, STOP_ON_ERROR" @{ '@path' = $path }
    $null = Invoke-BackupSql $sql 'RESTORE VERIFYONLY FROM DISK=@path WITH CHECKSUM, STOP_ON_ERROR' @{ '@path' = $path }
    [pscustomobject]@{ Database=$database; SetId=$SetId; ServerFile=$path; CompletedUtc=[DateTime]::UtcNow; VerifyOnly='PASS' }
} finally { $session.Connection.Dispose() }
