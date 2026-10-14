# Concurrencia SQL aislada — A1, A2, A3 y recepción

## Estado y límites

Infraestructura escrita para SQL Server real. **No se ha preparado ninguna base ni ejecutado esta suite.**
Objetivo: pruebas funcionales con ejecuciones concurrentes coordinadas. **B1 permanece abierto**:
no se acredita mediante observación del servidor la contención efectiva por un bloqueo SQL.
Compilar no ejecuta el preparador ni los tests. No hay pruebas estructurales sustituyendo estos escenarios.
No se registra ninguno de estos proyectos en la solución existente para evitar ejecución accidental.
Se necesitan autorización posterior, dos identidades Windows y validación de permisos por el administrador SQL.

No utilizar `MC360_INTEGRATION_12C1`: esa suite antigua exige la base de desarrollo.
No ejecutar el host web, el factory de diseño ni herramientas EF con configuración implícita.

## Guard y separación de identidades

Sólo `MC360_CONCURRENCY_CONNECTION` suministra la cadena. No se leen JSON, User Secrets ni DefaultConnection.
`MC360_CONCURRENCY_RUN_ID` debe ser GUID no vacío, minúsculas, formato N.
Instancia exacta: `BOOK-JKTL5UI49E\CABRALPEDRO`.
Base exacta: `MecaniCar360_Concurrency_<RUN_ID>`; se rechazan desarrollo, bases de sistema,
aliases, AttachDBFilename, FailoverPartner, UserInstance, credenciales SQL, MARS y transacciones ambientales.
Se exige `Enlist=False`; pooling queda deshabilitado. Cada conexión comprueba SERVERPROPERTY y DB_NAME.

Preparación: identidad con CREATE DATABASE, capacidad de aplicar el esquema en la base nueva,
crear extended properties, BACKUP DATABASE y RESTORE HEADERONLY/VERIFYONLY. SQL Server debe
tener permiso de escritura en su directorio de backup predeterminado. No se conceden permisos automáticamente.
El preparador sólo acepta `prepare --confirm-create-isolated`, se detiene ante base existente y
no borra/repara una preparación parcial. Una ejecución fallida requiere inspección; usar otro GUID
no elimina ni autoriza eliminar los restos de la anterior.

Ejecución: **otra identidad Windows**, con CONNECT y SELECT/INSERT/UPDATE/DELETE en las tablas
de esta base, más VIEW DEFINITION de esta base para comprobar todas las FK. No db_owner,
CONTROL/ALTER DATABASE, CREATE TABLE, BACKUP DATABASE ni permisos administrativos de servidor.
No acceso a `MecaniCar360` (comprobado con HAS_DBACCESS). El DBA debe revisar también roles/grants
heredados; el guard no reemplaza una identidad restringida. No usar un login sysadmin para los tests.
sp_getapplock se conserva con su principal público y recurso productivo, de ámbito de base.

El preparador marca el GUID y ORIGINAL_LOGIN mediante extended properties sólo al terminar.
El runner comprueba ambas marcas y que la identidad sea distinta. Una tabla **exclusiva del harness**,
`dbo.MC360ConcurrencyExecution`, creada sólo por el preparador, recibe un claim único por GUID.
Una segunda ejecución/proceso contra la misma base falla por PK: no se reutiliza silenciosamente.
Esa tabla no forma parte del modelo productivo ni de sus migraciones/DVH.
El runner no contiene comandos CREATE/DROP/RESTORE ni llama a Migrate/EnsureCreated/EnsureDeleted.

## Preparación (NO ejecutar sin autorización)

Desde una consola de la identidad de preparación, en la raíz del repositorio:

```powershell
$env:MC360_CONCURRENCY_RUN_ID = [guid]::NewGuid().ToString('N')
$basePrueba = "MecaniCar360_Concurrency_$env:MC360_CONCURRENCY_RUN_ID"
$env:MC360_CONCURRENCY_CONNECTION = "Server=BOOK-JKTL5UI49E\CABRALPEDRO;Database=$basePrueba;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Enlist=False;MultipleActiveResultSets=False"
dotnet run --project tests/MecaniCar360.ConcurrencySetup/MecaniCar360.ConcurrencySetup.csproj --configuration CierreB -- prepare --confirm-create-isolated
```

Secuencia: verificar destino/ausencia → CREATE de esa base → migraciones existentes → FULL COPY_ONLY
CHECKSUM y VERIFYONLY → InicializarAsync con backup propio → ArranqueIntegridad/seed/bootstrap
ficticio con password aleatorio en memoria → integridad/migraciones/modelo → marca de preparación.
Nunca se imprime contraseña, hash o stamp. `sin-escrituras` expresa una ventana exclusiva,
**no** un dry run: inicializar DVH/DVV escribe. No se recalcula baseline entre pruebas.
Los archivos de incidentes van a TEMP/base/integrity, nunca a los directorios productivos.

Después el DBA concede los permisos mínimos a la identidad de ejecución sobre **esta base exacta**.
No ejecutar comandos genéricos de grants desde la aplicación. No cambiar el propietario a la identidad runner.

## Ejecución posterior

En otra consola bajo la identidad restringida, reutilizar exactamente el GUID generado (no generar otro):

```powershell
$env:MC360_CONCURRENCY_RUN_ID = '<GUID_N_DE_LA_PREPARACION>'
$basePrueba = "MecaniCar360_Concurrency_$env:MC360_CONCURRENCY_RUN_ID"
$env:MC360_CONCURRENCY_CONNECTION = "Server=BOOK-JKTL5UI49E\CABRALPEDRO;Database=$basePrueba;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Enlist=False;MultipleActiveResultSets=False"
$env:MC360_CONCURRENCY_EXECUTE = '1'
dotnet test tests/MecaniCar360.ConcurrencyTests/MecaniCar360.ConcurrencyTests.csproj --configuration CierreB --filter 'Category=SqlConcurrency' --logger 'console;verbosity=normal'
```

Sin opt-in las pruebas quedan omitidas. Destino/identidad inválidos fallan, no se consideran omitidos
ni un rechazo funcional correcto. No hay limpieza automática; conservar base y manifiestos para diagnóstico.

## Fixtures

Cada caso crea ADMIN, dos MECANICO y CLIENTE completo/habilitado ficticios, catálogo y tres vehículos
con titularidad. Etiqueta `TEST_CONCURRENCY_<GUID>`, correo example.invalid, passwords sólo en memoria.
Prerrequisitos y estados históricos se disponen mediante EF SaveChanges y DVH/DVV, sin SQL directo
de negocio: no se está probando onboarding en esta suite. Incluye invitación consumida ficticia para
representar correo verificado. No se invoca correo, Google ni SMTP y no se reutiliza ADMIN del seed.
Turnos pasados para ausencia se disponen como fixtures, sin alterar relojes ni reglas productivas.
La OT se crea mediante el servicio real de recepción. Las operaciones concurrentes llaman servicios reales.
Manifiestos sin credenciales: TEMP/base/manifests. Los turnos creados por las reservas se localizan
por ClienteId/etiqueta del manifiesto. No se borran fixtures ni backups.

## Escenarios implementados (28 casos, todavía NO ejecutados)

| Casos | Criterio persistido |
|---|---|
| 2 reservas: último cupo / mismo vehículo | Un ganador; mensaje exacto del rechazo; cupo/duplicación, historial y auditoría correctos |
| 4 toma/toma o toma/asignación, ambos ganadores posibles | Una asignación, destinatario correcto, una auditoría, estado pendiente |
| 10 transiciones en ambos órdenes | Confirmar/cancelar y confirmar/recepción pueden producir dos éxitos seriales; restantes rechazan al perdedor conforme a las reglas |
| 2 recepción precargada contra cancelación/ausencia | Contexto conserva Confirmado antes de recepción; recarga provoca rechazo; snapshot persistido idéntico |
| 10 rollback (alta, toma, confirmar, ausente, recepción × dos puntos) | Excepción inyectada exacta, DML alcanzado o precommit alcanzado, todas las filas y verificadores iguales al snapshot inicial |

Las lecturas de resultado usan un tercer contexto y conexión. Cada escenario verifica DVH/DVV y
todas las FK declaradas mediante anti-joins de sólo lectura; se exige visibilidad de metadatos completa.
El snapshot de rollback ordena filas JSON de todas las tablas y compara SHA256 en memoria sin imprimir contenido.
No compara contadores IDENTITY (SQL Server no los revierte), que no son registros parciales.

## Coordinación y límites conocidos

Paralelismo xUnit entre casos deshabilitado. El primer participante se pausa mediante TaskCompletionSource
en una lectura del turno/OT con transacción activa (ya obtuvo mutex DVH). El segundo anuncia
TransactionStarting; se libera al primero y se esperan ambas tareas. No hay Thread.Sleep.
Se comprueba que los contextos/conexiones son distintos y que el competidor no terminó antes de intentar
su transacción. La señal TransactionStarting **no demuestra** que SQL ya esté esperando sp_getapplock:
ver esa espera concreta requiere instrumentación SQL adicional; no se afirma que esté implementada.
El comando sp_getapplock productivo es ADO.NET directo y no atraviesa DbCommandInterceptor.

Gates: 5 segundos; operación: 30 segundos; mutex productivo: 10 segundos. No se aumenta el timeout
ni se habilitan retries. Bajo carga, una barrera puede fallar: se reporta como fallo, nunca PASS.
No se promete ausencia global de deadlocks; los escenarios deben ejecutarse después en SQL real.

### B2: evidencia de escritura y rollback

`WriteEvidence` captura un snapshot leído por SQL antes de SaveChanges y lo compara con otro
después, usando la misma conexión y transacción de la operación. Exige `result > 0`, cambios
persistidos visibles dentro de esa transacción y la misma identidad de transacción. Los valores
y digests quedan sólo en memoria. No basta con que se ejecute el interceptor.

Fallo 1: `SavedChangesAsync`, después de acreditar DML y antes del mantenimiento DVH de ese guardado.
Fallo 2: `TransactionCommittingAsync`, exige además un cambio de las filas DVV respecto del inicio
y una verificación DVH/DVV válida dentro de la misma transacción, sin abrir otra ni recalcular baseline.
Si falta evidencia se lanza un error de infraestructura, distinto de la excepción inyectada esperada:
la prueba falla. Después del rollback, un contexto independiente compara todas las filas con el
snapshot anterior, incluidos estado, historial, auditoría y verificadores, y verifica integridad.

### B3: terminación obligatoria de participantes

Ante fallo o timeout se cancela cooperativamente la barrera y las siguientes operaciones EF
mediante un interceptor exclusivo del harness. Esto no cancela un comando ADO.NET directo ya en
curso: no se promete cancelación del `sp_getapplock` productivo. Se liberan las barreras y se espera
un máximo adicional de 30 segundos a que **ambas tareas estén en estado terminal**.
Se recogen las excepciones originales, fallos y cancelaciones del drenaje; ninguno se convierte
en rechazo funcional válido ni se descarta silenciosamente.

Si no terminan, se escribe `TEMP/base/ABORTED-NO-REUSE.txt` y se aborta todo el test host con código 2
mediante `Environment.Exit`, sin ejecutar verificaciones finales ni otros escenarios. El claim SQL
de ejecución única ya consumido también impide reutilizar la base desde otro proceso/equipo. Si no
puede escribirse la marca local se informa explícitamente y se aborta igualmente. No se intenta
escribir en SQL mientras hay participantes potencialmente activos. El servidor podría seguir
finalizando operaciones o su rollback al desconectarse el proceso: la base queda descartada para nuevas
pruebas y requiere inspección posterior autorizada. No hay limpieza automática.

Sólo después de comprobar terminación se disponen los contextos y se ejecutan verificaciones finales.
La terminación forzada aparecerá como ejecución abortada, nunca como suite aprobada.

## Validación sin SQL

```powershell
dotnet build tests/MecaniCar360.ConcurrencySetup/MecaniCar360.ConcurrencySetup.csproj --configuration CierreB -p:UseAppHost=false
dotnet build tests/MecaniCar360.ConcurrencyTests/MecaniCar360.ConcurrencyTests.csproj --configuration CierreB -p:UseAppHost=false
dotnet test tests/MecaniCar360.Tests/MecaniCar360.Tests.csproj --configuration CierreB -p:UseAppHost=false --filter 'FullyQualifiedName!~OnboardingSqlIntegrationTests'
git diff --check
```

No ejecutar `dotnet test` sobre el proyecto nuevo durante la fase de preparación de código.
Pendiente: validación real de permisos, backups, seed, fixtures, barreras y todos los resultados SQL;
observación DMV/XEvents de esperas; pruebas de carga prolongada. No sustituirlas por el resultado del build.
