namespace MecaniCar360.Models.DTOs;

public enum CondicionIntegridad { NoVerificada, Valida, Comprometida }
public sealed record ErrorIntegridad(string Entidad, string? Clave, string Tipo);
public sealed record ResultadoIntegridad(CondicionIntegridad Estado, DateTime FechaUtc,
    int VersionAlgoritmo, string? IncidenteId, IReadOnlyList<ErrorIntegridad> Errores)
{
    public bool EsValida => Estado == CondicionIntegridad.Valida;
}
public sealed class IntegridadException : Exception
{
    public IReadOnlyList<ErrorIntegridad> Errores { get; }
    public IntegridadException(IReadOnlyList<ErrorIntegridad> errores)
        : base("La verificacion de integridad impide continuar la operacion.") => Errores = errores;
}
