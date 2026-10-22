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

                //если сейчас светлая тема - включаем темную
                if (currentBg != null && currentBg.Color == (Color)ColorConverter.ConvertFromString("#F3F3F3"))
                {
                    Application.Current.Resources["BackgroundBrush"] = new SolidColorBrush(Color.FromRgb(25, 61, 87));  // #193D57 Темно-синий фон
                    Application.Current.Resources["MainBgBrush"] = new SolidColorBrush(Color.FromRgb(35, 62, 118));     // #233E76 Цвет чередующихся строк
                    Application.Current.Resources["ForegroundBrush"] = new SolidColorBrush(Colors.White);
                    if (AuthService.CurrentUser != null)
                    {
                        AuthService.CurrentUser.AppTheme = "Dark"; 

                        using (var context = new RestaurantDBEntities())
                        {
                            var dbUser = context.Users.Find(AuthService.CurrentUser.UserId);
                            if (dbUser != null) { dbUser.AppTheme = "Dark"; context.SaveChanges(); }
                        }
                    }
                }
                else //если темная - включаем светлую
                {
                    Application.Current.Resources["BackgroundBrush"] = new SolidColorBrush(Color.FromRgb(243, 243, 243)); // #F3F3F3 Светло-серый фон
                    Application.Current.Resources["MainBgBrush"] = new SolidColorBrush(Color.FromRgb(230, 230, 230));     // #E6E6E6 Цвет чередующихся строк (чуть темнее)
                    Application.Current.Resources["ForegroundBrush"] = new SolidColorBrush(Color.FromRgb(25, 61, 87));
                    if (AuthService.CurrentUser != null)
                    {
                        AuthService.CurrentUser.AppTheme = "Light"; 

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