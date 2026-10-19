using Resturant.Services;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Resturant.Pages
{
    public partial class RegisterPage : Page
    {
        public RegisterPage()
        {
            InitializeComponent();
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            var fullName = TxtFullName.Text.Trim();
            var login = TxtLogin.Text.Trim();
            var phone = TxtPhone.Text.Trim();
            var password = TxtPassword.Password;
            var confirmPassword = TxtPasswordConfirm.Password;

            // Валидация
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                TxtError.Text = "Заполните обязательные поля!";
                return;
            }
            if (password != confirmPassword)
            {
                TxtError.Text = "Пароли не совпадают!";
                return;
            }
            if (password.Length < 6)
            {
                TxtError.Text = "Пароль должен быть не менее 6 символов!";
                return;
            }
            if (!string.IsNullOrEmpty(phone) && (phone.Length != 12 || !phone.StartsWith("+7")))
            {
                TxtError.Text = "Телефон должен быть в формате +7XXXXXXXXXX";
                return;
            }

            using (var context = new RestaurantDBEntities()) // Ваш контекст БД
            {
                if (context.Users.Any(u => u.Login == login))
                {
                    TxtError.Text = "Пользователь с таким логином уже существует!";
                    return;
                }

                var newUser = new Users
                {
                    Login = login,
                    FullName = fullName,
                    Phone = phone,
                    Role = "Работник",
                    PasswordHash = AuthService.HashPassword(password),
                    FailedAttempts = 0,
                    AppTheme = "Light",
                    CreatedAt = DateTime.Now
                };

                context.Users.Add(newUser);
                context.SaveChanges();
            }

            MessageBox.Show("Регистрация прошла успешно!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

            // Возвращаемся на страницу входа
            NavigationService.Navigate(new LoginPage());
        }

        private void BtnBackToLogin_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new LoginPage());
        }
    }
}