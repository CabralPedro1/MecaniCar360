# MecaniCar360 1.0 — aceptación funcional y visual

## Estado vigente — 27/10/2026

Referencia inspeccionada: `master`, `8e17456`. Las correcciones P1 y el arranque de aceptación están versionados. Última ejecución registrada: 248 pruebas no SQL aprobadas, 0 fallos; build sin errores y 26 CS8618 existentes. No se repitió en esta actualización documental.

Los resultados de 212/221 pruebas y las observaciones de navegador/puerto de octubre que siguen son **históricos**, no describen la disponibilidad actual del entorno. D01 y D02 documentales están resueltos; no requieren reimplementación. Permanecen pendientes los recorridos completos S/M, la validación SQL/HTTP de P1 y las comprobaciones visuales V01/V02. Ninguna fila pasa a PASS por esta edición. Los 28 escenarios de concurrencia siguen sin ejecución; B1 no está resuelto.

## Antecedente historico de preparacion — 15/10/2026

La matriz siguiente continúa siendo la referencia por perfil; no se repitieron recorridos reales ni se cambian sus estados S/M a PASS. Guía breve para el operador: [OPERACION-LOCAL-1.0.md](OPERACION-LOCAL-1.0.md), con arranque de aceptación, pasos por los cinco perfiles, resultados esperados, diagnóstico y autorización SQL aislada. Clasificación de las 26 advertencias: [ADVERTENCIAS-1.0.md](ADVERTENCIAS-1.0.md).

Evidencia nueva: build CierreDoc 0 errores/26 CS8618; suite no SQL 221 PASS/0 FAIL/0 SKIP seleccionados, incluyendo modelo/snapshot. Evidencia histórica separada: el 14/10 arrancó una instancia de aceptación y GET Login dio 200; no se certifica que siga activa hoy. No hubo escrituras de base ni pruebas de usuario en esta actualización.

D01 documental se resuelve en PLAN-CIERRE (Modelo/Detalle ya existe); D02 se corrigió en seguridad-autorizacion (DASHBOARD_VER). Permanecen V01/V02 y todos los recorridos de navegador pendientes de reproducción. Para retomar: confirmar instancia/cuentas y autorizar escenario mutable separado, luego seguir IDs existentes; no rehacer el análisis.

Fecha: 14/10/2026. Referencia comprobada: `master`, `4ebf977` (`fix: ajusta vista de detalle de modelos`). Primera pasada de aceptación, **no acta de aprobación final**.

## 1. Resultado historico y limites de la pasada del 14/10

- **Ejecutado:** suite no SQL, 212 PASS, 0 FAIL, 0 omitidas entre las seleccionadas; build sin errores; comprobación modelo/snapshot incluida y aprobada; `git diff --check` correcto antes de redactar.
- **Revisado estáticamente:** correspondencia de recorridos del plan con rutas, permisos, vistas y componentes existentes; contratos de navegación cubiertos por pruebas; detalle de Modelo, entradas por perfil, portal y marcado responsive relevante.
- **Navegador: NO EJECUTADO.** La herramienta devolvió `apps: []`, `browsers: []`. No se tomaron capturas ni se midieron anchos, contraste o foco real.
- No se detectó un listener en el puerto 5190. No se inició el host: `Program.cs` llama a `ArranqueIntegridad` e `InicializadorBD`, por lo que iniciar no garantiza ausencia de escrituras. No se realizaron peticiones HTTP a la aplicación real ni login de perfiles.
- No se consultaron ni modificaron datos de SQL. No se verificaron asignaciones actuales de cuentas, integridad actual de la base ni migraciones aplicadas. No se crearon fixtures ni se avanzaron estados de `TEST_CIERRE10_A123`.
- Los 28 escenarios SQL siguen **NO EJECUTADOS**; B1 permanece como limitación conocida. No se infiere un defecto por falta de ejecución.
- No hay nuevos P0/P1 funcionales demostrados en esta pasada. La aceptación E2E de los cinco perfiles continúa pendiente.

**Corrección de referencia:** H01/D0 de `PLAN-CIERRE.md` no representan el estado actual. `Views/Modelo/Detalle.cshtml` existe, está versionada y fue ajustada en `4ebf977`. Su contrato con `ModeloController.Detalle` es coherente estáticamente; aún falta abrirla con datos en navegador. No volver a implementar esa vista. La observacion del plan fue corregida posteriormente; no es una tarea pendiente.

## 2. Cómo registrar evidencia sin repetir el análisis

Estados independientes:

- **E — Ejecutado:** prueba concreta con PASS/FAIL y alcance identificado; no equivale a recorrido E2E.
- **S — Estático:** implementación/contrato inspeccionado, sin ejecución del recorrido.
- **M — Manual pendiente:** faltan navegador, sesión, datos o autorización de escritura.
- **D — Defecto confirmado:** evidencia reproducible o contradicción inequívoca; indicar prioridad y alcance. Una duda visual no es D.

Todas las filas funcionales siguientes están **S / M**, salvo la cobertura automatizada parcial indicada en sección 3. No hay recorridos completos marcados PASS. Las responsabilidades por perfil se basan en el código y seed, no certifican las patentes efectivas de las cuentas actuales.

Al ejecutar una fila, anotar: fecha, commit/proceso, perfil, IDs ficticios, pasos realmente ejecutados, PASS/FAIL/NO EJECUTADO, captura sanitizada o respuesta observada, defecto asociado. No guardar contraseñas, tokens, hashes, enlaces seguros completos ni datos personales. Separar errores de entorno de errores del producto.

## 3. Evidencia automatizada historica del 14/10

Comandos ejecutados desde la raíz:

```powershell
dotnet test tests/MecaniCar360.Tests/MecaniCar360.Tests.csproj --configuration CierreB -p:UseAppHost=false --filter 'FullyQualifiedName!~OnboardingSqlIntegrationTests' -v:q
dotnet build MecaniCar360.csproj --configuration CierreB -p:UseAppHost=false -v:q
git diff --check
```

| Evidencia | Resultado | Alcance real |
|---|---|---|
| Suite seleccionada | 212 PASS, 0 FAIL, 0 SKIP | El caso SQL excluido no se cuenta como aprobado ni omitido del subconjunto |
| Build | PASS, 0 errores | Build incremental posterior: 0 warnings emitidos; durante la compilación de la suite aparecieron las 26 CS8618 conocidas. No afirmar que fueron eliminadas |
| `ModeloEfTests` | PASS | Modelo/snapshot sin cambios, conexión cerrada; nueve mapeos `decimal(18,2)`. No consulta esquema SQL ni prueba overflow real |
| `NavegacionCierreTests` | PASS | Controladores invocados directamente y aserciones sobre fuentes: retornos, error de presupuesto, botones por patente, consulta técnica, accesos cliente y duplicación OT. No clics reales |
| `CabecerasHtmlTests` | PASS | HTTP en host Kestrel mínimo, sin aplicación/SQL: no-store, nosniff, no-referrer, conservación de CSP y redirect simulado OAuth. No prueba Google real |
| `PermisosVistaTests`, `ClienteInactivoMiddlewareTests`, `ComposicionSeguridadTests` | PASS dentro de suite | Regresiones de componentes; no login real de los cinco perfiles ni lectura de sus permisos actuales |
| `TablasResponsiveTests`, `EnlaceSeguroPresentacionTests`, `RecursosLayoutTests` | PASS dentro de suite | Estructura/recursos del marcado. No demuestran ausencia de desbordamiento, contraste suficiente ni uso con teclado |
| `StockPresupuestoRegresionTests` y pruebas de onboarding/credenciales | PASS dentro de suite | Regresiones específicas; no SMTP, consentimiento Google ni flujo persistido completo |
| Alta/toma/transiciones/recepción: pruebas de atomicidad y lectura vigente | PASS dentro de suite | Evidencia estructural y casos del entorno de pruebas; no demuestra carreras reales de SQL Server |

## 4. Preparación mínima de la sesión real

1. Identificar versión de la instancia y comprobar Login sin reiniciar indiscriminadamente. Si no está iniciada, acordar arranque con sus posibles escrituras de bootstrap. No ejecutar seed/migraciones como diagnóstico.
2. Disponer de sesiones ficticias de ADMIN, CAJA, MECANICO, STOCK y CLIENTE habilitado, y de un segundo cliente para negativos de ownership. No crear cuentas ni cambiar permisos con esta autorización de sólo lectura.
3. Usar `TEST_CIERRE10_A123` exclusivamente para consultas autorizadas. Para completar el CORE se necesita autorización específica de un escenario separado: cliente/vehículo, dos turnos, OT aprobada y rechazada, repuestos/lotes, factura/pagos y postventa. Prepararlo mediante servicios, con manifiesto externo de IDs; sin limpieza automática.
4. Acordar captura de correo ficticio y participación del usuario para Google y credenciales. No usar secretos reales en scripts/documentos. No enviar enlaces ni consumir tokens durante una inspección de sólo lectura.
5. Antes de ejecutar operaciones mutantes, confirmar entorno e integridad según el procedimiento vigente. Registrar DVH/DVV antes/después mediante mecanismos existentes autorizados; nunca recalcular baseline para hacer pasar un caso.

En las matrices, **L** significa recorrido previsto de consulta; **W** implica escritura o efecto de sesión/correo que debe ejecutarse en la sesión acordada, no durante esta primera pasada. Revisar los efectos del endpoint, no asumir que todo GET es inocuo. Un login puede generar auditoría.

## 5. Matriz por perfil y operación

### Acceso común y ADMIN

| ID / tipo | Pasos y rutas de referencia | Resultado esperado / control | Estado |
|---|---|---|---|
| AC01 / W, cinco perfiles | Login → `Account/Inicio` → pantalla inicial → marca/Menú principal → logout POST | ADMIN: Dashboard con DASHBOARD_VER; CAJA: Inicio/Caja con TURNO_VER; MECANICO: Inicio/Mecanico con ORDEN_MODIFICAR; STOCK: Inicio/Stock con STOCK_VER; cliente habilitado: PortalCliente. Destino depende de patentes efectivas, no de nombre de rol. Logout bloquea nueva consulta privada | S / M |
| AC02 / L | Anónimo abre ruta privada; usuario autenticado sin patente abre URL directa; probar cuenta inválida/inactiva en sesión autorizada | Challenge/login sólo sin sesión válida; falta de patente produce denegación; retorno Account/Inicio seguro; no bucle de cliente inactivo | S / M |
| AD01 / L | Dashboard → enlaces operativos, filtros y retornos | Datos/totales y vacíos comprensibles; no acceso cliente exclusivo para ADMIN sin cuenta cliente habilitada | S / M |
| AD02 / W | Usuario/Administracion → Personal/Clientes → búsqueda → PersonaCuenta → NuevoPersonal/primer ingreso | Secciones separadas, búsqueda tolera null; cuenta interna y rol coherentes; errores visibles, sin duplicar Personas | S / M |
| AD03 / W | Persona/AdministrarRoles → Rol/Index → Seguridad/Index, Rol, Familia | CLIENTE no administrable genéricamente; protecciones ADMIN conservadas; revocación efectiva en nueva petición; acceso UI no sustituye validación servidor | S / M |
| AD04 / L; altas W | Marca/Index → Detalle → Modelo/Index → Detalle → retorno; edición/estado con permiso | Modelo muestra marca, estado, fecha y retorno; VEHICULO_VER para consulta y VEHICULO_MODIFICAR para acciones. No ViewNotFound ni botones indebidos | S / M |
| AD05 / L | Auditoria/Index y Reportes/Index → filtros → resultado/PDF → retorno | Filtros válidos, resultado vacío útil, PDF legible; sin secretos, enlaces autorizados y sin obligación de DASHBOARD_VER en retorno general | S / M |

### CAJA, MECANICO y STOCK: CORE aprobado y rechazado

| ID / perfil / tipo | Pasos y rutas de referencia | Resultado esperado / control | Estado |
|---|---|---|---|
| CO01 / CAJA / W | Persona/Clientes → NuevoCliente → Cliente → Vehiculo/Crear → elegir Marca/Modelo → detalle | Persona con CLIENTE; no Usuario automático; DNI/email según validación existente; vehículo asociado al titular correcto; errores conservan entrada | S / M |
| CO02 / CAJA / W | Turno/Index o Agenda → Crear → horarios → Detalle → Confirmar | Calendario usa CalendarioViewModel; cupo/fecha/vehículo válidos; fecha y estado persistidos coinciden. Sin excepciones de modelo | S / M |
| CO03 / CAJA / W | Desde turnos separados: cancelar, reprogramar, marcar ausente; repetir transición incompatible | Sólo transiciones permitidas, mensaje existente; rechazo sin historial de éxito. No utilizar el turno destinado al camino aprobado | S / M |
| CO04 / CAJA / W | Turno confirmado → IngresoVehiculo/Registrar → completar vehículo/km/combustible/exterior/accesorios/verificación → detalle | Rechazo controlado si faltan datos del cliente o recepción; un ingreso y una OT. Estado vigente validado, relaciones e historial coherentes | S / M |
| CO05 / ADMIN + MECANICO; negativo CAJA / W | Tras la recepción de CAJA que genera la OT: ADMIN asigna un mecánico autorizado o MECANICO toma una OT disponible sin asignar → DeMecanico → Detalle | CAJA no puede asignar, tampoco por POST directo; mecánico activo; toma ocupada rechazada (reintento del mismo mecánico sin cambios); sin sobrescrituras. Exigir rechazo tambien para CAJA con la patente; control ADMIN efectivo incorporado en servicio, pendiente recorrido real | S / M |
| CO06 / MECANICO / W | OT propia → diagnóstico → guardar → evidencia → historial | Descripción/observaciones/horas con etiquetas; archivos consultables sólo por usuarios autorizados; OT ajena no modificable | S / M |
| CO07 / MECANICO / L | Menú Repuestos → Stock/Index → consulta técnica; selector en Presupuesto/Detalle | Vista HTML, no JSON al usuario; selección sin PROVEEDOR_VER ni información comercial restringida; fallo de carga explícito | S / M |
| CO08 / MECANICO / W | OT → presupuesto → agregar ítems/repuestos → enviar a aprobación | Cantidades/importes válidos; versión vigente; errores en interfaz, no texto HTTP 400 aislado; acceso único a presupuesto/factura en OT | S / M |
| CO09 / CLIENTE + STOCK / W | Presupuesto/Propio → aprobar → consultar movimientos/lotes/existencias | Sólo presupuesto propio y versión habilitada; totales correctos y consumo coherente según servicio. Segundo envío/reintento no duplica efectos | S / M |
| CO10 / MECANICO / W | OT aprobada → reparar/cambiar estado → horas reales → finalizar | Estados/requisitos existentes respetados; historial coherente; no finalizar inválidamente | S / M |
| CO11 / CAJA / W | OT finalizada → emitir factura → PorId/Detalle → RegistrarPago parcial y saldo → entregar | Una factura, conceptos/importes correctos; sobrepago rechazado; no entrega sin pago; un egreso; retorno a destino permitido | S / M |
| CO12 / CLIENTE + MECANICO + CAJA / W | Segundo presupuesto → rechazar con motivo → consultar OT → facturar diagnóstico → pagar → entregar | Rechazo visible; no reparar por transición incompatible; no cobrar repuestos no aprobados; factura de diagnóstico según reglas existentes | S / M |
| ST01 / STOCK / W | Stock/Crear → intentar stock inicial no cero; SKU duplicado/nombre inválido → corregir; Editar con error | Alta inicia en cero, mensaje explica ingreso por movimiento; ModelState y rechazos visibles, valores conservados | S / M |
| ST02 / STOCK / W | Proveedor → Stock/AdministrarProveedores → movimiento de ingreso/lote → Movimientos/Detalle | Existencias por movimientos válidos, relación/proveedor y trazabilidad correctos; no editar stock inicial para simular ingreso | S / M |
| ST03 / STOCK / L; cambios W | Listado activos/inactivos → detalle/editar disponibles → reactivar autorizado | No enlaces a operaciones incompatibles; botones sólo con patente; reactivación conserva reglas y no abre información comercial al mecánico | S / M |

### CLIENTE y postventa

| ID / tipo | Pasos y rutas de referencia | Resultado esperado / control | Estado |
|---|---|---|---|
| CL01 / W | RegistroCliente → correo → ActivacionCliente; invitación CAJA sobre persona existente; Google con intervención del usuario | Activación de persona correspondiente sin duplicación; correo verificado; onboarding personal obligatorio; Google y local legítimamente asociados recuperan mismo Usuario/Persona | S / M |
| CL02 / L y W autorizado | PortalCliente/Index → MisDatos → actualizar contacto permitido | Nombre/apellido correctos; identidad y campos no editables preservados; POST antiforgery; no modificación de otro cliente | S / M |
| CL03 / L; altas W | MisVehiculos/Vehiculo → NuevoVehiculo; MisTurnos/Turno → NuevoTurno/cancelar | Sólo recursos propios; catálogo/horarios útiles; validaciones y estado visibles; manipular ID no permite ver/alterar titular ajeno | S / M |
| CL04 / L | MisOrdenes/Orden → diagnóstico actual → MisPresupuestos/Propio → facturas propias/pagos → garantías propias | Consultas consistentes con CORE, sin historial interno restringido ni datos ajenos; archivos privados autorizados | S / M |
| CL05 / W | Acción de seguridad de cuenta → enlace de agregar/cambiar/recuperar contraseña; vincular Google cuando corresponde | Acción según credenciales persistidas, sin duplicación visual; fragmento eliminado, token fuera de query; POST antiforgery, vencido/usado rechazado, sesiones invalidadas. Vinculación Google conserva reautenticación propia | S / M |
| PV01 / CAJA + CLIENTE / W | Garantia/Crear desde operación elegible → detalle interno/propio → anulación autorizada; calificación propia | Cobertura/vigencia y permisos existentes; cliente no califica OT ajena ni accede a garantía ajena | S / M |
| PV02 / todos / L y W | Notificacion/Index → recurso; marcar leída individual/todas | Sólo notificaciones propias, destino permitido; marcado sin afectar otro usuario. Historial de OT/diagnóstico/presupuesto y auditoría accesible a quien corresponde | S / M |

Para negativos de ownership usar IDs del escenario autorizado de otro cliente y no IDs de personas reales. Para CSRF repetir un POST controlado sin antiforgery sólo cuando haya autorización de prueba de escritura; resultado esperado rechazo sin efectos. Los callbacks OAuth, endpoints JSON auxiliares y POST no son pantallas faltantes por carecer de vista propia.

## 6. Sesión visual agrupada (pendiente íntegramente)

| Grupo | Pantallas | Inspección estática disponible | Comprobación en navegador / criterio |
|---|---|---|---|
| V-A | Layout e inicio de cinco perfiles | Menú por patentes, marca a Account/Inicio, viewport, colapso, salto al contenido | 1366/768/375/320 px: abrir/cerrar menú, foco visible, orden de tabulación, acciones alcanzables, sin scroll global evitable |
| V-B | Login y siete vistas independientes de ActivacionCliente/SeguridadCliente | Head compartido con viewport y CSS local; aislamiento y contratos cubiertos por suite | Móviles y zoom 200 %: campos/botones legibles, errores y estados; consola/red sin bloqueo JS/CSP. No consumir enlaces para probar sólo presentación |
| V-C | Turno/Crear/Index, recepción, OT/Detalle | Formularios existentes, etiquetas y flujo enlazado; radios de recepción requieren comprobación específica | Error y estado vacío, selección Marca/Modelo/horarios; teclado en radios, horas/diagnóstico, cargas JS; sin pérdida de valores ni botones cortados |
| V-D | Presupuesto Detalle/Propio, Stock Detalle/Movimientos/AdministrarProveedores | Tablas y etiquetas con regresiones de marcado | Scroll dentro de tabla; todas las columnas/acciones alcanzables; errores de presupuesto útiles, contraste de badges y confirmaciones visibles |
| V-E | Marca/Modelo/Rol/Persona/Usuario/Proveedor, mecánicos e historial OT, evidencias | Wrappers responsive cubiertos por tests, detalle Modelo existente | Listas vacías y pobladas, filas largas y formularios repetidos; IDs/foco, retorno, ninguna acción duplicada que confunda |
| V-F | Seguridad Index/Familia/Rol | Flex column/sm-row/wrap y etiquetas asociadas observadas | 320/375 px y zoom 200 %: selects/botones no desbordan ni se ocultan |
| V-G | Factura/pago, Portal/MisDatos, garantías, reportes/auditoría | Vistas y retornos existentes | Importes/saldos legibles, errores, retorno permitido por perfil, PDF a escala utilizable |

Hacer el CORE completo una vez a 1366 px; repetir sólo componentes representativos a 320 y 375, menú/tablas a 768 y teclado/zoom 200 %. Registrar capturas por grupo y por defecto, sin una campaña repetida por cada combinación. No aprobar contraste con una lectura de clases CSS: medir colores efectivos o comprobar herramienta de accesibilidad en el render.

## 7. Problemas y decisiones priorizadas

| ID | Prioridad / certeza | Evidencia e impacto | Acción recomendada |
|---|---|---|---|
| D01 | Historico, resuelto | PLAN-CIERRE H01/D0 y conclusión dicen que falta Modelo/Detalle; archivo versionado y HEAD 4ebf977 contradicen esa premisa | Plan actualizado; conservar solo aceptacion GET autenticada |
| D02 | Historico, resuelto | `docs/seguridad-autorizacion.md:19,167` describe Dashboard por permisos alternativos; `Controllers/DashboardController.cs:12` exige DASHBOARD_VER | Matriz corregida; conservar aceptacion del acceso autorizado |
| V01 | P3 / pendiente visual, ya conocido | `Views/IngresoVehiculo/Registrar.cshtml:58–59,73`: pares de radios con el mismo asp-for y sin ID explícito distinto | Comprobar HTML generado y teclado. Si se confirma ID duplicado, corrección localizada de id/label sin modificar name/POST. No afirmar bloqueo funcional |
| V02 | Pendiente visual, no defecto demostrado | Footer absoluto y nowrap en `Views/Shared/_Layout.cshtml.css`; formularios/tablas dependen del contenido real | Comprobar superposición/desborde móvil; corregir sólo si se reproduce. No rediseño preventivo |
| L01 | Pendiente de aceptación | Ningún recorrido autenticado/renderizado ejecutado en esta pasada | Completar matrices; no equivale a funcionalidad ausente ni vulnerabilidad |
| L02 | Limitación conocida, no resuelta | 28 casos SQL no ejecutados; barreras B1 no observan espera SQL real | Campaña separada y autorizada en base aislada. No ampliar harness ni usar MecaniCar360 |

No se confirmó un nuevo defecto de negocio P0/P1 ni una pantalla obligatoria ausente. Las 26 CS8618 y precisión monetaria implícita permanecen como deuda documentada; sólo escalar si una prueba demuestra fallo. No agregarlas al camino crítico de aceptación por sí solas.

## 8. Orden de continuación y salida

1. **Habilitar sesión segura:** navegador, instancia identificada y cuentas; autorización delimitada para el escenario mutable separado. Sin esto avanzar sólo con documentación/consultas seguras, no simular PASS.
2. **Consulta por perfiles:** AC01/02, AD01/04/05, CO07, ST03 y CL04; recoger simultáneamente V-A/D/E/G. Verificar primero Modelo/Detalle, sin reimplementarlo.
3. **CORE aprobado:** CO01/02/04–11 y ST01/02 con fixture autorizado; comprobar DVH/DVV antes/después. Luego CO12 y CO03 sobre registros separados. Registrar IDs finales y no limpiar automáticamente.
4. **Seguridad y postventa:** AD02/03, CL01–05, PV01/02; aprovechar evidencia histórica trazable cuando el responsable la acepte. Google/credenciales con intervención humana según corresponda.
5. **Visual representativo:** completar V-A–G en los anchos acordados y zoom/teclado. Corregir por lotes únicamente defectos reproducidos, orden P0 → P1 → P2; repetir el caso afectado y regresiones directas.
6. **Documentación:** mantener D01/D02 cerrados y completar este registro. Concurrencia SQL se mantiene como campaña separada prevista por el plan, no se ejecuta como parte de esta pasada.

Registro para continuar (una línea por caso ejecutado):

| Fecha / commit / entorno | ID de matriz | Perfil / IDs ficticios | Resultado real | Evidencia sanitizada / defecto | Próxima acción |
|---|---|---|---|---|---|
| 14/10/2026 / 4ebf977 / sin SQL | Suite, build y modelo/snapshot | No aplica | PASS en alcance de sección 3 | 212 pruebas; 0 errores; modelo alineado | Continuar sesión real, no repetir auditoría general |
| Pendiente | Matrices funcionales y V-A–G | Pendiente de sesión | NO EJECUTADO E2E | Disponibilidad actual no comprobada | Preparación segura de sección 4 |

Aceptar 1.0 cuando el camino aprobado y rechazado, consultas de los cinco perfiles y negativos críticos tengan evidencia; no queden P0/P1/P2 obligatorios abiertos; visual sea utilizable; y las limitaciones pendientes tengan decisión expresa del responsable. **Esta primera pasada no declara terminada la aceptación funcional ni visual.**

## Regresiones de autorizacion P1

### Criterios complementarios del circuito (inspección 27/10/2026)

Complementan los IDs existentes, sin crear nuevos casos de uso. Todos permanecen **S / M**, no ejecutados aquí. Preparar datos separados y autorizados; preservar `TEST_CIERRE10_A123`.

| IDs existentes / código de referencia | Resultado positivo esperado | Negativo y ausencia de efectos a comprobar |
|---|---|---|
| CO04 — `IngresoVehiculoService.RegistrarIngresoYCrearOrdenAsync` | Recepción válida genera ingreso, OT y transición/historial del turno relacionados | Turno cancelado/ausente o ingreso existente: rechazar sin nueva OT, ingreso, historial ni auditoría de éxito. Incluir turno precargado cuyo estado cambió |
| CO05 — `OrdenTrabajoService.AsignarMecanicoAsync/TomarOrdenAsync` | ADMIN efectivo asigna mecánico elegible; MECANICO toma OT pendiente libre | CAJA con patente no asigna; otro mecánico no sobrescribe asignación. Rechazo no deja asignación ni auditoría de éxito |
| CO06–08 — `DiagnosticoService`; `PresupuestoService.CrearAsync/EnviarAprobacionAsync` | Diagnóstico autorizado y presupuesto con ítems válidos; envío crea versión con instantánea y deja OT esperando aprobación | Sin diagnóstico requerido, ítems o evidencias válidas: no generar versión enviada ni avanzar OT. Un borrador modificado no debe sustituir la instantánea ya enviada al cliente |
| CO09 — `PresupuestoService.DecidirAsync`; `StockService.ProcesarAprobacionIncrementalAsync` | CLIENTE habilitado propietario decide última versión pendiente. Aprobación registra consumo incremental; con faltantes consume sólo lo disponible y muestra aviso | ADMIN/cliente ajeno, versión anterior o ya decidida: no cambiar decisión, OT, stock, historial ni auditoría de éxito. Reintento no duplica consumo. Un rechazo válido con motivo registra rechazo y no ejecuta consumo de aprobación |
| CO10 — `OrdenTrabajoService.IniciarReparacionAsync/FinalizarAsync` | Inicio desde estado aprobado con presupuesto vigente aprobado; finalización desde EnReparacion con asignación y última versión aprobada. Horas, si se informan, entre 0 y 1000 | Estado incompatible, versión no aprobada, actor sin asignación/autorización u horas fuera de rango: no avanzar estado, fecha final ni historial de éxito |
| CO11–12 — `FacturaService.EmitirAsync/RegistrarPagoAsync` | OT finalizada factura conceptos de versión aprobada; OT rechazada factura costo positivo de diagnóstico. Pago parcial reduce saldo y pago completo permite continuar | Factura duplicada, importes inválidos, saldo cambiado o sobrepago: rechazo sin nueva factura/pago ni saldo parcial incorrecto. No facturar repuestos no aprobados en el camino rechazado |
| CO11–12 — `OrdenTrabajoService.EntregarAsync` | OT Finalizado o Rechazado, factura Pagada y suma de pagos efectivos suficiente: entrega y egreso relacionados | Sin factura, deuda o estado incompatible: no registrar entrega ni egreso. Repetir no crea un segundo egreso |
| PV01 — `GarantiaService.PrepararCreacionAsync/CrearAsync` | Reparación finalizada y entregada, egreso efectivo, factura totalmente pagada y origen en versión aprobada; ítems de la factura y cobertura válidos | Garantía previa incluso anulada, entrega desde Rechazado, falta de pago/egreso o ítem ajeno: no emitir garantía ni auditoría de éxito. No corresponde garantía al cobro de diagnóstico/rechazo |

En las operaciones rechazadas comparar los registros relacionados antes/después, no sólo el mensaje HTTP. Ante un fallo de persistencia anterior al commit verificar desde otro contexto estado, historial, auditoría, stock y DVH/DVV afectados. Ante fallo del Observer posterior al commit, el resultado correcto es negocio guardado con advertencia, no rollback. Ver el [contrato transaccional](integridad-dvh-dvv.md#contrato-transaccional-del-circuito-de-presupuesto). Estas pruebas requieren entorno y escrituras autorizados; no fueron ejecutadas en esta actualización.

### Negativos específicos P1

- CO05: ADMIN efectivo asigna; CAJA, STOCK, CLIENTE y MECANICO no asignan directamente aunque tengan la patente. MECANICO conserva la toma voluntaria.
- CO09: CLIENTE habilitado propietario aprueba/rechaza; otro cliente, ADMIN y demas perfiles internos no deciden. ADMIN conserva consulta autorizada.
- En cada rechazo comprobar ausencia de cambios en OT, version, stock, historial, auditoria y notificaciones. Probar tambien cuenta/persona/rol inactivos, asignacion de rol dada de baja y version anterior.
- Las pruebas no SQL de AutorizacionOperacionesP1Tests ejecutan filtros reales en memoria, comprueban traduccion SQL sin conexion e integracion estructural. No certifican POST, commits ni rollback reales. Estas comprobaciones siguen pendientes en entorno autorizado.

Resultado automatizado de esta correccion: build sin errores (26 CS8618 existentes), 248 pruebas no SQL aprobadas, 0 fallidas. Los 27 casos nuevos no sustituyen la aceptacion SQL ni de navegador.
