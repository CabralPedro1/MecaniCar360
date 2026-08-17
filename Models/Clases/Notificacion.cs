using MecaniCar360.Models;

public class Notificacion
{
    public int Id { get; set; }

    public int PersonaId { get; set; }
    public Persona Persona { get; set; }

    public string Mensaje { get; set; }

    public bool Leida { get; set; }

    public DateTime Fecha { get; set; }
}