# Integridad DVH / DVV — BLOQUE 11B

## Alcance y límites

La integridad detecta alteraciones de los datos y del conjunto de filas de las siguientes doce tablas. No sustituye las restricciones, permisos SQL, auditoría ni backups. SHA-256 no es una firma autenticada: un atacante con control completo de la base que recalcule también los verificadores puede eludir este control.

El registro de `Data/Integridad/RegistroEntidadesProtegidas.cs` define explícitamente este orden de campos:

| Tabla | Campos, en orden |
|---|---|
| Usuarios | Id, Username, EmailLogin, PasswordHash, SecurityStamp, Activo, PrimerLogin, PersonaId, FechaCreacion |
| Personas | Id, Nombre, Apellido, Dni, Telefono, Email, Activo |
| Roles | Id, Nombre, EsRolCliente, FechaCreacion, Activo |
| PersonaRoles | PersonaId, RolId, FechaAlta, FechaBaja, OtorgadoPorUsuarioId |
| Facturas | Id, OrdenTrabajoId, PresupuestoOrigenId, PresupuestoVersionOrigenId, Estado, Total, FechaEmision, NumeroFactura, Observaciones, LinkPago, QRPago |
| FacturaItems | Id, FacturaId, Descripcion, Cantidad, PrecioUnitario |
| Pagos | Id, FacturaId, Monto, FechaPago, MetodoPago, Estado, RegistradoPorUsuarioId |
| Repuestos | Id, SKU, Nombre, Marca, Modelo, Compatibilidad, PrecioVenta, StockActual, StockMinimo, Activo, FechaCreacion |
| LotesRepuesto | Id, CodigoLote, RepuestoId, ProveedorRepuestoId, CantidadIngresada, CantidadDisponible, PrecioCompra, FechaIngreso |
| MovimientosStock | Id, RepuestoId, ProveedorRepuestoId, Cantidad, Fecha, Tipo, RealizadoPorUsuarioId, OrdenTrabajoId, Observaciones |
| MovimientosStockLote | MovimientoStockId, LoteRepuestoId, Cantidad |
| Auditorias | Id, UsuarioId, Accion, Entidad, EntidadId, Descripcion, Fecha |

La cobertura es parcial: Familia, Patente, Presupuesto, OrdenTrabajo, Garantia, Vehiculo, Turno, EvidenciaTrabajo, Notificacion, InvitacionCliente e IdentidadExterna no están protegidas por este bloque. Tampoco lo están propiedades de las doce entidades que no figuran en la tabla. No se incluyen navegaciones.

## Representación versión 1

`T(null) = N;`. Para cualquier valor no nulo: `T(valor) = V<longitud en bytes UTF-8>:<valor>;`. Se usa UTF-8 sin BOM, sin normalizar strings; vacío y null son diferentes. Enteros y enums se representan en decimal invariante; bool como 0/1; decimal como `0.00`; DateTime como `yyyy-MM-ddTHH:mm:ss.fffffff`, sin conversión de zona horaria. La precisión decimal corresponde al esquema actual. Cambiar precisión, campos o semántica requiere diseñar una nueva versión, no recalcular silenciosamente la existente.

DVH es SHA-256 hexadecimal mayúscula de `MC360|DVH|1|T(tabla)|T(campo1)|...`, con barras separadoras entre tokens. Incluye las claves. Se calcula desde valores leídos de SQL después del guardado, incluyendo identity, defaults, redondeo decimal y datetime2. PasswordHash y SecurityStamp participan pero nunca se imprimen en resultados o incidentes.

DVV utiliza hash incremental sobre `MC360|DVV|1|T(tabla)|T(cantidad)` seguido de cada clave y su DVH almacenado. Las claves compuestas aportan un token por componente. El orden es el ORDER BY numérico de la PK, no orden textual de IDs. Una tabla vacía tiene su propio DVV determinista. La verificación recalcula independientemente los DVH de los datos y el DVV del conjunto de claves/DVH almacenados.

`DVH` es propiedad sombra `varchar(64) NULL`, con CHECK de longitud y hexadecimal mayúscula. NULL permite la transición técnica, pero después de inicializar se considera `DVH_FALTANTE`. `DigitosVerificadoresVerticales` contiene una fila por tabla, versión, hash, cantidad y fecha UTC; no tiene DVH ni una FK polimórfica.

## Escrituras y transacciones

Las cuatro variantes SaveChanges del contexto delegan en el coordinador. Éste identifica tablas afectadas, incluidas cascadas de eliminación, verifica sus datos persistidos antes de escribir, ejecuta el guardado base sin aceptar cambios, lee los valores SQL, mantiene DVH y DVV mediante comandos internos y confirma únicamente si es dueño de la transacción. Respeta `acceptAllChangesOnSuccess=false` y no invoca SaveChanges recursivamente.

Un contexto que modificó una fila previamente cargada también debe conservar su DVH original: una escritura con snapshot obsoleto se rechaza como concurrencia. No se deben usar entidades desconectadas para sobrescribir filas protegidas sin cargarlas primero.

Todas las transacciones EF adquieren al comenzar `MecaniCar360:Integridad:Escritura:v1` mediante `sp_getapplock`, Exclusive, owner Transaction, timeout de 10 segundos. Se toma antes de los locks de negocio. Una transacción ajena sin esta marca se rechaza; TransactionScope no está soportado. Las lecturas de integridad mantienen locks de tabla hasta finalizar, incluyendo protección contra inserciones externas durante el cálculo.

Un fallo revierte toda la transacción, incluida una transacción del caller, y marca contexto/transacción como no reutilizables. No hace Clear del tracker ni permite confirmar después de un fallo. El caller debe descartar el contexto y reintentar la operación completa cuando corresponda.

El mutex serializa también transacciones EF de módulos no protegidos. Operaciones largas (por ejemplo SMTP dentro de una transacción) pueden causar esperas y timeout. No se permiten escrituras SQL directas, ExecuteUpdate o ExecuteDelete sobre tablas protegidas como vía alternativa de mantenimiento: se detectarán en una verificación posterior. La verificación no es vigilancia continua; se ejecuta al arrancar, técnicamente a demanda y antes de escribir las tablas afectadas. Una alteración externa posterior al startup puede verse en una lectura antes de su detección.

## Instalación e inicialización histórica

La migración `20261005195845_AgregarIntegridadDVHDVV` agrega solamente las columnas, checks y tabla de verificadores. No certifica los datos históricos. La herramienta independiente `tools/Integridad` no ejecuta el startup web, seed ni migraciones.

Procedimiento operativo, con aplicación y otros escritores detenidos:

1. Preservar y verificar un backup FULL con CHECKSUM de la base destino según BLOQUE 10. Conservar también evidencias físicas cuando corresponda. La ventana sin escrituras comprende backup, migración e inicialización.
2. Aplicar la migración aprobada mediante EF, fuera del arranque web. La factoría de diseño evita ejecutar el seed.
3. Ejecutar la inicialización explícita con la misma configuración externa y entorno de la aplicación:

   ```powershell
   dotnet run --project tools/Integridad/Integridad.csproj -- inicializar --proyecto . --backup "<ruta del backup accesible para SQL Server>" --confirmar-linea-base --sin-escrituras
   dotnet run --project tools/Integridad/Integridad.csproj -- verificar --proyecto .
   ```

4. Sólo habilitar la aplicación si la herramienta devuelve `INTEGRIDAD=Valida` y código de salida 0.

La herramienta exige un único backup FULL con CHECKSUM cuyo nombre de base coincida, y ejecuta RESTORE VERIFYONLY. Los flags son una confirmación del operador, no una prueba de que todos los escritores externos estén detenidos. No se pasan contraseñas por argumentos. User Secrets sólo se cargan con `ASPNETCORE_ENVIRONMENT=Development`; las variables de entorno conservan precedencia. El operador debe comprobar la base efectiva antes de ejecutar.

La inicialización requiere tabla DVV vacía y todas las columnas DVH sin inicializar. Lee todo el esquema requerido antes de escribir; crea los doce DVV, verifica y confirma dentro de una transacción. Rechaza una línea base existente o parcial. No existe endpoint web de recálculo. En una instalación sin datos se aplica el mismo procedimiento: inicializar tablas vacías antes del primer seed.

La base de desarrollo no fue migrada ni inicializada por esta implementación. El nuevo binario responderá 503 sobre el esquema anterior hasta realizar este procedimiento. No usar su arranque como mecanismo de actualización de la línea base.

## Startup, bloqueo e incidentes

El proceso comienza NoVerificada. Verifica esquema requerido y versión/DVH/DVV antes de ejecutar InicializadorBD. Si falla, no ejecuta seed y queda Comprometida; si pasa, ejecuta el inicializador normal con mantenimiento centralizado y vuelve a verificar antes de marcar Valida.

El middleware, anterior a autenticación y sesión, devuelve HTTP 503 para toda ruta cuando el estado no es Valida. Usa HTML fijo sin layout, acceso a usuarios ni estilos inline; muestra sólo mensaje genérico e ID de incidente. No hay bypass ADMIN ni recuperación web. La CSP existente permanece intacta.

Los incidentes no se escriben en Auditoria. Se almacenan como JSON privado fuera de SQL, por defecto `App_Data/Integridad`, excluido de Git; ubicación configurable mediante `Integridad:DirectorioIncidentes`. No admite una ubicación bajo wwwroot y rechaza directorios con enlaces/reparse points. Configurar ACL de sistema operativo para la identidad de la aplicación y operadores autorizados. Si el log no puede escribirse, el sistema sigue bloqueado y sólo informa el ID por stderr.

El registro contiene estado, timestamp UTC, versión, ID, entidad, clave numérica cuando corresponde y tipo. No contiene valores de negocio, hashes esperados/encontrados, credenciales ni datos completos. El estado Comprometida queda enclavado en el proceso; recuperarlo requiere intervención técnica y nuevo arranque verificado.

## Recuperación con BLOQUE 10

Preservar el incidente; detener el entorno comprometido; seleccionar backup; restaurarlo aisladamente mediante el procedimiento de BLOQUE 10; ejecutar la herramienta `verificar` sobre esa base; verificar también los archivos de Evidencias y su correspondencia. Sólo habilitar el entorno recuperado si ambas verificaciones pasan. Un backup SQL estructuralmente válido puede contener datos adulterados: VERIFYONLY no sustituye DVH/DVV. No inicializar ni recalcular sobre la base comprometida para hacer desaparecer el error.

## Validación

Ejecución de cierre del 05/10/2026: 65 verificaciones PASS en SQL Server Express 2022, sobre bases aisladas creadas para el bloque, sin adulterar desarrollo. Incluyeron inicialización con backup verificado, representación canónica, alteraciones externas, operaciones EF, rollback y transacciones externas, mutex concurrente/timeout, seed, gate HTTP real, logs privados y restore válido/adulterado. Las regresiones focalizadas de cuentas, PersonaRol, Stock, Factura/Pago y Auditoria también pasaron. `Database.HasPendingModelChanges()` devolvió false.

Mediciones orientativas en esa ejecución: INSERT + UPDATE de Persona, 636 ms; escritura conjunta de 100 auditorías, 125 ms; verificación completa posterior, 49 ms. No equivalen a pruebas de carga productivas ni garantizan esos tiempos con otra cantidad de datos. El timeout del mutex se probó manteniendo otra transacción abierta durante sus 10 segundos de espera. Se eliminaron las bases temporales al finalizar; los verificadores de prueba no forman parte de la aplicación.
