using Kurs_Bank.Data;
using Kurs_Bank.Helpers;
using Kurs_Bank.Models;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Kurs_Bank.Pages
{
    public partial class TransactionsPage : Page
    {
        public TransactionsPage()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                var passport = SessionManager.CurrentClient.PassportNumber;
                using (var db = DbContextFactory.Create())
                {
                    var accountIds = db.Accounts
                        .Where(a => a.PassportNumber == passport && a.Status == AccountStatus.Active)
                        .Select(a => a.AccountID)
                        .ToList();

                    var cards = db.Cards
                        .Include(c => c.Account)
                        .Where(c => accountIds.Contains(c.AccountID) && c.Status == CardStatus.Active)
                        .ToList();

                    FromCardCombo.ItemsSource = cards;

                    var transactions = db.TransactionAccounts
                        .Include(ta => ta.Transaction)
                        .Where(ta => accountIds.Contains(ta.AccountID))
                        .Select(ta => ta.Transaction)
                        .Distinct()
                        .OrderByDescending(t => t.OperationDate)
                        .ToList();

                    TransactionsGrid.ItemsSource = transactions;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка");
            }
        }

        private void ToCardNumberBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                string cardNumber = ToCardNumberBox.Text.Trim();
                if (cardNumber.Length != 16)
                {
                    ReceiverInfoBorder.Visibility = Visibility.Collapsed;
                    return;
                }

                using (var db = DbContextFactory.Create())
                {
                    var card = db.Cards
                        .Include(c => c.Account)
                        .ThenInclude(a => a.Client)
                        .FirstOrDefault(c => c.CardNumber == cardNumber);

                    if (card != null && card.Status == CardStatus.Active)
                    {
                        ReceiverInfoText.Text =
                            $"{card.Account.Client.LastName} {card.Account.Client.FirstName} {card.Account.Client.MiddleName}";
                        ReceiverInfoBorder.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        ReceiverInfoBorder.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch (Exception ex)
            {
                ReceiverInfoBorder.Visibility = Visibility.Collapsed;
            }
        }

        private void SendMoney_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (FromCardCombo.SelectedItem == null)
                {
                    MessageBox.Show("Выберите карту отправителя", "Ошибка");
                    return;
                }
                if (string.IsNullOrWhiteSpace(ToCardNumberBox.Text) || ToCardNumberBox.Text.Trim().Length != 16)
                {
                    MessageBox.Show("Введите корректный номер карты получателя (16 цифр)", "Ошибка");
                    return;
                }
                if (!decimal.TryParse(AmountBox.Text, out decimal amount) || amount <= 0)
                {
                    MessageBox.Show("Введите корректную сумму", "Ошибка");
                    return;
                }
                if (amount < 1)
                {
                    MessageBox.Show("Минимальная сумма перевода: 1 ₽", "Ошибка");
                    return;
                }
                if (amount > 500000)
                {
                    MessageBox.Show("Максимальная сумма одного перевода: 500 000 ₽", "Ошибка");
                    return;
                }

                var fromCard = FromCardCombo.SelectedItem as Card;
                string toCardNumber = ToCardNumberBox.Text.Trim();

                if (fromCard.CardNumber == toCardNumber)
                {
                    MessageBox.Show("Нельзя переводить на ту же карту", "Ошибка");
                    return;
                }

                using (var db = DbContextFactory.Create())
                {
                    using (var transaction = db.Database.BeginTransaction())
                    {
                        try
                        {
                            var toCard = db.Cards
                                .Include(c => c.Account)
                                .FirstOrDefault(c => c.CardNumber == toCardNumber);

                            if (toCard == null)
                            {
                                MessageBox.Show("Карта получателя не найдена", "Ошибка");
                                return;
                            }
                            if (toCard.Status == CardStatus.Blocked)
                            {
                                MessageBox.Show("Карта получателя заблокирована", "Ошибка");
                                return;
                            }
                            if (toCard.Account.Status == AccountStatus.Closed || toCard.Account.Status == AccountStatus.Blocked)
                            {
                                MessageBox.Show("Счёт получателя недоступен", "Ошибка");
                                return;
                            }

                            var fromAccount = db.Accounts.Find(fromCard.AccountID);
                            if (fromAccount.Balance < amount)
                            {
                                MessageBox.Show(
                                    $"Недостаточно средств.\nДоступно: {fromAccount.Balance:N2} ₽\nНужно: {amount:N2} ₽",
                                    "Ошибка");
                                return;
                            }

                            var toClient = db.Clients.Find(toCard.Account.PassportNumber);
                            var confirm = MessageBox.Show(
                                $"Перевести {amount:N2} ₽?\n\n" +
                                $"С карты: {fromCard.FormattedCardNumber}\n" +
                                $"На карту: {toCard.FormattedCardNumber}\n" +
                                $"Получатель: {toClient?.LastName} {toClient?.FirstName}",
                                "Подтверждение перевода",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Question);

                            if (confirm != MessageBoxResult.Yes) return;

                            fromAccount.Balance -= amount;
                            toCard.Account.Balance += amount;

                            var newTransaction = new Transaction
                            {
                                Amount = amount,
                                Status = TransactionStatus.Completed,
                                Type = TransactionType.Transfer,
                                OperationDate = DateTime.Now,
                                Comment = $"Перевод с карты {fromCard.FormattedCardNumber} на карту {toCard.FormattedCardNumber}"
                            };
                            db.Transactions.Add(newTransaction);
                            db.SaveChanges();

                            db.TransactionAccounts.Add(new Transaction_Account
                            {
                                TransactionID = newTransaction.TransactionID,
                                AccountID = fromAccount.AccountID,
                                Role = "Отправитель"
                            });
                            db.TransactionAccounts.Add(new Transaction_Account
                            {
                                TransactionID = newTransaction.TransactionID,
                                AccountID = toCard.Account.AccountID,
                                Role = "Получатель"
                            });
                            db.SaveChanges();

                            transaction.Commit();

                            MessageBox.Show("Перевод выполнен успешно!", "Успех");
                            ToCardNumberBox.Text = "";
                            AmountBox.Text = "";
                            ReceiverInfoBorder.Visibility = Visibility.Collapsed;
                            LoadData();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при переводе: {ex.Message}", "Ошибка");
            }
        }

        private void ExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var passport = SessionManager.CurrentClient.PassportNumber;

                using (var db = DbContextFactory.Create())
                {
                    var accountIds = db.Accounts
                        .Where(a => a.PassportNumber == passport)
                        .Select(a => a.AccountID)
                        .ToList();

                    var transactions = db.TransactionAccounts
                        .Include(ta => ta.Transaction)
                        .Where(ta => accountIds.Contains(ta.AccountID))
                        .Select(ta => ta.Transaction)
                        .Distinct()
                        .OrderByDescending(t => t.OperationDate)
                        .ToList();

                    if (!transactions.Any())
                    {
                        MessageBox.Show("Нет транзакций для экспорта", "Информация");
                        return;
                    }

                    var saveDialog = new Microsoft.Win32.SaveFileDialog
                    {
                        FileName = $"Транзакции_{DateTime.Now:dd-MM-yyyy}",
                        DefaultExt = ".xlsx",
                        Filter = "Excel файлы (.xlsx)|*.xlsx"
                    };

                    if (saveDialog.ShowDialog() != true) return;

                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Транзакции");

                        worksheet.Cell(1, 1).Value = "№";
                        worksheet.Cell(1, 2).Value = "Дата и время";
                        worksheet.Cell(1, 3).Value = "Тип";
                        worksheet.Cell(1, 4).Value = "Сумма (₽)";
                        worksheet.Cell(1, 5).Value = "Статус";
                        worksheet.Cell(1, 6).Value = "Комментарий";

                        var headerRow = worksheet.Row(1);
                        headerRow.Style.Font.Bold = true;
                        headerRow.Style.Font.FontColor = XLColor.White;
                        headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#5C3FBE");
                        headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        for (int i = 0; i < transactions.Count; i++)
                        {
                            var t = transactions[i];
                            int row = i + 2;

                            worksheet.Cell(row, 1).Value = i + 1;
                            worksheet.Cell(row, 2).Value = t.OperationDate.ToString("dd.MM.yyyy HH:mm");
                            worksheet.Cell(row, 3).Value = t.Type;
                            worksheet.Cell(row, 4).Value = (double)t.Amount;
                            worksheet.Cell(row, 5).Value = t.Status;
                            worksheet.Cell(row, 6).Value = t.Comment ?? "";

                            if (i % 2 == 0)
                            {
                                worksheet.Row(row).Style.Fill.BackgroundColor =
                                    XLColor.FromHtml("#F0EAFB");
                            }

                            var statusCell = worksheet.Cell(row, 5);
                            if (t.Status == TransactionStatus.Completed)
                                statusCell.Style.Font.FontColor = XLColor.Green;
                            else if (t.Status == TransactionStatus.Rejected)
                                statusCell.Style.Font.FontColor = XLColor.Red;
                            else
                                statusCell.Style.Font.FontColor = XLColor.Orange;
                        }

                        int lastRow = transactions.Count + 2;
                        worksheet.Cell(lastRow, 3).Value = "ИТОГО:";
                        worksheet.Cell(lastRow, 3).Style.Font.Bold = true;
                        worksheet.Cell(lastRow, 4).Value = (double)transactions.Sum(t => t.Amount);
                        worksheet.Cell(lastRow, 4).Style.Font.Bold = true;
                        worksheet.Cell(lastRow, 4).Style.Font.FontColor = XLColor.FromHtml("#5C3FBE");

                        worksheet.Columns().AdjustToContents();

                        var tableRange = worksheet.Range(1, 1, lastRow, 6);
                        tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                        tableRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#5C3FBE");
                        tableRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#A78BFA");

                        workbook.SaveAs(saveDialog.FileName);
                    }

                    MessageBox.Show($"Экспорт выполнен успешно!\nФайл сохранён: {saveDialog.FileName}", "Успех");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка");
            }
        }
    }
}