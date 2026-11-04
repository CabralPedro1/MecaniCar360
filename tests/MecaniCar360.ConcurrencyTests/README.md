# Concurrencia SQL aislada — A1, A2, A3 y recepción

## Estado y límites

Infraestructura para SQL Server real. **La campaña SEC-01/02/05/08 de 24 casos fue aprobada el 04/11/2026**, según el registro de cierre al final. Los 28 escenarios originales mantienen su estado separado: no quedan acreditados por esta campaña.
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

## SEC-05: idempotencia manual (12 casos SQL aprobados el 04/11/2026)

`SqlConcurrencyTests.StockIdempotencia.cs` agrega 12 casos opt-in: ingreso, salida,
ajuste positivo y negativo, por tres recorridos (primera operación/reintento/conflicto,
carrera de clave idéntica y rollback precommit con reintento). Filtro específico para
una futura ejecución autorizada: `Security=SEC05`. No ejecutar toda la campaña por defecto.

Reutiliza guard, claim, Race, drenaje, snapshots, WriteEvidence/FailBeforeCommit y
verificación DVH/DVV/FK. Los fixtures exclusivos agregan STOCK, proveedor, oferta,
repuesto y dos lotes; la salida exige OT aprobada, sin cierre ni factura. La restitución
positiva identifica el lote y respeta su cantidad ingresada. Los saldos iniciales
se preparan mediante EF, no representan movimientos históricos ejecutados por el test.

Se verifican FIFO, saldo, desglose, una fila de idempotencia/movimiento/auditoría,
destinatarios de stock bajo y ausencia de notificaciones en ingresos/restituciones.
Snapshots independientes exigen cero cambios por reintento, conflicto o rollback,
incluyendo verificadores. No se recalcula la línea base. La excepción inyectada sólo
es válida en rollback; errores/timeouts en operaciones normales o carreras fallan.
La limitación B1 permanece: coordinación no equivale a observar espera en SQL.

Pendiente: base aislada preparada con las 21 migraciones e identidad ejecutora
restringida, autorización y ejecución real. No hay limpieza automática ni cambios
a MecaniCar360; conservar los fixtures para diagnóstico hasta autorizar su retiro.

## Campaña conjunta SEC-01/02/05/08 — 24 casos (SQL aprobado el 04/11/2026)

SEC-01: 2 casos de diagnóstico. SEC-02: 2 casos de reparación/finalización.
SEC-05: 12 casos de movimientos manuales. SEC-08: 8 casos nuevos en
`SqlConcurrencyTests.CorreosCuenta.cs`, etiquetados `Security=SEC08`:

1. Contraseña: emisión, supresión pública/autenticada, token intacto y reemisión.
2. Contraseña consumida: espera conservada, auditoría de consumo y nueva emisión.
3. Carrera entre emisión autenticada y recuperación pública: un envío/fila.
4. Fallo de transporte: invalidación, supresión y recuperación tras el intervalo.
5. Invitación: supresión, conservación, reemisión e invalidación del enlace anterior.
6. Carrera de invitaciones: un envío/fila y auditoría de emisión única.
7. Fallo de invitación: invalidación/auditoría, supresión y posterior reemisión.
8. Recuperación elegible/inactiva/inexistente: misma vista pública; cero efectos
   adicionales para las solicitudes inactivas/inexistentes (acción MVC directa,
   no prueba del pipeline HTTP ni de tiempos de respuesta).

Cada caso instancia su propio ConcurrencyEnvironment, capturador de correo y
Data Protection efímero. El transporte sobrescribe EnviarMensajeAsync: no abre SMTP.
Tokens y contraseñas sólo en memoria; no se incluyen en mensajes de aserción,
logs o manifiestos. Se cuentan advertencias/errores sin conservar su contenido,
para detectar fallos SQL que el contrato público pudiera convertir en respuesta neutra.

Los intervalos vencidos se preparan mediante FechaCreacion y EF sobre registros
identificados por el fixture; no se altera el reloj ni la política productiva.
Los snapshots se toman después de terminar envío e invalidación, con contextos
independientes. Se verifican DVH/DVV/FK sin inicializar la línea base. No se migra
el test monolítico de onboarding ni se cambia su opt-in/destino.

Para una futura ejecución AUTORIZADA, usar una sola invocación/ensamblado/proceso:

```powershell
$filtro = 'Category=SqlConcurrency&(Security=SEC05|Security=SEC08|FullyQualifiedName~Diagnostico_InicioUnico|FullyQualifiedName~Reparacion_EntidadPrecargadaNoRepiteTransicion)'
dotnet vstest $dll "/TestCaseFilter:$filtro" "/ResultsDirectory:$resultados" '/Logger:trx;LogFileName=SEC01-SEC02-SEC05-SEC08.trx'
```

Requiere variables MC360_CONCURRENCY_* correctas, identidad restringida validada,
binarios actualizados, claim vacío y autorización explícita. El resultado esperado
es 24 aprobadas, ninguna fallida ni omitida. No se garantiza orden de métodos;
los fixtures son independientes y el paralelismo entre casos permanece deshabilitado.
No iniciar otra invocación contra la misma base. Conservar TRX, TEMP/base/manifests,
marcas de aborto si existen, base y backup. No hay limpieza automática.

Límites: SMTP real, navegador, antiforgery/rate limiting HTTP y latencias no quedan
acreditados por estos casos SQL. Las pruebas no SQL de EmisionCorreoCuentaTests
conservan esa cobertura HTTP simulada. B1 (sin observación directa de espera SQL)
permanece explícito. Una base preparada puede utilizarse si sigue sin claim y
con el esquema vigente; esta adaptación no añade tablas, permisos ni migraciones.

## Cierre de la tercera campaña — 04/11/2026

Campaña `b6693ea9b36f469eb13d130aee029d56`, base aislada `MecaniCar360_Concurrency_b6693ea9b36f469eb13d130aee029d56` en `BOOK-JKTL5UI49E\CABRALPEDRO`. TRX inspeccionado: **24 ejecutadas, 24 aprobadas, 0 fallidas, 0 omitidas**; 24 nombres, IDs de prueba e IDs de ejecución únicos, sin inconsistencias entre definiciones y resultados, errores, timeouts ni abortos. Duración inicio/fin: **49,822 segundos**. Código de salida **0 informado por el operador**, no acreditado independientemente por el TRX.

| Hallazgo | Casos aprobados |
|---|---|
| SEC-01 (2) | `Diagnostico_InicioUnico`, precargado false/true |
| SEC-02 (2) | `Reparacion_EntidadPrecargadaNoRepiteTransicion`, finalizar false/true |
| SEC-05 (12) | `StockIdempotencia_PrimeraReintentoYConflicto`, `StockIdempotencia_Concurrente` y `StockIdempotencia_RollbackAntesCommit`, cada uno con Ingreso/Salida/Positivo/Negativo |
| SEC-08 (8) | `CorreosCuenta_IntervaloPersistido`: PasswordSupresion, PasswordConsumido, PasswordConcurrente, PasswordFallo, InvitacionSupresion, InvitacionConcurrente, InvitacionFallo y RespuestaNeutra |

TRX local (no versionar datos ni artefactos):
`C:\ProgramData\MecaniCar360-Pruebas\b6693ea9b36f469eb13d130aee029d56\resultados\SEC01-SEC02-SEC05-SEC08-b6693ea9b36f469eb13d130aee029d56.trx`

SHA-256: `41107FBF1AD4A1C7F5C50F87D89982EC9BC68E7168254F95D03DD340902FE80C`.

Evidencia revisada: TRX de 35.505 bytes, 56 manifiestos JSON sin marca de aborto en el directorio inspeccionado, 152 archivos de binarios coincidentes con inventario/tamaños/SHA-256, y registros de preparación en `preparacion` junto a `resultados`. La versión ejecutada incluye cambios locales sin commit: el HEAD por sí solo no identifica el paquete; conservar `head.txt`, `git-status.txt`, `source-sha256.csv` y `package-sha256.csv`.

Antecedentes conservados, no sustituidos por este PASS:

- Primera campaña `1ed16434fba445a4b88acf61e9cba946`: fallo del fixture por `CK_InvitacionesCliente_Vigencia`; corregida la duración exacta de 48 horas y su preparación temporal. No se pudo inspeccionar íntegramente su carpeta de resultados durante este cierre.
- Segunda campaña `7351835e0e8f480daf41b597a10252a8`: TRX con 6 aprobadas y 18 fallidas por interpretación vacía de JSON en `SnapshotAsync`; TRX anterior presente. Se corrigió el helper de pruebas, sin alterar producción. Sus resultados parciales siguen siendo antecedentes, no PASS global.

No se verificaron los respaldos ni se consultaron bases durante la revisión documental. Conservar bases, claims, backups y evidencias; ninguna campaña ejecutada puede reutilizarse.

Este cierre acredita únicamente los 24 escenarios seleccionados y sus aserciones persistidas/integridad. **No cierra D3/AC14 ni los 28 escenarios originales. B1 continúa abierto:** coordinación no demuestra observación de espera por bloqueo SQL ni ausencia universal de deadlocks. No acredita navegador, SMTP real, OAuth, carga prolongada ni aceptación integral. SEC-08 usa transporte simulado; sus acciones MVC no sustituyen el pipeline HTTP. Los pendientes descritos antes de este registro son antecedentes de implementación en lo cubierto por estos casos; los demás permanecen pendientes.
