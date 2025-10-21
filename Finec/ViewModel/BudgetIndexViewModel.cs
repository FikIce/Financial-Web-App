using Finec.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Finec.ViewModels
{
    // A class to hold the data for a single row in our budget table
    public class BudgetCategoryViewModel
    {
        public string CategoryName { get; set; }
        public BudgetType Type { get; set; }
        public string AccountName { get; set; } // <-- ADD THIS
        public int? AssetId { get; set; }
        public int AccountId { get; set; }
        // Dictionary to hold the values for each month: <MonthNumber, Amount>
        public Dictionary<int, decimal> MonthlyAmounts { get; set; } = new Dictionary<int, decimal>();
    }

    // The main ViewModel for the entire Budget Index page
    public class BudgetIndexViewModel
    {
        public DateTime StartDate { get; set; }
        public List<BudgetCategoryViewModel> IncomeCategories { get; set; } = new List<BudgetCategoryViewModel>();
        public List<BudgetCategoryViewModel> SavingCategories { get; set; } = new List<BudgetCategoryViewModel>();
        public Dictionary<int, decimal> MonthlyIncomeTotals { get; set; } = new Dictionary<int, decimal>();
        public Dictionary<int, decimal> MonthlySavingTotals { get; set; } = new Dictionary<int, decimal>();
    }
}
