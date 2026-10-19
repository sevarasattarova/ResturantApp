using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Resturant.Pages
{
    public partial class AdminDishesPage : Page
    {
        public AdminDishesPage()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            using (var context = new RestaurantDBEntities())
            {
                DishesGrid.ItemsSource = context.Dishes.ToList();
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            using (var context = new RestaurantDBEntities())
            {
                var newDish = new Dishes
                {
                    Name = "Новое блюдо",
                    Category = "Разное",
                    Price = 100.00m,
                    Description = "Описание блюда", // Добавили описание
                    IsAvailable = true
                };
                context.Dishes.Add(newDish);
                context.SaveChanges();
            }
            LoadData();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Завершаем редактирование ячеек, чтобы данные попали в объект
            DishesGrid.CommitEdit(DataGridEditingUnit.Row, true);
            DishesGrid.CommitEdit(DataGridEditingUnit.Cell, true);

            try
            {
                using (var context = new RestaurantDBEntities())
                {
                    var dishes = DishesGrid.ItemsSource as System.Collections.Generic.List<Dishes>;
                    if (dishes != null)
                    {
                        foreach (var dish in dishes)
                        {
                            var dbDish = context.Dishes.Find(dish.DishId);
                            if (dbDish != null)
                            {
                                dbDish.Name = dish.Name;
                                dbDish.Category = dish.Category;
                                dbDish.Price = dish.Price;
                                dbDish.Description = dish.Description; // ВОТ ОНА! Сохраняем описание
                                dbDish.IsAvailable = dish.IsAvailable;
                            }
                        }
                        context.SaveChanges();
                    }
                }
                MessageBox.Show("Меню сохранено!", "Успех");
                LoadData();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Ошибка сохранения: " + ex.Message, "Ошибка");
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var selectedDish = DishesGrid.SelectedItem as Dishes;
            if (selectedDish == null) return;

            try
            {
                using (var context = new RestaurantDBEntities())
                {
                    // 1. Проверяем, заказывали ли это блюдо хоть раз
                    bool isOrdered = context.OrderItems.Any(oi => oi.DishId == selectedDish.DishId);

                    if (isOrdered)
                    {
                        MessageBox.Show($"Блюдо '{selectedDish.Name}' нельзя удалить, так как оно уже было заказано ранее.\n\n" +
                                        $"Чтобы убрать его из меню, просто снимите галочку 'В наличии' и нажмите 'Сохранить изменения'.",
                                        "Невозможно удалить", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // 2. Если блюдо не заказывали, удаляем
                    if (MessageBox.Show($"Удалить блюдо {selectedDish.Name}?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        var dish = context.Dishes.Find(selectedDish.DishId);
                        if (dish != null)
                        {
                            context.Dishes.Remove(dish);
                            context.SaveChanges();
                        }
                        LoadData();
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Ошибка при удалении: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}