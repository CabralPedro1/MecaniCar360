namespace MecaniCar360.Models.DTOs
{
    public class ServiceResult<T> where T : class
    {
        public bool Exitoso { get; set; }

        public string Mensaje { get; set; } = string.Empty;

        public T? Data { get; set; }

        public static ServiceResult<T> Ok(
            T data,
            string mensaje = "")
        {
            return new ServiceResult<T>
            {
                Exitoso = true,
                Data = data,
                Mensaje = mensaje
            };
        }

        public static ServiceResult<T> Error(string mensaje)
        {
            return new ServiceResult<T>
            {
                Exitoso = false,
                Mensaje = mensaje
            };
        }
    }
}