using System;
using System.Collections.Generic;
using System.ComponentModel; // Для ICollectionView
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;   // Для CollectionViewSource

namespace Resturant.Pages
{
    public class CartItem
    {
        public int DishId { get; set; }
        public string DishName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Total => Price * Quantity;
    }

    public partial class DishesPage : Page
    {
        private List<CartItem> _cart = new List<CartItem>();
        private ICollectionView _dishesView; // Переменная для фильтрации

        public DishesPage()
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
                    var dishes = context.Dishes.Where(d => d.IsAvailable).ToList();

                    // Настраиваем фильтрацию
                    _dishesView = CollectionViewSource.GetDefaultView(dishes);
                    _dishesView.Filter = FilterDishes;
                    DishesGrid.ItemsSource = _dishesView;

                    // Заполняем ComboBox категориями
                    var categories = dishes.Select(d => d.Category).Distinct().ToList();
                    categories.Insert(0, "Все категории");
                    CmbCategory.ItemsSource = categories;
                    CmbCategory.SelectedIndex = 0;

                    // Загружаем столики
                    CmbTables.ItemsSource = context.Tables.ToList();
                    if (CmbTables.Items.Count > 0) CmbTables.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных: " + ex.Message);
            }
        }

        // Логика фильтрации (поиск + категория)
        private bool FilterDishes(object item)
        {
            var dish = item as Dishes;
            if (dish == null) return false;

            string searchText = TxtSearch.Text?.ToLower() ?? "";
            bool matchesSearch = string.IsNullOrWhiteSpace(searchText) ||
                                 dish.Name.ToLower().Contains(searchText) ||
                                 (dish.Description != null && dish.Description.ToLower().Contains(searchText));

            string selectedCategory = CmbCategory.SelectedItem as string;
            bool matchesCategory = selectedCategory == "Все категории" || string.IsNullOrEmpty(selectedCategory) ||
                                   dish.Category == selectedCategory;

            return matchesSearch && matchesCategory;
        }

        // Обработчики для фильтрации
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            _dishesView?.Refresh();
        }

        private void CmbCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _dishesView?.Refresh();
        }

        // Добавление в корзину
        private void BtnAddToCart_Click(object sender, RoutedEventArgs e)
        {
            var selectedDish = DishesGrid.SelectedItem as Dishes;
            if (selectedDish == null)
            {
                MessageBox.Show("Выберите блюдо из меню!", "Внимание");
                return;
            }

            if (!int.TryParse(TxtQuantity.Text, out int qty) || qty <= 0)
            {
                MessageBox.Show("Введите корректное количество!", "Ошибка");
                return;
            }

            var existingItem = _cart.FirstOrDefault(c => c.DishId == selectedDish.DishId);
            if (existingItem != null)
                existingItem.Quantity += qty;
            else
                _cart.Add(new CartItem { DishId = selectedDish.DishId, DishName = selectedDish.Name, Price = selectedDish.Price, Quantity = qty });

            UpdateCartUI();
        }

        private void UpdateCartUI()
        {
            CartGrid.ItemsSource = null;
            CartGrid.ItemsSource = _cart;
            TxtTotalSum.Text = $"{_cart.Sum(c => c.Total):N2} р.";
        }

        // Оформление заказа
        private void BtnCheckout_Click(object sender, RoutedEventArgs e)
        {
            if (_cart.Count == 0) { MessageBox.Show("Корзина пуста!"); return; }
            if (CmbTables.SelectedItem == null) { MessageBox.Show("Выберите столик!"); return; }

            var selectedTable = CmbTables.SelectedItem as Tables;

            try
            {
                using (var context = new RestaurantDBEntities())
                {
                    var newOrder = new Orders
                    {
                        UserId = Services.AuthService.CurrentUser.UserId,
                        TableId = selectedTable.TableId,
                        StatusId = 1,
                        OrderDate = DateTime.Now,
                        TotalAmount = _cart.Sum(c => c.Total)
                    };

                    context.Orders.Add(newOrder);
                    context.SaveChanges();

                    foreach (var item in _cart)
                    {
                        context.OrderItems.Add(new OrderItems
                        {
                            OrderId = newOrder.OrderId,
                            DishId = item.DishId,
                            Quantity = item.Quantity,
                            Price = item.Price
                        });
                    }
                    context.SaveChanges();
                }

                MessageBox.Show($"Заказ на столик №{selectedTable.TableNumber} оформлен!", "Успех");
                _cart.Clear();
                UpdateCartUI();
            }
            catch (Exception ex)
            {
                string errorMsg = ex.InnerException?.InnerException?.Message ?? ex.InnerException?.Message ?? ex.Message;
                MessageBox.Show("Ошибка БД:\n" + errorMsg, "Сбой");
            }
        }
    }
}