using Resturant.Services;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Resturant.Pages
{
    public partial class LoginPage : Page
    {
        private int _failedAttempts = 0;
        private string _currentCaptcha = "";

        public LoginPage()
        {
            InitializeComponent();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            var login = TxtLogin.Text.Trim();
            var password = TxtPassword.Password;

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                TxtError.Text = "Заполните все поля!";
                return;
            }

            if (CaptchaPanel.Visibility == Visibility.Visible && TxtCaptchaInput.Text != _currentCaptcha)
            {
                TxtError.Text = "Неверный код капчи!";
                GenerateNewCaptcha();
                return;
            }

            if (AuthService.Login(login, password))
            {
                // Если вход успешен - переходим в главное меню
                if (Application.Current.MainWindow is MainWindow mainWin)
                {
                    mainWin.DataContext = new Resturant.ViewModels.MainViewModel(); // Обновляем VM, чтобы IsLoggedIn стало true
                    mainWin.MainFrame.Navigate(new ClientOrdersPage()); // Открываем стартовую страницу
                }
            }
            else
            {
                _failedAttempts++;
                TxtError.Text = $"Неверный логин или пароль. Попытка {_failedAttempts}.";

                if (_failedAttempts >= 3)
                {
                    CaptchaPanel.Visibility = Visibility.Visible;
                    GenerateNewCaptcha();
                    TxtError.Text = "Слишком много попыток! Введите капчу.";
                }
            }
        }

        private void GenerateNewCaptcha()
        {
            Random rnd = new Random();
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            _currentCaptcha = new string(Enumerable.Repeat(chars, 5).Select(s => s[rnd.Next(s.Length)]).ToArray());
            TxtCaptchaCode.Text = _currentCaptcha;
            TxtCaptchaInput.Clear();
        }

        private void BtnGoToRegister_Click(object sender, RoutedEventArgs e)
        {
            // Навигация на страницу регистрации
            NavigationService.Navigate(new RegisterPage());
        }
    }
}