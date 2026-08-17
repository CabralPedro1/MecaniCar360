using MecaniCar360.Models;

namespace MecaniCar360.Models.DTOs
{
    public class LoginResult : ServiceResult
    {
        public Usuario? Usuario { get; set; }

        public List<string> Roles { get; set; } = new();
    }
}