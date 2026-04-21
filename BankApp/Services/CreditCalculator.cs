using System;

namespace Kurs_Bank.Services
{
    public static class CreditCalculator
    {
        public static decimal CalculateRate(decimal amount, int term)
        {
            decimal rate;

            if (amount >= 1000000)
                rate = 10.0m;
            else if (amount >= 500000)
                rate = 12.0m;
            else if (amount >= 300000)
                rate = 14.0m;
            else if (amount >= 100000)
                rate = 16.0m;
            else
                rate = 20.0m;

            if (term >= 36)
                rate = Math.Max(10.0m, rate - 2.0m);
            else if (term >= 24)
                rate = Math.Max(10.0m, rate - 1.0m);

            return rate;
        }

        public static (int minTerm, int maxTerm) GetTermRange(decimal amount)
        {
            if (amount <= 100000) return (3, 24);
            if (amount <= 500000) return (6, 60);
            return (12, 84);
        }

        public static decimal CalculateMonthlyPayment(decimal amount, decimal rate, int term)
        {
            return Math.Round(amount * (1 + rate / 100) / term, 2);
        }

        public static int CalculateTermMonths(DateTime startDate, DateTime endDate)
        {
            return (int)Math.Round((endDate - startDate).TotalDays / 30.44);
        }

        public static decimal CalculateMonthlyPaymentFromDates(decimal amount, decimal rate, DateTime startDate, DateTime endDate)
        {
            int term = CalculateTermMonths(startDate, endDate);
            return CalculateMonthlyPayment(amount, rate, term);
        }
    }
}