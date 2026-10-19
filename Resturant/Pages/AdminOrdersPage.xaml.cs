using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

// Для экспорта
using Microsoft.Win32;
using ClosedXML.Excel;
using Xceed.Words.NET;
using Xceed.Document.NET;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Resturant.Pages
{
    // Класс для отображения строки в таблице
    public class AdminOrderViewModel
    {
        public int OrderId { get; set; }
        public int Quantity { get; set; }
        public string Status { get; set; }
        public decimal ItemTotal { get; set; }
        public DateTime OrderDate { get; set; }
        public string TableNumber { get; set; }
        public string DishName { get; set; }
        public decimal Price { get; set; }
    }

    public partial class AdminOrdersPage : Page
    {
        public AdminOrdersPage()
        {
            InitializeComponent();
            LoadData();
        }

        // --- ЗАГРУЗКА ДАННЫХ ИЗ БД ---
        private void LoadData()
        {
            using (var context = new RestaurantDBEntities())
            {
                var orderItems = context.OrderItems.ToList();
                var orders = context.Orders.ToList();
                var dishes = context.Dishes.ToList();
                var tables = context.Tables.ToList();
                var statuses = context.OrderStatuses.ToList();

                var displayList = (from oi in orderItems
                                   join o in orders on oi.OrderId equals o.OrderId
                                   join d in dishes on oi.DishId equals d.DishId
                                   join st in statuses on o.StatusId equals st.StatusId
                                   from t in tables.Where(x => x.TableId == o.TableId).DefaultIfEmpty()
                                   select new AdminOrderViewModel
                                   {
                                       OrderId = o.OrderId,
                                       Quantity = oi.Quantity,
                                       Status = st.Name,
                                       ItemTotal = oi.Price * oi.Quantity,
                                       OrderDate = o.OrderDate,
                                       TableNumber = t != null ? t.TableNumber.ToString() : "На вынос",
                                       DishName = d.Name,
                                       Price = oi.Price
                                   }).ToList();

                OrdersGrid.ItemsSource = displayList;
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => LoadData();

        // --- ПЕРЕСЧЕТ СУММЫ ПРИ ИЗМЕНЕНИИ КОЛИЧЕСТВА (ЭТОТ МЕТОД ЧИНИТ 1-Ю ОШИБКУ) ---
        private void OrdersGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            Dispatcher.BeginInvoke(new System.Action(() =>
            {
                var row = e.Row.Item as AdminOrderViewModel;
                if (row == null) return;

                if (e.Column.Header.ToString() == "Кол-во")
                {
                    row.ItemTotal = row.Price * row.Quantity;
                    OrdersGrid.Items.Refresh();
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        // --- СОХРАНЕНИЕ ИЗМЕНЕНИЙ (СТАТУС И КОЛ-ВО) ---
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            OrdersGrid.CommitEdit(DataGridEditingUnit.Row, true);

            try
            {
                using (var context = new RestaurantDBEntities())
                {
                    var list = OrdersGrid.ItemsSource as List<AdminOrderViewModel>;
                    if (list != null)
                    {
                        var uniqueOrderIds = list.Select(x => x.OrderId).Distinct();
                        var allStatuses = context.OrderStatuses.ToList();

                        foreach (var orderId in uniqueOrderIds)
                        {
                            var dbOrder = context.Orders.Find(orderId);
                            if (dbOrder != null)
                            {
                                var row = list.First(x => x.OrderId == orderId);

                                var statusObj = allStatuses.FirstOrDefault(s => s.Name.Trim().ToLower() == row.Status.Trim().ToLower());
                                if (statusObj != null)
                                    dbOrder.StatusId = statusObj.StatusId;
                                else
                                {
                                    MessageBox.Show($"Статус '{row.Status}' не найден в базе! Используйте: Новый, Готовится, Подан, Оплачен.", "Ошибка");
                                    return;
                                }

                                dbOrder.TotalAmount = list.Where(x => x.OrderId == orderId).Sum(x => x.ItemTotal);
                            }
                        }

                        foreach (var item in list)
                        {
                            var dish = context.Dishes.FirstOrDefault(d => d.Name == item.DishName);
                            if (dish != null)
                            {
                                var dbItem = context.OrderItems.FirstOrDefault(oi => oi.OrderId == item.OrderId && oi.DishId == dish.DishId);
                                if (dbItem != null) dbItem.Quantity = item.Quantity;
                            }
                        }

                        context.SaveChanges();
                    }
                }
                MessageBox.Show("Данные сохранены!", "Успех");
                LoadData();
            }
            catch (Exception ex)
            {
                string realError = ex.InnerException?.InnerException?.Message ?? ex.InnerException?.Message ?? ex.Message;
                MessageBox.Show("ОШИБКА БД:\n\n" + realError, "Сбой сохранения");
            }
        }

        // --- УДАЛЕНИЕ ЗАКАЗА ---
        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var selected = OrdersGrid.SelectedItem as AdminOrderViewModel;
            if (selected == null) return;

            if (MessageBox.Show($"Удалить заказ №{selected.OrderId} целиком?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                using (var context = new RestaurantDBEntities())
                {
                    var order = context.Orders.Find(selected.OrderId);
                    if (order != null)
                    {
                        context.Orders.Remove(order);
                        context.SaveChanges();
                    }
                }
                LoadData();
            }
        }

        // =========================================================================
        // ============================ ЭКСПОРТ ОТЧЕТОВ ============================
        // =========================================================================

        // 1. ЭКСПОРТ В EXCEL (.xlsx)
        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog { Filter = "Excel Files|*.xlsx", FileName = "Отчет_Заказы.xlsx" };
            if (sfd.ShowDialog() == true)
            {
                try
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var ws = workbook.Worksheets.Add("Заказы");

                        ws.Cell(1, 1).Value = "№ Заказа";
                        ws.Cell(1, 2).Value = "Столик";
                        ws.Cell(1, 3).Value = "Блюдо";
                        ws.Cell(1, 4).Value = "Кол-во";
                        ws.Cell(1, 5).Value = "Сумма";
                        ws.Cell(1, 6).Value = "Статус";

                        var list = OrdersGrid.ItemsSource as List<AdminOrderViewModel>;
                        for (int i = 0; i < list.Count; i++)
                        {
                            ws.Cell(i + 2, 1).Value = list[i].OrderId;
                            ws.Cell(i + 2, 2).Value = list[i].TableNumber;
                            ws.Cell(i + 2, 3).Value = list[i].DishName;
                            ws.Cell(i + 2, 4).Value = list[i].Quantity;
                            ws.Cell(i + 2, 5).Value = list[i].ItemTotal;
                            ws.Cell(i + 2, 6).Value = list[i].Status;
                        }

                        ws.Columns().AdjustToContents();
                        workbook.SaveAs(sfd.FileName);
                    }
                    MessageBox.Show("Отчет успешно экспортирован в Excel!", "Успех");
                }
                catch (Exception ex) { MessageBox.Show("Ошибка при экспорте: " + ex.Message); }
            }
        }

        // 2. ЭКСПОРТ В WORD (.docx)
        private void BtnExportWord_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog { Filter = "Word Files|*.docx", FileName = "Отчет_Заказы.docx" };
            if (sfd.ShowDialog() == true)
            {
                try
                {
                    using (var doc = DocX.Create(sfd.FileName))
                    {
                        doc.InsertParagraph("Отчет по заказам ресторана").FontSize(20).Bold().Alignment = Alignment.center;
                        doc.InsertParagraph($"Дата формирования: {DateTime.Now:g}\n").FontSize(12).Alignment = Alignment.center;

                        var table = doc.AddTable(1, 5);
                        table.Rows[0].Cells[0].Paragraphs.First().Append("№ Заказа").Bold();
                        table.Rows[0].Cells[1].Paragraphs.First().Append("Столик").Bold();
                        table.Rows[0].Cells[2].Paragraphs.First().Append("Блюдо").Bold();
                        table.Rows[0].Cells[3].Paragraphs.First().Append("Кол-во").Bold();
                        table.Rows[0].Cells[4].Paragraphs.First().Append("Сумма").Bold();

                        var list = OrdersGrid.ItemsSource as List<AdminOrderViewModel>;
                        foreach (var item in list)
                        {
                            var row = table.InsertRow();
                            row.Cells[0].Paragraphs.First().Append(item.OrderId.ToString());
                            row.Cells[1].Paragraphs.First().Append(item.TableNumber);
                            row.Cells[2].Paragraphs.First().Append(item.DishName);
                            row.Cells[3].Paragraphs.First().Append(item.Quantity.ToString());
                            row.Cells[4].Paragraphs.First().Append(item.ItemTotal.ToString("N2") + " р.");
                        }

                        doc.InsertTable(table);
                        doc.Save();
                    }
                    MessageBox.Show("Отчет успешно экспортирован в Word!", "Успех");
                }
                catch (Exception ex) { MessageBox.Show("Ошибка при экспорте: " + ex.Message); }
            }
        }

        // 3. ЭКСПОРТ В PDF (.pdf)
        private void BtnExportPdf_Click(object sender, RoutedEventArgs e)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var sfd = new SaveFileDialog { Filter = "PDF Files|*.pdf", FileName = "Отчет_Заказы.pdf" };
            if (sfd.ShowDialog() == true)
            {
                try
                {
                    var list = OrdersGrid.ItemsSource as List<AdminOrderViewModel>;

                    // ИСПРАВЛЕНИЕ 2-й ОШИБКИ: Добавляем QuestPDF.Fluent. перед Document
                    QuestPDF.Fluent.Document.Create(container =>
                    {
                        container.Page(page =>
                        {
                            page.Size(PageSizes.A4);
                            page.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);
                            page.PageColor(Colors.White);
                            page.DefaultTextStyle(x => x.FontSize(12));

                            page.Header().Text("Отчет по заказам ресторана")
                                .SemiBold().FontSize(20).FontColor(Colors.Blue.Darken2);

                            page.Content().PaddingVertical(15).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(60);
                                    columns.ConstantColumn(60);
                                    columns.RelativeColumn();
                                    columns.ConstantColumn(60);
                                    columns.ConstantColumn(80);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("№").Bold();
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Столик").Bold();
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Блюдо").Bold();
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Кол-во").Bold();
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Сумма").Bold();
                                });

                                foreach (var item in list)
                                {
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(item.OrderId.ToString());
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(item.TableNumber);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(item.DishName);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(item.Quantity.ToString());
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(item.ItemTotal.ToString("N2") + " р.");
                                }
                            });

                            page.Footer().AlignCenter().Text(x =>
                            {
                                x.CurrentPageNumber();
                                x.Span(" из ");
                                x.TotalPages();
                            });
                        });
                    }).GeneratePdf(sfd.FileName);

                    MessageBox.Show("Отчет успешно экспортирован в PDF!", "Успех");
                }
                catch (Exception ex) { MessageBox.Show("Ошибка при экспорте: " + ex.Message); }
            }
        }
    }
}