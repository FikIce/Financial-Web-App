using Finec.Models;
using System.Collections.Generic;

namespace Finec.ViewModels
{
    public class IncomeBreakdownItem
    {
        public string SourceName { get; set; }
        public decimal Amount { get; set; }
    }

    public class DashboardViewModel
    {
        // Card Totals
        public decimal TotalIncome { get; set; }
        public decimal TotalSpending { get; set; }
        public decimal TotalSaving { get; set; }

        // Upgraded Metrics for Cards
        public double IncomeChangePercentage { get; set; }
        public string IncomeChangeMessage { get; set; }
        public double SpendingChangePercentage { get; set; }
        public string SpendingChangeMessage { get; set; }
        public double SavingChangePercentage { get; set; }
        public string SavingChangeMessage { get; set; }

        // Data for Charts & Lists
        public List<IncomeBreakdownItem> IncomeBreakdown { get; set; } = new List<IncomeBreakdownItem>();
        public List<Account> Accounts { get; set; } = new List<Account>();
        public List<string> ChartLabels { get; set; } = new List<string>();
        public List<decimal> ExpenseChartData { get; set; } = new List<decimal>();
        public List<decimal> IncomeChartData { get; set; } = new List<decimal>();
        public List<decimal> SavingChartData { get; set; } = new List<decimal>();
    }
}