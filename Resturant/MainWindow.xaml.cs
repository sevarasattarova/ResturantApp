using System.Windows;
using Resturant.ViewModels;

namespace Resturant
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

           
            DataContext = new MainViewModel();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            
            if (Resturant.Services.AuthService.CurrentUser == null)
            {
                return; 
            }

            var result = MessageBox.Show("Вы действительно хотите выйти из системы?",
                                         "Подтверждение выхода",
                                         MessageBoxButton.YesNo,
                                         MessageBoxImage.Question);

            if (result == MessageBoxResult.No)
            {
                e.Cancel = true; 
            }
        }

        private void BtnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            var changePwdWindow = new ChangePasswordWindow();
            changePwdWindow.ShowDialog();
        }
    }
}