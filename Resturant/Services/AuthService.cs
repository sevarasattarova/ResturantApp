using Resturant; // Ваше пространство имен (где лежит Model1.edmx)
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Resturant.Services
{
    public static class AuthService
    {
        public static Users CurrentUser { get; set; }

        // Метод для создания хэша пароля (нужен будет при регистрации)
        public static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var builder = new StringBuilder();
                foreach (var b in bytes)
                {
                    builder.Append(b.ToString("x2")); // Переводим в HEX-строку
                }
                return builder.ToString();
            }
        }

        public static bool Login(string login, string password)
        {
            // Замените ResturantEntities на имя вашего класса контекста БД
            using (var context = new RestaurantDBEntities())
            {
                var user = context.Users.FirstOrDefault(u => u.Login == login);

                if (user == null)
                    return false;

                // Получаем хэш от введенного пароля
                string enteredHash = HashPassword(password);

                // Сравниваем с тем, что лежит в БД
                if (user.PasswordHash == enteredHash)
                {
                    CurrentUser = user;
                    return true;
                }

                return false;
            }
        }
    }
}