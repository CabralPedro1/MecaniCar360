# Shared helpers. Windows PowerShell 5.1; no web-process SQL privileges required.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Open-BackupConnection([Security.SecureString] $ConnectionString) {
    $ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($ConnectionString)
    try {
        $builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
        $builder.set_ConnectionString([Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr))
    } finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr) }
    Assert-DatabaseName $builder.InitialCatalog
    $database = $builder.InitialCatalog
    $builder.set_InitialCatalog('master')
    $builder.set_Pooling($false)
    $connection = New-Object System.Data.SqlClient.SqlConnection $builder.ConnectionString
    $builder.Clear()
    try { $connection.Open() } catch { $connection.Dispose(); throw 'No se pudo conectar a SQL Server. Revise instancia, identidad y permisos; no se imprime la cadena.' }
    return @{ Connection = $connection; Database = $database }
}

function Assert-DatabaseName([string] $Name) {
    if ($Name -notmatch '\A[A-Za-z][A-Za-z0-9_]{0,119}\z' -or $Name -in @('master','model','msdb','tempdb')) {
        throw 'Nombre de base invalido: use letras, numeros y guion bajo; no bases de sistema.'
    }
}

function Invoke-BackupSql($Connection, [string] $Sql, [hashtable] $Parameters = @{}) {
    $command = $Connection.CreateCommand()
    $command.CommandText = $Sql
    $command.CommandTimeout = 1800
    foreach ($key in $Parameters.Keys) { [void] $command.Parameters.AddWithValue($key, $Parameters[$key]) }
    try {
        $reader = $command.ExecuteReader()
        try { $table = New-Object System.Data.DataTable; $table.Load($reader); return ,$table }
        finally { $reader.Dispose() }
    } catch [System.Data.SqlClient.SqlException] {
        throw "SQL Server fallo (numero $($_.Exception.Number)). Revise permisos, rutas y espacio en el servidor; consulte el registro SQL protegido."
    } finally { $command.Dispose() }
}

function Assert-ServerPath([string] $Path) {
    if ($Path -notmatch '\A(?:[A-Za-z]:\\|\\\\[^\\]+\\[^\\]+\\)' -or $Path -match '[\x00-\x1f]') {
        throw 'Se requiere una ruta absoluta Windows del servidor SQL, no del navegador/cliente.'
    }
}

function Get-ServerFile($Connection, [string] $Path) {
    Assert-ServerPath $Path
    $result = Invoke-BackupSql $Connection 'EXEC master.dbo.xp_fileexist @path' @{ '@path' = $Path }
    return $result.Rows[0]
}

function Confirm-ServerDirectory($Connection, [string] $Path, [switch] $CreateLocalDirectory) {
    Assert-ServerPath $Path
    if ($CreateLocalDirectory) {
        $machine = Invoke-BackupSql $Connection "SELECT CONVERT(nvarchar(128), SERVERPROPERTY('MachineName')) AS Machine"
        if ($machine.Rows[0].Machine -ne $env:COMPUTERNAME) { throw 'No se crea una ruta local para un SQL Server remoto.' }
        [void] [IO.Directory]::CreateDirectory($Path)
    }
    if ((Get-ServerFile $Connection $Path)[1] -ne 1) { throw 'Directorio inexistente o inaccesible para SQL Server. Creelo en el servidor y configure su ACL.' }
}

function ConvertTo-SqlLiteral([string] $Value) { return "N'" + $Value.Replace("'", "''") + "'" }

function Assert-NoReparsePoint([string] $Path) {
    $current = [IO.Path]::GetFullPath($Path)
    while ($current) {
        if (Test-Path -LiteralPath $current) {
            if ((Get-Item -LiteralPath $current -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'No se permiten enlaces/junctions en el respaldo de archivos.' }
        }
        $current = [IO.Path]::GetDirectoryName($current)
    }
}
