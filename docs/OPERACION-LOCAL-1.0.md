# MecaniCar360 1.0 — operación local y aceptación

Actualización: 15/10/2026. Código de referencia: `4ebf977` más cambios locales de arranque de aceptación. Guía preparada por inspección y pruebas no SQL; no acredita una instalación nueva, restore ni aceptación manual ejecutados.

## Preparación y configuración

Requiere SDK capaz de compilar .NET 8, runtime ASP.NET Core 8 y SQL Server accesible. Versiones de paquetes: `MecaniCar360.csproj` y proyecto de tests. Usar configuración externa existente; no copiar secretos a Git. La configuración estándar combina appsettings, archivo de entorno, User Secrets en Development, variables de entorno y argumentos (estos últimos prevalecen).

| Clave | Uso / precaución |
|---|---|
| `ConnectionStrings:DefaultConnection` | Confirmar servidor/base efectivos antes de arrancar; no imprimir la cadena. Destino local esperado: BOOK-JKTL5UI49E\CABRALPEDRO / MecaniCar360 |
| `Arranque:ModoAceptacion` | `true` evita migraciones/seed/bootstrap/catálogo; por defecto false. No impide escrituras de negocio |
| `BootstrapAdmin:Email` | Destinatario explícito de la activación inicial ADMIN; no se deduce de Smtp:User ni modifica cuentas existentes |
| `BootstrapAdmin:OperadorSid` | SID Windows del operador autorizado para emitir/reemitir localmente; además requiere consola elevada |
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

### Google OAuth local: URI de retorno

`Program.cs` registra `AgregarGoogleCliente`; `Helpers/GoogleClienteConfiguracion.cs` conserva el callback del middleware `/signin-google`. El test `Google_ConfiguraSubjectVerificado_SinGuardarTokens` comprueba ese valor sin red ni SQL. `GoogleClienteController.Callback` es el destino interno posterior, no la URI que debe registrarse en Google.

| Perfil / origen utilizado en el navegador | URI de redirección autorizada |
|---|---|
| `https`, HTTPS recomendado | `https://localhost:7175/signin-google` |
| `http`, sólo si se usa ese origen local | `http://localhost:5190/signin-google` |
| IIS Express, sólo si se utiliza | `https://localhost:44310/signin-google` |

Para resolver `redirect_uri_mismatch`:

1. Abrir Google Cloud Console → Google Auth Platform → Clients (o APIs y servicios → Credenciales), en el proyecto del cliente OAuth configurado.
2. Seleccionar el cliente de tipo aplicación web cuyo ID coincide con `Authentication:Google:ClientId`, sin publicar su valor ni el secreto.
3. En **URI de redirección autorizadas**, agregar exactamente `https://localhost:7175/signin-google`. Conservar las URI existentes que se utilicen. Agregar las alternativas de la tabla sólo si se necesitan; configurar únicamente un origen JavaScript no sustituye esta URI.
4. Guardar y volver a iniciar el flujo desde `https://localhost:7175/Account/Login`. Esquema, host, puerto, ruta y barra final deben coincidir exactamente. Si persiste, comparar sólo el parámetro `redirect_uri` enviado con el registro; no compartir la URL completa de OAuth, códigos ni `state`.
5. Si el cliente está en modo de prueba y exige usuarios de prueba, verificar el usuario permitido en Audience. Es un requisito separado del error de URI.

El perfil `https` de `Properties/launchSettings.json` establece Development, que carga User Secrets. En una sesión de arranque autorizada: `dotnet run --project MecaniCar360.csproj --launch-profile https -- --Arranque:ModoAceptacion=true`. Este comando **abre SQL**; no ejecutarlo como comprobación sin autorización. El modo de aceptación no desactiva Google ni impide las escrituras del login.

No es necesario cambiar credenciales, `CallbackPath` ni autorizaciones para corregir este desajuste externo. La configuración de Google Cloud no se ha inspeccionado ni modificado aquí; la validación OAuth real sigue pendiente. Google permanece exclusivo para cuentas CLIENTE elegibles sin perfiles internos vigentes; la vinculación conserva la reautenticación en el portal.

Referencia: [documentación oficial de Google sobre OAuth para aplicaciones web](https://developers.google.com/identity/protocols/oauth2/web-server#authorization-errors-redirect-uri-mismatch).

### Menú personal

El encabezado mantiene nombre y notificaciones. **Mi cuenta** agrupa Mis datos para CLIENTE habilitado con sus permisos, seguridad y cierre de sesión. Agregar/cambiar contraseña del cliente envía el enlace mediante el POST existente; las cuentas locales que usan Account conservan su formulario con contraseña actual. Recuperar contraseña permanece en Login. Vincular Google permanece en el portal con contraseña local y comprobación reciente; no se duplica ni se omite ese paso. Cerrar sesión conserva POST y antiforgery.

Comprobar con Tab, Enter, flechas y Escape el desplegable en escritorio y móvil. La revisión de marcado y los tests no sustituyen esta aceptación en navegador.

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

### Activación inicial ADMIN por correo (implementación 04/11/2026)

Sólo una instalación nueva crea `admin` sin contraseña, inactivo y con `PrimerLogin=true`.
Persona, usuario, rol y marca de activación se confirman conjuntamente. Una cuenta ADMIN
existente, incluso suspendida, no se reemplaza ni se habilita: el bootstrap no es recuperación
administrativa. `BootstrapAdmin:Password` dejó de utilizarse; no se generan contraseñas temporales.

Antes del bootstrap autorizado configurar externamente `BootstrapAdmin:Email`,
`BootstrapAdmin:OperadorSid`, SMTP e `Invitaciones:UrlBase` (HTTPS, sin query, fragmento ni
credenciales). El destinatario puede coincidir con `Smtp:User`, pero ambos son independientes.
Ejemplo de configuración **manual, no ejecutada por esta implementación**, desde la raíz:

```powershell
whoami /user
dotnet user-secrets set 'BootstrapAdmin:Email' '<correo administrativo real>' --project MecaniCar360.csproj
dotnet user-secrets set 'BootstrapAdmin:OperadorSid' '<SID del operador Windows>' --project MecaniCar360.csproj
```

La consola de emisión debe ejecutarse como ese mismo usuario Windows, **elevada**.
No es un rol nuevo de la aplicación ni una cuenta secreta. El permiso protege contra
una invocación web o un operador Windows distinto; quien controla la configuración,
el código o SQL con privilegios ya pertenece a la frontera técnica de confianza.
No cambiar ACL ni conceder permisos SQL automáticamente para hacerlo funcionar.

El seed no envía correos y los reinicios normales no reemplazan enlaces. Después de
crear la instalación mediante el procedimiento anterior y comprobar su integridad,
iniciar el host HTTPS con `--Arranque:ModoAceptacion=true`. Para **enviar el primer enlace**
o **reemitir uno vencido/fallido**, usar otra consola elevada y confirmar el destino real:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development' # permite leer User Secrets del mismo operador
dotnet run --no-build --project MecaniCar360.csproj --configuration CierreB --no-launch-profile -- --bootstrap-admin-enviar --confirmar-servidor '<instancia exacta>' --confirmar-base '<base exacta>'
```

Este comando requiere autorización operativa para escribir la fila técnica/auditoría y
enviar SMTP. **No ejecutarlo contra la base actual para probarlo.** Sólo usa autenticación
SQL integrada, comprueba instancia/base reales e integridad antes de emitir; no inicia el
servidor web, no migra, no hace seed ni inicializa DVH/DVV. No imprime destinatario, token,
enlace, contraseña ni cadena. Retorna 0 si SMTP aceptó el mensaje e integridad sigue válida;
1 ante rechazo/fallo y 2 ante argumentos inválidos. No hay reintentos automáticos.

Abrir el correo en el equipo de la instalación (`localhost` se refiere al equipo donde se
abre el navegador), preferentemente sin otra sesión web. La URL contiene el token en el
fragmento, que el script elimina de la barra y entrega por header. No guardar HAR, headers,
cuerpos de petición, enlaces o correos en logs/evidencias. No habilitar logging HTTP sensible.
El formulario exige antiforgery, contraseña/confirmación y teléfono opcional con las reglas
de primer ingreso. Se valida el máximo de 72 bytes UTF-8 sin truncar. Al confirmar se activan
la cuenta y su credencial, se completa PrimerLogin, rota SecurityStamp y consume el enlace
en una misma transacción con auditoría y mantenimiento DVH/DVV. No inicia sesión: acceder
después al login normal como `admin` con la contraseña elegida.

El enlace dura 10 minutos. Lectura o contraseña inválida no lo consumen. Sólo puede
reemitarse desde el comando local para la marca pendiente original, con el mismo correo
configurado; nunca para un ADMIN legado o activado. Los reenvíos comparten una espera
persistente de 60 segundos y reemplazan el enlace anterior. No existe solicitud pública.
Si SMTP falla, se intenta invalidar únicamente el token afectado; si falla también esa
escritura, conserva su vencimiento. SMTP se ejecuta fuera de la transacción: su aceptación
no prueba entrega. Si el proceso termina después del commit y antes/durante SMTP, esperar
el intervalo y reemitir localmente. Si la activación se confirmó pero se perdió la respuesta,
usar login; no repetir bootstrap. Cambiar el destinatario en configuración no transfiere
la cuenta: requeriría revisión explícita, no hay mecanismo web para hacerlo.

Se reutiliza `EnlacesPasswordCliente`, `Finalidad=Agregar` y sus columnas existentes, con
hashes separados por propósito `MecaniCar360.BootstrapAdmin.Activacion.v1` para token,
stamp y correo. La marca inicial está invalidada y no contiene un token utilizable.
No se amplía el flujo CLIENTE ni sus permisos. No se agrega migración: la tabla técnica
continúa fuera de DVH/DVV; las entidades protegidas conservan la cobertura vigente.
Un atacante con escritura directa en SQL/configuración no queda contenido por este mecanismo.

Los 11 escenarios SQL ADMINACT están aprobados; ver el [cierre del harness](../tests/MecaniCar360.ConcurrencyTests/README.md#cierre-adminact-20261110). Pendientes: emisión/reemisión local privilegiada, rollback del bootstrap inicial, preservación de administradores por el seed, SMTP real, navegador y login administrativo posterior. No usar este procedimiento
como permiso para borrar o reinicializar la base de la defensa.
