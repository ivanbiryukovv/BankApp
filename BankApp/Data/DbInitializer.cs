using Kurs_Bank.Models;
using System;
using System.Linq;

namespace Kurs_Bank.Data
{
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext context)
        {
            context.Database.EnsureCreated();

            if (context.Users.Any())
            {
                return;
            }

            var client = new Client
            {
                PassportNumber = "1234567890",
                LastName = "Admin",
                FirstName = "Admin",
                MiddleName = "Adminovich",
                BirthDate = new DateTime(1990, 1, 1),
                Phone = "+79999999999",
                City = "Москва",
                Street = "Центральная",
                House = "1"
            };

            var user = new Users
            {
                PassportNumber = "1234567890",
                Login = "admin",
                Password = "admin123"
            };

            var account = new Account
            {
                PassportNumber = "1234567890",
                AccountType = "Текущий",
                Balance = 100000,
                OpenDate = DateTime.Now,
                Status = "Активен"
            };

            var card = new Card
            {
                CardNumber = "1111222233334444",
                CardType = "Дебетовая",
                IssueDate = DateTime.Now,
                ExpiryDate = DateTime.Now.AddYears(4),
                Status = "Активна"
            };

            client.Users = user;
            client.Accounts.Add(account);
            account.Cards.Add(card);

            context.Clients.Add(client);
            context.SaveChanges();
        }
    }
}