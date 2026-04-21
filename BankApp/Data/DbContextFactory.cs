using Microsoft.EntityFrameworkCore;
using System.Configuration;

namespace Kurs_Bank.Data
{
    public static class DbContextFactory
    {
        private static string _connectionString;

        static DbContextFactory()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["BankDB"]?.ConnectionString;
            if (string.IsNullOrEmpty(_connectionString))
            {
                _connectionString = @"Server=.\SQLEXPRESS;Database=BankDB;Trusted_Connection=True;TrustServerCertificate=True;";
            }
        }

        public static AppDbContext Create()
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlServer(_connectionString);
            return new AppDbContext(optionsBuilder.Options);
        }

        public static DbContextOptions<AppDbContext> GetOptions()
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlServer(_connectionString);
            return optionsBuilder.Options;
        }
    }
}