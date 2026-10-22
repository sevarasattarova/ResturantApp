using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Resturant.Pages
{
    public partial class UsersPage : Page
    {
        public UsersPage()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                using (var context = new RestaurantDBEntities())
                {
                    UsersGrid.ItemsSource = context.Users.ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки: " + ex.Message);
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => LoadData();

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var context = new RestaurantDBEntities())
                {
                    var newUser = new Users
                    {
                        Login = "user_" + DateTime.Now.Ticks.ToString().Substring(10),
                        FullName = "Новый пользователь",
                        Role = "Официант",
                        Phone = "+70000000000",
                        PasswordHash = Services.AuthService.HashPassword("123456"),
                        FailedAttempts = 0,
                        AppTheme = "Light",
                        CreatedAt = DateTime.Now
                    };
                    context.Users.Add(newUser);
                    context.SaveChanges();
                }
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при добавлении: " + ex.Message, "Ошибка");
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            //фиксируем ввод в таблице, чтобы данные попали в объекты
            UsersGrid.CommitEdit(DataGridEditingUnit.Row, true);
            UsersGrid.CommitEdit(DataGridEditingUnit.Cell, true);

            try
            {
                using (var context = new RestaurantDBEntities())
                {
                    var users = UsersGrid.ItemsSource as List<Users>;
                    if (users != null)
                    {
                        foreach (var user in users)
                        {
                            if (string.IsNullOrWhiteSpace(user.Role))
                            {
                                MessageBox.Show($"У пользователя {user.FullName} не выбрана роль! Выберите роль из списка.", "Ошибка");
                                return; 
                            }
                            var dbUser = context.Users.Find(user.UserId);
                            if (dbUser != null)
                            {
                                if (context.Users.Any(u => u.Login == user.Login && u.UserId != user.UserId))
                                {
                                    MessageBox.Show($"Логин '{user.Login}' уже занят другим пользователем!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    return; 
                                }

                                dbUser.FullName = user.FullName;
                                dbUser.Login = user.Login;
                                dbUser.Phone = user.Phone;
                                dbUser.Role = user.Role;
                            }
                        }
                        context.SaveChanges();
                    }
                }
                MessageBox.Show("Пользователи сохранены!", "Успех");
                LoadData();
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException ex)
            {
                string realError = ex.InnerException?.InnerException?.Message ?? ex.InnerException?.Message ?? ex.Message;
                MessageBox.Show("ОШИБКА БАЗЫ ДАННЫХ:\n\n" + realError, "Сбой сохранения", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Ошибка");
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var selectedUser = UsersGrid.SelectedItem as Users;
            if (selectedUser == null) return;

            if (selectedUser.UserId == Services.AuthService.CurrentUser.UserId)
            {
                MessageBox.Show("Нельзя удалить самого себя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show($"Удалить пользователя {selectedUser.FullName}?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new RestaurantDBEntities())
                    {
                        var user = context.Users.Find(selectedUser.UserId);
                        if (user != null)
                        {
                            context.Users.Remove(user);
                            context.SaveChanges();
                        }
                    }
                    LoadData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при удалении: " + ex.Message, "Ошибка");
                }
            }
        }
    }
}