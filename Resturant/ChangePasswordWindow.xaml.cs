using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Resturant
{
    /// <summary>
    /// Логика взаимодействия для ChangePasswordWindow.xaml
    /// </summary>
    public partial class ChangePasswordWindow : Window
    {
        public ChangePasswordWindow()
        {
            InitializeComponent();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var oldPwd = TxtOldPassword.Password;
            var newPwd = TxtNewPassword.Password;
            var confirmPwd = TxtConfirmPassword.Password;

            if (string.IsNullOrWhiteSpace(newPwd) || newPwd.Length < 6)
            {
                MessageBox.Show("Новый пароль должен быть не менее 6 символов!");
                return;
            }

            if (newPwd != confirmPwd)
            {
                MessageBox.Show("Новые пароли не совпадают!");
                return;
            }

            using (var context = new RestaurantDBEntities())
            {
                var currentUser = context.Users.Find(Services.AuthService.CurrentUser.UserId);

                // Проверяем старый пароль
                if (currentUser.PasswordHash != Services.AuthService.HashPassword(oldPwd))
                {
                    MessageBox.Show("Старый пароль введен неверно!");
                    return;
                }

                // Обновляем хэш
                currentUser.PasswordHash = Services.AuthService.HashPassword(newPwd);
                context.SaveChanges();
            }

            MessageBox.Show("Пароль успешно изменен!");
            this.Close();
        }
    }
}
