# MecaniCar360 1.0 — Catálogo de casos de uso

Fecha: 15/10/2026. Referencia: rama `master`, HEAD `4ebf977`, más cambios locales existentes. Catálogo del código inspeccionado, no certificación de una instalación ni acta de aceptación.

## 1. Criterio de identificación y alcance

Se identificaron **65 casos de uso: 61 de aplicación y 4 de operación técnica**. Cada caso expresa un objetivo observable de un actor y tiene una entrada disponible por MVC o por una herramienta operativa. Un mismo objetivo no se duplica por perfil, ruta, formato HTML/JSON/PDF o método de autenticación.

- Se agrupan alta/consulta/edición/estado en mantenimiento de un catálogo cuando representan la misma responsabilidad administrativa. Las consultas operativas que sirven a otros actores o decisiones independientes sí se distinguen.
- Se separan reserva, confirmación, cancelación, reprogramación y ausencia: se pueden iniciar independientemente, con intenciones, precondiciones y resultados distintos.
- Recepción incluye apertura automática de OT. Crear historial, emitir auditoría, mantener DVH/DVV, consultar horarios, resolver permisos y enviar correo son pasos o colaboraciones, no nuevos casos de negocio.
- Elaborar presupuesto incluye ítems, versiones y envío. Aprobar/rechazar son alternativas de una decisión del cliente. Consumo de repuestos por aprobación no duplica la salida manual de stock.
- Agregar/cambiar/recuperar contraseña por enlace son variantes de CU-010; la autenticación mediante contraseña actual de CU-004 es un recorrido distinto, con prueba de identidad diferente. El primer ingreso obligatorio CU-003 incluye su propia contraseña inicial.
- Consultas propias y administrativas comparten objetivo cuando sólo cambia el alcance autorizado. Esto **no** unifica permisos, DTO ni visibilidad.
- Los cuatro casos técnicos tienen actor y herramientas explícitos; se contabilizan separadamente para no presentarlos como pantallas web.
- No se agregan como casos “probar”, “compilar”, “ejecutar migraciones”, responder 403/503, renderizar layout, consultar un endpoint auxiliar ni ejecutar automáticamente un Observer.

### Fuentes y límites de trazabilidad

Se contrastaron documentación de `docs/`, controladores, servicios, modelos/DTO/ViewModels/enums, vistas/partials, recursos locales, permisos/seed, pruebas, scripts y herramientas. Las referencias de cada ficha indican símbolos o familias de vistas existentes, no una enumeración de todas las líneas internas.

**No se encontró en el repositorio una especificación original completa de requerimientos funcionales con IDs RF ni un catálogo original de CU.** Por eso no se inventan códigos RF ni se afirma cobertura contractual completa. Los códigos AC/AD/CO/ST/CL/PV proceden de [ACEPTACION-1.0.md](ACEPTACION-1.0.md): son referencias de aceptación, **no IDs de requerimientos originales**. El vínculo RF original de todas las fichas queda “no localizado”; se aporta trazabilidad sustituta explícita hacia esas fuentes. `T07` es la referencia conservada en la documentación de backup, no una numeración RF extrapolada.

Fuentes de contexto: [PLAN-CIERRE.md](PLAN-CIERRE.md), [OPERACION-LOCAL-1.0.md](OPERACION-LOCAL-1.0.md), [seguridad-autorizacion.md](seguridad-autorizacion.md), [testing.md](testing.md), [backup-restore.md](backup-restore.md), [integridad-dvh-dvv.md](integridad-dvh-dvv.md), [VALIDACION-12C1.md](../tests/MecaniCar360.Tests/VALIDACION-12C1.md) y [README del harness](../tests/MecaniCar360.ConcurrencyTests/README.md).

### Estados y evidencia

| Estado | Criterio |
|---|---|
| Implementado y verificado | Evidencia ejecutada y trazable del objetivo completo, actores, pre/postcondiciones y alternativas relevantes sobre una versión identificada |
| Implementado sin validación completa (I) | Entrada y lógica implementadas; pueden existir pruebas parciales, pero no aceptación completa vigente |
| Parcial (P) | Falta demostrable de una parte necesaria del objetivo, no mera falta de pruebas |
| No implementado (N) | Objetivo documentado exigible sin recorrido implementado |

**Las 65 fichas tienen estado I.** No se convierte la existencia de código ni un test de marcado en PASS funcional. El plan/aceptación registran 221 pruebas no SQL aprobadas y build sin errores en su última revisión; aquí no se repitieron ni se ejecutó navegador, login, SMTP, Google, SQL o herramientas operativas. Los 28 casos SQL escritos siguen sin ejecutar; B1 no acredita observación de espera SQL. Estas limitaciones no prueban por sí solas defectos o funcionalidad parcial.

## 2. Actores y límites del sistema

| Actor | Tipo y responsabilidad |
|---|---|
| ADMIN | Perfil interno: administración, configuración y supervisión; privilegios efectivos definidos por servicios. El bypass no lo convierte en CLIENTE habilitado |
| CAJA | Perfil interno: atención de clientes/vehículos, turnos, recepción, facturación, cobros y entrega; sólo las acciones y personas admitidas por sus patentes y filtros |
| MECANICO | Perfil interno: toma/asignación propia de trabajo, diagnóstico, evidencia, presupuesto, reparación y consulta técnica de repuestos |
| STOCK | Perfil interno: catálogo de repuestos, proveedores, oferta comercial, movimientos y trazabilidad |
| CLIENTE | Persona autenticada/habilitada que opera exclusivamente recursos propios. No equivale a cualquier Persona con una asignación histórica del rol |
| Visitante / solicitante / cliente invitado | Actor no autenticado que solicita acceso, verifica correo o recupera credenciales. Son estados de la misma persona, no perfiles internos nuevos |
| CLIENTE pendiente | Estado del cliente durante onboarding; no habilita operaciones del taller hasta cumplir los requisitos |
| Usuario autenticado | Generalización de los cinco perfiles para sesión y notificaciones; no es un sexto rol persistido |
| Usuario interno | Generalización de ADMIN, CAJA, MECANICO y STOCK para primer ingreso |
| Operador técnico | Actor operativo externo a los roles web: ejecuta backup/restore/integridad con permisos del entorno y autorización explícita |
| Google | Actor externo de apoyo: autenticación OAuth e identidad estable; no administra roles del taller |
| Servidor de correo | Actor externo de apoyo: transporte SMTP de invitaciones, orientación y enlaces de seguridad; aceptación SMTP no equivale a entrega en bandeja |
| SQL Server / sistema de archivos | Sistemas de apoyo de los casos técnicos. En los CU web son infraestructura interna, no clientes del negocio |
| Cliente presencial, destinatario o pagador | Interesado secundario cuando CAJA opera por él; no se presume interacción directa con la aplicación |

Proveedor es una entidad comercial, **no** un actor con portal implementado. No hay pasarela bancaria real demostrada: registrar un medio de pago no implica integración con un banco. EF Core, SessionManager y los patrones Facade/Memento/Observer no son actores.

**Autorización común:** cada operación privada requiere cuenta/persona/rol en estado admisible, patente efectiva y los controles de ownership/asignación del servicio. Antiforgery, límites y validación del servidor se mantienen donde están implementados. Las fichas no reemplazan esos controles ni afirman que todos los actores listados puedan realizar todas las variantes.

**Diferencias concretas con descripciones generales:**

- `AltaPersonalService.PuedeCrearAsync` exige ADMIN efectivo además de PERSONA_CREAR, USUARIO_CREAR y ROL_MODIFICAR: CU-012 no se atribuye a CAJA por tener USUARIOS/PERSONAS.
- `UsuarioService.PuedeAdministrarPersonaAsync` limita CAJA a clientes exclusivos; el alta interna no está abierta a CAJA.
- Asignación/quita de roles requiere ROL_MODIFICAR; no se encontró esa familia asignada por defecto a CAJA.
- Regla definitiva: CAJA recibe y genera OT; MECANICO toma OT disponible sin asignar; solo ADMIN efectivo asigna directamente. El servicio exige rol ADMIN activo ademas de la patente, antes y dentro de la transaccion. No se consulto ni cambio SQL.
- Urgencia/observaciones/costo de diagnóstico requieren ORDEN_MODIFICAR y ADMIN o mecánico asignado en servicio: CU-035 no se atribuye a CAJA.
- Las asignaciones efectivas de la base actual no fueron consultadas. No se proponen ni aplican ampliaciones de permisos.

## 3. Catálogo numerado

Convenciones de referencias: `XController.Accion` se encuentra en `Controllers/XController.cs`; `XService` en `Services/XService.cs`. Los parciales `StockService.Proveedores`, `.Lotes` y `.ConsultasTecnicas` son archivos `.cs` separados. Una vista `Modulo/Nombre` es `Views/Modulo/Nombre.cshtml`; nombres siguientes dentro de la misma referencia mantienen el módulo. Las herramientas/scripts se indican con su ruta literal.

| ID | Caso de uso | Módulo | Estado |
|---|---|---|---|
| CU-001 | Iniciar sesión | Acceso y cuenta | I |
| CU-002 | Cerrar sesión | Acceso y cuenta | I |
| CU-003 | Completar primer ingreso de cuenta interna | Acceso y cuenta | I |
| CU-004 | Cambiar contraseña local con contraseña actual | Acceso y cuenta | I |
| CU-005 | Solicitar autorregistro por correo | Alta y seguridad CLIENTE | I |
| CU-006 | Invitar a un cliente existente al portal | Alta y seguridad CLIENTE | I |
| CU-007 | Activar cuenta cliente mediante invitación | Alta y seguridad CLIENTE | I |
| CU-008 | Registrarse con Google y completar el alta | Alta y seguridad CLIENTE | I |
| CU-009 | Vincular Google a una cuenta cliente local | Alta y seguridad CLIENTE | I |
| CU-010 | Establecer o recuperar contraseña cliente por enlace | Alta y seguridad CLIENTE | I |
| CU-011 | Consultar y actualizar mis datos | Alta y seguridad CLIENTE | I |
| CU-012 | Dar de alta personal interno | Personas y seguridad administrativa | I |
| CU-013 | Administrar datos y estado de personas | Personas y seguridad administrativa | I |
| CU-014 | Administrar cuentas y su estado | Personas y seguridad administrativa | I |
| CU-015 | Asignar o quitar roles internos a una Persona | Personas y seguridad administrativa | I |
| CU-016 | Administrar catálogo de roles | Personas y seguridad administrativa | I |
| CU-017 | Configurar familias y composición de permisos | Personas y seguridad administrativa | I |
| CU-018 | Consultar personas y cuentas | Personas y seguridad administrativa | I |
| CU-019 | Dar de alta cliente presencial | Personas y seguridad administrativa | I |
| CU-020 | Administrar marcas | Vehículos y catálogos | I |
| CU-021 | Administrar modelos | Vehículos y catálogos | I |
| CU-022 | Administrar vehículos del taller | Vehículos y catálogos | I |
| CU-023 | Registrar vehículo propio | Vehículos y catálogos | I |
| CU-024 | Consultar vehículos e historial autorizado | Vehículos y catálogos | I |
| CU-025 | Consultar agenda y turnos | Turnos | I |
| CU-026 | Reservar turno | Turnos | I |
| CU-027 | Confirmar turno | Turnos | I |
| CU-028 | Cancelar turno | Turnos | I |
| CU-029 | Reprogramar turno | Turnos | I |
| CU-030 | Registrar ausencia del cliente | Turnos | I |
| CU-031 | Registrar recepción y apertura de OT | Recepción y órdenes | I |
| CU-032 | Consultar recepciones y vehículos en taller | Recepción y órdenes | I |
| CU-033 | Consultar órdenes y seguimiento del trabajo | Recepción y órdenes | I |
| CU-034 | Asignar responsable mecánico a una OT | Recepción y órdenes | I |
| CU-035 | Actualizar datos operativos de la OT | Recepción y órdenes | I |
| CU-036 | Registrar y revisar diagnóstico | Recepción y órdenes | I |
| CU-037 | Adjuntar y consultar evidencias del trabajo | Recepción y órdenes | I |
| CU-038 | Elaborar y enviar presupuesto | Presupuestos y reparación | I |
| CU-039 | Consultar presupuesto y versiones accesibles | Presupuestos y reparación | I |
| CU-040 | Aprobar o rechazar presupuesto propio | Presupuestos y reparación | I |
| CU-041 | Registrar ejecución y finalización de reparación | Presupuestos y reparación | I |
| CU-042 | Entregar vehículo y cerrar permanencia en taller | Recepción y órdenes | I |
| CU-043 | Administrar catálogo de repuestos | Stock y proveedores | I |
| CU-044 | Consultar repuestos técnicamente | Stock y proveedores | I |
| CU-045 | Administrar proveedores | Stock y proveedores | I |
| CU-046 | Gestionar oferta de proveedores por repuesto | Stock y proveedores | I |
| CU-047 | Registrar ingreso de stock | Stock y proveedores | I |
| CU-048 | Registrar salida de stock | Stock y proveedores | I |
| CU-049 | Ajustar existencias | Stock y proveedores | I |
| CU-050 | Consultar movimientos y trazabilidad de stock | Stock y proveedores | I |
| CU-051 | Emitir factura de una OT | Facturación y pagos | I |
| CU-052 | Consultar facturas y pagos | Facturación y pagos | I |
| CU-053 | Registrar pago de factura | Facturación y pagos | I |
| CU-054 | Emitir garantía | Postventa | I |
| CU-055 | Consultar garantías y cobertura | Postventa | I |
| CU-056 | Anular garantía | Postventa | I |
| CU-057 | Calificar trabajo recibido | Postventa | I |
| CU-058 | Consultar resumen de actividad por perfil | Consulta transversal y supervisión | I |
| CU-059 | Gestionar bandeja de notificaciones | Consulta transversal y supervisión | I |
| CU-060 | Consultar auditoría | Consulta transversal y supervisión | I |
| CU-061 | Obtener reportes de gestión | Consulta transversal y supervisión | I |
| CU-062 | Respaldar base y evidencias | Operación técnica | I |
| CU-063 | Restaurar base y evidencias | Operación técnica | I |
| CU-064 | Verificar integridad DVH/DVV | Operación técnica | I |
| CU-065 | Inicializar línea base de integridad autorizada | Operación técnica | I |

## 4. Descripción de los casos

### Acceso y cuenta

#### CU-001 — Iniciar sesión

- **Actor principal:** Visitante. **Secundarios:** Google, cuando se elige ese método.
- **Objetivo:** Acceder a la cuenta y al destino autorizado.
- **Precondiciones:** Cuenta habilitada; credencial local válida o identidad externa ya asociada.
- **Flujo principal:** Elegir método; acreditar identidad; obtener sesión; ingresar al destino decidido por patentes.
- **Alternativas relevantes:** Credenciales o cuenta inválidas: rechazo; perfil incompleto: recorrido obligatorio; sin patente: acceso denegado; Google no configurado: no disponible.
- **Postcondiciones:** Sesión asociada al Usuario interno, sin fusionar cuentas por correo.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: AC01, AC02, CL01.
- **Implementación:** AccountController.Login/Inicio; GoogleClienteController.Ingresar/Callback; AccountService, GoogleClienteService, SessionManager, PermisoService.
- **Vistas/interfaz:** Account/Login; GoogleCliente/NoDisponible; layout e inicios.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-002 — Cerrar sesión

- **Actor principal:** Usuario autenticado. **Secundarios:** —.
- **Objetivo:** Terminar el acceso autenticado.
- **Precondiciones:** Sesión iniciada.
- **Flujo principal:** Enviar logout mediante la acción prevista; invalidar sesión; volver al acceso público.
- **Alternativas relevantes:** Solicitud sin requisitos de seguridad: rechazo.
- **Postcondiciones:** El navegador deja de disponer de la sesión cerrada.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: AC01.
- **Implementación:** AccountController.Logout; AccountService, SessionManager.
- **Vistas/interfaz:** Views/Shared/_Layout.cshtml.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-003 — Completar primer ingreso de cuenta interna

- **Actor principal:** Usuario interno. **Secundarios:** —.
- **Objetivo:** Completar los datos exigidos por el recorrido interno.
- **Precondiciones:** Cuenta autenticada sujeta al flujo de completar datos.
- **Flujo principal:** Completar teléfono y nueva contraseña en el formulario de primer ingreso; validar; guardar; renovar sesión.
- **Alternativas relevantes:** Datos inválidos: mostrar errores; el onboarding CLIENTE tiene recorrido propio.
- **Postcondiciones:** Datos actualizados, PrimerLogin=false y SecurityStamp renovado.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: AD02.
- **Implementación:** AccountController.CompletarDatos/CompletarDatosExito; AccountService.
- **Vistas/interfaz:** Account/CompletarDatos; Account/CompletarDatosExito.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-004 — Cambiar contraseña local con contraseña actual

- **Actor principal:** Usuario autenticado. **Secundarios:** —.
- **Objetivo:** Cambiar una contraseña local acreditando conocimiento de la actual, en el recorrido Account.
- **Precondiciones:** Cuenta activa con PasswordHash; contraseña actual válida. El portal CLIENTE presenta el recorrido por enlace de CU-010.
- **Flujo principal:** Ingresar contraseña actual, nueva y confirmación; validar; guardar y renovar sesión.
- **Alternativas relevantes:** Contraseña actual incorrecta, cuenta sin credenciales locales o nueva contraseña inválida: rechazo.
- **Postcondiciones:** Credenciales actualizadas según política existente.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: AD02, AC01.
- **Implementación:** AccountController.CambiarContraseña; AccountService; PrimerLoginMiddleware.
- **Vistas/interfaz:** Account/CambiarContraseña.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

### Alta y seguridad CLIENTE

#### CU-005 — Solicitar autorregistro por correo

- **Actor principal:** Visitante. **Secundarios:** Servidor de correo.
- **Objetivo:** Iniciar el alta propia mediante verificación del correo.
- **Precondiciones:** Dirección válida; configuración de envío y URL base disponibles.
- **Flujo principal:** Enviar correo; recibir respuesta neutra; si es elegible, recibir enlace para CU-007.
- **Alternativas relevantes:** Correo existente: no duplicar cuenta, orientación verificada cuando corresponde; límite o fallo operativo: tratamiento seguro sin enumeración.
- **Postcondiciones:** Solicitud elegible deja invitación emitida, todavía sin activar cuenta.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: CL01.
- **Implementación:** RegistroClienteController.Index/Orientacion; RegistroClienteService, InvitacionClienteService, EmailService.
- **Vistas/interfaz:** RegistroCliente/Index; RegistroCliente/Solicitada; RegistroCliente/Orientacion.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-006 — Invitar a un cliente existente al portal

- **Actor principal:** CAJA, ADMIN. **Secundarios:** Servidor de correo; cliente destinatario.
- **Objetivo:** Habilitar la futura activación de la Persona cliente ya registrada.
- **Precondiciones:** Patentes de emisión y consulta de persona; Persona elegible.
- **Flujo principal:** Seleccionar cliente; emitir invitación; enviar enlace al correo; informar resultado.
- **Alternativas relevantes:** Persona/cuenta no elegible, conflicto o fallo de envío: rechazo según servicio; no crear una segunda Persona.
- **Postcondiciones:** Invitación de la Persona correspondiente disponible para CU-007.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: CL01, CO01.
- **Implementación:** InvitacionClienteController.Emitir; InvitacionClienteService, EmailService.
- **Vistas/interfaz:** Persona/Cliente; Usuario/PersonaCuenta (navegación de cuenta).
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-007 — Activar cuenta cliente mediante invitación

- **Actor principal:** Cliente invitado o solicitante. **Secundarios:** Servidor de correo, como canal del enlace.
- **Objetivo:** Crear el acceso local verificando la titularidad del correo.
- **Precondiciones:** Invitación vigente y no consumida; datos requeridos disponibles.
- **Flujo principal:** Abrir enlace con fragmento; validar sin consumir; completar formulario; confirmar POST; consumir invitación y crear acceso.
- **Alternativas relevantes:** Token vencido/usado/alterado, DNI/correo conflictivo o datos inválidos: no activar; invitación sobre Persona existente la conserva.
- **Postcondiciones:** Usuario asociado a la Persona correcta, acceso habilitado si cumple requisitos y token consumido una vez.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: CL01.
- **Implementación:** ActivacionClienteController.Index/Completada; InvitacionClienteService; RegistroCompletoClienteService.
- **Vistas/interfaz:** ActivacionCliente/Abrir; Index; Completada; NoDisponible.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-008 — Registrarse con Google y completar el alta

- **Actor principal:** Visitante / CLIENTE pendiente. **Secundarios:** Google.
- **Objetivo:** Obtener una cuenta cliente completa a partir de la identidad Google.
- **Precondiciones:** Google configurado; identidad elegible; correo verificado.
- **Flujo principal:** Consentir acceso; recuperar/crear por Provider+Subject; completar datos personales obligatorios; acceder al portal.
- **Alternativas relevantes:** Abandono: reanudar sin duplicar; identidad/correo en conflicto: no fusionar; datos incompletos: restringir operaciones; contraseña local puede agregarse después.
- **Postcondiciones:** Una sola identidad externa asociada a Usuario/Persona; habilitación sujeta a requisitos.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: CL01, CL05.
- **Implementación:** GoogleClienteController.Ingresar/Callback; RegistroCompletoClienteController.Index; GoogleClienteService, RegistroCompletoClienteService, ClienteHabilitadoService.
- **Vistas/interfaz:** RegistroCompletoCliente/Index; GoogleCliente/NoDisponible; PortalCliente/Index.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-009 — Vincular Google a una cuenta cliente local

- **Actor principal:** CLIENTE. **Secundarios:** Google.
- **Objetivo:** Permitir ambos métodos sobre la misma cuenta.
- **Precondiciones:** Cliente autorizado; contraseña local y comprobación reciente; identidad Google libre.
- **Flujo principal:** Elegir vincular; reautenticar; completar OAuth; confirmar asociación; actualizar acciones disponibles.
- **Alternativas relevantes:** Identidad ya vinculada, prueba reciente inválida o callback fallido: rechazo sin fusión.
- **Postcondiciones:** Usuario/Persona conservados con identidad externa asociada; opción redundante oculta.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: CL05.
- **Implementación:** GoogleClienteController.Vincular/Callback; GoogleClienteService, SeguridadClienteService, PresentacionCuentaService.
- **Vistas/interfaz:** PortalCliente/Index; GoogleCliente/NoDisponible.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-010 — Establecer o recuperar contraseña cliente por enlace

- **Actor principal:** CLIENTE / Visitante titular de cuenta. **Secundarios:** Servidor de correo.
- **Objetivo:** Agregar, sustituir o recuperar contraseña local verificando correo.
- **Precondiciones:** Cuenta elegible con correo verificado; para completar, token vigente.
- **Flujo principal:** Solicitar desde portal o recuperación; recibir enlace; ingresar contraseña y confirmación; confirmar POST.
- **Alternativas relevantes:** Respuesta pública neutra; cuenta inhabilitada, token vencido/usado/invalidado o límite: no cambiar; enlace nuevo invalida el anterior según servicio.
- **Postcondiciones:** Contraseña actualizada; enlaces y sesiones anteriores invalidados; identidad Google conservada.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: CL05.
- **Implementación:** SeguridadClienteController.SolicitarEnlace/Recuperar/Restablecer/Completada; SeguridadClienteService, EmailService.
- **Vistas/interfaz:** SeguridadCliente/Abrir; Recuperar; Restablecer; Solicitada; Completada; NoDisponible; Account/Login.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-011 — Consultar y actualizar mis datos

- **Actor principal:** CLIENTE. **Secundarios:** —.
- **Objetivo:** Mantener los datos propios permitidos.
- **Precondiciones:** Cliente autenticado y habilitado.
- **Flujo principal:** Consultar Mis datos; editar campos permitidos; validar y guardar.
- **Alternativas relevantes:** Campos no autorizados, entrada inválida o intento sobre otra persona: rechazo.
- **Postcondiciones:** Datos propios actualizados sin sustituir identidad ni titularidad de otra cuenta.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: CL02.
- **Implementación:** PortalClienteController.MisDatos; DatosClienteService, ClienteHabilitadoService.
- **Vistas/interfaz:** PortalCliente/MisDatos.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

### Personas y seguridad administrativa

#### CU-012 — Dar de alta personal interno

- **Actor principal:** ADMIN. **Secundarios:** Servidor de correo.
- **Objetivo:** Crear una Persona y su cuenta interna con roles permitidos.
- **Precondiciones:** Patentes exigidas; datos y rol interno válidos.
- **Flujo principal:** Completar alta unificada; validar duplicados/rol; crear persona, asignación y cuenta; enviar credenciales.
- **Alternativas relevantes:** Rol CLIENTE no elegible; duplicados o fallo de correo: rechazo/rollback según servicio.
- **Postcondiciones:** Alta interna coherente, con auditoría y primer ingreso pendiente.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: AD02.
- **Implementación:** UsuarioController.NuevoPersonal; AltaPersonalService, EmailService.
- **Vistas/interfaz:** Usuario/NuevoPersonal.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-013 — Administrar datos y estado de personas

- **Actor principal:** ADMIN, CAJA. **Secundarios:** —.
- **Objetivo:** Mantener personas existentes o crearlas por el circuito genérico autorizado.
- **Precondiciones:** Patentes de persona; restricciones de estado/identidad aplicables.
- **Flujo principal:** Consultar ficha; crear o editar datos; activar/desactivar cuando corresponda.
- **Alternativas relevantes:** Validación/duplicados/invariantes: rechazo; alta presencial CLIENTE usa CU-019.
- **Postcondiciones:** Persona mantenida sin alta implícita de cuenta.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: AD02, CO01.
- **Implementación:** PersonaController.Crear/Editar/Activar/Desactivar; PersonaService.
- **Vistas/interfaz:** Persona/Crear; Editar; Detalle; Index.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-014 — Administrar cuentas y su estado

- **Actor principal:** ADMIN, CAJA. **Secundarios:** Servidor de correo cuando el flujo lo requiere.
- **Objetivo:** Crear acceso a una Persona elegible y mantener datos/estado de cuenta.
- **Precondiciones:** Patentes de usuario y alcance de PuedeAdministrarPersonaAsync; alta interna reservada al administrador y Persona elegible.
- **Flujo principal:** ADMIN crea acceso interno elegible; consulta/edita/cambia estado en el alcance permitido. CAJA sólo administra cuentas de clientes exclusivos admitidos por el servicio.
- **Alternativas relevantes:** Persona CLIENTE no elegible para alta interna; duplicados o protecciones ADMIN: rechazo.
- **Postcondiciones:** Cuenta asociada correctamente; estado y auditoría coherentes.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: AD02.
- **Implementación:** UsuarioController.Crear/Editar/CambiarEstado; UsuarioService.
- **Vistas/interfaz:** Usuario/Crear; Editar; Detalle; Index.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-015 — Asignar o quitar roles internos a una Persona

- **Actor principal:** ADMIN. **Secundarios:** —.
- **Objetivo:** Ajustar responsabilidades internas permitidas.
- **Precondiciones:** Patentes requeridas y Persona elegible; rol interno permitido.
- **Flujo principal:** Consultar asignaciones; elegir rol del catálogo permitido; asignar/quitar.
- **Alternativas relevantes:** CLIENTE no asignable ni removible por este circuito; protecciones de ADMIN y cuenta sin rol válido se conservan.
- **Postcondiciones:** Asignaciones vigentes/históricas y auditoría actualizadas.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: AD03.
- **Implementación:** PersonaController.AdministrarRoles/AsignarRol/QuitarRol; PersonaService, RolService.
- **Vistas/interfaz:** Persona/AdministrarRoles.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-016 — Administrar catálogo de roles

- **Actor principal:** ADMIN. **Secundarios:** —.
- **Objetivo:** Mantener definición y estado de roles administrables.
- **Precondiciones:** Patentes de administración de roles.
- **Flujo principal:** Listar y consultar; crear/editar; cambiar estado cuando sea admisible.
- **Alternativas relevantes:** Roles protegidos y validaciones impiden operaciones incompatibles.
- **Postcondiciones:** Catálogo de roles actualizado, sin cambiar asignaciones por omisión.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: AD03.
- **Implementación:** RolController.Index/Detalle/Crear/Editar/CambiarEstado; RolService.
- **Vistas/interfaz:** Rol/Index; Detalle; Crear; Editar.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-017 — Configurar familias y composición de permisos

- **Actor principal:** ADMIN. **Secundarios:** —.
- **Objetivo:** Organizar patentes en familias y asociarlas a roles.
- **Precondiciones:** SEGURIDAD_ADMINISTRAR y restricciones de composición.
- **Flujo principal:** Consultar catálogo; crear/editar familia; asociar/quitar patente o familia hija; asociar/quitar familia de rol.
- **Alternativas relevantes:** Composición inválida/cíclica o protegida: rechazo; catálogo de patentes no es CRUD genérico.
- **Postcondiciones:** Composición autorizada actualizada y efectiva según resolución del servidor.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: AD03.
- **Implementación:** SeguridadController.Index/Familia/Patentes/Rol/Editar/Patente/Hija/FamiliaRol; SeguridadService, PermisoService.
- **Vistas/interfaz:** Seguridad/Index; Familia; Patentes; Rol; Editar.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-018 — Consultar personas y cuentas

- **Actor principal:** ADMIN, CAJA. **Secundarios:** —.
- **Objetivo:** Localizar una persona y conocer su situación personal y de acceso.
- **Precondiciones:** Patentes de consulta correspondientes.
- **Flujo principal:** Buscar/filtrar Personal o Clientes; abrir ficha y relación Persona/Cuenta.
- **Alternativas relevantes:** Sin resultados: listado vacío; cuenta inexistente: mostrar Sin cuenta sin crearla.
- **Postcondiciones:** Información autorizada consultada sin cambios.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: AD02, CO01.
- **Implementación:** UsuarioController.Administracion/PersonaCuenta; PersonaController.Index/Detalle/Clientes/Cliente; UsuarioService, PersonaService.
- **Vistas/interfaz:** Usuario/Administracion; PersonaCuenta; _PersonasCuentas; Persona/Clientes; Cliente; Index; Detalle.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-019 — Dar de alta cliente presencial

- **Actor principal:** CAJA, ADMIN. **Secundarios:** Cliente que aporta datos.
- **Objetivo:** Registrar un cliente del taller sin exigir una cuenta de portal.
- **Precondiciones:** Patentes de alta; datos obligatorios del formulario válidos.
- **Flujo principal:** Completar nombre/apellido/DNI/email y datos admitidos; validar duplicados; resolver CLIENTE en servidor; guardar.
- **Alternativas relevantes:** DNI/email duplicados o inválidos: rechazo sin fusionar; teléfono según validación específica del alta.
- **Postcondiciones:** Persona y PersonaRol CLIENTE creados; no se crea Usuario.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: CO01.
- **Implementación:** PersonaController.NuevoCliente; AltaClienteService.
- **Vistas/interfaz:** Persona/NuevoCliente.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

### Vehículos y catálogos

#### CU-020 — Administrar marcas

- **Actor principal:** ADMIN, CAJA. **Secundarios:** —.
- **Objetivo:** Mantener el catálogo de marcas utilizable por vehículos.
- **Precondiciones:** Patentes de consulta/modificación de vehículos según acción.
- **Flujo principal:** Listar/consultar; crear/editar; cambiar estado.
- **Alternativas relevantes:** Duplicados, entradas inválidas o restricciones de uso: rechazo.
- **Postcondiciones:** Marca persistida en estado permitido.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: AD04.
- **Implementación:** MarcaController; MarcaService.
- **Vistas/interfaz:** Marca/Index; Detalle; Crear; Editar.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-021 — Administrar modelos

- **Actor principal:** ADMIN, CAJA. **Secundarios:** —.
- **Objetivo:** Mantener modelos y su marca.
- **Precondiciones:** Patentes correspondientes; marca válida.
- **Flujo principal:** Consultar; crear/editar modelo; cambiar estado.
- **Alternativas relevantes:** Marca/datos no admisibles o conflictos: rechazo.
- **Postcondiciones:** Modelo relacionado con marca; detalle y navegación disponibles.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: AD04.
- **Implementación:** ModeloController; ModeloService.
- **Vistas/interfaz:** Modelo/Index; Detalle; Crear; Editar.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-022 — Administrar vehículos del taller

- **Actor principal:** CAJA, ADMIN. **Secundarios:** Cliente titular.
- **Objetivo:** Registrar y mantener vehículos asociados correctamente.
- **Precondiciones:** Patentes y titular CLIENTE elegible; catálogo válido.
- **Flujo principal:** Crear vehículo y titularidad inicial; editar datos; cambiar estado según reglas.
- **Alternativas relevantes:** Titular no elegible, patente/datos inválidos o relaciones incompatibles: rechazo.
- **Postcondiciones:** Vehículo y relación inicial coherentes; no implica transferir titularidad.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: CO01.
- **Implementación:** VehiculoController.Crear/Editar/CambiarEstado; VehiculoService, DominioVehicularService.
- **Vistas/interfaz:** Vehiculo/Crear; Editar; Detalle.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-023 — Registrar vehículo propio

- **Actor principal:** CLIENTE. **Secundarios:** —.
- **Objetivo:** Incorporar un vehículo al ámbito del cliente autenticado.
- **Precondiciones:** Cliente habilitado; datos de vehículo válidos.
- **Flujo principal:** Abrir Nuevo vehículo; elegir catálogo; completar y confirmar.
- **Alternativas relevantes:** Duplicados/conflicto de titularidad: rechazo sin apropiarse de vehículo ajeno.
- **Postcondiciones:** Vehículo registrado con titularidad del cliente resuelta en servidor.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: CL03.
- **Implementación:** PortalClienteController.NuevoVehiculo; VehiculoPropioAltaService.
- **Vistas/interfaz:** PortalCliente/NuevoVehiculo.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-024 — Consultar vehículos e historial autorizado

- **Actor principal:** CLIENTE, CAJA, ADMIN. **Secundarios:** —.
- **Objetivo:** Conocer ficha e información asociada de vehículos accesibles.
- **Precondiciones:** Patente de consulta o ownership propio.
- **Flujo principal:** Listar vehículos; abrir detalle; navegar a recursos relacionados autorizados.
- **Alternativas relevantes:** Vehículo ajeno para cliente o inexistente: denegar/no encontrado.
- **Postcondiciones:** Consulta sin alterar titularidad; historial visible según alcance del actor.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: CL03, CL04, CO01.
- **Implementación:** VehiculoController.Index/Detalle/MisVehiculos/Propio; PortalClienteController.MisVehiculos/Vehiculo; VehiculoService, DominioVehicularService.
- **Vistas/interfaz:** Vehiculo/Index; Detalle; PortalCliente/MisVehiculos; Vehiculo.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

### Turnos

#### CU-025 — Consultar agenda y turnos

- **Actor principal:** CAJA, ADMIN, CLIENTE. **Secundarios:** —.
- **Objetivo:** Conocer reservas, fechas y estados del ámbito autorizado.
- **Precondiciones:** Patentes internas o cliente habilitado para recursos propios.
- **Flujo principal:** Abrir agenda/listado propio; filtrar; consultar detalle.
- **Alternativas relevantes:** Sin reservas: estado vacío; turno ajeno para cliente: rechazo.
- **Postcondiciones:** Información de turnos consultada sin transición de estado.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: CO02, CL03.
- **Implementación:** TurnoController.Index/Agenda/Detalle/MisTurnos/Propio; PortalClienteController.MisTurnos/Turno; TurnoService, AgendaService.
- **Vistas/interfaz:** Turno/Index; Agenda; Detalle; PortalCliente/MisTurnos; Turno.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-026 — Reservar turno

- **Actor principal:** CAJA, ADMIN, CLIENTE. **Secundarios:** —.
- **Objetivo:** Obtener una reserva válida para un vehículo.
- **Precondiciones:** Cliente/vehículo elegibles y permisos; fecha/horario disponible.
- **Flujo principal:** Seleccionar vehículo y horario; validar cupo y turno activo; confirmar alta.
- **Alternativas relevantes:** Sin cupo, turno activo existente, datos inválidos o ownership incorrecto: rechazo.
- **Postcondiciones:** Reserva e historial coherentes; no se presupone concurrencia SQL ya ensayada.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: CO02, CL03.
- **Implementación:** TurnoController.Crear/CrearPropio/HorariosDisponibles/VehiculosDeCliente; PortalClienteController.NuevoTurno; TurnoService.CrearInternoAsync, AgendaService.
- **Vistas/interfaz:** Turno/Crear; PortalCliente/NuevoTurno; Turno/_HorariosScript.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-027 — Confirmar turno

- **Actor principal:** CAJA, ADMIN. **Secundarios:** —.
- **Objetivo:** Confirmar una reserva en estado admisible.
- **Precondiciones:** Patente y turno en estado permitido.
- **Flujo principal:** Seleccionar turno; validar estado vigente bajo protección; confirmar.
- **Alternativas relevantes:** Estado incompatible: rechazo sin historial/auditoría de éxito.
- **Postcondiciones:** Turno confirmado con registros asociados.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: CO02.
- **Implementación:** TurnoController.Confirmar; TurnoService.ConfirmarAsync.
- **Vistas/interfaz:** Turno/Detalle; Turno/_Lista.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-028 — Cancelar turno

- **Actor principal:** CAJA, ADMIN, CLIENTE. **Secundarios:** —.
- **Objetivo:** Anular una reserva aún cancelable.
- **Precondiciones:** Patente interna u ownership; transición permitida.
- **Flujo principal:** Seleccionar reserva; confirmar cancelación según formulario; registrar transición.
- **Alternativas relevantes:** Estado incompatible o turno ajeno: rechazo.
- **Postcondiciones:** Turno cancelado e historial coherente.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: CO03, CL03.
- **Implementación:** TurnoController.Cancelar/CancelarPropio; PortalClienteController.CancelarTurno; TurnoService.CancelarAsync.
- **Vistas/interfaz:** Turno/Detalle; PortalCliente/Turno.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-029 — Reprogramar turno

- **Actor principal:** CAJA, ADMIN. **Secundarios:** —.
- **Objetivo:** Cambiar una reserva a una fecha/horario admisible.
- **Precondiciones:** Patente, estado y disponibilidad requeridos.
- **Flujo principal:** Elegir nuevo horario; validar disponibilidad; confirmar reprogramación.
- **Alternativas relevantes:** Sin cupo/estado incompatible: conservar reserva previa.
- **Postcondiciones:** Reserva reprogramada con trazabilidad.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: CO03.
- **Implementación:** TurnoController.Reprogramar; TurnoService, AgendaService.
- **Vistas/interfaz:** Turno/Detalle.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-030 — Registrar ausencia del cliente

- **Actor principal:** CAJA, ADMIN. **Secundarios:** —.
- **Objetivo:** Dejar constancia de la inasistencia a una reserva.
- **Precondiciones:** Patente y condiciones temporales/de estado existentes.
- **Flujo principal:** Seleccionar turno; validar información vigente; marcar ausencia.
- **Alternativas relevantes:** Turno recibido/cancelado u otra condición no admisible: rechazo.
- **Postcondiciones:** Estado de ausencia e historial registrados sin ingreso u OT nuevos.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: CO03.
- **Implementación:** TurnoController.ClienteAusente; TurnoService.MarcarClienteAusenteAsync.
- **Vistas/interfaz:** Turno/Detalle.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

### Recepción y órdenes

#### CU-031 — Registrar recepción y apertura de OT

- **Actor principal:** CAJA, ADMIN. **Secundarios:** Cliente que verifica recepción.
- **Objetivo:** Admitir el vehículo al taller y abrir su trabajo.
- **Precondiciones:** Turno y cliente aptos; datos obligatorios de recepción completos.
- **Flujo principal:** Seleccionar turno; completar inspección/km/combustible/accesorios/verificación; validar estado vigente; crear ingreso y OT.
- **Alternativas relevantes:** Datos faltantes, turno incompatible o recepción existente: rechazo; fallo posterior: rollback.
- **Postcondiciones:** Ingreso, OT, estado e historial consistentes en la misma operación.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: CO04.
- **Implementación:** IngresoVehiculoController.Registrar; IngresoVehiculoService.RegistrarIngresoYCrearOrdenAsync.
- **Vistas/interfaz:** IngresoVehiculo/Registrar; Detalle.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-032 — Consultar recepciones y vehículos en taller

- **Actor principal:** CAJA, ADMIN. **Secundarios:** —.
- **Objetivo:** Localizar ingresos y conocer su situación.
- **Precondiciones:** Patentes de consulta de ingreso.
- **Flujo principal:** Abrir En taller o ingreso por turno; consultar detalle y OT.
- **Alternativas relevantes:** Sin ingresos o ID no disponible: respuesta controlada.
- **Postcondiciones:** Información de recepción consultada.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: CO04.
- **Implementación:** IngresoVehiculoController.EnTaller/PorTurno/Detalle; IngresoVehiculoService.
- **Vistas/interfaz:** IngresoVehiculo/EnTaller; Detalle.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-033 — Consultar órdenes y seguimiento del trabajo

- **Actor principal:** CAJA, ADMIN, MECANICO, CLIENTE. **Secundarios:** —.
- **Objetivo:** Conocer estado y avances de órdenes accesibles.
- **Precondiciones:** Patentes, asignación u ownership aplicables.
- **Flujo principal:** Abrir listado/pendientes/asignadas/propias; consultar detalle e historial permitido; navegar a diagnóstico/presupuesto/factura.
- **Alternativas relevantes:** OT ajena o detalle restringido: denegar; cliente sólo recibe información habilitada.
- **Postcondiciones:** Seguimiento consultado sin modificar OT.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: CO05, CO06, CL04.
- **Implementación:** OrdenTrabajoController.Index/Detalle/Pendientes/DeMecanico/MisOrdenes/Propia; PortalClienteController.MisOrdenes/Orden; OrdenTrabajoService.
- **Vistas/interfaz:** OrdenTrabajo/Index; Detalle; Pendientes; DeMecanico; PortalCliente/MisOrdenes; Orden.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-034 — Asignar responsable mecánico a una OT

- **Actor principal:** ADMIN, MECANICO. **Secundarios:** Mecánico destinatario en asignación administrativa.
- **Objetivo:** Establecer quién realizará el trabajo.
- **Precondiciones:** OT y mecánico elegibles. Por regla de negocio, sólo ADMIN asigna directamente; MECANICO toma una OT disponible sin asignar. CAJA no asigna. El control implementado de asignación directa exige ORDEN_ASIGNAR_MECANICO, sin comprobación adicional de ADMIN efectivo.
- **Flujo principal:** ADMIN elige un mecánico autorizado, o MECANICO toma voluntariamente una OT pendiente sin asignar; validar bajo bloqueo y guardar. La generación de OT durante la recepción de CAJA no incluye asignación.
- **Alternativas relevantes:** OT ocupada/no elegible o mecánico inválido: rechazo sin sobrescribir ni auditar éxito.
- **Postcondiciones:** Asignación única y trazable.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: CO05.
- **Implementación:** OrdenTrabajoController.AsignarMecanico/TomarOrden/MecanicosDisponibles; OrdenTrabajoService.AsignarMecanicoAsync/TomarOrdenAsync.
- **Vistas/interfaz:** OrdenTrabajo/Detalle; Pendientes; MecanicosDisponibles.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-035 — Actualizar datos operativos de la OT

- **Actor principal:** ADMIN, MECANICO. **Secundarios:** —.
- **Objetivo:** Mantener datos operativos editables sin alterar indebidamente el ciclo.
- **Precondiciones:** Patente, asignación y estado exigidos para cada campo.
- **Flujo principal:** Abrir OT; modificar urgencia, observaciones o costo de diagnóstico mediante la acción autorizada.
- **Alternativas relevantes:** Campo/estado/actor no permitido o importe inválido: rechazo.
- **Postcondiciones:** Datos admitidos actualizados y trazables; cada acción mantiene su autorización propia.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: CO06, CO12.
- **Implementación:** OrdenTrabajoController.CambiarUrgencia/ActualizarObservaciones/ActualizarCostoDiagnostico; OrdenTrabajoService.
- **Vistas/interfaz:** OrdenTrabajo/Detalle.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-036 — Registrar y revisar diagnóstico

- **Actor principal:** MECANICO, ADMIN. **Secundarios:** —.
- **Objetivo:** Documentar la evaluación técnica del trabajo.
- **Precondiciones:** OT accesible, asignación/estado y patente requeridos.
- **Flujo principal:** Iniciar diagnóstico; completar descripción; guardar; consultar versiones/historial autorizado.
- **Alternativas relevantes:** Estado/actor inválido o contenido no válido: rechazo.
- **Postcondiciones:** Diagnóstico actual e historial técnico preservados.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: CO06.
- **Implementación:** DiagnosticoController.Obtener/Iniciar/Guardar/Historial; DiagnosticoService.
- **Vistas/interfaz:** OrdenTrabajo/Detalle (diagnóstico integrado).
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-037 — Adjuntar y consultar evidencias del trabajo

- **Actor principal:** MECANICO, ADMIN. **Secundarios:** CLIENTE y personal autorizado como lectores.
- **Objetivo:** Documentar el trabajo con archivos privados consultables por autorizados.
- **Precondiciones:** OT y permiso admitidos; archivo válido para alta.
- **Flujo principal:** Abrir carga; adjuntar evidencia; guardar; consultar mediante descarga controlada.
- **Alternativas relevantes:** Archivo/OT/ownership no admisible: rechazo; no exponer almacenamiento privado directamente.
- **Postcondiciones:** Evidencia relacionada con OT y accesible sólo en su ámbito.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: CO06, CL04.
- **Implementación:** EvidenciaController.Crear/Archivo; EvidenciaTrabajoService, AlmacenEvidencias.
- **Vistas/interfaz:** Evidencia/Crear; Views/Shared/_Evidencias.cshtml.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-042 — Entregar vehículo y cerrar permanencia en taller

- **Actor principal:** CAJA, ADMIN. **Secundarios:** Cliente que retira.
- **Objetivo:** Registrar la entrega al cliente tras cumplir condiciones de cierre.
- **Precondiciones:** OT en estado admitido y factura/pagos completos según servicio.
- **Flujo principal:** Abrir OT; comprobar condiciones; confirmar entrega.
- **Alternativas relevantes:** Deuda, estado incompatible o entrega previa: rechazo.
- **Postcondiciones:** OT entregada y egreso único; historial coherente.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: CO11, CO12.
- **Implementación:** OrdenTrabajoController.Entregar; OrdenTrabajoService.
- **Vistas/interfaz:** OrdenTrabajo/Detalle.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

### Presupuestos y reparación

#### CU-038 — Elaborar y enviar presupuesto

- **Actor principal:** MECANICO, ADMIN. **Secundarios:** Cliente destinatario.
- **Objetivo:** Proponer trabajos/repuestos e importes para decisión del cliente.
- **Precondiciones:** OT/asignación/estado admitidos; catálogo técnico disponible.
- **Flujo principal:** Crear versión; agregar o quitar ítems; comprobar importes; enviar a aprobación.
- **Alternativas relevantes:** Datos/repuesto/estado inválidos o consulta fallida: mensaje en interfaz; no aprobar automáticamente.
- **Postcondiciones:** Presupuesto vigente enviado y pendiente de decisión.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: CO07, CO08.
- **Implementación:** PresupuestoController.Crear/AgregarItem/EliminarItem/EnviarAprobacion; PresupuestoService; StockService.ConsultasTecnicas.
- **Vistas/interfaz:** Presupuesto/Detalle; ErrorOperacion.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-039 — Consultar presupuesto y versiones accesibles

- **Actor principal:** MECANICO, ADMIN, CLIENTE. **Secundarios:** —.
- **Objetivo:** Conocer conceptos, totales y situación del presupuesto.
- **Precondiciones:** Patente/asignación u ownership según recorrido.
- **Flujo principal:** Abrir desde OT o Mis presupuestos; consultar detalle y versión disponible.
- **Alternativas relevantes:** Presupuesto ajeno/no disponible: rechazo; historial interno no se concede al cliente por defecto.
- **Postcondiciones:** Información autorizada consultada.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: CO08, CL04.
- **Implementación:** PresupuestoController.Detalle/Obtener/Propio/ObtenerPropio; PortalClienteController.MisPresupuestos; PresupuestoService.
- **Vistas/interfaz:** Presupuesto/Detalle; Propio; PortalCliente/MisPresupuestos.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-040 — Aprobar o rechazar presupuesto propio

- **Actor principal:** CLIENTE. **Secundarios:** —.
- **Objetivo:** Decidir si autoriza el trabajo propuesto.
- **Precondiciones:** Cliente habilitado; presupuesto propio y versión/estado decidible.
- **Flujo principal:** Revisar detalle; aprobar o rechazar con datos requeridos; confirmar.
- **Alternativas relevantes:** Versión incompatible, falta de stock u otras validaciones del servicio: no consolidar decisión inválida; reintento no debe duplicar efectos.
- **Postcondiciones:** Decisión, estado de OT y efectos de stock asociados coherentes; rechazo habilita el circuito previsto de diagnóstico.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: CO09, CO12.
- **Implementación:** PresupuestoController.Aprobar/Rechazar; PresupuestoService.
- **Vistas/interfaz:** Presupuesto/Propio.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-041 — Registrar ejecución y finalización de reparación

- **Actor principal:** MECANICO, ADMIN. **Secundarios:** —.
- **Objetivo:** Documentar realización del trabajo autorizado y dejarlo finalizado.
- **Precondiciones:** OT asignada, presupuesto/estado y permisos exigidos.
- **Flujo principal:** Iniciar reparación; completar datos/horas requeridos; finalizar.
- **Alternativas relevantes:** Sin aprobación, datos incompletos o transición incompatible: rechazo.
- **Postcondiciones:** Estado final e historial registrados; aún no equivale a entrega ni pago.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: CO10.
- **Implementación:** OrdenTrabajoController.IniciarReparacion/Finalizar; OrdenTrabajoService.
- **Vistas/interfaz:** OrdenTrabajo/Detalle.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

### Stock y proveedores

#### CU-043 — Administrar catálogo de repuestos

- **Actor principal:** STOCK, ADMIN. **Secundarios:** —.
- **Objetivo:** Mantener identidad, datos y estado de repuestos.
- **Precondiciones:** Patentes de stock correspondientes.
- **Flujo principal:** Consultar; crear con stock cero; editar datos; activar/desactivar.
- **Alternativas relevantes:** SKU duplicado, nombre/datos inválidos o regla de estado: mostrar error y conservar entrada; existencias se registran por movimientos.
- **Postcondiciones:** Catálogo actualizado sin fabricar stock inicial.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: ST01, ST03.
- **Implementación:** StockController.Index/Detalle/Crear/Editar/CambiarEstado; StockService.
- **Vistas/interfaz:** Stock/Index; Detalle; Crear; Editar.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-044 — Consultar repuestos técnicamente

- **Actor principal:** MECANICO, ADMIN. **Secundarios:** —.
- **Objetivo:** Localizar repuestos para el trabajo sin información comercial restringida.
- **Precondiciones:** Patente de consulta técnica.
- **Flujo principal:** Abrir menú Repuestos o selector técnico de presupuesto; buscar/consultar.
- **Alternativas relevantes:** Fallo de consulta: mensaje explícito; no exigir PROVEEDOR_VER para consulta técnica.
- **Postcondiciones:** Información técnica disponible sin ampliar permisos comerciales.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: CO07.
- **Implementación:** StockController.Index; StockService.ConsultasTecnicas; PresupuestoController.
- **Vistas/interfaz:** Stock/ConsultaTecnica; Presupuesto/Detalle.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-045 — Administrar proveedores

- **Actor principal:** STOCK, ADMIN. **Secundarios:** —.
- **Objetivo:** Mantener información y estado del proveedor de suministros.
- **Precondiciones:** Patentes de proveedor/stock pertinentes.
- **Flujo principal:** Listar/consultar; crear/editar; cambiar estado.
- **Alternativas relevantes:** Datos o relaciones no admisibles: rechazo.
- **Postcondiciones:** Proveedor mantenido con restricciones vigentes.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: ST02.
- **Implementación:** ProveedorController; ProveedorService.
- **Vistas/interfaz:** Proveedor/Index; Detalle; Crear; Editar.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-046 — Gestionar oferta de proveedores por repuesto

- **Actor principal:** STOCK, ADMIN. **Secundarios:** —.
- **Objetivo:** Mantener asociaciones, proveedor principal y precios de compra.
- **Precondiciones:** Permisos comerciales; repuesto/proveedor válidos.
- **Flujo principal:** Abrir proveedores del repuesto; asociar/quitar; elegir principal; actualizar precio.
- **Alternativas relevantes:** Relación duplicada, precio inválido o restricciones: rechazo.
- **Postcondiciones:** Relaciones comerciales consistentes; no cambia stock por editar precio.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: ST02.
- **Implementación:** StockController.AdministrarProveedores/AgregarProveedor/CambiarProveedorPrincipal/ActualizarPrecio/EliminarProveedor; StockService.Proveedores.
- **Vistas/interfaz:** Stock/AdministrarProveedores.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-047 — Registrar ingreso de stock

- **Actor principal:** STOCK, ADMIN. **Secundarios:** —.
- **Objetivo:** Incorporar existencias con trazabilidad de origen/lote.
- **Precondiciones:** Patente y datos de movimiento válidos.
- **Flujo principal:** Seleccionar repuesto/proveedor y cantidad/costo según formulario; registrar ingreso.
- **Alternativas relevantes:** Cantidad/costo/relación inválidos: rechazo sin saldo parcial.
- **Postcondiciones:** Movimiento, lotes y existencias coherentes.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: ST02.
- **Implementación:** StockController.RegistrarIngreso; StockService, StockService.Lotes.
- **Vistas/interfaz:** Stock/Detalle.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-048 — Registrar salida de stock

- **Actor principal:** STOCK, ADMIN. **Secundarios:** —.
- **Objetivo:** Documentar una salida manual autorizada de existencias.
- **Precondiciones:** Patente, saldo y datos de salida válidos.
- **Flujo principal:** Seleccionar repuesto y cantidad/motivo; validar; registrar.
- **Alternativas relevantes:** Saldo insuficiente o entrada inválida: rechazo.
- **Postcondiciones:** Salida y saldo trazables; no representa una segunda salida por aprobación de presupuesto.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: ST02.
- **Implementación:** StockController.RegistrarSalida; StockService, StockService.Lotes.
- **Vistas/interfaz:** Stock/Detalle.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-049 — Ajustar existencias

- **Actor principal:** STOCK, ADMIN. **Secundarios:** —.
- **Objetivo:** Corregir existencias mediante movimiento explícito autorizado.
- **Precondiciones:** Patente y justificación/datos de ajuste admitidos.
- **Flujo principal:** Completar ajuste; validar; registrar movimiento.
- **Alternativas relevantes:** Ajuste incompatible o inválido: rechazo.
- **Postcondiciones:** Saldo ajustado con trazabilidad, no edición directa del stock.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: ST02.
- **Implementación:** StockController.RegistrarAjuste; StockService.
- **Vistas/interfaz:** Stock/Detalle.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-050 — Consultar movimientos y trazabilidad de stock

- **Actor principal:** STOCK, ADMIN. **Secundarios:** —.
- **Objetivo:** Explicar los cambios de existencias y sus relaciones.
- **Precondiciones:** Patentes de consulta de stock.
- **Flujo principal:** Abrir movimientos/detalle; revisar entradas/salidas/ajustes y lotes disponibles.
- **Alternativas relevantes:** Sin movimientos: resultado vacío; recursos restringidos: denegar.
- **Postcondiciones:** Trazabilidad consultada sin movimientos nuevos.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: ST02, ST03.
- **Implementación:** StockController.Movimientos/Detalle; StockService, StockService.Lotes.
- **Vistas/interfaz:** Stock/Movimientos; Detalle.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

### Facturación y pagos

#### CU-051 — Emitir factura de una OT

- **Actor principal:** CAJA, ADMIN. **Secundarios:** —.
- **Objetivo:** Documentar el importe exigible del trabajo o diagnóstico rechazado.
- **Precondiciones:** OT facturable y permiso; condiciones de presupuesto/diagnóstico satisfechas.
- **Flujo principal:** Abrir OT; emitir factura; consultar conceptos/importe.
- **Alternativas relevantes:** Factura ya existente, estado o importes no válidos: rechazo.
- **Postcondiciones:** Factura relacionada con OT sin duplicación.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: CO11, CO12.
- **Implementación:** FacturaController.Emitir; FacturaService.
- **Vistas/interfaz:** OrdenTrabajo/Detalle; Factura/Detalle.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-052 — Consultar facturas y pagos

- **Actor principal:** CAJA, ADMIN, CLIENTE. **Secundarios:** —.
- **Objetivo:** Conocer conceptos, pagos y saldo autorizado.
- **Precondiciones:** Patente interna u ownership cliente.
- **Flujo principal:** Abrir listado/detalle; consultar pagos y saldo.
- **Alternativas relevantes:** Factura ajena para cliente o no disponible: rechazo.
- **Postcondiciones:** Consulta consistente con registros de facturación.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: CO11, CL04.
- **Implementación:** FacturaController.Index/MisFacturas/Detalle/PorId/DetallePropio/Pagos/PagosPropios; FacturaService.
- **Vistas/interfaz:** Factura/Index; MisFacturas; Detalle; Pagos; _TablaPagos.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-053 — Registrar pago de factura

- **Actor principal:** CAJA, ADMIN. **Secundarios:** Cliente pagador.
- **Objetivo:** Registrar cobro parcial o total y actualizar saldo.
- **Precondiciones:** Factura válida, permiso y saldo pendiente.
- **Flujo principal:** Elegir medio disponible; ingresar importe; validar y registrar.
- **Alternativas relevantes:** Sobrepago/importe inválido o error: rechazo sin duplicar pago.
- **Postcondiciones:** Pago y saldo persistidos; no se acredita una transacción bancaria real.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: CO11, CO12.
- **Implementación:** FacturaController.RegistrarPago; FacturaService; estrategias de pago existentes.
- **Vistas/interfaz:** Factura/RegistrarPago.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

### Postventa

#### CU-054 — Emitir garantía

- **Actor principal:** CAJA, ADMIN. **Secundarios:** Cliente beneficiario.
- **Objetivo:** Documentar cobertura de una operación elegible.
- **Precondiciones:** Factura/ítems y patente requeridos.
- **Flujo principal:** Seleccionar operación; indicar cobertura y vigencia según formulario; crear.
- **Alternativas relevantes:** Operación/ítem/datos no elegibles: rechazo.
- **Postcondiciones:** Garantía relacionada con la operación cubierta.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: PV01.
- **Implementación:** GarantiaController.Crear; GarantiaService.
- **Vistas/interfaz:** Garantia/Crear.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-055 — Consultar garantías y cobertura

- **Actor principal:** CAJA, ADMIN, CLIENTE. **Secundarios:** —.
- **Objetivo:** Conocer garantías, vigencia e ítems cubiertos.
- **Precondiciones:** Patente de consulta u ownership.
- **Flujo principal:** Listar generales/propias/por vehículo; abrir detalle; consultar cobertura.
- **Alternativas relevantes:** Garantía ajena/inexistente: denegar/no encontrado; vencida/anulada se refleja sin simular reclamo.
- **Postcondiciones:** Cobertura consultada sin emitir ni reclamar garantía.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: PV01, CL04.
- **Implementación:** GarantiaController.Index/Detalle/MisGarantias/PorVehiculo/EstaVigente/ItemEstaCubierto/DetallePropio/PorVehiculoPropio/Propia; GarantiaService.
- **Vistas/interfaz:** Garantia/Index; Detalle; MisGarantias; PorVehiculo; _Lista.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-056 — Anular garantía

- **Actor principal:** CAJA, ADMIN. **Secundarios:** —.
- **Objetivo:** Dejar sin vigencia una garantía por el circuito autorizado.
- **Precondiciones:** Patente y garantía en condición admitida.
- **Flujo principal:** Seleccionar garantía; confirmar anulación con información requerida.
- **Alternativas relevantes:** Estado/actor no permitido: rechazo.
- **Postcondiciones:** Garantía anulada y operación trazable.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: PV01.
- **Implementación:** GarantiaController.Anular; GarantiaService.
- **Vistas/interfaz:** Garantia/Detalle.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-057 — Calificar trabajo recibido

- **Actor principal:** CLIENTE. **Secundarios:** —.
- **Objetivo:** Registrar valoración de una OT propia elegible y consultar resultado.
- **Precondiciones:** Cliente habilitado; OT propia que admite calificación.
- **Flujo principal:** Abrir calificación; ingresar puntuación/comentario; validar y guardar.
- **Alternativas relevantes:** OT ajena, puntuación inválida o calificación no admisible: rechazo.
- **Postcondiciones:** Valoración asociada correctamente; no es evaluación de empleados genérica.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: PV01.
- **Implementación:** CalificacionController.Detalle/Crear; CalificacionTrabajoService.
- **Vistas/interfaz:** Calificacion/Detalle.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

### Consulta transversal y supervisión

#### CU-058 — Consultar resumen de actividad por perfil

- **Actor principal:** ADMIN, CAJA, MECANICO, STOCK, CLIENTE. **Secundarios:** —.
- **Objetivo:** Acceder a información y tareas relevantes desde el inicio autorizado.
- **Precondiciones:** Sesión y patentes; para portal, cliente habilitado.
- **Flujo principal:** Resolver destino; ver indicadores/listas/resumen; navegar al recurso permitido.
- **Alternativas relevantes:** Sin DASHBOARD_VER no ir al dashboard; sin capacidades usar destino seguro; vacíos manejados.
- **Postcondiciones:** Resumen consultado, sin nuevos privilegios.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: AC01, AD01, CL02.
- **Implementación:** DashboardController.Index; InicioController.Caja/Mecanico/Stock; PortalClienteController.Index; DashboardService, InicioOperativoService, PermisoService.
- **Vistas/interfaz:** Dashboard/Index; Inicio/Operativo; PortalCliente/Index.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-059 — Gestionar bandeja de notificaciones

- **Actor principal:** Usuario autenticado. **Secundarios:** Eventos del sistema como disparadores, no actores humanos.
- **Objetivo:** Conocer avisos propios y acceder al recurso relacionado.
- **Precondiciones:** Usuario destinatario autenticado.
- **Flujo principal:** Abrir bandeja; leer aviso; navegar al recurso; marcar uno o todos como leídos.
- **Alternativas relevantes:** Recurso no autorizado/no disponible: control servidor; no marcar notificación ajena.
- **Postcondiciones:** Avisos propios consultados y estado de lectura actualizado.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: PV02.
- **Implementación:** NotificacionController.Index/MarcarLeida/MarcarTodas/Recurso; NotificacionService.
- **Vistas/interfaz:** Notificacion/Index; layout.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-060 — Consultar auditoría

- **Actor principal:** ADMIN. **Secundarios:** —.
- **Objetivo:** Examinar eventos de actividad mediante filtros.
- **Precondiciones:** Patente de auditoría.
- **Flujo principal:** Abrir consulta; aplicar filtros; revisar resultados.
- **Alternativas relevantes:** Filtro inválido o sin coincidencias: resultado controlado; no exponer secretos.
- **Postcondiciones:** Eventos consultados sin alteración del registro.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: AD05.
- **Implementación:** AuditoriaController.Index; AuditoriaService.
- **Vistas/interfaz:** Auditoria/Index.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-061 — Obtener reportes de gestión

- **Actor principal:** ADMIN. **Secundarios:** —.
- **Objetivo:** Consultar información de gestión y obtener su presentación PDF.
- **Precondiciones:** Patente de reportes.
- **Flujo principal:** Elegir reporte y filtros disponibles; generar resultado; descargar PDF si se necesita.
- **Alternativas relevantes:** Filtros inválidos/sin datos: respuesta útil; no conceder consultas ajenas a patentes.
- **Postcondiciones:** Resultado o PDF emitido con los datos autorizados.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: AD05.
- **Implementación:** ReportesController.Index/Resultado/Pdf; ReportesService, ReportePdf.
- **Vistas/interfaz:** Reportes/Index; Resultado.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

### Operación técnica

#### CU-062 — Respaldar base y evidencias

- **Actor principal:** Operador técnico. **Secundarios:** SQL Server y sistema de archivos.
- **Objetivo:** Obtener respaldos recuperables mediante herramientas existentes.
- **Precondiciones:** Autorización explícita, destinos/permisos de backup y ventana según procedimiento.
- **Flujo principal:** Ejecutar scripts de base/evidencias; verificar resultado y checksum/VERIFYONLY cuando aplica; conservar archivos.
- **Alternativas relevantes:** Destino/permisos o verificación fallidos: detener; no declarar copia válida.
- **Postcondiciones:** Copias y evidencia de verificación generadas; no incluye scheduler nuevo.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: docs/backup-restore.md (T07).
- **Implementación:** scripts/backup.ps1; scripts/backup-evidencias.ps1; scripts/Backup.Common.ps1.
- **Vistas/interfaz:** Sin vista MVC; PowerShell.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-063 — Restaurar base y evidencias

- **Actor principal:** Operador técnico. **Secundarios:** SQL Server y sistema de archivos.
- **Objetivo:** Recuperar una instalación desde respaldos autorizados.
- **Precondiciones:** Autorización destructiva explícita; backup/destino verificados; acceso exclusivo.
- **Flujo principal:** Validar destino y copia; ejecutar restauración de base/evidencias; verificar consistencia según procedimiento.
- **Alternativas relevantes:** Incompatibilidad o fallo: detener; no improvisar recuperación.
- **Postcondiciones:** Datos/archivos restaurados al punto respaldado y pendientes de controles operativos correspondientes.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: docs/backup-restore.md (T07).
- **Implementación:** scripts/restore.ps1; scripts/restore-evidencias.ps1; scripts/Backup.Common.ps1.
- **Vistas/interfaz:** Sin vista MVC; PowerShell.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-064 — Verificar integridad DVH/DVV

- **Actor principal:** Operador técnico. **Secundarios:** SQL Server.
- **Objetivo:** Conocer si los datos protegidos conservan integridad.
- **Precondiciones:** Acceso de lectura/control según herramienta y configuración correcta.
- **Flujo principal:** Ejecutar comando verificar; revisar resultado por entidades y estado.
- **Alternativas relevantes:** Diferencias: informar integridad inválida; no recalcular para ocultarlas.
- **Postcondiciones:** Informe de integridad sin reparación automática.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: docs/integridad-dvh-dvv.md.
- **Implementación:** tools/Integridad/Program.cs (verificar); IntegridadService, EstadoIntegridad.
- **Vistas/interfaz:** Sin vista MVC; consola.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

#### CU-065 — Inicializar línea base de integridad autorizada

- **Actor principal:** Operador técnico. **Secundarios:** SQL Server.
- **Objetivo:** Establecer verificadores en el procedimiento explícito de puesta en servicio.
- **Precondiciones:** Backup y confirmaciones requeridos; ventana sin escrituras; autorización específica.
- **Flujo principal:** Ejecutar inicializar con controles del procedimiento; registrar verificadores; verificar resultado.
- **Alternativas relevantes:** Falta de backup/confirmaciones o estado incompatible: detener; no usar para encubrir alteraciones.
- **Postcondiciones:** Línea base creada y verificable; --sin-escrituras no significa simulación.
- **Requerimiento relacionado:** RF original no localizado; referencia de alcance/aceptación: docs/integridad-dvh-dvv.md.
- **Implementación:** tools/Integridad/Program.cs (inicializar); IntegridadService.
- **Vistas/interfaz:** Sin vista MVC; consola.
- **Estado:** implementado sin validación completa (I). Inspección estática; alcance de pruebas y aceptación pendiente en sección 6.

## 5. Matriz de trazabilidad documental

La relación es muchos-a-muchos: una aceptación puede recorrer varios CU y un CU puede tener varias aceptaciones. No sumar filas para contar casos. No se inventa correspondencia con RF originales ausentes.

| Referencia existente | Objetivo/cobertura | CU relacionados |
|---|---|---|
| AC01–AC02 | Acceso, sesión, destino y denegación | CU-001–004, CU-058; seguridad transversal de los privados |
| AD01 | Resumen administrativo | CU-058 |
| AD02 | Personal, personas y cuentas | CU-003–004, CU-012–014, CU-018 |
| AD03 | Roles y composición de seguridad | CU-015–017 |
| AD04 | Marcas/modelos | CU-020–021 |
| AD05 | Auditoría y reportes | CU-060–061 |
| CO01 | Cliente presencial y vehículo | CU-018–019, CU-022, CU-024 |
| CO02 | Agenda, reserva y confirmación | CU-025–027 |
| CO03 | Cancelación, reprogramación y ausencia | CU-028–030 |
| CO04 | Recepción y consulta | CU-031–032 |
| CO05 | Asignación por ADMIN, toma por MECANICO y rechazo de asignación por CAJA | CU-033–034; control de perfil efectivo explicado en sección 2 |
| CO06 | Diagnóstico, datos y evidencias | CU-033, CU-035–037 |
| CO07–CO08 | Consulta técnica y elaboración de presupuesto | CU-038–039, CU-044 |
| CO09 | Decisión y efecto de stock | CU-040, CU-050; consumo automático como paso interno |
| CO10 | Reparación/finalización | CU-041 |
| CO11–CO12 | Camino aprobado/rechazado, factura, pago, entrega | CU-035, CU-040–042, CU-051–053 |
| ST01–ST03 | Stock y proveedores | CU-043–050 |
| CL01 | Onboarding y acceso cliente | CU-001, CU-005–008 |
| CL02 | Datos e inicio cliente | CU-011, CU-058 |
| CL03 | Vehículos y turnos propios | CU-023–026, CU-028 |
| CL04 | Seguimiento, presupuesto, factura y garantía propios | CU-024, CU-033, CU-037, CU-039, CU-052, CU-055 |
| CL05 | Seguridad de cuenta cliente | CU-009–010 |
| PV01 | Garantías/calificación | CU-054–057 |
| PV02 | Avisos y acceso a antecedentes | CU-059; historiales incluidos en CU de consulta respectivos |
| backup-restore.md / T07 | Recuperabilidad de base y archivos | CU-062–063 |
| integridad-dvh-dvv.md | Verificación y puesta en servicio explícita | CU-064–065 |

Los controles transversales de [seguridad-autorizacion.md](seguridad-autorizacion.md) y las reglas operativas de [OPERACION-LOCAL-1.0.md](OPERACION-LOCAL-1.0.md) complementan estas relaciones; no añaden CU. Los requisitos de CSS, accesibilidad, rendimiento, CSP, integridad y concurrencia son condiciones de calidad o restricciones del recorrido, no objetivos adicionales del actor.

## 6. Trazabilidad de pruebas y validación pendiente

Todas las rutas/servicios citados son evidencia de implementación, no de ejecución satisfactoria. La siguiente tabla señala cobertura localizada; ningún grupo acredita por sí solo todo el CU.

| CU / ámbito | Evidencia automatizada disponible | Lo que todavía no demuestra |
|---|---|---|
| CU-001–011, CU-019 | `OnboardingClienteTests`, `RegistroCompletoClienteTests`, `NormalizacionEmailTests`, `PasswordValidatorTests`, `ReautenticacionClienteTests`, `InvitacionConfiguracionTests`, `ClienteInactivoMiddlewareTests` | Google/SMTP reales, todas las transiciones persistidas y recorridos de navegador actuales |
| CU-001–004, CU-010, formularios seguros | `CabecerasHtmlTests`, `EnlaceSeguroPresentacionTests`, `RecursosLayoutTests` | Renderizado móvil, entrega/consumo de enlaces reales y sesiones completas |
| CU-015–018, CU-058 y navegación por permisos | `ComposicionSeguridadTests`, `PermisosVistaTests`, `NavegacionCierreTests` | Matriz efectiva de cuentas de una base ni todos los POST autorizados/rechazados |
| CU-025–030 | `TurnoConsultasTests`, `TurnoAltaAtomicidadTests`, `TurnoTransicionesAtomicidadTests` | Contención y resultados simultáneos reales contra SQL Server |
| CU-031, CU-034 | `RecepcionLecturaVigenteTests`, `OrdenTomaAtomicidadTests` | Concurrencia real de recepción/toma/asignación y carga completa de negocio |
| CU-038, CU-043–050 | `StockPresupuestoRegresionTests` | Campaña integral de stock/lotes/presupuesto con persistencia real |
| CU-057 | `CalificacionValidacionTests` | Calificación completa en navegador y reglas con todas las relaciones persistidas |
| Vistas del catálogo | `TablasResponsiveTests`, `NavegacionCierreTests`, pruebas de recursos | Responsive real, teclado, contraste y zoom |
| Modelo e integridad | `ModeloEfTests`, `SerializadorCanonicoTests`, `ArranqueAceptacionTests` | Esquema/aplicación de migraciones e integridad actual de la base |
| Onboarding SQL | `OnboardingSqlIntegrationTests`, documento VALIDACION-12C1 | No se ejecutó aquí; no extrapolar evidencia histórica a todos los CU actuales |
| Concurrencia | 28 escenarios en `tests/MecaniCar360.ConcurrencyTests` | Ninguno ejecutado; B1 sigue limitado a coordinación, no observación de bloqueo efectivo |
| CU-062–065 | Scripts, herramienta y procedimientos documentados | Backup/restore/inicialización/VERIFYONLY actuales no ejecutados en este relevamiento |
| Resto del CORE y administración | Matriz ACEPTACION y código relacionado | Falta acta vigente de ejecución completa por perfil; no se deduce ausencia total de pruebas históricas |

Para cerrar la validación, usar los IDs de ACEPTACION existentes y añadir CU a cada evidencia: fecha, commit, entorno, actor, pasos, resultado y referencias sanitizadas. No repetir pruebas sólo para elevar el conteo. No ejecutar operaciones mutantes ni herramientas técnicas sin autorización; preservar `TEST_CIERRE10_A123`.

## 7. Duplicaciones resueltas y elementos fuera del recuento

| Elemento observado | Decisión y fundamento |
|---|---|
| Rutas propias en Turno/Vehiculo/OT/Factura/Garantia y pantallas PortalCliente | Un objetivo de consulta/reserva por recurso, con variante de ownership. No dos CU por HTML y JSON |
| Google ingresar/callback frente a login local | CU-001 para volver a entrar; CU-008 para alta/completar identidad nueva; callback es paso OAuth |
| Invitación pública y de CAJA | CU-005 y CU-006 tienen iniciadores y objetivos previos diferentes; convergen en CU-007, sin duplicar activación |
| Enlaces de agregar/cambiar/recuperar password | Un CU-010 con entradas alternativas; no un CU por token, GET, POST o pantalla Completada |
| Nueva Persona, PersonaRol y Usuario en un alta | Entidades de CU-012/CU-019/CU-007; no casos separados por inserción |
| Tomar OT y asignar mecánico | Variantes de CU-034; permanecen separadas sus autorizaciones y contratos técnicos |
| Iniciar/finalizar reparación | Etapas del objetivo CU-041; finalización no se confunde con entrega CU-042 |
| Historial de diagnóstico, OT o versiones de presupuesto | Parte de registrar/consultar el recurso; no “administrar historial” artificial |
| Evidencias: carga y Archivo | CU-037, con lectores secundarios y controles distintos; no CRUD de archivos públicos |
| Reportes Resultado/Pdf | CU-061; PDF es salida alternativa, no objetivo nuevo |
| Existencias, lotes, reservas/consumo de aprobación | Pasos de CU-040/CU-047–050; no inventar una pantalla de administración de lotes |
| Emisión automática de notificaciones, auditoría y verificadores | Efectos internos. Bandeja, consulta de auditoría y herramienta explícita de integridad sí son objetivos |
| Home/Index, Home/Error, AccesoDenegado y páginas NoDisponible | Navegación/información/alternativas; no nuevos objetivos funcionales |
| `DominioVehicularService.TransferirVehiculoAsync` y `FinalizarTitularidadAsync` | Se encontraron métodos, pero no invocaciones desde controladores/vistas u otro recorrido del actor en la búsqueda de símbolos. Capacidad de backend sin exposición identificada; no contar transferencia/baja de titularidad como CU utilizable |
| `EstadoReclamoGarantia` | Un enum no demuestra flujo de reclamo. No se encontró recorrido de gestión de reclamos; no se inventa ni se agrega como requisito pendiente |
| Harness, seeds, migraciones, bootstrap, arranque automático | Infraestructura de instalación/prueba y pasos internos; no objetivos web de negocio |
| Contratos/DTO, model binding, DI, reflexión y EF | No se declaran obsoletos por no tener referencias directas. Se revisan como soporte del recorrido, no se cuentan como CU |

### Comparación con documentación original

1. **CU originalmente documentados pero no implementados:** no hay un catálogo original disponible que permita producir una lista nominal fiable. Se registran **0 faltantes confirmados respecto de una fuente original**, no “cobertura original del 100 %”.
2. **Funciones implementadas ausentes de la documentación original:** no puede demostrarse esa ausencia sin el documento original. Onboarding mixto, enlaces de contraseña, evidencias, notificaciones, herramientas de backup/integridad y portal sí tienen soporte actual; su trazabilidad RF original queda pendiente, como la de los demás CU.
3. **Capacidades sin recorrido público identificado:** transferencia/finalización de titularidad y el enum de reclamos se documentan como evidencia limitada, fuera del total. No son automáticamente funcionalidades obligatorias pendientes.
4. **Responsabilidades aclaradas:** CAJA recibe y genera OT; MECANICO toma OT disponible sin asignar; ADMIN efectivo asigna directamente. La correccion P1 agrega la comprobacion explicita del perfil en el servicio, sin ampliar patentes. La aceptacion SQL y en navegador sigue pendiente.
5. Para completar la comparación académica basta aportar la especificación original y mapear sus RF a estos IDs. Mantener los CU estables; no multiplicarlos para coincidir con cantidad de pantallas.

## 8. Resumen cuantitativo

### Por módulo (suma sin duplicados)

| Módulo | CU | Implementados verificados | Implementados sin validación completa | Parciales | No implementados confirmados |
|---|---:|---:|---:|---:|---:|
| Acceso y cuenta | 4 | 0 | 4 | 0 | 0 |
| Alta y seguridad CLIENTE | 7 | 0 | 7 | 0 | 0 |
| Personas y seguridad administrativa | 8 | 0 | 8 | 0 | 0 |
| Vehículos y catálogos | 5 | 0 | 5 | 0 | 0 |
| Turnos | 6 | 0 | 6 | 0 | 0 |
| Recepción y órdenes | 8 | 0 | 8 | 0 | 0 |
| Presupuestos y reparación | 4 | 0 | 4 | 0 | 0 |
| Stock y proveedores | 8 | 0 | 8 | 0 | 0 |
| Facturación y pagos | 3 | 0 | 3 | 0 | 0 |
| Postventa | 4 | 0 | 4 | 0 | 0 |
| Consulta transversal y supervisión | 4 | 0 | 4 | 0 | 0 |
| Operación técnica | 4 | 0 | 4 | 0 | 0 |
| **Total** | **65** | **0** | **65** | **0** | **0** |

**61 CU de aplicación + 4 técnicos = 65 objetivos identificados.** Los 65 tienen implementación localizada; 65 quedan pendientes de validación completa según el criterio estricto de este catálogo. Esto no significa 65 defectos, ni 65 pruebas nunca realizadas. El número de requisitos originales pendientes **no es determinable** sin la fuente original.

### Por actor principal

Recuento de participación, no suma de casos distintos: un CU puede pertenecer a varios actores. Se expanden “Usuario autenticado” a los cinco perfiles y “Usuario interno” a los cuatro internos. CU-001 se atribuye al visitante antes de autenticarse; los perfiles que acceden son los cinco. No se atribuyen a ADMIN los CU exclusivos de cliente por su bypass. Los números expresan el ámbito del catálogo, no cuentas/permisos consultados en SQL.

| Actor | Cantidad | CU |
|---|---:|---|
| ADMIN | 51 | CU-002, CU-003, CU-004, CU-006, CU-012, CU-013, CU-014, CU-015, CU-016, CU-017, CU-018, CU-019, CU-020, CU-021, CU-022, CU-024, CU-025, CU-026, CU-027, CU-028, CU-029, CU-030, CU-031, CU-032, CU-033, CU-034, CU-035, CU-036, CU-037, CU-038, CU-039, CU-041, CU-042, CU-043, CU-044, CU-045, CU-046, CU-047, CU-048, CU-049, CU-050, CU-051, CU-052, CU-053, CU-054, CU-055, CU-056, CU-058, CU-059, CU-060, CU-061 |
| CAJA | 30 | CU-002, CU-003, CU-004, CU-006, CU-013, CU-014, CU-018, CU-019, CU-020, CU-021, CU-022, CU-024, CU-025, CU-026, CU-027, CU-028, CU-029, CU-030, CU-031, CU-032, CU-033, CU-042, CU-051, CU-052, CU-053, CU-054, CU-055, CU-056, CU-058, CU-059 |
| MECANICO | 14 | CU-002, CU-003, CU-004, CU-033, CU-034, CU-035, CU-036, CU-037, CU-038, CU-039, CU-041, CU-044, CU-058, CU-059 |
| STOCK | 12 | CU-002, CU-003, CU-004, CU-043, CU-045, CU-046, CU-047, CU-048, CU-049, CU-050, CU-058, CU-059 |
| CLIENTE | 18 | CU-002, CU-004, CU-009, CU-010, CU-011, CU-023, CU-024, CU-025, CU-026, CU-028, CU-033, CU-039, CU-040, CU-052, CU-055, CU-057, CU-058, CU-059 |
| Visitante/solicitante/invitado/pendiente | 5 | CU-001, CU-005, CU-007, CU-008, CU-010 |
| Operador técnico | 4 | CU-062–065 |

CLIENTE pendiente se considera en CU-008, no como cliente habilitado. CU-037 incluye además al CLIENTE como lector secundario. Google participa de apoyo en CU-001, CU-008 y CU-009. Correo participa de apoyo en CU-005–007, CU-010 y CU-012, y donde el mantenimiento de cuentas requiera comunicación. Estos actores secundarios no incrementan el total de 65.

## 9. Cobertura del inventario y conclusión

| Área de entrada | CU que evitan omisiones o duplicados |
|---|---|
| Account, GoogleCliente | CU-001–004, CU-008–009 |
| RegistroCliente, ActivacionCliente, RegistroCompletoCliente, InvitacionCliente, SeguridadCliente | CU-005–010 |
| Persona, Usuario, Rol, Seguridad | CU-012–019 |
| Marca, Modelo, Vehiculo | CU-020–024 |
| Turno | CU-025–030 |
| IngresoVehiculo, OrdenTrabajo, Diagnostico, Evidencia | CU-031–037, CU-041–042 |
| Presupuesto | CU-038–040 |
| Stock, Proveedor | CU-043–050 |
| Factura | CU-051–053 |
| Garantia, Calificacion | CU-054–057 |
| Dashboard, Inicio, Notificacion, Auditoria, Reportes | CU-058–061 |
| PortalCliente | CU-011, CU-023–026, CU-028, CU-033, CU-039, CU-058; navegación hacia otros CU propios |
| Home, shared/partials, JS/CSS | Soporte de navegación/presentación, no objetivos nuevos |
| Scripts y herramienta Integridad | CU-062–065 |

El flujo del taller queda trazado: **CU-019 → CU-022 → CU-026/027 → CU-031 → CU-034/036 → CU-038 → CU-040 → CU-041 → CU-051/053 → CU-042**, con stock CU-043–050 y consultas/postventa relacionadas. Las alternativas de rechazo/cancelación se conservan en sus fichas, sin crear recorridos inexistentes.

Este documento identifica implementación y límites; no declara completada la aceptación de MecaniCar360 1.0. La siguiente actividad académica es vincular evidencia real a los CU mediante la matriz existente y, si se dispone de ellos, incorporar los RF originales. No requiere nuevas funcionalidades, cambios de datos ni refactorizaciones.
