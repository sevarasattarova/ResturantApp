using Resturant.Helpers;
using Resturant.Pages;
using Resturant.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Resturant.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private Page _currentPage;
        public Page CurrentPage
        {
            get => _currentPage;
            set { _currentPage = value; OnPropertyChanged(); }
        }

        public bool IsLoggedIn => AuthService.CurrentUser != null;
        public bool IsAdmin => AuthService.CurrentUser?.Role == "Администратор";

        // Команды навигации
        public ICommand GoToClientOrdersCommand { get; }
        public ICommand GoToDishesCommand { get; }
        public ICommand GoToAdminOrdersCommand { get; }
        public ICommand GoToAdminDishesCommand { get; }
        public ICommand GoToUsersCommand { get; }

        // Системные команды
        public ICommand ToggleThemeCommand { get; }
        public ICommand LogoutCommand { get; }

        public MainViewModel()
        {
            // Стартовая страница
            if (AuthService.CurrentUser == null)
                CurrentPage = new LoginPage();
            else
                CurrentPage = new ClientOrdersPage();

            // НАВИГАЦИЯ (создаем новые экземпляры страниц)
            GoToClientOrdersCommand = new RelayCommand(o => CurrentPage = new ClientOrdersPage());
            GoToDishesCommand = new RelayCommand(o => CurrentPage = new DishesPage());
            GoToAdminOrdersCommand = new RelayCommand(o => CurrentPage = new AdminOrdersPage());
            GoToAdminDishesCommand = new RelayCommand(o => CurrentPage = new AdminDishesPage());
            GoToUsersCommand = new RelayCommand(o => CurrentPage = new UsersPage());

            // СМЕНА ТЕМЫ
            ToggleThemeCommand = new RelayCommand(o =>
            {
                var currentBg = Application.Current.Resources["BackgroundBrush"] as SolidColorBrush;

                if (currentBg != null && currentBg.Color == (Color)ColorConverter.ConvertFromString("#F0F0F0"))
                {
                    Application.Current.Resources["BackgroundBrush"] = new SolidColorBrush(Color.FromRgb(45, 45, 48));
                    Application.Current.Resources["ForegroundBrush"] = new SolidColorBrush(Colors.White);
                }
                else
                {
                    Application.Current.Resources["BackgroundBrush"] = new SolidColorBrush(Color.FromRgb(240, 240, 240));
                    Application.Current.Resources["ForegroundBrush"] = new SolidColorBrush(Color.FromRgb(51, 51, 51));
                }
            });

            // ВЫХОД ИЗ СИСТЕМЫ
            LogoutCommand = new RelayCommand(o =>
            {
                var result = MessageBox.Show("Вы действительно хотите выйти?", "Выход",
                                             MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    AuthService.CurrentUser = null; // Сбрасываем пользователя

                    // Пересоздаем MainViewModel, чтобы скрыть меню и вернуть экран входа
                    if (Application.Current.MainWindow is MainWindow mainWin)
                    {
                        mainWin.DataContext = new MainViewModel();
                    }
                }
            });
        }
    }
}