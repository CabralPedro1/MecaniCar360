using System.Globalization;
using MecaniCar360.Models.ViewModels;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
namespace MecaniCar360.Services;

public static class ReportePdf
{
    // El despliegue actual es Windows. No se solicitan fuentes ni recursos remotos.
    static ReportePdf() { GlobalFontSettings.UseWindowsFontsUnderWindows = true; }
    public static byte[] Generar(ReportesViewModel m, DateTime generado)
    {
        using var doc = new PdfDocument();
        doc.Info.Title = "MecaniCar360 - Rendimiento del taller";
        var page = doc.AddPage(); page.Size = PdfSharp.PageSize.A4;
        using var g = XGraphics.FromPdfPage(page);
        var normal = new XFont("Arial", 11); var titulo = new XFont("Arial", 20, XFontStyleEx.Bold);
        var negrita = new XFont("Arial", 12, XFontStyleEx.Bold);
        double y = 50;
        void Linea(string texto, XFont? fuente = null) { g.DrawString(texto, fuente ?? normal, XBrushes.Black, new XPoint(40, y)); y += 24; }
        Linea("MecaniCar360", titulo);
        Linea("Reporte de rendimiento del taller", negrita);
        Linea($"Período: {m.Filtro.Desde:dd/MM/yyyy} a {m.Filtro.Hasta:dd/MM/yyyy}");
        Linea($"Generado: {generado:dd/MM/yyyy HH:mm}");
        y += 10;
        Linea($"Órdenes ingresadas: {m.Ingresadas}");
        Linea($"Con finalización registrada: {m.Finalizadas} ({m.PorcentajeFinalizadas.ToString("N1", CultureInfo.GetCultureInfo("es-AR"))}%)");
        Linea($"Entregadas: {m.Entregadas} ({m.PorcentajeEntregadas.ToString("N1", CultureInfo.GetCultureInfo("es-AR"))}%)");
        Linea($"En reparación: {m.EnReparacion} | Rechazadas: {m.Rechazadas}");
        Linea("Cohorte por fecha de inicio; estado actual a la fecha de consulta.");
        Linea($"De {m.Ingresadas} órdenes ingresadas, {m.Finalizadas} finalizaron y {m.Entregadas} se entregaron.");
        y += 15; Linea("Distribución actual por estado", negrita);
        var max = Math.Max(1, m.Estados.Max(e => e.Cantidad));
        foreach (var e in m.Estados)
        {
            g.DrawString(e.Estado.ToString(), normal, XBrushes.Black, new XPoint(40, y));
            g.DrawRectangle(XBrushes.SteelBlue, 205, y - 11, 230.0 * e.Cantidad / max, 14);
            g.DrawString(e.Cantidad.ToString(), normal, XBrushes.Black, new XPoint(455, y));
            y += 30;
        }
        y += 15;
        Linea("Los valores en cero representan ausencia de órdenes en ese estado.");
        Linea("Las finalizadas incluyen las entregadas con fecha de fin registrada.");
        using var stream = new MemoryStream(); doc.Save(stream, false); return stream.ToArray();
    }
}
