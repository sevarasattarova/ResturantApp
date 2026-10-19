using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Resturant.Services
{
    public static class AuthService
    {
        public static Users CurrentUser { get; set; }

        // 1. Отдельный метод хэширования
        public static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return string.Concat(bytes.Select(b => b.ToString("x2")));
            }
        }

        // 2. Отдельный метод генерации капчи
        public static string GenerateCaptcha()
        {
            Random rnd = new Random();
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            return new string(Enumerable.Repeat(chars, 5).Select(s => s[rnd.Next(s.Length)]).ToArray());
        }

        // 3. Отдельный метод аутентификации (и проверки попыток)
        public static (bool Success, string Message, bool NeedCaptcha) Login(string login, string password, string enteredCaptcha, string actualCaptcha)
        {
            using (var context = new RestaurantDBEntities())
            {
                var user = context.Users.FirstOrDefault(u => u.Login == login);
                if (user == null) return (false, "Пользователь не найден", false);

                // Проверка на блокировку/капчу
                if (user.FailedAttempts >= 3 && enteredCaptcha != actualCaptcha)
                    return (false, "Неверная капча!", true);

                if (user.PasswordHash == HashPassword(password))
                {
                    user.FailedAttempts = 0; // Сброс попыток при успехе
                    context.SaveChanges();
                    CurrentUser = user;
                    return (true, "Успех", false);
                }
                else
                {
                    user.FailedAttempts++;
                    context.SaveChanges();
                    bool needCaptcha = user.FailedAttempts >= 3;
                    return (false, $"Неверный пароль. Осталось попыток: {3 - user.FailedAttempts}", needCaptcha);
                }
            }
        }
    }
}