using System;
using System.Collections.Generic;

namespace Finec.ViewModels
{
    // This is a dedicated ViewModel to prevent data contamination.
    public class ExpensesIndexViewModel
    {
        public DateTime StartDate { get; set; }
        public List<BudgetCategoryViewModel> ExpenseCategories { get; set; } = new List<BudgetCategoryViewModel>();
        public Dictionary<int, decimal> MonthlyExpenseTotals { get; set; } = new Dictionary<int, decimal>();
    }
}
