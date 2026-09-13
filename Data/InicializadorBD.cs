using BCrypt.Net;
using MecaniCar360.Models;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Data
{
    public static class InicializadorBD
    {
        public static void Inicializar(
            MecaniCarContext context)
        {
            context.Database.Migrate();

            // =====================================
            // ROLES
            // =====================================

            CrearRoles(context);

            // =====================================
            // FAMILIAS
            // =====================================

            CrearFamilias(context);

            // =====================================
            // PATENTES
            // =====================================

            CrearPatentes(context);

            // =====================================
            // FAMILIA - PATENTE
            // =====================================

            AsociarPatentesAFamilias(context);

            // =====================================
            // ROL - FAMILIA
            // =====================================

            AsociarFamiliasARoles(context);

            // =====================================
            // ADMINISTRADOR
            // =====================================

            CrearAdministrador(context);

            context.SaveChanges();
        }


        // =====================================================
        // ROLES
        // =====================================================

        private static void CrearRoles(
            MecaniCarContext context)
        {
            CrearRol(
                context,
                RolesSistema.ADMIN);

            CrearRol(
                context,
                RolesSistema.MECANICO);

            CrearRol(
                context,
                RolesSistema.STOCK);

            CrearRol(
                context,
                RolesSistema.CAJA);

            CrearRol(
                context,
                RolesSistema.CLIENTE,
                true);

            context.SaveChanges();
        }


        private static void CrearRol(
            MecaniCarContext context,
            string nombre,
            bool esRolCliente = false)
        {
            var rol =
                context.Roles
                    .FirstOrDefault(
                        r => r.Nombre == nombre);

            if (rol == null)
            {
                context.Roles.Add(
                    new Rol
                    {
                        Nombre = nombre,
                        EsRolCliente =
                            esRolCliente,
                        Activo = true,
                        FechaCreacion =
                            DateTime.Now
                    });
            }
            else
            {
                rol.Activo = true;
                rol.EsRolCliente =
                    esRolCliente;
            }
        }


        // =====================================================
        // FAMILIAS
        // =====================================================

        private static void CrearFamilias(
            MecaniCarContext context)
        {
            // -------------------------------------
            // ADMINISTRACION
            // -------------------------------------

            CrearFamilia(
                context,
                "ADMINISTRACION");

            CrearFamilia(context, "AUDITORIA", "ADMINISTRACION");

            CrearFamilia(
                context,
                "USUARIOS",
                "ADMINISTRACION");

            CrearFamilia(
                context,
                "PERSONAS",
                "ADMINISTRACION");

            CrearFamilia(
                context,
                "ROLES_Y_PERMISOS",
                "ADMINISTRACION");


            // -------------------------------------
            // OPERACIONES
            // -------------------------------------

            CrearFamilia(
                context,
                "OPERACIONES");

            CrearFamilia(
                context,
                "TURNOS",
                "OPERACIONES");

            CrearFamilia(
                context,
                "INGRESOS",
                "OPERACIONES");

            CrearFamilia(
                context,
                "ORDENES",
                "OPERACIONES");

            CrearFamilia(
                context,
                "ORDENES_CONSULTA",
                "ORDENES");

            CrearFamilia(
                context,
                "ORDENES_MECANICO",
                "ORDENES");

            CrearFamilia(
                context,
                "ORDENES_CAJA",
                "ORDENES");

            CrearFamilia(
                context,
                "DIAGNOSTICOS",
                "OPERACIONES");

            CrearFamilia(
                context,
                "PRESUPUESTOS",
                "OPERACIONES");

            CrearFamilia(
                context,
                "GARANTIAS",
                "OPERACIONES");


            // -------------------------------------
            // STOCK
            // -------------------------------------

            CrearFamilia(
                context,
                "STOCK");

            CrearFamilia(
                context,
                "REPUESTOS",
                "STOCK");

            CrearFamilia(
                context,
                "REPUESTOS_CONSULTA",
                "REPUESTOS");

            CrearFamilia(
                context,
                "REPUESTOS_GESTION",
                "REPUESTOS");

            CrearFamilia(
                context,
                "MOVIMIENTOS",
                "STOCK");

            CrearFamilia(
                context,
                "ALERTAS",
                "STOCK");

            CrearFamilia(
                context,
                "PROVEEDORES",
                "STOCK");


            // -------------------------------------
            // FACTURACION
            // -------------------------------------

            CrearFamilia(
                context,
                "FACTURACION");

            CrearFamilia(
                context,
                "FACTURAS",
                "FACTURACION");

            CrearFamilia(
                context,
                "PAGOS",
                "FACTURACION");

            CrearFamilia(
                context,
                "ENTREGA",
                "FACTURACION");


            // -------------------------------------
            // VEHICULOS
            // -------------------------------------

            CrearFamilia(
                context,
                "VEHICULOS");


            // -------------------------------------
            // CLIENTE
            // -------------------------------------

            CrearFamilia(
                context,
                "CLIENTE");

            CrearFamilia(
                context,
                "MIS_TURNOS",
                "CLIENTE");

            CrearFamilia(
                context,
                "MIS_VEHICULOS",
                "CLIENTE");

            CrearFamilia(
                context,
                "MIS_ORDENES",
                "CLIENTE");

            CrearFamilia(
                context,
                "MIS_PRESUPUESTOS",
                "CLIENTE");

            CrearFamilia(
                context,
                "MIS_FACTURAS",
                "CLIENTE");

            context.SaveChanges();
        }


        private static void CrearFamilia(
            MecaniCarContext context,
            string nombre,
            string? familiaPadre = null)
        {
            var familia =
                context.Familias
                    .FirstOrDefault(
                        f => f.Nombre == nombre);

            if (familia == null)
            {
                familia = new Familia
                {
                    Nombre = nombre,
                    Activo = true
                };

                context.Familias.Add(familia);

                context.SaveChanges();
            }

            if (familiaPadre != null)
            {
                var padre =
                    context.Familias
                        .FirstOrDefault(
                            f => f.Nombre ==
                                 familiaPadre);

                if (padre != null)
                {
                    familia.FamiliaPadreId =
                        padre.Id;
                }
            }
        }


        // =====================================================
        // PATENTES
        // =====================================================

        private static void CrearPatentes(
            MecaniCarContext context)
        {
            CrearPatente(context, "AUDITORIA_VER");
            CrearPatente(context, "CLIENTE_CALIFICACION_CREAR");
            // -------------------------------------
            // USUARIOS
            // -------------------------------------

            CrearPatente(
                context,
                "USUARIO_VER");

            CrearPatente(
                context,
                "USUARIO_CREAR");

            CrearPatente(
                context,
                "USUARIO_MODIFICAR");

            CrearPatente(
                context,
                "USUARIO_DESACTIVAR");


            // -------------------------------------
            // PERSONAS
            // -------------------------------------

            CrearPatente(
                context,
                "PERSONA_VER");

            CrearPatente(
                context,
                "PERSONA_CREAR");

            CrearPatente(
                context,
                "PERSONA_MODIFICAR");

            CrearPatente(
                context,
                "PERSONA_DESACTIVAR");


            // -------------------------------------
            // ROLES
            // -------------------------------------

            CrearPatente(
                context,
                "ROL_VER");

            CrearPatente(
                context,
                "ROL_CREAR");

            CrearPatente(
                context,
                "ROL_MODIFICAR");

            CrearPatente(
                context,
                "ROL_DESACTIVAR");


            // -------------------------------------
            // TURNOS
            // -------------------------------------

            CrearPatente(
                context,
                "TURNO_VER");

            CrearPatente(
                context,
                "TURNO_CREAR");

            CrearPatente(
                context,
                "TURNO_MODIFICAR");

            CrearPatente(
                context,
                "TURNO_CONFIRMAR");

            CrearPatente(
                context,
                "TURNO_CANCELAR");


            // -------------------------------------
            // INGRESOS
            // -------------------------------------

            CrearPatente(
                context,
                "INGRESO_VER");

            CrearPatente(
                context,
                "INGRESO_REGISTRAR");


            // -------------------------------------
            // ORDENES - CONSULTA
            // -------------------------------------

            CrearPatente(
                context,
                "ORDEN_VER");

            CrearPatente(
                context,
                "ORDEN_VER_DETALLE");


            // -------------------------------------
            // ORDENES - MECANICO
            // -------------------------------------

            CrearPatente(
                context,
                "ORDEN_CAMBIAR_ESTADO");

            CrearPatente(
                context,
                "ORDEN_FINALIZAR");

            CrearPatente(
                context,
                "ORDEN_MODIFICAR");


            // -------------------------------------
            // ORDENES - CAJA
            // -------------------------------------

            CrearPatente(
                context,
                "ORDEN_ASIGNAR_MECANICO");

            CrearPatente(
                context,
                "ORDEN_ENTREGAR");


            // -------------------------------------
            // DIAGNOSTICOS
            // -------------------------------------

            CrearPatente(
                context,
                "DIAGNOSTICO_VER");

            CrearPatente(
                context,
                "DIAGNOSTICO_CREAR");

            CrearPatente(
                context,
                "DIAGNOSTICO_MODIFICAR");

            CrearPatente(
                context,
                "DIAGNOSTICO_FINALIZAR");

            CrearPatente(
                context,
                "DIAGNOSTICO_HISTORIAL");


            // -------------------------------------
            // PRESUPUESTOS
            // -------------------------------------

            CrearPatente(
                context,
                "PRESUPUESTO_VER");

            CrearPatente(
                context,
                "PRESUPUESTO_CREAR");

            CrearPatente(
                context,
                "PRESUPUESTO_MODIFICAR");

            CrearPatente(
                context,
                "PRESUPUESTO_ENVIAR");

            CrearPatente(
                context,
                "PRESUPUESTO_APROBAR");

            CrearPatente(
                context,
                "PRESUPUESTO_RECHAZAR");

            // -------------------------------------
            // GARANTIAS
            // -------------------------------------

            CrearPatente(
                context,
                "GARANTIA_VER");

            CrearPatente(
                context,
                "GARANTIA_VER_PROPIA");

            CrearPatente(
                context,
                "GARANTIA_CREAR");

            CrearPatente(
                context,
                "GARANTIA_ANULAR");

            // -------------------------------------
            // STOCK
            // -------------------------------------

            CrearPatente(
                context,
                "STOCK_VER");

            CrearPatente(
                context,
                "STOCK_CREAR");

            CrearPatente(
                context,
                "STOCK_MODIFICAR");

            CrearPatente(
                context,
                "STOCK_MOVIMIENTO");

            CrearPatente(
                context,
                "STOCK_ALERTAS");


            // -------------------------------------
            // PROVEEDORES
            // -------------------------------------

            CrearPatente(
                context,
                "PROVEEDOR_VER");

            CrearPatente(
                context,
                "PROVEEDOR_CREAR");

            CrearPatente(
                context,
                "PROVEEDOR_MODIFICAR");

            CrearPatente(
                context,
                "PROVEEDOR_DESACTIVAR");


            // -------------------------------------
            // VEHICULOS
            // -------------------------------------

            CrearPatente(
                context,
                "VEHICULO_VER");

            CrearPatente(
                context,
                "VEHICULO_CREAR");

            CrearPatente(
                context,
                "VEHICULO_MODIFICAR");


            // -------------------------------------
            // FACTURAS
            // -------------------------------------

            CrearPatente(
                context,
                "FACTURA_VER");

            CrearPatente(
                context,
                "FACTURA_CREAR");


            // -------------------------------------
            // PAGOS
            // -------------------------------------

            CrearPatente(
                context,
                "PAGO_VER");

            CrearPatente(
                context,
                "PAGO_REGISTRAR");


            // -------------------------------------
            // ENTREGA
            // -------------------------------------

            CrearPatente(
                context,
                "ENTREGA_REGISTRAR");


            // -------------------------------------
            // CLIENTE
            // -------------------------------------

            CrearPatente(
                context,
                "CLIENTE_TURNO_VER");

            CrearPatente(
                context,
                "CLIENTE_TURNO_CREAR");

            CrearPatente(
                context,
                "CLIENTE_TURNO_CANCELAR");

            CrearPatente(
                context,
                "CLIENTE_VEHICULO_VER");

            CrearPatente(
                context,
                "CLIENTE_ORDEN_VER");

            CrearPatente(
                context,
                "CLIENTE_PRESUPUESTO_VER");

            CrearPatente(
                context,
                "CLIENTE_PRESUPUESTO_APROBAR");

            CrearPatente(
                context,
                "CLIENTE_PRESUPUESTO_RECHAZAR");

            CrearPatente(
                context,
                "CLIENTE_FACTURA_VER");


            context.SaveChanges();
        }


        private static void CrearPatente(
            MecaniCarContext context,
            string nombre)
        {
            var patente =
                context.Patentes
                    .FirstOrDefault(
                        p => p.Nombre == nombre);

            if (patente == null)
            {
                context.Patentes.Add(
                    new Patente
                    {
                        Nombre = nombre,
                        Activo = true
                    });
            }
        }


        // =====================================================
        // FAMILIA - PATENTE
        // =====================================================

        private static void AsociarPatentesAFamilias(
            MecaniCarContext context)
        {
            AsociarPatente(context, "AUDITORIA", "AUDITORIA_VER");
            // Calificar no es una consulta de orden: requiere una patente de escritura propia.
            AsociarPatente(context, "MIS_ORDENES", "CLIENTE_CALIFICACION_CREAR");
            AsociarPatente(context, "MIS_ORDENES", "GARANTIA_VER_PROPIA");

            // -------------------------------------
            // USUARIOS
            // -------------------------------------

            AsociarPatente(
                context,
                "USUARIOS",
                "USUARIO_VER");

            AsociarPatente(
                context,
                "USUARIOS",
                "USUARIO_CREAR");

            AsociarPatente(
                context,
                "USUARIOS",
                "USUARIO_MODIFICAR");

            AsociarPatente(
                context,
                "USUARIOS",
                "USUARIO_DESACTIVAR");


            // -------------------------------------
            // PERSONAS
            // -------------------------------------

            AsociarPatente(
                context,
                "PERSONAS",
                "PERSONA_VER");

            AsociarPatente(
                context,
                "PERSONAS",
                "PERSONA_CREAR");

            AsociarPatente(
                context,
                "PERSONAS",
                "PERSONA_MODIFICAR");

            AsociarPatente(
                context,
                "PERSONAS",
                "PERSONA_DESACTIVAR");


            // -------------------------------------
            // ROLES
            // -------------------------------------

            AsociarPatente(
                context,
                "ROLES_Y_PERMISOS",
                "ROL_VER");

            AsociarPatente(
                context,
                "ROLES_Y_PERMISOS",
                "ROL_CREAR");

            AsociarPatente(
                context,
                "ROLES_Y_PERMISOS",
                "ROL_MODIFICAR");

            AsociarPatente(
                context,
                "ROLES_Y_PERMISOS",
                "ROL_DESACTIVAR");


            // -------------------------------------
            // TURNOS
            // -------------------------------------

            AsociarPatente(
                context,
                "TURNOS",
                "TURNO_VER");

            AsociarPatente(
                context,
                "TURNOS",
                "TURNO_CREAR");

            AsociarPatente(
                context,
                "TURNOS",
                "TURNO_MODIFICAR");

            AsociarPatente(
                context,
                "TURNOS",
                "TURNO_CONFIRMAR");

            AsociarPatente(
                context,
                "TURNOS",
                "TURNO_CANCELAR");


            // -------------------------------------
            // INGRESOS
            // -------------------------------------

            AsociarPatente(
                context,
                "INGRESOS",
                "INGRESO_VER");

            AsociarPatente(
                context,
                "INGRESOS",
                "INGRESO_REGISTRAR");


            // -------------------------------------
            // ORDENES - CONSULTA
            // -------------------------------------

            AsociarPatente(
                context,
                "ORDENES_CONSULTA",
                "ORDEN_VER");

            AsociarPatente(
                context,
                "ORDENES_CONSULTA",
                "ORDEN_VER_DETALLE");


            // -------------------------------------
            // ORDENES - MECANICO
            // -------------------------------------

            AsociarPatente(
                context,
                "ORDENES_MECANICO",
                "ORDEN_CAMBIAR_ESTADO");

            AsociarPatente(
                context,
                "ORDENES_MECANICO",
                "ORDEN_FINALIZAR");

            AsociarPatente(
                context,
                "ORDENES_MECANICO",
                "ORDEN_MODIFICAR");


            // -------------------------------------
            // ORDENES - CAJA
            // -------------------------------------

            AsociarPatente(
                context,
                "ORDENES_CAJA",
                "ORDEN_ENTREGAR");


            // -------------------------------------
            // DIAGNOSTICOS
            // -------------------------------------

            AsociarPatente(
                context,
                "DIAGNOSTICOS",
                "DIAGNOSTICO_VER");

            AsociarPatente(
                context,
                "DIAGNOSTICOS",
                "DIAGNOSTICO_CREAR");

            AsociarPatente(
                context,
                "DIAGNOSTICOS",
                "DIAGNOSTICO_MODIFICAR");

            AsociarPatente(
                context,
                "DIAGNOSTICOS",
                "DIAGNOSTICO_FINALIZAR");

            AsociarPatente(
                context,
                "DIAGNOSTICOS",
                "DIAGNOSTICO_HISTORIAL");


            // -------------------------------------
            // PRESUPUESTOS
            // -------------------------------------

            AsociarPatente(
                context,
                "PRESUPUESTOS",
                "PRESUPUESTO_VER");

            AsociarPatente(
                context,
                "PRESUPUESTOS",
                "PRESUPUESTO_CREAR");

            AsociarPatente(
                context,
                "PRESUPUESTOS",
                "PRESUPUESTO_MODIFICAR");

            AsociarPatente(
                context,
                "PRESUPUESTOS",
                "PRESUPUESTO_ENVIAR");

            AsociarPatente(
                context,
                "PRESUPUESTOS",
                "PRESUPUESTO_APROBAR");

            AsociarPatente(
                context,
                "PRESUPUESTOS",
                "PRESUPUESTO_RECHAZAR");

            // -------------------------------------
            // GARANTIAS
            // -------------------------------------

            AsociarPatente(
                context,
                "GARANTIAS",
                "GARANTIA_VER");

            AsociarPatente(
                context,
                "GARANTIAS",
                "GARANTIA_CREAR");

            // Retirar únicamente anulación de GARANTIAS, también en bases ya sembradas.
            // Conservar las asociaciones de consulta propia; ADMIN mantiene el bypass.
            var asociacionesGarantiaRetiradas = context.FamiliaPatentes
                .Where(fp => fp.Familia.Nombre == "GARANTIAS" &&
                    fp.Patente.Nombre == "GARANTIA_ANULAR")
                .ToList();
            context.FamiliaPatentes.RemoveRange(asociacionesGarantiaRetiradas);

            // -------------------------------------
            // REPUESTOS - CONSULTA
            // -------------------------------------

            AsociarPatente(
                context,
                "REPUESTOS_CONSULTA",
                "STOCK_VER");


            // -------------------------------------
            // REPUESTOS - GESTION
            // -------------------------------------

            AsociarPatente(
                context,
                "REPUESTOS_GESTION",
                "STOCK_VER");

            AsociarPatente(
                context,
                "REPUESTOS_GESTION",
                "STOCK_CREAR");

            AsociarPatente(
                context,
                "REPUESTOS_GESTION",
                "STOCK_MODIFICAR");


            // -------------------------------------
            // MOVIMIENTOS
            // -------------------------------------

            AsociarPatente(
                context,
                "MOVIMIENTOS",
                "STOCK_MOVIMIENTO");


            // -------------------------------------
            // ALERTAS
            // -------------------------------------

            AsociarPatente(
                context,
                "ALERTAS",
                "STOCK_ALERTAS");


            // -------------------------------------
            // PROVEEDORES
            // -------------------------------------

            AsociarPatente(
                context,
                "PROVEEDORES",
                "PROVEEDOR_VER");

            AsociarPatente(
                context,
                "PROVEEDORES",
                "PROVEEDOR_CREAR");

            AsociarPatente(
                context,
                "PROVEEDORES",
                "PROVEEDOR_MODIFICAR");

            AsociarPatente(
                context,
                "PROVEEDORES",
                "PROVEEDOR_DESACTIVAR");


            // -------------------------------------
            // VEHICULOS
            // -------------------------------------

            AsociarPatente(
                context,
                "VEHICULOS",
                "VEHICULO_VER");

            AsociarPatente(
                context,
                "VEHICULOS",
                "VEHICULO_CREAR");

            AsociarPatente(
                context,
                "VEHICULOS",
                "VEHICULO_MODIFICAR");


            // -------------------------------------
            // FACTURAS
            // -------------------------------------

            AsociarPatente(
                context,
                "FACTURAS",
                "FACTURA_VER");

            AsociarPatente(
                context,
                "FACTURAS",
                "FACTURA_CREAR");


            // -------------------------------------
            // PAGOS
            // -------------------------------------

            AsociarPatente(
                context,
                "PAGOS",
                "PAGO_VER");

            AsociarPatente(
                context,
                "PAGOS",
                "PAGO_REGISTRAR");


            // -------------------------------------
            // ENTREGA
            // -------------------------------------

            AsociarPatente(
                context,
                "ENTREGA",
                "ENTREGA_REGISTRAR");


            // -------------------------------------
            // CLIENTE - TURNOS
            // -------------------------------------

            AsociarPatente(
                context,
                "MIS_TURNOS",
                "CLIENTE_TURNO_VER");

            AsociarPatente(
                context,
                "MIS_TURNOS",
                "CLIENTE_TURNO_CREAR");

            AsociarPatente(
                context,
                "MIS_TURNOS",
                "CLIENTE_TURNO_CANCELAR");


            // -------------------------------------
            // CLIENTE - VEHICULOS
            // -------------------------------------

            AsociarPatente(
                context,
                "MIS_VEHICULOS",
                "CLIENTE_VEHICULO_VER");


            // -------------------------------------
            // CLIENTE - ORDENES
            // -------------------------------------

            AsociarPatente(
                context,
                "MIS_ORDENES",
                "CLIENTE_ORDEN_VER");


            // -------------------------------------
            // CLIENTE - PRESUPUESTOS
            // -------------------------------------

            AsociarPatente(
                context,
                "MIS_PRESUPUESTOS",
                "CLIENTE_PRESUPUESTO_VER");

            AsociarPatente(
                context,
                "MIS_PRESUPUESTOS",
                "CLIENTE_PRESUPUESTO_APROBAR");

            AsociarPatente(
                context,
                "MIS_PRESUPUESTOS",
                "CLIENTE_PRESUPUESTO_RECHAZAR");


            // -------------------------------------
            // CLIENTE - FACTURAS
            // -------------------------------------

            AsociarPatente(
                context,
                "MIS_FACTURAS",
                "CLIENTE_FACTURA_VER");


            context.SaveChanges();
        }


        private static void AsociarPatente(
            MecaniCarContext context,
            string familiaNombre,
            string patenteNombre)
        {
            var familia =
                context.Familias
                    .FirstOrDefault(
                        f => f.Nombre ==
                             familiaNombre);

            var patente =
                context.Patentes
                    .FirstOrDefault(
                        p => p.Nombre ==
                             patenteNombre);

            if (familia == null ||
                patente == null)
            {
                return;
            }

            var existe =
                context.FamiliaPatentes.Any(
                    fp =>
                        fp.FamiliaId ==
                        familia.Id &&
                        fp.PatenteId ==
                        patente.Id);

            if (!existe)
            {
                context.FamiliaPatentes.Add(
                    new FamiliaPatente
                    {
                        FamiliaId =
                            familia.Id,

                        PatenteId =
                            patente.Id
                    });
            }
        }


        // =====================================================
        // ROL - FAMILIA
        // =====================================================

        private static void AsociarFamiliasARoles(
            MecaniCarContext context)
        {
            // -------------------------------------
            // ADMIN
            // -------------------------------------

            var familiasAdmin = new[]
            {
                "ADMINISTRACION",
                "OPERACIONES",
                "STOCK",
                "FACTURACION",
                "VEHICULOS",
                "CLIENTE"
            };

            foreach (var familia
                in familiasAdmin)
            {
                AsociarFamilia(
                    context,
                    RolesSistema.ADMIN,
                    familia);
            }


            // -------------------------------------
            // MECANICO
            // -------------------------------------

            AsociarFamilia(
                context,
                RolesSistema.MECANICO,
                "ORDENES_CONSULTA");

            AsociarFamilia(
                context,
                RolesSistema.MECANICO,
                "ORDENES_MECANICO");

            AsociarFamilia(
                context,
                RolesSistema.MECANICO,
                "DIAGNOSTICOS");

            AsociarFamilia(
                context,
                RolesSistema.MECANICO,
                "PRESUPUESTOS");

            // Solo consulta stock.
            AsociarFamilia(
                context,
                RolesSistema.MECANICO,
                "REPUESTOS_CONSULTA");


            // -------------------------------------
            // STOCK
            // -------------------------------------

            AsociarFamilia(
                context,
                RolesSistema.STOCK,
                "STOCK");


            // -------------------------------------
            // CAJA
            // -------------------------------------

            AsociarFamilia(
                context,
                RolesSistema.CAJA,
                "USUARIOS");

            AsociarFamilia(
                context,
                RolesSistema.CAJA,
                "PERSONAS");

            AsociarFamilia(
                context,
                RolesSistema.CAJA,
                "VEHICULOS");

            AsociarFamilia(
                context,
                RolesSistema.CAJA,
                "TURNOS");

            AsociarFamilia(
                context,
                RolesSistema.CAJA,
                "INGRESOS");

            AsociarFamilia(
                context,
                RolesSistema.CAJA,
                "ORDENES_CONSULTA");

            AsociarFamilia(
                context,
                RolesSistema.CAJA,
                "ORDENES_CAJA");

            AsociarFamilia(
                context,
                RolesSistema.CAJA,
                "FACTURACION");

            AsociarFamilia(
                context,
                RolesSistema.CAJA,
                "GARANTIAS");


            // -------------------------------------
            // CLIENTE
            // -------------------------------------

            AsociarFamilia(
                context,
                RolesSistema.CLIENTE,
                "CLIENTE");


            context.SaveChanges();
        }


        private static void AsociarFamilia(
            MecaniCarContext context,
            string rolNombre,
            string familiaNombre)
        {
            var rol =
                context.Roles
                    .FirstOrDefault(
                        r => r.Nombre ==
                             rolNombre);

            var familia =
                context.Familias
                    .FirstOrDefault(
                        f => f.Nombre ==
                             familiaNombre);

            if (rol == null ||
                familia == null)
            {
                return;
            }

            var existe =
                context.RolFamilias.Any(
                    rf =>
                        rf.RolId == rol.Id &&
                        rf.FamiliaId ==
                        familia.Id);

            if (!existe)
            {
                context.RolFamilias.Add(
                    new RolFamilia
                    {
                        RolId =
                            rol.Id,

                        FamiliaId =
                            familia.Id
                    });
            }
        }


        // =====================================================
        // ADMINISTRADOR
        // =====================================================

        private static void CrearAdministrador(
            MecaniCarContext context)
        {
            var existe =
                context.Usuarios.Any(
                    u =>
                        u.Username ==
                        "admin");

            if (existe)
            {
                return;
            }

            var rolAdmin =
                context.Roles
                    .First(
                        r =>
                            r.Nombre ==
                            RolesSistema.ADMIN);

            var persona =
                new Persona
                {
                    Nombre =
                        "Administrador",

                    Apellido =
                        "Principal",

                    Dni =
                        "00000000",

                    Telefono =
                        "1111111111",

                    Email =
                        "360.mecanicar@gmail.com",

                    Activo =
                        true
                };


            persona.Roles.Add(
                new PersonaRol
                {
                    Rol =
                        rolAdmin,

                    FechaAlta =
                        DateTime.Now
                });


            var usuario =
                new Usuario
                {
                    Username =
                        "admin",

                    EmailLogin =
                        "360.mecanicar@gmail.com",

                    PasswordHash =
                        BCrypt.Net.BCrypt
                            .HashPassword(
                                "admin123"),

                    PrimerLogin =
                        true,

                    Activo =
                        true,

                    Persona =
                        persona,

                    FechaCreacion =
                        DateTime.Now
                };


            context.Usuarios.Add(
                usuario);

            context.SaveChanges();
        }
    }
}
