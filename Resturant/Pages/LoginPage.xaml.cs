using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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

            //ыызов метода авторизации
            var result = Services.AuthService.Login(login, password, TxtCaptchaInput.Text, _currentCaptcha);

            if (result.Success)
            {
                ApplySavedTheme();

                if (Window.GetWindow(this) is MainWindow mainWin)
                {
                    mainWin.DataContext = new Resturant.ViewModels.MainViewModel();
                }
            }
            else
            {
                _failedAttempts++;
                TxtError.Text = result.Message;

                if (result.NeedCaptcha)
                {
                    CaptchaPanel.Visibility = Visibility.Visible;
                    GenerateNewCaptcha();
                }
            }
        }

        private void GenerateNewCaptcha()
        {
            _currentCaptcha = Services.AuthService.GenerateCaptcha();
            TxtCaptchaCode.Text = _currentCaptcha;
            TxtCaptchaInput.Clear();
        }

        private void ApplySavedTheme()
        {
            if (Services.AuthService.CurrentUser != null && Services.AuthService.CurrentUser.AppTheme == "Dark")
            {
                Application.Current.Resources["BackgroundBrush"] = new SolidColorBrush(Color.FromRgb(45, 45, 48));
                Application.Current.Resources["ForegroundBrush"] = new SolidColorBrush(Colors.White);
            }
            else
            {
                Application.Current.Resources["BackgroundBrush"] = new SolidColorBrush(Color.FromRgb(240, 240, 240));
                Application.Current.Resources["ForegroundBrush"] = new SolidColorBrush(Color.FromRgb(51, 51, 51));
            }
        }

        private void BtnGoToRegister_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new RegisterPage());
        }
    }
}