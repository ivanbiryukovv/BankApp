using Kurs_Bank.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Windows;

namespace Kurs_Bank
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
                optionsBuilder.UseSqlServer(@"Server=.\SQLEXPRESS;Database=BankDB;Trusted_Connection=True;TrustServerCertificate=True;");

                using (var db = new AppDbContext(optionsBuilder.Options))
                {
                    db.Database.Migrate();
                    DbInitializer.Initialize(db);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации базы данных: {ex.Message}\n\n{ex.InnerException?.Message}", "Ошибка");
            }
        }
    }
}