using Kurs_Bank.Data;
using Kurs_Bank.Helpers;
using Kurs_Bank.Models;
using Kurs_Bank.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Kurs_Bank
{
    public partial class CreditsPage : Page
    {
        public CreditsPage()
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
                    var accounts = db.Accounts
                        .Where(a => a.PassportNumber == passport && a.Status == AccountStatus.Active)
                        .ToList();
                    CreditAccountCombo.ItemsSource = accounts;

                    var credits = db.Credits
                        .Include(c => c.CreditPayments)
                        .Where(c => c.PassportNumber == passport)
                        .ToList();

                    var creditViewModels = credits.Select(c => new CreditViewModel
                    {
                        CreditID = c.CreditID,
                        Amount = c.Amount,
                        InterestRate = c.InterestRate,
                        TermMonths = CreditCalculator.CalculateTermMonths(c.StartDate, c.EndDate),
                        Status = c.Status,
                        AccountID = c.AccountID,
                        MonthlyPayment = CreditCalculator.CalculateMonthlyPaymentFromDates(c.Amount, c.InterestRate, c.StartDate, c.EndDate),
                        CreditPayments = c.CreditPayments.ToList()
                    }).ToList();

                    CreditsList.ItemsSource = creditViewModels;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка");
            }
        }

        private void ApplyCredit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!decimal.TryParse(CreditAmountBox.Text, out decimal amount))
                {
                    MessageBox.Show("Введите корректную сумму", "Ошибка");
                    return;
                }
                if (amount < 10000)
                {
                    MessageBox.Show("Минимальная сумма кредита: 10 000 ₽", "Ошибка");
                    return;
                }
                if (amount > 5000000)
                {
                    MessageBox.Show("Максимальная сумма кредита: 5 000 000 ₽", "Ошибка");
                    return;
                }
                if (!int.TryParse(CreditTermBox.Text, out int term) || term <= 0)
                {
                    MessageBox.Show("Введите корректный срок", "Ошибка");
                    return;
                }

                var termRange = CreditCalculator.GetTermRange(amount);
                if (term < termRange.minTerm)
                {
                    MessageBox.Show($"Минимальный срок для данной суммы: {termRange.minTerm} месяцев", "Ошибка");
                    return;
                }
                if (term > termRange.maxTerm)
                {
                    MessageBox.Show($"Максимальный срок для данной суммы: {termRange.maxTerm} месяцев", "Ошибка");
                    return;
                }
                if (CreditAccountCombo.SelectedItem == null)
                {
                    MessageBox.Show("Выберите счёт для зачисления", "Ошибка");
                    return;
                }

                var account = CreditAccountCombo.SelectedItem as Account;
                decimal rate = CreditCalculator.CalculateRate(amount, term);
                decimal monthlyPayment = CreditCalculator.CalculateMonthlyPayment(amount, rate, term);
                DateTime startDate = DateTime.Now;
                DateTime endDate = startDate.AddMonths(term);

                var confirm = MessageBox.Show(
                    $"Сумма кредита: {amount:N2} ₽\n" +
                    $"Срок: {term} мес.\n" +
                    $"Процентная ставка: {rate}%\n" +
                    $"Ежемесячный платёж: {monthlyPayment:N2} ₽\n\n" +
                    $"Подтвердить оформление?",
                    "Подтверждение", MessageBoxButton.YesNo);

                if (confirm != MessageBoxResult.Yes) return;

                using (var db = DbContextFactory.Create())
                {
                    using (var transaction = db.Database.BeginTransaction())
                    {
                        try
                        {
                            var credit = new Credit
                            {
                                PassportNumber = SessionManager.CurrentClient.PassportNumber,
                                AccountID = account.AccountID,
                                Amount = amount,
                                Status = CreditStatus.Active,
                                InterestRate = rate,
                                StartDate = startDate,
                                EndDate = endDate
                            };
                            db.Credits.Add(credit);
                            db.SaveChanges();

                            var dbAccount = db.Accounts.Find(account.AccountID);
                            dbAccount.Balance += amount;

                            for (int i = 1; i <= term; i++)
                            {
                                db.CreditPayments.Add(new CreditPayment
                                {
                                    CreditID = credit.CreditID,
                                    Amount = monthlyPayment,
                                    PlannedDate = DateTime.Now.AddMonths(i),
                                    ActualDate = null,
                                    Status = PaymentStatus.Pending
                                });
                            }
                            db.SaveChanges();

                            transaction.Commit();

                            MessageBox.Show(
                                $"Кредит оформлен!\nНа счёт зачислено: {amount:N2} ₽\nЕжемесячный платёж: {monthlyPayment:N2} ₽",
                                "Успех");
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
                MessageBox.Show($"Ошибка оформления кредита: {ex.Message}", "Ошибка");
            }
        }

        private void PayInstallment_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var btn = sender as Button;
                if (btn?.Tag == null) return;
                if (!int.TryParse(btn.Tag.ToString(), out int paymentId)) return;

                using (var db = DbContextFactory.Create())
                {
                    using (var transaction = db.Database.BeginTransaction())
                    {
                        try
                        {
                            var payment = db.CreditPayments
                                .Include(p => p.Credit)
                                .FirstOrDefault(p => p.PaymentID == paymentId);

                            if (payment == null || payment.Status != PaymentStatus.Pending)
                            {
                                MessageBox.Show("Платёж уже оплачен или не найден", "Ошибка");
                                return;
                            }

                            var account = db.Accounts.Find(payment.Credit.AccountID);
                            if (account == null)
                            {
                                MessageBox.Show("Счёт не найден", "Ошибка");
                                return;
                            }
                            if (account.Balance < payment.Amount)
                            {
                                MessageBox.Show(
                                    $"Недостаточно средств.\nНужно: {payment.Amount:N2} ₽\nНа счёте: {account.Balance:N2} ₽",
                                    "Ошибка");
                                return;
                            }

                            account.Balance -= payment.Amount;
                            payment.Status = PaymentStatus.Paid;
                            payment.ActualDate = DateTime.Now;

                            var allPaid = db.CreditPayments
                                .Where(p => p.CreditID == payment.CreditID && p.PaymentID != paymentId)
                                .All(p => p.Status == PaymentStatus.Paid);

                            if (allPaid)
                            {
                                var credit = db.Credits.Find(payment.CreditID);
                                if (credit != null) credit.Status = CreditStatus.Repaid;
                            }

                            db.SaveChanges();
                            transaction.Commit();

                            MessageBox.Show($"Платёж {payment.Amount:N2} ₽ выполнен!", "Успех");
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
                MessageBox.Show($"Ошибка оплаты: {ex.Message}", "Ошибка");
            }
        }
    }

    public class CreditViewModel
    {
        public int CreditID { get; set; }
        public decimal Amount { get; set; }
        public decimal InterestRate { get; set; }
        public int TermMonths { get; set; }
        public string Status { get; set; }
        public int AccountID { get; set; }
        public decimal MonthlyPayment { get; set; }
        public List<CreditPayment> CreditPayments { get; set; }
    }
}