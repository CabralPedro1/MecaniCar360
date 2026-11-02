# Componentes visuales — fase B

## Navegación superpuesta — actualización posterior a clase 12

El menú usa el offcanvas local de Bootstrap 5.1 en todos los tamaños: cerrado inicialmente, sin columna permanente. Conserva los cinco grupos existentes (Inicio, Operaciones, Taller, Administración y Gestión), sus destinos y condiciones Razor. Los grupos vacíos se ocultan; los de una opción presentan el acceso directamente. `details/summary` conserva la interacción nativa de teclado; el módulo actual se abre al cargar.

El logo completo conserva `Account/Inicio`: ADMIN/CAJA llegan a Agenda según la resolución existente, MECANICO a Mi trabajo, STOCK a Inventario y CLIENTE a su portal según sus permisos efectivos. No se añaden permisos ni rutas. Diagnóstico, presupuesto y demás operaciones contextuales siguen disponibles desde sus pantallas existentes.

Bootstrap controla Escape, backdrop y confinamiento del foco. El mismo botón hamburguesa se traslada dentro del panel mientras está abierto y vuelve al encabezado con foco al cerrar. La selección ordinaria de un enlace cierra el panel. En detalles reconocidos, la referencia de ubicación usa el texto y destino de una opción ya renderizada y autorizada; no inventa nombres de rutas ni repite el título de página. Las siete vistas independientes con `Layout = null` no cambian.

Aceptación pendiente (no ejecutada en navegador): para cada perfil, comprobar a 320, 375, 768 y 1366 px y zoom 200 %: apertura/cierre con el mismo botón, Escape, clic exterior, selección de destino, Tab/Shift+Tab sin salir del panel abierto, Enter/Espacio en submenús, retorno de foco, opción actual, grupos de una opción, scroll vertical y ausencia de columna vacía/desbordamiento. Comprobar también Mi cuenta, notificaciones y retorno mediante logo. Las pruebas JavaScript con DOM simulado no certifican estas interacciones reales ni el renderizado responsive. Reiniciar la aplicación requiere autorización; esta modificación no la reinicia.

Alcance: presentación de tres pilotos, sin cambios de reglas, rutas, permisos ni contratos. No constituye aceptación visual en navegador.

## Aplicación gradual

Los componentes viven en `wwwroot/css/site.css`, bajo `.mc-app .mc-components`. El contenedor es opt-in: no agregarlo a otras vistas sin revisar su contenido. Se reutilizan los colores de fase A y Bootstrap 5.1.0; sidebar y Mi cuenta no se modifican. No hay nuevos scripts ni parciales.

Pilotos originales de fase B (antecedente):

- `Views/Turno/Index.cshtml`: calendario/listado con filtros, tabla, capacidad y acciones. Los anchos fijos de encabezados pasan a clases; se conserva el ancho dinámico de la barra de capacidad y toda su lógica.
- `Views/IngresoVehiculo/Registrar.cshtml`: formulario de recepción, validación, casillas y grupos de radio. Se conservan IDs, nombres, atributos, mensajes, antiforgery y script de kilometraje.
- `Views/IngresoVehiculo/Detalle.cshtml`: tarjeta de datos, situación y acciones autorizadas.

## Convenciones

| Componente | Uso |
|---|---|
| `mc-page-header` | Título y futuras acciones existentes, con separación y flex-wrap; no crea acciones |
| `mc-toolbar` | Filtros existentes con controles legibles; `mc-filter-field` ocupa el ancho en móvil |
| `mc-form-panel` | Superficie y espaciado del formulario; etiquetas `form-label`, ayuda `form-text`, opciones `mc-choice` |
| Validaciones | Conservar texto y clases de MVC/jQuery; no ocultar errores ni mostrar resúmenes vacíos artificialmente |
| Obligatorios | Conservar `required`, metadatos y explicaciones existentes. No inferir obligatoriedad desde CSS ni agregar asteriscos engañosos a reglas condicionales |
| `mc-table` / `mc-table-heading` | Encabezado claro, padding y alineación superior; conservar todas las columnas, datos y acciones |
| `table-responsive` | Scroll horizontal de la tabla, sin ocultar columnas. Mantener etiqueta de región y foco existentes |
| `mc-actions` | Separación y ajuste a varias filas de enlaces y formularios existentes |
| Botones | `btn-primary`: principal; `btn-secondary`/`btn-outline-secondary`: secundaria; `btn-outline-primary`: consulta. Conservar `btn-danger` para destructivas y las variantes semánticas existentes. No recolorear todas las acciones a azul |
| Deshabilitados | Bootstrap conserva opacidad/estado; campos deshabilitados con superficie diferenciada. CSS nunca sustituye `disabled` ni autorización |
| Alertas | Conservar `alert-success/danger/warning/info`, roles y mensajes; sólo unificar padding, radio y separación |
| `mc-state` | Estado textual neutro, sin asignar significados nuevos a colores |
| `mc-empty` | Mensaje vacío existente con separación; no inventar ausencia de datos |
| `mc-facts` | Lista `dl` en dos columnas, apilada en móvil; no alterar el orden de lectura |

No se modifican las vistas independientes `Layout = null` de activación/seguridad, Login ni OAuth. Tampoco se extiende esta fase a Stock, Presupuesto o los demás módulos. Las clases de tarjetas, botones y foco de fase A continúan siendo la base; no se duplican en un nuevo sistema.

## Verificación pendiente

No se inicia la aplicación ni se usa SQL en esta fase. La validación visual de Razor renderizado y de datos reales requiere una sesión autorizada.

La lista original de pilotos se conserva como antecedente; la matriz integral siguiente cubre las fases posteriores. Revisar a 320, 375, 768 y 1366 px y zoom 200 %:

1. Turnos: filtro de fecha, tabla con/sin turnos, capacidad, desplazamiento por teclado y todos los enlaces de cada registro.
2. Recepción: etiquetas, radios/casillas, textos largos, errores visibles, ayudas de kilometraje y botones. Guardar una recepción requiere autorización de datos separada; no usar `TEST_CIERRE10_A123` para avanzar estados.
3. Detalle: datos largos, situación En taller/Egresado y acciones visibles según permisos. Confirmar que el apilado móvil conserva cada relación etiqueta/valor.
4. Tab/Shift+Tab, foco visible, controles deshabilitados y mensajes con lector de pantalla. Confirmar que sidebar y Mi cuenta no presentan regresiones.

La inspección de contratos y la suite no SQL no certifican estos recorridos visuales ni efectos persistidos.


## Fase E: revisi?n est?tica y l?mites

Correcciones puntuales: asociaci?n expl?cita de las etiquetas Usuario/Email con sus controles de s?lo lectura en Account/CompletarDatos; extensi?n del foco compartido a summary (versiones/historiales). Sin cambios de nombres de campos, POST, rutas ni datos. Las tablas inspeccionadas tienen contenedores responsive. Esto no acredita ausencia de desbordamiento con datos reales.

No se inici? la aplicaci?n, navegador ni SQL durante esta fase. Permanecen sin comprobar: recorte del foco en regiones desplazables, textos largos, contraste renderizado de estados din?micos, comportamiento del dropdown/offcanvas al cambiar resoluci?n y zoom. No son defectos confirmados por falta de prueba. Las vistas independientes conservan su aislamiento; deben incluirse en aceptaci?n posterior de enlaces reales.

## Matriz de aceptaci?n visual integral

En **cada fila**, ejecutar por separado 320, 375, 768 y 1366 px de ancho, y repetir con zoom 200 %. Registrar viewport efectivo y navegador. Teclado: Tab/Shift+Tab, Enter/Espacio y Escape donde corresponda; foco visible, orden l?gico y ausencia de trampas. El desplazamiento horizontal debe quedar dentro de tablas, conservando columnas y acciones. Formularios: etiquetas, ayudas, errores, controles deshabilitados y textos largos legibles. No enviar operaciones mutantes sin autorizaci?n de datos.

| Pantalla / m?dulo | Perfil autorizado (seg?n patentes) | Tablas / formularios y acciones cr?ticas | Resultado esperado | 320/375/768/1366 | Zoom 200 % | Teclado | Estado / evidencia |
|---|---|---|---|---|---|---|---|
| Layout, sidebar, Mi cuenta | Cinco perfiles | Abrir/cerrar men?, grupos, notificaciones y logout POST | Accesos completos, dropdown legible, foco retorna al disparador | Pendiente | Pendiente | Pendiente | Pendiente; no ejecutado |
| Login, primer ingreso y contrase?a | Visitante / usuario correspondiente | Mostrar contrase?a, errores, Google y guardar datos | Controles legibles y mensajes ?ntegros; sin cambios de identidad | Pendiente | Pendiente | Pendiente | Pendiente; no ejecutado |
| Registro, completar registro y recuperaci?n | Visitante / CLIENTE pendiente o titular | Solicitudes, errores y confirmaciones | Mensajes neutros y campos asociados | Pendiente | Pendiente | Pendiente | Pendiente; no ejecutado |
| Activaci?n y restablecimiento independientes | Titular del enlace | Enlace vigente, inv?lido y vencido | Aislamiento y recursos locales conservados | Pendiente | Pendiente | Pendiente | Pendiente; no ejecutado |
| Dashboard, inicio operativo, reportes y auditor?a | ADMIN; inicio seg?n perfil | Filtros, indicadores, tablas, gr?ficos y PDF existente | Datos y acciones sin cortes; valores legibles | Pendiente | Pendiente | Pendiente | Pendiente; no ejecutado |
| Personas, usuarios, roles y seguridad | ADMIN; CAJA en alcance permitido | Listados, alta/edici?n, roles, familias | Acciones s?lo autorizadas, errores visibles | Pendiente | Pendiente | Pendiente | Pendiente; no ejecutado |
| Marcas, modelos y veh?culos | ADMIN / CAJA autorizados | Cat?logos, selecci?n dependiente, fichas | Etiquetas y datos conservados, tablas desplazables | Pendiente | Pendiente | Pendiente | Pendiente; no ejecutado |
| Portal, datos y recursos propios | CLIENTE habilitado | Veh?culos, turnos, ?rdenes, seguridad de cuenta | S?lo recursos propios; vinculaci?n conserva reautenticaci?n | Pendiente | Pendiente | Pendiente | Pendiente; no ejecutado |
| Turnos y recepci?n | CAJA / ADMIN; propios CLIENTE | Horarios, formularios, estados, recepci?n | Campos completos; acciones seg?n estado y permiso | Pendiente | Pendiente | Pendiente | Pendiente; no ejecutado |
| OT, diagn?stico, reparaci?n y evidencias | MECANICO / ADMIN; consultas autorizadas | Tomar/asignar, diagn?stico, horas y carga | CAJA no asigna; no desaparecen validaciones ni acciones | Pendiente | Pendiente | Pendiente | Pendiente; no ejecutado |
| Presupuestos internos y propios | MECANICO / ADMIN consulta; CLIENTE decide | ?tems, versiones, summary, aprobar/rechazar | ADMIN sin decisi?n; propietario habilitado conserva acciones | Pendiente | Pendiente | Pendiente | Pendiente; no ejecutado |
| Facturas, pagos y entrega | CAJA / ADMIN; consulta propia CLIENTE | Importes, historial, pago y entrega | Totales legibles, confirmaciones y estados ?ntegros | Pendiente | Pendiente | Pendiente | Pendiente; no ejecutado |
| Stock, proveedores y consulta t?cnica | STOCK / ADMIN; t?cnica MECANICO | Ingreso, salida, ajuste, precios desplegables | Cantidades, asociaciones y restricciones visibles | Pendiente | Pendiente | Pendiente | Pendiente; no ejecutado |
| Garant?as, calificaci?n y notificaciones | CAJA / ADMIN; recursos propios CLIENTE; avisos propios | Cobertura, anulaci?n, puntuaci?n y lectura | Ownership, mensajes y controles conservados | Pendiente | Pendiente | Pendiente | Pendiente; no ejecutado |
| Estados vac?os, acceso denegado y errores | Seg?n recorrido | Sin registros, errores de validaci?n y retornos | Mensajes visibles y destino autorizado | Pendiente | Pendiente | Pendiente | Pendiente; no ejecutado |

Para aprobar una fila: completar todas sus combinaciones aplicables y registrar fecha, commit m?s cambios locales, perfil, navegador, resoluci?n, zoom, pasos, resultado y captura sanitizada. Si falla, indicar reproducci?n y archivo probable. No guardar secretos, tokens ni datos personales en evidencias. Esta matriz complementa ACEPTACION-1.0.md; no reemplaza sus criterios funcionales.

## Circuitos funcionales pendientes en SQL Server aislado

Todos **NO EJECUTADOS** en fase E. Requieren autorizaci?n separada para preparar la base y escribir fixtures; no usar ni avanzar TEST_CIERRE10_A123. No ejecutar migraciones, seeds o inicializaci?n de integridad por el solo hecho de aprobar esta matriz.

1. Cliente ? veh?culo ? reserva ? confirmaci?n ? recepci?n y OT: comprobar estados, validaciones y ausencia de duplicados.
2. Toma voluntaria por MECANICO y asignaci?n por ADMIN; rechazo a CAJA sin efectos persistidos.
3. Diagn?stico ? presupuesto/versiones ? decisi?n del propietario habilitado. ADMIN y terceros no deciden; comprobar rechazo, versi?n obsoleta y stock insuficiente sin cambios parciales.
4. Aprobaci?n ? consumo de stock ? reparaci?n ? finalizaci?n ? factura ? pagos ? entrega. Verificar saldo, sobrepago rechazado, egreso ?nico y trazabilidad.
5. Presupuesto rechazado ? cobro de diagn?stico seg?n reglas existentes ? entrega; no suponer reparaci?n aprobada.
6. Ingresos, salidas y ajustes de stock, lotes y proveedores: cantidades v?lidas, saldo e integridad coherentes tras ?xito y rechazo.
7. Garant?as, calificaci?n y ownership; notificaciones e historial coherentes con recursos autorizados.
8. Sesi?n, revocaci?n, alta/recuperaci?n y vinculaci?n Google: autorizaci?n adicional para SMTP/OAuth externos, cuentas ficticias aisladas y enlaces reales. No reutilizar credenciales existentes.
9. Concurrencia y rollback: campa?a aislada documentada en `../tests/MecaniCar360.ConcurrencyTests/README.md`. Los 28 escenarios escritos no fueron ejecutados; B1 permanece como limitaci?n: coordinaci?n no demuestra espera SQL efectiva. Setup y ejecuci?n necesitan autorizaci?n independiente.

Tras cada circuito, comprobar resultados persistidos, historial/auditor?a e integridad DVH/DVV donde corresponda. No recalcular verificadores para ocultar diferencias. La aprobaci?n visual no sustituye estas verificaciones.
