# Advertencias de compilación — criterio de cierre 1.0

15/10/2026, build nuevo `CierreDoc`, 0 errores y **26 CS8618**, sin CS8602. No se deshabilitó nullable ni se modificaron modelos. Ninguna advertencia por sí sola demuestra NullReferenceException.

## Inventario completo

| Archivo en Models/Clases | Propiedades y líneas | Cantidad / clasificación |
|---|---|---|
| Diagnostico.cs | OrdenTrabajo:8, DiagnosticoHistorial.Diagnostico:22 | 2 navegaciones EF |
| Diagnostico.cs | DescripcionActual:10, DiagnosticoHistorial.Descripcion:24 | 2 textos |
| DominioVehicular.cs | Vehiculo:8, Persona:11 | 2 navegaciones EF |
| MovimientoStock.cs | Repuesto:10, RealizadoPorUsuario:24 | 2 navegaciones EF |
| Pago.cs | Factura:10, RegistradoPorUsuario:19 | 2 navegaciones EF |
| Presupuesto.cs | OrdenTrabajo:10, PresupuestoItem.Presupuesto:48, PresupuestoHistorial.Presupuesto:58, Mecanico:67 | 4 navegaciones EF |
| Presupuesto.cs | PresupuestoItem.Descripcion:38, PresupuestoHistorial.Motivo:64 | 2 textos |
| Repuesto.cs | ProveedorRepuesto.Proveedor:70, Repuesto:74 | 2 navegaciones EF |
| Repuesto.cs | Repuesto.SKU:11, Nombre:15; Proveedor.Nombre:49, Apellido:51, Telefono:53, Email:55 | 6 textos |
| Usuario.cs | Username:10, EmailLogin:13 | 2 textos |

Total: 14 navegaciones y 12 textos. CS8618 informa ausencia de asignación en constructor; inicializadores de objetos, model binding y materialización EF ocurren después.

## Decisión justificada

- **Navegaciones: aceptables como deuda documentada para esta entrega**, no garantía de no-null universal. Las altas utilizan FK o relación (por ejemplo DiagnosticoService, DominioVehicularService, FacturaService, StockService.Lotes); EF no garantiza navegación cargada sólo por existir FK. Corregir el consumidor si se demuestra una desreferencia sin carga/guard. No convertir propiedades persistidas en nullable ni asignar entidades ficticias para silenciar warnings.
- **Textos: requieren atención en aceptación, no corrección preventiva del modelo.** DiagnosticoService asigna descripción normalizada e historial; PresupuestoService valida descripción y asigna motivo; Stock usa validación y Repuesto declara Required en SKU/Nombre; altas de usuario asignan Username/EmailLogin en UsuarioService, AltaPersonalService, GoogleClienteService e InvitacionClienteService. Proveedor conserva validación del servicio/formulario. Esta revisión no demuestra que un consumidor válido alcance null; tampoco certifica todas las ramas posibles.
- Pruebas manuales pertinentes: vacíos/espacios en alta/edición de repuesto/proveedor y diagnóstico/ítems; formulario conserva valores; rechazo sin persistencia parcial. Prepararlas sobre fixture autorizado, no modificar datos preservados.
- **Corrección obligatoria ahora: ninguna demostrada por estas advertencias.** Si una prueba falla, registrar archivo/método/entrada y corregir localmente antes de aceptar ese recorrido. No agregar `!`, cadenas vacías ni `required` indiscriminadamente: puede alterar contratos y model binding sin arreglar el consumidor.

## Precisión monetaria

Es un aviso EF distinto de CS8618. `ModeloEfTests` comprueba nueve campos `decimal(18,2)`: Factura.Total, FacturaItem.PrecioUnitario, OrdenTrabajo.CostoDiagnostico, Pago.Monto, Presupuesto.Total, PresupuestoHistorial.TotalAnterior, PresupuestoItem.PrecioUnitario, ProveedorRepuesto.PrecioCompraActual y Repuesto.PrecioVenta. No hubo cambios de esquema ni índices. Rango representable: hasta 16 dígitos enteros y 2 decimales; eso no autoriza importes de negocio arbitrarios.

La suite demuestra metadatos, no comportamiento SQL en límites. Mantener aceptación de importes fraccionarios ordinarios, entradas inválidas y sobrepago. No generar migración sólo para eliminar avisos; elevar a defecto si se demuestra pérdida monetaria o aceptación indebida.
