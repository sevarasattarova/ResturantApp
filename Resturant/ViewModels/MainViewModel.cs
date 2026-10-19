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

        public ICommand GoToClientOrdersCommand { get; }
        public ICommand GoToDishesCommand { get; }
        public ICommand GoToAdminOrdersCommand { get; }
        public ICommand GoToAdminDishesCommand { get; }
        public ICommand GoToUsersCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand LogoutCommand { get; }

        public MainViewModel()
        {
            if (AuthService.CurrentUser == null)
                CurrentPage = new LoginPage();
            else
                CurrentPage = new ClientOrdersPage();

            GoToClientOrdersCommand = new RelayCommand(o => CurrentPage = new ClientOrdersPage());
            GoToDishesCommand = new RelayCommand(o => CurrentPage = new DishesPage());
            GoToAdminOrdersCommand = new RelayCommand(o => CurrentPage = new AdminOrdersPage());
            GoToAdminDishesCommand = new RelayCommand(o => CurrentPage = new AdminDishesPage());
            GoToUsersCommand = new RelayCommand(o => CurrentPage = new UsersPage());

            ToggleThemeCommand = new RelayCommand(o =>
            {
                var currentBg = Application.Current.Resources["BackgroundBrush"] as SolidColorBrush;

                // Если сейчас светлая тема (цвет #F0F0F0) - включаем темную
                if (currentBg != null && currentBg.Color == (Color)ColorConverter.ConvertFromString("#F0F0F0"))
                {
                    Application.Current.Resources["BackgroundBrush"] = new SolidColorBrush(Color.FromRgb(45, 45, 48));
                    Application.Current.Resources["ForegroundBrush"] = new SolidColorBrush(Colors.White);

                    if (AuthService.CurrentUser != null)
                    {
                        AuthService.CurrentUser.AppTheme = "Dark"; // Обновляем в памяти

                        using (var context = new RestaurantDBEntities())
                        {
                            var dbUser = context.Users.Find(AuthService.CurrentUser.UserId);
                            if (dbUser != null) { dbUser.AppTheme = "Dark"; context.SaveChanges(); }
                        }
                    }
                }
                else // Если темная - включаем светлую
                {
                    Application.Current.Resources["BackgroundBrush"] = new SolidColorBrush(Color.FromRgb(240, 240, 240));
                    Application.Current.Resources["ForegroundBrush"] = new SolidColorBrush(Color.FromRgb(51, 51, 51));

                    if (AuthService.CurrentUser != null)
                    {
                        AuthService.CurrentUser.AppTheme = "Light"; // Обновляем в памяти

                        using (var context = new RestaurantDBEntities())
                        {
                            var dbUser = context.Users.Find(AuthService.CurrentUser.UserId);
                            if (dbUser != null) { dbUser.AppTheme = "Light"; context.SaveChanges(); }
                        }
                    }
                }
            });

            LogoutCommand = new RelayCommand(o =>
            {
                var result = MessageBox.Show("Вы действительно хотите выйти?", "Выход",
                                             MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    AuthService.CurrentUser = null;
                    if (Application.Current.MainWindow is MainWindow mainWin)
                    {
                        mainWin.DataContext = new MainViewModel();
                    }
                }
            });
        }
    }
}