using Kurs_Bank.Models;
using System;
using Xunit;

namespace BankApp.Tests
{
    public class CardTests
    {
        [Fact]
        public void CardNumber_Is16Digits()
        {
            var card = new Card
            {
                CardNumber = "1111222233334444",
                AccountID = 1,
                CardType = "Дебетовая",
                IssueDate = DateTime.Now,
                ExpiryDate = DateTime.Now.AddYears(4),
                Status = "Активна"
            };
            Assert.Equal(16, card.CardNumber.Length);
        }

        [Fact]
        public void BlockCard_StatusChangesToBlocked()
        {
            var card = new Card
            {
                CardNumber = "1111222233334444",
                AccountID = 1,
                CardType = "Дебетовая",
                IssueDate = DateTime.Now,
                ExpiryDate = DateTime.Now.AddYears(4),
                Status = "Активна"
            };
            card.Status = "Заблокирована";
            Assert.Equal("Заблокирована", card.Status);
        }
    }
}