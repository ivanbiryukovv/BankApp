using Kurs_Bank.Services;
using System;
using Xunit;

namespace BankApp.Tests
{
    public class CreditCalculatorTests
    {
        [Fact]
        public void CalculateRate_Amount100000_Returns16()
        {
            decimal amount = 100000;
            int term = 12;
            decimal expectedRate = 16.0m;
            decimal actualRate = CreditCalculator.CalculateRate(amount, term);
            Assert.Equal(expectedRate, actualRate);
        }

        [Fact]
        public void CalculateRate_Amount500000_Returns12()
        {
            decimal amount = 500000;
            int term = 12;
            decimal expectedRate = 12.0m;
            decimal actualRate = CreditCalculator.CalculateRate(amount, term);
            Assert.Equal(expectedRate, actualRate);
        }

        [Fact]
        public void CalculateMonthlyPayment_ValidData_ReturnsCorrectPayment()
        {
            decimal amount = 100000;
            decimal rate = 16.0m;
            int term = 12;
            decimal expectedPayment = Math.Round(100000 * (1 + 16.0m / 100) / 12, 2);
            decimal actualPayment = CreditCalculator.CalculateMonthlyPayment(amount, rate, term);
            Assert.Equal(expectedPayment, actualPayment);
        }

        [Fact]
        public void CalculateTermMonths_ValidDates_ReturnsCorrectMonths()
        {
            DateTime startDate = new DateTime(2024, 1, 15);
            DateTime endDate = new DateTime(2025, 1, 15);
            int expectedMonths = 12;
            int actualMonths = CreditCalculator.CalculateTermMonths(startDate, endDate);
            Assert.Equal(expectedMonths, actualMonths);
        }
    }
}