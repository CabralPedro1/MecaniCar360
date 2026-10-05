# Resguardo y restauración de MecaniCar360 (T07)

## Objetivo, alcance y responsable

Recuperar SQL Server y los archivos privados de Evidencias mediante procedimientos
reproducibles. Responsable: administrador del sistema / responsable técnico,
utilizando una identidad Windows técnica. El proceso web no recibe permisos nuevos.
No hay Controller, botón web ni scheduler interno de backup.

SQL incluye personas, usuarios, permisos, vehículos, turnos, ingresos, órdenes,
diagnósticos, presupuestos, facturas, pagos, garantías, auditoría, notificaciones
y demás tablas, índices, constraints y datos. El respaldo SQL **no contiene los
archivos** de `App_Data/Evidencias` ni código, configuración externa, User Secrets,
certificados o claves de Data Protection. El código se recupera desde Git; esos
materiales externos deben custodiarse separadamente, nunca dentro del repositorio.

Estado inspeccionado el 2026-10-05: SQL Server Express 2022, versión 16.0.1200.5,
instancia `BOOK-JKTL5UI49E\CABRALPEDRO`, base `MecaniCar360`, autenticación integrada.
La aplicación usa `ConnectionStrings:DefaultConnection` mediante la configuración
estándar de ASP.NET Core; User Secrets/variables de entorno pueden sobrescribirla.
Los scripts NO arrancan la aplicación, ejecutan seed ni migraciones y NO leen
automáticamente los secretos del proyecto: reciben una cadena como `SecureString`.

## Tipos evaluados y política elegida

| Tipo | Contenido | Ventaja | Costo / uso adecuado |
|---|---|---|---|
| FULL | Base completa y log necesario para consistencia del propio backup | Restauración independiente | Mayor espacio/tiempo; apropiado al volumen académico actual |
| DIFFERENTIAL | Cambios desde el FULL base convencional | Copia diaria menor | Necesita su FULL base; útil con bases grandes |
| TRANSACTION LOG | Registros desde el backup de log anterior | Recuperación a un momento preciso | Requiere FULL/BULK_LOGGED y cadena de logs cuidada; útil con RPO menor |

Se implementa FULL diario `COPY_ONLY, CHECKSUM, STOP_ON_ERROR`. COPY_ONLY permite
restauración completa independiente sin sustituir la base diferencial de una
política ajena. No se implementan diferenciales ni backups de log. No se solicita
compresión SQL, cuya disponibilidad depende de la edición. El script no cambia el
modelo de recuperación: si la base usa FULL/BULK_LOGGED, el responsable debe mantener
una política de log separada para evitar crecimiento; FULL COPY_ONLY no la sustituye.

Frecuencia: todos los días a las 02:00, preferentemente durante una ventana sin
escrituras, y antes de cambios estructurales importantes. RPO objetivo: hasta 24 h
desde el último conjunto completo y verificado. No hay RTO garantizado: medirlo
periódicamente según tamaño, servidor y disponibilidad del soporte.

Retención **por conjuntos SQL + Evidencias**, propuesta proporcional al proyecto:

- Diarios: últimos 7 días.
- Semanales: un conjunto del domingo durante 4 semanas.
- Mensuales: último conjunto del mes durante 6 meses.

Las categorías pueden compartir un conjunto. Revisión mensual de capacidad; no
borrar el único conjunto verificado ni retirar uno antes de tener su copia externa.
La selección/depuración corresponde al responsable: **los scripts no borran backups
por antigüedad**, ni se creó una tarea programada en el equipo.

## Ubicación, soporte y permisos

SQL escribe el `.bak` desde **el host y la cuenta del servicio SQL**, no desde el
equipo que ejecuta PowerShell. `ServerBackupDirectory`, `ServerBackupFile` y rutas
MOVE son rutas absolutas del servidor. Una unidad mapeada del operador puede no
existir para el servicio. Usar directorio local del servidor o UNC con permisos
adecuados de recurso compartido y NTFS.

El default detectado es `C:\SQLData\Backup`. En este equipo la cuenta del operador
no puede crear subcarpetas allí: el administrador debe preparar un directorio con
ACL restringida. `-CreateLocalDirectory` sólo crea carpetas si SQL declara el mismo
MachineName que el equipo actual y la cuenta dispone de permiso; nunca crea una
carpeta local suponiendo que pertenece a un servidor remoto. La existencia se
verifica mediante `xp_fileexist` en SQL. No se usa `xp_cmdshell`.

SQL necesita permiso BACKUP DATABASE; los metadatos RESTORE/VERIFYONLY y la creación
de bases de prueba requieren los permisos técnicos de SQL Server correspondientes.
No conceder sysadmin al proceso web. Una cuenta técnica debe poder leer el archivo
backup y SQL debe poder escribir en los directorios de datos/log para MOVE. Los
scripts reportan el número de error SQL, sin cadena de conexión; para diagnóstico
detallado, consultar el registro SQL bajo acceso restringido.

Flujo de soporte: copia local operativa controlada → copia secundaria a disco
externo/NAS/almacenamiento remoto aprobado. **No conservar la única copia en el disco
de la base.** No se integra ningún servicio cloud. Aplicar control de acceso y
cifrado del soporte (por ejemplo BitLocker administrado), también en la copia externa.
SQL y ZIP contienen información sensible; ZIP no implica cifrado. No colocarlos en
`wwwroot`, Git ni carpetas públicas. Se excluyen `.bak`, `Backups/`, `backups/`,
`evidencias_*.zip` y `RestoreTemp/` del versionado.

## Procedimiento de backup

Requisitos: Windows PowerShell 5.1, .NET Framework/SqlClient del sistema, SQL Server
Windows y espacio disponible. No se necesita `sqlcmd` ni módulo PowerShell SqlServer.
Ejecutar desde la raíz del proyecto con una política de ejecución autorizada. No
cambiar políticas globales por este procedimiento; en equipos restringidos usar
scripts firmados/aprobados. Para una ejecución técnica revisada puede emplearse
`powershell.exe -NoProfile -ExecutionPolicy Bypass`, limitado a ese proceso.

1. Confirmar la instancia/base efectiva, el directorio configurado en
   `Evidencias:Directorio` y la identidad técnica. No imprimir configuración sensible.
2. Preparar destinos privados. Detener/coordinar las escrituras durante ambas copias
   (mantenimiento del sitio por el responsable; los scripts no detienen procesos).
3. Ejecutar ambos scripts con el **mismo SetId**. Ejemplo sin credenciales SQL:

```powershell
$ErrorActionPreference = 'Stop'
$cs = ConvertTo-SecureString 'Server=BOOK-JKTL5UI49E\CABRALPEDRO;Database=MecaniCar360;Integrated Security=True;Encrypt=True;TrustServerCertificate=True' -AsPlainText -Force
# TrustServerCertificate corresponde al entorno local; con certificado válido usar False.
# Para SQL Authentication: $cs = Read-Host 'Cadena SQL' -AsSecureString
# No escribir contraseñas literales en comandos, historial, scripts ni logs.
$id = [DateTime]::UtcNow.ToString('yyyyMMdd_HHmmss_fff') + '_' + [Guid]::NewGuid().ToString('N')
$sql = .\scripts\backup.ps1 -ConnectionString $cs -ServerBackupDirectory 'D:\Backups\MecaniCar360' -SetId $id
$files = .\scripts\backup-evidencias.ps1 -SourceDirectory 'C:\Aplicaciones\MecaniCar360\App_Data\Evidencias' -BackupDirectory 'D:\Backups\MecaniCar360' -SetId $id
$sql
$files
```

Reemplazar las rutas por las preparadas, no por rutas supuestas. En un servidor SQL
remoto el BAK y el ZIP pueden quedar en hosts diferentes: copiar después ambos al
soporte secundario bajo el mismo identificador. No publicar la cadena; SecureString
evita mostrarla pero SqlClient necesariamente la materializa en memoria al conectar.

El SQL genera `MecaniCar360_<SetId>.bak` y ejecuta `RESTORE VERIFYONLY WITH CHECKSUM,
STOP_ON_ERROR`. El ZIP genera `evidencias_<SetId>.zip`, informa cantidad de archivos,
tamaño y SHA-256. Un directorio inexistente de evidencias falla explícitamente; uno
existente vacío produce ZIP vacío válido. Se preservan rutas de archivos anidados,
no carpetas vacías ni ACL NTFS. Se rechazan reparse points/junctions. No se sobrescribe
un backup existente. Ante error SQL puede quedar un BAK incompleto: no catalogarlo
como válido y revisar/eliminar únicamente ese archivo bajo control técnico.

4. Registrar fecha UTC, SetId, rutas, resultado VERIFYONLY, cantidad/bytes/SHA-256
   del ZIP y hash SHA-256 del BAK desde el host que pueda leerlo. Guardar el registro
   junto al conjunto, en almacenamiento protegido; no registrar secretos.
5. Copiar ambos y su registro al soporte secundario; comparar hashes de origen y
   copia. Reanudar escrituras y controlar el resultado de la tarea.

SQL y filesystem **no son una transacción distribuida**. Copiar SQL seguido
inmediatamente de Evidencias reduce la diferencia, pero sólo una ventana sin
escrituras evita altas/bajas concurrentes entre las dos copias. Un backup de archivos
con actividad puede fallar o no corresponder al mismo instante: repetir el conjunto,
no declarar recuperación completa. No hay snapshots distribuidos.

## Restore SQL seguro y verificación

`restore.ps1` sólo admite un archivo con un único FULL y CHECKSUM. Rechaza base de
sistema, nombre inválido, base activa de la cadena, nombre `MecaniCar360`, nombre
fuente del encabezado y **cualquier base destino existente**. No hay opción Force,
REPLACE, DROP, SINGLE_USER ni cierre de sesiones. Obtiene FILELISTONLY y genera MOVE
para cada archivo de datos/log a nombres físicos únicos. No admite FILESTREAM.

```powershell
$restored = .\scripts\restore.ps1 -ConnectionString $cs `
  -ServerBackupFile 'D:\Backups\MecaniCar360\MecaniCar360_<SetId>.bak' `
  -DestinationDatabase 'MecaniCar360_Recovery_20261005'
$restored
# Si hace falta: -ServerDataDirectory 'D:\SQLData' -ServerLogDirectory 'E:\SQLLog'
```

Los directorios MOVE deben existir y ser accesibles al servicio SQL. Si falta el
archivo o falla la operación, se informa error; **no se intenta arreglar/eliminar
automáticamente una base parcial**. Inspeccionarla con el responsable, sin usarla
como base activa. No ejecutar estos comandos contra una instancia no autorizada.

Después de ONLINE, conectarse a la base restaurada y comprobar al menos:

```sql
SELECT DB_NAME() AS BaseActual;
SELECT COUNT(*) AS Personas FROM dbo.Personas;
SELECT COUNT(*) AS Ordenes FROM dbo.OrdenesTrabajo;
SELECT COUNT(*) AS Evidencias FROM dbo.Evidencias;
SELECT COUNT(*) AS Facturas FROM dbo.Facturas;
SELECT COUNT(*) AS Pagos FROM dbo.Pagos;
SELECT MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;
-- Comparar con el registro de origen tomado durante la ventana sin escrituras.
-- Verificar además registros conocidos sin imprimir hashes/secretos.
DBCC CHECKDB WITH NO_INFOMSGS;
```

VERIFYONLY comprueba el backup, **no garantiza recuperación completa ni consistencia
lógica de toda la aplicación**. La evidencia definitiva es restore real + validación
de datos/archivos y, cuando corresponda, comprobación funcional en entorno aislado.
No arrancar la aplicación restaurada con SMTP activo ni configuración de producción:
el arranque normal ejecuta migraciones/seed. El ensayo de este bloque no la arranca.

## Restauración de Evidencias

```powershell
.\scripts\restore-evidencias.ps1 `
  -BackupFile 'D:\Backups\MecaniCar360\evidencias_<SetId>.zip' `
  -DestinationDirectory 'D:\RestoreTemp\MecaniCar360_Recovery_20261005\Evidencias'
```

El destino debe ser nuevo; nunca sobrescribe `App_Data/Evidencias`. Se validan
previamente rutas ZIP (traversal, ADS, nombres reservados y duplicados); se rechazan
enlaces. Usar sólo ZIP confiables de estos scripts, con espacio suficiente. Ante
fallo de extracción puede quedar un directorio **nuevo parcial** para inspección:
no promoverlo a almacenamiento activo. Comparar contenido/hashes y que cada
`Evidencias.RutaArchivo` recuperada tenga su archivo. Los nombres físicos privados
se conservan; no se cambian a URLs públicas. Las ACL deben configurarse en destino
para la identidad real de la aplicación, pues no viajan en ZIP.

Para recuperación operativa: validar SQL + archivos en los nuevos destinos, custodiar
la instalación anterior y planificar un cambio de configuración controlado durante
mantenimiento. El script no cambia cadenas, reemplaza la base original ni efectúa
ese cambio. No basta restaurar SQL y olvidar Evidencias.

## Frecuencia automatizable

SQL Express no requiere SQL Agent: puede usarse Windows Task Scheduler bajo la
identidad técnica. Programar a las 02:00, sin ejecuciones simultáneas, controlar
código de salida y alertar al responsable si falla. Ejemplo de acción (ajustar rutas):

```text
Programa: powershell.exe
Argumentos: -NoProfile -NonInteractive -Command "& { $ErrorActionPreference='Stop'; $cs=ConvertTo-SecureString 'Server=BOOK-JKTL5UI49E\CABRALPEDRO;Database=MecaniCar360;Integrated Security=True;Encrypt=True;TrustServerCertificate=True' -AsPlainText -Force; $id=[DateTime]::UtcNow.ToString('yyyyMMdd_HHmmss_fff'); & 'C:\Aplicaciones\MecaniCar360\scripts\backup.ps1' -ConnectionString $cs -ServerBackupDirectory 'D:\Backups\MecaniCar360' -SetId $id; & 'C:\Aplicaciones\MecaniCar360\scripts\backup-evidencias.ps1' -SourceDirectory 'C:\Aplicaciones\MecaniCar360\App_Data\Evidencias' -BackupDirectory 'D:\Backups\MecaniCar360' -SetId $id }"
```

Requiere política de ejecución que permita scripts revisados/firmados y permisos
de la identidad de la tarea; no usar credenciales literales. El ejemplo no automatiza
la ventana de mantenimiento, la copia secundaria ni la retención: deben coordinarse
por el responsable. No se creó ninguna tarea real ni se alteró la política global.

## Ensayo realizado y limitaciones

El 2026-10-05 se generó el esquema EF actual en una base temporal con nombre aleatorio,
sin aplicar migraciones ni escribir en MecaniCar360. Se añadieron sólo allí dos
Personas ficticias, un Rol ficticio y dos PersonaRoles. Se efectuaron FULL, VERIFYONLY
y RESTORE a otra base nueva; ambas tenían las 43 tablas y todos sus conteos coincidían.
Se verificaron valores conocidos y presencia de Personas, Usuarios, OrdenesTrabajo,
Evidencias, Notificaciones, Facturas y Pagos. Las tablas operativas vacías se compararon
como vacías: no se simula cobertura funcional del CORE. Este ensayo no prueba la
recuperación de certificados, secretos ni un volumen productivo grande.

Dos archivos ficticios (uno anidado) se copiaron a ZIP y restauraron con hashes
idénticos. Pasaron las protecciones de base activa/fuente/existente, backup duplicado,
nombre inválido, archivo inexistente, ZIP duplicado y traversal. Las bases de prueba,
su historial de backup en msdb y sus archivos se limpiaron. Los conteos de todas las
tablas de desarrollo quedaron iguales. No se detuvo la aplicación existente.

Limitaciones observadas: SSPI en sandbox (se resolvió ejecutando la prueba técnica
bajo la identidad Windows autorizada), ExecutionPolicy restrictiva (permiso sólo
del proceso temporal), y ACL que impide al operador crear en `C:\SQLData\Backup`.
Se usó un directorio temporal propio con permiso exclusivo añadido al servicio SQL
para el ensayo; fue eliminado. No se cambiaron ACL preexistentes ni SQL Server.

Repetir restore de control mensualmente y tras cambios relevantes del procedimiento,
en instancia/directorio aislados. Eliminar sólo recursos identificados como propios
del ensayo y conservar su resultado, sin volcar información sensible.

## Referencias técnicas

- [Microsoft: COPY_ONLY](https://learn.microsoft.com/en-us/sql/relational-databases/backup-restore/copy-only-backups-sql-server?view=sql-server-ver16).
- [Microsoft: RESTORE VERIFYONLY](https://learn.microsoft.com/en-us/sql/t-sql/statements/restore-statements-verifyonly-transact-sql?view=sql-server-ver16).
- [Microsoft: errores de medios y CHECKSUM](https://learn.microsoft.com/en-us/sql/relational-databases/backup-restore/possible-media-errors-during-backup-and-restore-sql-server?view=sql-server-ver16).
