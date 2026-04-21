using Kurs_Bank.Models;
using Microsoft.EntityFrameworkCore;

namespace Kurs_Bank.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Client> Clients { get; set; }
        public DbSet<Users> Users { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Card> Cards { get; set; }
        public DbSet<Credit> Credits { get; set; }
        public DbSet<CreditPayment> CreditPayments { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Transaction_Account> TransactionAccounts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Client>()
                .HasOne(c => c.Users)
                .WithOne(u => u.Client)
                .HasForeignKey<Users>(u => u.PassportNumber)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Account>()
                .HasOne(a => a.Client)
                .WithMany(c => c.Accounts)
                .HasForeignKey(a => a.PassportNumber)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Credit>()
                .HasOne(c => c.Client)
                .WithMany(cl => cl.Credits)
                .HasForeignKey(c => c.PassportNumber)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Credit>()
                .HasOne(c => c.Account)
                .WithMany(a => a.Credits)
                .HasForeignKey(c => c.AccountID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Card>()
                .HasOne(c => c.Account)
                .WithMany(a => a.Cards)
                .HasForeignKey(c => c.AccountID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CreditPayment>()
                .HasOne(p => p.Credit)
                .WithMany(c => c.CreditPayments)
                .HasForeignKey(p => p.CreditID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Transaction_Account>()
                .HasOne(ta => ta.Transaction)
                .WithMany(t => t.TransactionAccounts)
                .HasForeignKey(ta => ta.TransactionID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Transaction_Account>()
                .HasOne(ta => ta.Account)
                .WithMany(a => a.TransactionAccounts)
                .HasForeignKey(ta => ta.AccountID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Users>()
                .HasIndex(u => u.Login)
                .IsUnique();

            modelBuilder.Entity<Client>()
                .HasIndex(c => c.Phone)
                .IsUnique();

            modelBuilder.Entity<Users>(entity =>
            {
                entity.Property(u => u.PassportNumber)
                    .HasMaxLength(10)
                    .IsRequired();

                entity.Property(u => u.Login)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(u => u.Password)
                    .HasMaxLength(255)
                    .IsRequired();
            });

            modelBuilder.Entity<Account>()
                .Property(a => a.Balance)
                .HasPrecision(15, 2);

            modelBuilder.Entity<Credit>()
                .Property(c => c.Amount)
                .HasPrecision(15, 2);

            modelBuilder.Entity<Credit>()
                .Property(c => c.InterestRate)
                .HasPrecision(5, 2);

            modelBuilder.Entity<CreditPayment>()
                .Property(p => p.Amount)
                .HasPrecision(15, 2);

            modelBuilder.Entity<Transaction>()
                .Property(t => t.Amount)
                .HasPrecision(15, 2);
        }
    }
}