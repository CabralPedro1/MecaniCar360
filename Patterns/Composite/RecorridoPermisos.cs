namespace MecaniCar360.Patterns.Composite
{
    internal static class RecorridoPermisos
    {
        // Todo el estado pertenece a esta enumeración, nunca a otro usuario/request.
        internal static IEnumerable<string> ObtenerPatentes(IEnumerable<IComponentePermiso> raices)
        {
            var pendientes = new Stack<IComponentePermiso>(raices.Reverse());
            var visitados = new HashSet<IComponentePermiso>(ReferenceEqualityComparer.Instance);
            var nombres = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            while (pendientes.TryPop(out var componente))
            {
                // La identidad por referencia no mezcla IDs de familias y patentes.
                if (!visitados.Add(componente)) continue;

                switch (componente)
                {
                    case PatentePermiso patente when patente.Activo:
                        if (nombres.Add(patente.Nombre)) yield return patente.Nombre;
                        break;
                    case FamiliaPermiso familia when familia.Activo:
                        var hijos = familia.Componentes;
                        for (var i = hijos.Count - 1; i >= 0; i--)
                            pendientes.Push(hijos[i]);
                        break;
                }
            }
        }
    }
}
