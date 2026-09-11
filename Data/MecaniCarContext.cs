using MecaniCar360.Models;
using Microsoft.EntityFrameworkCore;

namespace MecaniCar360.Data
{
    public class MecaniCarContext : DbContext
    {
        public MecaniCarContext(DbContextOptions<MecaniCarContext> options)
            : base(options)
        {
        }

        // =============================
        // PERSONAS Y SEGURIDAD
        // =============================

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Persona> Personas { get; set; }
        public DbSet<Rol> Roles { get; set; }
        public DbSet<PersonaRol> PersonaRoles { get; set; }

        public DbSet<Familia> Familias { get; set; }

        public DbSet<Patente> Patentes { get; set; }

        public DbSet<FamiliaPatente> FamiliaPatentes { get; set; }

        public DbSet<RolFamilia> RolFamilias { get; set; }

        // =============================
        // VEHÍCULOS
        // =============================

        public DbSet<Vehiculo> Vehiculos { get; set; }
        public DbSet<DominioVehicular> DominiosVehiculares { get; set; }

        // =============================
        // MARCAS Y MODELOS
        // =============================

        public DbSet<Marca> Marcas { get; set; }
        public DbSet<Modelo> Modelos { get; set; }

        // =============================
        // TURNOS Y AGENDA
        // =============================

        public DbSet<Turno> Turnos { get; set; }
        public DbSet<TurnoEstadoHistorial> TurnoEstados { get; set; }


        public DbSet<IngresoVehiculo> IngresosVehiculo { get; set; }

        // =============================
        // ORDEN DE TRABAJO
        // =============================

        public DbSet<OrdenTrabajo> OrdenesTrabajo { get; set; }
        public DbSet<OrdenTrabajoEstadoHistorial> OrdenTrabajoEstados { get; set; }

        // =============================
        // DIAGNÓSTICO
        // =============================

        public DbSet<Diagnostico> Diagnosticos { get; set; }
        public DbSet<DiagnosticoHistorial> DiagnosticoHistoriales { get; set; }

        // =============================
        // PRESUPUESTOS
        // =============================

        public DbSet<Presupuesto> Presupuestos { get; set; }
        public DbSet<PresupuestoVersion> PresupuestoVersiones { get; set; }
        public DbSet<PresupuestoVersionItem> PresupuestoVersionItems { get; set; }
        public DbSet<PresupuestoVersionEvidencia> PresupuestoVersionEvidencias { get; set; }
        public DbSet<DiagnosticoHistorialEvidencia> DiagnosticoHistorialEvidencias { get; set; }
        public DbSet<PresupuestoItem> PresupuestoItems { get; set; }
        public DbSet<PresupuestoHistorial> PresupuestoHistoriales { get; set; }

        // =============================
        // FACTURACIÓN Y PAGOS
        // =============================

        public DbSet<Factura> Facturas { get; set; }
        public DbSet<FacturaItem> FacturaItems { get; set; }
        public DbSet<Pago> Pagos { get; set; }

        // =============================
        // STOCK
        // =============================

        public DbSet<Repuesto> Repuestos { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<ProveedorRepuesto> ProveedorRepuestos { get; set; }
        public DbSet<MovimientoStock> MovimientosStock { get; set; }


        // =============================
        // CALIFICACIONES Y EVIDENCIAS
        // =============================

        public DbSet<CalificacionTrabajo> Calificaciones { get; set; }
        public DbSet<EvidenciaTrabajo> Evidencias { get; set; }

        // =============================
        // AUDITORÍA Y NOTIFICACIONES
        // =============================

        public DbSet<Auditoria> Auditorias { get; set; }
        public DbSet<Notificacion> Notificaciones { get; set; }

        // =============================
        // GARANTÍAS
        // =============================

        public DbSet<Garantia> Garantias { get; set; }
        public DbSet<GarantiaItem> GarantiaItems { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =============================
            // PERSONA - ROL
            // =============================

            modelBuilder.Entity<PersonaRol>()
                .HasKey(pr => new
                {
                    pr.PersonaId,
                    pr.RolId
                });

            modelBuilder.Entity<PersonaRol>()
                .HasOne(pr => pr.Persona)
                .WithMany(p => p.Roles)
                .HasForeignKey(pr => pr.PersonaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PersonaRol>()
                .HasOne(pr => pr.Rol)
                .WithMany(r => r.Personas)
                .HasForeignKey(pr => pr.RolId)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================
            // FAMILIA
            // =====================================

            modelBuilder.Entity<Familia>()
                .HasOne(f => f.FamiliaPadre)
                .WithMany(f => f.FamiliasHijas)
                .HasForeignKey(f => f.FamiliaPadreId)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================
            // FAMILIA - PATENTE
            // =====================================

            modelBuilder.Entity<FamiliaPatente>()
                .HasKey(fp =>
                    new
                    {
                        fp.FamiliaId,
                        fp.PatenteId
                    });

            modelBuilder.Entity<FamiliaPatente>()
                .HasOne(fp => fp.Familia)
                .WithMany(f => f.Patentes)
                .HasForeignKey(fp => fp.FamiliaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FamiliaPatente>()
                .HasOne(fp => fp.Patente)
                .WithMany(p => p.Familias)
                .HasForeignKey(fp => fp.PatenteId)
                .OnDelete(DeleteBehavior.Cascade);


            // =====================================
            // ROL - FAMILIA
            // =====================================

            modelBuilder.Entity<RolFamilia>()
                .HasKey(rf =>
                    new
                    {
                        rf.RolId,
                        rf.FamiliaId
                    });

            modelBuilder.Entity<RolFamilia>()
                .HasOne(rf => rf.Rol)
                .WithMany(r => r.Familias)
                .HasForeignKey(rf => rf.RolId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RolFamilia>()
                .HasOne(rf => rf.Familia)
                .WithMany(f => f.Roles)
                .HasForeignKey(rf => rf.FamiliaId)
                .OnDelete(DeleteBehavior.Cascade);

            // =============================
            // USUARIO
            // =============================

            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.EmailLogin)
                .IsUnique();

            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.PersonaId)
                .IsUnique();

            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.Persona)
                .WithOne(p => p.Usuario)
                .HasForeignKey<Usuario>(u => u.PersonaId)
                .OnDelete(DeleteBehavior.Restrict);


            // =============================
            // VEHÍCULO
            // =============================

            modelBuilder.Entity<Vehiculo>()
                .HasIndex(v => v.Vin)
                .IsUnique();

            modelBuilder.Entity<Vehiculo>()
                .HasIndex(v => v.Patente)
                .IsUnique();

            modelBuilder.Entity<Vehiculo>()
                .Property(v => v.FechaCreacion)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<Vehiculo>()
                .HasOne(v => v.Marca)
                .WithMany()
                .HasForeignKey(v => v.MarcaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Vehiculo>()
                .HasOne(v => v.Modelo)
                .WithMany(m => m.Vehiculos)
                .HasForeignKey(v => v.ModeloId)
                .OnDelete(DeleteBehavior.Restrict);


            // =============================
            // MARCA
            // =============================

            modelBuilder.Entity<Marca>()
                .HasIndex(m => m.Nombre)
                .IsUnique();

            modelBuilder.Entity<Marca>()
                .Property(m => m.FechaCreacion)
                .HasDefaultValueSql("GETDATE()");


            // =============================
            // MODELO
            // =============================

            modelBuilder.Entity<Modelo>()
                .HasOne(m => m.Marca)
                .WithMany(m => m.Modelos)
                .HasForeignKey(m => m.MarcaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Modelo>()
                .HasIndex(m => new
                {
                    m.MarcaId,
                    m.Nombre
                })
                .IsUnique();

            modelBuilder.Entity<Modelo>()
                .Property(m => m.FechaCreacion)
                .HasDefaultValueSql("GETDATE()");


            // =============================
            // TURNO
            // =============================

            modelBuilder.Entity<Turno>()
                .HasOne(t => t.Vehiculo)
                .WithMany(v => v.Turnos)
                .HasForeignKey(t => t.VehiculoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Turno>()
                .HasOne(t => t.Cliente)
                .WithMany()
                .HasForeignKey(t => t.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Turno>()
                .HasOne(t => t.CreadoPorUsuario)
                .WithMany()
                .HasForeignKey(t => t.CreadoPorUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Turno>()
                .Property(t => t.FechaCreacion)
                .HasDefaultValueSql("GETDATE()");


            // =============================
            // TURNO - INGRESO VEHÍCULO
            // 1 : 1
            // =============================

            modelBuilder.Entity<Turno>()
                .HasOne(t => t.IngresoVehiculo)
                .WithOne(i => i.Turno)
                .HasForeignKey<IngresoVehiculo>(i => i.TurnoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<IngresoVehiculo>()
                .HasIndex(i => i.TurnoId)
                .IsUnique();

            modelBuilder.Entity<IngresoVehiculo>()
                .Property(i => i.FechaIngreso)
                .HasDefaultValueSql("GETDATE()");


            // =============================
            // INGRESO VEHICULO - ORDEN DE TRABAJO
            // 1 : 0..1
            // =============================

            modelBuilder.Entity<IngresoVehiculo>()
                .HasOne(i => i.OrdenTrabajo)
                .WithOne(o => o.IngresoVehiculo)
                .HasForeignKey<OrdenTrabajo>(o => o.IngresoVehiculoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrdenTrabajo>()
                .HasIndex(o => o.IngresoVehiculoId)
                .IsUnique();


            // =============================
            // ORDEN DE TRABAJO - MECÁNICO
            // =============================

            modelBuilder.Entity<OrdenTrabajo>()
                .HasOne(o => o.Mecanico)
                .WithMany()
                .HasForeignKey(o => o.MecanicoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrdenTrabajo>()
                .HasOne(o => o.CreadaPorUsuario)
                .WithMany()
                .HasForeignKey(o => o.CreadaPorUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);


            // =============================
            // ORDEN DE TRABAJO - ESTADOS
            // =============================

            modelBuilder.Entity<OrdenTrabajoEstadoHistorial>()
                .HasOne(h => h.OrdenTrabajo)
                .WithMany(o => o.HistorialEstados)
                .HasForeignKey(h => h.OrdenTrabajoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrdenTrabajoEstadoHistorial>()
                .HasOne(h => h.Mecanico)
                .WithMany()
                .HasForeignKey(h => h.MecanicoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrdenTrabajoEstadoHistorial>()
                .Property(h => h.Fecha)
                .HasDefaultValueSql("GETDATE()");


            // =============================
            // DIAGNÓSTICO
            // =============================

            modelBuilder.Entity<Diagnostico>()
                .HasOne(d => d.OrdenTrabajo)
                .WithOne(o => o.Diagnostico)
                .HasForeignKey<Diagnostico>(d => d.OrdenTrabajoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Diagnostico>()
                .HasIndex(d => d.OrdenTrabajoId)
                .IsUnique();

            modelBuilder.Entity<Diagnostico>()
                .Property(d => d.FechaUltimaModificacion)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<DiagnosticoHistorial>()
                .HasOne(h => h.Diagnostico)
                .WithMany(d => d.Historial)
                .HasForeignKey(h => h.DiagnosticoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DiagnosticoHistorial>()
                .HasOne(h => h.Mecanico)
                .WithMany()
                .HasForeignKey(h => h.MecanicoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DiagnosticoHistorial>()
                .Property(h => h.Fecha)
                .HasDefaultValueSql("GETDATE()");


            // =============================
            // PRESUPUESTO
            // =============================

            modelBuilder.Entity<Presupuesto>()
                .HasOne(p => p.OrdenTrabajo)
                .WithOne(o => o.Presupuesto)
                .HasForeignKey<Presupuesto>(p => p.OrdenTrabajoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Presupuesto>()
                .HasOne(p => p.Mecanico)
                .WithMany()
                .HasForeignKey(p => p.MecanicoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Presupuesto>()
                .Property(p => p.FechaUltimaModificacion)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<PresupuestoHistorial>()
                .Property(p => p.Fecha)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<PresupuestoItem>()
                .HasOne(i => i.Presupuesto)
                .WithMany(p => p.Items)
                .HasForeignKey(i => i.PresupuestoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PresupuestoHistorial>()
                .HasOne(h => h.Presupuesto)
                .WithMany(p => p.Historial)
                .HasForeignKey(h => h.PresupuestoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PresupuestoHistorial>()
                .HasOne(h => h.Mecanico)
                .WithMany()
                .HasForeignKey(h => h.MecanicoId)
                .OnDelete(DeleteBehavior.Restrict);


            // =============================
            // CALIFICACIÓN
            // =============================

            modelBuilder.Entity<CalificacionTrabajo>()
                .HasOne(c => c.OrdenTrabajo)
                .WithOne(o => o.Calificacion)
                .HasForeignKey<CalificacionTrabajo>(c => c.OrdenTrabajoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CalificacionTrabajo>()
                .HasIndex(c => c.OrdenTrabajoId)
                .IsUnique();

            modelBuilder.Entity<CalificacionTrabajo>()
                .Property(c => c.Fecha)
                .HasDefaultValueSql("GETDATE()");


            // =============================
            // FACTURA
            // =============================

            modelBuilder.Entity<Factura>()
                .HasOne(f => f.OrdenTrabajo)
                .WithOne(o => o.Factura)
                .HasForeignKey<Factura>(f => f.OrdenTrabajoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Factura>()
                .HasIndex(f => f.NumeroFactura)
                .IsUnique();

            modelBuilder.Entity<Factura>()
                .Property(f => f.FechaEmision)
                .HasDefaultValueSql("GETDATE()");


            // =============================
            // PAGO
            // =============================

            modelBuilder.Entity<Pago>()
                .Property(p => p.FechaPago)
                .HasDefaultValueSql("GETDATE()");


            // =============================
            // STOCK
            // =============================

            modelBuilder.Entity<MovimientoStock>()
                .Property(m => m.Fecha)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<Repuesto>()
                .Property(r => r.FechaCreacion)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<Repuesto>()
                .HasIndex(r => r.SKU)
                .IsUnique();

            modelBuilder.Entity<Proveedor>()
                .Property(p => p.FechaCreacion)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<MovimientoStock>()
                .HasOne(m => m.Repuesto)
                .WithMany(r => r.Movimientos)
                .HasForeignKey(m => m.RepuestoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProveedorRepuesto>()
                .HasOne(pr => pr.Proveedor)
                .WithMany(p => p.Repuestos)
                .HasForeignKey(pr => pr.ProveedorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProveedorRepuesto>()
                .HasOne(pr => pr.Repuesto)
                .WithMany(r => r.Proveedores)
                .HasForeignKey(pr => pr.RepuestoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MovimientoStock>()
                .HasOne(m => m.ProveedorRepuesto)
                .WithMany()
                .HasForeignKey(m => m.ProveedorRepuestoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProveedorRepuesto>()
                .HasIndex(pr => new
                {
                    pr.ProveedorId,
                    pr.RepuestoId
                })
                .IsUnique();

            modelBuilder.Entity<PresupuestoItem>()
                .HasOne(i => i.Repuesto)
                .WithMany(r => r.PresupuestoItems)
                .HasForeignKey(i => i.RepuestoId)
                .OnDelete(DeleteBehavior.Restrict);


            // =============================
            // DOMINIO VEHICULAR
            // =============================

            modelBuilder.Entity<DominioVehicular>()
                .Property(d => d.FechaDesde)
                .HasDefaultValueSql("GETDATE()");


            // =============================
            // EVIDENCIAS
            // =============================

            modelBuilder.Entity<EvidenciaTrabajo>()
                .Property(e => e.Fecha)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<EvidenciaTrabajo>()
                .HasOne(e => e.OrdenTrabajo).WithMany(o => o.Evidencias)
                .HasForeignKey(e => e.OrdenTrabajoId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<EvidenciaTrabajo>()
                .HasOne(e => e.SubidaPorUsuario).WithMany()
                .HasForeignKey(e => e.SubidaPorUsuarioId).OnDelete(DeleteBehavior.Restrict);

            // Snapshots de envíos y asociaciones de evidencia histórica.
            modelBuilder.Entity<PresupuestoVersion>(entity =>
            {
                entity.HasOne(v => v.Presupuesto).WithMany(p => p.Versiones)
                    .HasForeignKey(v => v.PresupuestoId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(v => v.EnviadaPorUsuario).WithMany()
                    .HasForeignKey(v => v.EnviadaPorUsuarioId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(v => v.DecididaPorUsuario).WithMany()
                    .HasForeignKey(v => v.DecididaPorUsuarioId).OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(v => new { v.PresupuestoId, v.NumeroVersion }).IsUnique();
                entity.Property(v => v.Total).HasPrecision(18, 2);
                entity.Property(v => v.MotivoRechazo).HasMaxLength(1000);
                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_PresupuestoVersiones_NumeroVersion", "[NumeroVersion] > 0");
                    t.HasCheckConstraint("CK_PresupuestoVersiones_Total", "[Total] >= 0");
                });
            });
            modelBuilder.Entity<PresupuestoVersionItem>(entity =>
            {
                entity.HasOne(i => i.PresupuestoVersion).WithMany(v => v.Items)
                    .HasForeignKey(i => i.PresupuestoVersionId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(i => i.Repuesto).WithMany()
                    .HasForeignKey(i => i.RepuestoId).OnDelete(DeleteBehavior.Restrict);
                entity.Property(i => i.Descripcion).IsRequired();
                entity.Property(i => i.PrecioUnitario).HasPrecision(18, 2);
                entity.Ignore(i => i.Subtotal);
                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_PresupuestoVersionItems_Cantidad", "[Cantidad] > 0");
                    t.HasCheckConstraint("CK_PresupuestoVersionItems_PrecioUnitario", "[PrecioUnitario] >= 0");
                });
            });
            modelBuilder.Entity<PresupuestoVersionEvidencia>(entity =>
            {
                entity.HasKey(e => new { e.PresupuestoVersionId, e.EvidenciaTrabajoId });
                entity.HasOne(e => e.PresupuestoVersion).WithMany(v => v.Evidencias)
                    .HasForeignKey(e => e.PresupuestoVersionId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.EvidenciaTrabajo).WithMany(e => e.PresupuestoVersiones)
                    .HasForeignKey(e => e.EvidenciaTrabajoId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<DiagnosticoHistorialEvidencia>(entity =>
            {
                entity.HasKey(e => new { e.DiagnosticoHistorialId, e.EvidenciaTrabajoId });
                entity.HasOne(e => e.DiagnosticoHistorial).WithMany(h => h.Evidencias)
                    .HasForeignKey(e => e.DiagnosticoHistorialId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.EvidenciaTrabajo).WithMany(e => e.DiagnosticoHistoriales)
                    .HasForeignKey(e => e.EvidenciaTrabajoId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<DiagnosticoHistorial>()
                .HasOne(h => h.RegistradoPorUsuario).WithMany()
                .HasForeignKey(h => h.RegistradoPorUsuarioId).OnDelete(DeleteBehavior.Restrict);


            // =============================
            // AUDITORÍA
            // =============================

            modelBuilder.Entity<Auditoria>()
                .Property(a => a.Fecha)
                .HasDefaultValueSql("GETDATE()");


            // =============================
            // NOTIFICACIONES
            // =============================

            modelBuilder.Entity<Notificacion>()
                .Property(n => n.Fecha)
                .HasDefaultValueSql("GETDATE()");


            // =============================
            // GARANTÍA
            // =============================

            modelBuilder.Entity<Garantia>()
                .HasOne(g => g.OrdenTrabajo)
                .WithOne(o => o.Garantia)
                .HasForeignKey<Garantia>(g => g.OrdenTrabajoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Garantia>()
                .HasIndex(g => g.OrdenTrabajoId)
                .IsUnique();

            modelBuilder.Entity<GarantiaItem>()
                .HasOne(gi => gi.Garantia)
                .WithMany(g => g.Items)
                .HasForeignKey(gi => gi.GarantiaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GarantiaItem>()
                .HasOne(gi => gi.PresupuestoItem)
                .WithMany()
                .HasForeignKey(gi => gi.PresupuestoItemId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
