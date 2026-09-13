# Seguridad de Controllers y Services — MecaniCar360

Revisión de código y pruebas en memoria. Las familias actuales de CAJA se conservaron por confirmación del usuario. No se ejecutó el seed ni se modificó la base de la aplicación. No se agregaron migraciones en esta tarea; la migración de Facturación pertenece al trabajo anterior.

## Matriz de acciones

`OR` dentro de un atributo permite alternativas; atributos separados exigen todas las patentes. `conv.` indica selección convencional sin restricción HTTP explícita. Las denegaciones de ownership se devuelven como recurso no encontrado o error controlado, sin revelar datos ajenos.

| Controller | Acción | Patente | Ownership | Estado |
|---|---|---|---|---|
| AccountController | CambiarContraseña (conv.) | Excepción: cuenta propia | Cuenta autenticada; identidad por NameIdentifier | Excepción documentada |
| AccountController | CambiarContraseña (POST) | Excepción: cuenta propia | Cuenta autenticada; identidad por NameIdentifier | Excepción documentada |
| AccountController | CompletarDatos (conv.) | Excepción: cuenta propia | Cuenta autenticada; identidad por NameIdentifier | Excepción documentada |
| AccountController | CompletarDatos (POST) | Excepción: cuenta propia | Cuenta autenticada; identidad por NameIdentifier | Excepción documentada |
| AccountController | CompletarDatosExito (conv.) | Excepción: cuenta propia | Cuenta autenticada; identidad por NameIdentifier | Excepción documentada |
| AccountController | Login (conv.) | Excepción: público / autenticación | Credenciales; anónimo | Excepción documentada |
| AccountController | Login (POST) | Excepción: público / autenticación | Credenciales; anónimo | Excepción documentada |
| AccountController | Logout (POST) | Excepción: cuenta propia | Cuenta autenticada; identidad por NameIdentifier | Excepción documentada |
| DashboardController | Index (GET) | (CLIENTE_TURNO_VER OR CLIENTE_PRESUPUESTO_VER OR ORDEN_VER OR TURNO_VER OR FACTURA_VER OR STOCK_ALERTAS OR PAGO_VER) | Patentes por bloque; cliente propio / mecánico asignado | OK (código) |
| DiagnosticoController | Guardar (POST) | (DIAGNOSTICO_CREAR OR DIAGNOSTICO_MODIFICAR) | Mecánico asignado o ADMIN; patente específica en service | OK (código) |
| DiagnosticoController | Historial (GET) | (DIAGNOSTICO_HISTORIAL) | Mecánico asignado o ADMIN; patente específica en service | OK (código) |
| DiagnosticoController | Iniciar (POST) | (DIAGNOSTICO_CREAR) | Mecánico asignado o ADMIN; patente específica en service | OK (código) |
| DiagnosticoController | Obtener (GET) | (DIAGNOSTICO_VER) | Mecánico asignado o ADMIN; patente específica en service | OK (código) |
| FacturaController | Detalle (GET) | (FACTURA_VER) | Operación administrativa por patente | OK (código) |
| FacturaController | DetallePropio (GET) | (CLIENTE_FACTURA_VER) | Cliente de la OT | OK (código) |
| FacturaController | Emitir (POST) | (FACTURA_CREAR) | Operación administrativa por patente | OK (código) |
| FacturaController | Index (GET) | (FACTURA_VER) | Operación administrativa por patente | OK (código) |
| FacturaController | MisFacturas (GET) | (CLIENTE_FACTURA_VER) | Cliente de la OT | OK (código) |
| FacturaController | Pagos (GET) | (PAGO_VER) | Operación administrativa por patente | OK (código) |
| FacturaController | PagosPropios (GET) | (CLIENTE_FACTURA_VER) | Cliente de la OT | OK (código) |
| FacturaController | PorId (GET) | (FACTURA_VER) | Operación administrativa por patente | OK (código) |
| FacturaController | RegistrarPago (GET) | (PAGO_REGISTRAR) | Operación administrativa por patente | OK (código) |
| FacturaController | RegistrarPago (POST) | (PAGO_REGISTRAR) | Operación administrativa por patente | OK (código) |
| GarantiaController | Anular (POST) | (GARANTIA_ANULAR) | Operación administrativa por patente | OK (código) |
| GarantiaController | Crear (POST) | (GARANTIA_CREAR) | Operación administrativa por patente | OK (código) |
| GarantiaController | Detalle (GET) | (GARANTIA_VER) | Operación administrativa por patente | OK (código) |
| GarantiaController | EstaVigente (GET) | (GARANTIA_VER) | Operación administrativa por patente | OK (código) |
| GarantiaController | Index (GET) | (GARANTIA_VER) | Operación administrativa por patente | OK (código) |
| GarantiaController | ItemEstaCubierto (GET) | (GARANTIA_VER) | Operación administrativa por patente | OK (código) |
| GarantiaController | MisGarantias (GET) | (GARANTIA_VER_PROPIA) | Cliente de la OT | OK (código) |
| GarantiaController | PorVehiculo (GET) | (GARANTIA_VER) | Operación administrativa por patente | OK (código) |
| GarantiaController | Propia (GET) | (GARANTIA_VER_PROPIA) | Cliente de la OT | OK (código) |
| HomeController | Error (conv.) | Excepción: público / autenticación | Sin datos funcionales; público | Excepción documentada |
| HomeController | Index (conv.) | Excepción: público / autenticación | Sin datos funcionales; público | Excepción documentada |
| HomeController | Privacy (conv.) | Excepción: público / autenticación | Sin datos funcionales; público | Excepción documentada |
| IngresoVehiculoController | Detalle (GET) | (INGRESO_VER) | Operación por patente; sin autoridad desde IDs del formulario | OK (código) |
| IngresoVehiculoController | EnTaller (GET) | (INGRESO_VER) | Operación por patente; sin autoridad desde IDs del formulario | OK (código) |
| IngresoVehiculoController | PorTurno (GET) | (INGRESO_VER) | Operación por patente; sin autoridad desde IDs del formulario | OK (código) |
| IngresoVehiculoController | Registrar (GET) | (INGRESO_REGISTRAR) | Operación por patente; sin autoridad desde IDs del formulario | OK (código) |
| IngresoVehiculoController | Registrar (POST) | (INGRESO_REGISTRAR) | Operación por patente; sin autoridad desde IDs del formulario | OK (código) |
| MarcaController | CambiarEstado (POST) | (VEHICULO_MODIFICAR) | Operación por patente; sin autoridad desde IDs del formulario | OK (código) |
| MarcaController | Crear (conv.) | (VEHICULO_MODIFICAR) | Operación por patente; sin autoridad desde IDs del formulario | OK (código) |
| MarcaController | Crear (POST) | (VEHICULO_MODIFICAR) | Operación por patente; sin autoridad desde IDs del formulario | OK (código) |
| MarcaController | Detalle (conv.) | (VEHICULO_VER) | Operación por patente; sin autoridad desde IDs del formulario | OK (código) |
| MarcaController | Editar (conv.) | (VEHICULO_MODIFICAR) | Operación por patente; sin autoridad desde IDs del formulario | OK (código) |
| MarcaController | Editar (POST) | (VEHICULO_MODIFICAR) | Operación por patente; sin autoridad desde IDs del formulario | OK (código) |
| MarcaController | Index (conv.) | (VEHICULO_VER) | Operación por patente; sin autoridad desde IDs del formulario | OK (código) |
| ModeloController | CambiarEstado (POST) | (VEHICULO_MODIFICAR) | Operación por patente; sin autoridad desde IDs del formulario | OK (código) |
| ModeloController | Crear (conv.) | (VEHICULO_MODIFICAR) | Operación por patente; sin autoridad desde IDs del formulario | OK (código) |
| ModeloController | Crear (POST) | (VEHICULO_MODIFICAR) | Operación por patente; sin autoridad desde IDs del formulario | OK (código) |
| ModeloController | Detalle (conv.) | (VEHICULO_VER) | Operación por patente; sin autoridad desde IDs del formulario | OK (código) |
| ModeloController | Editar (conv.) | (VEHICULO_MODIFICAR) | Operación por patente; sin autoridad desde IDs del formulario | OK (código) |
| ModeloController | Editar (POST) | (VEHICULO_MODIFICAR) | Operación por patente; sin autoridad desde IDs del formulario | OK (código) |
| ModeloController | Index (conv.) | (VEHICULO_VER) | Operación por patente; sin autoridad desde IDs del formulario | OK (código) |
| OrdenTrabajoController | ActualizarCostoDiagnostico (POST) | (ORDEN_MODIFICAR) | Consulta según alcance; operación técnica exige mecánico asignado o ADMIN | OK (código) |
| OrdenTrabajoController | ActualizarObservaciones (POST) | (ORDEN_MODIFICAR) | Consulta según alcance; operación técnica exige mecánico asignado o ADMIN | OK (código) |
| OrdenTrabajoController | AsignarMecanico (POST) | (ORDEN_ASIGNAR_MECANICO) | Patente específica; reglas de asignación / entrega | OK (código) |
| OrdenTrabajoController | CambiarUrgencia (POST) | (ORDEN_MODIFICAR) | Consulta según alcance; operación técnica exige mecánico asignado o ADMIN | OK (código) |
| OrdenTrabajoController | DeMecanico (conv.) | (ORDEN_VER) | Consulta según alcance; operación técnica exige mecánico asignado o ADMIN | OK (código) |
| OrdenTrabajoController | Detalle (conv.) | (ORDEN_VER_DETALLE) | Consulta según alcance; operación técnica exige mecánico asignado o ADMIN | OK (código) |
| OrdenTrabajoController | Entregar (POST) | (ORDEN_ENTREGAR) | Patente específica; reglas de asignación / entrega | OK (código) |
| OrdenTrabajoController | Finalizar (POST) | (ORDEN_FINALIZAR) | Consulta según alcance; operación técnica exige mecánico asignado o ADMIN | OK (código) |
| OrdenTrabajoController | Index (conv.) | (ORDEN_VER) | Consulta según alcance; operación técnica exige mecánico asignado o ADMIN | OK (código) |
| OrdenTrabajoController | IniciarReparacion (POST) | (ORDEN_CAMBIAR_ESTADO) | Consulta según alcance; operación técnica exige mecánico asignado o ADMIN | OK (código) |
| OrdenTrabajoController | MecanicosDisponibles (conv.) | (ORDEN_ASIGNAR_MECANICO) | Patente específica; reglas de asignación / entrega | OK (código) |
| OrdenTrabajoController | MisOrdenes (GET) | (CLIENTE_ORDEN_VER) | Cliente de la OT | OK (código) |
| OrdenTrabajoController | Pendientes (conv.) | (ORDEN_VER) | Reglas de toma / órdenes disponibles | OK (código) |
| OrdenTrabajoController | Propia (GET) | (CLIENTE_ORDEN_VER) | Cliente de la OT | OK (código) |
| OrdenTrabajoController | TomarOrden (POST) | (ORDEN_MODIFICAR) | Reglas de toma / órdenes disponibles | OK (código) |
| PersonaController | Activar (POST) | (PERSONA_DESACTIVAR) | Por patente; personas ADMIN y asignación ADMIN restringidas | OK (código) |
| PersonaController | AdministrarRoles (conv.) | (PERSONA_VER) AND (ROL_VER) | Por patente; personas ADMIN y asignación ADMIN restringidas | OK (código) |
| PersonaController | AsignarRol (POST) | (ROL_MODIFICAR) | Por patente; personas ADMIN y asignación ADMIN restringidas | OK (código) |
| PersonaController | Crear (conv.) | (PERSONA_CREAR) | Por patente; personas ADMIN y asignación ADMIN restringidas | OK (código) |
| PersonaController | Crear (POST) | (PERSONA_CREAR) | Por patente; personas ADMIN y asignación ADMIN restringidas | OK (código) |
| PersonaController | Desactivar (POST) | (PERSONA_DESACTIVAR) | Por patente; personas ADMIN y asignación ADMIN restringidas | OK (código) |
| PersonaController | Detalle (conv.) | (PERSONA_VER) | Por patente; personas ADMIN y asignación ADMIN restringidas | OK (código) |
| PersonaController | Editar (conv.) | (PERSONA_MODIFICAR) | Por patente; personas ADMIN y asignación ADMIN restringidas | OK (código) |
| PersonaController | Editar (POST) | (PERSONA_MODIFICAR) | Por patente; personas ADMIN y asignación ADMIN restringidas | OK (código) |
| PersonaController | Index (conv.) | (PERSONA_VER) | Por patente; personas ADMIN y asignación ADMIN restringidas | OK (código) |
| PersonaController | QuitarRol (POST) | (ROL_MODIFICAR) | Por patente; personas ADMIN y asignación ADMIN restringidas | OK (código) |
| PresupuestoController | AgregarItem (POST) | (PRESUPUESTO_MODIFICAR) | Mecánico asignado o ADMIN | OK (código) |
| PresupuestoController | Aprobar (POST) | (CLIENTE_PRESUPUESTO_APROBAR) | Cliente de la OT o ADMIN; versión vigente al decidir | OK (código) |
| PresupuestoController | Crear (POST) | (PRESUPUESTO_CREAR) | Mecánico asignado o ADMIN | OK (código) |
| PresupuestoController | Detalle (GET) | (PRESUPUESTO_VER) | Mecánico asignado o ADMIN | OK (código) |
| PresupuestoController | EliminarItem (POST) | (PRESUPUESTO_MODIFICAR) | Mecánico asignado o ADMIN | OK (código) |
| PresupuestoController | EnviarAprobacion (POST) | (PRESUPUESTO_ENVIAR) | Mecánico asignado o ADMIN | OK (código) |
| PresupuestoController | Obtener (GET) | (PRESUPUESTO_VER) | Mecánico asignado o ADMIN | OK (código) |
| PresupuestoController | ObtenerPropio (GET) | (CLIENTE_PRESUPUESTO_VER) | Cliente de la OT o ADMIN; versión vigente al decidir | OK (código) |
| PresupuestoController | Propio (GET) | (CLIENTE_PRESUPUESTO_VER) | Cliente de la OT o ADMIN; versión vigente al decidir | OK (código) |
| PresupuestoController | Rechazar (POST) | (CLIENTE_PRESUPUESTO_RECHAZAR) | Cliente de la OT o ADMIN; versión vigente al decidir | OK (código) |
| ProveedorController | CambiarEstado (POST) | (PROVEEDOR_DESACTIVAR) | Operación por patente; sin autoridad desde IDs del formulario | OK (código) |
| ProveedorController | Crear (conv.) | (PROVEEDOR_CREAR) | Operación por patente; sin autoridad desde IDs del formulario | OK (código) |
| ProveedorController | Crear (POST) | (PROVEEDOR_CREAR) | Operación por patente; sin autoridad desde IDs del formulario | OK (código) |
| ProveedorController | Detalle (conv.) | (PROVEEDOR_VER) | Operación por patente; sin autoridad desde IDs del formulario | OK (código) |
| ProveedorController | Editar (conv.) | (PROVEEDOR_MODIFICAR) | Operación por patente; sin autoridad desde IDs del formulario | OK (código) |
| ProveedorController | Editar (POST) | (PROVEEDOR_MODIFICAR) | Operación por patente; sin autoridad desde IDs del formulario | OK (código) |
| ProveedorController | Index (conv.) | (PROVEEDOR_VER) | Operación por patente; sin autoridad desde IDs del formulario | OK (código) |
| RolController | CambiarEstado (POST) | (ROL_DESACTIVAR) | Por patente; ADMIN reservado; sin binding de familias/asignaciones | OK (código) |
| RolController | Crear (conv.) | (ROL_CREAR) | Por patente; ADMIN reservado; sin binding de familias/asignaciones | OK (código) |
| RolController | Crear (POST) | (ROL_CREAR) | Por patente; ADMIN reservado; sin binding de familias/asignaciones | OK (código) |
| RolController | Detalle (conv.) | (ROL_VER) | Por patente; ADMIN reservado; sin binding de familias/asignaciones | OK (código) |
| RolController | Editar (conv.) | (ROL_MODIFICAR) | Por patente; ADMIN reservado; sin binding de familias/asignaciones | OK (código) |
| RolController | Editar (POST) | (ROL_MODIFICAR) | Por patente; ADMIN reservado; sin binding de familias/asignaciones | OK (código) |
| RolController | Index (conv.) | (ROL_VER) | Por patente; ADMIN reservado; sin binding de familias/asignaciones | OK (código) |
| StockController | ActualizarPrecio (POST) | (STOCK_MODIFICAR) | Operación por patente; sin autoridad desde IDs del formulario | OK (código) |
| StockController | AdministrarProveedores (conv.) | (STOCK_MODIFICAR) | Operación por patente; sin autoridad desde IDs del formulario | OK (código) |
| StockController | AgregarProveedor (POST) | (STOCK_MODIFICAR) | Operación por patente; sin autoridad desde IDs del formulario | OK (código) |
| StockController | CambiarEstado (POST) | (STOCK_MODIFICAR) | Operación por patente; sin autoridad desde IDs del formulario | OK (código) |
| StockController | CambiarProveedorPrincipal (POST) | (STOCK_MODIFICAR) | Operación por patente; sin autoridad desde IDs del formulario | OK (código) |
| StockController | Crear (conv.) | (STOCK_CREAR) | Operación por patente; sin autoridad desde IDs del formulario | OK (código) |
| StockController | Crear (POST) | (STOCK_CREAR) | Operación por patente; sin autoridad desde IDs del formulario | OK (código) |
| StockController | Detalle (conv.) | (STOCK_VER) | Operación por patente; sin autoridad desde IDs del formulario | OK (código) |
| StockController | Editar (conv.) | (STOCK_MODIFICAR) | Operación por patente; sin autoridad desde IDs del formulario | OK (código) |
| StockController | Editar (POST) | (STOCK_MODIFICAR) | Operación por patente; sin autoridad desde IDs del formulario | OK (código) |
| StockController | EliminarProveedor (POST) | (STOCK_MODIFICAR) | Operación por patente; sin autoridad desde IDs del formulario | OK (código) |
| StockController | Index (conv.) | (STOCK_VER) | Operación por patente; sin autoridad desde IDs del formulario | OK (código) |
| StockController | Movimientos (conv.) | (STOCK_MOVIMIENTO) | Operación por patente; sin autoridad desde IDs del formulario | OK (código) |
| StockController | RegistrarAjuste (POST) | (STOCK_MOVIMIENTO) | Operación por patente; sin autoridad desde IDs del formulario | OK (código) |
| StockController | RegistrarIngreso (POST) | (STOCK_MOVIMIENTO) | Operación por patente; sin autoridad desde IDs del formulario | OK (código) |
| StockController | RegistrarSalida (POST) | (STOCK_MOVIMIENTO) | Operación por patente; sin autoridad desde IDs del formulario | OK (código) |
| TurnoController | Agenda (conv.) | (TURNO_VER) | Operación administrativa por patente | OK (código) |
| TurnoController | Cancelar (POST) | (TURNO_CANCELAR) | Operación administrativa por patente | OK (código) |
| TurnoController | CancelarPropio (POST) | (CLIENTE_TURNO_CANCELAR) | Cliente de turno; titular del vehículo al crear | OK (código) |
| TurnoController | ClienteAusente (POST) | (TURNO_MODIFICAR) | Operación administrativa por patente | OK (código) |
| TurnoController | Confirmar (POST) | (TURNO_CONFIRMAR) | Operación administrativa por patente | OK (código) |
| TurnoController | Crear (POST) | (TURNO_CREAR) | Operación administrativa por patente | OK (código) |
| TurnoController | Crear (GET) | (TURNO_CREAR) | Operación administrativa por patente | OK (código) |
| TurnoController | CrearPropio (POST) | (CLIENTE_TURNO_CREAR) | Cliente de turno; titular del vehículo al crear | OK (código) |
| TurnoController | Detalle (conv.) | (TURNO_VER) | Operación administrativa por patente | OK (código) |
| TurnoController | HorariosDisponibles (GET) | (TURNO_VER OR TURNO_CREAR OR TURNO_MODIFICAR OR CLIENTE_TURNO_CREAR) | Operación administrativa por patente | OK (código) |
| TurnoController | Index (conv.) | (TURNO_VER) | Operación administrativa por patente | OK (código) |
| TurnoController | MisTurnos (GET) | (CLIENTE_TURNO_VER) | Cliente de turno; titular del vehículo al crear | OK (código) |
| TurnoController | Propio (GET) | (CLIENTE_TURNO_VER) | Cliente de turno; titular del vehículo al crear | OK (código) |
| TurnoController | Reprogramar (POST) | (TURNO_MODIFICAR) | Operación administrativa por patente | OK (código) |
| UsuarioController | CambiarEstado (POST) | (USUARIO_DESACTIVAR) | ADMIN; CAJA sólo cuentas de clientes sin roles privilegiados | OK (código) |
| UsuarioController | Crear (conv.) | (USUARIO_CREAR) | ADMIN; CAJA sólo cuentas de clientes sin roles privilegiados | OK (código) |
| UsuarioController | Crear (POST) | (USUARIO_CREAR) | ADMIN; CAJA sólo cuentas de clientes sin roles privilegiados | OK (código) |
| UsuarioController | Detalle (conv.) | (USUARIO_VER) | ADMIN; CAJA sólo cuentas de clientes sin roles privilegiados | OK (código) |
| UsuarioController | Editar (conv.) | (USUARIO_MODIFICAR) | ADMIN; CAJA sólo cuentas de clientes sin roles privilegiados | OK (código) |
| UsuarioController | Editar (POST) | (USUARIO_MODIFICAR) | ADMIN; CAJA sólo cuentas de clientes sin roles privilegiados | OK (código) |
| UsuarioController | Index (conv.) | (USUARIO_VER) | ADMIN; CAJA sólo cuentas de clientes sin roles privilegiados | OK (código) |
| VehiculoController | CambiarEstado (POST) | (VEHICULO_MODIFICAR) | Operación administrativa por patente | OK (código) |
| VehiculoController | Crear (conv.) | (VEHICULO_CREAR) | Operación administrativa por patente | OK (código) |
| VehiculoController | Crear (POST) | (VEHICULO_CREAR) | Operación administrativa por patente | OK (código) |
| VehiculoController | Detalle (conv.) | (VEHICULO_VER) | Operación administrativa por patente | OK (código) |
| VehiculoController | Editar (POST) | (VEHICULO_MODIFICAR) | Operación administrativa por patente | OK (código) |
| VehiculoController | Editar (conv.) | (VEHICULO_MODIFICAR) | Operación administrativa por patente | OK (código) |
| VehiculoController | Index (conv.) | (VEHICULO_VER) | Operación administrativa por patente | OK (código) |
| VehiculoController | MisVehiculos (GET) | (CLIENTE_VEHICULO_VER) | Titularidad vigente del cliente | OK (código) |
| VehiculoController | ObtenerModelos (GET) | (VEHICULO_VER OR VEHICULO_CREAR OR VEHICULO_MODIFICAR) | Operación administrativa por patente | OK (código) |
| VehiculoController | Propio (GET) | (CLIENTE_VEHICULO_VER) | Titularidad vigente del cliente | OK (código) |

## Excepciones y equivalencias

- Account: Login valida credenciales; CompletarDatos y CambiarContraseña sólo reciben como autoridad el UsuarioId resuelto de Claims, verifican Usuario/Persona activos y conservan las validaciones existentes. Logout sólo cierra la cuenta actual. No hay patente funcional equivalente necesaria para operar la cuenta propia.
- Home: páginas públicas sin consultas de recursos funcionales.
- Dashboard: no se creó una patente nueva; cada bloque exige su patente. La vista utiliza las decisiones del controller y no claims de rol como autorización.
- Marca/Modelo: catálogos vehiculares. Consultas con VEHICULO_VER; selectores también admiten VEHICULO_CREAR/MODIFICAR; mantenimiento con VEHICULO_MODIFICAR.
- Calificación: no existe controller. ObtenerPorOrdenTrabajoAsync usa CLIENTE_ORDEN_VER y ownership; ObtenerTodasAsync usa ORDEN_VER y limita al mecánico; CrearAsync usa la nueva CLIENTE_CALIFICACION_CREAR, porque una consulta de orden no equivale a escribir una calificación. Conserva la regla de orden entregada, una calificación y titularidad vigente; agrega cliente de la OT e identidad desde Usuario. La nueva patente se asocia a MIS_ORDENES en el seed.
- Garantía: GARANTIA_VER_PROPIA ya existía; se agrega su asociación a MIS_ORDENES. No se concede la familia administrativa GARANTIAS al cliente.
- Evidencia: no existe controller. Lectura técnica con DIAGNOSTICO_VER; creación con ORDEN_MODIFICAR; ambas exigen asignación o ADMIN. El cliente consulta sólo evidencias publicadas a través de sus PresupuestoVersion, no el repositorio técnico completo.
- Stock: movimientos manuales requieren STOCK_MOVIMIENTO; un stock inicial no nulo también. ProcesarAprobacionIncrementalAsync es interno, revalida CLIENTE_PRESUPUESTO_APROBAR, ownership, estado y versión vigente, y lee los ítems persistidos. ReservarParaOrdenAsync es privado. Se conserva el cálculo incremental usando todos los EgresoOrdenTrabajo de la OT/repuesto.
- Agenda.ValidarDisponibilidadAsync y DominioVehicular.EsTitularActualAsync son auxiliares internos sin endpoint, usados dentro de operaciones autorizadas; las consultas públicas exigen patente.
- NotificacionService: consultas y marcado resuelven Persona desde Usuario activo y filtran ownership. Son operaciones de cuenta propia sin patente equivalente; no hay controller. NotificarAsync y EmailService.EnviarCorreoAsync son internos y se usan como efectos de operaciones autorizadas/observers; no hay endpoint de envío libre.
- PermisoService: infraestructura de autorización (consultas de patentes, administrador y usuario activo), no endpoints funcionales. El bypass ADMIN permanece limitado a usuarios y personas activos.

## Alcance de Services

Se revisaron todos los archivos de Services. Las operaciones funcionales públicas exigen solicitante y patente/ownership directamente o mediante el helper autorizado que invocan. Las operaciones auxiliares y de cuenta propia quedan detalladas arriba. No se implementaron Singleton de sesión ni AuditoriaService.
- AccountService.cs
- AgendaService.cs
- CalificacionTrabajoService.cs
- DiagnosticoService.cs
- DominioVehicularService.cs
- EmailService.cs
- EvidenciaTrabajoService.cs
- FacturaService.cs
- GarantiaService.cs
- IngresoVehiculoService.cs
- MarcaService.cs
- ModeloService.cs
- NotificacionService.cs
- OrdenTrabajoService.cs
- PermisoService.cs
- PersonaService.cs
- PresupuestoService.cs
- ProveedorService.cs
- RolService.cs
- StockService.cs
- TurnoService.cs
- UsuarioService.cs
- VehiculoService.cs

## Comprobación

EF InMemory: 394 comprobaciones exitosas, incluyendo los nueve casos solicitados, positivos de ownership, usuario inactivo, calificación propia/ajena, proveedor por patente, bloqueo de cuentas con roles mixtos y rechazo de grafos de asignaciones. Se revisaron 151 acciones por reflexión, incluidas patentes y antiforgery en todos los POST.
El fixture reconstruye familias y patentes desde InicializadorBD sin ejecutar el inicializador, y utiliza UsuarioId distintos de PersonaId. Los registros de prueba existieron sólo en memoria. Estas comprobaciones no prueban concurrencia SQL ni el recorrido visual de pantallas.

## Pendientes operativos fuera del alcance de esta revisión

Validación final: `dotnet build MecaniCar360.sln --no-restore -t:Rebuild` terminó con **0 errores y 40 advertencias** de nulabilidad (CS8601, CS8602 y CS8618). `git diff --check` terminó con código 0; Git emitió avisos de normalización LF/CRLF, sin errores de espacios. No quedan usos de `Authorize(Roles = ...)` ni `User.IsInRole(...)` en Controllers/Services.

Los endpoints propios incorporados en Vehiculo/Turno/OrdenTrabajo y Garantia.Propia devuelven JSON con proyecciones explícitas. No se crearon sus vistas ni formularios. Persisten pantallas faltantes o incompletas de otros módulos; no se declara cerrado el recorrido funcional por UI.
Para que CLIENTE reciba las nuevas asociaciones, deberá ejecutarse el seed normal al iniciar la aplicación con su esquema actualizado. No se arrancó la aplicación después de estos cambios. Se detuvo la instancia del workspace que bloqueaba el build.
