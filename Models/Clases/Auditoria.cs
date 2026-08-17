using MecaniCar360.Models;

public class Auditoria
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; }

    public string Accion { get; set; }   // Crear, Modificar, Eliminar

    public string Entidad { get; set; }  // Turno, Presupuesto, Pago

    public int? EntidadId { get; set; }

    public string? Descripcion { get; set; }

    public DateTime Fecha { get; set; }
}