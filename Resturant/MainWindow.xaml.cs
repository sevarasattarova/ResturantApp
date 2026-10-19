using System.Windows;
using Resturant.ViewModels;

namespace Resturant
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Устанавливаем DataContext. 
            // Это связывает наш XAML (кнопки, Frame) с логикой в MainViewModel.
            DataContext = new MainViewModel();
        }

        // Обработчик закрытия окна (требование п. 9: подтверждение выхода)
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Если пользователь еще не авторизован (находится на экране входа), 
            // можно просто закрыть окно без лишних вопросов, либо спросить.
            if (Resturant.Services.AuthService.CurrentUser == null)
            {
                return; // Закрываем без вопросов
            }

            var result = MessageBox.Show("Вы действительно хотите выйти из системы?",
                                         "Подтверждение выхода",
                                         MessageBoxButton.YesNo,
                                         MessageBoxImage.Question);

            if (result == MessageBoxResult.No)
            {
                e.Cancel = true; // Отменяем закрытие окна
            }
        }

        private void BtnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            var changePwdWindow = new ChangePasswordWindow();
            changePwdWindow.ShowDialog();
        }
    }
}