namespace MecaniCar360.Helpers;

public static class OrigenInvitacion
{
    public static string Describir(int? emisorId, string? username) =>
        emisorId.HasValue ? "Emitida por " + (username ?? $"usuario #{emisorId}") : "Solicitud web";
}
