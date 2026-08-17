namespace MecaniCar360.Models.DTOs
{
    public class ServiceResult
    {
        public bool Exitoso { get; set; }

        public string Mensaje { get; set; } = string.Empty;

        public static ServiceResult Ok(string mensaje = "")
        {
            return new ServiceResult
            {
                Exitoso = true,
                Mensaje = mensaje
            };
        }

        public static ServiceResult Error(string mensaje)
        {
            return new ServiceResult
            {
                Exitoso = false,
                Mensaje = mensaje
            };
        }
    }
}