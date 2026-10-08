# Validación del onboarding 12C.1

## Integración SQL/HTTP optativa

`OnboardingSqlIntegrationTests` utiliza servicios productivos, SQL Server y una
instancia temporal de la aplicación para las peticiones HTTP. No sustituye la
autorización, las cookies, los filtros antiforgery ni DVH/DVV.

Requiere la base local de desarrollo ya migrada, con seed e integridad válidos.
Nunca crea/reinicializa bases ni aplica migraciones explícitamente. Antes de
arrancar la aplicación comprueba que no haya migraciones pendientes.

Desde la raíz del repositorio, en PowerShell:

```powershell
dotnet build MecaniCar360.csproj --configuration Check12C -p:UseAppHost=false
$env:MC360_INTEGRATION_12C1 = '1'
$env:MC360_TEST_CONNECTION = 'Server=BOOK-JKTL5UI49E\CABRALPEDRO;Database=MecaniCar360;Integrated Security=True;TrustServerCertificate=True'
dotnet test tests/MecaniCar360.Tests/MecaniCar360.Tests.csproj --configuration Check12C -p:UseAppHost=false --logger 'console;verbosity=detailed'
Remove-Item Env:MC360_INTEGRATION_12C1
Remove-Item Env:MC360_TEST_CONNECTION
```

Sin opt-in, la prueba SQL figura como omitida, nunca como PASS. No ejecutar dos
instancias de esta prueba simultáneamente. Cada ejecución usa un prefijo aleatorio
`TEST12C1_`, correos `example.invalid` y contraseñas ficticias en memoria. El envío
se captura mediante la extensión ya existente de EmailService y un servidor SMTP
de prueba exclusivamente loopback para el proceso HTTP, sin entrega externa.
Las identidades Google se simulan en la entrada del servicio: no prueban el proveedor
OAuth ni su consentimiento. No se imprimen tokens, contraseñas, cookies o hashes.

La limpieza sólo elimina los fixtures de ese prefijo y sus relaciones/auditorías,
mediante SaveChanges de EF para mantener DVH/DVV. El proceso HTTP propio se detiene.
Ante cierre forzado del proceso de tests, comprobar el prefijo informado: no borrar
filas protegidas mediante SQL ni recalcular la línea base para ocultar un error.

## Prueba manual Google pendiente

Usar una cuenta Google de prueba controlada por el usuario. No compartir credenciales,
tokens, cookies o capturas de las solicitudes de autenticación.

1. Iniciar la aplicación normalmente y abrir Login en una ventana privada. Si Google
   no está configurado, registrar ese bloqueo; no introducir secretos en el repositorio.
2. Elegir Google y completar personalmente el consentimiento del proveedor. Usar un
   correo sin Persona/Usuario previo. Registrar sólo los IDs internos para comparar.
3. Confirmar que se exige completar el registro. Probar acceso directo al portal y a
   operaciones del taller: debe permanecer restringido al registro/cierre de sesión.
4. Salir sin completar y volver con la misma cuenta Google: deben conservarse los
   mismos UsuarioId/PersonaId y no generarse otra identidad externa.
5. Confirmar que nombre, apellido, DNI y teléfono son obligatorios, pero no se solicita
   contraseña local. El correo verificado no debe poder sustituirse.
6. Completar con un DNI único y datos ficticios válidos. Debe finalizar el registro y
   solicitar un nuevo login (se revoca la cookie anterior al cambiar SecurityStamp).
7. Volver con Google: debe abrir PortalCliente con nombre/apellido correctos.
8. Elegir Agregar contraseña y abrir el enlace recibido en el correo verificado.
   Debe retirar el fragmento y presentar nueva contraseña y confirmación, sin código
   manual ni contraseña anterior. Completar dentro de 10 minutos; un enlace alterado,
   vencido, reutilizado o reemplazado por otro debe rechazarse.
   Volver con ese correo y la nueva contraseña local:
   deben recuperarse exactamente los mismos UsuarioId y PersonaId.
9. Revisar Mis datos, modificar sólo el teléfono y comprobar que se conserva identidad,
   correo y DNI. Revisar las opciones de autenticación según Google/local realmente asociados.
10. Verificar consola sin errores, navegación y formularios. No contabilizar las pruebas
    de HttpClient como ejecución JavaScript en navegador.
11. Solicitar autorregistro con ese correo existente: respuesta pública neutra, sin
    nueva Persona, Usuario ni invitación. Verificar el código en RegistroCliente/Orientacion:
    sólo entonces se indica el acceso mediante Google. Este código no cambia credenciales.
12. En una cuenta local sin Google, comprobar Vincular Google: contraseña actual obligatoria,
    callback dentro de cinco minutos y misma sesión/stamp. Una identidad ocupada se rechaza.
    Tras vincular, sólo debe ofrecer Cambiar contraseña, que envía un enlace al correo.
13. Desde Login, elegir ¿Olvidaste tu contraseña?: comparar la respuesta pública con
    una dirección inexistente (debe ser neutra). Abrir el enlace recibido, establecer
    una contraseña y confirmar que las sesiones anteriores se rechazan. La identidad
    Google debe seguir asociada al mismo Usuario/Persona.

Los enlaces de contraseña usan `/SeguridadCliente/Restablecer#<token>` y el encabezado
`X-Password-Token`; nunca query string. Verificar consola sin errores y URL sin
fragmento tras abrirlos. Las pruebas SQL/HTTP automatizadas cubren el servidor,
antiforgery, expiración, reemisión, concurrencia, hash persistido, sesiones y DVH/DVV;
no sustituyen esta comprobación de JavaScript y entrega al buzón real.

También queda fuera de la captura SMTP automatizada la entrega real a un buzón:
solicitar una cuenta desde Crear cuenta, abrir el enlace recibido con formato
`/ActivacionCliente/Index#<token>`, comprobar que se retira el fragmento y activar.
No poner el token en query string ni copiarlo al informe.

Después de pruebas manuales, verificar integridad y limpiar únicamente esos fixtures
con un procedimiento que conserve DVH/DVV. No reinicializar la base para limpiarlos.
