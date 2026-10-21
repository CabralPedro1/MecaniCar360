# MecaniCar360 1.0 — operación local y aceptación

Actualización: 15/10/2026. Código de referencia: `4ebf977` más cambios locales de arranque de aceptación. Guía preparada por inspección y pruebas no SQL; no acredita una instalación nueva, restore ni aceptación manual ejecutados.

## Preparación y configuración

Requiere SDK capaz de compilar .NET 8, runtime ASP.NET Core 8 y SQL Server accesible. Versiones de paquetes: `MecaniCar360.csproj` y proyecto de tests. Usar configuración externa existente; no copiar secretos a Git. La configuración estándar combina appsettings, archivo de entorno, User Secrets en Development, variables de entorno y argumentos (estos últimos prevalecen).

| Clave | Uso / precaución |
|---|---|
| `ConnectionStrings:DefaultConnection` | Confirmar servidor/base efectivos antes de arrancar; no imprimir la cadena. Destino local esperado: BOOK-JKTL5UI49E\CABRALPEDRO / MecaniCar360 |
| `Arranque:ModoAceptacion` | `true` evita migraciones/seed/bootstrap/catálogo; por defecto false. No impide escrituras de negocio |
| `BootstrapAdmin:Password` | Sólo para instalación normal que necesite ADMIN inicial; no cambiar cuentas existentes ni publicar el valor |
| `Smtp:Host`, `Port`, `User`, `Password`, `EnableSsl` | EmailService usa User como remitente y autenticación; TLS por defecto true. No enviar correo real sin autorización |
| `Invitaciones:UrlBase` | Origen público del enlace; para esta sesión local, http://localhost:5190. No cambiar puerto sin revisar coherencia del enlace |
| `Authentication:Google:ClientId`, `ClientSecret` | Google disponible cuando ambos están configurados; no inventar valores ni modificar consentimiento/redirects para una prueba |
| `Evidencias:Directorio` | Por defecto App_Data/Evidencias, privado, fuera de wwwroot; preservar archivos junto a la base |
| `Integridad:DirectorioIncidentes` | Por defecto App_Data/Integridad; registro privado de incidentes, puede escribir archivos incluso en aceptación |

La base habitual contiene datos de prueba que deben conservarse. `TEST_CIERRE10_A123` no es un fixture descartable ni puede avanzar de estado para completar esta sesión. No crear cuentas, cambiar permisos ni consumir enlaces sólo para conseguir un PASS.

## Arranque de aceptación sobre base existente

No ejecutar si el destino no coincide, falta la base/esquema o se necesita inicializar integridad. Esas operaciones requieren autorización específica. Comprobar puerto y procesos existentes; no terminar procesos ajenos.

Desde la raíz, compilar sin iniciar el host; elegir otra configuración si su binario está en uso:

```powershell
dotnet build MecaniCar360.csproj --configuration Aceptacion -p:UseAppHost=false -v:q
dotnet bin/Aceptacion/net8.0/MecaniCar360.dll --environment=Development --urls=http://localhost:5190 --Arranque:ModoAceptacion=true
```

El segundo comando **abre SQL para verificar integridad** y debe ejecutarse sólo en una sesión de arranque autorizada. Mantener esa terminal abierta; Ctrl+C detiene únicamente ese host. No usar el arranque normal como sustituto: llama a Database.Migrate e InicializadorBD. No cambiar configuración persistente para activar aceptación.

Comprobación de lectura, sin login ni cookies existentes:

```powershell
(Invoke-WebRequest http://localhost:5190/Account/Login -UseBasicParsing).StatusCode
```

Esperado: 200 y formulario Login. El middleware permite continuar sólo con integridad válida. Un 503 significa detener aceptación e investigar incidente privado; no recalcular baseline, ejecutar seed ni editar verificadores. Un 200 no prueba las cuentas ni la integridad después de futuras escrituras. El 14/10 se obtuvo 200 en una sesión anterior; esta guía no certifica que el proceso siga activo hoy.

## Pruebas seguras y diagnóstico

```powershell
dotnet test tests/MecaniCar360.Tests/MecaniCar360.Tests.csproj --configuration Aceptacion -p:UseAppHost=false --filter 'FullyQualifiedName!~OnboardingSqlIntegrationTests' -v:q
git diff --check
```

No habilitar opt-in de integración ni ejecutar todos los proyectos indiscriminadamente. Resultado del 15/10: build 0 errores, 26 CS8618; 221 tests seleccionados aprobados, modelo/snapshot alineados. No se ejecutó SQL. Clasificación: [ADVERTENCIAS-1.0.md](ADVERTENCIAS-1.0.md).

| Síntoma | Comprobación mínima | Qué evitar |
|---|---|---|
| Sin conexión HTTP | Proceso, puerto, entorno y log sanitizado del host autorizado | Reiniciar instancias ajenas o cambiar conexión a otra base |
| 503 de integridad | Incidente, accesibilidad SQL, esquema/baseline y versión por lectura autorizada | Reparación automática, eliminación de datos, recalcular hashes |
| Acceso denegado | Cuenta activa, patente efectiva, ownership/asignación; retornar por Account/Inicio | Ampliar permisos o bypass para ocultar el problema |
| Correo no llega | UrlBase presente, parámetros SMTP, resultado operativo sanitizado | Repetir envíos masivos, imprimir token/contraseña o asumir entrega por respuesta pública neutra |
| Google no disponible | Presencia de ambas claves, URI configurada y consentimiento del usuario | Vincular por coincidencia de correo/DNI o exponer secretos |
| Error de negocio | Guardar mensaje y pasos con IDs ficticios; comprobar estado vigente | Reintentar escrituras a ciegas o editar SQL manualmente |

## Sesión manual por perfil

Usar la matriz canónica [ACEPTACION-1.0.md](ACEPTACION-1.0.md), IDs AC/AD/CO/ST/CL/PV y grupos V-A–V-G. Antes de mutar datos, autorizar un escenario separado y registrar IDs en manifiesto privado externo. Login puede registrar auditoría; no equivale a consulta sin efectos. No se han confirmado aquí cuentas disponibles por rol.

| Perfil | Secuencia concreta | Esperado |
|---|---|---|
| ADMIN | Login → Dashboard → Personas y cuentas → buscar → detalle → Roles/Seguridad → Marca/Modelo/Detalle → Auditoría/Reportes → salir | Entradas autorizadas, datos legibles, retorno funcional; CLIENTE protegido del circuito de roles y último ADMIN protegido en pruebas autorizadas |
| STOCK | Login → Repuestos → detalle/movimientos/proveedores → alta con campos inválidos y luego válidos → ingreso de existencias autorizado | Stock inicial cero; errores visibles conservan entrada; existencia deriva de movimiento; inactivos no llevan a enlaces incompatibles |
| CAJA | Login → Clientes → cliente/vehículo → turno confirmado → recepción y generación de OT → seguimiento → factura/pago/entrega tras trabajo mecánico | Titularidad correcta, ingreso/OT únicos; no puede asignar mecánicos; no factura/entrega antes de requisitos; saldo y egreso coherentes |
| MECANICO | Login → pendientes/asignadas → tomar voluntariamente OT disponible sin asignar, o abrir OT asignada directamente por ADMIN → diagnóstico/evidencias → presupuesto/repuestos → enviar → reparar/finalizar tras aprobación | Sólo OT permitida; no tomar OT ocupada por otro mecánico; consulta técnica sin datos comerciales; mensajes claros y estados/historial coherentes |
| CLIENTE | Login → Mis datos/vehículos/turnos/órdenes → presupuesto propio → decidir → factura/garantía/calificación → seguridad de cuenta | Sólo recursos propios; acción Google/contraseña según estado real; no acceso ajeno ni duplicación de cuenta |

Ejecutar un camino aprobado y uno rechazado sobre registros diferentes. En el rechazado comprobar motivo y factura sólo de diagnóstico según regla existente. Importes: pago parcial, saldo final y rechazo de sobrepago; no realizar una transacción bancaria real.

Visual: a 1366 px hacer recorrido completo; a 320/375 repetir menú y formularios representativos; a 768 tablas/menú; zoom 200 %, Tab/Shift+Tab/Enter/Espacio y foco visible. Esperado: acciones alcanzables, etiquetas persistentes, mensajes legibles, scroll limitado a tablas, sin errores JS/CSP que impidan operar. No guardar capturas con secretos ni URL de token completa. Registrar cada caso PASS/FAIL/NO EJECUTADO con fecha, versión, perfil e IDs, no sólo “se ve bien”.

## Concurrencia aislada: autorización pendiente

Seguir exclusivamente [README del harness](../tests/MecaniCar360.ConcurrencyTests/README.md), que contiene comandos exactos de preparación/ejecución. **No ejecutarlos como parte de esta guía cotidiana.**

Lista de autorización previa:

1. Base nueva exacta `MecaniCar360_Concurrency_<GUID_N>` en instancia autorizada; nunca MecaniCar360. GUID nuevo para preparación y el mismo para runner.
2. Dos identidades Windows: preparador con creación/migraciones/backup; runner distinto con CONNECT, DML y VIEW DEFINITION sólo de la base aislada, sin acceso a desarrollo ni privilegios administrativos.
3. Conexión exclusiva `MC360_CONCURRENCY_CONNECTION`; sin appsettings/User Secrets/DefaultConnection. `MC360_CONCURRENCY_RUN_ID` y opt-in `MC360_CONCURRENCY_EXECUTE` según README.
4. Preparación escribe base/esquema/baseline/fixtures y backup: autorización explícita separada. Si falla, detenerse, no borrar ni reparar automáticamente. Directorio de backup accesible al servicio SQL y espacio disponible.
5. Runner no crea/migra/restaura bases; claim único impide reutilización. No paralelismo entre escenarios, ni retries para ocultar fallos. Conservar manifiestos/resultados sin secretos.
6. Esperar 28 resultados persistidos y DVH/DVV/FK verificados por contexto independiente. Timeout, deadlock o aborto no son PASS; si quedan tareas activas, base no reutilizable y revisión autorizada.
7. B1 sigue abierto: barreras coordinan ejecuciones, no observan espera efectiva SQL. No agregar DMV, procedimientos firmados ni permisos de servidor para cerrarlo.

No se ejecutó preparación, backup, migraciones ni ninguno de esos casos en esta sesión. La limpieza posterior también necesita autorización.

## Instalación nueva, backup y recuperación

Una instalación nueva es una operación de base autorizada aparte: confirmar destino y versión → aplicar migraciones existentes → backup verificado y baseline según [integridad](integridad-dvh-dvv.md) → bootstrap normal → verificación. No usar este procedimiento para reinicializar desarrollo. `--sin-escrituras` confirma ventana exclusiva, no dry run.

Seguir [backup-restore.md](backup-restore.md) para scripts y permisos. Custodiar por separado SQL, evidencias físicas, código/versiones, configuración externa, claves Data Protection y certificados. SQL solo no recupera los archivos. Ante incidente conservar logs sanitizados y backups; no restaurar encima de MecaniCar360 sin autorización. Validar una recuperación en destino aislado cuando se autorice; no se declara ensayada por leer la guía.
