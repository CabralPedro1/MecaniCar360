# MecaniCar360

Proyecto académico de Ingeniería en Sistemas para la gestión de un taller mecánico. MecaniCar360 integra la atención al cliente, el trabajo técnico, los repuestos y el circuito administrativo en una aplicación web local.

Su objetivo es centralizar información que suele quedar dispersa entre reservas, registros de vehículos, presupuestos y cobros, conservando trazabilidad y acceso según las responsabilidades de cada usuario. La versión 1.0 está orientada a una entrega académica funcional y demostrable; no se presenta como un producto comercial ni como una instalación preparada para producción.

## Funcionalidades

- **Clientes y acceso:** alta presencial, autorregistro por correo, invitaciones, autenticación local y Google, vinculación de identidad y enlaces seguros de contraseña.
- **Vehículos y turnos:** marcas/modelos, vehículos y titularidad, agenda, reservas, confirmación, cancelación, reprogramación y ausencias.
- **Taller:** recepción, generación de órdenes de trabajo (OT), toma/asignación, diagnóstico e historial, evidencias, presupuestos versionados, aprobación/rechazo, reparación y entrega.
- **Stock:** repuestos, proveedores, precios de compra, lotes, ingresos, salidas, ajustes y movimientos. El alta de repuesto no genera existencias iniciales.
- **Administración y postventa:** facturas, pagos parciales/totales, garantías, calificaciones, notificaciones, auditoría, indicadores y reportes PDF. Los pagos son registros académicos, no una pasarela bancaria.
- **Portal CLIENTE:** datos, vehículos, turnos, órdenes, presupuestos, facturas y garantías propios.
- **Operación técnica:** scripts de backup/restauración y herramienta de verificación/inicialización de integridad DVH/DVV.

Circuito principal: **cliente → vehículo → turno → recepción → OT → diagnóstico → presupuesto → decisión del cliente → reparación → factura → pago → entrega**.

## Perfiles

| Perfil | Responsabilidad |
|---|---|
| ADMIN | Administra personal, cuentas, roles y permisos; supervisa y puede asignar directamente una OT a un mecánico autorizado |
| CAJA | Registra clientes, vehículos, turnos y recepciones que generan OT; gestiona facturación, cobros y entrega. No asigna mecánicos |
| MECANICO | Toma voluntariamente órdenes disponibles sin asignar; trabaja sobre las OT autorizadas, con diagnóstico, evidencias, presupuesto y reparación |
| STOCK | Administra repuestos, proveedores, existencias y movimientos |
| CLIENTE | Consulta y opera sobre sus recursos propios, decide presupuestos y gestiona su acceso |

La autorización se valida en servidor mediante patentes efectivas, estado de cuenta, titularidad y asignación. La regla de asignación anterior es la acordada; la salvedad actual del control por patente está documentada en [seguridad y autorización](docs/seguridad-autorizacion.md).

## Tecnologías y arquitectura

- C# y ASP.NET Core MVC sobre **.NET 8**, vistas Razor.
- **Entity Framework Core 8.0.2** y SQL Server.
- Bootstrap y jQuery locales, JavaScript y CSS; validación jQuery Unobtrusive.
- Cookies de autenticación, BCrypt.Net-Next 4.0.3, integración Google de ASP.NET Core 8.0.30 y correo SMTP.
- PDFsharp 6.2.4 para reportes; xUnit 2.9.2 para pruebas.

Las versiones de referencia están en [MecaniCar360.csproj](MecaniCar360.csproj) y en los proyectos de pruebas. Los controladores coordinan HTTP, los servicios contienen las operaciones de negocio y EF Core realiza la persistencia. El proyecto incluye patrones académicos Composite, Facade, Memento, Observer, State y Strategy, junto con controles transversales de seguridad e integridad.

```text
Controllers/  Services/  Models/     # HTTP, negocio y modelos/DTO/ViewModels
Data/         Migrations/           # DbContext, integridad e historial EF
Attributes/   Helpers/ Middleware/  # Validaciones y controles transversales
Patterns/                          # Patrones de diseño
Views/        wwwroot/              # Razor y recursos locales
tests/                             # Suite principal y harness SQL aislado
scripts/      tools/Integridad/     # Backup, restore e integridad
docs/                              # Operación, aceptación y trazabilidad
```

## Requisitos y ejecución local

Se necesita el SDK .NET 8, SQL Server accesible y una base preparada con el esquema y la integridad correspondientes. Los procedimientos PowerShell y el harness documentados están orientados al entorno Windows local. Se requiere acceso al origen de paquetes para restaurar dependencias cuando no estén disponibles localmente.

1. Obtener el repositorio y abrir una terminal en su raíz.
2. Revisar [OPERACION-LOCAL-1.0.md](docs/OPERACION-LOCAL-1.0.md) y confirmar el destino de `ConnectionStrings:DefaultConnection`. Configurar valores privados mediante User Secrets en Development o variables de entorno, nunca en Git.
3. Para correo, configurar las claves `Smtp` y `Invitaciones:UrlBase`; para Google, las claves `Authentication:Google` y la configuración del proveedor. Google es opcional para el acceso local. No se incluyen credenciales de prueba en este repositorio.
4. Compilar y, **sólo sobre una base existente preparada y autorizada**, iniciar en modo de aceptación:

```powershell
dotnet build MecaniCar360.csproj --configuration Aceptacion -p:UseAppHost=false -v:q
dotnet bin/Aceptacion/net8.0/MecaniCar360.dll --environment=Development --urls=http://localhost:5190 --Arranque:ModoAceptacion=true
```

Abrir `http://localhost:5190/Account/Login`. Usar el puerto sólo si está disponible; los enlaces de correo deben conservar un origen coherente con `Invitaciones:UrlBase`. Ctrl+C detiene ese host.

El modo de aceptación omite las migraciones, seeds y bootstrap automáticos del arranque, pero **conserva la verificación de integridad y abre conexión a SQL**. No es un modo global de sólo lectura: las operaciones de usuario pueden escribir. Ante un bloqueo 503 de integridad, detener la aceptación y seguir el diagnóstico documentado; no recalcular verificadores para ocultar diferencias.

### Preparación de base de datos

El repositorio conserva **20 migraciones**, sus metadatos y el ModelSnapshot. No deben eliminarse ni consolidarse para instalar el sistema.

Una instalación nueva requiere un procedimiento separado y autorizado: confirmar destino, aplicar las migraciones existentes mediante EF fuera del arranque web, preparar backup verificado e inicializar la línea base sobre las tablas preparadas antes del primer seed; luego ejecutar bootstrap normal y verificar integridad. Consultar la secuencia y condiciones exactas en [operación local](docs/OPERACION-LOCAL-1.0.md), [integridad DVH/DVV](docs/integridad-dvh-dvv.md) y [backup/restore](docs/backup-restore.md). No usar los comandos de aceptación como instalador de una base vacía.

El arranque normal puede ejecutar migraciones e inicialización. La inicialización técnica `--sin-escrituras` confirma una ventana exclusiva: **no es una simulación**. Configurar `BootstrapAdmin:Password` externamente cuando corresponda crear el ADMIN inicial.

La base habitual es de prueba, pero sus registros deben preservarse, incluido `TEST_CIERRE10_A123`. No reinicializarla, limpiar fixtures ni cambiar cuentas para preparar una demostración sin autorización.

## Pruebas

Desde la raíz, ejecutar la suite principal excluyendo expresamente la integración SQL:

```powershell
dotnet test tests/MecaniCar360.Tests/MecaniCar360.Tests.csproj --configuration Aceptacion -p:UseAppHost=false --filter 'FullyQualifiedName!~OnboardingSqlIntegrationTests' -v:q
git diff --check
```

La suite combina validaciones unitarias, contratos, marcado, HTTP en memoria y metadatos EF, incluida la comparación modelo/snapshot sin abrir SQL. No habilitar variables opt-in de integración ni ejecutar indiscriminadamente todos los proyectos.

- [Suite y política de pruebas](docs/testing.md).
- [Harness de concurrencia SQL](tests/MecaniCar360.ConcurrencyTests/README.md): ejecutor y preparador independientes, fuera de la solución principal para evitar ejecuciones accidentales. Requiere base aislada y autorización específica.

## Estado de la versión 1.0

El flujo principal y los módulos tienen implementación identificada. El catálogo documenta **65 casos de uso: 61 de aplicación y 4 técnicos**, no 65 casos validados integralmente. Continúa pendiente la aceptación funcional completa por perfiles y la comprobación visual en navegador.

Los **28 escenarios de concurrencia SQL están escritos, pero no ejecutados**. B1 permanece como limitación: las barreras coordinan llamadas, pero no acreditan observación de espera efectiva en SQL Server. Las advertencias de nulabilidad aceptadas y sus límites se explican en [ADVERTENCIAS-1.0.md](docs/ADVERTENCIAS-1.0.md).

La asignación directa se controla actualmente por patente; no hay una comprobación adicional de ADMIN efectivo en el servicio. El seed no concede esa patente a CAJA, pero una concesión posterior a otro perfil permitiría asignar. Esta salvedad no se considera corregida por la descripción de responsabilidades.

## Documentación principal

| Documento | Contenido |
|---|---|
| [Casos de uso](docs/CASOS-DE-USO.md) | Catálogo, actores, implementación y trazabilidad |
| [Aceptación 1.0](docs/ACEPTACION-1.0.md) | Recorridos por perfil, resultados y pruebas manuales pendientes |
| [Operación local](docs/OPERACION-LOCAL-1.0.md) | Configuración, arranque y diagnóstico |
| [Plan de cierre](docs/PLAN-CIERRE.md) | Tareas y criterios de aceptación de la entrega |
| [Seguridad y autorización](docs/seguridad-autorizacion.md) | Patentes, restricciones y evidencia disponible |

Los documentos distinguen inspección estática, pruebas automatizadas y ejecución real. Una compilación correcta no certifica por sí sola el circuito completo, los servicios externos ni el diseño responsive.
