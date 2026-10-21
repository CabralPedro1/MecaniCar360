# MecaniCar360 — instrucciones de trabajo

## Objetivo y prioridades

Cerrar MecaniCar360 1.0: gestión académica de un taller, administración interna y portal del cliente. Entregar el flujo existente seguro, íntegro y utilizable; no ampliar funcionalidades ni emprender refactorizaciones generales.

- Usar `docs/PLAN-CIERRE.md` como backlog y criterios de aceptación. Priorizar P0 (impide operar), P1 (defecto crítico/seguridad/integridad demostrado) y P2 (obligatorio incompleto). P3 sólo por solicitud expresa.
- Antes de actuar, comprobar rama, `git status`, diff y archivos reales. Conservar trabajo pendiente y commits existentes. No revertir cambios ajenos.
- Contrastar cada hallazgo del plan con el código y Git; el documento puede estar desactualizado. En particular, **Modelo/Detalle ya existe versionado**: no tratar H01 como prueba de una vista ausente.
- Avanzar autónomamente dentro del alcance autorizado, en cambios pequeños. No repetir auditorías completas ni pruebas ya suficientes sin una razón concreta.

## Arquitectura y convenciones

- C# / ASP.NET Core MVC sobre .NET 8; Razor; EF Core con SQL Server. DI y pipeline en `Program.cs`; contexto, seed y mecanismos de integridad en `Data/`.
- `Controllers/`: HTTP, model binding, autorización y presentación de resultados. `Services/`: operaciones de negocio y persistencia. `Models/`: entidades, DTO, ViewModels y enums. Mantener los contratos existentes, incluidos `ServiceResult` y formularios POST.
- `Views/`, `Views/Shared/`, `wwwroot/`: reutilizar layouts, partials, Bootstrap/jQuery locales y CSS existentes. Conservar nombres de campos, etiquetas asociadas, IDs únicos, mensajes de validación y navegación autorizada.
- `Attributes/`, `Middleware/`, `Helpers/`: controles transversales. `Patterns/`: conservar patrones académicos existentes, incluidos Facade, Memento, Observer, State, Strategy y Composite; no eliminarlos por falta de referencias directas.
- Autenticación por cookie, BCrypt y Google; PDFsharp para reportes; xUnit para pruebas. Las versiones vigentes están en los `.csproj`; no agregar dependencias sin necesidad y autorización.
- Seguir el estilo del archivo intervenido, nombres del dominio y métodos async existentes. Evitar reformateos, cambios de nulabilidad persistida y operadores `!` para silenciar advertencias sin justificar su semántica.
- Mantener tracking, traducción LINQ a SQL, ordenamientos y filtros específicos. No introducir evaluaciones en memoria o consultas adicionales inadvertidas.

## Perfiles y flujo de negocio

Las capacidades se resuelven por patentes efectivas y controles del servicio, no por esta descripción resumida:

| Perfil | Responsabilidad existente |
|---|---|
| ADMIN | Administración de personas/cuentas, roles, familias/patentes, catálogos, supervisión, auditoría y reportes |
| CAJA | Clientes, vehículos, turnos, recepción con generación de OT, seguimiento, facturación, pagos, entrega y garantías; no asigna mecánicos |
| MECANICO | OT disponibles/asignadas, diagnóstico, evidencias, presupuesto, reparación y finalización; consulta técnica de repuestos |
| STOCK | Repuestos, proveedores, lotes, movimientos y existencias |
| CLIENTE | Datos y recursos propios, turnos, decisiones de presupuesto, facturas, garantías, calificación y seguridad de su cuenta |

Flujo principal: Persona/CLIENTE → vehículo/titularidad → turno → recepción/ingreso → OT → diagnóstico → presupuesto/versiones → aprobación o rechazo → repuestos/reparación → finalización → factura → pago → entrega → historial/postventa.

Asignación de trabajo: ADMIN asigna directamente una OT a un mecánico autorizado; MECANICO puede tomar voluntariamente una OT disponible y sin asignar. CAJA registra la recepción y genera la OT, pero no asigna mecánicos. No ampliar permisos para habilitarle esa acción.

Reutilizar servicios y transiciones existentes. Un cliente puede ser Persona con rol CLIENTE sin Usuario; no duplicarlo como otra entidad. Los pagos actuales son registro académico, no una pasarela bancaria. No inventar reglas para completar una pantalla.

## Autorización y seguridad

- Mantener `PermisoAttribute`, `PermisoService`, ownership y asignación de OT en servidor. No usar `User.IsInRole` como sustituto ni ampliar el bypass ADMIN.
- `PermisosVista` sólo resuelve presentación por petición/usuario; no sustituye autorización ni admite caché persistente de permisos. Las revocaciones deben reflejarse en nuevas peticiones.
- Conservar las protecciones de CLIENTE pendiente/inactivo, rol CLIENTE no administrable genéricamente y último ADMIN. No alterar permisos de CAJA para resolver problemas de UI.
- Preservar antiforgery, cookies, SecurityStamp, sesiones, rate limiting y CSP. No debilitar políticas para permitir scripts/estilos incompatibles.
- No fusionar ni vincular cuentas automáticamente por correo/DNI. Mantener verificación de titularidad, identidad Google estable y respuestas públicas neutras de los flujos sensibles.
- No imprimir ni versionar contraseñas, tokens, hashes, secretos, cadenas con credenciales, backups o datos personales. No cambiar User Secrets/SMTP ni enviar correos reales sin autorización; usar datos ficticios sólo en escenarios autorizados.

## Datos, migraciones e integridad

- `MecaniCar360` es una base de desarrollo/prueba, **no descartable por defecto**. Preservar íntegramente `TEST_CIERRE10_A123` y relaciones; no avanzar sus estados, limpiar ni reutilizar sus registros para escrituras sin autorización específica.
- Antes de cualquier escritura autorizada, confirmar instancia/base/entorno y alcance. Utilizar servicios de negocio; no saltar validaciones, auditoría ni DVH/DVV mediante SQL directo.
- Leer `docs/integridad-dvh-dvv.md` y `docs/backup-restore.md` para operaciones de datos. El coordinador EF mantiene DVH/DVV sobre las entidades registradas; no ampliar cobertura ni recalcular baseline para ocultar errores.
- Mantener orden de bloqueos: protección global de integridad antes de bloqueos de negocio. Lecturas decisivas y escritura deben quedar protegidas conjuntamente; considerar entidades precargadas y refresco bajo bloqueo. No introducir transacciones anidadas ni reutilizar contextos fallidos.
- Generar/aplicar migraciones, ejecutar seeds, inicializar integridad, borrar/recrear/restaurar bases o eliminar datos requiere aprobación explícita para esa operación. No editar migraciones históricas ni snapshot manualmente para silenciar diferencias.
- Iniciar la aplicación no es necesariamente una operación de sólo lectura: inspeccionar startup/seed antes de hacerlo bajo restricciones de datos. `--sin-escrituras` en la inicialización confirma una ventana exclusiva; **no significa dry run**.

## Pruebas y validación

Para cambios de código, ejecutar pruebas focalizadas y regresiones relevantes; antes de entregar, build y suite no SQL según el alcance. Desde la raíz:

```powershell
dotnet build MecaniCar360.csproj --configuration CierreB -p:UseAppHost=false -v:q
dotnet test tests/MecaniCar360.Tests/MecaniCar360.Tests.csproj --configuration CierreB -p:UseAppHost=false --filter 'FullyQualifiedName!~OnboardingSqlIntegrationTests' -v:q
git diff --check
```

- Usar `-t:Rebuild` cuando sea necesario medir todas las advertencias. La suite incluye comparación modelo/snapshot sin abrir SQL. No confundir ese resultado con migraciones aplicadas en una base.
- Para documentación únicamente, revisar contenido, enlaces y diff; no es necesario repetir build/tests sin cambios ejecutables.
- Agregar regresiones cuando aporten evidencia del defecto; no sustituir pruebas funcionales por búsquedas de texto presentadas como equivalentes.
- No ejecutar integración SQL ni habilitar variables de opt-in implícitamente. El harness `ConcurrencySetup`/`ConcurrencyTests` requiere autorización separada y su README: conexión exclusiva `MC360_CONCURRENCY_CONNECTION`, base aislada con GUID e identidades restringidas. Nunca apuntarlo a `MecaniCar360`.
- Los 28 escenarios SQL escritos no equivalen a pruebas ejecutadas. **B1 sigue siendo una limitación:** las barreras coordinan llamadas, no demuestran espera efectiva por bloqueo SQL. No añadir DMV/procedimientos firmados al cierre sin nueva solicitud.
- Distinguir PASS, FAIL y NO EJECUTADO; separar inspección estática, pruebas en memoria, SQL y navegador. No certificar responsive o enlaces reales sin ejecutarlos. Ausencia de pruebas no demuestra un defecto.
- Informar archivos cambiados, comandos/resultados, warnings nuevos o existentes y pendientes. No declarar terminada una tarea hasta satisfacer sus criterios de aceptación; si falta validación, identificarla expresamente.

## Autonomía, aprobación y Git

Continuar sin pedir confirmaciones repetidas para lecturas, cambios y validaciones ya autorizados. Detener la parte afectada y presentar evidencia, alternativa mínima e impacto cuando:

- Haga falta ampliar alcance, modificar otro módulo no autorizado, cambiar reglas/contratos, permisos, autenticación o esquema.
- Sea necesaria una operación de datos, migración, seed, limpieza, restore, modificación de secretos o interacción externa no autorizada.
- Una validación revele un riesgo concreto de seguridad/integridad o una dependencia impida probar con seguridad. No improvisar reparaciones ni simular garantías.

Preparar una solución concreta dentro del alcance antes de solicitar aprobación; no usar advertencias hipotéticas para detener trabajo seguro. Las instrucciones explícitas actuales del usuario prevalecen sobre este archivo.

No hacer commit ni push sin autorización explícita; autorización de commit no implica push. Revisar archivos nuevos y diff staged, excluir artefactos/secretos y conservar commits previos. No usar force push ni reescribir historial sin autorización específica. Registrar limitaciones aceptadas y evidencia en la documentación autorizada, sin abrir una nueva ronda indefinida de mejoras P3.
