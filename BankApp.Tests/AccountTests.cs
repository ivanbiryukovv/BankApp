using Kurs_Bank.Models;
using System;
using Xunit;

namespace BankApp.Tests
{
    public class AccountTests
    {
        [Fact]
        public void OpenAccount_BalanceIsZeroAndStatusActive()
        {
            var account = new Account
            {
                AccountID = 1,
                PassportNumber = "1234567890",
                AccountType = "Текущий",
                Balance = 0,
                OpenDate = DateTime.Now,
                Status = "Активен"
            };
            Assert.Equal(0, account.Balance);
            Assert.Equal("Активен", account.Status);
        }

        [Fact]
        public void CloseAccount_StatusChangesToClosed()
        {
            var account = new Account
            {
                AccountID = 1,
                PassportNumber = "1234567890",
                AccountType = "Текущий",
                Balance = 0,
                OpenDate = DateTime.Now,
                Status = "Активен"
            };
            account.Status = "Закрыт";
            account.CloseDate = DateTime.Now;
            Assert.Equal("Закрыт", account.Status);
            Assert.NotNull(account.CloseDate);
        }
    }
}