    using System.Text.RegularExpressions;

    namespace MecaniCar360.Helpers
    {
        public static class PasswordValidator
        {
            public static bool EsValida(string password, out string error)
            {
                error = "";

                if (string.IsNullOrWhiteSpace(password))
                {
                    error = "La contraseña es obligatoria.";
                    return false;
                }

                if (password.Length < 8)
                {
                    error = "Debe tener al menos 8 caracteres.";
                    return false;
                }

                if (!Regex.IsMatch(password, @"[A-Z]"))
                {
                    error = "Debe contener al menos una letra mayúscula.";
                    return false;
                }

                if (!Regex.IsMatch(password, @"[a-z]"))
                {
                    error = "Debe contener al menos una letra minúscula.";
                    return false;
                }

                if (!Regex.IsMatch(password, @"\d"))
                {
                    error = "Debe contener al menos un número.";
                    return false;
                }

                if (!Regex.IsMatch(password, @"[^a-zA-Z0-9]"))
                {
                    error = "Debe contener al menos un símbolo.";
                    return false;
                }

                return true;
            }
        }
    }
