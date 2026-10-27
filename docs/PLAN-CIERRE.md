# MecaniCar360 1.0 — Plan de cierre

## Estado vigente

Actualización documental: **27/10/2026**, código de referencia `master`, **`8e17456`**. El repositorio estaba limpio al comenzar esta tarea. Las correcciones de autorización P1 y el modo de aceptación ya están versionados; no son archivos locales pendientes. No se consultó el remoto ni se certifica su sincronización.

- Última ejecución registrada: build CierreB sin errores, 26 CS8618 existentes; **248 pruebas no SQL aprobadas**, 0 fallos, incluyendo 27 regresiones P1. Es evidencia de la sesión de corrección, no una ejecución del 27/10 ni una certificación E2E del commit actual.
- ADMIN efectivo asigna directamente; MECANICO toma OT disponibles; CAJA recibe y genera OT. Sólo CLIENTE habilitado propietario decide presupuestos; ADMIN conserva consulta autorizada.
- Pendiente: aceptación funcional y visual por perfiles, POST y persistencia real de los negativos P1, servicios externos y reproducibilidad de la guía local. Usar los IDs existentes de ACEPTACION, sin repetir auditorías generales.
- Los 28 escenarios SQL están escritos, **no ejecutados**; B1 permanece documentado. Su ejecución requiere autorización independiente. No se consultó la base ni se verificó su integridad actual en esta tarea.
- Esta actualización sólo revisó documentación y código fuente. No ejecutó build, tests, host, migraciones, seeds ni SQL.

Las secciones siguientes conservan inventario, tareas y antecedentes. Los resultados de 212/221 pruebas, referencias a `4ebf977`, commits locales y conclusiones de ausencia de P1 corresponden a sesiones anteriores; **no sustituyen este estado vigente**. Las matrices de aceptación siguen pendientes salvo evidencia individual registrada.

### Antecedentes históricos de preparación y corrección

> Actualizacion P1: la auditoria posterior identifico dos discrepancias de autorizacion (asignacion directa de OT y decision de presupuesto). En aquella sesion ambas quedaron corregidas localmente en los servicios, sin cambios en permisos ni datos. Build: 0 errores, 26 CS8618 existentes; suite no SQL: 248 aprobadas, incluidas 27 regresiones nuevas. Pendiente validacion SQL/HTTP autorizada de operaciones completas y ausencia de efectos ante rechazo. Esta actualizacion prevalece sobre las conclusiones historicas de ausencia de P1 de las secciones anteriores.

Fecha de actualización: 15/10/2026. Base inspeccionada: `master`, commit `4ebf977`, más cambios locales de arranque de aceptación.
Documento principal de entrega. Reemplaza como referencia de estado actual los recuentos y pendientes históricos de bloques; no reemplaza los procedimientos técnicos de backup e integridad. La matriz ejecutable está en [ACEPTACION-1.0.md](ACEPTACION-1.0.md); no duplicar sus casos.

### Evidencia historica de la revision del 15/10

**Avance autónomo completado el 15/10:** D4 documental preparado en [OPERACION-LOCAL-1.0.md](OPERACION-LOCAL-1.0.md); H02 corregido en seguridad-autorizacion/testing; matriz manual conservada y enlazada en ACEPTACION; 26 warnings clasificados en [ADVERTENCIAS-1.0.md](ADVERTENCIAS-1.0.md). No se justificó una corrección productiva adicional. Build nuevo CierreDoc: 0 errores/26 CS8618; suite no SQL: 221 PASS/0 FAIL/0 SKIP seleccionados; modelo/snapshot PASS. No se ejecutó SQL ni navegador. D4 sólo requiere incorporar resultados reales futuros y confirmación de reproducibilidad por otro operador.

- Build en configuración separada `PlanFinal`: **0 errores y 26 CS8618**.
- Suite no SQL actual: **221 PASS, 0 FAIL, 0 SKIP** entre los seleccionados; incluye nueve casos de `ArranqueAceptacionTests`. Modelo/snapshot alineados sin conectar a SQL.
- Se conservaron los cambios locales de `Program.cs`, `Services/ArranqueIntegridad.cs`, sus pruebas y los documentos existentes. Sólo se actualiza este plan.
- Evidencia de la sesión anterior: aplicación en Development, `http://localhost:5190/Account/Login` respondió 200 con modo aceptación e integridad habilitada. No es una prueba de login ni de recorridos autenticados; no se reinició ni se certificó nuevamente el proceso en esta revisión.
- `Arranque:ModoAceptacion=true` omite InicializadorBD (migraciones, seed, bootstrap y catálogo), conserva verificaciones DVH/DVV y bloqueo 503. El modo normal sigue inicializando. No es un modo global de sólo lectura: las operaciones funcionales pueden escribir.
- No se conectó a SQL ni se alteró TEST_CIERRE10_A123. Los 20 archivos de migración, sus designers y snapshot están presentes; su aplicación efectiva actual no fue consultada.

Comandos actuales: `dotnet build MecaniCar360.csproj --configuration PlanFinal -p:UseAppHost=false -v:q` y `dotnet test tests/MecaniCar360.Tests/MecaniCar360.Tests.csproj --configuration PlanFinal -p:UseAppHost=false --filter 'FullyQualifiedName!~OnboardingSqlIntegrationTests' -v:q`. El filtro evita una prueba que puede escribir en SQL; no se ejecutan los 28 escenarios del harness.

## 1. Decisión ejecutiva

**Detener las refactorizaciones generales. Completar una aceptación finita por perfiles y cerrar la documentación operativa. Modelo/Detalle ya está implementado.**

El sistema tiene implementado el recorrido del taller y sus módulos administrativos. Esto no equivale a afirmar que todas las pantallas funcionan en navegador o que todas las carreras fueron probadas en SQL. La revisión actual confirma que Modelo/Detalle existe y retira ese hallazgo anterior. Continúa la documentación histórica desactualizada; no demostró un defecto P0 ni una vulnerabilidad/inconsistencia P1 nueva.

El trabajo obligatorio restante estimado es **13–24 horas-persona**, más una reserva condicionada de 2–4 horas sólo si aparecen defectos reproducibles. Supone disponibilidad de cuentas ficticias autorizadas, navegador y responsables del entorno SQL/correo. Las esperas de permisos o consentimiento no están incluidas. Si aparece un P0/P1, se estima y corrige ese defecto; no se abre una refactorización general.

Antecedente historico: se conservaron entonces los cinco commits locales: `466755c`, `031c1df`, `2103734`, `0227695`, `229c67d`. No se publicaron durante este relevamiento.

## 2. Alcance, evidencia y límites

Se inventariaron 32 controllers, 132 archivos Razor (incluidos layouts y partials), servicios, rutas/atributos, DI, navegación, CSS/JavaScript locales, pruebas, scripts y documentación. Se revisaron especialmente los puntos de conexión del CORE, permisos, facturación/entrega y el harness. Es una revisión estática orientada al cierre, no una prueba exhaustiva de cada rama ni una auditoría de penetración.

Evidencia historica obtenida en la sesion del 15/10:

| Comprobación | Resultado | Qué demuestra / qué no demuestra |
|---|---|---|
| Build de `MecaniCar360.csproj`, configuración PlanFinal | 0 errores; 26 CS8618 | Compilación C#/Razor; no prueba renderizado con datos |
| Suite existente excluyendo `OnboardingSqlIntegrationTests` | 221 PASS, 0 FAIL, 0 SKIP entre los seleccionados | Unitarias, contratos, metadatos, pruebas estructurales y HTTP en memoria; no 221 recorridos E2E |
| `ModeloActual_ContraSnapshot_NoTieneCambiosPendientes` | PASS, conexión cerrada | `HasPendingModelChanges=false`; no prueba esquema aplicado en una base |
| Nueve casos de precisión monetaria | PASS | Mapeo EF `decimal(18,2)`; no ensayos de redondeo/overflow contra SQL |
| Migraciones en código | 20 más snapshot | Historial disponible; aplicación efectiva no consultada aquí |
| Navegador, login real, SMTP, Google real | NO EJECUTADO en esta revisión | No se atribuye validación visual ni de proveedores externos |
| Concurrencia SQL aislada | 28 casos escritos, NO EJECUTADOS | B2/B3 implementados; B1 sigue limitado |
| Base `MecaniCar360` y DVH/DVV actuales | NO CONSULTADOS | Se conserva el estado; no se certifica integridad actual sin lectura real |

No se inició el host web: su startup verifica integridad y puede ejecutar seed. No se ejecutaron migraciones, SQL, seeds ni operaciones de negocio. La base habitual es **de prueba**, no productiva. `TEST_CIERRE10_A123` y sus relaciones deben permanecer intactos, incluso durante las próximas sesiones: usar lectura sobre él o un escenario separado autorizado para mutaciones. No reutilizar su OT para completar un recorrido que altere sus estados sin autorización específica.

Clasificación:

- **P0:** impide ejecutar o utilizar el sistema.
- **P1:** fallo crítico, vulnerabilidad o riesgo de integridad demostrado.
- **P2:** funcionalidad obligatoria incompleta; incluye documentación indispensable para operar. Las tareas de aceptación se identifican aparte como validación pendiente, no como defectos P2 ficticios.
- **P3:** mejora opcional/deuda no bloqueante.

Tipos de evidencia: **confirmado estáticamente**, **verificado por prueba ejecutada**, **implementado pendiente de aceptación**, **incompleto**. Ausencia de pruebas no implica automáticamente defecto.

## 3. Inventario funcional y perfiles

La matriz refleja controllers/services y asociaciones de `Data/InicializadorBD.cs`, no una lectura de las asignaciones actuales de la base. La autorización real depende de patentes efectivas, estado de cuenta, ownership y asignación de OT, no sólo del nombre del rol. Mantener las capacidades actuales de CAJA; no recortarlas ni ampliarlas como parte del cierre.

Regla definitiva de asignacion: CAJA recibe y genera OT sin asignar; MECANICO toma OT disponible sin asignar; solo ADMIN efectivo asigna directamente. Correccion P1 implementada localmente: patente y perfil activo se comprueban antes de la operacion y el perfil se revalida bajo transaccion. Pendiente aceptacion SQL/navegador, sin cambios en seed ni permisos.

| Perfil | Entrada / pantallas principales | Funciones implementadas | Aceptación pendiente |
|---|---|---|---|
| ADMIN | `Account/Inicio` → `Dashboard/Index`; `Usuario/Administracion`, `Persona`, `Rol`, `Seguridad`, `Auditoria`, `Reportes` | Personal/cuentas, roles internos, familias/patentes, catálogos, supervisión de operaciones y consulta administrativa | Navegación y acciones; protección último ADMIN/CLIENTE; no mostrar portal cliente si no corresponde |
| CAJA | `Inicio/Caja`; `Persona/Clientes`, `Turno`, `IngresoVehiculo`, OT, `Factura`, `Garantia` | Alta presencial, invitaciones según permisos, vehículos, agenda, recepción con generación de OT, seguimiento, facturación, cobros, entrega y garantías; no asigna mecánicos | Un recorrido completo con transiciones válidas y rechazos; formularios recuperan valores; asignación denegada |
| MECANICO | `Inicio/Mecanico`; OT pendientes/asignadas/detalle, presupuesto, `Stock/ConsultaTecnica` | Tomar OT, diagnóstico, evidencias, presupuesto/versiones, reparación y finalización según asignación | Sin datos comerciales restringidos; OT ajena denegada; selector de repuestos y mensajes |
| STOCK | `Inicio/Stock`; `Stock`, movimientos/proveedores/lotes | Alta sin stock inicial, proveedores, ingreso/salida/ajuste y trazabilidad | SKU duplicado, estados activos/inactivos, mensajes, conciliación lote/movimiento/existencia |
| CLIENTE | `PortalCliente/Index`; MisDatos, vehículos, turnos, órdenes, presupuestos, facturas, garantías | Registro/invitación/Google, completar datos, credenciales duales, enlaces de contraseña, operaciones propias, decisiones de presupuesto y calificación | Propiedad de recursos, sesión pendiente/inactiva, enlaces reales, presentación móvil |

Referencias: `AccountController.DestinoInicialAsync`, `InicioController`, `InicioOperativoService`, `PermisoService`, `PermisosVista`, `PresentacionCuentaService`, `ClienteHabilitadoService`, `Attributes/PermisoAttribute.cs` y `Views/Shared/_Layout.cshtml`.

### Inventario por módulo

| Módulo / controllers | Servicios y pantallas | Estado de implementación |
|---|---|---|
| Acceso: Account, GoogleCliente, RegistroCliente, RegistroCompletoCliente, ActivacionCliente, SeguridadCliente, InvitacionCliente | Servicios homónimos, AccountService, IdentidadClienteService, EmailService; vistas de login/onboarding/enlaces | Implementado; endpoints de invitación/callback no requieren vista propia; integración real no repetida |
| Inicio, Dashboard, Home | InicioOperativoService, DashboardService; Operativo, Dashboard, Home/Error | Entradas por patentes; Home conserva plantilla como fallback de cuenta sin destino |
| Persona, Usuario | AltaClienteService, AltaPersonalService, PersonaService, UsuarioService; Clientes/Cliente, NuevoCliente, Administración/PersonaCuenta/NuevoPersonal | Implementado; Persona CLIENTE puede existir sin Usuario; no confundir con alta interna |
| Rol, Seguridad | RolService, SeguridadService; roles, familias, patentes y asociaciones | Implementado; CLIENTE protegido del circuito genérico; aceptación de controles pendiente |
| Marca, Modelo | MarcaService, ModeloService; listas/altas/edición, Marca/Detalle | Implementado: Modelo/Detalle existe y está versionado en 4ebf977; renderizado autenticado pendiente |
| Vehiculo | VehiculoService, DominioVehicularService, VehiculoPropioAltaService; CRUD interno y portal | Implementado con titularidad; comprobar selección Marca/Modelo y ownership |
| Turno | TurnoService, AgendaService; Index con CalendarioViewModel, Agenda, Crear, Detalle, horarios JSON | Implementado; alta y transiciones con correcciones locales ya commiteadas; SQL pendiente |
| IngresoVehiculo | IngresoVehiculoService; Registrar, EnTaller, Detalle | Implementado; validación de recepción, creación OT y refresco del turno |
| OrdenTrabajo, Diagnostico | OrdenTrabajoService, DiagnosticoService, State; Index/Pendientes/DeMecanico/Detalle/MecanicosDisponibles | Implementado; diagnóstico embebido, acciones/historial auxiliares no necesitan páginas independientes |
| Presupuesto | PresupuestoService; Detalle, Propio, ErrorOperacion | Implementado; edición, versiones, decisión y stock; probar ambos caminos de decisión |
| Stock, Proveedor | StockService y parciales, ProveedorService; listas, detalles, formularios, movimientos, consulta técnica | Implementado; restricciones comerciales diferenciadas |
| Factura | FacturaService y estrategias de pago; Index, Detalle, MisFacturas, Pagos, RegistrarPago | Implementado; pago registrado académicamente, no pasarela bancaria real |
| Garantia, Calificacion | GarantiaService, CalificacionTrabajoService; vistas internas/propias | Implementado; cobertura por ítems, vigencia/anulación, calificación propia |
| Evidencia | EvidenciaTrabajoService, AlmacenEvidencias; Crear, archivo autorizado y partial compartido | Implementado; archivos privados y relación con OT/diagnóstico/versiones |
| Notificacion | NotificacionService y eventos; Index, marcar leída, navegación al recurso | Implementado; verificar ownership y enlaces por perfil |
| Auditoria, Reportes | AuditoriaService, ReportesService, ReportePdf; consulta y exportación | Implementado; permisos y resultados vacíos por validar en sesión final |
| Backup/restore e integridad | `scripts/`, `tools/Integridad`, ArranqueIntegridad, EstadoIntegridad y Data/Integridad | Herramientas técnicas documentadas; **no falta un controller web** |

## 4. Cobertura del flujo completo del taller

| Paso | Conexión existente / evidencia principal | Criterio de aceptación pendiente |
|---|---|---|
| Cliente y vehículo | Persona/Cliente → Vehiculo/Crear; DominioVehicularService; portal propio | Titular correcto, catálogo activo, DNI normalizado/único; no crear otra Persona por reutilizar un correo |
| Turno | Crear interno/propio → AgendaService; TurnoService.CrearInternoAsync | Cupo, vehículo activo, fecha y estados válidos; confirmar/cancelar/reprogramar/ausencia con mensajes coherentes |
| Recepción | Turno → IngresoVehiculo/Registrar → RegistrarIngresoYCrearOrdenAsync | Datos requeridos, kilometraje/combustible/exterior/cliente; exactamente un ingreso y una OT; turno finalizado |
| OT | Pendientes → MECANICO toma una OT disponible sin asignar o ADMIN asigna directamente → Detalle | CAJA sólo genera la OT al recibir; no asigna. Mecánico habilitado; una asignación; estados/historial; rechazo no deja auditoría de éxito |
| Diagnóstico y evidencias | Formularios en OrdenTrabajo/Detalle → DiagnosticoController; EvidenciaController | Mecánico asignado, descripción e historial; acceso a archivos autorizado |
| Presupuesto | OT → Crear/Detalle → ítems/repuestos → Enviar versión | Consulta técnica sin PROVEEDOR_VER; cantidades/precios válidos; errores dentro de la UI |
| Aprobación | Portal → Presupuesto/Propio → Aprobar/Rechazar | Sólo propietario habilitado o excepción ADMIN existente; versión vigente; rechazo exige motivo |
| Existencias | PresupuestoService.DecidirAsync → StockService.ProcesarAprobacionIncrementalAsync | Movimientos y lotes coherentes, sin doble consumo; insuficiencia según comportamiento actual, sin inventar reglas |
| Reparación/finalización | OrdenTrabajo/Detalle → cambio de estado y FinalizarAsync | Transición permitida, horas reales, rechazo si faltan requisitos; historial consistente |
| Factura | OT finalizada/rechazada → FacturaService.EmitirAsync | Una factura por OT; versión aprobada o costo de diagnóstico; conceptos/importes consistentes |
| Pago | Factura/RegistrarPago → RegistrarPagoAsync | Pago parcial/total, saldo esperado, rechazo de sobrepago o saldo obsoleto; no cobro externo real |
| Entrega | OrdenTrabajoService.EntregarAsync | Factura pagada y suma cubierta, estado Finalizado/Rechazado; egreso único; no entrega repetida |
| Postventa/historial | Garantía por factura/vehículo; calificación; historial de OT/diagnóstico/presupuesto, auditoría | Cliente sólo propio; vigencia/cobertura correctas; historial consultable y notificaciones navegables |

La conexión funcional está presente en código. La aceptación final debe cubrir **un camino aprobado y uno rechazado**; no se necesita repetir todas las campañas históricas ni construir un segundo sistema E2E.

## 5. Interfaz, CSS y navegación

Base existente: Bootstrap/jQuery locales, `_Layout.cshtml`, CSS aislado `_Layout.cshtml.css`, `wwwroot/css/site.css`, `enlace-seguro.css`, `_EnlaceSeguroHead`, scripts de login y contraseña, scripts Razor de horarios/recepción. El layout tiene viewport, menú colapsable, salto al contenido, patentes visuales y acceso por Account/Inicio. Las vistas de enlaces seguros mantienen aislamiento y recursos locales.

Correcciones anteriores de tablas responsive, etiquetas y retornos están representadas en Razor y pruebas (`TablasResponsiveTests`, `EnlaceSeguroPresentacionTests`, `NavegacionCierreTests`, `RecursosLayoutTests`, `StockPresupuestoRegresionTests`). **Estas pruebas no miden ancho real, foco, contraste ni clics con sesión.** No declarar V1–V18 cerrados sólo por compilar; tampoco reabrir todos como defectos sin reproducción.

Sesión visual finita:

1. A 1366 px, recorrer la entrada de los cinco perfiles y el CORE; verificar títulos, retorno, permisos, tablas, vacíos y errores.
2. A 320 y 375 px, comprobar layout, login/enlaces seguros, Turno/Crear, recepción, OT/Detalle, presupuesto, stock, pago y portal. Se admite scroll horizontal dentro de tablas; no controles esenciales inaccesibles ni scroll horizontal global evitable.
3. A 768 px, repetir menú, tablas y formularios representativos; no repetir todo el CORE.
4. Zoom 200 %, teclado y foco visible en menú, modal/formularios usados, decisiones de presupuesto y seguridad de cuenta. Etiquetas persistentes y mensajes asociados.
5. Consola/red: ningún error JS/CSP que impida el recorrido; antiforgery vigente; tokens de enlace fuera de query string, fragmento eliminado sin debilitar CSP.

Si una pantalla no pudo abrirse por falta de cuenta/datos, marcar NO EJECUTADO. No sustituirlo por PASS estático. Corregir sólo problemas reproducidos que obstaculicen la tarea, no homogeneizar cada margen/color.

## 6. Hallazgos y prioridades

| ID | Prioridad / naturaleza | Evidencia y ubicación | Acción mínima / aceptación | Esfuerzo |
|---|---|---|---|---|
| H01 | Cerrado como supuesto defecto | `Views/Modelo/Detalle.cshtml` existe y está versionada; `ModeloController.Detalle` devuelve el modelo compatible. HEAD 4ebf977 ajusta la vista. | No implementar otra vista. Sólo falta GET autenticado desde catálogo y retorno, dentro de aceptación | Incluido en D2 |
| H02 | **Corrección documental completada 15/10** | seguridad-autorizacion actualizado a DASHBOARD_VER; testing distingue checkpoint histórico de suite vigente; OPERACION-LOCAL-1.0 reúne arranque/perfiles/diagnóstico | Pendiente sólo ensayo manual de la guía y resultados futuros; no cambiar permisos | Incorporación final en D4 |
| H03 | **Validación obligatoria pendiente, no defecto P1** | Harness `tests/MecaniCar360.ConcurrencyTests/SqlConcurrencyTests.cs`, 28 casos; A1/A2/A3/recepción y pruebas estructurales existentes | Ejecutar una campaña aislada autorizada; separar fallos de preparación/harness de defectos productivos. Aceptar B1 documentado: resultados coordinados no demuestran espera SQL efectiva. | 4–7 h con entorno disponible |
| H04 | **Validación obligatoria pendiente, no funcionalidad faltante** | Controllers/Views del CORE y portal; 221 pruebas no abren la aplicación completa con los cinco perfiles | Sesión E2E/seguridad/UI de D2, checklist de sección 8; registrar evidencias mínimas sin secretos | 7–12 h entre D1/D2 |
| H05 | **P3, advertencias confirmadas** | 26 CS8618 en Models/Clases, principalmente navegaciones y strings; rebuild sin errores | Documentar. Corregir sólo si aparece una dereferencia inválida reproducible; no cambiar nulabilidad persistida ni introducir `!` masivo | Fuera del cierre |
| H06 | **P3, precisión implícita / límite conocido** | `tests/MecaniCar360.Tests/ModeloEfTests.cs`, nueve mapeos decimal(18,2) | No migrar para silenciar warnings. Incluir importes fraccionarios ordinarios y rechazo de importes inválidos en aceptación; elevar sólo si se demuestra pérdida/cálculo incorrecto | Incluido en D2; rediseño fuera |
| H07 | **P3, fallback de inicio** | AccountController.DestinoInicialAsync termina en Home/Index si no hay capacidades; Views/Home/Index.cshtml ofrece un mensaje y acceso al login | Comprobar que no bloquea salir/iniciar de nuevo; no crear una pantalla nueva en el camino crítico | Fuera del cierre |
| H08 | **P3, detalle semántico estático** | `Views/IngresoVehiculo/Registrar.cshtml:58`: dos radios `asp-for="EstadoExterior"` sin IDs diferenciados; pueden generar el mismo id, aunque cada input está envuelto por label | Validar teclado/etiquetas; IDs únicos es corrección puntual conveniente si se interviene esa vista, sin cambiar name/POST | 0,25–0,5 h opcional |

**P0 confirmados: ninguno. P1 nuevos demostrados: ninguno.** Esto no es garantía de ausencia universal. H01 está retirado como defecto; queda probar la navegación de catálogos; H02 antes del traspaso operativo. H03/H04 son puertas de aceptación, no prueba de que haya corrupción ni módulos sin implementar.

No son faltantes de 1.0: pasarela bancaria real, observador DMV, procedimientos firmados, nuevos dashboards, nuevas patentes, caché persistente, consolidación de migraciones, UI web de restore ni ampliación de las doce entidades DVH/DVV. No incorporarlos al camino crítico.

## 7. Plan finito y dependencias

Cada bloque termina con evidencia y una decisión PASS/FAIL/NO EJECUTADO. Responsable: desarrollador para D0/D3/D4; usuario/operador para cuentas, consentimiento y aceptación; administrador SQL para permisos aislados. Una misma persona puede cubrir varios roles.

| Bloque | Trabajo obligatorio | Depende de | Salida verificable | Horas-persona |
|---|---|---|---|---:|
| D0 | Modelo/Detalle ya implementado; no más desarrollo | Resuelto en 4ebf977 | Queda renderizado autenticado en D2 | 0 |
| D1 | Confirmar instancia de aceptación, cuentas y fixture separado autorizado | Operador y autorización de escrituras | IDs y alcance; TEST_CIERRE10_A123 intacto | 1–2 |
| D2 | Matriz funcional/visual de ACEPTACION-1.0.md, CORE aprobado/rechazado y negativos | D1 y navegador | Evidencia por perfil, sin confundir estático y ejecución | 6–10 |
| D3 | Preparar base aislada y ejecutar una vez los 28 casos funcionales concurrentes; revisar persistencia e integridad | Autorización separada; dos identidades y permisos del README | 28 resultados reales; B1 explícito; timeout/deadlock no contado como éxito | 4–7 |
| D4 | Guía, permisos, warnings y preparación manual/SQL completados; incorporar resultados reales futuros | Documentación disponible; final depende de D2/D3 | Operador confirma reproducibilidad y resultados trazables | 1–2 restantes |
| D5 | Build, suite no SQL, revisión de evidencia y cierre local | D2/D3/D4 | Cero defectos P0/P1/P2 obligatorios abiertos | 1–3 |
| D6 | Consolidación/publicación posterior, sólo con autorización | D5 | No es requisito para una versión local funcional | Fuera de esta estimación |

**Total local restante: 13–24 h**, más reserva condicionada de 2–4 h para defectos demostrados. D6/publicación queda fuera del cierre local y requiere autorización. D3 puede ejecutarse antes o después de D2 y no usa sus datos. Si faltan permisos, registrar bloqueo y resolverlo; no desarrollar observadores ni reinstalar desarrollo para acortar esa espera.

Política de parada: corregir el defecto acotado, repetir el caso que falló y sus regresiones directas, no reiniciar todas las auditorías. Si la reserva no alcanza, informar los defectos nuevos y su costo antes de extender alcance. No repetir manualmente campañas antiguas sin cambio relevante: aceptar evidencia histórica trazable al código vigente, dejando claro su origen.

## 8. Checklist de aceptación obligatoria

Registrar para cada fila: fecha, commit, perfil, entorno/IDs ficticios, resultado, evidencia y defecto asociado. No almacenar contraseñas, tokens, hashes ni datos personales reales. Capturas sólo sanitizadas. Esta tabla comienza pendiente; los PASS automatizados de sección 2 no la completan automáticamente.

| ID | Caso | Aceptación |
|---|---|---|
| AC01 | Login/logout cinco perfiles | Destino permitido; cuenta inválida/inactiva rechazada; logout POST; acceso anónimo bloqueado |
| AC02 | Autorización/ownership | MECANICO no consulta datos comerciales ni modifica OT ajena; CLIENTE no obtiene recursos ajenos; STAFF sin patente recibe denegación, no éxito oculto |
| AC03 | Cuentas y permisos | Alta interna/primer ingreso; CLIENTE fuera de asignación genérica; último ADMIN protegido; revocación efectiva en nueva petición; no mezclar persona pendiente con cuenta habilitada |
| AC04 | Onboarding/seguridad cliente | Invitación/registro local y Google según estado; misma cuenta al vincular legítimamente; correo existente no duplica; enlaces vencidos/usados/manipulados rechazados; password invalida sesiones; sólo usuario ejecuta consentimiento Google |
| AC05 | Cliente → vehículo → turno → recepción | Relaciones correctas, Marca/Modelo visibles, catálogo detalle funcional, cupo/estado y datos obligatorios; un ingreso/OT |
| AC06 | OT y diagnóstico | Asignación directa por ADMIN o toma voluntaria por MECANICO de OT disponible sin asignar; CAJA sin asignación. Iniciar/guardar diagnóstico, evidencias, historial y permisos coherentes |
| AC07 | Presupuesto aprobado | Repuestos técnicos, cantidades/precios, envío, decisión vigente, stock/lotes/movimientos, reparación/finalización |
| AC08 | Presupuesto rechazado | Motivo, bloqueo de reparación incompatible, costo diagnóstico válido, facturación del rechazo sin cobrar repuestos no aprobados |
| AC09 | Factura/pago/entrega | Factura única, saldo parcial/total, rechazo sobrepago, no entrega antes de pago, egreso único y consulta posterior |
| AC10 | Stock A1–A3 y navegación B1–B11 de cierre UI | Alta con stock cero, SKU duplicado/nombre inválido, edición conserva valores; inactivos sin enlaces rotos; botones por patente |
| AC11 | Postventa/transversales | Garantía y calificación propias, notificaciones al recurso correcto, reportes/PDF, auditoría legible y sin secretos |
| AC12 | Interfaz por perfiles | Anchos y zoom de sección 5; foco/labels, mensajes, vacíos, sin 500/errores JS que interrumpan tareas |
| AC13 | Integridad | DVH/DVV válidos antes/después de escrituras y rechazos en escenario autorizado; sin SQL manual sobre tablas protegidas |
| AC14 | Concurrencia | 28 resultados persistidos del harness; 2 contextos operativos y lectura independiente; fallos de infraestructura identificados; B1 no declarado resuelto |
| AC15 | Operación/documentación | Arranque reproducible, claves externas identificadas sin valores, backup SQL + evidencias, procedimiento de recuperación e incidente comprensible |

AC04 puede apoyarse en las pruebas manuales históricas del bloque 12C.1 si el responsable confirma evidencia suficiente y ausencia de cambios posteriores relevantes. Si no hay evidencia conservada, repetir sólo un ciclo representativo y los rechazos de mayor riesgo. Restore histórico documentado puede aceptarse como antecedente: no ejecutar un restore destructivo sobre `MecaniCar360`; cualquier nueva demostración será aislada y autorizada.

## 9. Documentación operativa mínima

Existe documentación útil en [backup-restore](backup-restore.md), [integridad-dvh-dvv](integridad-dvh-dvv.md), [seguridad-autorizacion](seguridad-autorizacion.md), [testing](testing.md), [validación 12C.1](../tests/MecaniCar360.Tests/VALIDACION-12C1.md) y [harness SQL](../tests/MecaniCar360.ConcurrencyTests/README.md). La entrada operativa ya está preparada en [OPERACION-LOCAL-1.0.md](OPERACION-LOCAL-1.0.md), con distinción de evidencia histórica y actual.

La guía D4 cubre los puntos siguientes; falta su ensayo por otro operador y anexar resultados reales:

- SDK/runtime .NET, SQL Server, plataforma/rutas y revisión de paquetes; versiones efectivas obtenidas del proyecto, no copiar valores históricos sin comprobar.
- Configuración externa requerida: `ConnectionStrings:DefaultConnection`, bootstrap ADMIN, SMTP, `Invitaciones:UrlBase`, configuración Google si se usa, directorios privados de evidencias/incidentes y sus ACL. Enumerar claves desde sus consumidores sin publicar valores; no exportar User Secrets.
- Instalación nueva: base destino confirmada → migraciones existentes → backup verificado y baseline explícita según guía → arranque normal/seed y verificación. `--sin-escrituras` confirma una ventana exclusiva: **no es dry run**. No usar este procedimiento para reinicializar la base habitual durante el cierre.
- Arranque cotidiano: comprobar esquema/integridad, iniciar versión identificada, HTTP Login y login por perfil; detener/investigar ante 503 de integridad, no recalcular baseline para ocultarlo.
- Flujo rápido de cada rol, restricciones y cómo identificar un rechazo de negocio frente a incidente técnico. Pagos son registro académico, no cobro bancario/fiscal homologado.
- Backup/restauración: preservar también archivos de evidencias, secretos/configuración y versión de código por vías seguras; responsables y ubicación de incidentes. Mantener scripts existentes; no construir una consola nueva.
- Evidencia de pruebas asociada al commit final, 26 warnings conocidos, límites de integridad y de B1; soporte básico para SMTP, OAuth y permisos.

### Comandos de la ejecucion historica (no repetidos el 27/10)

Desde la raíz; no inician la aplicación ni cargan la base real:

```powershell
dotnet build MecaniCar360.csproj --configuration CierreB -p:UseAppHost=false -t:Rebuild -v:q
dotnet test tests/MecaniCar360.Tests/MecaniCar360.Tests.csproj --configuration CierreB -p:UseAppHost=false --filter 'FullyQualifiedName!~OnboardingSqlIntegrationTests' -v:q
git diff --check
```

El caso SQL excluido no se contabiliza como PASS ni como SKIP del subconjunto. No usar `dotnet test` indiscriminadamente con variables de integración activas. Para D3 seguir exclusivamente el README del harness y su conexión aislada; no ejecutar sus comandos como parte del build normal.

## 10. Condiciones de terminado y fuera de alcance

Declarar MecaniCar360 1.0 terminado sólo cuando:

1. H01 retirado por existir la vista y H02 documental cerrado; ningún P0/P1 confirmado ni P2 obligatorio abierto.
2. AC01–AC15 tienen PASS/evidencia o una aceptación explícita y justificada del responsable para una limitación no crítica; un defecto crítico no se acepta como simple pendiente.
3. CORE aprobado y rechazado utilizables por perfiles previstos; ownership, antiforgery, cuentas y permisos conservados.
4. Build sin errores, suite no SQL verde y modelo/snapshot alineados. Warnings CS8618/precisión documentados no obligan a una migración/refactorización.
5. Campaña funcional concurrente aislada registrada. **B1 puede permanecer documentado**: no se promete demostración de espera SQL ni ausencia universal de deadlocks. Si la campaña no se ejecuta, la concurrencia continúa NO VALIDADA y requiere decisión explícita de entrega; no afirmar cierre de pruebas.
6. Sin cambios en TEST_CIERRE10_A123; cualquier fixture nuevo tiene autorización/identificación y no se limpia automáticamente.
7. Guía operativa y recuperación disponibles, secretos fuera de Git, commit final identificable y publicación sólo con autorización.

**Fuera del camino crítico 1.0:** refactorización general, reducción de todos los warnings, cambios de arquitectura, eliminación de patrones académicos, observación SQL avanzada, nuevas entidades/migraciones, rediseño integral de interfaz, optimización de consultas sin medición, consolidación del historial EF, nuevas integraciones o funcionalidades opcionales.

La próxima acción es completar D1/D2 con la matriz de ACEPTACION-1.0.md. D0 ya está resuelto. Ejecutar D3 sólo con autorización aislada y actualizar D4; no comenzar otra auditoría integral.

## 11. Bloques independientes y criterio de intervención

- **D4 documental** ya preparado: Dashboard/DASHBOARD_VER corregido, arranque normal/aceptación y evidencia vigente documentados. Sólo incorporar resultados finales y ensayo del operador después de D2/D3.
- **D3 concurrencia aislada** es independiente de los datos de aceptación. Requiere autorización separada, nunca utiliza MecaniCar360 y no resuelve B1 por sí mismo.
- **D2 consulta/visual** puede dividirse por perfil con sesiones disponibles; el CORE mutable debe respetar cliente → vehículo → turno → recepción → OT → presupuesto → decisión → factura/pago/entrega. Necesita fixture autorizado distinto del preservado.
- **Correcciones productivas** sólo se abrirán por defecto reproducible. No hay un nuevo lote demostrado que justifique implementar funcionalidades o refactorizar ahora.
- **D5 cierre local** depende de los resultados anteriores. Commit/push no son requisitos para funcionamiento local y necesitan autorización específica.

Clasificación final: P0 nuevos confirmados = ninguno; P1 nuevos demostrados = ninguno; P2 obligatorio = documentación operativa y aceptación pendiente (esta última no es defecto funcional); P3 = warnings, precisión implícita y detalles visuales no bloqueantes sujetos a reproducción. No se demostró una funcionalidad obligatoria ausente ni una ruta esencial sin acción. La ausencia de prueba no certifica que el recorrido falle ni que funcione.
