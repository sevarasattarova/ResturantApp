using System.Linq;
using System.Windows.Controls;

namespace Resturant.Pages
{
    public partial class ClientOrdersPage : Page
    {
        public ClientOrdersPage()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            if (Services.AuthService.CurrentUser == null) return;

            using (var context = new RestaurantDBEntities())
            {
                int currentUserId = Services.AuthService.CurrentUser.UserId;
                string currentRole = Services.AuthService.CurrentUser.Role;

                var orderItems = context.OrderItems.ToList();

                // ЛОГИКА: Если Повар или Админ - видит ВСЕ заказы. Если Официант - только СВОИ.
                var orders = (currentRole == "Повар" || currentRole == "Администратор")
                    ? context.Orders.ToList()
                    : context.Orders.Where(o => o.UserId == currentUserId).ToList();

                var dishes = context.Dishes.ToList();
                var tables = context.Tables.ToList();
                var statuses = context.OrderStatuses.ToList();

                var displayList = (from oi in orderItems
                                   join o in orders on oi.OrderId equals o.OrderId
                                   join d in dishes on oi.DishId equals d.DishId
                                   join st in statuses on o.StatusId equals st.StatusId
                                   from t in tables.Where(x => x.TableId == o.TableId).DefaultIfEmpty()
                                   select new
                                   {
                                       OrderId = o.OrderId,
                                       TableNumber = t != null ? t.TableNumber.ToString() : "На вынос",
                                       DishName = d.Name,
                                       Quantity = oi.Quantity,
                                       TotalAmount = oi.Price * oi.Quantity,
                                       Status = st.Name,
                                       OrderDate = o.OrderDate
                                   }).ToList();

                OrdersGrid.ItemsSource = displayList;
            }
        }
    }
}