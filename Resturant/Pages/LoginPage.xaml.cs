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
            if (Services.AuthService.CurrentUser == null) return;

            if (Services.AuthService.CurrentUser.AppTheme == "Dark")
            {
                Application.Current.Resources["BackgroundBrush"] = new SolidColorBrush(Color.FromRgb(25, 61, 87));  
                Application.Current.Resources["MainBgBrush"] = new SolidColorBrush(Color.FromRgb(35, 62, 118));     
                Application.Current.Resources["ForegroundBrush"] = new SolidColorBrush(Colors.White);               
            }
            else
            {
                Application.Current.Resources["BackgroundBrush"] = new SolidColorBrush(Color.FromRgb(243, 243, 243)); 
                Application.Current.Resources["MainBgBrush"] = new SolidColorBrush(Color.FromRgb(230, 230, 230));     
                Application.Current.Resources["ForegroundBrush"] = new SolidColorBrush(Color.FromRgb(25, 61, 87));   
            }
        }
        private void BtnGoToRegister_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new RegisterPage());
        }
    }
}