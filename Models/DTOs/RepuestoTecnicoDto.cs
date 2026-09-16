namespace MecaniCar360.Models.DTOs;

public sealed record RepuestoTecnicoDto(int Id, string SKU, string Nombre, string? Compatibilidad,
    int StockActual, int StockMinimo);
